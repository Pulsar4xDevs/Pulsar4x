using ImGuiNET;
using Pulsar4X.ECSLib;
using System.Numerics;
using System.Collections.Generic;
using System.Linq;
using System;
using Pulsar4X.ECSLib.ComponentFeatureSets;


namespace Pulsar4X.SDL2UI
{
    public class EntitySpawnWindow : PulsarGuiWindow
    {
        private List<ShipDesign> _exsistingClasses;
        private string[] _entitytypes = new string[]{ "Ship", "Planet" };
        private int _entityindex;


        private EntitySpawnWindow()
	    {
	        _flags = ImGuiWindowFlags.AlwaysAutoResize;
        
        }

        internal static EntitySpawnWindow GetInstance() {

            EntitySpawnWindow thisItem;
            if (!_uiState.LoadedWindows.ContainsKey(typeof(EntitySpawnWindow)))
            {
                thisItem = new EntitySpawnWindow();
            }
            else
            {
                thisItem = (EntitySpawnWindow)_uiState.LoadedWindows[typeof(EntitySpawnWindow)];
            }
             

            return thisItem;


        }
        //displays selected entity info
        internal override void Display()
        {
           
            if (IsActive && ImGui.Begin("Spawn Entity", _flags))
            {


                
                if (ImGui.Combo("##entityselector", ref _entityindex, _entitytypes, _entitytypes.Length)) 
                { 

                }


                if (_entitytypes[_entityindex] == "Ship") 
                {
                    //ImGui.BeginChild("exsistingdesigns");

                    if (_exsistingClasses == null || _exsistingClasses.Count != _uiState.Faction.GetDataBlob<FactionInfoDB>().ShipDesigns.Values.ToList().Count)
                    {
                        _exsistingClasses = _uiState.Faction.GetDataBlob<FactionInfoDB>().ShipDesigns.Values.ToList();
                    }

                    for (int i = 0; i < _exsistingClasses.Count; i++)
                    {

                        string name = _exsistingClasses[i].Name;
                        if (ImGui.Selectable(name))
                        {

                            Entity _spawnedship = ShipFactory.CreateShip(_exsistingClasses[i], _uiState.Faction, _uiState.LastClickedEntity.Entity, _uiState.SelectedSystem, Guid.NewGuid().ToString());
                            NewtonionMovementProcessor.UpdateNewtonThrustAbilityDB(_spawnedship);
                            //_uiState.SelectedSystem.SetDataBlob(_spawnedship.ID, new TransitableDB());
                            //var rp1 = NameLookup.GetMaterialSD(game, "LOX/Hydrocarbon");
                            //StorageSpaceProcessor.AddCargo(_spawnedship.GetDataBlob<CargoStorageDB>(), rp1, 15000);
                        }
                    }

                    //ImGui.EndChild();
                }





            }

            
        }

        public override void OnGameTickChange(DateTime newDate)
        {
        }

        public override void OnSystemTickChange(DateTime newDate)
        {
        }

        public override void OnSelectedSystemChange(StarSystem newStarSys)
        {
            throw new NotImplementedException();
        }
    }
}
