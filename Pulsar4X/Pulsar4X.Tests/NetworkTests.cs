using NUnit.Framework;
using Pulsar4X.ECSLib;
using System;

namespace Pulsar4X.Tests
{
    [Description("Network Tests")]
    public class NetworkTests
    {
        TestGame _host;

        Game _client;
        Networking.NetworkClient _netClient;
        private DateTime _currentDateTime { get { return _host.Game.CurrentDateTime; } }



        [SetUp]
        public void Init()
        {
            _host = new TestGame(1);
            _host.Game.OrderHandler = new ServerOrderHandler(_host.Game, 4888);

            _client = new Game();
            _netClient = new Networking.NetworkClient("localhost", 4888, new GameVM());//ugly, refactor so GameVM isn't required and or network isnt needed to be created here?
            _client.OrderHandler = new ClientOrderHandler(_client, _netClient); 
        }

        [Test] 
        public void TestFactionConnect()
        {
            _netClient.ClientConnect();
            //TODO: set up events so we can listen for updates. 
            //_netClient.Messages.Contains(
            //_netClient.SendFactionDataRequest("New Terran Utopian Empire", "");//name is hardcoded in TestGame.cs
        }
    }
}
