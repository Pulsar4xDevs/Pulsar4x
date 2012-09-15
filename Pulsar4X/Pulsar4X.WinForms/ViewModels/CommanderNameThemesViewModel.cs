using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Windows.Forms;
using Pulsar4X.Entities;
using Pulsar4X.Storage;

namespace Pulsar4X.ViewModels
{
    public class CommanderNameThemesViewModel : INotifyPropertyChanged
    {
        private BindingList<CommanderNameTheme> _nameThemes;
        public BindingList<CommanderNameTheme> NameThemes
        {
            get { return _nameThemes; }
            set
            {
                _nameThemes = value;
                OnPropertyChanged(() => NameThemes);
                CurrentTheme = NameThemes.FirstOrDefault();
            }
        }

        private CommanderNameTheme _currentTheme;
        public CommanderNameTheme CurrentTheme
        {
            get { return _currentTheme; }
            set
            {
                _currentTheme = value;
                OnPropertyChanged(() => CurrentTheme);
                if (_currentTheme != null)
                {
                    CurrentThemeName = _currentTheme.Name;
                    NameEntryBindingSource.DataSource = CurrentTheme.NameEntries;
                }
            }
        }

        private string _currentThemeName;
        public string CurrentThemeName
        {
            get { return _currentThemeName; }
            set
            {
                _currentThemeName = value;
                OnPropertyChanged(() => CurrentThemeName);
            }
        }

        private BindingSource _nameEntryBindingSource;
        public BindingSource NameEntryBindingSource
        {
            get
            {
                if (_nameEntryBindingSource == null)
                    _nameEntryBindingSource = new BindingSource();
                return _nameEntryBindingSource;
            }
            set { _nameEntryBindingSource = value; }
        }

        public CommanderNameThemesViewModel()
        {
            NameThemes = new BindingList<CommanderNameTheme>(CommanderNameThemes.Instance.NameThemes.Values.ToList());

        }

        private void OnPropertyChanged(Expression<Func<object>> property)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this,
                    new PropertyChangedEventArgs(BindingHelper.Name(property)));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public CommanderNameTheme AddNewTheme(string name)
        {
            var theme = new CommanderNameTheme()
            {
                Id = Guid.NewGuid(),
                Name = name
            };
            CommanderNameThemes.Instance.NameThemes.Add(theme.Id, theme);

            //update binding list
            NameThemes.Add(theme);

            CurrentTheme = theme;
            return theme;
        }

        public void SaveTheme()
        {
            var bootstrap = new Bootstrap();
            bootstrap.SaveCommanderNameTheme(CommanderNameThemes.Instance.NameThemes);
        }
    }
}
