using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Xml.XPath;

namespace CodeSequences
{
	internal class _5_code_becomes_data
	{
		private readonly XmlDocument _character = new XmlDocument();
		private readonly WotcResponseCleaner _cleaner = new WotcResponseCleaner();
		private readonly PowerFormatter _formatter = new PowerFormatter();

		// Would be correct client. Logging in is someone else's concern. Assume that is done before calling Parse.
		private readonly WotcClient _wotcService = new WotcClient();

		public IEnumerable<CardViewModel> ParseCharacterIntoCards()
		{
			var pipeline = CreatePipeline();
			return FindAllPowers().Cast<XPathNavigator>().Select(powerElement => ApplyTransformPipeline(powerElement, pipeline, _character));
		}

		public static CardViewModel ApplyTransformPipeline(XPathNavigator powerElement,
			IEnumerable<Func<PowerPipelineState, PowerPipelineState>> pipeline, XmlDocument character)
		{
			var state = new PowerPipelineState(powerElement, character.CreateNavigator());
			state = pipeline.Aggregate(state, (current, op) => op(current));
			return state.ViewModel;
		}

		public XPathNodeIterator FindAllPowers()
		{
			return _character.CreateNavigator().Select("details/detail[@type='power']");
		}

		public List<Func<PowerPipelineState, PowerPipelineState>> CreatePipeline()
		{
			return new List<Func<PowerPipelineState, PowerPipelineState>>
			{
				ToPowerInfo,
				GetOnlineInfoForPower,
				CleanTheResponse,
				CreateViewModel
			};
		}

		public PowerPipelineState CreateViewModel(PowerPipelineState state)
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

		public PowerPipelineState GetOnlineInfoForPower(PowerPipelineState state)
		{
			state.WotcResponse = _wotcService.GetPowerDetails(state.LocalInfo.PowerId);
			return state;
		}

		public PowerPipelineState CleanTheResponse(PowerPipelineState state)
		{
			state.CleanResponse = _cleaner.CleanTheXml(_ParseXml(_cleaner.CleanTheText(state.WotcResponse)));
			return state;
		}

		public PowerPipelineState ToPowerInfo(PowerPipelineState state)
		{
			var name = state.PowerElement.GetAttribute("Name", "");
			var powerId = state.PowerElement.GetAttribute("Id", "");
			var math = state.Character.Select(string.Format("calculations/power[@name='{0}']", name)).Cast<XPathNavigator>().First().Value;
			state.LocalInfo = new PowerLocalInfo(name, powerId, math);
			return state;
		}

		private XmlDocument _ParseXml(string powerDetails)
		{
			var powerInfo = new XmlDocument();
			powerInfo.LoadXml(powerDetails);
			return powerInfo;
		}
	}
}
