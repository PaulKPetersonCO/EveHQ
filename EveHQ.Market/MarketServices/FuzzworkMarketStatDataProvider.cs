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

#endregion


namespace EveHQ.Market.MarketServices
{
	public class FuzzworkMarketStatDataProvider : IMarketStatDataProvider
	{
		public FuzzworkMarketStatDataProvider(string cacheRootFolder, IHttpRequestProvider requestProvider)
		{
			_regionDataCache = new TextFileCacheProvider(Path.Combine(cacheRootFolder, "Region"));
			_requestProvider = requestProvider;
		}

		public bool LimitedRegionSelection => true;

		public bool LimitedSystemSelection => true;

		public static string Name => "Fuzzworks market data API";

		public string ProviderName => Name;

		public IEnumerable<int> SupportedRegions => _supportedRegions;

		public IEnumerable<int> SupportedSystems => _supportedSystemToStationMap.Keys;

		public Task<IEnumerable<ItemOrderStats>> GetOrderStats(
			IEnumerable<int> typeIds,
			IEnumerable<int> includeRegions,
			int? systemId,
			int minQuantity)
		{
			var typeIdsArray = typeIds.Distinct().ToArray();
			var regions = includeRegions?.ToArray() ?? new[] { JitaSystemId };
			var regionsArray = systemId.HasValue && _supportedSystemToStationMap.ContainsKey(systemId.Value)
				? _supportedSystemToStationMap[systemId.Value]
				: regions.First();
			return GetOrderStats(typeIdsArray, regionsArray);
		}

		private Task<IEnumerable<ItemOrderStats>> GetOrderStats(int[] typeIds, int regionId)
		{
			return Task<IEnumerable<ItemOrderStats>>.Factory.TryRun(
				() =>
				{
					var cacheKey = regionId.ToInvariantString();
					// Get all cached items for the selected region.
					var cachedItemStats = typeIds
						.Select(typeId => _regionDataCache.Get<ItemOrderStats>(ItemKeyFormat.FormatInvariant(typeId, cacheKey))).ToList();
					// Add to the result set not expired items for quered types from cache.
					var result = cachedItemStats.Where(item => item?.Data != null && !item.IsDirty).Select(item => item.Data).ToList();
					// Get only type IDs that not in the cache.
					var typesToRequest = typeIds.Except(result.Select(stats => stats.ItemTypeId)).ToArray();

					if (!typesToRequest.Any())
					{
						return result;
					}

					result.AddRange(RetrieveItems(typesToRequest, regionId, cacheKey));

					// Dirty cached items is better than nothing in the end. Add them to dirty cache in case of no data/error.
					var fromDirtyCache = cachedItemStats
						.Where(
							item => item?.Data != null && item.IsDirty &&
									result.Any(resultItem => item.Data.ItemTypeId == resultItem.ItemTypeId))
						.Select(item => item.Data);
					result.AddRange(fromDirtyCache);

					return result;
				});
		}

		private IEnumerable<ItemOrderStats> RetrieveItems(IEnumerable<int> typesToRequest, int regionId, string cacheKey)
		{
			var resultItems = new List<ItemOrderStats>();

			if (!_isServiceAvailable)
			{
				return resultItems;
			}

			var requestUri = new Uri($"https://market.fuzzwork.co.uk/aggregates/?region={regionId}&types={string.Join(",", typesToRequest)}");
			var requestTask = _requestProvider.GetAsync(requestUri);
			requestTask.Wait(); // wait for the completion (we're in a background task anyways)

			if (!requestTask.Result.IsSuccessStatusCode ||
				WebServiceExceptionHelper.IsServiceUnavailableError(requestTask.Exception))
			{
				_isServiceAvailable = false; // TODO: postpone next check.
				return resultItems;
			}

			if (!requestTask.IsTaskSuccessfullyCompleted())
			{
				return resultItems;
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
					foreach (var item in retrievedItems)
					{
						_regionDataCache.Add(
							ItemKeyFormat.FormatInvariant(item.ItemTypeId, cacheKey),
							item,
							DateTimeOffset.Now.Add(_cacheTtl));
						resultItems.Add(item);
					}
				}
				catch (Exception ex)
				{
					Trace.TraceError(ex.FormatException());
					throw;
				}
			}

			return resultItems;
		}

		private IEnumerable<ItemOrderStats> ParseResult(string content)
		{
			try
			{
				var result = new List<ItemOrderStats>();
				foreach (var itemOrderStats in FuzzworkResponse.FromJson(content))
				{
					var itemTypeId = itemOrderStats.Key;
					var buy = itemOrderStats.Value.Buy;
					var sell = itemOrderStats.Value.Sell;
					result.Add(
						new ItemOrderStats
						{
							ItemTypeId = itemTypeId,
							Buy = new OrderStats
								{
									Average = buy.WeightedAverage,
									Maximum = buy.Max,
									Median = buy.Median,
									Minimum = buy.Min,
									Percentile = buy.Percentile,
									StdDeviation = buy.Stddev,
									Volume = buy.Volume.ToInt32()
								},
							Sell = new OrderStats
									{
										Average = sell.WeightedAverage,
										Maximum = sell.Max,
										Median = sell.Median,
										Minimum = sell.Min,
										Percentile = sell.Percentile,
										StdDeviation = sell.Stddev,
										Volume = sell.Volume.ToInt32()
									}
						});
				}

				return result;
			}
			catch (Exception exception)
			{
				throw new Exception(
					$"Can not parse a response from the Structure Name API. The response body:{Environment.NewLine}{content}",
					exception);
			}
		}

		private bool _isServiceAvailable = true;
		private readonly IHttpRequestProvider _requestProvider;
		private readonly TextFileCacheProvider _regionDataCache;
		private readonly Dictionary<int, int> _supportedSystemToStationMap =
			new Dictionary<int, int>
			{
				{ 30000142, 60003760 }, // Jita IV - Moon 4 - Caldari Navy Assembly Plant
				{ 30002187, 60008494 }, // Amarr VIII (Oris) - Emperor Family Academy
				{ 30002659, 60011866 }, // Dodixie IX - Moon 20 - Federation Navy Assembly Plant
				{ 30002510, 60004588 }, // Rens VI - Moon 8 - Brutor Tribe Treasury
				{ 30002053, 60005686 } // Hek VIII - Moon 12 - Boundless Creation Factory
			};

		private readonly int[] _supportedRegions =
		{
			10000001,
			10000002,
			10000003,
			10000004,
			10000005,
			10000006,
			10000007,
			10000008,
			10000009,
			10000010,
			10000011,
			10000012,
			10000013,
			10000014,
			10000015,
			10000016,
			10000017,
			10000018,
			10000019,
			10000020,
			10000021,
			10000022,
			10000023,
			10000025,
			10000027,
			10000028,
			10000029,
			10000030,
			10000031,
			10000032,
			10000033,
			10000034,
			10000035,
			10000036,
			10000037,
			10000038,
			10000039,
			10000040,
			10000041,
			10000042,
			10000043,
			10000044,
			10000045,
			10000046,
			10000047,
			10000048,
			10000049,
			10000050,
			10000051,
			10000052,
			10000053,
			10000054,
			10000055,
			10000056,
			10000057,
			10000058,
			10000059,
			10000060,
			10000061,
			10000062,
			10000063,
			10000064,
			10000065,
			10000066,
			10000067,
			10000068,
			10000069
		};
		private readonly TimeSpan _cacheTtl = TimeSpan.FromMinutes(10);

		private const int JitaSystemId = 30000142;
		private const string ItemKeyFormat = "{0}-{1}";
	}
}