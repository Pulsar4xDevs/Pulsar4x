using System;
using Newtonsoft.Json;

namespace Pulsar4X.Datablobs
{
    public class ProjectileInfoDB : BaseDataBlob
    {
        public int LaunchedBy { get; set; } = -1;
        public int Count = 1;

        [JsonConstructor]
        private ProjectileInfoDB()
        {
        }

        public ProjectileInfoDB(int launchedBy, int count)
        {
            LaunchedBy = launchedBy;
            Count = count;
        }

        public override object Clone()
        {
            throw new System.NotImplementedException();
        }
    }
}