using Newtonsoft.Json;


namespace EveHQ.Market.MarketServices
{
	public class FuzzworkOrderStats
	{
		[JsonProperty("orderCount")]
		public double OrderCount { get; set; }

		[JsonProperty("median")]
		public double Median { get; set; }

		[JsonProperty("max")]
		public double Max { get; set; }

		[JsonProperty("min")]
		public double Min { get; set; }

		[JsonProperty("stddev")]
		public double Stddev { get; set; }

		[JsonProperty("percentile")]
		public double Percentile { get; set; }

		[JsonProperty("volume")]
		public double Volume { get; set; }

		[JsonProperty("weightedAverage")]
		public double WeightedAverage { get; set; }
	}
}