using System;
using Pulsar4X.Engine;

namespace Pulsar4X.Interfaces
{
    /// <summary>
    /// Hotloop processor - this proccessor is fired at a specific regular time interval
    /// </summary>
    public interface IHotloopProcessor
    {
        /// <summary>
        /// used to initialize processors that need access to static data etc, used to construct a derived class's concrete object.
        /// </summary>
        /// <param name="game"></param>
        void Init(Game game);

        /// <summary>
        /// used when a specific entity should be processed.
        /// </summary>
        /// <param name="entity">Entity.</param>
        /// <param name="deltaSeconds">Delta seconds.</param>
        void ProcessEntity(Entity entity, int deltaSeconds);

        /// <summary>
        /// used to process all entities that have the GetParameterType type of datablob in a specific manager.
        /// </summary>
        /// <param name="manager">Manager.</param>
        /// <param name="deltaSeconds">Delta seconds.</param>
        int ProcessManager(EntityManager manager, int deltaSeconds);

        /// <summary>
        /// How Often this processor should run.
        /// </summary>
        /// <value>The run frequency.</value>
        TimeSpan RunFrequency { get; }
        /// <summary>
        /// This is so that each processor can be offset a bit, so they are spaced apart a bit.
        /// In the case of short quick turns this should help prevent a lag spike as the game tries to process all economy etc on a single tick.
        /// </summary>
        /// <value>The first run offset.</value>
        TimeSpan FirstRunOffset { get; }
        /// <summary>
        /// this should return the specific Datablob that a derived class is accociated with.
        /// </summary>
        /// <value>The type of the get parameter.</value>
        Type GetParameterType { get; }
    }
}