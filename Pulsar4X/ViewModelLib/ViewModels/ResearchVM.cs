using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Pulsar4X.ECSLib;

namespace Pulsar4X.ViewModel
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
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public void Refresh(bool partialRefresh = false)
        {
            
            _colonyResearchVms = new List<ColonyResearchVM>();
            foreach (var colony in _factionEntity.GetDataBlob<FactionInfoDB>().Colonies)
            {
                List<Guid> labDesigns = _factionEntity.GetDataBlob<FactionInfoDB>().ComponentDesigns.Select(design => design.Key).ToList();
                foreach (var kvp in colony.GetDataBlob<ColonyInfoDB>().Installations)
                {
                    if (labDesigns.Contains(kvp.Key.Guid))
                    {
                        _colonyResearchVms.Add(new ColonyResearchVM(_staticData, colony));
                    }
                }
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
            //RefineingJob newjob = new RefineingJob(NewJobSelectedItem, NewJobBatchCount, _staticData_.RefinedMaterials[NewJobSelectedItem].RefinaryPointCost, NewJobRepeat);
            //RefiningProcessor.AddJob(_staticData_, _colonyEntity_, newjob);
            TechProcessor.AssignProject(SelectedScientist.ScientistEntity, SelectedTech.ID);
            SelectedScientist.Refresh();
            //Refresh();
        }

        public ColonyResearchVM()
        {         
        }

        public ColonyResearchVM(StaticDataStore staticData, Entity colonyEntity)
        {
            _factionEntity = colonyEntity.GetDataBlob<ColonyInfoDB>().FactionEntity;
            _colonyEntity = colonyEntity;
            _factionTech = _factionEntity.GetDataBlob<FactionTechDB>();
            Scientists = new List<ScientistControlVM>();
            if (_factionTech.ResearchableTechs.Count > 0)
            {
                //ResearchableTechs = new ObservableCollection<TechSD>(_factionTech.ResearchableTechs.Keys);
                ResearchableTechs = new DictionaryVM<TechSD, string>(DisplayMode.Value);
                foreach (var tech in _factionTech.ResearchableTechs.Keys)
                    ResearchableTechs.Add(tech, tech.Name);
                SelectedTech = ResearchableTechs.GetKey(0);
            }

            foreach (var scientist in _colonyEntity.GetDataBlob<ColonyInfoDB>().Scientists)
            {
                Scientists.Add(new ScientistControlVM(staticData, _factionTech, scientist));
            }
            SelectedScientist = Scientists[0];
            Refresh();

        }

        public TechSD SelectedTech { get; set; }

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
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
        public void Refresh(bool partialRefresh = false)
        {
            List<Entity> labDesigns = _factionEntity.GetDataBlob<FactionInfoDB>().ComponentDesigns.Values.Where(item => item.HasDataBlob<ResearchPointsAbilityDB>()).ToList();

            _allLabs = new Dictionary<Guid, int>();

            //ResearchableTechs = new ObservableCollection<TechSD>(_factionTech.ResearchableTechs.Keys);
            ResearchableTechs = new DictionaryVM<TechSD, string>(DisplayMode.Value);
            foreach (var tech in _factionTech.ResearchableTechs.Keys)
                ResearchableTechs.Add(tech, tech.Name);

            foreach (var kvp in _colonyEntity.GetDataBlob<ColonyInfoDB>().Installations)
            {
                if (labDesigns.Contains(kvp.Key))
                    _allLabs.Add(kvp.Key.Guid, kvp.Value);
            }
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

        public Entity ScientistEntity;
        private StaticDataStore _staticData;
        //private ColonyResearchVM _parentResearchVM;
        private FactionTechDB _factionTech;

        public string ScientistFirstName { get { return ScientistEntity.GetDataBlob<CommanderDB>().Name.First; } }
        public string ScientistLastName { get { return ScientistEntity.GetDataBlob<CommanderDB>().Name.Last; } }
        public int ScientistMaxLabs { get { return ScientistEntity.GetDataBlob<ScientistDB>().MaxLabs; } set{OnPropertyChanged();} }

        public byte ScientistAssignedLabs
        {
            get { return ScientistEntity.GetDataBlob<ScientistDB>().AssignedLabs; } 
            set {TechProcessor.AssignLabs(ScientistEntity, value); OnPropertyChanged();}
        }
        //public int ColonyFreeLabs { get}

        public Dictionary<ResearchCategories,float> ScientistBonus { get { return ScientistEntity.GetDataBlob<ScientistDB>().Bonuses; } }
        private ObservableCollection<ResearchTechControlVM> _projectQueue;
        public ObservableCollection<ResearchTechControlVM> ProjectQueue { get { return _projectQueue; } }



        #region AddTech 




        #endregion


        public ScientistControlVM()
        {            
        }

        public ScientistControlVM(StaticDataStore staticData, FactionTechDB factionTech, Entity scientist)
        {
            _staticData = staticData;
            _factionTech = factionTech;
            ScientistEntity = scientist;

            _projectQueue = new ObservableCollection<ResearchTechControlVM>();
            //foreach (var techGuid in scientist.GetDataBlob<ScientistDB>().ProjectQueue)
            //{
            //    _projectQueue.Add(new ResearchTechControlVM(_factionTech, _staticData.Techs[techGuid]));
            //}
            Refresh();
            
        }

        public void AddTechToQueue(Guid techID)
        {
            //Pulsar4X.ECSLib.s
        }

        public event PropertyChangedEventHandler PropertyChanged;
        [NotifyPropertyChangedInvocator]
        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
        public void Refresh(bool partialRefresh = false)
        {
            _projectQueue.Clear();
            foreach (var techGuid in ScientistEntity.GetDataBlob<ScientistDB>().ProjectQueue)
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

        public int PointCost { get { return TechProcessor.CostFormula(_factionTech, _techSD); } }
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
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
        public void Refresh(bool partialRefresh = false)
        {
            PointsCompleted = PointsCompleted;
        }
    }
}
