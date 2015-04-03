using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using Pulsar4X.ECSLib.DataBlobs;

namespace Pulsar4X.ECSLib
{
    /// <summary>
    /// This is a Wrapper Class for DataBlob references. It will automaticaly be re-linked when the game is loaded from disk.
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
            set { _ref = value; }
        }
        private T _ref;
        
        // Cache for the referenced datablob owning entities Guid, so we can re-link the reference on post load.
        private Guid _refOwnerGuid;

        public DataBlobRef()
        {
            
        }

        /// <summary>
        /// Creates a DataBlob reference by looking up the datablob for the specified Entity.
        /// </summary>
        /// <param name="fromEntityGuid"> </param>
        public DataBlobRef(Guid fromEntityGuid)
        {
            _refOwnerGuid = fromEntityGuid;
            _ref = ResolveGuid();
        }

        /// <summary>
        /// Creates a DataBlob reference from the datablob provided.
        /// </summary>
        /// <param name="dataBlob"></param>
        public DataBlobRef(T dataBlob)
        {
            _ref = dataBlob;
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

        public override int GetHashCode()
        {
            return _ref.GetHashCode();
        }

        #endregion

        #region IPostLoad

        public void PostLoad()
        {
            // on post load we lookup the datablob by guid, thus re-linking the reference.
            _ref = ResolveGuid();
        }

        #endregion

        #region ISerializable Methods

        public DataBlobRef(SerializationInfo info, StreamingContext context) : this()
        {
            // on de-serilaize we just want to cache the guid:
            _refOwnerGuid = (Guid)info.GetValue("_refOwnerGuid", typeof(Guid));
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
}
