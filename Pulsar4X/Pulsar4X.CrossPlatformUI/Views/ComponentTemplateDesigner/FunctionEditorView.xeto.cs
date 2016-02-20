using System;
using System.Collections.Generic;
using Eto.Forms;
using Eto.Drawing;
using Eto.Serialization.Xaml;

namespace Pulsar4X.CrossPlatformUI.Views.ComponentTemplateDesigner
{
    public class FormulaEditorView : Panel
    {
        public FormulaEditorView()
        {
            XamlReader.Load(this);
        }
    }
}
