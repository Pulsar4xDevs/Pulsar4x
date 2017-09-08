using System.Collections.Generic;
using System.Linq;

namespace Pulsar4X.ECSLib
{
    public class EntityVM
    {
        Game _game;
        private Entity _entity;
        public string Guid { get { return _entity.Guid.ToString(); } }
        public string EntityName { get; set; }

        Dictionary<ICreateViewmodel, IDBViewmodel> _viewmodelDict = new Dictionary<ICreateViewmodel, IDBViewmodel>();
        internal List<IDBViewmodel> Viewmodels = new List<IDBViewmodel>();

        public EntityVM(Game game, Entity entity)
        {
            _game = game;
            _entity = entity;

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


    public interface IDBViewmodel
    {
        void Update();
    }


    internal interface ICreateViewmodel
    {
        IDBViewmodel CreateVM(Game game);
    }
}
