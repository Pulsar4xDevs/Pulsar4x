using System;
using System.Globalization;
using System.Numerics;
using ImGuiNET;
using Pulsar4X.Api;
using Pulsar4X.Client.Interface.Widgets;

namespace Pulsar4X.Client;

/// <summary>
/// The corporation ledger. Opened from the funds line in <see cref="Selector"/>.
/// Rows come from <see cref="FactionSnapshot.Transactions"/>, newest first.
/// </summary>
public class LedgerWindow : UniquePulsarGuiWindow<LedgerWindow>
{
    LedgerWindow()
    {
    }

    internal static LedgerWindow GetInstance()
    {
        if (_uiState.TryGetUniqueWindow<LedgerWindow>(out var window))
            return window;

        return _uiState.AddUniqueWindow(new LedgerWindow());
    }

    internal override void Display()
    {
        if (!IsActive)
            return;

        ImGui.SetNextWindowSize(new Vector2(720, 420), ImGuiCond.FirstUseEver);
        if (!Window.Begin("Transactions", ref IsActive))
        {
            Window.End();
            return;
        }

        var faction = _uiState.GameClient?.Galaxy.Faction;
        if (faction == null)
        {
            ImGui.TextUnformatted("No corporation.");
            Window.End();
            return;
        }

        ImGui.TextUnformatted(faction.Name);
        if (faction.Funds < 0)
            Finances.ColouredText(Finances.BannerColour, "Balance " + faction.Funds.ToString("C2", CultureInfo.CurrentCulture));
        else
            ImGui.TextUnformatted("Balance " + faction.Funds.ToString("C2", CultureInfo.CurrentCulture));
        // The galaxy's faction is the player's own; NPC factions pay no wages and have no books here.
        Finances.Draw(faction);
        ImGui.Separator();

        var rows = faction.Transactions;
        if (rows == null || rows.Count == 0)
        {
            ImGui.TextUnformatted("No transactions.");
            Window.End();
            return;
        }

        if (ImGui.BeginTable("LedgerRows", 4, Styles.TableFlags | ImGuiTableFlags.ScrollY,
                new Vector2(0, ImGui.GetContentRegionAvail().Y)))
        {
            ImGui.TableSetupColumn("Date", ImGuiTableColumnFlags.WidthFixed, System.Math.Max(168f, ImGui.CalcTextSize(DateDisplay.WidestDateTimeSample()).X + 8f));
            ImGui.TableSetupColumn("Category", ImGuiTableColumnFlags.WidthFixed, 140);
            ImGui.TableSetupColumn("Description", ImGuiTableColumnFlags.WidthStretch);
            ImGui.TableSetupColumn("Amount", ImGuiTableColumnFlags.WidthFixed, 120);
            ImGui.TableHeadersRow();

            foreach (var row in rows)
            {
                ImGui.TableNextColumn();
                ImGui.TextUnformatted(DateDisplay.FormatDateTime(row.Date));

                ImGui.TableNextColumn();
                ImGui.TextUnformatted(CategoryLabel(row.Category));

                ImGui.TableNextColumn();
                ImGui.TextUnformatted(row.Description);
                if (ImGui.IsItemHovered() && row.Description.Length > 0)
                {
                    ImGui.BeginTooltip();
                    ImGui.TextUnformatted(row.Description);
                    ImGui.EndTooltip();
                }

                ImGui.TableNextColumn();
                ImGui.TextUnformatted(row.Amount.ToString("C2", CultureInfo.CurrentCulture));
            }

            ImGui.EndTable();
        }

        Window.End();
    }

    /// <summary>The ledger's category column. Wages and research are billed once a month, on the 1st.</summary>
    public static string CategoryLabel(string category) => category switch
    {
        "InitialInvestment" => "Initial investment",
        "Research" => "Research (monthly)",
        "Trade" => "Trade",
        FactionSnapshot.WagesCategory => "Wages (monthly)",
        _ => category
    };
}
