// Проект: EveHQ.Market
// Имя файла: CcpMarketDataItem.cs
// GUID файла: 794BC73D-5E3A-4F31-93FA-BF1D2238E484
// Автор: Mike Eshva (mike@eshva.ru)
// Дата создания: 14.12.2017

#region Usings

using System;
using Newtonsoft.Json;

#endregion


namespace EveHQ.Market.MarketServices
{
	public sealed class CcpMarketDataItem
	{
		[JsonProperty("order_id")]
		public long OrderId { get; set; }

		[JsonProperty("type_id")]
		public int TypeId { get; set; }

		[JsonProperty("location_id")]
		public long LocationId { get; set; }

		[JsonProperty("volume_total")]
		public int VolumeTotal { get; set; }

		[JsonProperty("volume_remain")]
		public int VolumeRemain { get; set; }

		[JsonProperty("min_volume")]
		public int MinVolume { get; set; }

		[JsonProperty("price")]
		public double Price { get; set; }

		[JsonProperty("is_buy_order")]
		public bool IsBuyOrder { get; set; }

		[JsonProperty("duration")]
		public int DurationInDays { get; set; }

		[JsonProperty("issued")]
		public DateTimeOffset Issued { get; set; }

		[JsonProperty("range")]
		public string Range { get; set; }
	}
}