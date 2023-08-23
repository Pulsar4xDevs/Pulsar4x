using System.Numerics;
using ImGuiNET;
using Pulsar4X.ECSLib;

namespace Pulsar4X.SDL2UI;

public static class PowerDBDisplay
{
    static Vector2 _plotSize = new Vector2(512, 64);
    public static void Display(this EnergyGenAbilityDB _energyGenDB, EntityState entityState, GlobalUIState uiState)
    {
        if (_energyGenDB.dateTimeLastProcess != uiState.SelectedSystemTime)
            EnergyGenProcessor.EnergyGen(entityState.Entity, uiState.SelectedSystemTime);
        
        ImGui.Text("Current Load: ");
        ImGui.SameLine();
        ImGui.Text(_energyGenDB.Load.ToString());
        
        ImGui.Text("Current Output: ");
        ImGui.SameLine();
        
        ImGui.Text(_energyGenDB.Output.ToString() + " / " + _energyGenDB.TotalOutputMax);
        
        ImGui.Text("Current Demand: ");
        ImGui.SameLine();
        ImGui.Text(_energyGenDB.Demand.ToString());
        
        ImGui.Text("Stored: ");
        ImGui.SameLine();
        string stor = _energyGenDB.EnergyStored[_energyGenDB.EnergyType.ID].ToString();
        string max = _energyGenDB.EnergyStoreMax[_energyGenDB.EnergyType.ID].ToString();
        ImGui.Text(stor + " / " + max);

        //

        //ImGui.PlotLines()
        var colour1 = ImGui.GetColorU32(ImGuiCol.Text);
        var colour2 = ImGui.GetColorU32(ImGuiCol.PlotLines);
        var colour3 = ImGui.GetColorU32(ImGuiCol.Button);
        ImDrawListPtr draw_list = ImGui.GetWindowDrawList();

        var plotPos = ImGui.GetCursorScreenPos();
        ImGui.InvisibleButton("PowerPlot", _plotSize);


        var hg = _energyGenDB.Histogram;
        
        int hgFirstIdx = _energyGenDB.HistogramIndex;
        int hgLastIdx;
        if (hgFirstIdx == 0)
            hgLastIdx = hg.Count - 1;
        else
            hgLastIdx = hgFirstIdx - 1;
    
        var hgFirstObj = hg[hgFirstIdx];
        var hgLastObj = hg[hgLastIdx];
        
        
        float xstep = _plotSize.X / hgLastObj.seconds ;
        float ystep = (float)(_plotSize.Y / _energyGenDB.EnergyStoreMax[_energyGenDB.EnergyType.ID]);
        float posX = 0;
        float posYBase = plotPos.Y + _plotSize.Y;
        int index = _energyGenDB.HistogramIndex;
        var thisData = _energyGenDB.Histogram[index];
        float posYO = ystep * (float)thisData.outputval;
        float posYD = ystep * (float)thisData.demandval;
        float posYS = ystep * (float)thisData.storval;
        //float ypos = plotPos.Y + _plotSize.Y;
        
        for (int i = 0; i < _energyGenDB.HistogramSize; i++)
        {
            
            int idx = index + i;
            if (idx >= _energyGenDB.HistogramSize)
                idx -= _energyGenDB.HistogramSize;
            thisData = _energyGenDB.Histogram[idx];
            
            float nextX = xstep * thisData.seconds;
            float nextYO = ystep * (float)thisData.outputval;
            float nextYD = ystep * (float)thisData.demandval;
            float nextYS = ystep * (float)thisData.storval;
            draw_list.AddLine(new Vector2(plotPos.X + posX, posYBase - posYO), new Vector2(plotPos.X + nextX, posYBase - nextYO), colour1);
            draw_list.AddLine(new Vector2(plotPos.X + posX, posYBase - posYD), new Vector2(plotPos.X + nextX, posYBase - nextYD), colour2);
            draw_list.AddLine(new Vector2(plotPos.X + posX, posYBase - posYS), new Vector2(plotPos.X + nextX, posYBase - nextYS), colour3);
            posX = nextX;
            posYO = nextYO;
            posYD = nextYD;
            posYS = nextYS;
        }
    }
}