using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using ImGuiNET;
using Pulsar4X.Engine;
using Pulsar4X.Engine.Industry;
using Pulsar4X.Extensions;
using Pulsar4X.Interfaces;
using Pulsar4X.Modding;
using Pulsar4X.Orbital;

namespace Pulsar4X.SDL2UI;

public class DataViewerWindow : PulsarGuiWindow
{

    SystemState? _systemState;
    private ModDataStore? _modDataStore;

    internal static DataViewerWindow GetInstance()
    {
        DataViewerWindow instance;
        if (!_uiState.LoadedWindows.ContainsKey(typeof(DataViewerWindow)))
            instance = new DataViewerWindow(_uiState.Game.StartingGameData);
        else
        {
            instance = (DataViewerWindow)_uiState.LoadedWindows[typeof(DataViewerWindow)];
        }

        instance._systemState = _uiState.StarSystemStates[_uiState.SelectedStarSysGuid];
        return instance;
    }

    private DataViewerWindow(ModDataStore modDataStore)
    {
        _modDataStore = modDataStore;
    }

    internal override void Display()
    {
        if(!IsActive || _modDataStore == null) return;

        if (ImGui.Begin("Mod Data", ref IsActive))
        {
            
            Type objType = _modDataStore.GetType();
            BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;
            MemberInfo[] memberInfos = objType.GetMembers(flags);
            ModDataInspector.DisplayDataObj(_modDataStore);
            
        }
    }

}


public static class ModDataInspector
{
    private static int _numLines;
    private static float _heightMultiplyer = ImGui.GetTextLineHeightWithSpacing();

    private static bool _isActive = false;
    private static int _selectedItem = 0;
    
    public static void DisplayDataObj(object dataObj)
    {
        
        object? value = null;
        Type objType = dataObj.GetType();
        BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.GetProperty;
        MemberInfo[] memberInfos = objType.GetMembers(flags);

        List<string> stList = new List<string>();
        List<int> itemIndexList = new List<int>();
        int memInfosIndex = 0;
        foreach (var memberInfo in memberInfos)
        {
            if (typeof(FieldInfo).IsAssignableFrom(memberInfo.GetType()) || typeof(PropertyInfo).IsAssignableFrom(memberInfo.GetType()))
            {
                value = GetValue(memberInfo, dataObj);
                if (value == null)
                    continue;
                stList.Add( memberInfo.Name);
                itemIndexList.Add(memInfosIndex);
            }
            memInfosIndex++;
        }

        string[] stArray = stList.ToArray();
        
        BorderListOptions.Begin("DataItems:", stArray, ref _selectedItem, 300f);
        
        var p0 = ImGui.GetCursorPos();

        if (_selectedItem >= stArray.Length)
            _selectedItem = -1;

        if(_selectedItem >= 0)
        {
            MemberInfo memberInfo = memberInfos[itemIndexList[_selectedItem]];
            value = GetValue(memberInfo, dataObj);
            var _totalHeight = _numLines * _heightMultiplyer;
            _numLines = memberInfos.Length;
            var chsize = new System.Numerics.Vector2(ImGui.GetContentRegionAvail().X, _totalHeight);
            ImGui.BeginChild("InnerColomns", chsize);
            ImGui.Columns(2);

            if (value != null && typeof(IDictionary).IsAssignableFrom(value.GetType()))
            {
                var items = (IDictionary?)GetValue(memberInfo, dataObj);
                if (items != null)
                {
                    int itemsCount = items.Count;
                    lock (items) //TODO: IDK the best way to fix this.
                    {
                        foreach (var item in items)
                        {
                            DictionaryEntry de = (DictionaryEntry)item;

                            if (ImGui.TreeNode(de.Key.ToString()))
                            {
                                ImGui.NextColumn();
                                ImGui.NextColumn();
                                _numLines += itemsCount;
                                RecursiveReflection(de.Value);
                                ImGui.Unindent();
                                ImGui.Separator();
                            }
                            else
                            {

                            }
                        }
                    }
                    ImGui.TreePop();
                }
            }
            
            
            else if (value != null && typeof(ICollection).IsAssignableFrom(value.GetType()))
            {
                var items = (ICollection?)GetValue(memberInfo, dataObj);
                if (items != null)
                {
                    int itemsCount = items.Count;
                    if (ImGui.TreeNode(memberInfo.Name))
                    {
                        ImGui.NextColumn();
                        ImGui.Text("Count: " + itemsCount);
                        ImGui.NextColumn();
                        _numLines += itemsCount;
                        lock (items) //TODO: IDK the best way to fix this.
                        {
                            foreach (var item in items)
                            {
                                RecursiveReflection(item);
                            }
                        }

                        ImGui.TreePop();
                    }
                    else
                    {
                        ImGui.NextColumn();
                        ImGui.Text("Count: " + itemsCount);
                        ImGui.NextColumn();
                    }
                }
            }
            
            else
            {
                ImGui.Text(memberInfo.Name);
                ImGui.NextColumn();
                //object value = memberInfo.GetValue(obj);
                string? displayStr = "null";
                string tooltipStr = "";
                if (value != null)
                {
                    if(value is string)
                    {
                        var guid = (string)value;
                        displayStr = guid.ToString();
                        
                    }
                    else if(value is Entity)
                    {
                        var entity = (Entity)value;
                        displayStr = entity.GetOwnersName();
                        tooltipStr = "ID: " + entity.Id.ToString();
                    }
                    else if (value is Vector2)
                    {
                        displayStr = value.ToString();
                        Vector2 v = (Vector2)value;
                        tooltipStr = "Magnitude: " + Stringify.Number(v.Length());
                    }
                    else if (value is Vector3)
                    {
                        displayStr = value.ToString();
                        Vector3 v = (Vector3)value;
                        tooltipStr = "Magnitude: " + Stringify.Number(v.Length());
                    }
                    else
                    {
                        displayStr = value.ToString();
                    }

                    if (value is ProcessedMaterial)
                    {
                        ProcessedMaterial mat = (ProcessedMaterial)value;
                        displayStr = ("MaterialSD: " + mat.Name);
                    }

                    if (value is IConstructableDesign)
                    {
                        IConstructableDesign constD = (IConstructableDesign)value;
                        displayStr = "Constructable: " + constD.Name;
                    }
                    if (value is (Tech tech ,int pointsResearched, int pointCost))
                    {
                        (Tech tech ,int pointsResearched, int pointCost) tval = ((Tech tech ,int pointsResearched, int pointCost))value;
                        displayStr = "TechSD: " + tval.tech.Name + " Points Researched: " + tval.pointsResearched + " / " +tval.pointCost;
                    }
                }
                ImGui.Text(displayStr);
                if (ImGui.IsItemHovered())
                    ImGui.SetTooltip(tooltipStr);
                ImGui.NextColumn();
            }
            
            
            
            ImGui.Columns(0);
            ImGui.EndChild();
        }

        var p1 = ImGui.GetCursorPos();
        var size = new System.Numerics.Vector2(ImGui.GetContentRegionAvail().X, p1.Y - p0.Y );

        BorderListOptions.End(size);
    }
    
        static void RecursiveReflection(object? obj)
        {
            if(obj == null) return;
            object? value = null;
            Type objType = obj.GetType();
            BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;
            MemberInfo[] memberInfos = objType.GetMembers(flags);
            foreach (var memberInfo in memberInfos)
            {
                if (typeof(FieldInfo).IsAssignableFrom(memberInfo.GetType()) || typeof(PropertyInfo).IsAssignableFrom(memberInfo.GetType()))
                {
                    value = GetValue(memberInfo, obj);
                    if(value == null || memberInfo.GetCustomAttribute<CompilerGeneratedAttribute>()!= null)
                        continue;
                    if (typeof(ICollection).IsAssignableFrom(value.GetType()))
                    {
                        var items = (ICollection?)GetValue(memberInfo, obj);
                        if(items == null) continue;
                        int itemsCount = items.Count;

                        if (ImGui.TreeNode(memberInfo.Name))
                        {
                            ImGui.NextColumn();
                            ImGui.Text("Count: " + itemsCount);
                            ImGui.NextColumn();
                            _numLines += itemsCount;
                            lock (items)//TODO: IDK the best way to fix this.
                            {
                                foreach (var item in items)
                                {
                                    RecursiveReflection(item);
                                }
                            }

                            ImGui.TreePop();
                        }
                        else
                        {
                            ImGui.NextColumn();
                            ImGui.Text("Count: " + itemsCount);
                            ImGui.NextColumn();
                        }
                    }
                    else if (typeof(HashSet<Tech>).IsAssignableFrom(value.GetType()))
                    {
                        var items = (HashSet<Tech>?)GetValue(memberInfo, obj);
                        if(items == null) continue;
                        int itemsCount = items.Count;

                        if (ImGui.TreeNode(memberInfo.Name))
                        {
                            ImGui.NextColumn();
                            ImGui.Text("Count: " + itemsCount);
                            ImGui.NextColumn();
                            _numLines += itemsCount;
                            lock (items)//TODO: IDK the best way to fix this.
                            {
                                foreach (var item in items)
                                {
                                    RecursiveReflection(item);
                                }
                            }

                            ImGui.TreePop();
                        }
                        else
                        {
                            ImGui.NextColumn();
                            ImGui.Text("Count: " + itemsCount);
                            ImGui.NextColumn();
                        }
                    }
                    else if (typeof(IDictionary).IsAssignableFrom(value.GetType()))
                    {
                        var items = (IDictionary?)GetValue(memberInfo, obj);
                        if(items == null) continue;
                        int itemsCount = items.Count;

                        if (ImGui.TreeNode(memberInfo.Name))
                        {
                            ImGui.NextColumn();
                            ImGui.Text("Count: " + itemsCount);
                            ImGui.NextColumn();
                            _numLines += itemsCount;
                            lock (items) //TODO: IDK the best way to fix this.
                            {
                                foreach (var item in items)
                                {
                                    RecursiveReflection(item);
                                }
                            }

                            ImGui.TreePop();
                        }
                        else
                        {
                            ImGui.NextColumn();
                            ImGui.Text("Count: " + itemsCount);
                            ImGui.NextColumn();
                        }
                    }
                    else if (typeof(KeplerElements).IsAssignableFrom(value.GetType()))
                    {
                        //var items = (KeplerElements)GetValue(memberInfo, obj);
                        MemberInfo[] memberInfoske =  typeof(KeplerElements).GetMembers(flags);
                        int itemsCount = memberInfoske.Length;

                        if (ImGui.TreeNode(memberInfo.Name))
                        {
                            ImGui.NextColumn();
                            ImGui.Text("Count: " + itemsCount);
                            ImGui.NextColumn();
                            _numLines += itemsCount;

                                foreach (var memberInfoke in memberInfoske)
                                {
                                    object? valueke = GetValue(memberInfoke, value);
                                    ImGui.Text(memberInfoke.Name);
                                    ImGui.NextColumn();
                                    //object value = memberInfo.GetValue(obj);
                                    if (valueke != null)
                                        ImGui.Text(valueke.ToString());
                                    else ImGui.Text("null");
                                    ImGui.NextColumn();
                                }

                            ImGui.TreePop();
                        }
                        else
                        {
                            ImGui.NextColumn();
                            ImGui.Text("Count: " + itemsCount);
                            ImGui.NextColumn();
                        }
                    }
                    else
                    {
                        ImGui.Text(memberInfo.Name);
                        ImGui.NextColumn();
                        //object value = memberInfo.GetValue(obj);
                        string? displayStr = "null";
                        string tooltipStr = "";
                        if (value != null)
                        {
                            if(value is string)
                            {
                                var guid = (string)value;
                                displayStr = guid.ToString();
                                
                            }
                            else if(value is Entity)
                            {
                                var entity = (Entity)value;
                                displayStr = entity.GetOwnersName();
                                tooltipStr = "ID: " + entity.Id.ToString();
                            }
                            else if (value is Vector2)
                            {
                                displayStr = value.ToString();
                                Vector2 v = (Vector2)value;
                                tooltipStr = "Magnitude: " + Stringify.Number(v.Length());
                            }
                            else if (value is Vector3)
                            {
                                displayStr = value.ToString();
                                Vector3 v = (Vector3)value;
                                tooltipStr = "Magnitude: " + Stringify.Number(v.Length());
                            }
                            else
                            {
                                displayStr = value.ToString();
                            }

                            if (value is ProcessedMaterial)
                            {
                                ProcessedMaterial mat = (ProcessedMaterial)value;
                                displayStr = ("MaterialSD: " + mat.Name);
                            }

                            if (value is IConstructableDesign)
                            {
                                IConstructableDesign constD = (IConstructableDesign)value;
                                displayStr = "Constructable: " + constD.Name;
                            }
                            if (value is (Tech tech ,int pointsResearched, int pointCost))
                            {
                                (Tech tech ,int pointsResearched, int pointCost) tval = ((Tech tech ,int pointsResearched, int pointCost))value;
                                displayStr = "TechSD: " + tval.tech.Name + " Points Researched: " + tval.pointsResearched + " / " +tval.pointCost;
                            }
                        }
                        ImGui.Text(displayStr);
                        if (ImGui.IsItemHovered())
                            ImGui.SetTooltip(tooltipStr);
                        ImGui.NextColumn();
                    }
                }
            }
        }
    
    static object? GetValue(this MemberInfo memberInfo, object forObject)
    {
        
            switch (memberInfo.MemberType)
            {
                case MemberTypes.Field:
                {

                    return ((FieldInfo)memberInfo).GetValue(forObject);
                }
                case MemberTypes.Property:
                {
                    try
                    {
                        return ((PropertyInfo)memberInfo).GetValue(forObject);
                    }
                    catch (Exception)
                    {
                        return "";
                    }

                }

            }
        
        return "";
    }
}