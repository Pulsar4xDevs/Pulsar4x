using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pulsar4X.ECSLib.ComponentFeatureSets.CargoStorage
{
    public interface ICargoDefinitionsLibrary
    {
        void LoadDefinitions(List<MineralSD> minerals,
            List<ProcessedMaterialSD> processedMaterials,
            List<ICargoable> otherCargo);

        void LoadMineralDefinitions(List<MineralSD> minerals);
        void LoadMaterialsDefinitions(List<ProcessedMaterialSD> materials);
        void LoadOtherDefinitions(List<ICargoable> otherCargo);

        Dictionary<Guid, ICargoable> GetAll();

        ICargoable GetAny(Guid id);

        bool IsOther(Guid id);
        ICargoable GetOther(string nameOfCargo);
        ICargoable GetOther(Guid guidOfCargo);

        bool IsMineral(Guid id);
        MineralSD GetMineral(string name);
        MineralSD GetMineral(Guid guid);
        Dictionary<Guid, MineralSD> GetMinerals();
        List<MineralSD> GetMineralsList();

        bool IsMaterial(Guid id);
        ProcessedMaterialSD GetMaterial(string name);
        ProcessedMaterialSD GetMaterial(Guid guid);
        Dictionary<Guid, ProcessedMaterialSD> GetMaterials();
        List<ProcessedMaterialSD> GetMaterialsList();
    }

    public class CargoDefinitionsLibrary : ICargoDefinitionsLibrary
    {
        private Dictionary<Guid, ICargoable> _definitions;
        private Dictionary<Guid, MineralSD> _minerals;
        private Dictionary<Guid, ProcessedMaterialSD> _processedMaterials;

        public CargoDefinitionsLibrary() : this(new List<MineralSD>(),
            new List<ProcessedMaterialSD>(),
            new List<ICargoable>())
        {
        }

        public CargoDefinitionsLibrary(List<MineralSD> minerals,
            List<ProcessedMaterialSD> processedMaterials,
            List<ICargoable> otherCargo)
        {
            _definitions = new Dictionary<Guid, ICargoable>();
            _minerals = new Dictionary<Guid, MineralSD>();
            _processedMaterials = new Dictionary<Guid, ProcessedMaterialSD>();

            LoadDefinitions(minerals, processedMaterials, otherCargo);
        }

        
        public void LoadDefinitions(List<MineralSD> minerals,
            List<ProcessedMaterialSD> processedMaterials,
            List<ICargoable> otherCargo)
        {
            LoadMineralDefinitions(minerals);
            LoadMaterialsDefinitions(processedMaterials);
            LoadOtherDefinitions(otherCargo);
        }

        public void LoadMineralDefinitions(List<MineralSD> minerals)
        {
            if (minerals != null)
            {
                foreach (var entry in minerals)
                {
                    _definitions[entry.ID] = entry;
                    _minerals[entry.ID] = entry;
                }
            }
        }

        public void LoadMaterialsDefinitions(List<ProcessedMaterialSD> materials)
        {
            if (materials != null)
            {
                foreach (var entry in materials)
                {
                    _definitions[entry.ID] = entry;
                    _processedMaterials[entry.ID] = entry;
                }
            }
        }

        public void LoadOtherDefinitions(List<ICargoable> otherCargo)
        {
            if (otherCargo != null)
            {
                foreach (var entry in otherCargo)
                {
                    _definitions[entry.ID] = entry;
                }
            }
        }

        public ICargoable GetAny(Guid id)
        {
            if (_minerals.ContainsKey(id))
                return _minerals[id];

            if (_processedMaterials.ContainsKey(id))
                return _processedMaterials[id];

            if (_definitions.ContainsKey(id))
                return _definitions[id];

            return null;
        }

        public Dictionary<Guid, ICargoable> GetAll()
        {
            return _definitions;
        }


        public bool IsOther(Guid id)
        {
            return _definitions.ContainsKey(id) && (IsMineral(id) == false) && (IsMaterial(id) == false);
        }

        public ICargoable GetOther(string nameOfCargo)
        {
            if (_definitions.Values.Any(tg => tg.Name == nameOfCargo))
            {
                return _definitions.Values.Single(tg => tg.Name == nameOfCargo);
            }

            throw new Exception("Cargo item with the name " + nameOfCargo + " not found in TradeGoodLibrary. Was the trade good properly loaded?");
        }

        public ICargoable GetOther(Guid guidOfCargo)
        {
            return _definitions[guidOfCargo];
        }


        public bool IsMineral(Guid id)
        {
            return _minerals.ContainsKey(id);
        }

        public MineralSD GetMineral(string name)
        {
            var result = GetOther(name);
            return _minerals[result.ID];
        }

        public MineralSD GetMineral(Guid guid)
        {
            var result = GetOther(guid);
            return _minerals[result.ID];
        }

        public Dictionary<Guid, MineralSD> GetMinerals()
        {
            return _minerals;
        }

        public List<MineralSD> GetMineralsList()
        {
            return _minerals.Values.ToList();
        }


        public bool IsMaterial(Guid id)
        {
            return _processedMaterials.ContainsKey(id);
        }

        public ProcessedMaterialSD GetMaterial(string name)
        {
            var result = GetOther(name);
            return _processedMaterials[result.ID];
        }

        public ProcessedMaterialSD GetMaterial(Guid guid)
        {
            var result = GetOther(guid);
            return _processedMaterials[result.ID];
        }

        public Dictionary<Guid, ProcessedMaterialSD> GetMaterials()
        {
            return _processedMaterials;
        }

        public List<ProcessedMaterialSD> GetMaterialsList()
        {
            return _processedMaterials.Values.ToList();
        }
    }
}
