using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;

namespace Pulsar4X.ECSLib
{
    [Flags]
    public enum AccessRole : uint
    {
        None            = 0,            // Player can't do anything with this faction.
        ColonyVision    = 1,            // UNUSED - EXAMPLE ONLY Player can see locations of colonies.
        UnitVision      = 2,            // UNUSED - EXAMPLE ONLY Player can see locations of units.
        SensorVision    = 4,            // UNUSED - EXAMPLE ONLY Player can see sensor data.
        IssueOrders     = 8,            // UNUSED - EXAMPLE ONLY Player can issue orders to units.
        ManageIndustry  = 16,           // UNUSED - EXAMPLE ONLY Player can manage colony industry.
        ManageShipyards = 32,           // UNUSED - EXAMPLE ONLY Player can manage shipyards.
        ManageResearch  = 64,           // UNUSED - EXAMPLE ONLY Player can manage research projects.
        ManageTeams     = 128,          // UNUSED - EXAMPLE ONLY Player can manage teams.

        FullAccess      = 1073741823,   // Player can do anything with this faction, except remove Owners/SM.
        Owner           = 2147483647,   // Player can do anything with this faction, except remove SM.
        SM              = 4294967295    // Player can do anything with this faction.
    }
    
    public class AuthenticationToken
    {
        public Guid PlayerID { get; set; }
        public string Password { get; set; }
    }

    [JsonObject(MemberSerialization.OptIn)]
    public class Player
    {
        [JsonProperty]
        public Guid ID { get; }

        [JsonProperty]
        public string Name { get; set; }
        
        [JsonProperty]
        private readonly Dictionary<ProtoEntity, AccessRole> _factionAccessRoles;

        [JsonProperty]
        private string PasswordHash { get; }

        [JsonProperty]
        private string Salt { get; }

        [JsonConstructor]
        private Player()
        { }

        public Player(string name, string password) : this(name, password, Guid.NewGuid())
        { }

        public Player(string name, string password, Guid id) : this(name, password, id, new Dictionary<ProtoEntity, AccessRole>())
        { }

        public Player(string name, string password, Guid id, Dictionary<ProtoEntity, AccessRole> factionAccessRoles)
        {
            ID = id;
            Name = name;
            _factionAccessRoles = factionAccessRoles;
            Salt = GenerateSalt();
            PasswordHash = Convert.ToBase64String(Hash(password, Salt));
        }
        
        internal AccessRole GetAccess(ProtoEntity faction)
        {
            AccessRole role;
            _factionAccessRoles.TryGetValue(faction, out role);
            return role;
        }

        internal void SetAccess(ProtoEntity faction, AccessRole accessRole)
        {
            if (_factionAccessRoles.ContainsKey(faction))
            {
                _factionAccessRoles[faction] = accessRole;
            }
            else
            {
                _factionAccessRoles.Add(faction, accessRole);
            }
        }

        internal bool ConfirmPassword(string password)
        {
            byte[] passwordHash = Hash(password, Salt);

            return Convert.FromBase64String(PasswordHash).SequenceEqual(passwordHash);
        }

        private static string GenerateSalt()
        {
            var rng = new RNGCryptoServiceProvider();
            var buff = new byte[16];
            rng.GetBytes(buff);

            // Return a Base64 string representation of the random number.
            return Convert.ToBase64String(buff);
        }

        private static byte[] Hash(string value, string salt)
        {
            return Hash(Encoding.UTF8.GetBytes(value), Convert.FromBase64String(salt));
        }

        private static byte[] Hash(byte[] value, byte[] salt)
        {
            byte[] saltedValue = salt.Concat(value).ToArray();

            return new SHA256Managed().ComputeHash(saltedValue);
        }

        #region Equality Members

        protected bool Equals(Player other)
        {
            return ID.Equals(other.ID);
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
            if (obj.GetType() != GetType())
            {
                return false;
            }
            return Equals((Player)obj);
        }

        public override int GetHashCode()
        {
            return ID.GetHashCode();
        }

        #endregion

    }
}
