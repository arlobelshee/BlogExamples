using System.Collections.Generic;
using System.Xml;
using System.Xml.XPath;
using System.Linq;

namespace CodeSequences
{
	internal class _4_to_uniform_function_signatures
	{
		private readonly XmlDocument _character = new XmlDocument();
		private readonly WotcResponseCleaner _cleaner = new WotcResponseCleaner();
		private readonly PowerFormatter _formatter = new PowerFormatter();

		// Logging in is someone else's concern. Assume that is done before calling Parse.
		private readonly WotcClient _wotcService = new WotcClient();

		public IEnumerable<CardViewModel> ParseCharacterIntoCards()
		{
			foreach (XPathNavigator powerElement in FindAllPowers())
			{
				var state = new PowerPipelineState(powerElement, _character.CreateNavigator());
				state = ToPowerInfo(state);
				state = GetOnlineInfoForPower(state);
				state = CleanTheResponse(state);
				state = CreateViewModel(state);
				yield return state.ViewModel;
			}
		}

		public XPathNodeIterator FindAllPowers()
		{
			return _character.CreateNavigator().Select("details/detail[@type='power']");
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

	public class PowerPipelineState
	{
		public PowerPipelineState(XPathNavigator powerElement, XPathNavigator character)
		{
			PowerElement = powerElement;
			Character = character;
		}

		public XPathNavigator PowerElement { get; set; }
		public XPathNavigator Character { get; set; }
		public PowerLocalInfo LocalInfo { get; set; }
		public string WotcResponse { get; set; }
		public XmlDocument CleanResponse { get; set; }
		public CardViewModel ViewModel { get; set; }
	}
}
