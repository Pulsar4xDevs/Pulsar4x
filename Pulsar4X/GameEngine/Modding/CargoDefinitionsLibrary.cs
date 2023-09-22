using System.Collections.Generic;
using System.Linq;
using Pulsar4X.DataStructures;
using Pulsar4X.Interfaces;
using Pulsar4X.Blueprints;

namespace Pulsar4X.Modding
{
    public class CargoDefinitionsLibrary
    {
        private SafeDictionary<string, ICargoable> _definitions;
        private SafeDictionary<string, MineralBlueprint> _minerals;
        private SafeDictionary<string, ProcessedMaterialBlueprint> _processedMaterials;

        public CargoDefinitionsLibrary() : this(new List<MineralBlueprint>(),
            new List<ProcessedMaterialBlueprint>(),
            new List<ICargoable>())
        {
        }

        public CargoDefinitionsLibrary(List<MineralBlueprint> minerals,
            List<ProcessedMaterialBlueprint> processedMaterials,
            List<ICargoable> otherCargo)
        {
            _definitions = new SafeDictionary<string, ICargoable>();
            _minerals = new SafeDictionary<string, MineralBlueprint>();
            _processedMaterials = new SafeDictionary<string, ProcessedMaterialBlueprint>();

            LoadDefinitions(minerals, processedMaterials, otherCargo);
        }

        public CargoDefinitionsLibrary(CargoDefinitionsLibrary other)
        {
            _definitions = new SafeDictionary<string, ICargoable>(other._definitions);
            _minerals = new SafeDictionary<string, MineralBlueprint>(other._minerals);
            _processedMaterials = new SafeDictionary<string, ProcessedMaterialBlueprint>(other._processedMaterials);
        }


        public void LoadDefinitions(List<MineralBlueprint> minerals,
            List<ProcessedMaterialBlueprint> processedMaterials,
            List<ICargoable> otherCargo)
        {
            LoadMineralDefinitions(minerals);
            LoadMaterialsDefinitions(processedMaterials);
            LoadOtherDefinitions(otherCargo);
        }

        public void LoadMineralDefinitions(List<MineralBlueprint> minerals)
        {
            if (minerals != null)
            {
                foreach (var entry in minerals)
                {
                    Add(entry);
                }
            }
        }

        public void LoadMaterialsDefinitions(List<ProcessedMaterialBlueprint> materials)
        {
            if (materials != null)
            {
                foreach (var entry in materials)
                {
                    Add(entry);
                }
            }
        }

        public void LoadOtherDefinitions(List<ICargoable> otherCargo)
        {
            if (otherCargo != null)
            {
                foreach (var entry in otherCargo)
                {
                    Add(entry);
                }
            }
        }

        public void Add(ICargoable cargoable)
        {
            _definitions[cargoable.UniqueID] = cargoable;
        }

        public void Add(MineralBlueprint mineral)
        {
            _definitions[mineral.UniqueID] = mineral;
            _minerals[mineral.UniqueID] = mineral;
        }

        public void Add(ProcessedMaterialBlueprint material)
        {
            _definitions[material.UniqueID] = material;
            _processedMaterials[material.UniqueID] = material;
            material.MineralsRequired?.ToList().ForEach(x => material.ResourceCosts[x.Key] = x.Value);
            material.MaterialsRequired?.ToList().ForEach(x => material.ResourceCosts[x.Key] = x.Value);
        }

        public bool Remove(ICargoable cargoable)
        {
            return _definitions.Remove(cargoable.UniqueID);
        }

        public bool Remove(MineralBlueprint mineral)
        {
            return _definitions.Remove(mineral.UniqueID) && _minerals.Remove(mineral.UniqueID);
        }

        public bool Remove(ProcessedMaterialBlueprint material)
        {
            return _definitions.Remove(material.UniqueID) && _processedMaterials.Remove(material.UniqueID);
        }

        public ICargoable this[string id]
        {
            get { return _definitions[id]; }
        }

        public ICargoable GetAny(string id) => _definitions[id];

        public bool Contains(string id) => _definitions.ContainsKey(id);

        public SafeDictionary<string, ICargoable> GetAll() => _definitions;

        public bool IsOther(string id) => _definitions.ContainsKey(id) && !IsMineral(id) && !IsMaterial(id);

        public ICargoable GetOtherByName(string nameOfCargo) => _definitions.Values.Where(c => c.Name.Equals(nameOfCargo)).First();

        public ICargoable GetOther(string id) => _definitions[id];

        public bool IsMineral(string id) => _minerals.ContainsKey(id);

        public MineralBlueprint GetMineralByName(string name) => _minerals.Values.Where(m => m.Name.Equals(name)).First();

        public MineralBlueprint GetMineral(string id) => _minerals[id];

        public SafeDictionary<string, MineralBlueprint> GetMinerals() => _minerals;

        public SafeList<MineralBlueprint> GetMineralsList() => new SafeList<MineralBlueprint>(_minerals.Values.ToList());

        public bool IsMaterial(string id) => _processedMaterials.ContainsKey(id);

        public ProcessedMaterialBlueprint GetMaterialByName(string name) => _processedMaterials.Values.Where(p => p.Name.Equals(name)).First();

        public ProcessedMaterialBlueprint GetMaterial(string id) => _processedMaterials[id];

        public SafeDictionary<string, ProcessedMaterialBlueprint> GetMaterials() => _processedMaterials;

        public SafeList<ProcessedMaterialBlueprint> GetMaterialsList() => new SafeList<ProcessedMaterialBlueprint>(_processedMaterials.Values.ToList());
    }
}
