using System;
using Newtonsoft.Json;

namespace Pulsar4X.Datablobs
{
    public class ProjectileInfoDB : BaseDataBlob
    {
        public string LaunchedBy = Guid.NewGuid().ToString();
        public int Count = 1;

        [JsonConstructor]
        private ProjectileInfoDB()
        {
        }

        public ProjectileInfoDB(string launchedBy, int count)
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