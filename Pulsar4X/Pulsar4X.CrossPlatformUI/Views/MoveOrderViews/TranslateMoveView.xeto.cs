using System;
using System.Collections.Generic;
using Eto.Forms;
using Eto.Drawing;
using Eto.Serialization.Xaml;
using Pulsar4X.ECSLib;
namespace Pulsar4X.CrossPlatformUI.Views.MoveOrderViews
{

    public class TranslateMoveView : TableLayout
    {
      
        protected ComboBox TargetSelection;

        public TranslateMoveView()
        {
            XamlReader.Load(this);
            TargetSelection.BindDataContext(c => c.DataStore, (DictionaryVM<Guid, string> m) => m.DisplayList);
            TargetSelection.SelectedIndexBinding.BindDataContext((DictionaryVM<Guid, string> m) => m.SelectedIndex);
            DataContextChanged += OnDataContextChanged;
        }

        private void OnDataContextChanged(object sender, EventArgs e)
        {
            if (DataContext is TranslationMoveVM dc)
            {
                
            }
        }

    }
}