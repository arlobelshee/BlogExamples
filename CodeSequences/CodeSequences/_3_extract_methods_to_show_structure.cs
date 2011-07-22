using System.Collections.Generic;
using System.Xml;
using System.Xml.XPath;

namespace CodeSequences
{
	internal class _3_extract_methods_to_show_structure
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
				var localInfo = _ToPowerInfo(powerElement);
				var powerDetails = _GetOnlineInfoForPower(localInfo);
				var powerInfo = _CleanTheResponse(powerDetails);
				yield return _CreateViewModel(localInfo, powerInfo);
			}
		}

		private CardViewModel _CreateViewModel(PowerLocalInfo localInfo, XmlDocument powerInfo)
		{
			return new CardViewModel
			{
				Title = localInfo.Name,
				Subtitle =
					string.Format("{0} {1} {2}", _formatter.Source(powerInfo), _formatter.Kind(powerInfo), _formatter.Level(powerInfo)),
				Details = _formatter.ToBlocks(_formatter.DetailParagraphs(powerInfo)),
				Color = _formatter.ToColor(_formatter.Refresh(powerInfo)),
				UnderlyingCalculations = localInfo.Math
			};
		}

		private string _GetOnlineInfoForPower(PowerLocalInfo localInfo)
		{
			return _wotcService.GetPowerDetails(localInfo.PowerId);
		}

		private XmlDocument _CleanTheResponse(string powerDetails)
		{
			powerDetails = _cleaner.CleanTheText(powerDetails);
			return _cleaner.CleanTheXml(_ParseXml(powerDetails));
		}

		private XmlDocument _ParseXml(string powerDetails)
		{
			var powerInfo = new XmlDocument();
			powerInfo.LoadXml(powerDetails);
			return powerInfo;
		}

		private PowerLocalInfo _ToPowerInfo(XPathNavigator powerElement)
		{
			var name = powerElement.GetAttribute("Name", "");
			var powerId = powerElement.GetAttribute("Id", "");
			var math = _character.SelectNodes(string.Format("calculations/power[@name='{0}']", name)).Item(0).Value;
			return new PowerLocalInfo(name, powerId, math);
		}
	}
}
