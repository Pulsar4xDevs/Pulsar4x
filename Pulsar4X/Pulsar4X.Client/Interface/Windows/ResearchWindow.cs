using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Numerics;
using ImGuiNET;
using Pulsar4X.Api;
using Pulsar4X.Client.Interface.Widgets;

namespace Pulsar4X.Client
{
    public class ResearchWindow : UniquePulsarGuiWindow<ResearchWindow>
    {
        private readonly Vector2 invisButtonSize = new (15, 15);

        // The lab is selected by entity id and re-resolved each frame: labs are entities in the
        // active system's snapshot, which is replaced wholesale by server pushes.
        private int? _selectedLabId = null;

        private int selectCategoryFilterIndex = 0;
        private int _showAssignmentModal = -1;

        // Derived lookups, rebuilt only when a new ResearchSnapshot is pushed (reference change).
        private ResearchSnapshot? _research;
        private string[] _categoryNames = Array.Empty<string>();
        private string[] _categoryIds = Array.Empty<string>();
        private Dictionary<string, TechSnapshot> _techsById = new ();
        private List<TechSnapshot> _researchableTechs = new ();

        private ResearchWindow()
        {
        }

        internal static ResearchWindow GetInstance()
        {
            if(_uiState.TryGetUniqueWindow<ResearchWindow>(out var window))
            {
                return window;
            }

            return _uiState.AddUniqueWindow<ResearchWindow>(new ResearchWindow());
        }

        private void RefreshDerivedData(ResearchSnapshot research)
        {
            _research = research;

            _categoryNames = new string[research.Categories.Count + 1];
            _categoryIds = new string[research.Categories.Count + 1];
            _categoryNames[0] = "All";
            _categoryIds[0] = "";
            for (int i = 0; i < research.Categories.Count; i++)
            {
                _categoryNames[i + 1] = research.Categories[i].Name;
                _categoryIds[i + 1] = research.Categories[i].Id;
            }

            if (selectCategoryFilterIndex >= _categoryIds.Length)
                selectCategoryFilterIndex = 0;

            _techsById = research.Techs.ToDictionary(t => t.Id);
            RefreshTechs();
        }

        private void RefreshTechs()
        {
            if (_research == null)
                return;

            string categoryId = _categoryIds[selectCategoryFilterIndex];
            _researchableTechs = _research.Techs
                .Where(t => t.IsResearchable && (categoryId.Length == 0 || t.CategoryId.Equals(categoryId)))
                .OrderBy(t => t.Name)
                .ToList();
        }

        internal override void Display()
        {
            if(!IsActive)
                return;

            var galaxy = _uiState.GameClient?.Galaxy;
            var research = galaxy?.Research;

            ImGui.SetNextWindowSize(new Vector2(1050, 675), ImGuiCond.FirstUseEver);
            using (var w = Window.Scope("Research and Development", ref IsActive, _flags))
            {
                if (w.Visible)
                {
                    if(galaxy != null && research != null)
                    {
                        if (!ReferenceEquals(research, _research))
                            RefreshDerivedData(research);

                        // Labs are the faction's researcher entities in the viewed system (the server only
                        // projects ResearcherView for the owning faction).
                        var system = galaxy.GetSystem(_uiState.SelectedStarSystemId);
                        var labs = system == null
                            ? new List<EntitySnapshot>()
                            : system.Entities.Where(e => e.HasView<ResearcherView>()).ToList();

                        // Keep the selection valid, defaulting to the first lab so the
                        // window is immediately usable without an extra click.
                        EntitySnapshot? selectedLab = null;
                        if(labs.Count > 0)
                        {
                            selectedLab = labs.FirstOrDefault(l => l.Id == _selectedLabId) ?? labs[0];
                        }
                        _selectedLabId = selectedLab?.Id;

                        Vector2 windowContentSize = ImGui.GetContentRegionAvail();
                        var labListSize = new Vector2(Styles.LeftColumnWidthLg, windowContentSize.Y);
                        var detailSize = new Vector2(windowContentSize.X - Styles.LeftColumnWidthLg - 8, windowContentSize.Y);

                        if(ImGui.BeginChild("LabList", labListSize, ImGuiChildFlags.Borders))
                        {
                            DisplayHelpers.Header("Research Labs", "Select a lab to manage its research queue");
                            DisplayLabList(labs);
                        }
                        ImGui.EndChild();

                        ImGui.SameLine();
                        if(ImGui.BeginChild("LabDetail", detailSize, ImGuiChildFlags.Borders))
                        {
                            if(selectedLab != null)
                                DisplayLabDetail(selectedLab, research);
                            else
                                ImGui.TextColored(Styles.DescriptiveColor, "No research labs in this system.");
                        }
                        ImGui.EndChild();
                    }
            
                }
            }
        }

        private void DisplayLabList(List<EntitySnapshot> labs)
        {
            foreach(var lab in labs)
            {
                var researcher = lab.GetView<ResearcherView>();
                if(researcher == null)
                    continue;

                ImGui.PushID(lab.Id);

                if(ImGui.Selectable(researcher.DesignName + $"###{lab.Id}", _selectedLabId == lab.Id))
                {
                    _selectedLabId = lab.Id;
                }
                if(ImGui.IsItemHovered() && researcher.DesignTemplateName.Length > 0)
                {
                    DisplayHelpers.DescriptiveTooltip(
                        researcher.DesignName,
                        researcher.DesignTemplateName,
                        researcher.DesignDescription);
                }

                ImGui.TextColored(Styles.DescriptiveColor, researcher.LocationName.Length > 0 ? researcher.LocationName : "Unknown");

                var currentTechId = researcher.TechQueue.FirstOrDefault();
                if(currentTechId != null && _techsById.TryGetValue(currentTechId, out var tech) && tech.IsResearchable)
                {
                    float frac = (float)tech.ResearchProgress / tech.ResearchCost;
                    ImGui.ProgressBar(frac, new Vector2(ImGui.GetContentRegionAvail().X, ImGui.GetTextLineHeight()), tech.Name);
                    DisplayHelpers.TechTooltip(tech);
                }
                else
                {
                    ImGui.TextColored(Styles.OkColor, "Idle");
                }

                ImGui.Separator();
                ImGui.PopID();
            }
        }

        private void DisplayLabDetail(EntitySnapshot lab, ResearchSnapshot research)
        {
            var researcher = lab.GetView<ResearcherView>();
            if(researcher == null)
                return;

            DisplayHelpers.Header(researcher.DesignName);

            // Lab stats in an aligned label/value grid, two pairs per row
            if(ImGui.BeginTable("LabSummary", 4, ImGuiTableFlags.SizingStretchProp))
            {
                // Label columns are fixed to their widest label plus a gap so
                // labels never touch their values; value columns share the rest.
                float labelGap = ImGui.GetStyle().ItemSpacing.X * 2;
                float leftLabelWidth = Math.Max(
                    Math.Max(ImGui.CalcTextSize("Location").X, ImGui.CalcTextSize("Cost per Month").X),
                    ImGui.CalcTextSize("Funding").X) + labelGap;
                float rightLabelWidth = Math.Max(
                    ImGui.CalcTextSize("Scientist").X,
                    ImGui.CalcTextSize("Progress per Day").X) + labelGap;
                ImGui.TableSetupColumn("", ImGuiTableColumnFlags.WidthFixed, leftLabelWidth);
                ImGui.TableSetupColumn("", ImGuiTableColumnFlags.WidthStretch, 1f);
                ImGui.TableSetupColumn("", ImGuiTableColumnFlags.WidthFixed, rightLabelWidth);
                ImGui.TableSetupColumn("", ImGuiTableColumnFlags.WidthStretch, 1f);

                ImGui.TableNextColumn();
                ImGui.AlignTextToFramePadding();
                ImGui.TextColored(Styles.DescriptiveColor, "Location");
                ImGui.TableNextColumn();
                ImGui.AlignTextToFramePadding();
                ImGui.Text(researcher.LocationName.Length > 0 ? researcher.LocationName : "Unknown");

                ImGui.TableNextColumn();
                ImGui.AlignTextToFramePadding();
                ImGui.TextColored(Styles.DescriptiveColor, "Scientist");
                ImGui.TableNextColumn();
                var nameDisplay = researcher.ScientistName ?? "Assign Scientist###assignbtn" + lab.Id;
                PortraitRenderer.For(_uiState).Draw(PortraitIdentity.Scientist(researcher), PortraitRenderer.SmallSize);
                ImGui.SameLine();
                if(ImGui.Button(nameDisplay))
                {
                    _showAssignmentModal = lab.Id;
                }

                if(_showAssignmentModal > 0 && _showAssignmentModal == lab.Id)
                {
                    int currentScientistId = researcher.ScientistId ?? -1;
                    PersonAssignmentWindow.GetInstance().Present(
                        "Assign Scientist",
                        string.IsNullOrEmpty(researcher.DesignName) ? "Research lab" : researcher.DesignName,
                        () => research.Scientists,
                        currentScientistId,
                        selectedId => SubmitCommand(new AssignScientistCommand(lab.Id, selectedId)),
                        currentScientistId >= 0
                            ? () => SubmitCommand(new UnassignScientistCommand(lab.Id, currentScientistId))
                            : null,
                        () => _showAssignmentModal = -1);
                }

                ImGui.TableNextColumn();
                ImGui.TextColored(Styles.DescriptiveColor, "Cost per Month");
                ImGui.TableNextColumn();
                ImGui.TextUnformatted(researcher.CostPerMonth.ToString("C0", CultureInfo.CurrentCulture));
                if(ImGui.IsItemHovered())
                {
                    DisplayHelpers.DescriptiveTooltip(
                        "Cost per Month",
                        researcher.CostPerDay.Value.ToString("C0", CultureInfo.CurrentCulture) + " a day",
                        LabCostTooltip(researcher) + "\n\n"
                            + $"{researcher.CostPerDay.BaseValue.ToString("C0", CultureInfo.CurrentCulture)} a day Base Value",
                        delegate {
                            foreach(var modifier in researcher.CostPerDay.Modifiers)
                            {
                                ImGui.TextUnformatted($"{modifier.Delta.ToString("C0", CultureInfo.CurrentCulture)} {modifier.Name}");
                            }
                        });
                }

                ImGui.TableNextColumn();
                ImGui.TextColored(Styles.DescriptiveColor, "Progress per Day");
                ImGui.TableNextColumn();
                ImGui.Text(researcher.PointsPerDay.Value.ToString());
                if(ImGui.IsItemHovered())
                {
                    DisplayHelpers.DescriptiveTooltip(
                        "Progress per Day",
                        "",
                        $"{researcher.PointsPerDay.BaseValue} Base Value",
                        delegate {
                            foreach(var modifier in researcher.PointsPerDay.Modifiers)
                            {
                                ImGui.TextUnformatted($"{modifier.Delta} {modifier.Name}");
                            }
                        });
                }

                ImGui.TableNextColumn();
                ImGui.AlignTextToFramePadding();
                ImGui.TextColored(Styles.DescriptiveColor, "Funding");
                ImGui.TableNextColumn();
                int funding = researcher.FundingLevel;
                string label = researcher.FundingLevel switch
                {
                    0 => "No Funding",
                    1 => "Standard",
                    2 => "Enhanced",
                    3 => "Robust",
                    4 => "Generous",
                    5 => "Spared No Expense",
                    _ => ""
                };
                ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
                if(ImGui.SliderInt($"###{lab.Id}-funding", ref funding, 0, 5, label))
                {
                    SubmitCommand(new SetResearchFundingCommand(lab.Id, funding));
                }
                ImGui.TableNextColumn();
                ImGui.TableNextColumn();

                ImGui.EndTable();
            }

            // Current research as a prominent full-width bar
            ImGui.Spacing();
            var currentTechId = researcher.TechQueue.FirstOrDefault();
            var barSize = new Vector2(ImGui.GetContentRegionAvail().X, ImGui.GetTextLineHeight() + 10);
            if(currentTechId != null && _techsById.TryGetValue(currentTechId, out var currentTech) && currentTech.IsResearchable)
            {
                float frac = (float)currentTech.ResearchProgress / currentTech.ResearchCost;
                ImGui.ProgressBar(frac, barSize, $"{currentTech.Name}  {currentTech.ResearchProgress}/{currentTech.ResearchCost}  ({frac:P0})");
                DisplayHelpers.TechTooltip(currentTech);
            }
            else
            {
                ImGui.ProgressBar(0f, barSize, "Idle — double click a tech to begin research");
            }
            ImGui.Spacing();

            var contentSize = ImGui.GetContentRegionAvail();
            var queueSize = new Vector2(contentSize.X - Styles.LeftColumnWidthLg - 8, contentSize.Y);
            var techsSize = new Vector2(Styles.LeftColumnWidthLg, contentSize.Y);

            if(ImGui.BeginChild("TechQueue", queueSize, ImGuiChildFlags.Borders))
            {
                DisplayHelpers.Header("Tech Queue");
                DisplayQueue(lab.Id, researcher);
            }
            ImGui.EndChild();

            ImGui.SameLine();
            if(ImGui.BeginChild("AvailableTechs", techsSize, ImGuiChildFlags.Borders))
            {
                DisplayHelpers.Header("Available Techs", "Double click a tech to add it to this lab's queue");

                var availableSize = ImGui.GetContentRegionAvail();
                ImGui.SetNextItemWidth(availableSize.X);
                if(ImGui.Combo("###template-filter", ref selectCategoryFilterIndex, _categoryNames, _categoryNames.Length))
                {
                    RefreshTechs();
                }
                DisplayTechs(lab.Id);
            }
            ImGui.EndChild();
        }

        private void DisplayQueue(int labId, ResearcherView researcher)
        {
            if(researcher.TechQueue.Count == 0)
            {
                ImGui.TextColored(Styles.DescriptiveColor, "Queue is empty. Double click a tech on the right to add it.");
                return;
            }

            if(ImGui.BeginTable("TechQueue", 3, Styles.TableFlags | ImGuiTableFlags.SizingStretchProp))
            {
                ImGui.TableSetupColumn("#", ImGuiTableColumnFlags.None, 0.05f);
                ImGui.TableSetupColumn("Name", ImGuiTableColumnFlags.None, 0.5f);
                ImGui.TableSetupColumn("Options", ImGuiTableColumnFlags.None, 0.45f);
                ImGui.TableHeadersRow();

                int index = 0;
                foreach(var techId in researcher.TechQueue)
                {
                    if(!_techsById.TryGetValue(techId, out var tech))
                        continue;

                    ImGui.TableNextColumn();
                    ImGui.Text($"{index + 1}");
                    ImGui.TableNextColumn();
                    string queuedName = FitText(tech.Name, ImGui.GetContentRegionAvail().X, t => ImGui.CalcTextSize(t).X);
                    ImGui.TextUnformatted(queuedName);
                    if (queuedName != tech.Name && ImGui.IsItemHovered())
                    {
                        ImGui.BeginTooltip();
                        ImGui.TextUnformatted(tech.Name);
                        ImGui.EndTooltip();
                    }
                    else
                        DisplayHelpers.TechTooltip(tech);
                    ImGui.TableNextColumn();
                    Buttons(labId, researcher, techId, ref index);
                    index++;
                }

                ImGui.EndTable();
            }
        }

        private void DisplayTechs(int labId)
        {
            if(ImGui.BeginTable("ResearchableTechs", 1, ImGuiTableFlags.BordersInnerV))
            {
                for (int i = 0; i < _researchableTechs.Count; i++)
                {
                    var tech = _researchableTechs[i];
                    if (tech.ResearchCost > 0) //could happen if bad json data?
                    {
                        ImGui.TableNextColumn();

                        float frac = (float)tech.ResearchProgress / tech.ResearchCost;
                        var size = ImGui.GetContentRegionAvail();
                        var height = ImGui.GetTextLineHeight();
                        var pos = ImGui.GetCursorPos();
                        ImGui.ProgressBar(frac, new Vector2(size.X, height), "");
                        // Long names are cut with "..." to fit the row; the hover shows the full name.
                        string shownName = FitText(tech.DisplayName, size.X - 4f, t => ImGui.CalcTextSize(t).X);
                        if (ImGui.IsItemHovered())
                        {
                            string metaInfo = "";
                            if(tech.NextLevelUnlocks.Count > 0)
                            {
                                metaInfo += "Unlocks:\n";
                                foreach(var unlockName in tech.NextLevelUnlocks)
                                {
                                    metaInfo += unlockName + "\n";
                                }
                            }
                            if(tech.MaxLevel > 1)
                            {
                                metaInfo += "\nMaximum: " + tech.MaxLevelName;
                            }

                            DisplayHelpers.DescriptiveTooltip(
                                tech.DisplayName,
                                tech.CategoryName,
                                FullNameLine(tech.DisplayName, shownName) + tech.Description,
                                () => ImGui.TextUnformatted(metaInfo));
                        }
                        ImGui.SetCursorPos(new Vector2(pos.X + 2f, pos.Y));
                        ImGui.TextUnformatted(shownName);

                        if (ImGui.IsItemHovered() && ImGui.IsMouseDoubleClicked(0))
                        {
                            SubmitCommand(new AddTechToQueueCommand(labId, tech.Id));
                        }
                    }
                }

                ImGui.EndTable();
            }
        }

        void Buttons(int labId, ResearcherView researcher, string techID, ref int i)
        {
            ImGui.BeginGroup();

            if (i > 0)
            {
                if(ImGui.SmallButton("^" + "##" + i))
                {
                    SubmitCommand(new MoveTechInQueueCommand(labId, techID, MoveUp: true));
                }
            }
            else
            {
                ImGui.InvisibleButton("invis2", invisButtonSize);
            }
            ImGui.SameLine();

            if (i < researcher.TechQueue.Count - 1)
            {
                if(ImGui.SmallButton("v" + "##" + i))
                {
                    SubmitCommand(new MoveTechInQueueCommand(labId, techID, MoveUp: false));
                }
            }
            else
            {
                ImGui.InvisibleButton("invis3", invisButtonSize);
            }
            ImGui.SameLine();

            if (ImGui.SmallButton("x" + "##" + i))
            {
                SubmitCommand(new RemoveTechFromQueueCommand(labId, techID));
                i--;
            }

            ImGui.EndGroup();
        }

        private void SubmitCommand(GameCommand command) => _uiState.GameClient?.SubmitCommandAsync(command);

        public const string Ellipsis = "...";

        /// <summary>
        /// <paramref name="text"/> if it fits <paramref name="width"/> (as measured), else the longest prefix that fits
        /// with "..." after it. Never returns more than the original text.
        /// </summary>
        public static string FitText(string text, float width, Func<string, float> measure)
        {
            if (string.IsNullOrEmpty(text) || measure(text) <= width)
                return text ?? "";
            int lo = 0, hi = text.Length;
            while (lo < hi)
            {
                int mid = (lo + hi + 1) / 2;
                if (measure(text.Substring(0, mid).TrimEnd() + Ellipsis) <= width)
                    lo = mid;
                else
                    hi = mid - 1;
            }
            return lo == 0 ? Ellipsis : text.Substring(0, lo).TrimEnd() + Ellipsis;
        }

        /// <summary>The full name as the tooltip's first line when the list shows it cut; empty otherwise. '%' is escaped (the tooltip text is a format string).</summary>
        public static string FullNameLine(string fullName, string shownName)
            => shownName == fullName ? "" : fullName.Replace("%", "%%") + "\n\n";

        /// <summary>The lab cost tooltip: the daily rate, this month's days, and when the bill is booked.</summary>
        public static string LabCostTooltip(ResearcherView researcher)
        {
            string text = researcher.CostPerDay.Value.ToString("C0", CultureInfo.CurrentCulture) + " a day x "
                + researcher.DaysInMonth + " days this month. Accrues each day the lab researches";
            text += researcher.NextPayday is { } payday
                ? " and is billed on " + DateDisplay.FormatDate(payday) + "."
                : " and is billed on the 1st of next month.";
            return text + " A lab only runs when funds, less what is already owed this month, cover its day.";
        }
    }
}
