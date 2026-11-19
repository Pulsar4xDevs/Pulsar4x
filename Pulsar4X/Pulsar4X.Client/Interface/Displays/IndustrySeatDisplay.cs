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
        PortraitRenderer.For(uiState).Draw(PortraitIdentity.IndustryHead(view), PortraitRenderer.SmallSize);
        ImGui.SameLine();
        ImGui.TextUnformatted("Industry head: " + IndustrySeatUi.HolderText(view.IndustryHeadId, view.IndustryHeadName));

        var candidates = IndustrySeatUi.Candidates(
            galaxy.Commanders,
            IndustrySeatUi.CityHallIds(IndustrySeatUi.ColonyViews(galaxy)));
        bool seated = IndustrySeatUi.IsSeated(view.IndustryHeadId);
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

        if (!string.IsNullOrEmpty(refusal))
        {
            ImGui.PushStyleColor(ImGuiCol.Text, Styles.TerribleColor);
            ImGui.TextUnformatted(refusal);
            ImGui.PopStyleColor();
        }
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
        ResultModal.GetInstance().DisplayCustomButtons(
            "Seat industry head",
            () => _chooserColonyId = null,
            close =>
            {
                if (candidates.Count == 0)
                    ImGui.TextDisabled("No unassigned civilians. A city-hall chair holder cannot take this seat.");

                int selected = DisplayHelpers.PeopleChooser(
                    uiState,
                    candidates,
                    -1,
                    "industry-head_" + colonyId,
                    close,
                    person => person.Name,
                    320f);
                if (selected >= 0)
                {
                    Submit(uiState, new SeatIndustryHeadCommand(colonyId, selected));
                    close();
                }
            });
    }

    public static void DrawProduction(IndustryView industry)
    {
        ImGui.TextUnformatted("Industry head: " + IndustrySeatUi.HolderText(industry.IndustryHeadId, industry.IndustryHeadName));
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
