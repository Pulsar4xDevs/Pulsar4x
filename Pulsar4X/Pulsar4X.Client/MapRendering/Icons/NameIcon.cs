﻿using System;
using System.Collections.Generic;
using System.Linq;
using ImGuiNET;
using Pulsar4X.Engine;
using Pulsar4X.Datablobs;
using SDL2;
using System.Numerics;

namespace Pulsar4X.SDL2UI
{
    public class NameIcon : Icon, IComparable<NameIcon>, IRectangle
    {

        protected ImGuiWindowFlags _flags = ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoSavedSettings | ImGuiWindowFlags.NoBringToFrontOnFocus;
        internal bool IsActive = true;
        GlobalUIState _state;
        EntityState _entityState;
        NameDB _nameDB;
        internal string NameString;
        public float Width { get; set; }
        public float Height{ get; set; }
        public float X { get { return ViewScreenPos.x; }  }
        public float Y { get { return ViewScreenPos.y; } }
        int _entityGuid;
        string _starSysGuid;
        public Dictionary<int, string> SubNames = new ();
        public System.Numerics.Vector2 ViewOffset { get; set; } = new System.Numerics.Vector2();
        public Rectangle ViewDisplayRect = new Rectangle();
        UserOrbitSettings.OrbitBodyType _bodyType = UserOrbitSettings.OrbitBodyType.Unknown;
        internal float DrawAtZoom { get { return _state.DrawNameZoomLvl[_bodyType]; } }

        public Vector4 TextDisplayColor { get; private set; } = Styles.StandardText;

        public NameIcon(EntityState entityState, GlobalUIState state) : base(entityState.Entity.GetDataBlob<PositionDB>())
        {
            Random rnd = new Random();
            _state = state;
            _entityState = entityState;
            _entityGuid = entityState.Entity.Id;
            StarSystem starsys = (StarSystem)entityState.Entity.Manager;
            _starSysGuid = starsys.Guid;
            _nameDB = entityState.Entity.GetDataBlob<NameDB>();
            NameString = _nameDB.GetName(state.Faction);
            entityState.Name = NameString;
            entityState.NameIcon = this;
            _bodyType = entityState.BodyType;
            if(entityState.Entity.FactionOwnerID == Game.NeutralFactionId)
            {
                TextDisplayColor = Styles.NeutralColor;
            }
            else if(entityState.Entity.FactionOwnerID != _state.Faction.Id)
            {
                TextDisplayColor = Styles.BadColor;
            }
        }


        public static NameIcon operator +(NameIcon nameIcon, SDL.SDL_Point point)
        {
            System.Numerics.Vector2 newpoint = new System.Numerics.Vector2()
            {
                X = nameIcon.ViewOffset.X + point.x,
                Y = nameIcon.ViewOffset.Y + point.y
            };
            nameIcon.ViewOffset = newpoint;

            return nameIcon;

        }

        //adds or updates the subname - this is mostly used for colonys on a planet
        public void AddSubName(Entity entity)
        {
            var nameString = entity.GetDataBlob<NameDB>().GetName(_state.Faction);
            SubNames[entity.Id] = nameString;
        }
        public void RemoveSubName(int guid)
        {
            SubNames.Remove(guid);
        }

        public override void OnFrameUpdate(Matrix matrix, Camera camera)
        {
            //DefaultViewOffset = new SDL.SDL_Point() { x = Width, y = -Height };
            if (camera.ZoomLevel < DrawAtZoom)
                return;
            System.Numerics.Vector2 defualtOffset = new System.Numerics.Vector2(4,-(Height / 2));
            ViewOffset = defualtOffset;
            base.OnFrameUpdate(matrix, camera);

            ViewDisplayRect.X = ViewScreenPos.x;
            ViewDisplayRect.Y = ViewScreenPos.y;

        }

        /// <summary>
        /// Default comparer, based on worldposition. TODO: should this maybe be done on viewscreen position?
        /// Sorts Bottom to top, left to right, then alphabetically
        /// </summary>
        /// <param name="compareIcon"></param>
        /// <returns></returns>
        public int CompareTo(NameIcon compareIcon)
        {

            if (WorldPosition_AU.Y > compareIcon.WorldPosition_AU.Y) return -1;
            else if (this.WorldPosition_AU.Y < compareIcon.WorldPosition_AU.Y) return 1;
            else
            {
                if (this.WorldPosition_AU.X > compareIcon.WorldPosition_AU.X) return 1;
                else if (this.WorldPosition_AU.X < compareIcon.WorldPosition_AU.X) return -1;
                else return -NameString.CompareTo(compareIcon.NameString);
            }
        }

        private void SetUpContextMenu(int entityGuid)
        {
            _state.EntityClicked(entityGuid, _starSysGuid, MouseButtons.Alt);
            _state.ContextMenu = new EntityContextMenu(_state, entityGuid);
            _state.ContextMenu.Display();
        }


        //use to correctly draw all passed name icons
        public static void DrawAll(IntPtr rendererPtr, Camera camera, List<NameIcon> nameIcons)
        {
            List<List<NameIcon>> nameIconGroupings = new List<List<NameIcon>>();
            List<bool> alreadyGroupedItems = new List<bool>();
            alreadyGroupedItems.Resize<bool>(nameIcons.Count, false);

            int iterations = 0;
            foreach(var nameIcon in nameIcons)
            {
                if(!alreadyGroupedItems[iterations])
                {
                    nameIconGroupings.Add(new List<NameIcon>());
                    nameIconGroupings[nameIconGroupings.Count -1].Add(nameIcon);
                    alreadyGroupedItems[iterations] = true;
                    int nestedIterations = 0;
                    foreach(var nestedNameIcon in nameIcons)
                    {
                        if(iterations != nestedIterations && !alreadyGroupedItems[nestedIterations])
                        {
                            //check if two names are within the same pixel of distance, if so groups them together into a single window to prevent name overlapping.
                            var xDistance = Helpers.GetSingleDistanceSquared(nameIcon.X, nestedNameIcon.X);
                            var yDistance = Helpers.GetSingleDistanceSquared(nameIcon.Y, nestedNameIcon.Y);

                            if(yDistance < 256 && xDistance < 9216)
                            {
                                nameIconGroupings[nameIconGroupings.Count -1].Add(nestedNameIcon);
                                alreadyGroupedItems[nestedIterations] = true;
                            }
                        }
                        nestedIterations++;
                    }
                }
                iterations++;
            }

            foreach(var nameIconGrouping in nameIconGroupings)
            {
                var orderedGroupedIcons = nameIconGrouping.GroupBy(i => i._bodyType).OrderBy(g => g.Key).ToList();
                var highestPriorityGroup = orderedGroupedIcons.First().ToList();
                orderedGroupedIcons.RemoveAt(0);
                ImGui.PushStyleColor(ImGuiCol.ButtonHovered, Styles.NameIconHighlight);
                for(int i = 0; i < highestPriorityGroup.Count; i++)
                {
                    if(i == 0)
                        BeginNameIcon(highestPriorityGroup[i]);

                    DisplayNameIcon(camera, highestPriorityGroup[i], orderedGroupedIcons);

                    if(i == highestPriorityGroup.Count - 1)
                        EndNameIcon(highestPriorityGroup[i]);
                }
                ImGui.PopStyleColor();
            }
        }

        private static void DisplayNameIcon(Camera camera, NameIcon icon, List<IGrouping<UserOrbitSettings.OrbitBodyType, NameIcon>> subIcons)
        {
            if (camera.ZoomLevel < icon.DrawAtZoom)
                return;

            if(!subIcons.Any())
            {
                ImGui.PushStyleColor(ImGuiCol.Button, Styles.InvisibleColor);
                ImGui.PushStyleColor(ImGuiCol.Text, icon.TextDisplayColor);

                if (ImGui.Button(icon.NameString + "##" + icon._entityGuid.ToString()))
                {
                    icon._state.EntityClicked(icon._entityGuid, icon._starSysGuid, MouseButtons.Primary);
                }
                DisplayContextMenu(camera, icon);
                ImGui.PopStyleColor(2);
                return;
            }

            ImGui.PushStyleColor(ImGuiCol.Text, icon.TextDisplayColor);
            if(ImGui.BeginMenu(icon.NameString))
            {
                ImGui.PushStyleColor(ImGuiCol.Text, Styles.StandardText);
                if(ImGui.MenuItem("View " + icon.NameString))
                {
                    icon._state.EntityClicked(icon._entityGuid, icon._starSysGuid, MouseButtons.Primary);
                }
                ImGui.PopStyleColor();
                DisplayContextMenu(camera, icon);

                if(subIcons.Any())
                    ImGui.Separator();

                // If there is only a single type of subIcon it doesn't need to be buried in another menu
                if(subIcons.Count == 1)
                {
                    foreach(var subIcon in subIcons[0])
                    {
                        ImGui.PushStyleColor(ImGuiCol.Text, subIcon.TextDisplayColor);
                        if(ImGui.MenuItem(subIcon.NameString))
                        {
                            subIcon._state.EntityClicked(subIcon._entityGuid, subIcon._starSysGuid, MouseButtons.Primary);
                        }
                        ImGui.PopStyleColor();
                        DisplayContextMenu(camera, subIcon);
                    }
                }
                else
                {
                    foreach(var group in subIcons)
                    {
                        ImGui.PushStyleColor(ImGuiCol.Text, Styles.StandardText);
                        if(ImGui.BeginMenu(group.Key.ToString()))
                        {
                            foreach(var subIcon in group)
                            {
                                ImGui.PushStyleColor(ImGuiCol.Text, subIcon.TextDisplayColor);
                                if(ImGui.MenuItem(subIcon.NameString))
                                {
                                    subIcon._state.EntityClicked(subIcon._entityGuid, subIcon._starSysGuid, MouseButtons.Primary);
                                }
                                ImGui.PopStyleColor();
                                DisplayContextMenu(camera, subIcon);
                            }
                            ImGui.EndMenu();
                        }
                        ImGui.PopStyleColor();
                    }
                }
                ImGui.EndMenu();
            }
            ImGui.PopStyleColor();
        }

        private static void BeginNameIcon(NameIcon icon)
        {
            var yOffset = 10;
            var xOffset = icon._bodyType == UserOrbitSettings.OrbitBodyType.Star ? 14 : icon._bodyType == UserOrbitSettings.OrbitBodyType.Ship ? 4 : 10;
            System.Numerics.Vector2 pos = new System.Numerics.Vector2(icon.X + xOffset, icon.Y - yOffset);

            ImGui.PushStyleColor(ImGuiCol.WindowBg, Styles.InvisibleColor); //make the background transperent.

            ImGui.PushStyleVar(ImGuiStyleVar.WindowBorderSize, 0);
            ImGui.PushStyleVar(ImGuiStyleVar.WindowRounding, 2);

            ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new System.Numerics.Vector2(1, 2));
            ImGui.SetNextWindowPos(pos, ImGuiCond.Always);
            ImGui.Begin(icon.NameString, ref icon.IsActive, icon._flags | ImGuiWindowFlags.NoDocking);
            ImGui.PopStyleColor(); //have to pop the color change after pushing it.
            ImGui.PopStyleVar(3);
        }

        private static void EndNameIcon(NameIcon icon)
        {
            ImGui.End();
        }

        private static void DisplayContextMenu(Camera camera, NameIcon icon)
        {
            if(ImGui.BeginPopupContextItem())
            {
                icon.SetUpContextMenu(icon._entityGuid);
                ImGui.EndPopup();
            }
        }

        public override void Draw(IntPtr rendererPtr, Camera camera)
        {
            base.Draw(rendererPtr, camera);
            Draw(rendererPtr, camera, true);
        }

        //WARNING!!! DO NOT USE FOR BULK DRAWING, USE DRAWALL FOR BULK DRAWING^^;
        public void Draw(IntPtr rendererPtr, Camera camera, bool createNewWindow)
        {
            if (camera.ZoomLevel < DrawAtZoom)
                return;

            if(createNewWindow)
            {
                ImGui.PushStyleColor(ImGuiCol.WindowBg, Styles.InvisibleColor); //make the background transperent.
                ImGui.PushStyleVar(ImGuiStyleVar.WindowBorderSize, 0);
                ImGui.PushStyleVar(ImGuiStyleVar.WindowRounding, 2);
                ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new System.Numerics.Vector2(1, 2));
                ImGui.SetNextWindowPos(new System.Numerics.Vector2(X, Y), ImGuiCond.Always);
                ImGui.Begin(NameString, ref IsActive, _flags);
            }

            // if(ImGui.BeginMenu(NameString))
            // {
            //     ImGui.MenuItem("hello world");
            //     ImGui.EndMenu();
            // }

            ImGui.PushStyleColor(ImGuiCol.Button, Styles.InvisibleColor);
            if (ImGui.Button(NameString+"##"+_entityGuid.ToString())) //If the name gets clicked, we tell the state.
            {
                _state.EntityClicked(_entityGuid, _starSysGuid, MouseButtons.Primary);

            }

            if (ImGui.BeginPopupContextItem("NameContextMenu"+_entityGuid.ToString()+NameString, ImGuiPopupFlags.MouseButtonRight))
            {
                SetUpContextMenu(_entityGuid);
                ImGui.EndPopup();
            }
            //checks the state if the icon of the entity with this nameicon was altClicked, if yes then display the normal context menu
            if(_state._lastContextMenuOpenedEntityGuid == _entityGuid)
            {
                if(ImGui.BeginPopupContextVoid())
                {
                   SetUpContextMenu(_entityGuid);
                   ImGui.EndPopup();
                }
            }
            if(_state.StarSystemStates[_starSysGuid].EntityStatesWithNames[_entityGuid].Entity.HasDataBlob<JPSurveyableDB>() && _state.StarSystemStates.ContainsKey(_state.StarSystemStates[_starSysGuid].EntityStatesWithNames[_entityGuid].Entity.GetDataBlob<JPSurveyableDB>().SystemToGuid))
            {
                ImGui.Text("Jumps to: "+_state.StarSystemStates[_state.StarSystemStates[_starSysGuid].EntityStatesWithNames[_entityGuid].Entity.GetDataBlob<JPSurveyableDB>().SystemToGuid].StarSystem.NameDB.DefaultName);
            }

            foreach (var name in SubNames)
            {
                if (ImGui.Button(name.Value+"##"+_entityGuid.ToString()+name.Key.ToString()+name.Value))
                {
                    _state.EntityClicked(name.Key, _starSysGuid, MouseButtons.Primary);
                }
                if (ImGui.BeginPopupContextItem("subNameContextMenu"+name.Key+name.Value+_entityGuid.ToString()+NameString, ImGuiPopupFlags.MouseButtonRight))
                {
                    SetUpContextMenu(name.Key);

                    ImGui.EndPopup();
                }
                //checks the state if the icon of the entity with this subNameicon was altClicked, if yes then display the normal context menu for the the subname
                if(_state._lastContextMenuOpenedEntityGuid == name.Key)
                {
                    if(ImGui.BeginPopupContextVoid())
                    {
                        SetUpContextMenu(name.Key);
                        ImGui.EndPopup();
                    }
                }
            }

            //var size = ImGui.GetItemRectSize();
            var size = ImGui.GetWindowSize();
            Width = size.X;
            Height = size.Y;
            ViewDisplayRect.Width = size.X;
            ViewDisplayRect.Height = size.Y;

            ImGui.PopStyleColor();
            if(createNewWindow)
            {
               ImGui.PopStyleColor(); //have to pop the color change after pushing it.
               ImGui.PopStyleVar(3);
               ImGui.End();
            }
        }
    }

    /// <summary>
    /// IComparer for the Texticonrectangles (or any other rectangle)
    /// Sorts Bottom to top, left to right
    /// </summary>
    internal class ByViewPosition : IComparer<IRectangle>
    {
        public int Compare(IRectangle r1, IRectangle r2)
        {
            float r1B = r1.Y + r1.Height;
            float r1L = r1.X;
            float r2B = r2.Y + r1.Height;
            float r2L = r2.X;

            if (r1B > r2B) return -1;
            else if (r1B < r2B) return 1;
            else
            {
                if (r1L > r2L) return -1;
                else if (r1L < r2L) return 1;
                else return 0;
            }
        }
    }
}
