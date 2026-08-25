using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace Coworking.External.Squidex.Abstractions.Serialization;

/// <summary>
/// Marks a component hierarchy and names the field that tells one component from another.
/// <para>
/// System.Text.Json has polymorphism of its own, but it only reads a discriminator that is
/// the first property, and Squidex puts schemaId ahead of it — so this reads the field
/// wherever it sits.
/// </para>
/// <code>
/// [SquidexComponent("componentType")]
/// [SquidexComponentType("hero", typeof(HeroBlock))]
/// [SquidexComponentType("cta", typeof(CtaBlock))]
/// public abstract class PageBlock;
/// </code>
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class SquidexComponentAttribute(string discriminator) : JsonConverterAttribute
{
    public string Discriminator { get; } = discriminator;

    public override JsonConverter CreateConverter(Type typeToConvert) =>
        (JsonConverter)Activator.CreateInstance(
            typeof(SquidexComponentConverter<>).MakeGenericType(typeToConvert), Discriminator)!;
}

/// <summary>Binds one discriminator value to the type that carries that component's fields.</summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
public sealed class SquidexComponentTypeAttribute(string name, Type type) : Attribute
{
    public string Name { get; } = name;
    public Type Type { get; } = type;
}

internal sealed class SquidexComponentConverter<TBase>(string discriminator) : JsonConverter<TBase>
{
    private static readonly Dictionary<string, Type> TypeByName =
        typeof(TBase).GetCustomAttributes<SquidexComponentTypeAttribute>(inherit: false)
            .ToDictionary(attribute => attribute.Name, attribute => attribute.Type);

    private static readonly Dictionary<Type, string> NameByType =
        TypeByName.ToDictionary(pair => pair.Value, pair => pair.Key);

    public override TBase? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using var document = JsonDocument.ParseValue(ref reader);
        var element = document.RootElement;

        if (!element.TryGetProperty(discriminator, out var marker) ||
            marker.ValueKind is not JsonValueKind.String)
            throw new JsonException(
                $"Squidex component has no '{discriminator}' field to tell its type.");

        var name = marker.GetString()!;

        if (!TypeByName.TryGetValue(name, out var derived))
            throw new JsonException(
                $"Squidex component '{name}' matches no type derived from {typeof(TBase).Name}.");

        return (TBase?)element.Deserialize(derived, options);
    }

    public override void Write(Utf8JsonWriter writer, TBase value, JsonSerializerOptions options)
    {
        var node = JsonSerializer.SerializeToNode(value, value!.GetType(), options)!.AsObject();

        // the discriminator is a real Squidex field, so it has to go back on write
        if (NameByType.TryGetValue(value.GetType(), out var name))
            node[discriminator] = name;

        node.WriteTo(writer);
    }
}
