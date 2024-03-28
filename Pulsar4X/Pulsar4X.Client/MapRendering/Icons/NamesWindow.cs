using System;
using System.Collections.Generic;
using System.Numerics;
namespace Pulsar4X.SDL2UI
{
    class NameWidget : IComparable<NameWidget>, IRectangle
    {
        EntityState EntityState;
        string NameString { get { return EntityState.Name; } }
        List<EntityState> SubEntites;

        public System.Numerics.Vector2 WorldPosition;
        public System.Numerics.Vector2 ViewPostion;

        public float X => ViewPostion.X;
        public float Y => ViewPostion.Y;


        public float Width { get; set; }
        public float Height { get; set; }

        public int CompareTo(NameWidget compareIcon)
        {
            if (WorldPosition.Y > compareIcon.WorldPosition.Y) return -1;
            else if (this.WorldPosition.Y < compareIcon.WorldPosition.Y) return 1;
            else
            {
                if (this.WorldPosition.X > compareIcon.WorldPosition.X) return 1;
                else if (this.WorldPosition.X < compareIcon.WorldPosition.X) return -1;
                else return -NameString.CompareTo(compareIcon.NameString);
            }
        }
    }

    public class NamesWindow
    {
        List<EntityState> PrimaryEntities = new List<EntityState>();
        Dictionary<EntityState, List<EntityState>> SecondaryEntitys = new Dictionary<EntityState, List<EntityState>> ();      

        public NamesWindow()
        {
        }


        internal void AddPrimary(EntityState entityState)
        {
            PrimaryEntities.Add(entityState); 
        }


    }
}
