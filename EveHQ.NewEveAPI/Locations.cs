#region Usings

using System;
using EveHQ.Common;
using EveHQ.EveData;
using EveHQ.NewEveApi;

#endregion


namespace EveHQ.NewEveAPI
{
	public class Locations
	{
		public Locations(StructureNameClient structureNameClient, Action<string> logAction)
		{
			_structureNameClient = structureNameClient;
			_logAction = logAction;
		}

		public AssetLocation GetLocationFromID(long locationId)
		{
			if (locationId >= 66014933)
			{
				return GetStructureLocation(locationId);
			}

			if (locationId >= 66000000)
			{
				if (locationId < 66014933)
				{
					locationId = locationId - 6000001;
				}
				else
				{
					locationId = locationId - 6000000;
				}
			}

			if ((double)locationId >= 61000000 & (double)locationId <= 61999999)
			{
				return GetOutpostLocation(locationId);
			}
			if (locationId < 60000000)
			{
				return GetSolarSystemLocation(locationId);
			}
			return GetStationLocation(locationId);
		}

		private AssetLocation GetStationLocation(long locationId)
		{
			var result = new AssetLocation();
			if (StaticData.Stations.ContainsKey(locationId))
			{
				var newLocation = StaticData.Stations[locationId];
				result.ContainerName = newLocation.StationName;
				result.ContainerId = newLocation.StationId;
				result.SolarSystem = StaticData.SolarSystems[newLocation.SystemId];
			}
			else
			{
				result.ContainerName = $"Unknown station {locationId}";
			}

			return result;
		}

		private AssetLocation GetSolarSystemLocation(long locationId)
		{
			var result = new AssetLocation();
			if (StaticData.SolarSystems.ContainsKey((int)locationId))
			{
				var newSystem = StaticData.SolarSystems[(int)locationId];
				result.ContainerName = newSystem.Name;
				result.ContainerId = newSystem.Id;
				result.SolarSystem = newSystem;
			}
			else
			{
				result.ContainerName = $"Unknown System {locationId}";
				result.ContainerId = locationId;
				result.SolarSystem = null;
			}

			return result;
		}

		private AssetLocation GetOutpostLocation(long locationId)
		{
			var result = new AssetLocation();
			if (StaticData.Stations.ContainsKey(locationId))
			{
				var newLocation = StaticData.Stations[locationId];
				result.ContainerName = newLocation.StationName;
				result.ContainerId = newLocation.StationId;
				result.SolarSystem = StaticData.SolarSystems[newLocation.SystemId];
			}
			else
			{
				result.ContainerName = $"Unknown Outpost {locationId}";
				result.ContainerId = locationId;
				result.SolarSystem = null;
			}

			return result;
		}

		private AssetLocation GetStructureLocation(long locationId)
		{
			var result = new AssetLocation();
			var structureNameTask = _structureNameClient.GetStructureNameAsync(locationId);
			var isStructureNameGotten = false;
			if (_isStructureNameServiceAbailable)
			{
				try
				{
					structureNameTask.Wait();
					isStructureNameGotten = structureNameTask.Result.IsSuccess;
				}
				catch (Exception exception)
				{
					_isStructureNameServiceAbailable = !WebServiceExceptionHelper.IsServiceUnavailableError(exception);
					_logAction($"During call to structure name service an exception occured:\n {exception}");
				}
			}

			if (isStructureNameGotten)
			{
				var eveStructure = structureNameTask.Result.ResultData;
				result.ContainerName = $"{eveStructure.StructureName} ({eveStructure.StructureTypeName})";
				result.ContainerId = locationId;
				result.SolarSystem = StaticData.SolarSystems[eveStructure.SystemId];
			}
			else
			{
				result.ContainerName = $"Unknown Location {locationId} (Wreck?)";
				result.ContainerId = locationId;
				result.SolarSystem = null;
			}

			return result;
		}

		private bool _isStructureNameServiceAbailable = true;

		private readonly StructureNameClient _structureNameClient;
		private readonly Action<string> _logAction;
	}
}