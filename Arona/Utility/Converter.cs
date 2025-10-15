using System.Text.Json.Serialization;
using System.Text.Json;
using Arona.ApiModels;

namespace Arona.Utility;

// Denna klass finns för att hantera enstaka felaktiga ratingsdata
// där parametrar inom stage kan vara null eller andra data som inte är förväntad
// som INTE ska finnas
// Används för data från APIn https://clans.worldofwarships.<region>/api/clanbase/<clan id>/claninfo/
internal class Converter : JsonConverter<ClanBase.Stage>
{
    public static JsonSerializerOptions Options { get; } = new(){ Converters = { new Converter() } };
    
    public override ClanBase.Stage? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
            return null;

        using var doc = JsonDocument.ParseValue(ref reader);
        var root = doc.RootElement;

        // Ex: om type == "brawl" och målfält är null, behandla som olagligt
        if (root.TryGetProperty("type", out var typeProp) &&
            typeProp.ValueKind == JsonValueKind.String &&
            typeProp.GetString() == "brawl")
        {
            return null; // Ignorera skräpdata
        }

        // Deserialisera normalt
        return JsonSerializer.Deserialize<ClanBase.Stage>(root.GetRawText())!;
    }

    public override void Write(Utf8JsonWriter writer, ClanBase.Stage value, JsonSerializerOptions options) =>
        JsonSerializer.Serialize(writer, value, options);
}