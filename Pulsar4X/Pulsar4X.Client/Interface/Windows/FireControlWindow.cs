using System;
using System.Collections.Generic;
using System.Linq;
using ImGuiNET;
using Pulsar4X.Api;
using Pulsar4X.Client.Interface.Widgets;
using Vector2 = System.Numerics.Vector2;

namespace Pulsar4X.Client
{

    public class FireControl : PulsarGuiWindow
    {
        private int? _entityId;
        private string? _systemId;
        private bool _showOwnAsTarget;

        private string? _dragDropWeaponId;
        private string? _dragDropOrdnanceId;
        private int _dragDropTargetId;

        private FireControl()
        {
            _flags = ImGuiWindowFlags.None;
        }

        public static FireControl GetInstance(EntityState orderEntity)
        {
            FireControl thisitem;
            if (!_uiState.LoadedWindows.ContainsKey(typeof(FireControl)))
            {
                thisitem = new FireControl();
            }
            else
            {
                thisitem = (FireControl)_uiState.LoadedWindows[typeof(FireControl)];
            }
            if (orderEntity.StarSystemId != null)
                thisitem.SetEntity(orderEntity.Entity.Id, orderEntity.StarSystemId);

            return thisitem;
        }

        public void SetEntity(int entityId, string systemId)
        {
            _entityId = entityId;
            _systemId = systemId;
        }

        internal override void Display()
        {
            if (!IsActive)
                return;

            var system = _systemId != null ? _uiState.GameClient?.Galaxy.GetSystem(_systemId) : null;
            var entity = _entityId is int entityId ? system?.GetEntity(entityId) : null;
            var fireControl = entity?.GetView<FireControlView>();

            ImGui.SetNextWindowSize(new Vector2(600f, 400f), ImGuiCond.FirstUseEver);
            if (Window.Begin("Fire Control", ref IsActive, _flags))
            {
                if (system != null && entity != null && fireControl != null)
                {
                    var weaponsById = fireControl.Weapons.ToDictionary(w => w.Id);

                    ImGui.Columns(2);
                    ImGui.SetColumnWidth(0, 400);
                    DisplayFC(entity, fireControl, weaponsById);

                    UnAssignedWeapons(entity, fireControl, weaponsById);

                    ImGui.NewLine();

                    DisplayOrdnance(fireControl);

                    ImGui.NextColumn();

                    DisplayTargetColumn(system, entity);
                }
                else
                {
                    ImGui.TextColored(Styles.DescriptiveColor, "No fire controls on the selected entity.");
                }
            }
            Window.End();
        }

        void DisplayFC(EntitySnapshot entity, FireControlView fireControl, Dictionary<string, WeaponSnapshot> weaponsById)
        {
            int fcindex = 0;
            foreach (var fc in fireControl.FireControls)
            {
                var startPoint = ImGui.GetCursorPos();
                BorderGroup.Begin(fc.Name + "##" + fcindex++);

                if (fc.TargetId != null)
                {
                    ImGui.Text(fc.TargetName ?? "Unknown");
                    if (fc.IsEngaging)
                    {
                        if (ImGui.Button("Cease Fire"))
                            SubmitCommand(new SetFireModeCommand(entity.Id, fc.Id, OpenFire: false));
                    }
                    else
                    {
                        if (ImGui.Button("Open Fire"))
                            SubmitCommand(new SetFireModeCommand(entity.Id, fc.Id, OpenFire: true));
                    }
                }
                else
                {
                    ImGui.PushStyleColor(ImGuiCol.Text, Styles.BadColor);
                    ImGui.Text("No target selected");
                    ImGui.PopStyleColor();
                }

                foreach (var weaponId in fc.AssignedWeaponIds)
                {
                    if (weaponsById.TryGetValue(weaponId, out var weapon))
                        ShowWeapon(entity, weapon);
                }

                BorderGroup.End();
                ImGui.SetCursorPos(startPoint);
                ImGui.InvisibleButton("fcddarea" + fcindex, BorderGroup.GetSize);

                if (ImGui.BeginDragDropTarget())
                {
                    var acceptPayload = ImGui.AcceptDragDropPayload("AssignAsTarget");
                    bool isDroppingSensorTarget = false;
                    unsafe
                    {
                        isDroppingSensorTarget = acceptPayload.NativePtr != null;
                    }

                    acceptPayload = ImGui.AcceptDragDropPayload("AssignWeapon");
                    bool isDroppingWeapon = false;
                    unsafe
                    {
                        isDroppingWeapon = acceptPayload.NativePtr != null;
                    }

                    if (isDroppingSensorTarget)
                        SubmitCommand(new SetFireControlTargetCommand(entity.Id, fc.Id, _dragDropTargetId));
                    if (isDroppingWeapon && _dragDropWeaponId != null && !fc.AssignedWeaponIds.Contains(_dragDropWeaponId))
                    {
                        var weaponIds = fc.AssignedWeaponIds.ToList();
                        weaponIds.Add(_dragDropWeaponId);
                        SubmitCommand(new SetFireControlWeaponsCommand(entity.Id, fc.Id, weaponIds));
                    }
                    ImGui.EndDragDropTarget();
                }
                ImGui.NewLine();
            }

        }

        string GetRichWeaponName(WeaponSnapshot weapon)
        {
            string weaponname = weapon.Name + "\t";
            if (weapon.OrdnanceName != null)
            {
                weaponname += weapon.OrdnanceName;
                weaponname += "(" + weapon.OrdnanceStored + ")";
            }
            return weaponname;
        }

        void ShowWeapon(EntitySnapshot entity, WeaponSnapshot weapon, int i = 0)
        {
            int nameSize = 128;

            var cpos = ImGui.GetCursorPos();

            ImGui.Text(GetRichWeaponName(weapon));
            var selectableSize = new Vector2(ImGui.GetColumnWidth(0) - 24, ImGui.GetTextLineHeightWithSpacing());
            Vector2 progsize = new Vector2(selectableSize.X - nameSize, selectableSize.Y);
            float reloadAmountPerc = weapon.MagazineSize > 0 ? (float)weapon.MagazineCurrent / weapon.MagazineSize : 0;
            ImGui.SetCursorPos(new Vector2(nameSize, cpos.Y));
            ImGui.ProgressBar(reloadAmountPerc, progsize);

            //draw an invisible button over everything for the drag and drop source.
            ImGui.SetCursorPos(cpos);
            ImGui.InvisibleButton(weapon.Id, selectableSize);

            if (ImGui.BeginDragDropSource())
            {
                ImGui.Text(weapon.Name);
                unsafe
                {
                    int* tesnum = &i;
                    ImGui.SetDragDropPayload("AssignWeapon", new IntPtr(tesnum), sizeof(int));
                    _dragDropWeaponId = weapon.Id;
                }

                ImGui.EndDragDropSource();
            }

            if (ImGui.BeginDragDropTarget())
            {
                ImGuiPayloadPtr acceptPayload = ImGui.AcceptDragDropPayload("AssignOrdnance");
                bool isDropping = false;
                unsafe
                {
                    isDropping = acceptPayload.NativePtr != null;
                }

                if (isDropping && _dragDropOrdnanceId != null)
                    SubmitCommand(new AssignOrdnanceCommand(entity.Id, weapon.Id, _dragDropOrdnanceId));

                ImGui.EndDragDropTarget();
            }
        }

        void UnAssignedWeapons(EntitySnapshot entity, FireControlView fireControl, Dictionary<string, WeaponSnapshot> weaponsById)
        {
            Vector2 unAssStartPos = ImGui.GetCursorPos();
            BorderGroup.Begin("Un-Assigned Weapons");
            {
                foreach (var weapon in fireControl.Weapons.Where(w => w.FireControlId == null))
                    ShowWeapon(entity, weapon);
            }
            BorderGroup.End();
            var unAssSize = BorderGroup.GetSize;

            ImGui.SetCursorPos(unAssStartPos);
            ImGui.InvisibleButton("unassDnDArea", unAssSize);

            if (ImGui.BeginDragDropTarget())
            {

                var acceptPayload = ImGui.AcceptDragDropPayload("AssignWeapon");
                bool isDroppingWeapon = false;
                unsafe
                {
                    isDroppingWeapon = acceptPayload.NativePtr != null;
                }

                if (isDroppingWeapon && _dragDropWeaponId != null
                    && weaponsById.TryGetValue(_dragDropWeaponId, out var dropped)
                    && dropped.FireControlId is { } holdingFc)
                {
                    var remaining = fireControl.FireControls
                        .First(fc => fc.Id == holdingFc)
                        .AssignedWeaponIds.Where(id => id != _dragDropWeaponId).ToList();
                    SubmitCommand(new SetFireControlWeaponsCommand(entity.Id, holdingFc, remaining));
                }

                ImGui.EndDragDropTarget();
                ImGui.NewLine();
            }
        }

        void DisplayOrdnance(FireControlView fireControl)
        {
            BorderGroup.Begin("Ordnance");
            {
                for (int i = 0; i < fireControl.Ordnance.Count; i++)
                {
                    var ord = fireControl.Ordnance[i];
                    ImGui.Selectable($"{ord.Name} ({ord.Stored})");
                    if (ImGui.BeginDragDropSource())
                    {
                        ImGui.Selectable(ord.Name);
                        unsafe
                        {
                            int* tesnum = &i;
                            ImGui.SetDragDropPayload("AssignOrdnance", new IntPtr(tesnum), sizeof(int));
                            _dragDropOrdnanceId = ord.Id;
                        }

                        ImGui.EndDragDropSource();
                    }
                }
            }
            BorderGroup.End();
        }

        void DisplayTargetColumn(IClientSystem system, EntitySnapshot entity)
        {
            BorderGroup.Begin("Set Target:");
            ImGui.Checkbox("Show Own", ref _showOwnAsTarget);

            ImGui.PushStyleColor(ImGuiCol.Text, Styles.BadColor);
            foreach (var target in system.Entities)
            {
                if (target.Relation == OwnerRelation.Hostile && target.HasView<PositionView>())
                    DisplayTarget(target);
            }
            ImGui.PopStyleColor();

            if (_showOwnAsTarget)
            {
                foreach (var target in system.Entities)
                {
                    if (target.Id == entity.Id) continue;
                    if ((target.Relation == OwnerRelation.Owned || target.Relation == OwnerRelation.Friendly)
                        && target.HasView<PositionView>())
                        DisplayTarget(target);
                }
            }

            BorderGroup.End();
        }

        private void DisplayTarget(EntitySnapshot target)
        {
            int i = target.Id;
            string name = target.GetView<NameView>()?.Name ?? "Unknown";
            ImGui.Selectable(name + "###target" + target.Id);
            if (ImGui.BeginDragDropSource())
            {
                ImGui.Text(name);
                unsafe
                {
                    int* tesnum = &i;
                    ImGui.SetDragDropPayload("AssignAsTarget", new IntPtr(tesnum), sizeof(int));
                    _dragDropTargetId = target.Id;
                }

                ImGui.EndDragDropSource();
            }
        }

        private void SubmitCommand(GameCommand command) => _uiState.GameClient?.SubmitCommandAsync(command);
    }
}
