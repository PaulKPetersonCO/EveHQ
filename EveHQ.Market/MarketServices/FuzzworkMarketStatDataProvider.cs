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
	public sealed class FuzzworkMarketStatDataProvider : IMarketStatDataProvider
	{
		public FuzzworkMarketStatDataProvider(string cacheRootFolder, IHttpRequestProvider requestProvider, SupportedMarket supportedMarket)
		{
			_regionDataCache = new TextFileCacheProvider(Path.Combine(cacheRootFolder, "Region"));
			_requestProvider = requestProvider;
			_supportedMarket = supportedMarket;
		}

		public bool LimitedRegionSelection => true;

		public bool LimitedSystemSelection => true;

		public static string Name => "Fuzzworks market data WEB API";

		public string ProviderName => Name;

		public IEnumerable<int> SupportedRegions => _supportedMarket.Regions;

		public IEnumerable<int> SupportedSystems => _supportedMarket.SystemToStationMap.Keys;

		public Task<IEnumerable<ItemOrderStats>> GetOrderStats(
			IEnumerable<int> typeIds,
			IEnumerable<int> includeRegions,
			int? systemId,
			int minQuantity)
		{
			var typeIdsArray = typeIds.Distinct().ToArray();
			var regions = includeRegions?.ToArray() ?? new[] { SupportedMarket.JitaSystemId };
			var regionsArray = systemId.HasValue && _supportedMarket.SystemToStationMap.ContainsKey(systemId.Value)
				? _supportedMarket.SystemToStationMap[systemId.Value]
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
		private readonly SupportedMarket _supportedMarket;
		private readonly TextFileCacheProvider _regionDataCache;
		private readonly TimeSpan _cacheTtl = TimeSpan.FromMinutes(10);

		private const string ItemKeyFormat = "{0}-{1}";
	}
}