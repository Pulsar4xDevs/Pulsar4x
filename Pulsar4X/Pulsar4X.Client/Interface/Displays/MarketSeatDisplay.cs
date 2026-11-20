using ImGuiNET;
using Pulsar4X.Api;
using Pulsar4X.Client.Interface.Widgets;

namespace Pulsar4X.Client;

/// <summary>Staffing and market-tab chrome for the colony market-head seat.</summary>
public static class MarketSeatDisplay
{
    static string? _pendingRefusal;
    static volatile bool _pendingReady;
    static int? _chooserColonyId;

    public static void TakePendingRefusal(ref string? refusal)
    {
        if (!_pendingReady)
            return;
        refusal = _pendingRefusal;
        _pendingRefusal = null;
        _pendingReady = false;
    }

    public static void DrawStaffing(
        int colonyId,
        ColonyView view,
        IClientGalaxy galaxy,
        GlobalUIState uiState,
        ref string? refusal)
    {
        TakePendingRefusal(ref refusal);
        ImGui.TextUnformatted("Market head: " + MarketSeatUi.HolderText(view.MarketHeadId, view.MarketHeadName));
        DrawSeatButtons(colonyId, view.MarketHeadId, galaxy, uiState, ref refusal);
        DrawRefusal(refusal);
    }

    public static void DrawChooser(
        IClientGalaxy galaxy,
        GlobalUIState uiState,
        ref string? refusal)
    {
        TakePendingRefusal(ref refusal);
        if (_chooserColonyId is not int colonyId)
            return;

        EntitySnapshot? colony = null;
        foreach (var summary in galaxy.KnownSystems)
        {
            colony = galaxy.GetSystem(summary.SystemId)?.GetEntity(colonyId);
            if (colony != null)
                break;
        }
        var view = colony?.GetView<ColonyView>();
        if (colony == null || view == null)
        {
            _chooserColonyId = null;
            return;
        }

        var candidates = MarketSeatUi.Candidates(
            galaxy.Commanders,
            MarketSeatUi.BlockedIds(IndustrySeatUi.ColonyViews(galaxy)));
        PersonAssignmentWindow.GetInstance().Present(
            "Seat Market Head",
            MarketSeatUi.PostLabel,
            () => candidates,
            view.MarketHeadId,
            selected => Submit(uiState, new SeatMarketHeadCommand(colonyId, selected)),
            () => Submit(uiState, new UnseatMarketHeadCommand(colonyId)),
            () => _chooserColonyId = null,
            person => person.Name);
    }

    /// <summary>Portrait (from the roster), name, post, and Seat/Unseat — for the Market tab.</summary>
    public static void DrawMarket(
        int colonyId,
        ColonyView view,
        IClientGalaxy? galaxy,
        GlobalUIState uiState,
        ref string? refusal)
    {
        TakePendingRefusal(ref refusal);
        var portrait = MarketSeatUi.Portrait(view.MarketHeadId, galaxy?.Commanders);
        if (!portrait.IsEmpty)
        {
            PortraitRenderer.For(uiState).Draw(portrait, PortraitRenderer.SmallSize);
            ImGui.SameLine();
        }
        ImGui.BeginGroup();
        ImGui.TextUnformatted(MarketSeatUi.HolderText(view.MarketHeadId, view.MarketHeadName));
        ImGui.TextUnformatted(MarketSeatUi.PostLabel);
        ImGui.EndGroup();
        if (galaxy != null)
        {
            ImGui.SameLine();
            ImGui.BeginGroup();
            DrawSeatButtons(colonyId, view.MarketHeadId, galaxy, uiState, ref refusal);
            ImGui.EndGroup();
        }
        DrawRefusal(refusal);
    }

    /// <summary>Seat opens the shared assignment chooser; Unseat is disabled when the seat is empty.</summary>
    static void DrawSeatButtons(
        int colonyId,
        int marketHeadId,
        IClientGalaxy galaxy,
        GlobalUIState uiState,
        ref string? refusal)
    {
        var candidates = MarketSeatUi.Candidates(
            galaxy.Commanders,
            MarketSeatUi.BlockedIds(IndustrySeatUi.ColonyViews(galaxy)));
        bool seated = MarketSeatUi.IsSeated(marketHeadId);
        bool canSeat = candidates.Count > 0;

        if (!canSeat)
            ImGui.BeginDisabled();
        if (ImGui.Button("Seat###market-seat-" + colonyId) && canSeat)
            _chooserColonyId = colonyId;
        if (!canSeat)
            ImGui.EndDisabled();
        if (!canSeat && ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))
            ImGui.SetTooltip("No unassigned civilians. Hire an administrator first.\nA city-hall chair or industry head cannot take this seat.");

        ImGui.SameLine();
        if (!seated)
            ImGui.BeginDisabled();
        if (ImGui.Button("Unseat###market-unseat-" + colonyId) && seated)
        {
            Submit(uiState, new UnseatMarketHeadCommand(colonyId));
            TakePendingRefusal(ref refusal);
        }
        if (!seated)
            ImGui.EndDisabled();
        if (!seated && ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))
            ImGui.SetTooltip("The market seat is already empty.");
    }

    static void DrawRefusal(string? refusal)
    {
        if (string.IsNullOrEmpty(refusal))
            return;
        ImGui.PushStyleColor(ImGuiCol.Text, Styles.TerribleColor);
        ImGui.TextUnformatted(refusal);
        ImGui.PopStyleColor();
    }

    static void Submit(GlobalUIState uiState, GameCommand command)
    {
        var task = uiState.GameClient?.SubmitCommandAsync(command);
        if (task == null)
            return;
        if (task.IsCompleted)
        {
            _pendingRefusal = MarketSeatUi.RefusalMessage(task.Result);
            _pendingReady = true;
            return;
        }
        task.ContinueWith(t =>
        {
            if (!t.IsCompletedSuccessfully)
                return;
            _pendingRefusal = MarketSeatUi.RefusalMessage(t.Result);
            _pendingReady = true;
        });
    }
}
