using System;
using System.Collections.Generic;
using System.Windows.Input;

namespace Pulsar4X.ECSLib
{
    public class TranslationMoveVM : ViewModelBase, IDBViewmodel
    {

        WarpMovingDB _tMoveDB;
        PositionDB _posDB;
        PropulsionAbilityDB _propDB;
        public double Xpos { get {return _posDB.AbsolutePosition_AU.X; }}
        public double Ypos { get { return _posDB.AbsolutePosition_AU.Y; }}
        public double Speed { get { return _propDB.CurrentVectorMS.Length(); }}

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
            _tMoveDB = entity.GetDataBlob<WarpMovingDB>();
            _posDB = entity.GetDataBlob<PositionDB>();
            _propDB = entity.GetDataBlob<PropulsionAbilityDB>();
            _cmdRef = cmdRef;
            TargetList.SelectionChangedEvent += OnTargetSelectonChange;
            Entity faction;
            entity.Manager.FindEntityByGuid(entity.FactionOwner, out faction);
            UpdateTargetList(faction, entity.Manager);
        }

        void UpdateTargetList(Entity faction, EntityManager manager)
        {
            var ownedEntites = manager.GetEntitiesByFaction(faction.Guid);
            foreach (var entityItem in ownedEntites)//entity.Manager.GetAllEntitiesWithDataBlob<PositionDB>())
            {
                if (entityItem.HasDataBlob<PositionDB>() && entityItem.HasDataBlob<NameDB>())
                {
                    TargetList.Add(entityItem.Guid, entityItem.GetDataBlob<NameDB>().GetName(faction.Guid));
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

        /*
        private ICommand _orbitCommand;
        public ICommand OrbitCommand
        {
            get
            {
                return _orbitCommand ?? (_orbitCommand = new CommandHandler(OnOrbitCommand, true));
            }
        }
        */
        private ICommand _moveCommand;
        public ICommand MoveCommand
        {
            get
            {
                return _moveCommand ?? (_moveCommand = new CommandHandler(OnMoveCommand, true));
            }
        }

        /*
        private void OnOrbitCommand()
        {
            OrbitBodyCommand newmove = new OrbitBodyCommand()
            {
                RequestingFactionGuid = _cmdRef.FactionGuid,
                EntityCommandingGuid = _cmdRef.EntityGuid,
                CreatedDate = _cmdRef.GetSystemDatetime,
                TargetEntityGuid = TargetList.SelectedKey,
                ApoapsisInKM = this.Range,
                PeriapsisInKM = this.Perihelion,
            };
            _cmdRef.Handler.HandleOrder(newmove);
        }*/

        private void OnMoveCommand()
        {
            TranslateMoveCommand newmove = new TranslateMoveCommand()
            {
                RequestingFactionGuid = _cmdRef.FactionGuid,
                EntityCommandingGuid = _cmdRef.EntityGuid,
                CreatedDate = _cmdRef.GetSystemDatetime,
                TargetEntityGuid = TargetList.SelectedKey,
                RangeInKM = Range,
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
