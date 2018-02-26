// Проект: EveHQ.NewEveAPI
// Имя файла: AssetLocation.cs
// GUID файла: B492A8DF-B368-42D3-BC3D-96FC2519C8FA
// Автор: Mike Eshva (mike@eshva.ru)
// Дата создания: 17.12.2017

#region Usings

using EveHQ.EveData;

#endregion


namespace EveHQ.NewEveAPI
{
	public sealed class AssetLocation
	{
		public SolarSystem SolarSystem { get; set; }

		public long ContainerId { get; set; }

		public string ContainerName { get; set; }
	}
}