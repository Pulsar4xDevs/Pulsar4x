using System.Collections.Generic;
using System.Linq;
using Pulsar4X.DataStructures;
using Pulsar4X.Engine.Industry;
using Pulsar4X.Interfaces;

namespace Pulsar4X.Engine
{
    public class CargoDefinitionsLibrary
    {
        private SafeDictionary<int, ICargoable> _definitions;
        private SafeDictionary<int, Mineral> _minerals;
        private SafeDictionary<int, ProcessedMaterial> _processedMaterials;

        public CargoDefinitionsLibrary() : this(new List<Mineral>(),
            new List<ProcessedMaterial>(),
            new List<ICargoable>())
        {
        }

        public CargoDefinitionsLibrary(List<Mineral> minerals,
            List<ProcessedMaterial> processedMaterials,
            List<ICargoable> otherCargo)
        {
            _definitions = new SafeDictionary<int, ICargoable>();
            _minerals = new SafeDictionary<int, Mineral>();
            _processedMaterials = new SafeDictionary<int, ProcessedMaterial>();

            LoadDefinitions(minerals, processedMaterials, otherCargo);
        }

        public CargoDefinitionsLibrary(CargoDefinitionsLibrary other)
        {
            _definitions = new SafeDictionary<int, ICargoable>(other._definitions);
            _minerals = new SafeDictionary<int, Mineral>(other._minerals);
            _processedMaterials = new SafeDictionary<int, ProcessedMaterial>(other._processedMaterials);
        }


        public void LoadDefinitions(List<Mineral> minerals,
            List<ProcessedMaterial> processedMaterials,
            List<ICargoable> otherCargo)
        {
            LoadMineralDefinitions(minerals);
            LoadMaterialsDefinitions(processedMaterials);
            LoadOtherDefinitions(otherCargo);
        }

        public void LoadMineralDefinitions(List<Mineral> minerals)
        {
            if (minerals != null)
            {
                foreach (var entry in minerals)
                {
                    Add(entry);
                }
            }
        }

        public void LoadMaterialsDefinitions(List<ProcessedMaterial> materials)
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
            _definitions[cargoable.ID] = cargoable;
        }

        public void Add(Mineral mineral)
        {
            _definitions[mineral.ID] = mineral;
            _minerals[mineral.ID] = mineral;
        }

        public void Add(ProcessedMaterial material)
        {
            _definitions[material.ID] = material;
            _processedMaterials[material.ID] = material;
            // material.MineralsRequired?.ToList().ForEach(x => material.ResourceCosts[x.Key] = x.Value);
            // material.MaterialsRequired?.ToList().ForEach(x => material.ResourceCosts[x.Key] = x.Value);
        }

        public bool Remove(ICargoable cargoable)
        {
            return _definitions.Remove(cargoable.ID);
        }

        public bool Remove(Mineral mineral)
        {
            return _definitions.Remove(mineral.ID) && _minerals.Remove(mineral.ID);
        }

        public bool Remove(ProcessedMaterial material)
        {
            return _definitions.Remove(material.ID) && _processedMaterials.Remove(material.ID);
        }

        public ICargoable this[int id]
        {
            get { return _definitions[id]; }
        }

        public ICargoable this[string uniqueID]
        {
            get { return _definitions.Values.Where(d => d.UniqueID.Equals(uniqueID)).First(); }
        }

        public ICargoable GetAny(int id) => _definitions[id];
        public ICargoable? GetAny(string id) => _definitions.Count == 0 ? null : _definitions.Where(d => d.Value.UniqueID.Equals(id)).Select(kvp => kvp.Value).First();

        public bool Contains(int id) => _definitions.ContainsKey(id);
        public bool Contains(string uniqueID) => _definitions.Any(d => d.Value.UniqueID.Equals(uniqueID));

        public SafeDictionary<int, ICargoable> GetAll() => _definitions;

        public bool IsOther(int id) => _definitions.ContainsKey(id) && !IsMineral(id) && !IsMaterial(id);

        public ICargoable GetOtherByName(string nameOfCargo) => _definitions.Values.Where(c => c.Name.Equals(nameOfCargo)).First();

        public ICargoable GetOther(int id) => _definitions[id];
        public ICargoable GetOther(string uniqueID) => _definitions.Values.Where(m => m.UniqueID.Equals(uniqueID)).First();

        public bool IsMineral(int id) => _minerals.ContainsKey(id);
        public bool IsMineral(string uniqueID) => _minerals.Values.Any(m => m.UniqueID.Equals(uniqueID));

        public Mineral GetMineralByName(string name) => _minerals.Values.Where(m => m.Name.Equals(name)).First();

        public Mineral GetMineral(int id) => _minerals[id];
        public Mineral GetMineral(string uniqueID) => _minerals.Values.Where(m => m.UniqueID.Equals(uniqueID)).First();

        public SafeDictionary<int, Mineral> GetMinerals() => _minerals;

        public SafeList<Mineral> GetMineralsList() => new SafeList<Mineral>(_minerals.Values.ToList());

        public bool IsMaterial(int id) => _processedMaterials.ContainsKey(id);
        public bool IsMaterial(string uniqueID) => _processedMaterials.Values.Any(m => m.UniqueID.Equals(uniqueID));

        public ProcessedMaterial GetMaterialByName(string name) => _processedMaterials.Values.Where(p => p.Name.Equals(name)).First();

        public ProcessedMaterial GetMaterial(int id) => _processedMaterials[id];
        public ProcessedMaterial GetMaterial(string uniqueID) => _processedMaterials.Values.Where(p => p.UniqueID.Equals(uniqueID)).First();

        public SafeDictionary<int, ProcessedMaterial> GetMaterials() => _processedMaterials;

        public SafeList<ProcessedMaterial> GetMaterialsList() => new SafeList<ProcessedMaterial>(_processedMaterials.Values.ToList());
    }
}
