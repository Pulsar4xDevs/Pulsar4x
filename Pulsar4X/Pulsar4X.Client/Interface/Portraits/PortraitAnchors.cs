using System;
using System.Collections.Generic;
using System.Numerics;
using Newtonsoft.Json.Linq;

namespace Pulsar4X.Client;

/// <summary>
/// Head-sidecar anchors are in 256-px template pixels, relative to the template positions.
/// </summary>
public static class PortraitAnchors
{
    public const float TemplateSize = 256f;

    /// <summary>Default anchor for a layer id when species.json omits <c>anchor</c>.</summary>
    public static string? DefaultAnchor(string layerId) => layerId switch
    {
        "eye-whites" or "eyes" or "brows" => "eyes",
        "nose" => "nose",
        "mouth" or "facial-hair" => "mouth",
        "hair" => "hairline",
        _ => null,
    };

    /// <summary>Scale a template-pixel offset to the drawn portrait size.</summary>
    public static Vector2 ScaleOffset(Vector2 templatePixels, float drawSize)
        => templatePixels * (drawSize / TemplateSize);

    /// <summary>
    /// Reads a head sidecar. Unknown keys (including optional <c>weight</c> and <c>roles</c>) are ignored.
    /// Missing or malformed anchors default to unused (treated as [0,0] at draw time).
    /// </summary>
    public static IReadOnlyDictionary<string, Vector2> ParseSidecar(string json)
    {
        var result = new Dictionary<string, Vector2>(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrWhiteSpace(json))
            return result;
        try
        {
            var root = JObject.Parse(json);
            if (root["anchors"] is not JObject anchors)
                return result;
            foreach (var prop in anchors.Properties())
            {
                if (TryReadOffset(prop.Value, out var offset))
                    result[prop.Name] = offset;
            }
        }
        catch
        {
            // Broken sidecars are skipped quietly.
        }
        return result;
    }

    static bool TryReadOffset(JToken token, out Vector2 offset)
    {
        offset = Vector2.Zero;
        if (token is not JArray arr || arr.Count < 2)
            return false;
        if (!TryNumber(arr[0], out var x) || !TryNumber(arr[1], out var y))
            return false;
        offset = new Vector2(x, y);
        return true;
    }

    static bool TryNumber(JToken token, out float value)
    {
        value = 0f;
        if (token.Type is JTokenType.Integer or JTokenType.Float)
        {
            value = token.Value<float>();
            return true;
        }
        return float.TryParse(token.ToString(), System.Globalization.NumberStyles.Float,
            System.Globalization.CultureInfo.InvariantCulture, out value);
    }
}
