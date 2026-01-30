using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using ImGuiNET;
using Pulsar4X.Client.Interface.Widgets;
using Pulsar4X.Engine;
using Pulsar4X.Datablobs;
using Pulsar4X.Names;

namespace Pulsar4X.Client
{
    public class SMWindow : PulsarGuiWindow
    {
        private Game? _game;
        private StarSystem? _currentSystem;

        private int _selectedEntityIndex = -1;
        private Entity[] _systemEntities = new Entity[0];
        private string[] _systemEntityNames = new string[0];

        Entity? SelectedEntity
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
            HardRefresh();

            _uiState.OnStarSystemChanged += state =>
            {
                _selectedEntityIndex = -1;
                HardRefresh();
            };
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
            _currentSystem = _uiState.SelectedSystem;
            _systemEntities = _currentSystem.GetAllEntites().ToArray();
            _systemEntityNames = new string[_systemEntities.Length];
            for (int i = 0; i < _systemEntities.Length; i++)
            {
                var entity = _systemEntities[i];
                if(entity.HasDataBlob<NameDB>())
                    _systemEntityNames[i] = _systemEntities[i].GetDataBlob<NameDB>().OwnersName;
                else
                {
                    _systemEntityNames[i] = "No NameDB";
                }
            }
        }

        private bool _entityInspectorWindow = false;
        internal override void Display()
        {
            if (!_uiState.SMenabled || _game == null) return;

            //selectedEntityData
            ImGui.SetNextWindowSizeConstraints(new System.Numerics.Vector2(32, 32), new System.Numerics.Vector2(720, 720));
            if (Window.Begin("SM", ref IsActive, _flags))
            {
                ImGui.Columns(2);
                ImGui.SetColumnWidth(0, 200);
                for (int i = 0; i < _systemEntities.Length; i++)
                {
                    ImGui.PushID(_systemEntities[i].Id);
                    bool isSelected = _selectedEntityIndex == i;
                    if (ImGui.Selectable(_systemEntityNames[i], isSelected))
                    {
                        if (i == _selectedEntityIndex)
                        {
                            _selectedEntityIndex = -1;
                            _entityInspectorWindow = false;
                        }
                        else
                        {
                            _selectedEntityIndex = i;
                            _entityInspectorWindow = true;
                        }
                    }

                    ImGui.NextColumn();
                    ImGui.Text(_systemEntities[i].GetFactionName());
                    ImGui.NextColumn();
                    ImGui.PopID();
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
            Window.End();

            if (_entityInspectorWindow && SelectedEntity != null)
            {
                EntityInspector.Begin(SelectedEntity);
            }
        }

        internal override void EntityClicked(EntityState entity, MouseButtons button)
        {
            //if (button == MouseButtons.Primary)
            //    _selectedEntity = entity;
        }
    }
}
