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
        Unused1         = 256,
        Unused2         = 512,
        Unused3         = 1024,
        Unused4         = 2048,
        Unused5         = 4096,
        Unused6         = 8192,
        Unused7         = 16384,
        Unused8         = 32768,
        Unused9         = 65536,
        Unused10        = 131072,
        Unused11        = 262144,
        Unused12        = 524288,
        Unused13        = 1048576,
        Unused14        = 2097152,
        Unused15        = 4194304,
        Unused16        = 8388608,
        Unused17        = 16777216,
        Unused18        = 33554432,
        Unused19        = 67108864,
        Unused20        = 134217728,
        Unused21        = 268435456,
        Unused22        = 536870912,
            FullAccess      = 1073741823,   // Player can do anything with this faction, except edit the FullAccess players.
        EditFullAccess  = 1073741824,
            Owner           = 2147483647,   // Player can do anything with this faction, except edit the Owner players.
        EditOwners      = 2147483648,
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
        #region Properties

        [JsonProperty]
        public Guid ID { get; }

        [JsonProperty]
        public string Name { get; private set; }

        [JsonProperty]
        private Dictionary<ProtoEntity, AccessRole> FactionAccessRoles { get; }

        [JsonProperty]
        private string PasswordHash { get; set; }

        [JsonProperty]
        private string Salt { get; set; }

        #endregion

        #region Constructors

        [JsonConstructor]
        [UsedImplicitly]
        private Player()
        { }

        internal Player(string name, string password = "") : this(name, password, Guid.NewGuid())
        { }

        internal Player(string name, string password, Guid id) : this(name, password, id, new Dictionary<ProtoEntity, AccessRole>())
        { }

        internal Player(string name, string password, Guid id, Dictionary<ProtoEntity, AccessRole> factionAccessRoles)
        {
            ID = id;
            Name = string.IsNullOrEmpty(name) ? "Unnamed Player" : name;
            FactionAccessRoles = factionAccessRoles;
            Salt = GenerateSalt();
            PasswordHash = GeneratePasswordHash(password, Salt);
        }

        #endregion

        #region Internal API

        internal AccessRole GetAccess(ProtoEntity faction)
        {
            AccessRole role;
            FactionAccessRoles.TryGetValue(faction, out role);
            return role;
        }

        internal void SetAccess(ProtoEntity faction, AccessRole accessRole)
        {
            if (FactionAccessRoles.ContainsKey(faction))
            {
                FactionAccessRoles[faction] = accessRole;
            }
            else
            {
                FactionAccessRoles.Add(faction, accessRole);
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
        /// <param name="authToken">Current AuthenticationToken for this player.</param>
        /// <param name="newName">New name for the player</param>
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
        /// <param name="authToken">Current AuthenticationToken for this player.</param>
        /// <param name="newPassword">New password for the player.</param>
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

        #endregion

        #region Private Functions

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
            var rng = new RNGCryptoServiceProvider();
            var buff = new byte[16];
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

            return new SHA256Managed().ComputeHash(saltedValue);
        }

        #endregion

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
            return obj.GetType() == GetType() && Equals((Player)obj);
        }

        public override int GetHashCode()
        {
            return ID.GetHashCode();
        }

        public static bool operator ==(Player playerA, Player playerB)
        {
            return Equals(playerA, playerB);
        }

        public static bool operator !=(Player playerA, Player playerB)
        {
            return !Equals(playerA, playerB);
        }

        #endregion

        public override string ToString()
        {
            return ID.ToString();
        }

        #endregion
        
    }
}
