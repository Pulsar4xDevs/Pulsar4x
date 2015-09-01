using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Newtonsoft.Json;

namespace Pulsar4X.ECSLib
{
    public delegate void StatRecalc(Entity ship);
    public class ComponentInfoDB : BaseDataBlob
    {
        [JsonProperty]
        private int _sizeInTons;
        public int SizeInTons { get { return _sizeInTons; } internal set { _sizeInTons = value; } }

        [JsonProperty]
        private int _htk;
        public int HTK { get { return _htk; } internal set { _htk = value; } }

        [JsonProperty]
        private JDictionary<Guid, int> _materialCosts;
        public JDictionary<Guid, int> MaterialCosts { get { return _materialCosts; } internal set { _materialCosts = value; } }

        [JsonProperty]
        private Guid _techReqToBuild; //maybe have a requirement to use as well? might be usefull later down the track...
        public Guid TechRequirementToBuild { get { return _techReqToBuild; } internal set { _techReqToBuild = value; } }

        [JsonProperty] 
        private int _crewRequirement = 0;
        public int CrewRequrements { get { return _crewRequirement; } internal set { _crewRequirement = value; } }

        //this should maybe be a list of delegates, for if there's multiple component abilitys...
        public Delegate StatRecalcDelegate;
        

        public ComponentInfoDB(int size, int htk, JDictionary<Guid,int> materialCosts, Guid techRequrement, int crewReqirement)
        {
            _sizeInTons = size;
            _htk = htk;
            _materialCosts = materialCosts;
            _techReqToBuild = techRequrement;
            _crewRequirement = crewReqirement;
        }

        public ComponentInfoDB(ComponentInfoDB db)
        {
            _sizeInTons = db.SizeInTons;
            _htk = db.HTK;
            _materialCosts = db.MaterialCosts;
            _techReqToBuild = db.TechRequirementToBuild;
        }

        public override object Clone()
        {
            return new ComponentInfoDB(this);
        }
    }
}