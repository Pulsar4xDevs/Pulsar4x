using System;
using System.Collections.Generic;
using System.Text;

namespace Pulsar4X.Orbital.Helpers
{
    public static class PositionHelper
    {
        public static Vector3 GetAbsolutePositionInAU(EntityBase entity)
        {
            return Distance.MToAU(GetAbsolutePositionInMetres(entity));
        }

        public static Vector3 GetAbsolutePositionInMetres(EntityBase entity)
        {
            if (entity.Parent == null || entity.Parent == entity)
                return entity.PositionInMetres;

            Vector3 parentpos = GetAbsolutePositionInMetres(entity.Parent);
            return parentpos + entity.PositionInMetres;
        }
    }
}
