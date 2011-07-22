using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Xml.XPath;

namespace CodeSequences
{
	internal class _6_split_out_the_classes
	{
		private readonly XmlDocument _character = new XmlDocument();

		public IEnumerable<CardViewModel> ParseCharacterIntoCards()
		{
			var pipeline = CreatePipeline();
			return
				FindAllPowers().Cast<XPathNavigator>().Select(
					powerElement => ApplyTransformPipeline(powerElement, pipeline, _character));
		}

		public static CardViewModel ApplyTransformPipeline(XPathNavigator powerElement,
			IEnumerable<Operation<PowerPipelineState>> pipeline, XmlDocument character)
		{
			var state = new PowerPipelineState(powerElement, character.CreateNavigator());
			state = pipeline.Aggregate(state, (current, op) => op.Apply(current));
			return state.ViewModel;
		}

		public XPathNodeIterator FindAllPowers()
		{
			return _character.CreateNavigator().Select("details/detail[@type='power']");
		}

		public List<Operation<PowerPipelineState>> CreatePipeline()
		{
			return new List<Operation<PowerPipelineState>>
			{
				new ToPowerInfo(),
				new GetOnlineInfoForPower(),
				new CleanTheResponse(),
				new CreateViewModel()
			};
		}
	}

	public interface Operation<T>
	{
		T Apply(T state);
	}

	public class ToPowerInfo : Operation<PowerPipelineState>
	{
		public PowerPipelineState Apply(PowerPipelineState state)
		{
			var name = state.PowerElement.GetAttribute("Name", "");
			var powerId = state.PowerElement.GetAttribute("Id", "");
			var math =
				state.Character.Select(string.Format("calculations/power[@name='{0}']", name)).Cast<XPathNavigator>().First().Value;
			state.LocalInfo = new PowerLocalInfo(name, powerId, math);
			return state;
		}
	}

	public class GetOnlineInfoForPower : Operation<PowerPipelineState>
	{
		// Logging in is someone else's concern. Assume that is done before calling Parse.
		private readonly WotcClient _wotcService = new WotcClient();

		public PowerPipelineState Apply(PowerPipelineState state)
		{
			state.WotcResponse = _wotcService.GetPowerDetails(state.LocalInfo.PowerId);
			return state;
		}
	}

	public class CleanTheResponse : Operation<PowerPipelineState>
	{
		private readonly WotcResponseCleaner _cleaner = new WotcResponseCleaner();

		public PowerPipelineState Apply(PowerPipelineState state)
		{
			state.CleanResponse = _cleaner.CleanTheXml(_ParseXml(_cleaner.CleanTheText(state.WotcResponse)));
			return state;
		}

		private XmlDocument _ParseXml(string powerDetails)
		{
			var powerInfo = new XmlDocument();
			powerInfo.LoadXml(powerDetails);
			return powerInfo;
		}
	}

	public class CreateViewModel : Operation<PowerPipelineState>
	{
		private readonly PowerFormatter _formatter = new PowerFormatter();

		public PowerPipelineState Apply(PowerPipelineState state)
		{
			state.ViewModel = new CardViewModel
			{
				Title = state.LocalInfo.Name,
				Subtitle =
					string.Format("{0} {1} {2}", _formatter.Source(state.CleanResponse), _formatter.Kind(state.CleanResponse),
						_formatter.Level(state.CleanResponse)),
				Details = _formatter.ToBlocks(_formatter.DetailParagraphs(state.CleanResponse)),
				Color = _formatter.ToColor(_formatter.Refresh(state.CleanResponse)),
				UnderlyingCalculations = state.LocalInfo.Math
			};
			return state;
		}
	}
}
