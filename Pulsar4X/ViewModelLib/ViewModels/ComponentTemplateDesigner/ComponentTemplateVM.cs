using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Pulsar4X.ECSLib;

namespace Pulsar4X.ViewModel
{
    public class ComponentTemplateVM : IViewModel
    {
        private ComponentDesignDB _designDB;

        public string Name { get; set; }
        public string Description { get; set; }
        public Guid ID { get; set; }

        public string SizeFormula { get; set; }
        public string HTKFormula { get; set; }
        public string CrewReqFormula { get; set; }
        public Dictionary<Guid, string> MineralCostFormula { get; set; }
        public string ResearchCostFormula { get; set; }
        public string CreditCostFormula { get; set; }
        public string BuildPointCostFormula { get; set; }
        //if it can be fitted to a ship as a ship component, on a planet as an installation, can be cargo etc.
        public Dictionary<ComponentMountType, bool> MountType { get; set; }

        public List<ComponentAbilityTemplateVM> ComponentAbilitySDs { get; set; }



        public ComponentTemplateVM()
        {

        }

        public ComponentTemplateVM(ComponentSD designSD)
        { }

        public void SetDesignDB(ComponentSD designSD)
        {
            Name = designSD.Name;
            Description = designSD.Description;
            ID = designSD.ID;

            SizeFormula = designSD.SizeFormula;
            HTKFormula = designSD.HTKFormula;
            CrewReqFormula = designSD.CrewReqFormula;
            MineralCostFormula = designSD.MineralCostFormula;
            ResearchCostFormula = designSD.ResearchCostFormula;
            CreditCostFormula = designSD.CreditCostFormula;
            BuildPointCostFormula = designSD.BuildPointCostFormula;
            MountType = designSD.MountType;
            ComponentAbilitySDs = new List<ComponentAbilityTemplateVM>();
            foreach (var item in designSD.ComponentAbilitySDs)
            {
                ComponentAbilitySDs.Add(new ComponentAbilityTemplateVM());
            }
        }

        public void Create(GameVM game)
        {
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public void Refresh(bool partialRefresh = false)
        {
            throw new NotImplementedException();
        }
    }
}
