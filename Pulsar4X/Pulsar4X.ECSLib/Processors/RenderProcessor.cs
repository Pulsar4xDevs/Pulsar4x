using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pulsar4X.ECSLib
{
    /// <summary>
    /// This processor goes through the entities and updates the scenes
    /// </summary>
    public static class RenderProcessor
    {

        /// <summary>
        /// TypeIndexes for several dataBlobs used frequently by this processor.
        /// </summary>
        private static int _orbitTypeIndex = -1;
        private static int _positionTypeIndex = -1;
        private static int _starInfoTypeIndex = -1;

        /// <summary>
        /// Initializes this Processor.
        /// </summary>
        internal static void Initialize()
        {
            // Resolve TypeIndexes.
            _orbitTypeIndex = EntityManager.GetTypeIndex<OrbitDB>();
            _positionTypeIndex = EntityManager.GetTypeIndex<PositionDB>();
            _starInfoTypeIndex = EntityManager.GetTypeIndex<StarInfoDB>();
        }

        /// <summary>
        /// Function called by Game.RunProcessors to run this processor.
        /// </summary>
        internal static void Process()
        {

        }
    }
}