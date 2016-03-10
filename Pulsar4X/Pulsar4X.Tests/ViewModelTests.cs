using NUnit.Framework;
using Pulsar4X.ECSLib;
using System;
using System.Collections.Generic;
using System.Linq;
using Pulsar4X.ViewModel;

namespace Pulsar4X.Tests
{
    [TestFixture]
    [Description("ViewModel and Generic Game Tests")]
    class ViewModelTests
    {
        private Game _game;
        private GameVM _gameVM;
        private NewGameOptionsVM _newGameOptions;

        [Test]
        public void NewGame()
        {
            _gameVM = new GameVM();
            _newGameOptions = NewGameOptionsVM.Create(_gameVM);
            Assert.DoesNotThrow(() => _gameVM.CreateGame(_newGameOptions));
        }


    }
}
