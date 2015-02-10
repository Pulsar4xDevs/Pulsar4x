using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;

namespace Pulsar4X.Entities
{
    public class CommanderNameTheme
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public BindingList<NameEntry> NameEntries { get; set; }

        public CommanderNameTheme()
        {
            NameEntries = new BindingList<NameEntry>();
        }
    }

    public class NameEntry
    {
        public string Name { get; set; }
        public NamePosition NamePosition { get; set; }
        public bool IsFemale { get; set; }
    }

    public enum NamePosition
    {
        FirstName,
        LastName,
        Any
    }
}
