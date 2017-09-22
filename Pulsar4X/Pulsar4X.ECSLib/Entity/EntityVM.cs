using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Pulsar4X.ECSLib
{
    public class EntityVM : ViewModelBase
    {
        Game _game;
        private Entity _entity;
        private bool _hasEntity = false;
        public bool HasEntity 
        {   get {return _hasEntity;}
            set 
            {
                if(_hasEntity != value)
                {
                    _hasEntity = value;
                    if(_hasEntity) Update();

                }
                OnPropertyChanged();
            }
        }
        public string Guid { get { return _entity.Guid.ToString(); } }
        public string EntityName { get; set; }

        Dictionary<ICreateViewmodel, IDBViewmodel> _viewmodelDict = new Dictionary<ICreateViewmodel, IDBViewmodel>();
        public List<IDBViewmodel> Viewmodels = new List<IDBViewmodel>();

        public DictionaryVM<Guid, string> SelectableEntites { get; } = new DictionaryVM<Guid, string>();
        private Dictionary<Guid, Entity> _selectableEntitys { get; } = new Dictionary<Guid, Entity>();

        public EntityVM(GameVM gamevm) 
        {
            _game = gamevm.Game;
            if(gamevm.CurrentFaction != null && gamevm.CurrentFaction.IsValid)
                Init(gamevm.CurrentFaction);
        }

        public void Init(Entity faction)
        {
            var possibleEntites = faction.GetDataBlob<FactionOwnedEntitesDB>().OwnedEntites.Values;
            SelectableEntites.Clear();
            _selectableEntitys.Clear();
            foreach(var entity in possibleEntites)
            {
                string name = entity.GetDataBlob<NameDB>().DefaultName;
                SelectableEntites.Add(entity.Guid, name);
                _selectableEntitys.Add(entity.Guid, entity);
            }
            SelectableEntites.DisplayMode = DisplayMode.Value;
            SelectableEntites.SelectionChangedEvent += OnEntitySelected;
            SelectableEntites.SelectedIndex = 0;
            //HasEntity = true;
        }

        private void OnEntitySelected(int oldindex, int newindex)
        {
            Guid key = SelectableEntites.GetKey(newindex);
            _entity = _selectableEntitys[key];
            HasEntity = true;
        }

        internal void Update()
        {

            foreach(ICreateViewmodel datablob in _entity.DataBlobs.Where(item => item is ICreateViewmodel))
            {
                if(datablob is ICreateViewmodel &!_viewmodelDict.ContainsKey(datablob))
                {
                    var newvm = datablob.CreateVM(_game);
                    Viewmodels.Add(newvm);
                    _viewmodelDict.Add(datablob, newvm);
                }
            }
            foreach(var datablobAsKey in _viewmodelDict.Keys.ToArray())
            {
                if(!_entity.DataBlobs.Contains((BaseDataBlob)datablobAsKey))
                {
                    Viewmodels.Remove(_viewmodelDict[datablobAsKey]);
                    _viewmodelDict.Remove(datablobAsKey); 
                }
            }

            foreach(var viewmodel in Viewmodels)
            {
                viewmodel.Update();
            }
        }
    }

    public class EntityNameVM
    {
        private Entity _entity;
        internal Entity GetEntity { get { return _entity; } }
        internal System.Guid GetEntityGuid {get {return _entity.Guid;}}
        public string Name { get { return _entity.GetDataBlob<NameDB>().DefaultName; } }
        internal EntityNameVM(Entity entity)
        { _entity = entity; }

    }

    public interface IDBViewmodel
    {
        void Update();
    }


    internal interface ICreateViewmodel
    {
        IDBViewmodel CreateVM(Game game);
    }
}
