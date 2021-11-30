using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Reflection;
using ImGuiNET;
using Pulsar4X.ECSLib;

namespace Pulsar4X.SDL2UI
{
    public static class EntityInspector
    {
        private static Guid _entityID = Guid.Empty;
        private static BaseDataBlob[] _dataBlobs = new BaseDataBlob[0];
        private static int _selectedDB = -1;
        //private static float _totalHeight;
        private static int _numLines;
        private static float _heightMultiplyer = ImGui.GetTextLineHeightWithSpacing();

        private static bool _isActive = false;
        
        /// <summary>
        /// use this to display the inspector as it's own window
        /// </summary>
        /// <param name="entity"></param>
        public static void Begin(Entity entity)
        {
            
            string ownerName = entity.GetDataBlob<NameDB>().OwnersName;
            if (ImGui.Begin("Entity Inspector:  " + ownerName, ref _isActive))
            {
                if(entity.Guid != _entityID || entity.DataBlobs.Count != _dataBlobs.Length)
                    Refresh(entity);
                
                DisplayDatablobs(entity);
                
                
                
            }

        }


        public static void Refresh(Entity entity)
        {
            _entityID = entity.Guid;
            _dataBlobs = entity.DataBlobs.ToArray();
        }

        /// <summary>
        /// This can be used to display the inspector within another window
        /// </summary>
        /// <param name="entity"></param>
        public static void DisplayDatablobs(Entity entity)
        {
            if (_dataBlobs.Length < 1 || _entityID != entity.Guid)
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
            Type dbType = dataBlob.GetType();

            MemberInfo[] memberInfos = dbType.GetMembers();
            
            var _totalHeight = _numLines * _heightMultiplyer;
            _numLines = memberInfos.Length;
            var size = new System.Numerics.Vector2(ImGui.GetContentRegionAvail().X, _totalHeight);
            
            ImGui.BeginChild("InnerColomns", size);
            
            ImGui.Columns(2);

            RecursiveReflection(dataBlob);
            

            ImGui.Columns(0);
            
            ImGui.EndChild();
            DisplayDBSpecifics(dataBlob);
        }

        static void RecursiveReflection(object obj)
        {
            
            Type objType = obj.GetType();
            BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;
            MemberInfo[] memberInfos = objType.GetMembers(flags);
            foreach (var memberInfo in memberInfos)
            {
                if (typeof(FieldInfo).IsAssignableFrom(memberInfo.GetType()) || typeof(PropertyInfo).IsAssignableFrom(memberInfo.GetType()))
                {
                    MemberTypes membertype = memberInfo.MemberType;
                    object value = GetValue(memberInfo, obj);
                    if(value == null)
                        continue;
                    if (typeof(IList).IsAssignableFrom(value.GetType()))
                    {
                        var items = (IList)GetValue(memberInfo, obj);
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
                        var items = (IDictionary)GetValue(memberInfo, obj);
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
                    else
                    {
                        ImGui.Text(memberInfo.Name);
                        ImGui.NextColumn();
                        //object value = memberInfo.GetValue(obj);
                        if (value != null)
                            ImGui.Text(value.ToString());
                        else ImGui.Text("null");
                        ImGui.NextColumn();
                    }
                }
            }
        }
        
        static object GetValue(this MemberInfo memberInfo, object forObject)
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
            var componentsByDesign = instancesDB.ComponentsByDesign;
            

            StaticRefLib.Game.GlobalManager.TryGetEntityByGuid(instancesDB.OwningEntity.FactionOwnerID, out var faction);
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
            //var db = entity.GetDataBlob<SensorProfileDB>();
            var componentInstancesDB = db.OwningEntity.GetDataBlob<ComponentInstancesDB>();
            ImGui.Text("Reflected");
            foreach (var kvp in db.ReflectedEMSpectra)
            {
                DisplayValues(kvp.Key, kvp.Value);
                
            }

            ImGui.Text("Emmitted");
            foreach (var kvp in db.EmittedEMSpectra)
            {
                DisplayValues(kvp.Key, kvp.Value);
            }

            ImGui.Text("By Component:");
            var emmitterComponents = componentInstancesDB.ComponentsByAttribute[typeof(SensorSignatureAtbDB)];

            foreach (var component in emmitterComponents)
            {
                ImGui.Text(component.Name);
                ImGui.SameLine();
                ImGui.Text(" ("+ component.Design.TypeName+")");
                SensorSignatureAtbDB emmitterAtbs = (SensorSignatureAtbDB)component.Design.AttributesByType[typeof(SensorSignatureAtbDB)];
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