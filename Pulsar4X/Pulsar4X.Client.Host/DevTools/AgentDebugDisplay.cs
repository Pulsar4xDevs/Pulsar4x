using System;
using System.Collections.Generic;
using System.Linq;
using GameEngine.Engine.Orders;
using ImGuiNET;
using Pulsar4X.Engine;
using Pulsar4X.Fleets;
using Pulsar4X.Industry;
using Pulsar4X.Names;

namespace Pulsar4X.Client
{
    /// <summary>
    /// Readable agent state for the debug entity inspector.
    /// Reflection on <see cref="GoalsDB"/> only prints the goal type name.
    /// </summary>
    public static class AgentDebugDisplay
    {
        public static bool Applies(Entity entity)
        {
            return entity.HasDataBlob<GoalsDB>()
                || entity.HasDataBlob<ActionQueueDB>()
                || entity.HasDataBlob<AgentDB>();
        }

        public static void Display(Entity entity, string dateFormat)
        {
            try
            {
                ShowWake(entity, dateFormat);
                ShowTraits(entity);

                entity.TryGetDataBlob<GoalsDB>(out var goals);
                var given = goals?.GivenGoal;
                var active = goals?.ActiveGoal;

                if (given == null && active == null)
                {
                    ImGui.Text("No goal");
                }
                else if (ReferenceEquals(given, active) && active != null)
                {
                    ImGui.Separator();
                    ImGui.Text("Given and active");
                    ShowGoal(entity, active);
                }
                else
                {
                    if (given != null)
                    {
                        ImGui.Separator();
                        ImGui.Text("Given");
                        ShowGoal(entity, given);
                    }
                    if (active != null)
                    {
                        ImGui.Separator();
                        ImGui.Text("Active");
                        ShowGoal(entity, active);
                    }
                }

                if (active != null)
                    ShowHandedOut(entity, active);

                ShowActions(entity, given, active, dateFormat);
                ShowLines(entity);
            }
            catch (Exception ex)
            {
                ImGui.TextWrapped(ex.GetType().Name + ": " + ex.Message);
            }
        }

        static void ShowWake(Entity entity, string dateFormat)
        {
            if (entity.Manager == null)
            {
                ImGui.Text("No manager");
                return;
            }

            var upcoming = new List<(DateTime when, string processor)>();
            foreach (var item in entity.Manager.ManagerSubpulses.InstanceProcessorsQueue)
            {
                if (item.Item.Item2.Id != entity.Id)
                    continue;
                upcoming.Add((item.Time, item.Item.Item1));
            }

            if (upcoming.Count == 0)
            {
                ImGui.Text("No interrupt queued");
                return;
            }

            int shown = Math.Min(upcoming.Count, 6);
            for (int i = 0; i < shown; i++)
            {
                ImGui.Text(upcoming[i].when.ToString(dateFormat) + "  " + upcoming[i].processor);
            }
            if (upcoming.Count > shown)
                ImGui.Text("+" + (upcoming.Count - shown) + " later interrupts");
        }

        static void ShowTraits(Entity entity)
        {
            if (!entity.TryGetDataBlob<AgentDB>(out var agent))
            {
                ImGui.Text("No AgentDB");
                return;
            }

            ImGui.Text(
                "Aggression " + agent.Aggression.ToString("0.##")
                + "   Caution " + agent.Caution.ToString("0.##")
                + "   Curiosity " + agent.Curiosity.ToString("0.##")
                + "   Greed " + agent.Greed.ToString("0.##")
                + "   Loyalty " + agent.Loyalty.ToString("0.##"));
        }

        static void ShowGoal(Entity entity, Goal goal)
        {
            string name = string.IsNullOrEmpty(goal.Name) ? goal.Type.ToString() : goal.Name;
            ImGui.Text(name + "   " + goal.Status + "   " + goal.Type);
            if (!string.IsNullOrEmpty(goal.Message))
                ImGui.TextWrapped(goal.Message);

            ImGui.Text("Id " + Short(goal.Id));
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip(goal.Id);

            if (!string.IsNullOrEmpty(goal.ParentGoalId))
            {
                ImGui.Text("Parent " + Short(goal.ParentGoalId));
                if (ImGui.IsItemHovered())
                    ImGui.SetTooltip(goal.ParentGoalId);
            }

            if (goal.TargetEntityID >= 0)
                ImGui.Text("Target " + EntityLabel(entity, goal.TargetEntityID));

            if (!string.IsNullOrEmpty(goal.CargoId) || goal.SourceEntityId >= 0 || goal.DestEntityId >= 0 || goal.UnitShare > 0)
            {
                ImGui.Text("Cargo " + (string.IsNullOrEmpty(goal.CargoId) ? "(none)" : goal.CargoId));
                if (goal.SourceEntityId >= 0)
                    ImGui.Text("From " + EntityLabel(entity, goal.SourceEntityId));
                if (goal.DestEntityId >= 0)
                    ImGui.Text("To " + EntityLabel(entity, goal.DestEntityId));
                if (goal.UnitShare > 0)
                    ImGui.Text("Share " + goal.UnitShare);
            }

            if (goal.Type == GoalType.SupplyLocal)
                ImGui.Text("Supply " + goal.SupplyMode);

            ImGui.Text("Weight " + goal.Weight.ToString("0.###"));
        }

        static void ShowHandedOut(Entity entity, Goal goal)
        {
            if (entity.Manager == null || string.IsNullOrEmpty(goal.Id))
                return;

            var rows = new List<(int id, string who, string slot, Goal child)>();
            var seen = new HashSet<(int id, string slot)>();
            foreach (var other in entity.Manager.GetAllEntitiesWithDataBlob<GoalsDB>())
                Consider(entity.Id, other, goal.Id, rows, seen);

            if (entity.TryGetDataBlob<FleetDB>(out var fleet))
            {
                foreach (var child in fleet.Children.ToArray())
                    Consider(entity.Id, child, goal.Id, rows, seen);
            }

            if (rows.Count == 0)
                return;

            rows.Sort((a, b) => string.Compare(a.who, b.who, StringComparison.Ordinal));
            ImGui.Separator();
            ImGui.Text("Handed out");
            foreach (var row in rows)
            {
                string name = string.IsNullOrEmpty(row.child.Name) ? row.child.Type.ToString() : row.child.Name;
                ImGui.Text(row.who + "   " + row.slot + "   " + name + "   " + row.child.Status);
                if (!string.IsNullOrEmpty(row.child.Message))
                    ImGui.TextWrapped(row.child.Message);
            }
        }

        static void Consider(int ownerId, Entity other, string parentId, List<(int id, string who, string slot, Goal child)> rows, HashSet<(int id, string slot)> seen)
        {
            if (other.Id == ownerId || !other.TryGetDataBlob<GoalsDB>(out var childGoals))
                return;

            if (childGoals.ActiveGoal != null
                && childGoals.ActiveGoal.ParentGoalId == parentId
                && seen.Add((other.Id, "active")))
                rows.Add((other.Id, Who(other), "active", childGoals.ActiveGoal));

            if (childGoals.GivenGoal != null
                && !ReferenceEquals(childGoals.GivenGoal, childGoals.ActiveGoal)
                && childGoals.GivenGoal.ParentGoalId == parentId
                && seen.Add((other.Id, "given")))
                rows.Add((other.Id, Who(other), "given", childGoals.GivenGoal));
        }

        static void ShowActions(Entity entity, Goal? given, Goal? active, string dateFormat)
        {
            ImGui.Separator();
            if (!entity.TryGetDataBlob<ActionQueueDB>(out var queue) || queue.ActionList.Count == 0)
            {
                ImGui.Text("No actions queued");
                return;
            }

            var actions = queue.ActionList.ToArray();
            ImGui.Text("Actions " + actions.Length);
            if (!ImGui.BeginTable("agent-actions", 4, Styles.TableFlags))
                return;

            ImGui.TableSetupColumn("Status");
            ImGui.TableSetupColumn("Action");
            ImGui.TableSetupColumn("Detail");
            ImGui.TableSetupColumn("When");
            ImGui.TableHeadersRow();

            foreach (var action in actions)
            {
                ImGui.TableNextRow();
                ImGui.TableSetColumnIndex(0);
                ImGui.Text(action.Status.ToString());
                ImGui.TableSetColumnIndex(1);
                ImGui.Text(action.Name);
                if (ImGui.IsItemHovered())
                    ImGui.SetTooltip(LaneText(action) + "\n" + ParentLabel(action, given, active) + "\n" + action.GetType().Name);
                ImGui.TableSetColumnIndex(2);
                ImGui.Text(ActionDetail(action));
                ImGui.TableSetColumnIndex(3);
                ImGui.Text(action.ActionOnDate.Year < 2 ? "" : action.ActionOnDate.ToString(dateFormat));
            }

            ImGui.EndTable();
        }

        static void ShowLines(Entity entity)
        {
            if (!entity.TryGetDataBlob<IndustryAbilityDB>(out var industry) || industry.ProductionLines.Count == 0)
                return;

            ImGui.Separator();
            ImGui.Text("Production lines");
            foreach (var pair in industry.ProductionLines.ToArray())
            {
                var line = pair.Value;
                if (line.Jobs == null || line.Jobs.Count == 0)
                {
                    ImGui.Text(LineName(pair.Key, line) + "   idle");
                    continue;
                }

                foreach (var job in line.Jobs.ToArray())
                {
                    string item = string.IsNullOrEmpty(job.ItemGuid) ? "(no item)" : job.ItemGuid;
                    string auto = job.Auto ? "   auto" : "";
                    ImGui.Text(LineName(pair.Key, line) + "   " + item + "   " + job.NumberCompleted + "/" + job.NumberOrdered + auto);
                }
            }
        }

        static string LineName(string id, IndustryAbilityDB.ProductionLine line)
        {
            return string.IsNullOrEmpty(line.Name) ? id : line.Name;
        }

        static string ActionDetail(EntityAction action)
        {
            if (action is IndustryOrder2 job)
            {
                string item = string.IsNullOrEmpty(job.ItemID) ? job.OrderType.ToString() : job.ItemID;
                return job.AutoAddSubJobs ? item + " +subjobs" : item;
            }

            if (!string.IsNullOrEmpty(action.Details))
                return action.Details;
            return action.GetType().Name;
        }

        static string LaneText(EntityAction action)
        {
            var lanes = action.ActionLanes;
            var parts = new List<string>();
            if (lanes == EntityAction.ActionLaneTypes.InstantOrder)
                parts.Add("instant");
            else
            {
                if (lanes.HasFlag(EntityAction.ActionLaneTypes.Movement))
                    parts.Add("movement");
                if (lanes.HasFlag(EntityAction.ActionLaneTypes.InteractWithExternalEntity))
                    parts.Add("external");
                if (lanes.HasFlag(EntityAction.ActionLaneTypes.InteractWithEntitySameFleet))
                    parts.Add("fleet");
                if (lanes.HasFlag(EntityAction.ActionLaneTypes.InteractWithSelf))
                    parts.Add("self");
            }
            if (!action.UseActionLanes)
                parts.Add("runs on submit");
            if (action.IsBlocking)
                parts.Add("blocking");
            return string.Join(", ", parts);
        }

        static string ParentLabel(EntityAction action, Goal? given, Goal? active)
        {
            if (string.IsNullOrEmpty(action.ParentGoalId))
                return "no parent goal";
            if (active != null && action.ParentGoalId == active.Id)
                return "parent: active goal";
            if (given != null && action.ParentGoalId == given.Id)
                return "parent: given goal";
            return "parent " + action.ParentGoalId;
        }

        static string EntityLabel(Entity owner, int id)
        {
            if (owner.Manager != null && owner.Manager.TryGetGlobalEntityById(id, out var found) && found.IsValid)
                return Who(found) + " (" + id + ")";
            return id.ToString();
        }

        static string Who(Entity entity)
        {
            if (entity.TryGetDataBlob<NameDB>(out var name) && !string.IsNullOrEmpty(name.DefaultName))
                return name.DefaultName;
            return entity.DebuggerDisplay;
        }

        static string Short(string id)
        {
            if (string.IsNullOrEmpty(id) || id.Length <= 8)
                return id;
            return id.Substring(0, 8);
        }
    }
}
