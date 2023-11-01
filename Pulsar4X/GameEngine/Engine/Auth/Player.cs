using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.Serialization;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;
using Pulsar4X.Engine.Events;

namespace Pulsar4X.Engine.Auth
{
    [JsonObject(MemberSerialization.OptIn)]
    public class Player : ISerializable
    {
        #region Properties
        [PublicAPI]
        [JsonProperty]
        public Guid ID { get; protected set; }

        [PublicAPI]
        [JsonProperty]
        public string Name { get; protected set; }

        //[JsonProperty]
        //public OrderQueue Orders;

        [JsonProperty]
        private Dictionary<int, uint> FactionAccessRoles { get; set; }
        internal ReadOnlyDictionary<int, AccessRole> AccessRoles => new ReadOnlyDictionary<int, AccessRole>(FactionAccessRoles.ToDictionary(kvp => kvp.Key, kvp => (AccessRole)kvp.Value));

        [JsonProperty]
        private string PasswordHash { get; set; }

        [JsonProperty]
        private string Salt { get; set; }

        [JsonProperty]
        public Dictionary<EventType, bool> HaltsOnEvent { get; } = new Dictionary<EventType, bool>();

        #endregion

        #region Constructors

        // public Player(SerializationInfo info, StreamingContext context)
        // {
        //     ID = (Guid)info.GetValue(nameof(ID), typeof(Guid));
        //     Name = info.GetString(nameof(Name));

        //     if (context.State != StreamingContextStates.Persistence)
        //     {
        //         return;
        //     }

        //     PasswordHash = info.GetString(nameof(PasswordHash));
        //     Salt = info.GetString(nameof(Salt));
        //     FactionAccessRoles = (Dictionary<int, uint>)info.GetValue(nameof(FactionAccessRoles), typeof(Dictionary<int, uint>));
        //     //Orders = new OrderQueue();
        //     HaltsOnEvent = (Dictionary<EventType, bool>)info.GetValue(nameof(HaltsOnEvent), typeof(Dictionary<EventType, bool>));
        // }

        internal Player(string name, string password = "") : this(name, password, Guid.NewGuid())
        { }

        internal Player(string name, string password, Guid id) : this(name, password, id, new Dictionary<int, uint>())
        { }

        internal Player(string name, string password, Guid id, Dictionary<int, uint> factionAccessRoles)
        {
            ID = id;
            Name = string.IsNullOrEmpty(name) ? "Unnamed Player" : name;
            FactionAccessRoles = factionAccessRoles;
            Salt = GenerateSalt();
            PasswordHash = GeneratePasswordHash(password, Salt);
            //Orders = new OrderQueue();


        }

        #endregion

        #region Internal API

        internal AccessRole GetAccess(int factionId)
        {
            uint role;
            FactionAccessRoles.TryGetValue(factionId, out role);
            return (AccessRole)role;
        }

        internal void SetAccess(int factionId, AccessRole accessRole)
        {
            if (FactionAccessRoles.ContainsKey(factionId))
            {
                FactionAccessRoles[factionId] = (uint)accessRole;
            }
            else
            {
                FactionAccessRoles.Add(factionId, (uint)accessRole);
            }
        }

        internal bool IsTokenValid(AuthenticationToken authToken)
        {
            return authToken != null && ID == authToken.PlayerID && ConfirmPassword(authToken.Password);
        }

        #endregion

        #region Public API

        /// <summary>
        /// Changes the name of this player.
        /// </summary>
        /// <returns>True if operation is successful.</returns>
        [PublicAPI]
        public bool ChangeName([NotNull] AuthenticationToken authToken, [NotNull] string newName)
        {
            if (string.IsNullOrEmpty(newName))
            {
                throw new ArgumentException("Argument is null or empty", nameof(newName));
            }

            if (!IsTokenValid(authToken))
            {
                return false;
            }

            Name = newName;
            return true;
        }

        /// <summary>
        /// Changes the password of this player.
        /// </summary>
        /// <returns>True if operation is successful.</returns>
        [PublicAPI]
        public bool ChangePassword([NotNull] AuthenticationToken authToken, [NotNull] string newPassword)
        {
            if (!IsTokenValid(authToken))
            {
                return false;
            }

            Salt = GenerateSalt();
            PasswordHash = GeneratePasswordHash(newPassword, Salt);
            return true;
        }

        /// <summary>
        /// Gets all the access roles of this player.
        /// </summary>
        /// <returns>ReadOnlyDictionary containing the access roles.</returns>
        [PublicAPI]
        [Pure]
        public ReadOnlyDictionary<int, AccessRole> GetAccessRoles(AuthenticationToken authToken)
        {
            return !IsTokenValid(authToken) ? new ReadOnlyDictionary<int, AccessRole>(new Dictionary<int, AccessRole>()) : AccessRoles;
        }

        /// <summary>
        /// Retrieves the AccessRole this player has over the specified faction.
        /// </summary>
        [PublicAPI]
        public AccessRole GetAccess(AuthenticationToken authToken, int factionId)
        {
            var role = AccessRole.None;
            if (IsTokenValid(authToken))
            {
                role = GetAccess(factionId);
            }
            return role;
        }

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue(nameof(ID), ID);
            info.AddValue(nameof(Name), Name);

            if (context.State != StreamingContextStates.Persistence)
            {
                return;
            }

            info.AddValue(nameof(PasswordHash), PasswordHash);
            info.AddValue(nameof(Salt), Salt);
            info.AddValue(nameof(FactionAccessRoles), FactionAccessRoles);
            info.AddValue(nameof(HaltsOnEvent), HaltsOnEvent);
        }
        /*
        public void ProcessOrders()
        {
            int orders = Orders.Length();

            while (orders > 0)
            {
                BaseOrder nextOrder = Orders.ProcessOrder();
                // Process all the orders
                //@todo - finish
                if (nextOrder != null)
                {
                    Entity owner = nextOrder.Owner;
                    owner.GetDataBlob<ShipInfoDB>().Orders.Enqueue(nextOrder);

                }
                else
                    return;
            }
        }

        public void ClearOrders()
        {
            Orders.ClearOrders();
        }
*/
        #endregion

        #region Private Functions

        #region Equality Members

        protected bool Equals(Player? other)
        {
            return ID.Equals(other.ID);
        }

        public override bool Equals(object? obj)
        {
            if(obj == null) return false;
            if (ReferenceEquals(null, obj))
            {
                return false;
            }
            if (ReferenceEquals(this, obj))
            {
                return true;
            }
            return obj.GetType() == GetType() && Equals((Player)obj);
        }

        public override int GetHashCode()
        {
            return ID.GetHashCode();
        }

        public static bool operator ==(Player? playerA, Player? playerB)
        {
            if(playerA is null && playerB is null) return true;
            if(playerA is null || playerB is null) return false;
            return Equals(playerA, playerB);
        }

        public static bool operator !=(Player playerA, Player playerB)
        {
            return !Equals(playerA, playerB);
        }

        #endregion

        public override string ToString()
        {
            return $"{Name} {ID}";
        }

        #region Crypto Functions

        private bool ConfirmPassword(string password)
        {
            byte[] passwordHash = Hash(password, Salt);

            return Convert.FromBase64String(PasswordHash).SequenceEqual(passwordHash);
        }

        private static string GeneratePasswordHash([NotNull] string password, string salt)
        {
            return Convert.ToBase64String(Hash(password, salt));
        }

        private static string GenerateSalt()
        {
            var rng = RandomNumberGenerator.Create();
            var buff = new byte[12];
            rng.GetBytes(buff);

            // Return a Base64 string representation of the random number.
            return Convert.ToBase64String(buff);
        }

        private static byte[] Hash(string value, string salt)
        {
            if (value == null)
            {
                value = "";
            }
            return Hash(Encoding.UTF8.GetBytes(value), Convert.FromBase64String(salt));
        }

        private static byte[] Hash(byte[] value, byte[] salt)
        {
            byte[] saltedValue = salt.Concat(value).ToArray();
            var sha256 = SHA256.Create();

            return sha256.ComputeHash(saltedValue);
        }

        #endregion

        #endregion

    }
}