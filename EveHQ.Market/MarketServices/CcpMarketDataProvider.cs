#region Usings

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using EveHQ.Caching;
using EveHQ.Common;
using EveHQ.Common.Extensions;
using EveHQ.NewEveAPI;
using MathNet.Numerics.Statistics;
using Newtonsoft.Json;

#endregion


namespace EveHQ.Market.MarketServices
{
	public sealed class CcpMarketDataProvider : IMarketOrderDataProvider, IMarketStatDataProvider
	{
		public CcpMarketDataProvider(
			string cacheRootFolder,
			IHttpRequestProvider requestProvider,
			SupportedMarket supportedMarket,
			Locations locations)
		{
			_regionOrdersCache = new TextFileCacheProvider(Path.Combine(cacheRootFolder, "MarketOrders"));
			_requestProvider = requestProvider;
			_supportedMarket = supportedMarket;
			_locations = locations;
		}

		public bool LimitedRegionSelection => true;

		public bool LimitedSystemSelection => true;

		public static string Name => "CCP ESI Market data provider";

		public string ProviderName => Name;

		public IEnumerable<int> SupportedRegions => _supportedMarket.Regions;

		public IEnumerable<int> SupportedSystems => _supportedMarket.SystemToStationMap.Keys;

		public Task<ItemMarketOrders> GetMarketOrdersForItemType(
			int itemTypeId,
			IEnumerable<int> includedRegions,
			int? systemId,
			int minQuantity)
		{
			var regionIds = CalculateRegionIds(includedRegions, systemId);

			return Task<ItemMarketOrders>.Factory.TryRun(
				() =>
				{
					var result = new ItemMarketOrders();
					foreach (var regionId in regionIds)
					{
						var orders = RetrieveOrders(itemTypeId, regionId);
						result.ItemTypeId = itemTypeId;
						result.Regions = new HashSet<int>(new[] { regionId });
						result.BuyOrders = orders.Where(order => order.IsBuyOrder).ToList();
						result.SellOrders = orders.Where(order => !order.IsBuyOrder).ToList();
					}

					return result;
				});
		}

		public Task<IEnumerable<ItemOrderStats>> GetOrderStats(
			IEnumerable<int> typeIds,
			IEnumerable<int> includeRegions,
			int? systemId,
			int minQuantity)
		{
			return Task<IEnumerable<ItemOrderStats>>.Factory.TryRun(
				() => typeIds.Select(typeId => CalculateOrderStats(typeId, includeRegions, systemId)));
		}

		private ItemOrderStats CalculateOrderStats(int typeId, IEnumerable<int> includeRegions, int? systemId)
		{
			var regionIds = CalculateRegionIds(includeRegions, systemId);
			return new ItemOrderStats
					{
						ItemTypeId = typeId,
						Buy = CalculateBuyOrderStats(typeId, regionIds),
						Sell = CalculateSellOrderStats(typeId, regionIds),
						All = CalculateAllOrderStats(typeId, regionIds)
					};
		}

		private OrderStats CalculateBuyOrderStats(int typeId, IEnumerable<int> includeRegions) =>
			CalculateOrderStats(GetMarketOrdersForItemType(typeId, includeRegions, null, 0).Result.BuyOrders);

		private OrderStats CalculateSellOrderStats(int typeId, IEnumerable<int> includeRegions) =>
			CalculateOrderStats(GetMarketOrdersForItemType(typeId, includeRegions, null, 0).Result.SellOrders);

		private OrderStats CalculateAllOrderStats(int typeId, IEnumerable<int> includeRegions)
		{
			var orders = GetMarketOrdersForItemType(typeId, includeRegions, null, 0).Result;
			return CalculateOrderStats(orders.BuyOrders.Concat(orders.SellOrders));
		}

		private OrderStats CalculateOrderStats(IEnumerable<MarketOrder> orders)
		{
			var marketOrders = orders.ToArray();
			var prices = marketOrders.Select(order => order.Price).ToArray();
			return new OrderStats
					{
						Minimum = prices.Minimum(),
						Maximum = prices.Maximum(),
						Average = prices.Mean(),
						Median = prices.Median(),
						StdDeviation = prices.StandardDeviation(),
						Percentile = prices.Percentile(5),
						Volume = marketOrders.Sum(order => (long)order.QuantityRemaining)
					};
		}

		private List<MarketOrder> RetrieveOrders(int typeId, int regionId)
		{
			var result = new List<MarketOrder>();
			if (TryRetrieveOrdersFromCache(typeId, regionId, result))
			{
				return result;
			}

			RetrieveOrdersFromWebService(typeId, regionId, result);
			if (result.Any())
			{
				AddOrdersIntoCache(typeId, regionId, result);
			}

			return result;
		}

		private bool TryRetrieveOrdersFromCache(int typeId, int regionId, List<MarketOrder> result)
		{
			var cachedItems = _regionOrdersCache.Get<List<MarketOrder>>(CreateCacheKey(typeId, regionId));
			if (cachedItems == null ||
				!cachedItems.Data.Any() ||
				cachedItems.IsDirty)
			{
				return false;
			}

			result.AddRange(cachedItems.Data);
			return true;
		}

		private void AddOrdersIntoCache(int typeId, int regionId, List<MarketOrder> marketOrders) =>
			_regionOrdersCache.Add(CreateCacheKey(typeId, regionId), marketOrders, DateTimeOffset.Now.Add(_cacheTtl));

		private void RetrieveOrdersFromWebService(int typeId, int regionId, ICollection<MarketOrder> result)
		{
			if (!_isServiceAvailable)
			{
				return;
			}

			var requestUri = new Uri(
				$"https://esi.tech.ccp.is/latest/markets/{regionId}/orders/?datasource=tranquility&order_type=all&page=1&type_id={typeId}");
			var requestTask = _requestProvider.GetAsync(requestUri);
			requestTask.Wait(); // wait for the completion (we're in a background task anyways)

			if (!requestTask.Result.IsSuccessStatusCode ||
				WebServiceExceptionHelper.IsServiceUnavailableError(requestTask.Exception))
			{
				_isServiceAvailable = false; // TODO: postpone next check.
				return;
			}

			if (!requestTask.IsTaskSuccessfullyCompleted())
			{
				return;
			}

			var contentStreamTask = requestTask.Result.Content.ReadAsStreamAsync();
			contentStreamTask.Wait();

			using (var reader = new StreamReader(contentStreamTask.Result))
			{
				try
				{
					// process result
					var retrievedItems = ParseResult(reader.ReadToEnd());

					// cache it.
					foreach (var marketOrder in retrievedItems.Select(item => ConvertOrder(item, regionId)))
					{
						result.Add(marketOrder);
					}
				}
				catch (Exception exception)
				{
					Trace.TraceError(exception.FormatException());
					throw;
				}
			}
		}

		private MarketOrder ConvertOrder(CcpMarketDataItem marketDataItem, int regionId) =>
			new MarketOrder
			{
				IsBuyOrder = marketDataItem.IsBuyOrder,
				Duration = marketDataItem.DurationInDays,
				MinQuantity = marketDataItem.MinVolume,
				RegionId = regionId,
				ItemId = marketDataItem.TypeId,
				Expires = marketDataItem.Issued.AddDays(marketDataItem.DurationInDays),
				OrderId = marketDataItem.OrderId,
				Price = marketDataItem.Price,
				QuantityRemaining = marketDataItem.VolumeRemain,
				QuantityEntered = marketDataItem.VolumeTotal,
				LocationId = marketDataItem.LocationId,
				OrderRange = marketDataItem.Range,
				Issued = marketDataItem.Issued,
				StationName = _locations.GetLocationFromID(marketDataItem.LocationId).ContainerName
			};

		private static string CreateCacheKey(int typeId, int regionId) => $"{typeId} - {regionId}";

		private int[] CalculateRegionIds(IEnumerable<int> includedRegions, int? systemId)
		{
			var regionIds = systemId.HasValue && _supportedMarket.HubToRegionMap.ContainsKey(systemId.Value)
				? new[] { _supportedMarket.HubToRegionMap[systemId.Value] }
				: (includedRegions?.ToArray() ?? new[] { SupportedMarket.TheForgeRegionId });
			return regionIds;
		}

		private IEnumerable<CcpMarketDataItem> ParseResult(string content)
		{
			try
			{
				return JsonConvert.DeserializeObject<IList<CcpMarketDataItem>>(content);
			}
			catch (Exception exception)
			{
				throw new Exception(
					$"Can not parse a response from the CCP list orders in a region API. The response body:{Environment.NewLine}{content}",
					exception);
			}
		}

		private bool _isServiceAvailable = true;

		private readonly IHttpRequestProvider _requestProvider;
		private readonly TextFileCacheProvider _regionOrdersCache;
		private readonly SupportedMarket _supportedMarket;
		private readonly Locations _locations;
		private readonly TimeSpan _cacheTtl = TimeSpan.FromMinutes(10);
	}
}