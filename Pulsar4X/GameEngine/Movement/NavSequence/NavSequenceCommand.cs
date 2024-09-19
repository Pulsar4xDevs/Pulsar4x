using System;
using System.Collections.Generic;
using Pulsar4X.Datablobs;
using Pulsar4X.DataStructures;

namespace Pulsar4X.Engine.Orders
{
    //This is the interface between the UI and the NavSequenceDB. 
    //We use this to add, remove, edit etc movement manuvers to the ship.
    public class NavSequenceCommand : EntityCommand
    {
        public override string Name { get; } = "";

        public override string Details { get; } = "";

        public bool CycleCommand = false;

        public override ActionLaneTypes ActionLanes => ActionLaneTypes.Movement;

        private SafeList<Manuver> _manuvers = new SafeList<Manuver>();
        
        public override bool IsBlocking { get; } = false;
        
        Entity _entityCommanding;
        internal override Entity EntityCommanding { get { return _entityCommanding; } }
        internal override bool IsValidCommand(Game game)
        {
            return true;
        }


        public static void CreateNewCommand(Entity entity, Manuver manuver)
        {
            var navCommand = new NavSequenceCommand();
            navCommand._entityCommanding = entity;
            navCommand._manuvers.Add(manuver);
            navCommand.Execute(entity.StarSysDateTime);
        }
        
        
        internal override void Execute(DateTime atDateTime)
        {
            if (!EntityCommanding.TryGetDatablob(out NavSequenceDB navDB))
            {
                navDB = new NavSequenceDB();
                EntityCommanding.SetDataBlob(navDB);
            }

            foreach (var manuver in _manuvers)
            {
                navDB.AddManuver(manuver);
            }
        }

        public override bool IsFinished()
        {
            return true;
        }

        public override EntityCommand Clone()
        {
            throw new NotImplementedException();
        }
    }
}