using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using ImGuiNET;
using Pulsar4X.Engine;
using Pulsar4X.Datablobs;

namespace Pulsar4X.SDL2UI
{
    public class SMWindow : PulsarGuiWindow
    {
        private Game? _game;
        private Entity[] _factions = new Entity[0];
        private StarSystem[] _starSystems = new StarSystem[0];
        private StarSystem? _currentSystem;

        private int _selectedEntityIndex = -1;
        private Entity[] _systemEntities = new Entity[0];
        private string[] _systemEntityNames = new string[0];


        private Entity[] _filteredEntities = new Entity[0];

        Entity? _selectedEntity
        {
            get
            {
                if (_selectedEntityIndex >= 0 && _selectedEntityIndex < _systemEntities.Length)
                    return _systemEntities[_selectedEntityIndex];
                return null;
            }
        }



        private SMWindow()
        {
            //_uiState.SpaceMasterVM = new SpaceMasterVM();
            HardRefresh();
        }

        //TODO auth of some kind.
        public static SMWindow GetInstance()
        {
            if (!_uiState.LoadedWindows.ContainsKey(typeof(SMWindow)))
            {
                return new SMWindow();
            }
            return (SMWindow)_uiState.LoadedWindows[typeof(SMWindow)];
        }

        void HardRefresh()
        {
            _game = _uiState.Game;
            _starSystems = new StarSystem[_uiState.Game.Systems.Count];
            int i = 0;
            foreach (var starsys in _uiState.Game.Systems)
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
            ImGui.SetNextWindowSizeConstraints(new System.Numerics.Vector2(32, 32), new System.Numerics.Vector2(720, 720));
            if (_uiState.SMenabled && ImGui.Begin("SM", ref IsActive, _flags))
            {
                if(_currentSystem != _uiState.SelectedSystem)
                    HardRefresh();

                if(_game == null)
                {
                    ImGui.End();
                    return;
                }

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
                    ImGui.Text(_systemEntities[i].GetFactionName());
                    ImGui.NextColumn();
                }

                if (_entityInspectorWindow && _selectedEntity != null)
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
