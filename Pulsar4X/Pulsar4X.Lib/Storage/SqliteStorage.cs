using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Mime;
using System.Text;
using System.Data.SQLite;

namespace Pulsar4X.Storage
{
    //TODO: Add functionality for providing the folder to save the game into, to override default save folder
    class SqliteStorage
    {
        private const string DEFAULT_SAVED_GAMES_FOLDER = "SavedGames";

        /// <summary>
        /// Saves the current game GameState. Will overwrite a file with the same name.
        /// Will save to /SavedGames/[GameName]/[saveName].db
        /// </summary>
        /// <param name="gameState">GameState to be saved</param>
        /// <param name="applicationPath">Application path the game is running in (Application.StartupPath) </param>
        /// <param name="saveName">Name for the saved game file, if none is given, the name of the game will be used</param>
        public void Save(GameState gameState, string applicationPath, string saveName = null)
        {
            ValidateGameStateForSave(gameState);

            string dbFileName = string.Format("{0}.db", string.IsNullOrEmpty(saveName) ? gameState.Name : saveName);
            string path = Path.Combine(applicationPath, DEFAULT_SAVED_GAMES_FOLDER, gameState.Name);
            
            var cd = new CreateDatabase(path, dbFileName);
            cd.Save(gameState);
        }

        private void ValidateGameStateForSave(GameState gameState)
        {
            if (gameState == null) throw new ArgumentNullException("gameState", "Cannot save null game state.");
            if (string.IsNullOrEmpty(gameState.Name)) throw new ArgumentException("gameState.Name must not be null or empty");
        }

        /// <summary>
        /// Saves the current game GameState to the autosave folder
        /// /SavedGames/[GameName]/AutoSave/AutoSave-[GameDate].db
        /// </summary>
        /// <param name="gameState"></param>
        /// <param name="applicationPath"> </param>
        public void AutoSave(GameState gameState, string applicationPath)
        {
            ValidateGameStateForSave(gameState);

            var saveName = string.Format("AutoSave-{0}.db", gameState.Name);
            Save(gameState, applicationPath, saveName);
        }

        /// <summary>
        /// Load a saved game from the .db file at the given path. 
        /// </summary>
        /// <param name="fullPath">Full path including file name of the game file to be loaded</param>
        /// <returns>A GameState populated from the saved game file</returns>
        public GameState Load(string fullPath)
        {
            throw new NotImplementedException();
        }
    }
}
