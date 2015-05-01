using System;

namespace Pulsar4X.ECSLib
{
    public struct RefineingSD
    {
        public Guid ID;
        public string Name;
        public string Description;
        public JDictionary<Guid, int> Ingredients;
    }
}