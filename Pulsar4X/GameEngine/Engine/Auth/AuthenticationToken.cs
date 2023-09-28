using System;

namespace Pulsar4X.Engine.Auth
{
    public class AuthenticationToken
    {
        public Guid PlayerID { get; set; }
        public string Password { get; set; }

        public AuthenticationToken() { }

        public AuthenticationToken(Guid playerID, string password = "")
        {
            PlayerID = playerID;
            Password = password;
        }

        public AuthenticationToken(Player player, string password = "")
        {
            PlayerID = player.ID;
            Password = password;
        }
    }
}