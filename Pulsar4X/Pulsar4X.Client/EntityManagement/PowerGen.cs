using System;
using System.Numerics;
using ImGuiNET;
using ImGuiSDL2CS;
using Pulsar4X.Engine;
using Pulsar4X.Datablobs;
using Pulsar4X.SDL2UI;
using Vector2 = System.Numerics.Vector2;

namespace Pulsar4X.ImGuiNetUI.EntityManagement
{
    public class PowerGen : PulsarGuiWindow
    {
        private EntityState _entityState;
        Vector2 _plotSize = new Vector2(512, 64);
        private EnergyGenAbilityDB _energyGenDB;
        
        
        internal static PowerGen GetInstance()
        {
            PowerGen instance;
            if (!_uiState.LoadedWindows.ContainsKey(typeof(PowerGen)))
            {
                instance = new PowerGen(_uiState.LastClickedEntity);
            }
            else
                instance = (PowerGen)_uiState.LoadedWindows[typeof(PowerGen)];
            if(instance._entityState != _uiState.LastClickedEntity)
                instance.SetEntity(_uiState.LastClickedEntity);
            //instance._sysState = _uiState.StarSystemStates[_uiState.SelectedSystem.ID];
            


            return instance;
        }

        private PowerGen(EntityState entity)
        {
            _entityState = entity;
        }

        public void SetEntity(EntityState entityState)
        {
            if (entityState.DataBlobs.ContainsKey(typeof(EnergyGenAbilityDB)))//If the entity has power data
            {
                _entityState = entityState;//Store it as the entity being show
                _energyGenDB = (EnergyGenAbilityDB)entityState.DataBlobs[typeof(EnergyGenAbilityDB)];//Store it's power value
                CanActive = true;//And note if that it can be displayed
            }
            else
            {
                //CanActive = false;
                //_entityState = null;
            }
        }

        public override void OnSystemTickChange(DateTime newDateTime)
        {
            //if we are looking at this, then we should process it even if nothing has changed.
            if (IsActive && CanActive)
            {
                if (_energyGenDB.dateTimeLastProcess != newDateTime)
                    EnergyGenProcessor.EnergyGen(_entityState.Entity, newDateTime);
            }
        }


        internal override void Display()
        {
            if(_entityState != _uiState.LastClickedEntity)//If the selected entity has changed
                SetEntity(_uiState.LastClickedEntity);
            //If the player has activated the menu and there is a body that can be displayed show the menu
            if (IsActive && CanActive && ImGui.Begin("Power Display " + _entityState.Name, ref IsActive, _flags))
            {
                ImGui.Text("Current Load: ");
                ImGui.SameLine();
                ImGui.Text(_energyGenDB.Load.ToString());
                
                ImGui.Text("Current Output: ");
                ImGui.SameLine();
                
                ImGui.Text(_energyGenDB.Output.ToString() + " / " + _energyGenDB.TotalOutputMax);
                
                ImGui.Text("Current Demand: ");
                ImGui.SameLine();
                ImGui.Text(_energyGenDB.Demand.ToString());
                
                ImGui.Text("Stored: ");
                ImGui.SameLine();
                string stor = _energyGenDB.EnergyStored[_energyGenDB.EnergyType.UniqueID].ToString();
                string max = _energyGenDB.EnergyStoreMax[_energyGenDB.EnergyType.UniqueID].ToString();
                ImGui.Text(stor + " / " + max);

                //

                //ImGui.PlotLines()
                var colour1 = ImGui.GetColorU32(ImGuiCol.Text);
                var colour2 = ImGui.GetColorU32(ImGuiCol.PlotLines);
                var colour3 = ImGui.GetColorU32(ImGuiCol.Button);
                ImDrawListPtr draw_list = ImGui.GetWindowDrawList();

                var plotPos = ImGui.GetCursorScreenPos();
                ImGui.InvisibleButton("PowerPlot", _plotSize);


                var hg = _energyGenDB.Histogram;
                
                int hgFirstIdx = _energyGenDB.HistogramIndex;
                int hgLastIdx;
                if (hgFirstIdx == 0)
                    hgLastIdx = hg.Count - 1;
                else
                    hgLastIdx = hgFirstIdx - 1;
            
                var hgFirstObj = hg[hgFirstIdx];
                var hgLastObj = hg[hgLastIdx];
                
                
                float xstep = _plotSize.X / hgLastObj.seconds ;
                float ystep = (float)(_plotSize.Y / _energyGenDB.EnergyStoreMax[_energyGenDB.EnergyType.UniqueID]);
                float posX = 0;
                float posYBase = plotPos.Y + _plotSize.Y;
                int index = _energyGenDB.HistogramIndex;
                var thisData = _energyGenDB.Histogram[index];
                float posYO = ystep * (float)thisData.outputval;
                float posYD = ystep * (float)thisData.demandval;
                float posYS = ystep * (float)thisData.storval;
                //float ypos = plotPos.Y + _plotSize.Y;
                
                for (int i = 0; i < _energyGenDB.HistogramSize; i++)
                {
                    
                    int idx = index + i;
                    if (idx >= _energyGenDB.HistogramSize)
                        idx -= _energyGenDB.HistogramSize;
                    thisData = _energyGenDB.Histogram[idx];
                    
                    float nextX = xstep * thisData.seconds;
                    float nextYO = ystep * (float)thisData.outputval;
                    float nextYD = ystep * (float)thisData.demandval;
                    float nextYS = ystep * (float)thisData.storval;
                    draw_list.AddLine(new Vector2(plotPos.X + posX, posYBase - posYO), new Vector2(plotPos.X + nextX, posYBase - nextYO), colour1);
                    draw_list.AddLine(new Vector2(plotPos.X + posX, posYBase - posYD), new Vector2(plotPos.X + nextX, posYBase - nextYD), colour2);
                    draw_list.AddLine(new Vector2(plotPos.X + posX, posYBase - posYS), new Vector2(plotPos.X + nextX, posYBase - nextYS), colour3);
                    posX = nextX;
                    posYO = nextYO;
                    posYD = nextYD;
                    posYS = nextYS;
                }
                ImGui.End();
                
            }

        }
    }
}