using ImGuiNET;
using Pulsar4X.Client.Interface.Widgets;
using Pulsar4X.Client.Interface;
using Pulsar4X.Engine;
using Pulsar4X.Factions;
using Pulsar4X.Galaxy;
using Pulsar4X.JumpPoints;
using Pulsar4X.Messaging;
using Pulsar4X.Movement;
using Pulsar4X.Names;
using Pulsar4X.Ships;
using SDL3;
using System.Collections.Generic;
using System.Linq;
using System;
using System.Diagnostics;

namespace Pulsar4X.Client
{
    public class EntityLabelExtCombo : EntityLabel
    {
        private bool _hoverOpen = false;

        private SDL.FRect _dropDownRect = new ();

        private IOrderedEnumerable<IGrouping<UserOrbitSettings.OrbitBodyType, Entity>> _subEntities;

        public EntityLabelExtCombo(Entity entity, IEnumerable<Entity>? subEntities = null) : base(entity) {
            SetEntities(subEntities ?? []);

            _dropDownRect.W = 5;
            _dropDownRect.H = 5;
        }

        public void SetEntities(IEnumerable<Entity> subEntities)
        {
            _subEntities = subEntities
                .GroupBy(x => Utils.EntityBodyType(x))
                .OrderBy(x => x.Key);
        }

        private bool _hovered = false;
        public override bool OnPointerEnter(SDL.Event sevent)
        {
            if (_subEntities.Any())
                _hoverOpen = true;

            _hovered = true;
            return base.OnPointerEnter(sevent);
        }
        public override bool OnPointerExit(SDL.Event sevent)
        {
            // Don't end _hoverOpen here
            _hovered = false;
            return base.OnPointerExit(sevent);
        }

        private bool _clickedAlt = false;
        public override bool OnPointerDown(SDL.Event sevent)
        {
            return base.OnPointerDown(sevent);
        }
        public override bool OnPointerUp(SDL.Event sevent)
        {
            if (sevent.Button.Button == 3)
                _clickedAlt = true;
            return base.OnPointerUp(sevent);
        }

        // TODO: this could be a static maybe?
        private Action? TooltipCallback(Entity entity)
        {
            if (_state == null)
                return null;
            var state = _state!;

            if(Entity.HasDataBlob<JPSurveyableDB>())
                return () => Displays.GravitationalAnomlay(state, Entity.GetDataBlob<JPSurveyableDB>());
            else if(Entity.HasDataBlob<ShipInfoDB>()
                    && Entity.HasDataBlob<MassVolumeDB>()
                    && Entity.HasDataBlob<PositionDB>())
                return () => Displays.Ship(state, Entity.GetDataBlob<ShipInfoDB>(), Entity.GetDataBlob<MassVolumeDB>(), Entity.GetDataBlob<PositionDB>(), state.Faction.GetDataBlob<FactionInfoDB>().Data.CargoGoods);
            else if(Entity.HasDataBlob<SystemBodyInfoDB>()
                    && Entity.HasDataBlob<MassVolumeDB>()
                    && Entity.HasDataBlob<PositionDB>())
                return () => Displays.SystemBody(state, Entity.GetDataBlob<SystemBodyInfoDB>(), Entity.GetDataBlob<MassVolumeDB>(), Entity.GetDataBlob<PositionDB>());
            else if(Entity.HasDataBlob<StarInfoDB>())
                return () => Displays.Star(state, Entity.GetDataBlob<StarInfoDB>());
            return null;
        }

        protected override void DrawExt(IntPtr rendererPtr, Camera camera)
        {
            if (_state == null || _starSysGuid == null)
                return;
            var state = _state!;
            var starSys = _starSysGuid!;

            // Alt click
            if (_clickedAlt)
            {
                state.ContextMenu = new EntityContextMenu(state, Entity.Id);
                ImGui.OpenPopup(_name + "##Alt");
                _clickedAlt = false;
            }
            if(ImGui.BeginPopupContextItem(_name + "##Alt"))
            {
                state.ContextMenu.Display();
                ImGui.EndPopup();
            }

            // Hover window
            if (_hoverOpen)
            {
                var pos = new System.Numerics.Vector2(Rect.X + Rect.Width, Rect.Y - Rect.Height);
                ImGui.SetNextWindowPos(pos, ImGuiCond.Always);

                ImGui.Begin(_name + "##Hover", ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.AlwaysAutoResize);

                // Close window if not hovered on label or window
                if (! _hovered && ! ImGui.IsWindowHovered())
                    _hoverOpen = false;

                if(ImGui.MenuItem("View " + _name))
                {
                    state.EntityClicked(Entity.Id, starSys, MouseButtons.Primary);
                }
                ImGui.Separator();

                var count = _subEntities.Count();

                // Display all _subEntities in a flat list, separated by type
                for(int i = 0; i < count; i++)
                {
                    var itm = _subEntities.ElementAt(i);

                    // Add a type header if there are multiple types
                    if(count > 1)
                    {
                        ImGui.TextDisabled(itm.Key.ToString());
                    }

                    foreach(var s in itm)
                    {
                        var nam = Utils.EntityName(s);
                        if(ImGui.MenuItem(nam))
                        {
                            state.EntityClicked(s.Id, starSys, MouseButtons.Primary);
                        }
                        if (ImGui.IsItemHovered())
                        {
                            DisplayHelpers.DescriptiveTooltipRaw(
                                    nam,
                                    Utils.EntityBodyType(s).ToString(),
                                    "",
                                    TooltipCallback(s),
                                    hideDescriptionColor: true);
                        }
                    }

                    // Add separator between groups (but not after the last one)
                    if(i < count - 1)
                    {
                        ImGui.Separator();
                    }
                }

                ImGui.End();
            }
            // TODO: make this nicer
            else if (_subEntities.Any()) // There are sub entities, but not hovered. Draw an icon to indicate that there's a menu that can be opened.
            {
                _dropDownRect.X = _nameRect.X + _nameRect.W;
                _dropDownRect.Y = _nameRect.Y;

                byte r, g, b, a;
                SDL.GetRenderDrawColor(rendererPtr, out r, out g, out b, out a);

                SDL.SetRenderDrawColor(rendererPtr, 255, 255, 0, 255);
                SDL.RenderFillRect(rendererPtr, _dropDownRect);

                SDL.SetRenderDrawColor(rendererPtr, r, g, b ,a);
            }
            else if (_hovered) // We are hovered but don't have any sub entities. Display a tooltip.
            {
                // Display the tooltip
                DisplayHelpers.DescriptiveTooltipRaw(
                        _name,
                        Utils.EntityBodyType(Entity).ToString(),
                        "",
                        TooltipCallback(Entity),
                        hideDescriptionColor: true);
            }
        }
    }
}
