using Pulsar4X.ECSLib;

namespace Pulsar4X.ECSLib
{
    public class NewSpeciesVM : ViewModelBase
    {
        public string Name { get { return _name; } set { _name = value;  OnPropertyChanged(); } }
        private string _name;

        public MinMaxSliderVM BaseGravity { get; } = new MinMaxSliderVM();
        public int BaseGravityCost {
            get { return _baseGravityCost; }
            set { _baseGravityCost = value; OnPropertyChanged(); }}
        private int _baseGravityCost;

        public MinMaxSliderVM MinGravity { get; } = new MinMaxSliderVM();
        public int MinGravityCost
        {
            get { return _minGravityCost; }
            set { _minGravityCost = value; OnPropertyChanged(); }
        }
        private int _minGravityCost;

        public MinMaxSliderVM MaxGravity { get; } = new MinMaxSliderVM();
        public int MaxGravityCost
        {
            get { return _maxGravityCost; }
            set { _maxGravityCost = value; OnPropertyChanged(); }
        }
        private int _maxGravityCost;

        public MinMaxSliderVM BasePressure { get; } = new MinMaxSliderVM();
        public int BasePressureCost
        {
            get { return _basePressureCost; }
            set { _basePressureCost = value; OnPropertyChanged(); }
        }
        private int _basePressureCost;

        public MinMaxSliderVM MinPressure { get; } = new MinMaxSliderVM();
        public int MinPressureCost
        {
            get { return _minPressureCost; }
            set { _minPressureCost = value; OnPropertyChanged(); }
        }
        private int _minPressureCost;

        public MinMaxSliderVM MaxPressure { get; } = new MinMaxSliderVM();
        public int MaxPressureCost
        {
            get { return _maxPressureCost; }
            set { _maxPressureCost = value; OnPropertyChanged(); }
        }
        private int _maxPressureCost;

        public MinMaxSliderVM BaseTemprature { get; } = new MinMaxSliderVM();
        public int BaseTempratureCost
        {
            get { return _baseTempratureCost; }
            set { _baseTempratureCost = value; OnPropertyChanged(); }
        }
        private int _baseTempratureCost;

        public MinMaxSliderVM MinTemprature { get; } = new MinMaxSliderVM();
        public int MinTempratureCost
        {
            get { return _minTempratureCost; }
            set { _minTempratureCost = value; OnPropertyChanged(); }
        }
        private int _minTempratureCost;

        public MinMaxSliderVM MaxTemprature { get; } = new MinMaxSliderVM();
        public int MaxTempratureCost
        {
            get { return _maxTempratureCost; }
            set { _maxTempratureCost = value; OnPropertyChanged(); }
        }
        private int _maxTempratureCost;

        public MinMaxSliderVM MiningBonus { get; } = new MinMaxSliderVM();
        public int MiningbonusCost
        {
            get { return _miningBonusCost; }
            set { _miningBonusCost = value; OnPropertyChanged(); }
        }
        private int _miningBonusCost;

        public MinMaxSliderVM RefiningBonus { get; } = new MinMaxSliderVM();
        public int RefiningBonusCost
        {
            get { return _refiningBonusCost; }
            set { _refiningBonusCost = value; OnPropertyChanged(); }
        }
        private int _refiningBonusCost;

        public MinMaxSliderVM ConstructionBonus { get; } = new MinMaxSliderVM();
        public int ConstructionBonusCost
        {
            get { return _constructionBonusCost; }
            set { _constructionBonusCost = value; OnPropertyChanged(); }
        }
        private int _constructionBonusCost;

        public MinMaxSliderVM TrainingBonus { get; } = new MinMaxSliderVM();
        public int TrainingBonusCost
        {
            get { return _trainingBonusCost; }
            set { _trainingBonusCost = value; OnPropertyChanged(); }
        }
        private int _trainingBonusCost;

        public MinMaxSliderVM ReproductionBonus { get; } = new MinMaxSliderVM();
        public int ReproductionBonusCost
        {
            get { return _reproductionBonusCost; }
            set { _reproductionBonusCost = value; OnPropertyChanged(); }
        }
        private int _reproductionBonusCost;

        public MinMaxSliderVM ResearchBonus { get; } = new MinMaxSliderVM();
        public int ResearchBonusCost
        {
            get { return _researchBonusCost; }
            set { _researchBonusCost = value; OnPropertyChanged(); }
        }
        private int _researchBonusCost;

        public NewSpeciesVM()
        {
            
        }

        Entity CreateNewSpecies(GameVM gameVM)
        {

            NameDB name = new NameDB(Name);
            SpeciesDB species = new SpeciesDB(BaseGravity.Value,
                MinGravity.Value, MaxGravity.Value,
                BasePressure.Value, MinPressure.Value,
                MaxPressure.Value, BaseTemprature.Value,
                MinTemprature.Value, MaxTemprature.Value);

           return SpeciesFactory.CreateSpeciesFromBlobs(gameVM.CurrentFaction, gameVM.Game.GlobalManager, name, species);
        }

    }
}
