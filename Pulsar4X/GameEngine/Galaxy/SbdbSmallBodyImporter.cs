using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Text;
using Newtonsoft.Json.Linq;
using Pulsar4X.Blueprints;

namespace Pulsar4X.Galaxy
{
    /// <summary>
    /// Pulls the largest measured small bodies from the JPL Small-Body Database
    /// and turns them into <see cref="SystemBodyBlueprint"/> values. Does not
    /// read or write mod files. A caller that wants them in a running game can
    /// load the blueprints. The SolBodies console writes them into basemod.
    /// </summary>
    public static class SbdbSmallBodyImporter
    {
        public const string QueryUrl = "https://ssd-api.jpl.nasa.gov/sbdb_query.api";
        public const double AuInKm = 149597870.7;
        // G in km³ / kg / s², so mass_kg = GM / G when GM is km³/s².
        public const double GravitationalConstantKm = 6.67430e-20;

        public static readonly IReadOnlyList<string> KnownFields = new[]
        {
            "main-belt",
            "mars-crossers",
            "near-earth",
            "trojans",
            "centaurs",
            "trans-neptunian",
            "other",
        };

        static readonly Dictionary<string, string> ClassToField = new Dictionary<string, string>
        {
            ["IMB"] = "main-belt",
            ["MBA"] = "main-belt",
            ["OMB"] = "main-belt",
            ["MCA"] = "mars-crossers",
            ["IEO"] = "near-earth",
            ["ATE"] = "near-earth",
            ["APO"] = "near-earth",
            ["AMO"] = "near-earth",
            ["TJN"] = "trojans",
            ["CEN"] = "centaurs",
            ["TNO"] = "trans-neptunian",
        };

        public static string FileNameForField(string field) => "asteroids-" + field + ".json";

        public static SbdbImportResult Import(SbdbImportRequest request, HttpClient? http, string? responseJson = null)
        {
            if (request.Count < 1)
                throw new ArgumentOutOfRangeException(nameof(request), "Count must be at least 1.");

            var fields = NormalizeFields(request.Fields);
            responseJson ??= Fetch(http ?? throw new ArgumentNullException(nameof(http)), request, fields);
            return Parse(responseJson, request, fields);
        }

        public static string BuildQueryUrl(SbdbImportRequest request)
        {
            return BuildQueryUrl(request, NormalizeFields(request.Fields));
        }

        static string Fetch(HttpClient http, SbdbImportRequest request, IReadOnlyCollection<string>? fields)
        {
            return http.GetStringAsync(BuildQueryUrl(request, fields)).GetAwaiter().GetResult();
        }

        static string BuildQueryUrl(SbdbImportRequest request, IReadOnlyCollection<string>? fields)
        {
            int extra = request.SkipNames?.Count ?? 0;
            int limit = request.Count + extra + 16;
            var query = new StringBuilder();
            query.Append(QueryUrl);
            query.Append("?fields=spkid,full_name,name,pdes,class,epoch_cal,e,a,i,om,w,ma,diameter,albedo,density,GM,rot_per");
            query.Append("&sb-kind=a");
            query.Append("&sb-cdata=").Append(Uri.EscapeDataString("{\"AND\":[\"diameter|DF\",\"e|LT|1\"]}"));
            query.Append("&sort=-diameter");
            query.Append("&limit=").Append(limit.ToString(CultureInfo.InvariantCulture));
            query.Append("&full-prec=1");

            if (SendsClassFilter(fields))
            {
                var codes = new List<string>();
                foreach (var field in fields)
                {
                    foreach (var pair in ClassToField)
                    {
                        if (pair.Value == field && !codes.Contains(pair.Key))
                            codes.Add(pair.Key);
                    }
                }
                if (codes.Count > 0)
                    query.Append("&sb-class=").Append(string.Join(",", codes));
            }

            return query.ToString();
        }

        static IReadOnlyCollection<string>? NormalizeFields(IReadOnlyCollection<string>? fields)
        {
            if (fields == null || fields.Count == 0)
                return null;

            var normalized = new List<string>();
            foreach (var field in fields)
            {
                if (string.IsNullOrWhiteSpace(field))
                    continue;
                if (!KnownFields.Contains(field))
                    throw new ArgumentException("Unknown field '" + field + "'. Known fields: " + string.Join(", ", KnownFields));
                if (!normalized.Contains(field))
                    normalized.Add(field);
            }

            return normalized.Count == 0 ? null : normalized;
        }

        static bool SendsClassFilter(IReadOnlyCollection<string>? fields)
        {
            // "other" is every class we do not name. The query has to stay open so those rows come back.
            return fields != null && !fields.Contains("other");
        }

        static SbdbImportResult Parse(string responseJson, SbdbImportRequest request, IReadOnlyCollection<string>? fields)
        {
            var root = JObject.Parse(responseJson);
            var fieldNames = root["fields"] as JArray;
            var rows = root["data"] as JArray;
            if (fieldNames == null || rows == null)
                throw new InvalidOperationException("SBDB response has no fields/data table.");

            var columns = new Dictionary<string, int>();
            for (int i = 0; i < fieldNames.Count; i++)
                columns[fieldNames[i]!.Value<string>()!] = i;

            var skip = new HashSet<string>(request.SkipNames ?? Array.Empty<string>(), StringComparer.OrdinalIgnoreCase);
            var usedIds = new HashSet<string>(StringComparer.Ordinal);
            var bodies = new List<SbdbImportedBody>();
            int skippedExisting = 0;

            foreach (var rowToken in rows)
            {
                if (bodies.Count >= request.Count)
                    break;
                if (rowToken is not JArray row)
                    continue;

                string? name = Text(row, columns, "name");
                string? pdes = Text(row, columns, "pdes");
                string display = !string.IsNullOrWhiteSpace(name) ? name! : (pdes ?? Text(row, columns, "spkid") ?? "");
                if (display.Length == 0)
                    continue;
                if (skip.Contains(display))
                {
                    skippedExisting++;
                    continue;
                }

                string jplClass = Text(row, columns, "class") ?? "";
                string field = ClassToField.TryGetValue(jplClass, out var mapped) ? mapped : "other";
                if (fields != null && !fields.Contains(field))
                    continue;

                double? eccentricity = Num(row, columns, "e");
                double? semiMajorAu = Num(row, columns, "a");
                double? diameterKm = Num(row, columns, "diameter");
                if (eccentricity == null || eccentricity >= 1 || semiMajorAu == null || semiMajorAu <= 0 || diameterKm == null || diameterKm <= 0)
                    continue;

                string idSource = !string.IsNullOrWhiteSpace(pdes) ? pdes! : (Text(row, columns, "spkid") ?? display);
                string uniqueId = UniqueId(idSource);
                if (!usedIds.Add(uniqueId))
                    uniqueId = uniqueId + "-" + (Text(row, columns, "spkid") ?? bodies.Count.ToString(CultureInfo.InvariantCulture));

                double? gm = Num(row, columns, "GM");
                double? density = Num(row, columns, "density");
                double mass = MassKg(gm, density, diameterKm.Value, field);
                double? rotHours = Num(row, columns, "rot_per");
                double? albedo = Num(row, columns, "albedo");

                var info = new SystemBodyBlueprint.SystemBodyInfoBlueprint
                {
                    Type = "asteroid",
                    Albedo = (float)(albedo ?? 0.15),
                    Mass = mass,
                    Radius = diameterKm.Value / 2.0,
                };
                if (rotHours != null && rotHours > 0 && rotHours < 10000)
                    info.LengthOfDay = TimeSpan.FromHours(rotHours.Value);

                var orbit = new SystemBodyBlueprint.OrbitBlueprint
                {
                    SemiMajorAxis = semiMajorAu.Value * AuInKm,
                    Eccentricity = eccentricity,
                    EclipticInclination = Num(row, columns, "i") ?? 0,
                    LoAN = Num(row, columns, "om") ?? 0,
                    AoP = Num(row, columns, "w") ?? 0,
                    MeanAnomaly = Num(row, columns, "ma") ?? 0,
                    Epoch = ParseEpoch(Text(row, columns, "epoch_cal")),
                };

                uint survey = (uint)Math.Clamp((int)Math.Round(diameterKm.Value), 25, 750);
                bodies.Add(new SbdbImportedBody
                {
                    Field = field,
                    FileName = FileNameForField(field),
                    Blueprint = new SystemBodyBlueprint
                    {
                        UniqueID = uniqueId,
                        Name = display,
                        Colonizable = false,
                        GeoSurveyPointsRequired = survey,
                        GenerateMinerals = "random",
                        Info = info,
                        Orbit = orbit,
                    }
                });
            }

            return new SbdbImportResult
            {
                Bodies = bodies,
                SkippedExisting = skippedExisting,
            };
        }

        static double MassKg(double? gm, double? densityGPerCm3, double diameterKm, string field)
        {
            if (gm != null && gm > 0)
                return gm.Value / GravitationalConstantKm;

            double density = densityGPerCm3 ?? (field == "centaurs" || field == "trans-neptunian" ? 1.0 : 2.5);
            double radiusM = diameterKm / 2.0 * 1000.0;
            double volume = 4.0 / 3.0 * Math.PI * radiusM * radiusM * radiusM;
            return density * 1000.0 * volume;
        }

        public static DateTime ParseEpoch(string? epochCal)
        {
            if (string.IsNullOrWhiteSpace(epochCal) || epochCal.Length < 10)
                throw new FormatException("SBDB epoch_cal is missing.");

            var date = DateTime.ParseExact(epochCal.Substring(0, 10), "yyyy-MM-dd", CultureInfo.InvariantCulture);
            if (epochCal.Length > 11 && epochCal[10] == '.')
            {
                if (double.TryParse("0." + epochCal.Substring(11), NumberStyles.Float, CultureInfo.InvariantCulture, out double fraction))
                    date = date.AddDays(fraction);
            }
            return DateTime.SpecifyKind(date, DateTimeKind.Unspecified);
        }

        static string UniqueId(string pdes)
        {
            var builder = new StringBuilder("asteroid-");
            bool hyphen = false;
            foreach (char c in pdes.Trim().ToLowerInvariant())
            {
                if (char.IsLetterOrDigit(c))
                {
                    builder.Append(c);
                    hyphen = false;
                }
                else if (!hyphen)
                {
                    builder.Append('-');
                    hyphen = true;
                }
            }
            while (builder.Length > "asteroid".Length && builder[builder.Length - 1] == '-')
                builder.Length--;
            return builder.ToString();
        }

        static string? Text(JArray row, Dictionary<string, int> columns, string name)
        {
            if (!columns.TryGetValue(name, out int index) || index >= row.Count)
                return null;
            var token = row[index];
            if (token == null || token.Type == JTokenType.Null)
                return null;
            var text = token.Type == JTokenType.String ? token.Value<string>() : token.ToString();
            text = text?.Trim();
            return string.IsNullOrEmpty(text) ? null : text;
        }

        static double? Num(JArray row, Dictionary<string, int> columns, string name)
        {
            var text = Text(row, columns, name);
            if (text == null)
                return null;
            if (double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out double value))
                return value;
            return null;
        }
    }

    public sealed class SbdbImportRequest
    {
        public int Count { get; init; } = 500;
        public IReadOnlyCollection<string>? Fields { get; init; }
        public IReadOnlyCollection<string>? SkipNames { get; init; }
    }

    public sealed class SbdbImportedBody
    {
        public string Field { get; init; } = "";
        public string FileName { get; init; } = "";
        public SystemBodyBlueprint Blueprint { get; init; } = new SystemBodyBlueprint();
    }

    public sealed class SbdbImportResult
    {
        public IReadOnlyList<SbdbImportedBody> Bodies { get; init; } = Array.Empty<SbdbImportedBody>();
        public int SkippedExisting { get; init; }
    }
}
