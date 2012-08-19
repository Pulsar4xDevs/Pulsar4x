using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Pulsar4X.Storage;
using Pulsar4X.Entities;

namespace Pulsar4X.Tests
{
    [TestFixture]
    public class StorageTests
    {
        private GameState _model;
        private CreateDatabase _createDatabase;

        private string _appPath;
        private string _saveFolder;
        private const string SAVE_GAME_FILE_NAME = "TestGame.db";

        [SetUp]
        public void TestSetup()
        {
            _model = new GameState();
            _model.Name = "Test Game";

            UriBuilder uri = new UriBuilder(System.Reflection.Assembly.GetExecutingAssembly().CodeBase);
            _appPath = Path.GetDirectoryName(Uri.UnescapeDataString(uri.Path));
            _saveFolder = Path.Combine(_appPath, "Test");
            _createDatabase = new CreateDatabase(_saveFolder, SAVE_GAME_FILE_NAME);
        }

        [Test]
        public void Create_Saved_Game_DB_File()
        {
            _createDatabase.CreateSaveFile();

            Console.WriteLine(_saveFolder);
            Assert.IsTrue(File.Exists(Path.Combine(_saveFolder, SAVE_GAME_FILE_NAME)));
        }

        [Test]
        public void Create_Saved_Game_DB_Tables()
        {
            _createDatabase.CreateSaveFile();
            _createDatabase.CreateTables();
        }

    }
}
