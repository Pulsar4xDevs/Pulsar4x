using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using ImGuiNET;
using Pulsar4X.DataStructures;

namespace Pulsar4X.SDL2UI.ModFileEditing;

public static class DictEditWidget
{
    private static string? _editingID;
    private static int _editInt;
    private static string _editStr;
    private static long _editLong;
    private static double _editDouble;
    private static uint _buffSize = 128;
    private static byte[] _strInputBuffer = new byte[128];
    private static int _techIndex = 0;
    private static int _addKey = -1;
    private static int _addVal = -1;
    public static bool Display(string label, ref Dictionary<int, List<string>> dict, string[] techs)
    {
        ImGui.BeginChild("##dic");
        ImGui.Columns(2);
        ImGui.SetColumnWidth(0, 64);
        bool isChanged = false;
        _addKey = -1;
        foreach (var kvp in dict.ToArray())
        {
            _editInt = kvp.Key;
            int oldVal = kvp.Key;
            if (IntEditWidget.Display(label + _editInt, ref _editInt))
            {
                isChanged = true;
                if(!dict.ContainsKey(_editInt))
                {
                    dict.Add(_editInt, kvp.Value);
                    dict.Remove(oldVal);
                }
            }
            ImGui.NextColumn();
            //values list
            int valIndex = 0;
            foreach (var item in kvp.Value.ToArray())
            {
                _techIndex = Array.IndexOf(techs, item);
                if(SelectFromListWiget.Display(label+"chValue", techs, ref _techIndex))
                {
                    dict[kvp.Key][valIndex] = techs[_techIndex];
                }
                valIndex++;
            }
            
            if(_editingID != label+"addValue")
            {
                if (ImGui.Button("+##addval" + label))
                {
                    _editingID = label+"addValue";
                }
            }
            else
            {
                if (SelectFromListWiget.Display(label+"addValue", techs, ref _techIndex))
                {
                    dict[kvp.Key].Add(techs[_techIndex]);
                    _editingID = null;
                }
            }
            ImGui.NextColumn();
            ImGui.Separator();
        }

        //if (dict.Count == 0)
        {
            if(_editingID != label+"addKey")
            {
                if (ImGui.Button("+"))
                {
                    _editingID = label+"addKey";
                }
            }
            else
            {
                _addKey = dict.Keys.Count;
                while (dict.ContainsKey(_addKey))
                    _addKey++;
                dict.Add(_addKey, new List<string>());
                _editingID = null;
            }
        }
        
        ImGui.EndChild();
        return isChanged;
    }
    
    public static bool Display(string label, ref Dictionary<string, string> dict)
    {
        ImGui.BeginChild("##dic" + label, new Vector2(400,160), true);
        ImGui.Columns(2);
        bool isChanged = false;
        if (dict is null)
            dict = new Dictionary<string, string>();
        _addKey = -1;
        foreach (var kvp in dict)
        {
            _editStr = kvp.Key;
            if (TextEditWidget.Display(label + kvp.Key + "k", ref _editStr))
            {
                isChanged = true;
                if(!dict.ContainsKey(_editStr))
                    dict.Add(_editStr,kvp.Value);
            }
            ImGui.NextColumn();
            
            //values
            _editStr = kvp.Value;
            if(TextEditWidget.Display(label+kvp.Key + "v", ref _editStr))
            {
                dict[kvp.Key] = _editStr;
            }
            ImGui.NextColumn();
        }
        ImGui.Columns(0);
        ImGui.NewLine();
        ImGui.EndChild();

        return isChanged;
    }
    
    /// <summary>
    /// Note this casts to an int, not long.
    /// </summary>
    /// <param name="label"></param>
    /// <param name="dict"></param>
    /// <returns></returns>
    public static bool Display(string label, ref Dictionary<string, long> dict)
    {
        ImGui.BeginChild("##dic" + label, new Vector2(400,160), true);
        ImGui.Columns(2);
        bool isChanged = false;
        if (dict is null)
            dict = new Dictionary<string, long>();
        _addKey = -1;
        foreach (var kvp in dict)
        {
            _editStr = kvp.Key;
            if (TextEditWidget.Display(label + kvp.Key + "k", ref _editStr))
            {
                isChanged = true;
                if(!dict.ContainsKey(_editStr))
                    dict.Add(_editStr,kvp.Value);
            }
            ImGui.NextColumn();
            
            //values
            _editInt = (int)kvp.Value;
            if(IntEditWidget.Display(label+kvp.Key + "v", ref _editInt))
            {
                dict[kvp.Key] = _editInt;
            }
            ImGui.NextColumn();
        }

        if (ImGui.Button("+##addkey" + label))
        {
            dict.Add("???", 0);
        }
        
        
        ImGui.Columns(0);
        ImGui.NewLine();
        ImGui.EndChild();

        return isChanged;
    }
    
    public static bool Display(string label, ref Dictionary<BodyType, double> dict)
    {
        ImGui.BeginChild("##dic" + label, new Vector2(400,160), true);
        ImGui.Columns(2);
        bool isChanged = false;
        if (dict is null)
        {
            dict = new Dictionary<BodyType, double>();
            foreach (var bodyType in Enum.GetValues(typeof(BodyType)))
            {
                dict.Add((BodyType)bodyType, 0);
            }
        }
        _addKey = -1;
        foreach (var kvp in dict)
        {
            _editStr = Enum.GetName(kvp.Key);
            _editDouble = kvp.Value;
            
            ImGui.Text(_editStr);
            ImGui.NextColumn();

            if(DoubleEditWidget.Display(label+_editStr,ref _editDouble))
            {
                dict[kvp.Key] = _editDouble;
            }
            ImGui.NextColumn();

        }

        ImGui.EndChild();
        return isChanged;
    }
    
}