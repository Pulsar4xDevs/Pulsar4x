using Pulsar4X.Engine;

namespace Pulsar4X.Interfaces
{
    /// <summary>
    /// Recalc processor. - this processor is called when something on the entity changes.
    /// ie if a ship gets damaged, or modified, etc. the max speed and other stuff may need to be recalculated.
    /// </summary>
    internal interface IRecalcProcessor
    {
        /// <summary>
        /// This is used so that some recalc processors can be run before others
        /// </summary>
        /// <value>The process priority.</value>
        byte ProcessPriority { get; set; }
        /// <summary>
        /// function to recalculate an entity.
        /// </summary>
        /// <param name="entity">Entity.</param>
        void RecalcEntity(Entity entity);
    }
}