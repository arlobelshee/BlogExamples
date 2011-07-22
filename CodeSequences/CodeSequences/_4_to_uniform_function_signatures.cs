using System.Collections.Generic;
using System.Xml;
using System.Xml.XPath;

namespace CodeSequences
{
	internal class _4_to_uniform_function_signatures
	{
		private readonly XmlDocument _character = new XmlDocument();
		private readonly WotcResponseCleaner _cleaner = new WotcResponseCleaner();
		private readonly PowerFormatter _formatter = new PowerFormatter();

		// Would be correct client. Logging in is someone else's concern. Assume that is done before calling Parse.
		private WotcClient _wotcService;

		public IEnumerable<CardViewModel> ParseCharacterIntoCards()
		{
			foreach (XPathNavigator powerElement in _character.CreateNavigator().Select("details/detail[@type='power']"))
			{
				var state = new PowerPipelineState(powerElement);
				state = _ToPowerInfo(state);
				state = _GetOnlineInfoForPower(state);
				state = _CleanTheResponse(state);
				state = _CreateViewModel(state);
				yield return state.ViewModel;
			}
		}

		private PowerPipelineState _CreateViewModel(PowerPipelineState state)
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

		private PowerPipelineState _GetOnlineInfoForPower(PowerPipelineState state)
		{
			state.WotcResponse = _wotcService.GetPowerDetails(state.LocalInfo.PowerId);
			return state;
		}

		private PowerPipelineState _CleanTheResponse(PowerPipelineState state)
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

		private PowerPipelineState _ToPowerInfo(PowerPipelineState state)
		{
			var name = state.PowerElement.GetAttribute("Name", "");
			var powerId = state.PowerElement.GetAttribute("Id", "");
			var math = _character.SelectNodes(string.Format("calculations/power[@name='{0}']", name)).Item(0).Value;
			state.LocalInfo = new PowerLocalInfo(name, powerId, math);
			return state;
		}
	}

	public class PowerPipelineState
	{
		public PowerPipelineState(XPathNavigator powerElement)
		{
			PowerElement = powerElement;
		}

		public XPathNavigator PowerElement { get; set; }
		public PowerLocalInfo LocalInfo { get; set; }
		public string WotcResponse { get; set; }
		public XmlDocument CleanResponse { get; set; }
		public CardViewModel ViewModel { get; set; }
	}
}
