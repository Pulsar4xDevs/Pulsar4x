using ImGuiNET;
using Pulsar4X.Api;
using Pulsar4X.Client.Interface;
using SDL3;
using System.Collections.Generic;
using System.Linq;
using System;

namespace Pulsar4X.Client
{
    public class EntityLabelExtCombo : EntityLabel
    {
        private bool _hoverOpen = false;

        private SDL.FRect _dropDownRect = new ();

        private IOrderedEnumerable<IGrouping<UserOrbitSettings.OrbitBodyType, EntityLabel>> _subEntities;

        public EntityLabelExtCombo(GlobalUIState state, EntitySnapshot entity, string systemId, IEnumerable<EntityLabel>? subEntities = null)
            : base(state, entity, systemId)
        {
            SetEntities(subEntities ?? []);

            _dropDownRect.W = 5;
            _dropDownRect.H = 5;
        }

        public void SetEntities(IEnumerable<EntityLabel> subEntities)
        {
            _subEntities = subEntities
                .GroupBy(x => x.BodyType)
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

        private Action? TooltipCallback(int entityId)
        {
            var system = _state.GameClient?.Galaxy.GetSystem(SystemId);
            var entity = system?.GetEntity(entityId);
            if (system == null || entity == null)
                return null;

            if (entity.GetView<GravSurveyView>() is { } gravSurvey && !entity.HasView<BodyView>())
                return () => Displays.GravitationalAnomlay(gravSurvey);
            if (entity.HasView<ShipView>() && entity.GetView<ThrustView>() is { } thrust)
                return () => Displays.Ship(thrust);
            if (entity.HasView<BodyView>() && entity.Kind != BodyKind.Star)
                return () => Displays.SystemBody(entity, system);
            if (entity.GetView<StarView>() is { } star)
                return () => Displays.Star(star);
            return null;
        }

        protected override void DrawExt(IntPtr rendererPtr, Camera camera)
        {
            // Alt click
            if (_clickedAlt)
            {
                _state.ContextMenu = new EntityContextMenu(_state, EntityId);
                ImGui.OpenPopup(_name + "##Alt");
                _clickedAlt = false;
            }
            if(ImGui.BeginPopupContextItem(_name + "##Alt"))
            {
                _state.ContextMenu.Display();
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
                    _state.EntityClicked(EntityId, SystemId, MouseButtons.Primary);
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
                        if(ImGui.MenuItem(s.Name))
                        {
                            _state.EntityClicked(s.EntityId, SystemId, MouseButtons.Primary);
                        }
                        if (ImGui.IsItemHovered())
                        {
                            DisplayHelpers.DescriptiveTooltipRaw(
                                    s.Name,
                                    s.BodyType.ToString(),
                                    "",
                                    TooltipCallback(s.EntityId),
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
                        BodyType.ToString(),
                        "",
                        TooltipCallback(EntityId),
                        hideDescriptionColor: true);
            }
        }
    }
}
