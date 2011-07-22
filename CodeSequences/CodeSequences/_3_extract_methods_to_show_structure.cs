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
		private readonly WotcClient _wotcService = new WotcClient();

		public IEnumerable<CardViewModel> ParseCharacterIntoCards()
		{
			foreach (XPathNavigator powerElement in _character.CreateNavigator().Select("details/detail[@type='power']"))
			{
				var localInfo = ToPowerInfo(powerElement);
				var powerDetails = GetOnlineInfoForPower(localInfo);
				var powerInfo = CleanTheResponse(powerDetails);
				yield return CreateViewModel(localInfo, powerInfo);
			}
		}

		public CardViewModel CreateViewModel(PowerLocalInfo localInfo, XmlDocument powerInfo)
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

		public string GetOnlineInfoForPower(PowerLocalInfo localInfo)
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
}
