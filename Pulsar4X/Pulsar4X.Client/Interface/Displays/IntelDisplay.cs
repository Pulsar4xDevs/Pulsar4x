using System.Collections.Generic;
using System.Globalization;
using ImGuiNET;
using Pulsar4X.Api;

namespace Pulsar4X.Client
{
    /// <summary>
    /// Intel list under the cargo bars. The table prints the name and result tag it was given.
    /// </summary>
    public static class IntelDisplay
    {
        sealed class RowEdit
        {
            public double Ask;
            public bool ForSale;
        }

        static readonly Dictionary<string, RowEdit> Rows = new();
        static readonly Dictionary<int, bool> ShowAll = new();
        static readonly Dictionary<int, int> AddIndex = new();
        static readonly Dictionary<int, double> AddAsk = new();

        public static void Display(EntitySnapshot colony, IntelBookView? book, string officeSystemId, GlobalUIState uiState)
        {
            if (book == null)
                return;

            ImGui.Separator();
            ImGui.Text("Intel");

            bool all = ShowAll.TryGetValue(colony.Id, out bool show) && show;
            if (ImGui.BeginCombo("Show##intel-" + colony.Id, all ? "All" : "This system"))
            {
                if (ImGui.Selectable("This system", !all))
                    ShowAll[colony.Id] = false;
                if (ImGui.Selectable("All", all))
                    ShowAll[colony.Id] = true;
                ImGui.EndCombo();
            }
            all = ShowAll.TryGetValue(colony.Id, out show) && show;

            var rows = new List<IntelRowView>();
            foreach (var row in book.Rows)
            {
                if (ShowRow(row.SystemId, officeSystemId, all))
                    rows.Add(row);
            }

            if (rows.Count == 0)
            {
                ImGui.Text("No intel listed.");
            }
            else
            {
                const ImGuiTableFlags flags = ImGuiTableFlags.BordersInnerV | ImGuiTableFlags.RowBg | ImGuiTableFlags.SizingStretchProp;
                if (ImGui.BeginTable("##intel-" + colony.Id, 4, flags))
                {
                    ImGui.TableSetupColumn("Item", ImGuiTableColumnFlags.WidthFixed, 180f);
                    ImGui.TableSetupColumn("Result", ImGuiTableColumnFlags.WidthFixed, 140f);
                    ImGui.TableSetupColumn("Ask");
                    ImGui.TableSetupColumn("", ImGuiTableColumnFlags.WidthFixed, book.CanEdit ? 220f : 80f);
                    ImGui.TableHeadersRow();

                    bool idle = !ImGui.IsAnyItemActive();
                    foreach (var row in rows)
                    {
                        ImGui.TableNextRow();
                        ImGui.PushID(colony.Id + ":" + (int)row.Kind + ":" + row.Subject);
                        ImGui.TableNextColumn();
                        ImGui.Text(row.Name);
                        ImGui.TableNextColumn();
                        ImGui.Text(row.ResultTag);
                        ImGui.TableNextColumn();
                        if (book.CanEdit)
                        {
                            var edit = Edit(colony.Id, row, idle);
                            ImGui.SetNextItemWidth(100f);
                            ImGui.InputDouble("##ask", ref edit.Ask, 0, 0, "%.2f");
                            ImGui.TableNextColumn();
                            ImGui.Checkbox("For sale", ref edit.ForSale);
                            ImGui.SameLine();
                            if (ImGui.Button("Set"))
                                SubmitSet(colony.Id, row, edit, uiState);
                            ImGui.SameLine();
                            if (ImGui.Button("Remove"))
                                uiState.GameClient?.SubmitCommandAsync(new ClearIntelListingCommand(colony.Id, row.Subject, row.Kind));
                        }
                        else
                        {
                            ImGui.Text(row.Ask.ToString(CultureInfo.InvariantCulture));
                            ImGui.TableNextColumn();
                            if (row.OwnedByViewer)
                                ImGui.Text("Owned");
                            else if (ImGui.Button("Buy"))
                                uiState.GameClient?.SubmitCommandAsync(new BuyIntelCommand(colony.Id, row.Subject, row.Kind));
                        }
                        ImGui.PopID();
                    }
                    ImGui.EndTable();
                }
            }

            if (!book.CanEdit)
                return;

            var addable = new List<IntelCandidateView>();
            foreach (var candidate in book.Candidates)
            {
                if (!ShowRow(candidate.SystemId, officeSystemId, all))
                    continue;
                bool listed = false;
                foreach (var row in book.Rows)
                {
                    if (row.Kind == candidate.Kind && row.Subject == candidate.Subject)
                    {
                        listed = true;
                        break;
                    }
                }
                if (!listed)
                    addable.Add(candidate);
            }
            if (addable.Count == 0)
                return;

            int index = AddIndex.TryGetValue(colony.Id, out int stored) ? stored : 0;
            if (index < 0 || index >= addable.Count)
                index = 0;
            var selected = addable[index];
            ImGui.Separator();
            ImGui.Text("Add intel");
            if (ImGui.BeginCombo("Secret##intel-add-" + colony.Id, selected.Name))
            {
                for (int i = 0; i < addable.Count; i++)
                {
                    var choice = addable[i];
                    if (ImGui.Selectable(choice.Name + "###" + (int)choice.Kind + choice.Subject, i == index))
                        index = i;
                }
                ImGui.EndCombo();
            }
            AddIndex[colony.Id] = index;
            selected = addable[index];
            if (!AddAsk.TryGetValue(colony.Id, out double ask))
                ask = 0;
            ImGui.SetNextItemWidth(100f);
            ImGui.InputDouble("Ask##intel-add-ask-" + colony.Id, ref ask, 0, 0, "%.2f");
            AddAsk[colony.Id] = ask;
            if (ImGui.Button("Add##intel-" + colony.Id))
            {
                uiState.GameClient?.SubmitCommandAsync(new SetIntelListingCommand(
                    colony.Id, selected.Subject, selected.Kind, (decimal)ask, true));
                AddAsk[colony.Id] = 0;
            }
        }

        /// <summary>One offer line for a body or a grav pin. The table does not use this.</summary>
        public static void OfferLine(IClientSystem? system, int subjectId, IntelKind kind, string label, GlobalUIState? uiState)
        {
            if (!TryBest(system, subjectId, kind, out var offer) || offer.Owned)
                return;
            ImGui.Text(offer.SellerName + ", " + label + ", " + offer.Ask.ToString(CultureInfo.InvariantCulture));
            ImGui.SameLine();
            if (ImGui.Button("Buy##intel-offer-" + (int)kind + "-" + subjectId))
                uiState?.GameClient?.SubmitCommandAsync(new BuyIntelCommand(offer.ColonyId, offer.Subject, kind));
        }

        public static bool ListedPin(IClientSystem? system, int pinId)
            => TryBest(system, pinId, IntelKind.Pin, out var offer) && !offer.Owned;

        public readonly struct Offer
        {
            public int ColonyId { get; init; }
            public string Subject { get; init; }
            public string SellerName { get; init; }
            public decimal Ask { get; init; }
            public bool Owned { get; init; }
        }

        public static bool TryBest(IClientSystem? system, int subjectId, IntelKind kind, out Offer offer)
        {
            offer = default;
            if (system == null)
                return false;
            string subject = subjectId.ToString(CultureInfo.InvariantCulture);
            bool found = false;
            decimal best = 0;
            foreach (var colony in system.Entities)
            {
                if (colony.GetView<IntelBookView>() is not { } book)
                    continue;
                foreach (var row in book.Rows)
                {
                    if (row.Kind != kind || row.Subject != subject || !row.ForSale)
                        continue;
                    if (found && row.Ask >= best)
                        continue;
                    string seller = colony.GetView<NameView>()?.Name ?? "Colony";
                    offer = new Offer
                    {
                        ColonyId = colony.Id,
                        Subject = row.Subject,
                        SellerName = seller,
                        Ask = row.Ask,
                        Owned = row.OwnedByViewer,
                    };
                    best = row.Ask;
                    found = true;
                }
            }
            return found;
        }

        static bool ShowRow(string? systemId, string officeSystemId, bool all)
        {
            if (all)
                return true;
            return systemId != null && systemId == officeSystemId;
        }

        static RowEdit Edit(int colonyId, IntelRowView row, bool idle)
        {
            string key = colonyId + ":" + (int)row.Kind + ":" + row.Subject;
            if (idle || !Rows.TryGetValue(key, out var edit))
            {
                edit = new RowEdit { Ask = (double)row.Ask, ForSale = row.ForSale };
                Rows[key] = edit;
            }
            return edit;
        }

        static void SubmitSet(int colonyId, IntelRowView row, RowEdit edit, GlobalUIState uiState)
        {
            uiState.GameClient?.SubmitCommandAsync(new SetIntelListingCommand(
                colonyId, row.Subject, row.Kind, (decimal)edit.Ask, edit.ForSale));
        }
    }
}
