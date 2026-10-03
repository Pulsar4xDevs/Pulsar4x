using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Pulsar4X.Galaxy;

namespace Pulsar4X.SolBodies;

/// <summary>
/// Writes JPL small bodies into basemod. The conversion lives in
/// <see cref="SbdbSmallBodyImporter"/> so the game can call it later.
/// </summary>
public static class Program
{
    public static int Main(string[] args)
    {
        try
        {
            return Run(args);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex.Message);
            return 1;
        }
    }

    static int Run(string[] args)
    {
        int count = 500;
        var fields = new List<string>();
        bool dryRun = false;
        string? basemod = null;

        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--count":
                    count = int.Parse(Next(args, ref i), System.Globalization.CultureInfo.InvariantCulture);
                    break;
                case "--field":
                    fields.Add(Next(args, ref i));
                    break;
                case "--basemod":
                    basemod = Next(args, ref i);
                    break;
                case "--dry-run":
                    dryRun = true;
                    break;
                case "--help":
                case "-h":
                    Console.WriteLine("SolBodies --count 500 [--field main-belt] [--basemod path] [--dry-run]");
                    Console.WriteLine("Fields: " + string.Join(", ", SbdbSmallBodyImporter.KnownFields));
                    return 0;
                default:
                    throw new ArgumentException("Unknown argument " + args[i]);
            }
        }

        string modRoot = FindBasemod(basemod);
        var skip = ReadHandAuthoredNames(modRoot);
        using var http = new HttpClient();
        http.Timeout = TimeSpan.FromSeconds(60);
        http.DefaultRequestHeaders.UserAgent.ParseAdd("Pulsar4X-SolBodies");

        var result = SbdbSmallBodyImporter.Import(new SbdbImportRequest
        {
            Count = count,
            Fields = fields,
            SkipNames = skip,
        }, http);

        var groups = result.Bodies.GroupBy(body => body.FileName).OrderBy(group => group.Key).ToList();
        Console.WriteLine("Skipped " + result.SkippedExisting + " already in Sol.");
        foreach (var group in groups)
            Console.WriteLine(group.Key + "  " + group.Count());
        Console.WriteLine(result.Bodies.Count + " bodies.");
        if (dryRun)
        {
            Console.WriteLine("Dry run. No files written.");
            return 0;
        }

        WriteBodies(modRoot, groups);
        UpdateModInfo(modRoot, groups.Select(group => group.Key));
        UpdateSystemBodies(modRoot, result.Bodies.Select(body => body.Blueprint.UniqueID));
        Console.WriteLine("Wrote " + modRoot);
        return 0;
    }

    static string Next(string[] args, ref int i)
    {
        if (i + 1 >= args.Length)
            throw new ArgumentException(args[i] + " needs a value.");
        i++;
        return args[i];
    }

    static string FindBasemod(string? requested)
    {
        if (!string.IsNullOrWhiteSpace(requested))
        {
            string full = Path.GetFullPath(requested);
            if (!File.Exists(Path.Combine(full, "modInfo.json")))
                throw new DirectoryNotFoundException("No modInfo.json in " + full);
            return full;
        }

        foreach (var start in new[] { Directory.GetCurrentDirectory(), AppContext.BaseDirectory })
        {
            var dir = new DirectoryInfo(start);
            while (dir != null)
            {
                string nested = Path.Combine(dir.FullName, "Pulsar4X", "GameData", "basemod");
                string here = Path.Combine(dir.FullName, "GameData", "basemod");
                if (File.Exists(Path.Combine(nested, "modInfo.json")))
                    return nested;
                if (File.Exists(Path.Combine(here, "modInfo.json")))
                    return here;
                dir = dir.Parent;
            }
        }

        throw new DirectoryNotFoundException("Could not find GameData/basemod. Pass --basemod.");
    }

    static HashSet<string> ReadHandAuthoredNames(string modRoot)
    {
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        string solDir = Path.Combine(modRoot, "ScenarioFiles", "systems", "sol");
        foreach (var file in Directory.GetFiles(solDir, "*.json"))
        {
            if (Path.GetFileName(file).StartsWith("asteroids-", StringComparison.Ordinal))
                continue;
            var items = JArray.Parse(File.ReadAllText(file));
            foreach (var item in items)
            {
                if (item?["Type"]?.Value<string>() != "SystemBody")
                    continue;
                string? name = item["Payload"]?["Name"]?.Value<string>();
                if (!string.IsNullOrWhiteSpace(name))
                    names.Add(name);
            }
        }
        return names;
    }

    static void WriteBodies(string modRoot, IEnumerable<IGrouping<string, SbdbImportedBody>> groups)
    {
        string solDir = Path.Combine(modRoot, "ScenarioFiles", "systems", "sol");
        var serializer = JsonSerializer.Create(new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Ignore,
            Formatting = Formatting.Indented,
            DateFormatString = "yyyy-MM-ddTHH:mm:ss",
        });

        var written = new HashSet<string>(StringComparer.Ordinal);
        foreach (var group in groups)
        {
            var array = new JArray();
            foreach (var body in group)
            {
                array.Add(new JObject
                {
                    ["Type"] = "SystemBody",
                    ["Payload"] = JObject.FromObject(body.Blueprint, serializer),
                });
            }
            File.WriteAllText(Path.Combine(solDir, group.Key), array.ToString(Formatting.Indented) + "\n", new UTF8Encoding(false));
            written.Add(group.Key);
        }

        foreach (var stale in Directory.GetFiles(solDir, "asteroids-*.json"))
        {
            if (!written.Contains(Path.GetFileName(stale)))
                File.Delete(stale);
        }
    }

    static void UpdateModInfo(string modRoot, IEnumerable<string> fileNames)
    {
        string path = Path.Combine(modRoot, "modInfo.json");
        var manifest = JObject.Parse(File.ReadAllText(path));
        var files = (JArray)manifest["DataFiles"]!;
        var kept = new JArray();
        int insertAt = 0;
        foreach (var token in files)
        {
            string entry = token.Value<string>()!;
            if (entry.StartsWith("ScenarioFiles/systems/sol/asteroids-", StringComparison.Ordinal))
                continue;
            kept.Add(entry);
            if (entry.EndsWith("/comets.json", StringComparison.Ordinal))
                insertAt = kept.Count;
        }
        if (insertAt == 0)
            insertAt = kept.Count;
        foreach (var fileName in fileNames.OrderBy(name => name, StringComparer.Ordinal))
            kept.Insert(insertAt++, "ScenarioFiles/systems/sol/" + fileName);

        manifest["DataFiles"] = kept;
        File.WriteAllText(path, manifest.ToString(Formatting.Indented) + "\n", new UTF8Encoding(false));
    }

    static void UpdateSystemBodies(string modRoot, IEnumerable<string> ids)
    {
        string path = Path.Combine(modRoot, "ScenarioFiles", "systems", "sol", "sol.json");
        var items = JArray.Parse(File.ReadAllText(path));
        foreach (var item in items)
        {
            if (item?["Type"]?.Value<string>() != "System")
                continue;
            var bodies = (JArray)item["Payload"]!["Bodies"]!;
            var kept = new JArray();
            foreach (var body in bodies)
            {
                string id = body.Value<string>()!;
                if (!id.StartsWith("asteroid-", StringComparison.Ordinal))
                    kept.Add(id);
            }
            foreach (var id in ids)
                kept.Add(id);
            item["Payload"]!["Bodies"] = kept;
        }
        File.WriteAllText(path, items.ToString(Formatting.Indented) + "\n", new UTF8Encoding(false));
    }
}
