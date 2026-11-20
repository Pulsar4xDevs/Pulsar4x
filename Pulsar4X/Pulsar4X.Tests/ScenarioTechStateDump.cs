using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using NUnit.Framework;
using Pulsar4X.Client;
using Pulsar4X.Client.Host;
using Pulsar4X.Engine;
using Pulsar4X.Factions;
using Pulsar4X.Names;

namespace Pulsar4X.Tests;

/// <summary>
/// A tool, not a check: writes every faction's starting tech state (each unlocked tech with its level, plus the unlocked
/// component templates, armor, cargo, industry types) for each shipped scenario to the file named by the TECH_DUMP
/// environment variable. Run it before and after a tech-tree change and diff the two files to prove the scenarios start
/// with the same researched and researchable sets. Explicit, so the normal run skips it.
/// </summary>
[TestFixture]
public class ScenarioTechStateDump
{
    [Test, Explicit("Writes the file named by TECH_DUMP")]
    public void Dump()
    {
        var path = Environment.GetEnvironmentVariable("TECH_DUMP");
        Assert.That(path, Is.Not.Null.And.Not.Empty, "set TECH_DUMP to the output file");
        var result = new SortedDictionary<string, object>();
        foreach (var scenario in new[] { "Pulsar4x.Scenario.EarlyTech", "Pulsar4x.Scenario.WarpTechStart" })
        {
            ModsState.RefreshModsList("Data");
            var paths = ModsState.AvailableMods.Where(m => m.Mod.ModName == "Pulsar4x").Select(m => m.Path).ToList();
            var store = GameLifecycle.LoadModStore(paths, ModsState.AvailableMods.Select(m => m.Path), scenario);
            var catalog = GameLifecycle.BuildCatalog(store);
            var request = GameLifecycle.BuildQuickstartRequest(paths, catalog, 12345678);
            var created = GameLifecycle.CreateGameCore(store, request);
            Assert.That(created, Is.Not.Null);
            var game = created!.Value.game;
            foreach (var faction in game.Factions.Values.OrderBy(f => f.GetDataBlob<NameDB>().DefaultName))
            {
                var data = faction.GetDataBlob<FactionInfoDB>().Data;
                result[scenario + " / " + faction.GetDataBlob<NameDB>().DefaultName] = new SortedDictionary<string, object>
                {
                    ["techs"] = data.Techs.ToDictionary(kv => kv.Key, kv => kv.Value.Level).OrderBy(kv => kv.Key).ToDictionary(kv => kv.Key, kv => kv.Value),
                    ["lockedTechCount"] = data.LockedTechs.Count,
                    ["components"] = data.ComponentTemplates.Keys.OrderBy(k => k).ToList(),
                    ["armor"] = data.Armor.Keys.OrderBy(k => k).ToList(),
                    ["cargoTypes"] = data.CargoTypes.Keys.OrderBy(k => k).ToList(),
                    ["industryTypes"] = data.IndustryTypes.Keys.OrderBy(k => k).ToList(),
                };
            }
        }
        File.WriteAllText(path!, JsonConvert.SerializeObject(result, Formatting.Indented));
    }
}
