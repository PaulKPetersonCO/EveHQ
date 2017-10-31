using Newtonsoft.Json;


namespace EveHQ.Market.MarketServices
{
	public class FuzzworkItemOrderStats
	{
		[JsonProperty("buy")]
		public FuzzworkOrderStats Buy { get; set; }

		[JsonProperty("sell")]
		public FuzzworkOrderStats Sell { get; set; }
	}
}