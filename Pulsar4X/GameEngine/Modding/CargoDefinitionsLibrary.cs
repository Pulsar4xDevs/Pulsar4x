using System.Collections.Generic;
using System.Linq;
using Pulsar4X.Blueprints;
using Pulsar4X.DataStructures;
using Pulsar4X.Engine.Industry;
using Pulsar4X.Interfaces;

namespace Pulsar4X.Modding
{
    public class CargoDefinitionsLibrary
    {
        private SafeDictionary<string, ICargoable> _definitions;
        private SafeDictionary<string, Mineral> _minerals;
        private SafeDictionary<string, ProcessedMaterial> _processedMaterials;

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
            _minerals = new SafeDictionary<string, Mineral>();
            _processedMaterials = new SafeDictionary<string, ProcessedMaterial>();

            LoadDefinitions(minerals, processedMaterials, otherCargo);
        }

        public CargoDefinitionsLibrary(CargoDefinitionsLibrary other)
        {
            _definitions = new SafeDictionary<string, ICargoable>(other._definitions);
            _minerals = new SafeDictionary<string, Mineral>(other._minerals);
            _processedMaterials = new SafeDictionary<string, ProcessedMaterial>(other._processedMaterials);
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
                    Add(new Mineral(entry));
                }
            }
        }

        public void LoadMaterialsDefinitions(List<ProcessedMaterialBlueprint> materials)
        {
            if (materials != null)
            {
                foreach (var entry in materials)
                {
                    Add(new ProcessedMaterial(entry));
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

        public void Add(Mineral mineral)
        {
            _definitions[mineral.UniqueID] = mineral;
            _minerals[mineral.UniqueID] = mineral;
        }

        public void Add(ProcessedMaterial material)
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

        public bool Remove(Mineral mineral)
        {
            return _definitions.Remove(mineral.UniqueID) && _minerals.Remove(mineral.UniqueID);
        }

        public bool Remove(ProcessedMaterial material)
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

        public Mineral GetMineralByName(string name) => _minerals.Values.Where(m => m.Name.Equals(name)).First();

        public Mineral GetMineral(string id) => _minerals[id];

        public SafeDictionary<string, Mineral> GetMinerals() => _minerals;

        public SafeList<Mineral> GetMineralsList() => new SafeList<Mineral>(_minerals.Values.ToList());

        public bool IsMaterial(string id) => _processedMaterials.ContainsKey(id);

        public ProcessedMaterial GetMaterialByName(string name) => _processedMaterials.Values.Where(p => p.Name.Equals(name)).First();

        public ProcessedMaterial GetMaterial(string id) => _processedMaterials[id];

        public SafeDictionary<string, ProcessedMaterial> GetMaterials() => _processedMaterials;

        public SafeList<ProcessedMaterial> GetMaterialsList() => new SafeList<ProcessedMaterial>(_processedMaterials.Values.ToList());
    }
}
