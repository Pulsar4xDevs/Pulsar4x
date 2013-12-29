using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace Pulsar4X.Entities
{
    /// <summary>
    /// Base Game Entity. All objects/entities in the game should inherit from this.
    /// </summary>
    public class GameEntity : INotifyPropertyChanged
    {
        public Guid Id { get; set; }
        public string Name { get; set; }

        public GameEntity()
        {
        }

        public override string ToString() 
        {
            if (Name != null)
            {
                return Name;
            }

            return this.GetType().FullName;
        }


        // The Below causes problems...
        //public static bool operator ==(GameEntity a_oLeft, GameEntity a_oRight)
        //{
        //    if (a_oLeft.Id == a_oRight.Id)
        //    {
        //        return true;
        //    }

        //    return false;
        //}

        //public static bool operator !=(object a_oLeft, object a_oRight)
        //{
        //    if (a_oLeft.Id != a_oRight.Id)
        //    {
        //        return true;
        //    }

        //    return false;
        //}

        //public override bool Equals(object obj)
        //{
        //    if (obj.GetType() != typeof(GameEntity))
        //    {
        //        return false;
        //    }

        //    GameEntity oGameEntity = (GameEntity)obj;
        //    return oGameEntity == this;
        //}

        //public override int GetHashCode()
        //{
        //    return Id.GetHashCode();
        //}

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
