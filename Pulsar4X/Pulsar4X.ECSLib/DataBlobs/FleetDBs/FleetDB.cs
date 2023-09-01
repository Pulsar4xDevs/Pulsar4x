using System;
using System.Collections.Generic;

namespace Pulsar4X.ECSLib
{
    public class NavyDB : TreeHierarchyDB
    {
        public NavyDB() : base(null) {}

        public override object Clone()
        {
            return new NavyDB();
        }
    }
}