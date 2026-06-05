using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using ImGuiNET;
using Pulsar4X.Engine;
using Pulsar4X.Datablobs;
using Pulsar4X.Extensions;
using Pulsar4X.Colonies;
using Pulsar4X.Factions;
using Pulsar4X.Industry;
using Pulsar4X.People;
using Pulsar4X.Storage;
using Pulsar4X.Technology;
using Pulsar4X.Galaxy;

namespace Pulsar4X.Client
{
    public static class EntityDisplay
    {
        public static void DisplaySummary(this Entity entity, EntityState entityState, GlobalUIState uiState)
        {
            Vector2 windowContentSize = ImGui.GetContentRegionAvail();
            var firstChildSize = new Vector2(windowContentSize.X * 0.33f, windowContentSize.Y);
            var secondChildSize = new Vector2(windowContentSize.X * 0.33f, windowContentSize.Y);
            var thirdChildSize = new Vector2(windowContentSize.X * 0.33f - (windowContentSize.X * 0.01f), windowContentSize.Y);
            if(ImGui.BeginChild("ColonySummary1", firstChildSize, ImGuiChildFlags.Borders))
            {
                var colonyInfoDb = entity.GetDataBlob<ColonyInfoDB>();
                var bodyInfoDb = colonyInfoDb.PlanetEntity.GetDataBlob<SystemBodyInfoDB>();
                var headerName = colonyInfoDb.PlanetEntity.GetDefaultName() + " Information";

                if(ImGui.CollapsingHeader(headerName, ImGuiTreeNodeFlags.DefaultOpen))
                {
                    ImGui.Columns(2);
                    ImGui.PushStyleColor(ImGuiCol.Text, Styles.DescriptiveColor);
                    ImGui.Text("Name");
                    ImGui.PopStyleColor();
                    ImGui.NextColumn();
                    if(ImGui.SmallButton(colonyInfoDb.PlanetEntity.GetDefaultName()))
                    {
                        uiState.EntityClicked(colonyInfoDb.PlanetEntity.Id, uiState.SelectedStarSystemId, MouseButtons.Primary);
                    }
                    ImGui.NextColumn();
                    ImGui.Separator();
                    DisplayHelpers.PrintRow("Type", bodyInfoDb.BodyType.ToDescription());
                    DisplayHelpers.PrintRow("Tectonic Activity", bodyInfoDb.Tectonics.ToDescription());
                    DisplayHelpers.PrintRow("Gravity", Stringify.Velocity(bodyInfoDb.Gravity));
                    DisplayHelpers.PrintRow("Temperature", bodyInfoDb.BaseTemperature.ToString("#.#") + " C");
                    DisplayHelpers.PrintRow("Length of Day", bodyInfoDb.LengthOfDay.TotalHours + " hours");
                    DisplayHelpers.PrintRow("Tilt", bodyInfoDb.AxialTilt.ToString("#") + "°");
                    DisplayHelpers.PrintRow("Magnetic Field", bodyInfoDb.MagneticField.ToString("#") + " μT");
                    DisplayHelpers.PrintRow("Radiation Level", bodyInfoDb.RadiationLevel.ToString("#"));
                    DisplayHelpers.PrintRow("Atmospheric Dust", bodyInfoDb.AtmosphericDust.ToString("#"), separator: false);
                }
                ImGui.Columns(1);
                if(colonyInfoDb.PlanetEntity.TryGetDataBlob<AtmosphereDB>(out var atmosphereDB))
                {
                    atmosphereDB.Display(entityState, uiState);
                }
                else
                {
                    if(ImGui.CollapsingHeader("Atmosphere", ImGuiTreeNodeFlags.DefaultOpen))
                    {
                        ImGui.Text("No Atmosphere");
                    }
                }
            }
            ImGui.EndChild();

            ImGui.SameLine();
            if(ImGui.BeginChild("ColonySummary2", secondChildSize, ImGuiChildFlags.Borders))
            {
                entity.GetDataBlob<ColonyInfoDB>().Display(entityState, uiState);
                ImGui.Columns(1);

                if(entity.TryGetDataBlob<InfrastructureDB>(out var infrastructure)
                    && ImGui.CollapsingHeader("Infrastructure", ImGuiTreeNodeFlags.DefaultOpen))
                {
                    bool overCapacity = infrastructure.CapacityAvailable < 0;

                    ImGui.Columns(2);
                    DisplayHelpers.PrintRow("Provided", infrastructure.CapacityProvided.ToString("N0"));
                    DisplayHelpers.PrintRow("Used", infrastructure.CapacityRequired.ToString("N0"));
                    DisplayHelpers.PrintRow("Available", infrastructure.CapacityAvailable.ToString("N0"));
                    ImGui.Columns(1);

                    // Use TextUnformatted: ImGui.Text/TextColored treat the string as a printf
                    // format, so a literal '%' would be parsed as a format specifier.
                    if(overCapacity)
                    {
                        ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(1f, 0.4f, 0.4f, 1f));
                        ImGui.TextUnformatted($"Over capacity - all output reduced to {infrastructure.Efficiency * 100:0}%");
                        ImGui.PopStyleColor();
                    }
                    else
                    {
                        ImGui.PushStyleColor(ImGuiCol.Text, Styles.DescriptiveColor);
                        ImGui.TextUnformatted($"Output at {infrastructure.Efficiency * 100:0}% of capacity");
                        ImGui.PopStyleColor();
                    }
                }

                if(ImGui.CollapsingHeader("Installations", ImGuiTreeNodeFlags.DefaultOpen))
                {
                    if(entity.TryGetDataBlob<ComponentInstancesDB>(out var componentInstances))
                    {
                        componentInstances.Display(entityState, uiState);
                    }
                }
            }
            ImGui.EndChild();

            ImGui.SameLine();
            if(ImGui.BeginChild("ColonySummary3", thirdChildSize, ImGuiChildFlags.Borders))
            {
                if(ImGui.CollapsingHeader("Stockpile", ImGuiTreeNodeFlags.DefaultOpen))
                {
                    if(entity.TryGetDataBlob<CargoStorageDB>(out var storage))
                    {
                        var size = ImGui.GetContentRegionAvail();
                        ImGui.PushStyleColor(ImGuiCol.Button, Styles.Theme.Button.ToImVector4());
                        ImGui.PushStyleColor(ImGuiCol.ButtonHovered, Styles.Theme.ButtonHovered.ToImVector4());
                        ImGui.PushStyleColor(ImGuiCol.ButtonActive, Styles.Theme.ButtonActive.ToImVector4());
                        if(ImGui.Button("Initiate Transfer", new Vector2(size.X - 8, 18)))
                        {
                            CreateTransferWindow.GetInstance().SetLeft(entity);
                            CreateTransferWindow.GetInstance().SetActive(true);
                        }
                        ImGui.PopStyleColor(3);

                        ImGui.Columns(2);
                        DisplayHelpers.PrintRow("Total Mass in Storage", Stringify.Mass(storage.TotalStoredMass));
                        DisplayHelpers.PrintRow("Transfer Rate", storage.TransferRate.ToString() + " kg/hr");
                        DisplayHelpers.PrintRow("Transfer Range", storage.TransferRangeDv_mps.ToString("0.#") + " dV m/s", tooltipOne: "This is confusing as hell :D", separator: false);
                        ImGui.Columns(1);
                        storage.Display(entityState, uiState, ImGuiTreeNodeFlags.None);
                    }
                }
            }
            ImGui.EndChild();
        }

        public static void DisplayIndustry(this Entity entity, EntityState entityState, GlobalUIState uiState)
        {
            IndustryDisplay.GetInstance(entityState).Display(uiState);
        }

        public static void DisplayConstruction(this Entity entity, EntityState entityState, GlobalUIState uiState)
        {
            ConstructionDisplay.GetInstance(entityState).Display(uiState);
        }
        public static void DisplayMining(this Entity entity, GlobalUIState uiState)
        {
            if(uiState.Faction == null) return;

            var mineralStaticInfo = uiState.Faction.GetDataBlob<FactionInfoDB>().Data.CargoGoods.GetMineralsList();
            var minerals = entity.GetDataBlob<ColonyInfoDB>().PlanetEntity.HasDataBlob<MineralsDB>() ?
                            entity.GetDataBlob<ColonyInfoDB>().PlanetEntity.GetDataBlob<MineralsDB>()?.Minerals :
                            null;
            var miningRates = entity.HasDataBlob<MiningDB>() ? entity.GetDataBlob<MiningDB>().ActualMiningRate : new ();
            var storage = entity.GetDataBlob<CargoStorageDB>()?.TypeStores;

            Vector2 topSize = ImGui.GetContentRegionAvail();
            if(ImGui.BeginChild("NumberOfMines" + entity.Id, new Vector2(topSize.X, 28f), ImGuiChildFlags.Borders, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse))
            {
                if(entity.TryGetDataBlob<MiningDB>(out var miningDB))
                {
                    ImGui.Text("Number of Mines:");
                    if(ImGui.IsItemHovered())
                        ImGui.SetTooltip("You can build more mines on this colony using the Production tab.");
                    ImGui.SameLine();
                    ImGui.PushStyleColor(ImGuiCol.Text, Styles.HighlightColor);
                    ImGui.Text(miningDB.NumberOfMines.ToString());
                    ImGui.PopStyleColor();

                }
                else
                {
                    ImGui.Text("Number of Mines: 0");
                }
            }
            ImGui.EndChild();

            if(ImGui.BeginTable("###MineralTable" + entity.Id, 6, ImGuiTableFlags.BordersV | ImGuiTableFlags.BordersOuterH | ImGuiTableFlags.RowBg | ImGuiTableFlags.SizingStretchProp))
            {
                ImGui.TableSetupColumn("Mineral");
                ImGui.TableSetupColumn("Stockpile");
                ImGui.TableSetupColumn("Available to Mine");
                ImGui.TableSetupColumn("Accessibility");
                ImGui.TableSetupColumn("Annual Production");
                ImGui.TableSetupColumn("Years to Depletion");
                ImGui.TableHeadersRow();

                if(minerals == null) minerals = new Dictionary<int, MineralDeposit>();

                foreach(var (id, mineral) in minerals)
                {
                    var mineralData = mineralStaticInfo.FirstOrDefault(x => x.ID == id);

                    if(mineralData == null) continue;

                    var stockpileData = storage?.FirstOrDefault(x => x.Value.CurrentStoreInUnits.ContainsKey(id)).Value;
                    var stockpileUnits = stockpileData?.CurrentStoreInUnits;
                    var annualProduction = miningRates.ContainsKey(id) ? 365 * miningRates[id] : 0;

                    ImGui.TableNextRow();
                    ImGui.TableNextColumn();
                    ImGui.Text(mineralData.Name);
                    if(ImGui.IsItemHovered())
                        DisplayHelpers.DescriptiveTooltip(mineralData.Name, "Mineral", mineralData.Description);
                    ImGui.TableNextColumn();
                    if(stockpileUnits != null && stockpileUnits.ContainsKey(id))
                    {
                        ImGui.Text(stockpileUnits[id].ToString("#,###,###,###,###,###,##0"));
                    }
                    else
                    {
                        if(storage == null)
                            ImGui.Text("Unavailable");
                        else
                            ImGui.Text("0");
                    }
                    if(ImGui.IsItemHovered())
                        ImGui.SetTooltip("Amount of " + mineralData.Name + " available for use in the colony stockpile.");

                    ImGui.TableNextColumn();
                    ImGui.Text(mineral.Amount.For(uiState.FactionMask)?.ToString("#,###,###,###,###,###,##0") ?? "N/A");
                    if(ImGui.IsItemHovered())
                        ImGui.SetTooltip("Amount of " + mineralData.Name + " available that can be mined from this colony.");
                    ImGui.TableNextColumn();
                    ImGui.Text(mineral.Accessibility.ToString("0.00"));
                    if(ImGui.IsItemHovered())
                        ImGui.SetTooltip("How easy it is to mine " + mineralData.Name + " from this colony.\n\n1.0 = easiest\n0.0 = hardest");
                    ImGui.TableNextColumn();
                    if(miningRates.ContainsKey(id))
                    {
                        ImGui.Text(annualProduction.ToString("#,###,###"));
                        if(ImGui.IsItemHovered())
                            ImGui.SetTooltip("Annual production of " + mineralData.Name + " from this colony.");
                    }
                    else
                    {
                        ImGui.Text("-");
                        if(ImGui.IsItemHovered())
                            ImGui.SetTooltip("This colony is currently unable to mine " + mineralData.Name + ".");
                    }
                    ImGui.TableNextColumn();
                    if(annualProduction > 0)
                    {
                        var amount = mineral.Amount.For(uiState.FactionMask) ?? 0;
                        string yearsToDepletion = Math.Round((double)amount / (double)annualProduction, 4).ToString("#.0");
                        ImGui.Text(yearsToDepletion);
                        if(ImGui.IsItemHovered())
                            ImGui.SetTooltip("The colony will exhaust the available " + mineralData.Name + " in " + yearsToDepletion + " years.");
                    }
                    else
                    {
                        ImGui.Text("-");
                    }
                }

                ImGui.EndTable();

                if(minerals.Count == 0)
                {
                    ImGui.Text("No minerals available.");
                }
            }
        }

        public static void DisplayResearch(this Entity entity, EntityState entityState, GlobalUIState uiState)
        {
            if(!entity.TryGetDataBlob<EntityResearchDB>(out var researchDB)) return;

            Vector2 topSize = ImGui.GetContentRegionAvail();
            if(ImGui.BeginChild("NumberOfResearchLabs" + entity.Id, new Vector2(topSize.X, 28f), ImGuiChildFlags.Borders, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse))
            {
                ImGui.Text("Universities:");
                ImGui.SameLine();
                ImGui.PushStyleColor(ImGuiCol.Text, Styles.HighlightColor);
                ImGui.Text(researchDB.Labs.Count.ToString("0"));
                ImGui.PopStyleColor();
                ImGui.SameLine();
                ImGui.Text("Research Points:");
                ImGui.SameLine();
                ImGui.PushStyleColor(ImGuiCol.Text, Styles.HighlightColor);
                ImGui.Text(researchDB.Labs.Values.Sum().ToString());
                ImGui.PopStyleColor();

                ImGui.EndChild();
            }

            Vector2 sizeAvailable = ImGui.GetContentRegionAvail();
            if(ImGui.BeginChild("UniversityList", sizeAvailable, ImGuiChildFlags.Borders))
            {
                foreach(var (instance, value) in researchDB.Labs)
                {
                    ImGui.Text(instance.Name);
                    ImGui.Text(value.ToString());
                }
                ImGui.EndChild();
            }
        }

        public static void DisplayLogistics(this Entity entity, EntityState entityState, GlobalUIState uiState)
        {
            ColonyLogisticsDisplay.GetInstance(entity.GetFactionOwner.GetDataBlob<FactionInfoDB>().Data, entityState).Display();
        }

        public static void DisplayNavalAcademy(this Entity entity, EntityState entityState, GlobalUIState uiState)
        {
            if(!entity.TryGetDataBlob<NavalAcademyDB>(out var navalAcademyDB)) return;

            Vector2 topSize = ImGui.GetContentRegionAvail();
            if(ImGui.BeginChild("NumberOfAcademies" + entity.Id, new Vector2(topSize.X, 28f), ImGuiChildFlags.Borders, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse))
            {
                ImGui.Text("Academies:");
                ImGui.SameLine();
                ImGui.PushStyleColor(ImGuiCol.Text, Styles.HighlightColor);
                ImGui.Text(navalAcademyDB.Academies.Count.ToString("0"));
                ImGui.PopStyleColor();
                ImGui.EndChild();
            }

            Vector2 sizeAvailable = ImGui.GetContentRegionAvail();
            if(ImGui.BeginChild("AcademyList", new Vector2(sizeAvailable.X * .25f, sizeAvailable.Y), ImGuiChildFlags.Borders))
            {
                if(ImGui.BeginTable("AcademyListTable", 4, Styles.TableFlags))
                {
                    ImGui.TableSetupColumn("#", ImGuiTableColumnFlags.None, 0.1f);
                    ImGui.TableSetupColumn("Class Size", ImGuiTableColumnFlags.None, 0.25f);
                    ImGui.TableSetupColumn("Length", ImGuiTableColumnFlags.None, 0.2f);
                    ImGui.TableSetupColumn("Graduation", ImGuiTableColumnFlags.None, 0.3f);
                    ImGui.TableHeadersRow();

                    for(int i = 0; i < navalAcademyDB.Academies.Count; i++)
                    {
                        ImGui.TableNextColumn();
                        ImGui.Text((i + 1).ToString());
                        ImGui.TableNextColumn();
                        ImGui.Text(navalAcademyDB.Academies[i].ClassSize.ToString());
                        ImGui.TableNextColumn();
                        ImGui.Text(navalAcademyDB.Academies[i].TrainingPeriodInMonths.ToString() + " months");
                        ImGui.TableNextColumn();
                        ImGui.Text(navalAcademyDB.Academies[i].GraduationDate.ToShortDateString());
                    }
                    ImGui.EndTable();
                }
                ImGui.EndChild();
            }
        }
    }
}
