using System.Collections.Generic;
using System.Xml;
using System.Xml.XPath;

namespace CodeSequences
{
	internal class _1_ugly_mess
	{
		private readonly XmlDocument _character = new XmlDocument();

		// Logging in is someone else's concern. Assume that is done before calling Parse.
		private readonly WotcClient _wotcService = new WotcClient();

		public IEnumerable<CardViewModel> ParseCharacterIntoCards()
		{
			foreach (XPathNavigator powerElements in _character.CreateNavigator().Select("details/detail[@type='power']"))
			{
				var name = powerElements.GetAttribute("Name", "");
				var powerId = powerElements.GetAttribute("Id", "");
				var math = _character.SelectNodes(string.Format("calculations/power[@name='{0}']", name)).Item(0).Value;

				var powerDetails = _wotcService.GetPowerDetails(powerId);
				powerDetails = _CleanTheText(powerDetails);
				var powerInfo = new XmlDocument();
				powerInfo.LoadXml(powerDetails);
				powerInfo = _CleanTheXml(powerInfo);

				yield return new CardViewModel
				{
					Title = name,
					Subtitle = string.Format("{0} {1} {2}", _Source(powerInfo), _Kind(powerInfo), _Level(powerInfo)),
					Details = _ToBlocks(_DetailParagraphs(powerInfo)),
					Color = _ToColor(_Refresh(powerInfo)),
					UnderlyingCalculations = math
				};
			}
		}

		private CardColor _ToColor(string refresh) {}

		private string _Refresh(XmlDocument powerInfo) {}

		private IEnumerable<Block> _ToBlocks(IEnumerable<string> detailParagraphs) {}

		private IEnumerable<string> _DetailParagraphs(XmlDocument powerInfo) {}

		private string _Level(XmlDocument powerInfo) {}

		private string _Kind(XmlDocument powerInfo) {}

		private string _Source(XmlDocument powerInfo) {}

		private XmlDocument _CleanTheXml(XmlDocument powerInfo) {}

		private string _CleanTheText(string powerDetails) {}
	}

	public class WotcClient
	{
		public string GetPowerDetails(string powerId) {}
	}

	public enum CardColor
	{
		Red,
		Green,
		Black,
		Blue,
		Silver,
		Gold
	}

	public class CardViewModel
	{
		public CardColor Color;
		public IEnumerable<Block> Details;
		public string Subtitle;
		public string Title;
		public string UnderlyingCalculations;
	}

	public class Block {}
}
