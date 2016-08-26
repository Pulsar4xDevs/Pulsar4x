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
        protected GridView GVevents;
        protected GridView GVTypeActions;
        //private LogViewerVM _vm;
        public LogViewer()
        {
            XamlReader.Load(this);
            #region GVevents setup
            GVevents.Columns.Add(new GridColumn
            {
                HeaderText = "Date Time",               
                DataCell = new TextBoxCell { Binding = Binding.Property<EventVM, DateTime>(r => r.Time).Convert(r => r.ToString()) }
            });
            GVevents.Columns.Add(new GridColumn
            {
                HeaderText = "System",
                DataCell = new TextBoxCell { Binding = Binding.Property<EventVM, string>(r => r.SystemName) }
            });
            GVevents.Columns.Add(new GridColumn
            {
                HeaderText = "Event Type",
                DataCell = new TextBoxCell { Binding = Binding.Property<EventVM, string>(r => r.EventTypeSsring) }
            });
            GVevents.Columns.Add(new GridColumn
            {
                HeaderText = "Entity",
                DataCell = new TextBoxCell { Binding = Binding.Property<EventVM, string>(r => r.EntityName) }
            });
            GVevents.Columns.Add(new GridColumn
            {
                HeaderText = "Message",
                DataCell = new TextBoxCell { Binding = Binding.Property<EventVM, string>(r => r.Message) }            
            });
            #endregion

            #region GVTypeAction setup
            GVTypeActions.Columns.Add(new GridColumn
            {
                HeaderText = "Event Type",
                DataCell = new TextBoxCell { Binding = Binding.Property<LogViewerVM.EventTypeBoolPair, EventType>(r => r.EventType).Convert(r => Enum.GetName(typeof(EventType), r)) }
            });

            GVTypeActions.Columns.Add(new GridColumn
            {
                HeaderText = "Halts",
                DataCell = new CheckBoxCell { Binding = Binding.Property<LogViewerVM.EventTypeBoolPair, bool?>(r => r.IsHalting) },
                Editable = true
                
            });
            #endregion

        }

    }
}
