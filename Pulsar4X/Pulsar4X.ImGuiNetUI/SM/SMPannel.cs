using System;
using System.Collections.Generic;
using System.Data;
using ImGuiNET;
using Pulsar4X.ECSLib;

namespace Pulsar4X.SDL2UI
{
    public class SMPannel : PulsarGuiWindow
    {
        private Game game;
        private Entity[] _factions = new Entity[0];
        private StarSystem[] _starSystems = new StarSystem[0];
        private StarSystem _currentSystem;

        private int _selectedEntityIndex = -1;
        private Entity[] _systemEntities = new Entity[0];
        private string[] _systemEntityNames = new string[0];


        private Entity[] _filteredEntities = new Entity[0];
        
        Entity _selectedEntity
        {
            get
            {
                if (_selectedEntityIndex >= 0 && _selectedEntityIndex < _systemEntities.Length)
                    return _systemEntities[_selectedEntityIndex];
                return null;
            }
        }

        
        
        private SMPannel() 
        {
            _uiState.SpaceMasterVM = new SpaceMasterVM();
            HardRefresh();
        }

        //TODO auth of some kind. 
        public static SMPannel GetInstance()
        {
            if (!_uiState.LoadedWindows.ContainsKey(typeof(SMPannel)))
            {
                return new SMPannel();
            }
            return (SMPannel)_uiState.LoadedWindows[typeof(SMPannel)];
        }

        void HardRefresh()
        {
            game = _uiState.Game;
            _starSystems = new StarSystem[_uiState.Game.Systems.Count];
            int i = 0;
            foreach (var starsys in _uiState.Game.Systems.Values)
            {
                _starSystems[i] = starsys;
                
                i++;
            }

            _currentSystem = _uiState.SelectedSystem;

            //_systemEntities = _currentSystem.GetAllEntites().ToArray();
            List<Entity> allEntites = new List<Entity>();
            foreach (var entity in _currentSystem.GetAllEntites())
            {
                if(entity == null)
                    continue;
                allEntites.Add(entity);
            }
            _systemEntities = allEntites.ToArray();
            
            _systemEntityNames = new string[_systemEntities.Length];
            for (int j = 0; j < _systemEntities.Length; j++)
            {
                var entity = _systemEntities[j];
                if(entity.HasDataBlob<NameDB>())
                    _systemEntityNames[j] = _systemEntities[j].GetDataBlob<NameDB>().OwnersName;
                else
                {
                    _systemEntityNames[j] = "No NameDB";
                }


            }


        }

        private bool _entityInspectorWindow = false;
        internal override void Display()
        {
            //selectedEntityData
            if (_uiState.SMenabled && ImGui.Begin("SM", ref IsActive, _flags))
            {
                if(_currentSystem != _uiState.SelectedSystem)
                    HardRefresh();

                ImGui.Columns(2);
                ImGui.SetColumnWidth(0, 200);
                for (int i = 0; i < _systemEntities.Length; i++)
                {
                    if (ImGui.Selectable(_systemEntityNames[i]))
                    {
                        _selectedEntityIndex = i;
                        _entityInspectorWindow = !_entityInspectorWindow;
                    }
                    
                    ImGui.NextColumn();
                    var ownerFactionID = _systemEntities[i].FactionOwnerID;
                    if(ownerFactionID != Guid.Empty)
                    {
                        var ownerFaction = game.GlobalManager.GetGlobalEntityByGuid(ownerFactionID);
                        var factionName = ownerFaction.GetDataBlob<NameDB>().OwnersName;
                        ImGui.Text(factionName);
                    }
                    else
                    {
                        ImGui.Text(ownerFactionID.ToString());
                    }
                    ImGui.NextColumn();
                    
                    
                }

                if (_entityInspectorWindow)
                {
                    EntityInspector.Begin(_selectedEntity);
                }



                /*
                if (_selectedEntity != null && _selectedEntity.Entity != null)
                {
                    Entity entity = _selectedEntity.Entity;
                    var datablobs = entity.DataBlobs;
                    ImGui.Text(_selectedEntity.Name);
                    foreach (var datablob in datablobs)
                    {
                        ImGui.Text(datablob.GetType().Name);
                    }
                    if (ImGui.Button("AddOrbit"))
                    {
                        var pannel = WarpOrderWindow.GetInstance(_selectedEntity, true);
                        pannel.SetActive();
                        _uiState.ActiveWindow = pannel;
                    }

                }
                */
            }
            ImGui.End();
        }

        internal override void EntityClicked(EntityState entity, MouseButtons button)
        {
            //if (button == MouseButtons.Primary)
            //    _selectedEntity = entity;
        }
    }
}
