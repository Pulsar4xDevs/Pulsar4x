using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using ImGuiNET;
using Pulsar4X.Client.Interface.Widgets;
using Pulsar4X.Engine;
using Pulsar4X.Datablobs;
using Pulsar4X.Orbital;
using Pulsar4X.Interfaces;
using Pulsar4X.Industry;
using Pulsar4X.Components;
using Pulsar4X.DataStructures;
using Pulsar4X.Extensions;
using Pulsar4X.Factions;
using Pulsar4X.Names;
using Pulsar4X.Sensors;
using Pulsar4X.Technology;
using Stringify = Pulsar4X.Api.Stringify;

namespace Pulsar4X.Client
{
    public static class EntityInspector
    {
        private static int _entityID = -1;
        private static BaseDataBlob[] _dataBlobs = new BaseDataBlob[0];
        private static int _selectedDB = -1;
        //private static float _totalHeight;
        private static int _numLines;
        private static float _heightMultiplyer = ImGui.GetTextLineHeightWithSpacing();

        private static bool _isActive = false;
        private static int _depth;
        private static readonly HashSet<object> _seen = new(ReferenceEqualityComparer.Instance);
        const int MaxDepth = 12;
        const int MaxItems = 200;

        /// <summary>
        /// use this to display the inspector as it's own window
        /// </summary>
        /// <param name="entity"></param>
        public static void Begin(Entity entity)
        {
            if (Window.Begin("Entity Inspector", ref _isActive))
            {
                if(entity.Id != _entityID || (entity.Manager != null && entity.Manager.GetAllDataBlobsForEntity(entity.Id).Count != _dataBlobs.Length))
                    Refresh(entity);

                DisplayDatablobs(entity);
            }

            Window.End();

        }


        public static void Refresh(Entity entity)
        {
            _entityID = entity.Id;
            if(entity.Manager != null)
                _dataBlobs = entity.Manager.GetAllDataBlobsForEntity(entity.Id).ToArray();
        }

        /// <summary>
        /// This can be used to display the inspector within another window
        /// </summary>
        /// <param name="entity"></param>
        public static void DisplayDatablobs(Entity entity)
        {
            if (_dataBlobs.Length < 1 || _entityID != entity.Id)
            {
                Refresh(entity);
            }


            string[] stArray = new string[_dataBlobs.Length];
            for (int i = 0; i < _dataBlobs.Length; i++)
            {
                var db = _dataBlobs[i];
                stArray[i] = db.GetType().ToString();

            }
            BorderListOptions.Begin("DataBlobs:", stArray, ref _selectedDB, 300f);

            var p0 = ImGui.GetCursorPos();

            if (_selectedDB >= _dataBlobs.Length)
                _selectedDB = -1;

            if(_selectedDB >= 0)
                DBDisplay(_dataBlobs[_selectedDB]);

            var p1 = ImGui.GetCursorPos();
            var size = new System.Numerics.Vector2(ImGui.GetContentRegionAvail().X, p1.Y - p0.Y );

            BorderListOptions.End(size);
        }

        public static void DBDisplay(BaseDataBlob dataBlob)
        {
            // Height follows the rows drawn last frame, including opened lists and private fields.
            var _totalHeight = (_numLines + 1) * _heightMultiplyer;
            _numLines = 0;
            var size = new System.Numerics.Vector2(ImGui.GetContentRegionAvail().X, _totalHeight);

            ImGui.BeginChild("InnerColomns", size);

            ImGui.Columns(2);

            _seen.Clear();
            _depth = 0;
            RecursiveReflection(dataBlob);


            ImGui.Columns(1);

            ImGui.EndChild();
            DisplayDBSpecifics(dataBlob);
        }

        static void RecursiveReflection(object obj)
        {
            // Structs are copied, so a cycle has to go through a class.
            if (obj is not ValueType && !_seen.Add(obj))
            {
                Row("…", "cycle");
                return;
            }

            _depth++;
            try
            {
                if (_depth > MaxDepth)
                {
                    Row("…", "nested too deep");
                    return;
                }

                BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
                foreach (var memberInfo in obj.GetType().GetMembers(flags))
                {
                    if (memberInfo is not FieldInfo and not PropertyInfo)
                        continue;
                    if (memberInfo.GetCustomAttribute<CompilerGeneratedAttribute>() != null)
                        continue;
                    if (memberInfo.Name == "_lock")
                        continue;
                    if (memberInfo is PropertyInfo property)
                    {
                        if (!property.CanRead || property.GetIndexParameters().Length > 0)
                            continue;
                    }

                    object? value;
                    try
                    {
                        value = GetValue(memberInfo, obj);
                    }
                    catch (Exception e)
                    {
                        Row(memberInfo.Name, ErrorText(e));
                        continue;
                    }

                    try
                    {
                        ShowMember(memberInfo.Name + "###" + memberInfo.MetadataToken, value);
                    }
                    catch (Exception e)
                    {
                        Row(memberInfo.Name, ErrorText(e));
                    }
                }
            }
            finally
            {
                _depth--;
                if (obj is not ValueType)
                    _seen.Remove(obj);
            }
        }

        static void ShowMember(string label, object? value)
        {
            if (value == null)
            {
                Row(Visible(label), "null");
                return;
            }

            if (value is string || IsSimple(value.GetType()))
            {
                Row(Visible(label), SafeSummary(value), SafeTooltip(value));
                return;
            }

            if (value is byte[] bytes)
            {
                Row(Visible(label), "byte[" + bytes.Length + "]");
                return;
            }

            if (IsDictionary(value))
            {
                ShowDictionary(label, value);
                return;
            }

            if (value is IEnumerable)
            {
                ShowSequence(label, value);
                return;
            }

            if (IsLeaf(value))
            {
                Row(Visible(label), SafeSummary(value), SafeTooltip(value));
                return;
            }

            if (IsExpandable(value))
            {
                ShowExpandable(label, value);
                return;
            }

            Row(Visible(label), SafeSummary(value), SafeTooltip(value));
        }

        static void ShowDictionary(string label, object value)
        {
            int count = CountOf(value);
            bool open = ImGui.TreeNode(label);
            ImGui.NextColumn();
            ImGui.TextUnformatted(count < 0 ? "" : "Count: " + count);
            ImGui.NextColumn();
            _numLines++;
            if (!open)
                return;

            try
            {
                int index = 0;
                foreach (var item in (IEnumerable)value)
                {
                    if (index >= MaxItems)
                    {
                        Row("…", count < 0 ? "more" : (count - index) + " more");
                        break;
                    }

                    try
                    {
                        if (item != null && TryPair(item, out var key, out var pairValue))
                        {
                            string keyText = key == null ? "null" : SafeSummary(key).Replace("###", "#");
                            ShowMember(keyText + "###k" + index, pairValue);
                        }
                        else
                        {
                            ShowMember("[" + index + "]", item);
                        }
                    }
                    catch (Exception e)
                    {
                        Row("[" + index + "]", ErrorText(e));
                    }
                    index++;
                }
            }
            catch (Exception e)
            {
                Row("…", ErrorText(e));
            }
            finally
            {
                ImGui.TreePop();
            }
        }

        static void ShowSequence(string label, object value)
        {
            int count = CountOf(value);
            bool open = ImGui.TreeNode(label);
            ImGui.NextColumn();
            ImGui.TextUnformatted(count < 0 ? "" : "Count: " + count);
            ImGui.NextColumn();
            _numLines++;
            if (!open)
                return;

            try
            {
                int index = 0;
                foreach (var item in (IEnumerable)value)
                {
                    if (index >= MaxItems)
                    {
                        Row("…", count < 0 ? "more" : (count - index) + " more");
                        break;
                    }

                    try
                    {
                        ShowMember("[" + index + "]", item);
                    }
                    catch (Exception e)
                    {
                        Row("[" + index + "]", ErrorText(e));
                    }
                    index++;
                }
            }
            catch (Exception e)
            {
                Row("…", ErrorText(e));
            }
            finally
            {
                ImGui.TreePop();
            }
        }

        static void ShowExpandable(string label, object value)
        {
            bool open = ImGui.TreeNode(label);
            ImGui.NextColumn();
            ImGui.TextUnformatted(SafeSummary(value));
            ImGui.NextColumn();
            _numLines++;
            if (!open)
                return;

            try
            {
                RecursiveReflection(value);
            }
            finally
            {
                ImGui.TreePop();
            }
        }

        static void Row(string name, string value, string? tooltip = null)
        {
            ImGui.TextUnformatted(Visible(name));
            ImGui.NextColumn();
            ImGui.TextUnformatted(value);
            if (!string.IsNullOrEmpty(tooltip) && ImGui.IsItemHovered())
            {
                ImGui.BeginTooltip();
                ImGui.TextUnformatted(tooltip);
                ImGui.EndTooltip();
            }
            ImGui.NextColumn();
            _numLines++;
        }

        // TreeNode shows the text before ### and uses the rest as a stable id.
        static string Visible(string label)
        {
            int hash = label.IndexOf("###", StringComparison.Ordinal);
            return hash < 0 ? label : label.Substring(0, hash);
        }

        static bool IsDictionary(object value)
        {
            if (value is IDictionary)
                return true;

            var type = value.GetType();
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(SafeDictionary<,>))
                return true;

            foreach (var iface in type.GetInterfaces())
            {
                if (!iface.IsGenericType || iface.GetGenericTypeDefinition() != typeof(IEnumerable<>))
                    continue;
                var arg = iface.GetGenericArguments()[0];
                if (arg.IsGenericType && arg.GetGenericTypeDefinition() == typeof(KeyValuePair<,>))
                    return true;
            }
            return false;
        }

        static bool TryPair(object item, out object? key, out object? value)
        {
            if (item is DictionaryEntry entry)
            {
                key = entry.Key;
                value = entry.Value;
                return true;
            }

            var type = item.GetType();
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(KeyValuePair<,>))
            {
                key = type.GetProperty("Key")?.GetValue(item);
                value = type.GetProperty("Value")?.GetValue(item);
                return true;
            }

            key = null;
            value = null;
            return false;
        }

        static int CountOf(object value)
        {
            if (value is ICollection collection)
                return collection.Count;
            var count = value.GetType().GetProperty("Count");
            if (count != null && count.GetValue(value) is int number)
                return number;
            return -1;
        }

        static bool IsSimple(Type type)
        {
            if (type.IsPrimitive || type.IsEnum)
                return true;
            return type == typeof(string)
                || type == typeof(decimal)
                || type == typeof(DateTime)
                || type == typeof(DateTimeOffset)
                || type == typeof(TimeSpan)
                || type == typeof(Guid);
        }

        static bool IsLeaf(object value)
        {
            return value is Entity
                or Delegate
                or Vector2
                or Vector3
                or ProcessedMaterial
                or IConstructableDesign
                or ValueTuple<Tech, int, int>;
        }

        static bool IsExpandable(object value)
        {
            var type = value.GetType();
            if (type.Namespace == null || !type.Namespace.StartsWith("Pulsar4X", StringComparison.Ordinal))
                return false;
            // Opening the whole simulation from a stray reference would freeze the window.
            return type.Name is not ("Game" or "EntityManager" or "StarSystem" or "ModDataStore");
        }

        static string SafeSummary(object value)
        {
            try
            {
                return Summary(value);
            }
            catch (Exception e)
            {
                return ErrorText(e);
            }
        }

        static string? SafeTooltip(object value)
        {
            try
            {
                return TooltipFor(value);
            }
            catch
            {
                return null;
            }
        }

        static string ErrorText(Exception e)
        {
            if (e is TargetInvocationException && e.InnerException != null)
                e = e.InnerException;
            return e.GetType().Name;
        }

        static string Summary(object value)
        {
            switch (value)
            {
                case string text:
                    return text;
                case Entity entity:
                    return entity.GetOwnersName();
                case ProcessedMaterial material:
                    return "MaterialSD: " + material.Name;
                case IConstructableDesign design:
                    return "Constructable: " + design.Name;
                case ValueTuple<Tech, int, int> tech:
                    return "TechSD: " + tech.Item1.Name + " Points Researched: " + tech.Item2 + " / " + tech.Item3;
                case Ledger ledger:
                    return "Funds: " + ledger.GetCurrentFunds();
            }

            string? textValue = value.ToString();
            if (string.IsNullOrEmpty(textValue)
                || textValue == value.GetType().ToString()
                || textValue == value.GetType().FullName)
                return value.GetType().Name;
            return textValue;
        }

        static string? TooltipFor(object value)
        {
            switch (value)
            {
                case Entity entity:
                    return "ID: " + entity.Id;
                case Vector2 vector:
                    return "Magnitude: " + Stringify.Quantity(vector.Length());
                case Vector3 vector:
                    return "Magnitude: " + Stringify.Quantity(vector.Length());
                default:
                    return null;
            }
        }

        static object? GetValue(this MemberInfo memberInfo, object forObject)
        {
            switch (memberInfo.MemberType)
            {
                case MemberTypes.Field:
                    return ((FieldInfo)memberInfo).GetValue(forObject);
                case MemberTypes.Property:
                    return ((PropertyInfo)memberInfo).GetValue(forObject);

            }
            return "";
        }


        static void DisplayDBSpecifics(BaseDataBlob db)
        {
            Type type = db.GetType();
            switch (db)
            {
                case SensorProfileDB dbtype:
                    DebugDisplaySensorProfile.Display(dbtype);
                    break;
            }
        }


        static int _selectedDesign = -1;
        private static int _selectedComponent = -1;
        static void DisplayComponents(ComponentInstancesDB instancesDB)
        {
            if(instancesDB.OwningEntity == null || instancesDB.OwningEntity.Manager == null) return;

            var componentsByDesign = instancesDB.ComponentsByDesign;
            var faction = instancesDB.OwningEntity.Manager.Game.Factions[instancesDB.OwningEntity.FactionOwnerID];
            FactionInfoDB factionInfoDB = faction.GetDataBlob<FactionInfoDB>();

            string[] designNames = new string[componentsByDesign.Count];
            string[][] componentNames = new string[componentsByDesign.Count][];

            ComponentDesign[] designs = new ComponentDesign[componentsByDesign.Count];

            ComponentAbilityState[][][] states = new ComponentAbilityState[componentsByDesign.Count][][];

            int i = 0;
            foreach (var kvp in componentsByDesign)
            {
                var design = factionInfoDB.ComponentDesigns[kvp.Key];
                designNames[i] = design.Name;
                designs[i] = design;
                i++;

                int j = 0;
                componentNames[i] = new string[kvp.Value.Count];
                foreach (var component in kvp.Value)
                {
                    componentNames[i][j] = component.Name;
                    var allstates = component.GetAllStates();
                    states[i][j] = new ComponentAbilityState[allstates.Count];
                    int k = 0;
                    foreach (var state in allstates)
                    {
                        states[i][j][k] = state.Value;
                        //state.Value.Name;
                    }


                    //states[i][j] = component.GetAbilityState<>()
                    j++;
                }


            }
            //string[] componentInstances = .


            BorderListOptions.Begin("Components", designNames, ref _selectedDesign, 200);

            BorderListOptions.Begin("Instances", componentNames[_selectedDesign], ref _selectedComponent, 150);

            foreach (var state in states[_selectedDesign][_selectedComponent])
            {
                ImGui.Text(state.Name);
            }

            BorderListOptions.End(new System.Numerics.Vector2(200, 200));

            BorderListOptions.End(new System.Numerics.Vector2(250, 500));





        }


    }


    public static class DebugDisplaySensorProfile
    {
        public static void Display(SensorProfileDB db)
        {
            if(db.OwningEntity ==  null) return;

            if(!db.OwningEntity.TryGetDataBlob<ComponentInstancesDB>(out var componentInstancesDB))
            {
                return;
            }

            ImGui.Text("Reflected");
            foreach (var kvp in db.ReflectedEMSpectra)
            {
                DisplayValues(kvp.WaveForm, kvp.Magnitude);

            }

            ImGui.Text("Emmitted");
            foreach (var kvp in db.EmittedEMSpectra)
            {
                DisplayValues(kvp.WaveForm, kvp.Magnitude);
            }

            ImGui.Text("By Component:");
            var emmitterComponents = componentInstancesDB.ComponentsByAttribute[typeof(SensorSignatureAtb)];

            foreach (var component in emmitterComponents)
            {
                ImGui.Text(component.Name);
                ImGui.SameLine();
                ImGui.Text(" ("+ component.Design.TemplateName+")");
                SensorSignatureAtb emmitterAtbs = (SensorSignatureAtb)component.Design.AttributesByType[typeof(SensorSignatureAtb)];
                DisplayValues(emmitterAtbs.PartWaveForm, emmitterAtbs.PartWaveFormMag);
            }
        }

        public static void DisplayValues(EMWaveForm waveForm, double magnatude)
        {
            var min = waveForm.WavelengthMin_nm;
            var avg = waveForm.WavelengthAverage_nm;
            var max = waveForm.WavelengthMax_nm;
            var hight = magnatude;

            ImGui.Text(Stringify.DistanceSmall(min));
            ImGui.Text(Stringify.DistanceSmall(avg));
            ImGui.SameLine();
            ImGui.Text(Stringify.Power(hight));
            ImGui.Text(Stringify.DistanceSmall(max));
        }


        /*
        void DrawWav(WaveDrawData wavesArry, uint colour)
        {
            for (int i = 0; i < wavesArry.Count; i++)
            {
                Vector2 p0 = _translation + wavesArry.Points[i].p0 * _scalingFactor;
                Vector2 p1 = _translation + wavesArry.Points[i].p1 * _scalingFactor;
                Vector2 p2 = _translation + wavesArry.Points[i].p2 * _scalingFactor;
                if (wavesArry.IsWaveDrawn[i].drawSrc)
                {

                    //_draw_list.AddLine(p0, p1, colour);
                    //_draw_list.AddLine(p1, p2, colour);
                    _draw_list.AddTriangleFilled(p0, p1, p2, colour);
                }

                if (wavesArry.HasAtn && wavesArry.IsWaveDrawn[i].drawAtn)
                {
                    Vector2 p3 = _translation + wavesArry.Points[i].p3 * _scalingFactor;
                    _draw_list.AddTriangleFilled(p0, p3, p2, colour);
                }

            }

        } */
    }
}