using Pulsar4X.ECSLib;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace Pulsar4X.ECSLib
{
    public class FactionResearchVM : IViewModel
    {
        private Dictionary<Guid, Entity> _colonyDictionary;
        private Entity _factionEntity;
        private StaticDataStore _staticData;

        public List<ColonyResearchVM> ColonyResearchVms
        {
            get {return _colonyResearchVms;} 
            set { _colonyResearchVms = value; OnPropertyChanged(); }
        }
        private List<ColonyResearchVM> _colonyResearchVms;

        public FactionResearchVM()
        {            
        }

        public FactionResearchVM(StaticDataStore staticData, Entity factionEntity)
        {
            _staticData = staticData;
            _factionEntity = factionEntity;
        }


        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public void Refresh(bool partialRefresh = false)
        {
            
            _colonyResearchVms = new List<ColonyResearchVM>();
            foreach (var colony in _factionEntity.GetDataBlob<FactionInfoDB>().Colonies)
            {
                List<Guid> labDesigns = _factionEntity.GetDataBlob<FactionInfoDB>().ComponentDesigns.Select(design => design.Key).ToList();
                ComponentInstancesDB instances = colony.GetDataBlob<ComponentInstancesDB>();
                /*
                foreach (var kvp in instances.ComponentsByDesign)
                {
                    if (labDesigns.Contains(kvp.Key.Guid))
                    {
                        _colonyResearchVms.Add(new ColonyResearchVM(_staticData, colony));
                    }
                }*/
            }
            foreach (var vm in ColonyResearchVms)
            {
                vm.Refresh();
            }
        }
    }

    public class ColonyResearchVM : IViewModel
    {
        private Entity _factionEntity;
        private FactionTechDB _factionTech;
        private Entity _colonyEntity;

        public string ColonyName
        {
            get { return _colonyEntity.GetDataBlob<NameDB>().DefaultName; }
            set { OnPropertyChanged();}
        }

        public Dictionary<Guid, int> AllLabs
        {
            get { return _allLabs; } 
            set { _allLabs = value; OnPropertyChanged(); }
        }
        private Dictionary<Guid, int> _allLabs;

        public int FreeLabs {
            get { return _freeLabs; }
            set { _freeLabs = value; OnPropertyChanged(); } }
        private int _freeLabs;

        //public ObservableCollection<TechSD> ResearchableTechs { get; set; }
        public DictionaryVM<TechSD, string> ResearchableTechs { get; set; }

        public List<ScientistControlVM> Scientists { get; set; }

        public int SelectedScientistIndex { get; set; }
        public ScientistControlVM SelectedScientist { get; set; }
        //public string SelectedScientist { get { return Scientists[SelectedScientist].ScientistFirstName}

        private ICommand _addNewProject;
        public ICommand AddNewProject
        {
            get
            {
                return _addNewProject ?? (_addNewProject = new CommandHandler(OnNewProject, true));
            }
        }

        public void OnNewProject()
        {
            //RefineingJob newjob = new RefineingJob(NewJobSelectedItem, NewJobBatchCount, _staticData_.RefinedMaterials[NewJobSelectedItem].RefineryPointCost, NewJobRepeat);
            //RefiningProcessor.AddJob(_staticData_, _colonyEntity_, newjob);
            ResearchProcessor.AssignProject(SelectedScientist.ScientistEntity, SelectedTech.ID);
            SelectedScientist.Refresh();
            //Refresh();
        }

        public ColonyResearchVM()
        {         
        }

        public ColonyResearchVM(StaticDataStore staticData, Entity colonyEntity)
        {
            //_factionEntity;// = colonyEntity.GetDataBlob<OwnedDB>().OwnedByFaction;
            colonyEntity.Manager.FindEntityByGuid(colonyEntity.FactionOwner, out _factionEntity);
            _colonyEntity = colonyEntity;
            _factionTech = _factionEntity.GetDataBlob<FactionTechDB>();
            Scientists = new List<ScientistControlVM>();
            if (_factionTech.ResearchableTechs.Count > 0)
            {
                //ResearchableTechs = new ObservableCollection<TechSD>(_factionTech.ResearchableTechs.Keys);
                ResearchableTechs = new DictionaryVM<TechSD, string>(DisplayMode.Value);
                foreach (var tech in _factionTech.ResearchableTechs.Keys)
                    ResearchableTechs.Add(tech, tech.Name);
                SelectedTechIndex = 0;
            }

            foreach (Scientist scientist in _colonyEntity.GetDataBlob<TeamsHousedDB>().TeamsByType[TeamTypes.Science])
            {
                Scientists.Add(new ScientistControlVM(staticData, _factionTech, scientist));
            }
            SelectedScientist = Scientists[0];
            Refresh();

        }

        public int SelectedTechIndex { get; set; }
        public TechSD SelectedTech { get { return ResearchableTechs.GetKey(SelectedTechIndex); } }

        public int SelectedTechPointsComplete
        {
            get
            {
                if (SelectedTech.Name == null)
                    return 0;
                return _factionTech.ResearchableTechs[SelectedTech];
            }
        }

        public int SelectedTechNextLevel
        {
            get
            {
                if (SelectedTech.Name == null)
                    return 0;
                return _factionTech.LevelforTech(SelectedTech) + 1;
            }
        }



        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        public void Refresh(bool partialRefresh = false)
        {
            List<ComponentDesign> labDesigns = _factionEntity.GetDataBlob<FactionInfoDB>().ComponentDesigns.Values.Where(item => item.HasAttribute<ResearchPointsAtbDB>()).ToList();

            _allLabs = new Dictionary<Guid, int>();

            //ResearchableTechs = new ObservableCollection<TechSD>(_factionTech.ResearchableTechs.Keys);
            ResearchableTechs = new DictionaryVM<TechSD, string>(DisplayMode.Value);
            foreach (var tech in _factionTech.ResearchableTechs.Keys)
                ResearchableTechs.Add(tech, tech.Name);
            /*
            foreach (var kvp in _colonyEntity.GetDataBlob<ComponentInstancesDB>().ComponentsByDesign)
            {
                if (labDesigns.Contains(kvp.Key))
                    _allLabs.Add(kvp.Key.Guid, kvp.Value.Count);
            }*/
            AllLabs = AllLabs;

            _freeLabs = 0;
            foreach (var kvp in AllLabs)
            {
                _freeLabs += kvp.Value;
            }
            foreach (var scientist in Scientists)
            {
                _freeLabs -= scientist.ScientistAssignedLabs;
            }
            FreeLabs = _freeLabs;
        }        
    }

    

    public class ScientistControlVM : IViewModel
    {

        public Scientist ScientistEntity;
        private StaticDataStore _staticData;
        //private ColonyResearchVM _parentResearchVM;
        private FactionTechDB _factionTech;

        //public string ScientistFirstName { get { return ScientistEntity.GetDataBlob<CommanderDB>().Name.First; } }
        //public string ScientistLastName { get { return ScientistEntity.GetDataBlob<CommanderDB>().Name.Last; } }
        //public int ScientistMaxLabs { get { return ScientistEntity.GetDataBlob<ScientistDB>().MaxLabs; } set{OnPropertyChanged();} }

        public byte ScientistAssignedLabs
        {
            get { return ScientistEntity.AssignedLabs; } 
            set {ResearchProcessor.AssignLabs(ScientistEntity, value); OnPropertyChanged();}
        }
        //public int ColonyFreeLabs { get}

        public Dictionary<ResearchCategories,float> ScientistBonus { get { return ScientistEntity.Bonuses; } }
        private ObservableCollection<ResearchTechControlVM> _projectQueue;
        public ObservableCollection<ResearchTechControlVM> ProjectQueue { get { return _projectQueue; } }



        #region AddTech 




        #endregion


        public ScientistControlVM()
        {            
        }

        public ScientistControlVM(StaticDataStore staticData, FactionTechDB factionTech, Scientist scientist)
        {
            _staticData = staticData;
            _factionTech = factionTech;
            ScientistEntity = scientist;

            _projectQueue = new ObservableCollection<ResearchTechControlVM>();
            Refresh();
            
        }

        public event PropertyChangedEventHandler PropertyChanged;
        [NotifyPropertyChangedInvocator]
        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        public void Refresh(bool partialRefresh = false)
        {
            _projectQueue.Clear();
            foreach (var techGuid in ScientistEntity.ProjectQueue)
            {
                _projectQueue.Add(new ResearchTechControlVM(_factionTech, techGuid));
            }
        }
    }

    public class ResearchTechControlVM : IViewModel
    {
        private FactionTechDB _factionTech;
        private TechSD _techSD;

        public string TechName { get { return _techSD.Name; } }
        public int Level { get { return _factionTech.LevelforTech(_techSD) + 1; } }

        public int PointCost { get { return ResearchProcessor.CostFormula(_factionTech, _techSD); } }
        public int PointsCompleted { get { return _factionTech.ResearchableTechs[_techSD]; } set{OnPropertyChanged();} }

        public ResearchTechControlVM()
        {           
        }

        public ResearchTechControlVM(FactionTechDB factionTech, Guid techID)
        {
            _factionTech = factionTech;
            _techSD = factionTech.ResearchableTechs.Keys.First(k => k.ID == techID);
        }

        public event PropertyChangedEventHandler PropertyChanged;
        [NotifyPropertyChangedInvocator]
        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        public void Refresh(bool partialRefresh = false)
        {
            PointsCompleted = PointsCompleted;
        }
    }
}
