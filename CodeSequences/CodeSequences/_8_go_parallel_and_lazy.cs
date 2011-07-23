using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.XPath;

namespace CodeSequences
{
	internal class _8_go_parallel_and_lazy
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
			var powerDetails = GetOnlineInfoForPower(localInfo);
			var powerInfo = powerDetails.ContinueWith(t => CleanTheResponse(t.Result));
			powerInfo.ContinueWith(t => UpdateViewModel(localInfo, t.Result, card));
			return card;
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

	public class AsyncWotcClient
	{
		public Task<string> GetPowerDetails(string powerId) {}
	}
}
