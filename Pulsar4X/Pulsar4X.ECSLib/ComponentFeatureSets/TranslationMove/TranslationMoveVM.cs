using System;
using System.Collections.Generic;
using System.Windows.Input;

namespace Pulsar4X.ECSLib
{
    public class TranslationMoveVM : ViewModelBase, IDBViewmodel
    {

        TranslateMoveDB _tMoveDB;
        PositionDB _posDB;
        PropulsionDB _propDB;
        public double Xpos { get {return _posDB.AbsolutePosition.X; }}
        public double Ypos { get { return _posDB.AbsolutePosition.Y; }}
        public double Speed { get { return _propDB.CurrentSpeed.Length(); }}

        Entity _selectedEntity { get { return _targetDict[TargetList.SelectedKey]; } }
        private Dictionary<Guid, Entity> _targetDict = new Dictionary<Guid, Entity>();
        public DictionaryVM<Guid, string> TargetList { get; } = new DictionaryVM<Guid, string>(DisplayMode.Value);

        CommandReferences _cmdRef;

        public bool CanOrbitSelected { get; private set; } = false;
        public bool CanMatchSelected { get; private set; } = false;

        public double Range { get; set; } = 20000;
        public double Perihelion { get; set; } = 10000;

        public TranslationMoveVM(Game game, CommandReferences cmdRef, Entity entity)
        {
            _tMoveDB = entity.GetDataBlob<TranslateMoveDB>();
            _posDB = entity.GetDataBlob<PositionDB>();
            _propDB = entity.GetDataBlob<PropulsionDB>();
            _cmdRef = cmdRef;
            TargetList.SelectionChangedEvent += OnTargetSelectonChange;
            foreach (var entityItem in entity.Manager.GetAllEntitiesWithDataBlob<PositionDB>())
            {
                if (entityItem.HasDataBlob<NameDB>())
                {
                    TargetList.Add(entityItem.Guid, entityItem.GetDataBlob<NameDB>().DefaultName);
                    _targetDict.Add(entityItem.Guid, entityItem);
                }
            }
        }

        private void OnTargetSelectonChange(int oldSelection, int newSelection)
        {
            if (_selectedEntity.HasDataBlob<MassVolumeDB>())
            {
                MassVolumeDB massdb = _selectedEntity.GetDataBlob<MassVolumeDB>();
                if (massdb.Mass >= 1.5E15)
                    CanOrbitSelected = true; 
                else
                    CanOrbitSelected = false;
                OnPropertyChanged(nameof(CanOrbitSelected));
            }
            CanMatchSelected = _selectedEntity.HasDataBlob<OrbitDB>();
            OnPropertyChanged(nameof(CanMatchSelected));
        }

        private ICommand _orbitCommand;
        public ICommand OrbitCommand
        {
            get
            {
                return _orbitCommand ?? (_orbitCommand = new CommandHandler(OnOrbitCommand, true));
            }
        }

        private ICommand _moveCommand;
        public ICommand MoveCommand
        {
            get
            {
                return _moveCommand ?? (_moveCommand = new CommandHandler(OnMoveCommand, true));
            }
        }

        private void OnOrbitCommand()
        {
            OrbitBodyCommand newmove = new OrbitBodyCommand()
            {
                RequestingFactionGuid = _cmdRef.FactionGuid,
                EntityCommandingGuid = _cmdRef.EntityGuid,
                CreatedDate = _cmdRef.GetSystemDatetime,
                TargetEntityGuid = TargetList.SelectedKey,
                ApihelionInKM = this.Range,
                PerhelionInKM = this.Perihelion,
            };
            _cmdRef.Handler.HandleOrder(newmove);
        }

        private void OnMoveCommand()
        {
            TranslateMoveCommand newmove = new TranslateMoveCommand()
            {
                RequestingFactionGuid = _cmdRef.FactionGuid,
                EntityCommandingGuid = _cmdRef.EntityGuid,
                CreatedDate = _cmdRef.GetSystemDatetime,
                TargetEntityGuid = TargetList.SelectedKey,
            };
            _cmdRef.Handler.HandleOrder(newmove);
        }

        public void Update()
        {
            OnPropertyChanged(nameof(Xpos));
            OnPropertyChanged(nameof(Ypos));
            OnPropertyChanged(nameof(Speed));

        }
    }
}
