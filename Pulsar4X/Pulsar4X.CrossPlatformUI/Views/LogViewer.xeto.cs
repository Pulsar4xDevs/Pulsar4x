using System;
using System.Collections.Generic;
using Eto.Forms;
using Eto.Drawing;
using Eto.Serialization.Xaml;
using Pulsar4X.ViewModel;
using Pulsar4X.ECSLib;

namespace Pulsar4X.CrossPlatformUI.Views
{
    public class LogViewer : Panel
    {
        //protected GridColumn GCdatetime;
        //protected GridColumn GCsys;
        //protected GridColumn GCtype;
        //protected GridColumn GChalts;
        //protected GridColumn GCentity;
        //protected GridColumn GCmsg;
        protected GridView GVevents;
        private LogViewerVM _vm;
        public LogViewer(LogViewerVM vm)
        {
            XamlReader.Load(this);
            _vm = vm;
            GVevents.DataStore = _vm.EventsDict;



            //GCdatetime.DataCell = new TextBoxCell { Binding = Binding.Property<Event, DateTime>(r => r.Time).Convert(r => r.ToString()) };
            ////GCsys.DataCell = new TextBoxCell { Binding = Binding.Property<Event, Event>(r => r).Convert(r => r.GetSystemName(_vm.Game, _vm.Auth)) };
            //GCtype.DataCell = new TextBoxCell { Binding = Binding.Property<Event, EventType>(r => r.EventType).Convert(r => Enum.GetName(typeof(EventType), r)) };
            ////GChalts.DataCell = new CheckBoxCell { Binding = Binding.Property<Event, bool>(r => r..ToString()) };
            //GCentity.DataCell = new TextBoxCell { Binding = Binding.Property<Event, Entity>(r => r.Entity).Convert(r=> r.ToString()) };
            //GCmsg.DataCell = new TextBoxCell { Binding = Binding.Property<Event, string>(r => r.Message) };


            GVevents.Columns.Add(new GridColumn
            {
                HeaderText = "Date Time",               
                DataCell = new TextBoxCell { Binding = Binding.Property<Event, DateTime>(r => r.Time).Convert(r => r.ToString()) }
            });
            GVevents.Columns.Add(new GridColumn
            {
                HeaderText = "System",
                //DataCell = new TextBoxCell { Binding = Binding.Property<Event, Event>(r => r).Convert(r => r.GetSystemName(_vm.Game, _vm.Auth)) };
            });
            GVevents.Columns.Add(new GridColumn
            {
                HeaderText = "Event Type",
                DataCell = new TextBoxCell { Binding = Binding.Property<Event, EventType>(r => r.EventType).Convert(r => Enum.GetName(typeof(EventType), r)) }
            });
            GVevents.Columns.Add(new GridColumn
            {
                HeaderText = "Halts",
                //DataCell = new CheckBoxCell { Binding = Binding.Property<Player, bool>(r => r.HaltsOnEvent[]) };
            });
            GVevents.Columns.Add(new GridColumn
            {
                HeaderText = "Entity",
                //DataCell = new TextBoxCell { Binding = Binding.Property<Event, Entity>(r => r.Entity).Convert(r => r.ToString()) }
            });
            GVevents.Columns.Add(new GridColumn
            {
                HeaderText = "Message",
                DataCell = new TextBoxCell { Binding = Binding.Property<Event, string>(r => r.Message) }            
            });

            DataContextChanged += LogViewer_DataContextChanged;
            vm.PropertyChanged += Vm_PropertyChanged;
            vm.EventsDict.CollectionChanged += EventsDict_CollectionChanged;
        }

        private void EventsDict_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            //throw new NotImplementedException();
        }

        private void Vm_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            //throw new NotImplementedException();
        }

        private void LogViewer_DataContextChanged(object sender, EventArgs e)
        {
            if (DataContext is LogViewerVM)
                _vm = (LogViewerVM)DataContext;
        }
    }
}
