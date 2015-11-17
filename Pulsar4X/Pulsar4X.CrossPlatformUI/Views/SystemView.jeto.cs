using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Collections.ObjectModel;
using Eto.Forms;
using Eto.Drawing;
using Eto.Serialization.Json;
using Pulsar4X.ViewModel;

namespace Pulsar4X.CrossPlatformUI.Views
{
    public class SystemView : Panel
    {
        protected ListBox Systems;
        protected SystemVM CurrentSystem;

        protected Splitter Body;

        protected GLSurface GLSurface;

        public SystemView(GameVM GameVM)
        {
            DataContext = GameVM;
            GLSurface = new GLSurface();
            JsonReader.Load(this);
            Systems.BindDataContext(c => c.DataStore, (GameVM c) => c.StarSystems);
            Systems.ItemTextBinding = Binding.Property((SystemVM vm) => vm.Name);
            Systems.ItemKeyBinding = Binding.Property((SystemVM vm) => vm.ID).Convert((Guid ID) => ID.ToString());
            Systems.SelectedIndexChanged += loadSystem;
            //var panel = new TableLayout();
            //panel.Rows.Add(new TableRow(new TableCell(GlContext)));
            Body.Panel2 = GLSurface;
        }

        public void loadSystem(object sender, EventArgs e)
        {
            CurrentSystem = (SystemVM)((ListBox)sender).SelectedValue;
        }
    }
}
