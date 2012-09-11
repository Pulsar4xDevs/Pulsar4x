using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Pulsar4X.Storage;

namespace Pulsar4X.Entities
{
    public class CommanderNameThemes
    {
        private static readonly CommanderNameThemes _instance = new CommanderNameThemes();
        public static CommanderNameThemes Instance { get { return _instance; } }

        public List<CommanderNameTheme> NameThemes { get; set; } 

        static CommanderNameThemes()
        {
        }
        private CommanderNameThemes()
        {
            NameThemes = new List<CommanderNameTheme>();
            var bootstrap = new Bootstrap();
            NameThemes.AddRange(bootstrap.LoadCommanderNameTheme());
        }

        
    }

    
}
