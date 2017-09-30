using System;
namespace Pulsar4X.ECSLib
{
    public class TranslationMoveVM : ViewModelBase, IDBViewmodel
    {

        TranslateMoveDB _tMoveDB;
        PositionDB _posDB;
        PropulsionDB _propDB;
        public double Xpos { get {return _posDB.AbsolutePosition.X; }}
        public double Ypos { get { return _posDB.AbsolutePosition.Y; } }
        public double Speed { get { return _propDB.CurrentSpeed.Length(); } }

        public DictionaryVM<Guid, string> TargetList { get; } = new DictionaryVM<Guid, string>();

        CommandReferences _cmdRef;

        public TranslationMoveVM()
        {
        }

        private void NewMoveOrder()
        {
            TranslateMoveCommand newmove = new TranslateMoveCommand()
            {
                RequestingFactionGuid = _cmdRef.EntityGuid,
                EntityCommandingGuid = _cmdRef.EntityGuid,
                CreatedDate = _cmdRef.GetSystemDatetime,
                TargetEntityGuid = TargetList.SelectedKey,
            };
        }

        public void Update()
        {
            OnPropertyChanged(nameof(Xpos));
            OnPropertyChanged(nameof(Ypos));
            OnPropertyChanged(nameof(Speed));
        }
    }
}
