#region Usings

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

#endregion


namespace EveHQ.Market.MarketServices
{
	public class StabMarketOrderDataProvider : IMarketOrderDataProvider
	{
		public Task<ItemMarketOrders> GetMarketOrdersForItemType(int itemTypeId, IEnumerable<int> includedRegions, int? systemId, int minQuantity)
			=> Task<ItemMarketOrders>.Factory.StartNew(
				() => new ItemMarketOrders
					{
						BuyOrders = new List<MarketOrder>(),
						Hours = 0,
						ItemName = "Not an item",
						ItemTypeId = itemTypeId,
						MinQuantity = minQuantity,
						Regions = new HashSet<int>(),
						SellOrders = new List<MarketOrder>(),
						Timestamp = DateTimeOffset.MaxValue
					});
	}
}