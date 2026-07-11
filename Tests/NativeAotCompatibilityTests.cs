using ElementEngine;
using ElementEngine.ECS;
using ElementEngine.ElementUI;
using ElementEngine.Util;
using System.ComponentModel;
using System.Globalization;
using System.Text.Json.Serialization;
using Xunit;

namespace ElementEngine.Tests;

public sealed class NativeAotCompatibilityTests
{
    [Fact]
    public void JsonMetadataOverloadDeserializesApplicationType()
    {
        var path = WriteTemporaryFile("""{"Name":"example","Count":7}""");

        try
        {
            var payload = JSONUtil.LoadJSON(path, TestJsonContext.Default.TestJsonPayload);

            Assert.Equal("example", payload.Name);
            Assert.Equal(7, payload.Count);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void EngineJsonContextIncludesTexturePackerFields()
    {
        const string json = """
            {
              "frames": [
                {
                  "filename": "player",
                  "frame": { "x": 1, "y": 2, "w": 3, "h": 4 },
                  "rotated": false,
                  "trimmed": true,
                  "spriteSourceSize": { "x": 5, "y": 6, "w": 7, "h": 8 },
                  "sourceSize": { "w": 9, "h": 10 }
                }
              ],
              "meta": {
                "app": "TexturePacker",
                "size": { "w": 64, "h": 32 }
              }
            }
            """;
        var path = WriteTemporaryFile(json);

        try
        {
            var atlas = JSONUtil.LoadJSON(
                path,
                EngineJsonContext.Default.TexturePackerAtlasData);

            var sprite = Assert.Single(atlas.frames);
            Assert.Equal("player", sprite.filename);
            Assert.Equal(new Rectangle(1, 2, 3, 4), sprite.frame.Rect);
            Assert.True(sprite.trimmed);
            Assert.Equal(64, atlas.meta.size.w);
            Assert.Equal(32, atlas.meta.size.h);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void ComponentDebugViewExposesComponentWithoutRuntimeGenericConstruction()
    {
        var registry = new Registry();
        var entity = registry.CreateEntity();
        var emptyEntity = registry.CreateEntity();
        entity.TryAddComponent(new DebugComponent { Name = "player", Health = 42 });

        var debugView = Assert.IsAssignableFrom<IComponentDebugView>(
            registry.ComponentStores[typeof(DebugComponent).GetHashCode()]);
        var component = Assert.IsType<DebugComponent>(debugView.GetBoxed(entity.ID));

        Assert.Equal("player", component.Name);
        Assert.Equal(42, component.Health);
        Assert.Null(debugView.GetBoxed(emptyEntity.ID));
        Assert.Equal(
            [nameof(DebugComponent.Health), nameof(DebugComponent.Name)],
            debugView.PublicFields.Select(field => field.Name).OrderBy(name => name));
    }

    [Fact]
    public void NullableCustomTypeConverterBehaviorIsPreserved()
    {
        CustomConvertedValue? value = "value:19".ConvertTo<CustomConvertedValue?>();

        Assert.True(value.HasValue);
        Assert.Equal(19, value.Value.Number);
    }

    [Fact]
    public void EnumDropdownItemsRetainNamesAndValues()
    {
        var items = UIDropdownHelper.ItemsFromEnum<TestDropdownValue>();

        Assert.Collection(
            items,
            item =>
            {
                Assert.Equal(nameof(TestDropdownValue.First), item.Text);
                Assert.Equal(TestDropdownValue.First, item.Value);
            },
            item =>
            {
                Assert.Equal(nameof(TestDropdownValue.Second), item.Text);
                Assert.Equal(TestDropdownValue.Second, item.Value);
            });
    }

    private static string WriteTemporaryFile(string contents)
    {
        var path = Path.GetTempFileName();
        File.WriteAllText(path, contents);
        return path;
    }

    private struct DebugComponent
    {
        public string Name;
        public int Health;
    }

    private enum TestDropdownValue
    {
        First,
        Second,
    }
}

internal sealed class TestJsonPayload
{
    public string Name { get; set; } = string.Empty;
    public int Count { get; set; }
}

[JsonSerializable(typeof(TestJsonPayload))]
internal partial class TestJsonContext : JsonSerializerContext
{
}

[TypeConverter(typeof(CustomConvertedValueConverter))]
internal readonly record struct CustomConvertedValue(int Number);

internal sealed class CustomConvertedValueConverter : TypeConverter
{
    public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType)
    {
        return sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);
    }

    public override object? ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value)
    {
        if (value is string text && text.StartsWith("value:", StringComparison.Ordinal))
            return new CustomConvertedValue(int.Parse(text[6..], CultureInfo.InvariantCulture));

        return base.ConvertFrom(context, culture, value);
    }
}
