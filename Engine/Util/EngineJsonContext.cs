using ElementEngine.EndlessTiles;
using ElementEngine.Ogmo;
using ElementEngine.TexturePacker;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace ElementEngine.Util;

[JsonSourceGenerationOptions(IncludeFields = true)]
[JsonSerializable(typeof(Dictionary<string, string>))]
[JsonSerializable(typeof(EndlessTilesWorldData))]
[JsonSerializable(typeof(OgmoLevelData))]
[JsonSerializable(typeof(TexturePackerAtlasData))]
internal partial class EngineJsonContext : JsonSerializerContext
{
}
