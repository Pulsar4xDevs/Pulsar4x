using System.Windows.Forms;
using Pulsar4X.ECSLib;

namespace ModdingTools.JsonDataEditor.UserControls
{
    public partial class InstallationUC : UserControl
    {
        private InstallationSD _staticData;
        public InstallationSD StaticData
        {
            get { return _staticData; }
            set
            {
                _staticData = value;
                setData();
            }
        }
        public InstallationUC()
        {
            InitializeComponent();
        }

        public int GetPopReqirement
        { get { return (int)numericUpDown_Population.Value; }}
        public int GetWealthCost
        { get { return (int)numericUpDown_Wealth.Value; } }
        public int GetCargoSize
        { get { return (int)numericUpDown_CargoSize.Value; } }
        public int GetBuildPoints
        { get { return (int)numericUpDown_BuildPoints.Value; } }

        void setData()
        {
            numericUpDown_Population.Value = StaticData.PopulationRequired;
            numericUpDown_Wealth.Value = StaticData.WealthCost;
            numericUpDown_CargoSize.Value = StaticData.CargoSize;
            numericUpDown_BuildPoints.Value = StaticData.BuildPoints;
        }
    }
}
