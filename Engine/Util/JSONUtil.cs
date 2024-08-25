using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ElementEngine.Util;

public static class JSONUtil
{
    public static T LoadJSON<T>(
            string path,
            JsonSerializerOptions serializerOptions = null,
            List<JsonConverter> converters = null)
    {
        var json = File.ReadAllText(path);

        if (serializerOptions == null)
            serializerOptions = new();

        if (converters != null)
        {
            foreach (var converter in converters)
                serializerOptions.Converters.Add(converter);
        }

        var obj = JsonSerializer.Deserialize<T>(json, serializerOptions);

        return obj;
    }

    public static T LoadJSON<T>(
            FileStream fs,
            JsonSerializerOptions serializerOptions = null,
            List<JsonConverter> converters = null)
    {
        using var reader = new StreamReader(fs);
        var json = reader.ReadToEnd();

        if (serializerOptions == null)
            serializerOptions = new();

        if (converters != null)
        {
            foreach (var converter in converters)
                serializerOptions.Converters.Add(converter);
        }

        var obj = JsonSerializer.Deserialize<T>(json, serializerOptions);

        return obj;
    }
}
