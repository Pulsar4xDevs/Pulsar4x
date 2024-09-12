using System;
using Pulsar4X.Engine;

namespace Pulsar4X.Interfaces
{
    /// <summary>
    /// Instance processor. - This processor is fired at a specific timedate or on command.
    /// </summary>
    public abstract class IInstanceProcessor
    {
        internal string TypeName { get { return GetType().Name; } }
        internal abstract void ProcessEntity(Entity entity, DateTime atDateTime);
    }
}