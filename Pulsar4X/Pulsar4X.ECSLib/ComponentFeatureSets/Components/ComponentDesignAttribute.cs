using System;
using System.Collections.Generic;
using System.Diagnostics;
using NCalc;
using NCalc.Domain;

namespace Pulsar4X.ECSLib
{
    public interface IComponentDesignAttribute
    {
        //void OnComponentInstantiation(Entity component);
        void OnComponentInstallation(Entity parentEntity, ComponentInstance componentInstance);
        //void OnComponentDeInstalation(Entity ship, Entity component);
    }

    public class ComponentDesignAttribute
    {
        private ComponentTemplateAttributeSD _templateSD;
        public string Name { get { return _templateSD.Name; } }
        public string Description { get { return _templateSD.Description; } }
        public string Unit { get { return _templateSD.Unit; } }
        public GuiHint GuiHint { get { return _templateSD.GuiHint; } }
        
        public Type DataBlobType;

        public Type EnumType;
        public int EnumSelection;
        //public BaseDataBlob DataBlob;
        internal ComponentDesigner ParentComponent; 
        public ComponentDesignAttribute(ComponentDesigner parentComponent, ComponentTemplateAttributeSD templateAtb, FactionTechDB factionTech)
        {
            ParentComponent = parentComponent;
            _templateSD = templateAtb;
            var staticData = StaticRefLib.StaticData;

            if (_templateSD.AbilityFormula != null)
            {
                Formula = new ChainedExpression(_templateSD.AbilityFormula, this, factionTech, staticData);
            }

            if (_templateSD.GuidDictionary != null )
            {
                GuidDictionary = new Dictionary<object, ChainedExpression>();
                if (GuiHint == GuiHint.GuiTechSelectionList)
                {
                    foreach (var kvp in _templateSD.GuidDictionary)
                    {
                        if (factionTech.ResearchedTechs.ContainsKey(Guid.Parse(kvp.Key.ToString())))
                        {
                            TechSD techSD = staticData.Techs[Guid.Parse(kvp.Key.ToString())];
                            GuidDictionary.Add(kvp.Key, new ChainedExpression(ResearchProcessor.DataFormula(factionTech, techSD).ToString(), this, factionTech, staticData));                      
                        }
                    }
                }
                else
                {
                    foreach (var kvp in _templateSD.GuidDictionary)
                    {
                        GuidDictionary.Add(kvp.Key, new ChainedExpression(kvp.Value, this, factionTech, staticData));
                    }
                }
            }
            if (GuiHint == GuiHint.GuiSelectionMaxMin)
            {
                MaxValueFormula = new ChainedExpression(_templateSD.MaxFormula, this, factionTech, staticData);
                MinValueFormula = new ChainedExpression(_templateSD.MinFormula, this, factionTech, staticData);
                StepValueFormula = new ChainedExpression(_templateSD.StepFormula, this, factionTech, staticData);
            }
            if (_templateSD.AbilityDataBlobType != null)
            {
                DataBlobType = Type.GetType(_templateSD.AbilityDataBlobType);        
            }

            if (GuiHint == GuiHint.GuiEnumSelectionList)
            {
                MaxValueFormula = new ChainedExpression(_templateSD.MaxFormula, this, factionTech, staticData);
                MinValueFormula = new ChainedExpression(_templateSD.MinFormula, this, factionTech, staticData);
                StepValueFormula = new ChainedExpression(_templateSD.StepFormula, this, factionTech, staticData);
                SetMax();
                SetMin();
                SetStep();
                EnumType = Type.GetType(_templateSD.EnumTypeName);
                if(EnumType == null)
                    throw new Exception("EnymTypeName not found: " + _templateSD.EnumTypeName);
                EnumSelection = (int)Value;
                //string[] names = Enum.GetNames(EnumType);
            }


        }

        public Dictionary<object, ChainedExpression> GuidDictionary;

        public void SetValueFromGuidList(Guid techguid)
        {
            Formula.ReplaceExpression("TechData('" + techguid + "')");
        }

        internal ChainedExpression Formula { get; set; }
        public void SetValue()
        {
            Formula.Evaluate();
        }

        public void SetValueFromInput(double input)
        {

            Debug.Assert(GuiHint != GuiHint.GuiTextDisplay || GuiHint != GuiHint.None, Name + " is not an editable value");
            SetMin();
            SetMax();
            if (input < MinValue)
                input = MinValue;
            else if (input > MaxValue)
                input = MaxValue;
            Formula.ReplaceExpression(input.ToString()); //prevents it being reset to the default value on Evaluate;
            Formula.Evaluate();//force dependants to recalc.
            ParentComponent.EvalAll();// this recalcs mass etc. which don't seem to be dependants? TODO: mass, volume etc etc should get the dependant handle if needed.
        }

        public double Value { get { return Formula.DResult; } }

        public double MinValue;
        internal ChainedExpression MinValueFormula { get; set; }
        public void SetMin()
        {
            MinValueFormula.Evaluate();
            MinValue = MinValueFormula.DResult;
        }
        public double MaxValue;
        internal ChainedExpression MaxValueFormula { get; set; }
        public void SetMax()
        {
            MaxValueFormula.Evaluate();
            MaxValue = MaxValueFormula.DResult;
        }

        public double StepValue;
        internal ChainedExpression StepValueFormula { get; set; }
        public void SetStep()
        {
            StepValueFormula.Evaluate();
            StepValue = StepValueFormula.DResult;
        }

        internal object[] DataBlobArgs { get; set; }
    }

    public class ChainedExpression
    {
        private StaticDataStore _staticDataStore;
        private FactionTechDB _factionTechDB;
        private ComponentDesigner _designer;
        private ComponentDesignAttribute _designAttribute;
        private Expression _expression;

        // ReSharper disable once NotAccessedField.Local (Used for debuging puroposes. though maybe it could be public and shown in the UI?)
        internal string RawExpressionString;

        //this bool is used for tempory created ChainedExpressions that will not have dependants or be dependant. if these are alowed to be dependants they tend to change a dependee's dependant list while itterating.
        private bool _isDependant = true;
        internal List<ChainedExpression> DependantExpressions = new List<ChainedExpression>();

        /// <summary>
        /// returns Result as an object. consider using IntResult or DResult
        /// </summary>
        internal object Result { get; private set; }

        /// <summary>
        /// This should probilby be avoided, but can be usefull for another formula reading this one, doing another calc, then setting this result again.
        /// Note that doing so will not recalc other dependants. 
        /// if I can avoid using this I will remove it. 
        /// </summary>
        internal object SetResult { set { Result = value; } }

        /// <summary>
        /// Returns Result as an Int. note that if the result was a double you will loose the fraction (ie 1.8 will be 1)
        /// Getting this will fire the Evaluate if Result is null (but won't know if it's old)
        /// </summary>
        internal int IntResult
        {
            get
            {
                switch (Result)
                {
                    case null:
                        Evaluate();
                        if(Result is null)
                            throw new Exception("Result type is unexpectedly null");
                        else
                            return IntResult;
                    case double val:
                        return (int)val; 
                    case float val:
                        return (int)val;
                    case int val:
                        return val;
                    default:
                        throw new Exception("Unexpected Result data Type " + Result.GetType() + " is not double or int");
                    
                }
            }
        }
        /// <summary>
        /// Returns Result as a double
        /// Getting this will fire the Evaluate if Result is null (but won't know if its old)
        /// </summary>
        internal double DResult
        {
            get
            {
                switch (Result)
                {
                    case null:
                        Evaluate();
                        if(Result is null)
                            throw new Exception("Result type is unexpectedly null");
                        else
                            return DResult;
                    case double val:
                        return val; 
                    case float val:
                        return val;
                    case int val:
                        return val;
                    default:
                        throw new Exception("Unexpected Result data Type " + Result.GetType() + " is not double or int");
                    
                }
            }
        }


        /// <summary>
        /// Evaluates the expression and updates the Result.
        /// will also cause any other dependant ChainedExpressions to evaluate. 
        /// </summary>
        internal void Evaluate()
        {
            Result = _expression.Evaluate();
            foreach (var dependant in DependantExpressions)
            {
                dependant.Evaluate();
            }

        }

        internal bool HasErrors()
        {
            return _expression.HasErrors();
        }

        internal string Error()
        {
            return _expression.Error;
        }

        /// <summary>
        /// Primary Constructor for ComponentDesignDB
        /// </summary>
        /// <param name="expressionString"></param>
        /// <param name="designer"></param>
        /// <param name="factionTech"></param>
        /// <param name="staticDataStore"></param>
        internal ChainedExpression(string expressionString, ComponentDesigner designer, FactionTechDB factionTech, StaticDataStore staticDataStore)
        {
            _staticDataStore = staticDataStore;
            _factionTechDB = factionTech;
            _designer = designer;
            ReplaceExpression(expressionString);
        }

        internal ChainedExpression(string expressionString, ComponentDesigner designer, FactionTechDB factionTech)
        {
            _staticDataStore = StaticRefLib.StaticData;
            _factionTechDB = factionTech;
            _designer = designer;
            ReplaceExpression(expressionString);
        }

        /// <summary>
        /// Primary Constructor for ComponentDesignAbilityDB
        /// </summary>
        /// <param name="expressionString"></param>
        /// <param name="designAbility"></param>
        /// <param name="factionTech"></param>
        /// <param name="staticDataStore"></param>
        internal ChainedExpression(string expressionString, ComponentDesignAttribute designAbility, FactionTechDB factionTech, StaticDataStore staticDataStore)
        {
            _staticDataStore = staticDataStore;
            _factionTechDB = factionTech;
            _designer = designAbility.ParentComponent;
            _designAttribute = designAbility;
            ReplaceExpression(expressionString);
        }

        /// <summary>
        /// a private constructor that is used internaly for a one use Expression 
        /// </summary>
        /// <param name="expression"></param>
        /// <param name="designer"></param>
        /// <param name="factionTech"></param>
        /// <param name="staticDataStore"></param>
        private ChainedExpression(Expression expression, ComponentDesigner designer, FactionTechDB factionTech, StaticDataStore staticDataStore)
        {
            _staticDataStore = staticDataStore;
            _factionTechDB = factionTech;
            _designer = designer;
            _expression = expression;
            SetupExpression();

        }

        /// <summary>
        /// a private constructor that is used internaly for a one use Expression 
        /// </summary>
        /// <param name="expression"></param>
        /// <param name="designAbility"></param>
        /// <param name="factionTech"></param>
        /// <param name="staticDataStore"></param>
        private ChainedExpression(Expression expression, ComponentDesignAttribute designAbility, FactionTechDB factionTech, StaticDataStore staticDataStore)
        {
            _staticDataStore = staticDataStore;
            _factionTechDB = factionTech;
            _designer = designAbility.ParentComponent;
            _designAttribute = designAbility;
            _expression = expression;
            SetupExpression();

        }

        /// <summary>
        /// replaces the expression with a new expression, without having to recreate the whole chainedExpression with deisgn, tech, staticdata.
        /// </summary>
        /// <param name="expressionString"></param>
        internal void ReplaceExpression(string expressionString)
        {
            RawExpressionString = expressionString;
            _expression = new Expression(expressionString);
            SetupExpression();
        }

        /// <summary>
        /// it's better to use the string version of this, as that will store the origional string.
        /// </summary>
        /// <param name="expression"></param>
        internal void ReplaceExpression(Expression expression)
        {

            _expression = expression;
            SetupExpression();
            RawExpressionString = _expression.ParsedExpression.ToString();
        }

        /// <summary>
        /// adds this to the given ChainedExpressions dependants list.
        /// </summary>
        /// <param name="dependee"></param>
        private void MakeThisDependant(ChainedExpression dependee)
        {
            if (!dependee.DependantExpressions.Contains(this) && _isDependant)
                dependee.DependantExpressions.Add(this);
        }

        /// <summary>
        /// adds teh given exression to this expressions dependant list
        /// </summary>
        /// <param name="dependant"></param>
        public void AddDependee(ChainedExpression dependant)
        {
            if (!DependantExpressions.Contains(dependant) && dependant._isDependant)
                DependantExpressions.Add(dependant);
        }


        /// <summary>
        /// sets the function and parameter stuff.
        /// </summary>
        private void SetupExpression()
        {
            _expression.EvaluateFunction += NCalcPulsarFunctions;
            _expression.EvaluateParameter += NCalcPulsarParameters;

            _expression.Parameters["xMassx"] = new Expression("Mass"); //unknown string will force it to look in the NCalcPulsarParameters or something
            //see http://ncalc.codeplex.com/wikipage?title=parameters&referringTitle=Home (Dynamic Parameters)
            _expression.Parameters["xVolumex"] = new Expression("Volume");
            _expression.Parameters["xCrewx"] = new Expression("Crew");
            _expression.Parameters["xHTKx"] = new Expression("HTK");
            _expression.Parameters["xResearchCostx"] = new Expression("ResearchCost");
            _expression.Parameters["xMineralCosts"] = new Expression("MineralCosts");
            _expression.Parameters["xCreditCosts"] = new Expression("CreditCosts");
            _expression.Parameters["xGuidDictx"] = new Expression("GuidDict");
            //put extra parameters that don't require extra processing here.ie:
            //_expression.Parameters["X"] = 5;
        }


        /// <summary>
        /// extra parameters that requre additional stuff done go here
        /// </summary>
        /// <param name="name"></param>
        /// <param name="args"></param>
        private void NCalcPulsarParameters(string name, ParameterArgs args)
        {

            switch (name)
            {
                case "Mass":
                    MakeThisDependant(_designer.MassFormula);
                    args.Result = _designer.MassValue;
                    break;
                
                case "Volume":
                
                    MakeThisDependant(_designer.VolumeFormula);
                    args.Result = _designer.VolumeFormula;
                    break;
                
                case "Crew":
                    MakeThisDependant(_designer.CrewFormula);
                    args.Result = _designer.CrewReqValue;
                    break;
                
                case "HTK":
                    MakeThisDependant(_designer.HTKFormula);
                    args.Result = _designer.HTKValue;
                    break;

                case "ResearchCost":
                    MakeThisDependant(_designer.ResearchCostFormula);
                    args.Result = _designer.ResearchCostValue;
                    break;

                case "ResourceCosts":
                    foreach (var formula in _designer.ResourceCostFormulas.Values)
                    {
                        MakeThisDependant(formula);
                    }

                    args.Result = _designer.ResourceCostValues;
                    break;
                
                case "MineralCosts":
                    foreach (var formula in _designer.ResourceCostFormulas.Values)
                    {
                        MakeThisDependant(formula);
                    }

                    args.Result = _designer.ResourceCostValues;
                    break;

                case "CreditCost":
                    MakeThisDependant(_designer.CreditCostFormula);
                    args.Result = _designer.ResearchCostValue;
                    break;

                case "GuidDict":
                    Dictionary<Guid, double> dict = new Dictionary<Guid, double>();
                    foreach (var kvp in _designAttribute.GuidDictionary)
                    {
                        //MakeThisDependant(kvp.Value);
                        dict.Add((Guid.Parse(kvp.Key.ToString())), kvp.Value.DResult);
                    }

                    args.Result = dict;
                    break;
            }
        }


        /// <summary>
        /// extra custom functinos go in here.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="args"></param>
        private void NCalcPulsarFunctions(string name, FunctionArgs args)
        {
            string key = "Unknown Key";
            int index = -1;
            Guid techGuid;
            Guid typeGuid;
            
            switch (name)
            {
                case "Ability":
                    try
                    {
                        //TODO: get rid of this once json data is rewritten to use names instead of indexes
                        if (args.Parameters[0].Evaluate() is int)
                        {
                            index = (int)args.Parameters[0].Evaluate();
                            ChainedExpression result = _designer.ComponentDesignAttributeList[index].Formula;
                            if (result.Result == null)
                                result.Evaluate();
                            MakeThisDependant(result);
                            args.Result = result.Result;
                        }
                        else
                        {
                            key = (string)args.Parameters[0].Evaluate();

                            ChainedExpression result = _designer.ComponentDesignAttributes[key].Formula;
                            if (result.Result == null)
                                result.Evaluate();
                            MakeThisDependant(result);
                            args.Result = result.Result;
                        }

                    }
                    //TODO: maybe log this catch and throw the component out. (instead of throwing)
                    catch (KeyNotFoundException e)
                    {
                        throw new Exception("Cannot find an ability named " + key + ". " + e);
                    }

                    //TODO: the two catches below will be unnesiary once ComponentDesignAttributeList is gone.
                    catch (InvalidCastException e)
                    {
                        throw new Exception("Parameter must be an intiger. " + e);
                    }
                    catch (IndexOutOfRangeException e)
                    {
                        throw new Exception("This component does not have an ComponentAbilitySD at index " + index + ". " + e);
                    }
                    break;

                case "SetAbilityValue": //I might remove this..
                    try
                    {

                        //TODO: get rid of this once json data is rewritten to use names instead of indexes
                        if (args.Parameters[0].Evaluate() is int)
                        {
                            index = (int)args.Parameters[0].Evaluate();
                            ChainedExpression expression = _designer.ComponentDesignAttributeList[index].Formula;
                            expression.SetResult = args.Parameters[1].Evaluate();
                        }
                        else
                        {
                            key = (string)args.Parameters[0].Evaluate();

                            ChainedExpression expression = _designer.ComponentDesignAttributes[key].Formula;
                            expression.SetResult = args.Parameters[1].Evaluate();
                        }

                    }
                    //TODO: maybe log this catch and throw the component out. (instead of throwing)
                    catch (KeyNotFoundException e)
                    {
                        throw new Exception("Cannot find an ability named " + key + ". " + e);
                    }

                    //TODO: the two catches below will be unnesiary once ComponentDesignAttributeList is gone.
                    catch (InvalidCastException e)
                    {
                        throw new Exception("Parameter must be an intiger. " + e);
                    }
                    catch (IndexOutOfRangeException e)
                    {
                        throw new Exception("This component does not have an ComponentAbilitySD at index " + index + ". " + e);
                    }

                    break;

                case "EnumDict":
                    string typeAsString = (string)args.Parameters[0].Evaluate();
                    Type type = Type.GetType(typeAsString);
                    if (type == null)
                    {
                        throw new Exception("Type not found: " + typeAsString + " Check spelling and namespaces");
                    }

                    Type dictType = typeof(Dictionary<,>).MakeGenericType(type, typeof(double));
                    dynamic dict = Activator.CreateInstance(dictType);

                    Type enumDictType = typeof(Dictionary<,>).MakeGenericType(typeof(string), type);
                    dynamic enumConstants = Activator.CreateInstance(enumDictType);
                    foreach (dynamic value in Enum.GetValues(type))
                    {
                        enumConstants.Add(Enum.GetName(type, value), value);
                    }

                    foreach (var kvp in _designAttribute.GuidDictionary)
                    {
                        dynamic keyd = enumConstants[(string)kvp.Key];
                        dict.Add(keyd, kvp.Value.DResult);
                    }

                    args.Result = dict;
                    break;

                case "TechData":
                    techGuid = Guid.Parse((string)args.EvaluateParameters()[0]);
                    TechSD techSD = _staticDataStore.Techs[techGuid];
                    args.Result = ResearchProcessor.DataFormula(_factionTechDB, techSD);
                    break;

                //Returns the tech level for the given guid
                case "TechLevel":
                    techGuid = Guid.Parse((string)args.EvaluateParameters()[0]);
                    if (_factionTechDB.ResearchedTechs.ContainsKey(techGuid))
                        args.Result = _factionTechDB.ResearchedTechs[techGuid];
                    else args.Result = 0;
                    break;
                //currently not used, but an future experiment to pass the CargoTypeSD as a parameter
                case "CargoType":
                    typeGuid = Guid.Parse((string)args.EvaluateParameters()[0]);
                    CargoTypeSD typeSD = _staticDataStore.CargoTypes[typeGuid];
                    args.Result = typeSD;
                    break;
                //used for datablob args for when a guid is required as a parameter
                case "GuidString":
                    typeGuid = Guid.Parse((string)args.EvaluateParameters()[0]);
                    args.Result = typeGuid;
                    break;

                //This sets the DatablobArgs. it's up to the user to ensure the right number of args for a specific datablob
                //The datablob will be the one defined in designAbility.DataBlobType
                //TODO document blobs and what args they take!!
                case "DataBlobArgs":
                    if (_designAttribute.DataBlobType == null)
                        throw new Exception("This Ability does not have a DataBlob defined! define a datablob for this ability!");
                    //_designAbility.DataBlobArgs = new List<double>();
                    List<object> argList = new List<object>();
                    foreach (var argParam in args.Parameters)
                    {
                        ChainedExpression argExpression = new ChainedExpression(argParam, _designAttribute, _factionTechDB, _staticDataStore);
                        _isDependant = false;
                        argExpression.Evaluate();
                        argList.Add(argExpression.Result);
                    }

                    _designAttribute.DataBlobArgs = argList.ToArray();
                    args.Result = argList;
                    break;
            }
        }
    }
}