using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Pulsar4X.ECSLib
{
    /// <summary>
    /// This is a Wrapper Class for DataBlob references. It will automaticaly be re-linked when the game is loaded from disk.
    /// @note DO NOT use this object as a key to a dictionary. instead use the Guid of the Datablobs entity (which you can access via DataBlob.EntityGuid)
    /// </summary>
    /// <typeparam name="T">A type of Datablob.</typeparam>
    public class DataBlobRef<T> : IPostLoad, ISerializable
        where T : BaseDataBlob
    {
        /// <summary>
        /// A reference to a data blob.
        /// </summary>
        public T Ref
        {
            get { return _ref; }
        }
        private T _ref;
        
        // Cache for the referenced datablob owning entities Guid, so we can re-link the reference on post load.
        private Guid _refOwnerGuid;

        /// <summary>
        /// Creates a DataBlob reference by looking up the datablob for the specified Entity.
        /// </summary>
        /// <param name="fromEntityGuid"> </param>
        public DataBlobRef(Guid fromEntityGuid)
        {
            _refOwnerGuid = fromEntityGuid;
            _ref = ResolveGuid();
            RegisterPostLoad();
        }

        /// <summary>
        /// Creates a DataBlob reference from the datablob provided.
        /// </summary>
        /// <param name="dataBlob"></param>
        public DataBlobRef(T dataBlob)
        {
            _ref = dataBlob;
            RegisterPostLoad();
        }

        private T ResolveGuid()
         {
             if (_refOwnerGuid == Guid.Empty)
                 return null;  // this references nothing!

             EntityManager em;
             int entityID;
             if (!EntityManager.FindEntityByGuid(_refOwnerGuid, out em, out entityID))
             {
                 throw new GuidNotFoundException();
             }
             return em.GetDataBlob<T>(entityID);
         }

        public bool IsNull()
        {
            return _ref == null;
        }

        public static DataBlobRef<T> Null()
        {
            return null;
        }

        #region Operator Overloads

        public static bool operator ==(DataBlobRef<T> a, DataBlobRef<T> b)
        {
            // If both are null, or both are same instance, return true.
            if (System.Object.ReferenceEquals(a, b))
                return true;

            // If one is null, but not both, return false.
            if (((object)a == null) || ((object)b == null))
            {
                return false;
            }

            return a._refOwnerGuid == b._refOwnerGuid && a.Ref == b.Ref;
        }

        public static bool operator ==(DataBlobRef<T> a, T b)
        {
            // If both are null, return true.
            if (((object)a == null) && b == null)
                return true;

            // a is null return false.
            if (((object)a == null))
            {
                return false;
            }

            return a.Ref == b;
        }

        public static bool operator ==(T b, DataBlobRef<T> a)
        {
            return (a == b);
        }

        public static bool operator !=(DataBlobRef<T> a, DataBlobRef<T> b)
        {
            return !(a == b);
        }

        public static bool operator !=(DataBlobRef<T> a, T b)
        {
            return !(a == b);
        }

        public static bool operator !=(T b, DataBlobRef<T> a)
        {
            return !(a == b);
        }

        #endregion

        #region Base Overloads

        protected bool Equals(DataBlobRef<T> other)
        {
            return EqualityComparer<T>.Default.Equals(_ref, other._ref);
        }

        protected bool Equals(T other)
        {
            return EqualityComparer<T>.Default.Equals(_ref, other);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
            {
                return false;
            }
            if (ReferenceEquals(this, obj))
            {
                return true;
            }
            if (obj.GetType() == this.GetType())
            {
                return Equals((DataBlobRef<T>)obj);
            }
            if (obj.GetType() == _ref.GetType())
            {
                return Equals((T)obj);
            }

            return false;
        }

        /// <summary>
        /// Note that this hasing function does not match the .Equals() and == methods as
        /// it will not return the same has as the object held in Ref. This is to make the 
        /// object work as a key to a dictionay for JSON.net and LINQ support.
        /// </summary>
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        #endregion

        #region IPostLoad

        public void RegisterPostLoad()
        {
            if (!Game.Instance.IsLoaded)
                Game.Instance.PostLoad += new EventHandler(PostLoad);
        }

        public void PostLoad(object sender, EventArgs e)
        {
            // on post load we lookup the datablob by guid, thus re-linking the reference.
            _ref = ResolveGuid();

            // De-register for post load event:
            Game.Instance.PostLoad -= PostLoad;
        }

        #endregion

        #region ISerializable Methods

        public DataBlobRef(SerializationInfo info, StreamingContext context)
        {
            // on de-serilaize we just want to cache the guid:
            _refOwnerGuid = (Guid)info.GetValue("_refOwnerGuid", typeof(Guid));
            RegisterPostLoad();
        }

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            // cache guid:
            if (Ref != null)
                _refOwnerGuid = Ref.EntityGuid;
            else
                _refOwnerGuid = Guid.Empty;                 // save out a empty guid to signial this reference is currently null

            // on serilize we just save out the guid:
            info.AddValue("_refOwnerGuid", _refOwnerGuid);
        }

        #endregion
    }

    /// <summary>
    /// Small factory to create DataBlobRefs without the need for specifiying the Datablob type all the time.
    /// </summary>
    public static class RefGenerator
    {
        public static DataBlobRef<T> MakeDataBlobRef<T>(T dataBlob) where T : BaseDataBlob
        {
            return new DataBlobRef<T>(dataBlob);
        }
    }
}
