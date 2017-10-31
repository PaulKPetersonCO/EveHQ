// To parse this JSON data, add NuGet 'Newtonsoft.Json' then do:
//
//    using QuickType;
//
//    var data = Fuzz.FromJson(jsonString);
//

#region Usings

using System.Collections.Generic;
using Newtonsoft.Json;

#endregion


namespace EveHQ.Market.MarketServices
{
	public class FuzzworkResponse : Dictionary<int, FuzzworkItemOrderStats>
	{
		public static FuzzworkResponse FromJson(string json) =>
			JsonConvert.DeserializeObject<FuzzworkResponse>(
				json,
				new JsonSerializerSettings
				{
					MetadataPropertyHandling = MetadataPropertyHandling.Ignore,
					DateParseHandling = DateParseHandling.None
				});
	}
}