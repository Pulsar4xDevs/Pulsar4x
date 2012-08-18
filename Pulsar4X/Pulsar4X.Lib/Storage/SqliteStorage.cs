using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Mime;
using System.Text;
using System.Data.SQLite;

namespace Pulsar4X.Lib.Storage
{
    //TODO: Add functionality for providing the folder to save the game into, to override default save folder
    class SqliteStorage
    {
        private const string DEFAULT_SAVED_GAMES_FOLDER = "SavedGames";
        private const string CONNECTION_STRING = "Data Source={0};Version=3;";

        /// <summary>
        /// Saves the current game Model. Will overwrite a file with the same name.
        /// Will save to /SavedGames/[GameName]/[saveName].db
        /// </summary>
        /// <param name="gameState">Model to be saved</param>
        /// <param name="applicationPath">Application path the game is running in (Application.StartupPath) </param>
        /// <param name="saveName">Name for the saved game file, if none is given, the name of the game will be used</param>
        public void Save(Model gameState, string applicationPath, string saveName = null)
        {
            if (gameState == null) throw new ArgumentNullException("gameState", "Cannot save null game state.");
            if (string.IsNullOrEmpty(gameState.Name)) throw new ArgumentException("gameState.Name must not be null or empty");

            string dbFileName = string.Format("{0}.db", gameState.Name);
            string path = Path.Combine(applicationPath, DEFAULT_SAVED_GAMES_FOLDER, gameState.Name);
            string fullPathName = Path.Combine(path, dbFileName);

            if (Directory.Exists(path) == false)
                Directory.CreateDirectory(path);

            //remove the old file if there is one
            if (File.Exists(fullPathName))
            {
                File.Delete(fullPathName);
            }

            //create our new db file
            SQLiteConnection.CreateFile(fullPathName);

            using (var conn = new SQLiteConnection(string.Format(CONNECTION_STRING, fullPathName)))
            {
                //create tables for saved game
            }
        }

        /// <summary>
        /// Saves the current game Model to the autosave folder
        /// /SavedGames/[GameName]/AutoSave/AutoSave-[GameDate].db
        /// </summary>
        /// <param name="gameState"></param>
        public void AutoSave(Model gameState)
        {

        }

        /// <summary>
        /// Load a saved game from the .db file at the given path. 
        /// </summary>
        /// <param name="fullPath">Full path including file name of the game file to be loaded</param>
        /// <returns>A Model populated from the saved game file</returns>
        public Model Load(string fullPath)
        {
            throw new NotImplementedException();
        }
    }
}
