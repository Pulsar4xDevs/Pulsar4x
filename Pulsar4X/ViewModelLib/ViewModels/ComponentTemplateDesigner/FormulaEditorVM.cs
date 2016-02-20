using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pulsar4X.ViewModel.ViewModels
{

    public class FormulaEditorVM : INotifyPropertyChanged
    {
        public string FunctionName { get; set; }
        public string Function { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;


        public void AddParam(int carrat, string param)
        {
            Function = Function.Insert(carrat, param);
        }
    }
}
