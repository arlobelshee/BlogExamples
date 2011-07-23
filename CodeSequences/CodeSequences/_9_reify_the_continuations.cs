using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.XPath;

namespace CodeSequences
{
	internal class _9_reify_the_continuations
	{
		private readonly XmlDocument _character = new XmlDocument();
		private readonly WotcResponseCleaner _cleaner = new WotcResponseCleaner();
		private readonly PowerFormatter _formatter = new PowerFormatter();

		// Logging in is someone else's concern. However, it can be done before or after calling Parse.
		private readonly AsyncWotcClient _wotcService = new AsyncWotcClient();

		public IEnumerable<CardViewModel> ParseCharacterIntoCards()
		{
			return FindAllPowers().Select(ToPowerInfo).Select(ParseOneCard);
		}

		private CardViewModel ParseOneCard(PowerLocalInfo localInfo)
		{
			var card = new CardViewModel();
			ParseIntoCard(localInfo, card);
			return card;
		}

		public Continuation ParseIntoCard(PowerLocalInfo localInfo, CardViewModel card)
		{
			return
				Start.With(() => GetOnlineInfoForPower(localInfo)).Then(s => CleanTheResponse(s)).Then(
					t => UpdateViewModel(localInfo, t, card));
		}

		public ParallelQuery<XPathNavigator> FindAllPowers()
		{
			return _character.CreateNavigator().Select("details/detail[@type='power']").AsParallel().Cast<XPathNavigator>();
		}

		public void UpdateViewModel(PowerLocalInfo localInfo, XmlDocument powerInfo, CardViewModel card)
		{
			card.Title = localInfo.Name;
			card.Subtitle =
				string.Format("{0} {1} {2}", _formatter.Source(powerInfo), _formatter.Kind(powerInfo), _formatter.Level(powerInfo));
			card.Details = _formatter.ToBlocks(_formatter.DetailParagraphs(powerInfo));
			card.Color = _formatter.ToColor(_formatter.Refresh(powerInfo));
			card.UnderlyingCalculations = localInfo.Math;
		}

		public Task<string> GetOnlineInfoForPower(PowerLocalInfo localInfo)
		{
			return _wotcService.GetPowerDetails(localInfo.PowerId);
		}

		public XmlDocument CleanTheResponse(string powerDetails)
		{
			powerDetails = _cleaner.CleanTheText(powerDetails);
			return _cleaner.CleanTheXml(_ParseXml(powerDetails));
		}

		public PowerLocalInfo ToPowerInfo(XPathNavigator powerElement)
		{
			var name = powerElement.GetAttribute("Name", "");
			var powerId = powerElement.GetAttribute("Id", "");
			var math = _character.SelectNodes(string.Format("calculations/power[@name='{0}']", name)).Item(0).Value;
			return new PowerLocalInfo(name, powerId, math);
		}

		private XmlDocument _ParseXml(string powerDetails)
		{
			var powerInfo = new XmlDocument();
			powerInfo.LoadXml(powerDetails);
			return powerInfo;
		}
	}

	public static class Start
	{
		public static Continuation<T> With<T>(Expression<Func<Task<T>>> taskGenerator)
		{
			var thisStep = new BindingInfo(taskGenerator);
			return new Continuation<T>(taskGenerator.Compile()(), thisStep);
		}
	}

	public static class ContinuationExtensions
	{
		public static Continuation<R> Then<T, R>(this Task<T> input, Expression<Func<T, R>> op)
		{
			var compiledOp = op.Compile();
			var task = input.ContinueWith(t => compiledOp(t.Result));
			return new Continuation<R>(task, new BindingInfo(op));
		}

		public static Continuation Then<T>(this Task<T> input, Expression<Action<T>> op)
		{
			var compiledOp = op.Compile();
			var task = input.ContinueWith(t => compiledOp(t.Result));
			return new Continuation(task, new BindingInfo(op));
		}

		public static Continuation<R> Then<R>(this Task input, Expression<Func<R>> op)
		{
			var compiledOp = op.Compile();
			var task = input.ContinueWith(t => compiledOp());
			return new Continuation<R>(task, new BindingInfo(op));
		}

		public static Continuation Then<T>(this Task input, Expression<Action> op)
		{
			var compiledOp = op.Compile();
			var task = input.ContinueWith(t => compiledOp());
			return new Continuation(task, new BindingInfo(op));
		}
	}

	public class BindingInfo
	{
		/// <summary>
		/// 	Given a lambda that consists of a single method call, gets the binding info for that inner method call.
		/// 
		/// 	This method is implemented correctly in the Extract class in my ArsMagicaEditor sample. I've not duplicated it here because
		/// 	the implementation is large enough to distract from the rest of this example.
		/// </summary>
		/// <param name = "wrappedMethod">A lambda that is a call to exactly one method or property</param>
		public BindingInfo(LambdaExpression wrappedMethod) {}

		public MethodInfo Method { get; private set; }
		public object Target { get; private set; }
	}

	public interface IContinue
	{
		IEnumerable<BindingInfo> CallSequence { get; }
	}

	public class Continuation<TResult> : IContinue
	{
		private readonly IContinue _previousStep;
		private readonly Task<TResult> _task;
		private readonly BindingInfo _thisStep;

		public Continuation(Task<TResult> task, BindingInfo thisStep) : this(null, task, thisStep) {}

		public Continuation(IContinue previousStep, Task<TResult> task, BindingInfo thisStep)
		{
			_previousStep = previousStep;
			_task = task;
			_thisStep = thisStep;
		}

		public IEnumerable<BindingInfo> CallSequence
		{
			get
			{
				var thisStep = new[] {_thisStep};
				return _previousStep == null ? thisStep : _previousStep.CallSequence.Concat(thisStep);
			}
		}

		public Continuation<R> Then<R>(Expression<Func<TResult, R>> op)
		{
			var compiledOp = op.Compile();
			var task = _task.ContinueWith(t => compiledOp(t.Result));
			return new Continuation<R>(this, task, new BindingInfo(op));
		}

		public Continuation Then(Expression<Action<TResult>> op)
		{
			var compiledOp = op.Compile();
			var task = _task.ContinueWith(t => compiledOp(t.Result));
			return new Continuation(this, task, new BindingInfo(op));
		}
	}

	public class Continuation : IContinue
	{
		private readonly IContinue _previousStep;
		private readonly Task _task;
		private readonly BindingInfo _thisStep;

		public Continuation(Task task, BindingInfo thisStep) : this(null, task, thisStep) {}

		public Continuation(IContinue previousStep, Task task, BindingInfo thisStep)
		{
			_previousStep = previousStep;
			_task = task;
			_thisStep = thisStep;
		}

		public IEnumerable<BindingInfo> CallSequence
		{
			get
			{
				var thisStep = new[] {_thisStep};
				return _previousStep == null ? thisStep : _previousStep.CallSequence.Concat(thisStep);
			}
		}

		public Continuation<R> Then<R>(Expression<Func<R>> op)
		{
			var compiledOp = op.Compile();
			var task = _task.ContinueWith(t => compiledOp());
			return new Continuation<R>(this, task, new BindingInfo(op));
		}

		public Continuation Then(Expression<Action> op)
		{
			var compiledOp = op.Compile();
			var task = _task.ContinueWith(t => compiledOp());
			return new Continuation(this, task, new BindingInfo(op));
		}
	}
}
