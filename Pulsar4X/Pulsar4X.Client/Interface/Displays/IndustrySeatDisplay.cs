using ImGuiNET;
using Pulsar4X.Api;
using Pulsar4X.Client.Interface.Widgets;

namespace Pulsar4X.Client;

/// <summary>Staffing and production chrome for the colony industry-head seat.</summary>
public static class IndustrySeatDisplay
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
        ImGui.TextUnformatted("Industry head: " + IndustrySeatUi.HolderText(view.IndustryHeadId, view.IndustryHeadName));
        DrawSeatButtons(colonyId, view.IndustryHeadId, galaxy, uiState, ref refusal);
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

        var candidates = IndustrySeatUi.Candidates(
            galaxy.Commanders,
            IndustrySeatUi.CityHallIds(IndustrySeatUi.ColonyViews(galaxy)));
        PersonAssignmentWindow.GetInstance().Present(
            "Seat Industry Head",
            "Industry head",
            () => candidates,
            view.IndustryHeadId,
            selected => Submit(uiState, new SeatIndustryHeadCommand(colonyId, selected)),
            () => Submit(uiState, new UnseatIndustryHeadCommand(colonyId)),
            () => _chooserColonyId = null,
            person => person.Name);
    }

    public static void DrawProduction(
        int colonyId,
        IndustryView industry,
        IClientGalaxy? galaxy,
        GlobalUIState uiState,
        ref string? refusal)
    {
        TakePendingRefusal(ref refusal);
        PortraitRenderer.For(uiState).Draw(PortraitIdentity.IndustryHead(industry), PortraitRenderer.SmallSize);
        ImGui.SameLine();
        ImGui.BeginGroup();
        ImGui.TextUnformatted(IndustrySeatUi.HolderText(industry.IndustryHeadId, industry.IndustryHeadName));
        ImGui.TextUnformatted("Industry head");
        ImGui.EndGroup();
        if (galaxy != null)
        {
            ImGui.SameLine();
            ImGui.BeginGroup();
            DrawSeatButtons(colonyId, industry.IndustryHeadId, galaxy, uiState, ref refusal);
            ImGui.EndGroup();
        }
        DrawRefusal(refusal);
    }

    /// <summary>Seat opens the shared assignment chooser; Unseat is disabled when the seat is empty.</summary>
    static void DrawSeatButtons(
        int colonyId,
        int industryHeadId,
        IClientGalaxy galaxy,
        GlobalUIState uiState,
        ref string? refusal)
    {
        var candidates = IndustrySeatUi.Candidates(
            galaxy.Commanders,
            IndustrySeatUi.CityHallIds(IndustrySeatUi.ColonyViews(galaxy)));
        bool seated = IndustrySeatUi.IsSeated(industryHeadId);
        bool canSeat = candidates.Count > 0;

        if (!canSeat)
            ImGui.BeginDisabled();
        if (ImGui.Button("Seat###industry-seat-" + colonyId) && canSeat)
            _chooserColonyId = colonyId;
        if (!canSeat)
            ImGui.EndDisabled();
        if (!canSeat && ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))
            ImGui.SetTooltip("No unassigned civilians. Hire an administrator first.\nA city-hall chair holder cannot take this seat.");

        ImGui.SameLine();
        if (!seated)
            ImGui.BeginDisabled();
        if (ImGui.Button("Unseat###industry-unseat-" + colonyId) && seated)
        {
            Submit(uiState, new UnseatIndustryHeadCommand(colonyId));
            TakePendingRefusal(ref refusal);
        }
        if (!seated)
            ImGui.EndDisabled();
        if (!seated && ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))
            ImGui.SetTooltip("The industry seat is already empty.");
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
            _pendingRefusal = IndustrySeatUi.RefusalMessage(task.Result);
            _pendingReady = true;
            return;
        }
        task.ContinueWith(t =>
        {
            if (!t.IsCompletedSuccessfully)
                return;
            _pendingRefusal = IndustrySeatUi.RefusalMessage(t.Result);
            _pendingReady = true;
        });
    }
}
