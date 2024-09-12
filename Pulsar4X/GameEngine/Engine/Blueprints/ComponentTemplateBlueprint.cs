using System.Collections.Generic;
using Pulsar4X.DataStructures;

namespace Pulsar4X.Blueprints
{
    public class ComponentTemplateBlueprint : Blueprint
    {
        public string Name { get; set;}

        /// <summary>
        /// Description
        /// Mass
        /// Volume
        /// HTK
        /// CrewReq
        /// ResourceCost
        /// ResearchCost
        /// CreditCost
        /// BuildPointCost
        /// </summary>
        public Dictionary<string, string> Formulas { get; set;}
        public Dictionary<string, string> ResourceCost { get; set; }
        public string ComponentType { get; set; }
        public ComponentMountType MountType { get; set; }
        public string IndustryTypeID { get; set;}
        public string CargoTypeID { get; set;}
        public List<ComponentTemplateAttributeBlueprint> Attributes { get; set; }
    }
}