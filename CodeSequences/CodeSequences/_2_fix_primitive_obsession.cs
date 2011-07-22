using System.Collections.Generic;
using System.Xml;
using System.Xml.XPath;

namespace CodeSequences
{
	internal class _2_fix_primitive_obsession
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

				var powerDetails = _wotcService.GetPowerDetails(localInfo.PowerId);
				powerDetails = _cleaner.CleanTheText(powerDetails);
				var powerInfo = _cleaner.CleanTheXml(_ParseXml(powerDetails));

				yield return new CardViewModel
				{
					Title = localInfo.Name,
					Subtitle =
						string.Format("{0} {1} {2}", _formatter.Source(powerInfo), _formatter.Kind(powerInfo), _formatter.Level(powerInfo)),
					Details = _formatter.ToBlocks(_formatter.DetailParagraphs(powerInfo)),
					Color = _formatter.ToColor(_formatter.Refresh(powerInfo)),
					UnderlyingCalculations = localInfo.Math
				};
			}
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

	public class WotcResponseCleaner
	{
		public XmlDocument CleanTheXml(XmlDocument powerInfo) {}
		public string CleanTheText(string powerDetails) {}
	}

	public class PowerFormatter
	{
		public CardColor ToColor(string refresh) {}
		public string Refresh(XmlDocument powerInfo) {}
		public IEnumerable<Block> ToBlocks(IEnumerable<string> detailParagraphs) {}
		public IEnumerable<string> DetailParagraphs(XmlDocument powerInfo) {}
		public string Level(XmlDocument powerInfo) {}
		public string Kind(XmlDocument powerInfo) {}
		public string Source(XmlDocument powerInfo) {}
	}

	public class PowerLocalInfo
	{
		public PowerLocalInfo(string name, string powerId, string math)
		{
			Name = name;
			PowerId = powerId;
			Math = math;
		}

		public string Name { get; set; }
		public string PowerId { get; set; }
		public string Math { get; set; }
	}
}
