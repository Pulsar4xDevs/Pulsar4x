using System;
using Pulsar4X.Engine;
using Pulsar4X.Interfaces;

namespace Pulsar4X.People
{
    public class NavalAcademyProcessor : IInstanceProcessor
    {
        internal override void ProcessEntity(Entity entity, DateTime atDateTime)
        {
            NavalAcademyCourse.Graduate(entity, atDateTime);
        }
    }
}
