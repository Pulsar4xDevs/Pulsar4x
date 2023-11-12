using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Pulsar4X.DataStructures;
using Pulsar4X.Interfaces;

namespace Pulsar4X.Engine;

public class ProcessSet
{
    [JsonProperty] //this needs to get saved. need to check that entities here are saved as guids in the save file and that they get re-referenced on load too (should happen if the serialization manager does its job properly).
    public Dictionary<string, List<Entity>> InstanceProcessors { get; } = new Dictionary<string, List<Entity>>();

    //todo: need to get a list of InstanceProcessors that have entites owned by a specific faction.

    internal ProcessSet()
    {
    }

    // FIXME: needs to get rid of StaticRefLib references
    // internal ProcessSet(SerializationInfo info, StreamingContext context)
    // {
    //     Game game = (Game)context.Context;
    //     Dictionary<string, List<Guid>> instanceProcessors = (Dictionary<string, List<Guid>>)info.GetValue(nameof(InstanceProcessors), typeof(Dictionary<string, List<Guid>>));
    //     ProcessorManager processManager = StaticRefLib.ProcessorManager;
    //     foreach (var kvpItem in instanceProcessors)
    //     {

    //         string typeName = kvpItem.Key;

    //         //IInstanceProcessor processor = processManager.GetInstanceProcessor(typeName);
    //         if (!InstanceProcessors.ContainsKey(typeName))
    //             InstanceProcessors.Add(typeName, new List<Entity>());

    //         foreach (var entityGuid in kvpItem.Value)
    //         {
    //             if (game.GlobalManager.FindEntityByGuid(entityGuid, out Entity entity)) //might be a better way to do this, can we get the manager from here and just search localy?
    //             {
    //                 InstanceProcessors[typeName].Add(entity);
    //             }
    //             else
    //             {
    //                 // Entity has not been deserialized.
    //                 // throw new Exception("Unfound Entity Exception, possibly this entity hasn't been deseralised yet?"); //I *think* we'll have the entitys all deseralised for this manager at this point...
    //             }
    //         }
    //     }
    // }

    internal List<string> GetInstanceProcForEntity(Entity entity)
    {
        var procList = new List<string>();


        foreach (var kvp in InstanceProcessors)
        {
            if (kvp.Value.Contains(entity))
                procList.Add(kvp.Key);
        }

        return procList;
    }

    internal List<string> RemoveEntity(Entity entity)
    {
        var procList = new List<string>();
        var removelist = new List<string>();
        foreach (var kvp in InstanceProcessors)
        {
            if (kvp.Value.Contains(entity))
                kvp.Value.Remove(entity);
            procList.Add(kvp.Key);
            if (kvp.Value.Count == 0)
                removelist.Add(kvp.Key);
        }

        foreach (var item in removelist)
        {
            InstanceProcessors.Remove(item);
        }

        return procList;
    }

    internal bool IsEmpty()
    {
        return InstanceProcessors.Count == 0;
    }
}