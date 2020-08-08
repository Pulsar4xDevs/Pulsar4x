using System;
using System.Numerics;
using System.Collections.Generic;
using System.Linq;
using ImGuiNET;
using ImGuiSDL2CS;
using Pulsar4X.ECSLib;
using SDL2;

namespace Pulsar4X.SDL2UI
{
    public class ComponentDesignUI : PulsarGuiWindow
    {
        private int _designType;
        private string[] _designTypes;
        private ComponentTemplateSD[] _designables;
        private byte[] _nameInputBuffer = new byte[128];
        private List<PartDesignUI> _openwindows = new List<PartDesignUI>();

        private ComponentDesignUI()
        {
            //_flags = ImGuiWindowFlags.NoCollapse;
        }

        internal static ComponentDesignUI GetInstance()
        {
            ComponentDesignUI thisitem;
            if (!_uiState.LoadedWindows.ContainsKey(typeof(ComponentDesignUI)))
            {
                thisitem = new ComponentDesignUI();
            }
            thisitem = (ComponentDesignUI)_uiState.LoadedWindows[typeof(ComponentDesignUI)];

            //TODO: pull this from faction info and have designables unlocked via tech.
            thisitem._designables = StaticRefLib.StaticData.ComponentTemplates.Values.ToArray();
            int count = thisitem._designables.Length;
            thisitem._designTypes = new string[count];
            for (int i = 0; i < count ; i++)
            {
                thisitem._designTypes[i] = thisitem._designables[i].Name;
            }
            return thisitem;
        }

        internal override void Display()
        {
            if (IsActive && ImGui.Begin("Component Design", ref IsActive, _flags))
            {
                if (ImGui.Button("Design Missile"))
                    OrdinanceDesignUI.GetInstance().ToggleActive();
                int numelements = Math.Min(Math.Max(4, Convert.ToInt32((ImGui.GetContentRegionAvail().Y - 20) / 17)), _designTypes.Length);
                
                ImGui.PushItemWidth(-1);
                if (ImGui.ListBox("", ref _designType, _designTypes, _designTypes.Length, numelements))     //Lists the possible comp types
                {
                    _openwindows.Add(new PartDesignUI(_designType, _uiState));
                }
                ImGui.End();
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