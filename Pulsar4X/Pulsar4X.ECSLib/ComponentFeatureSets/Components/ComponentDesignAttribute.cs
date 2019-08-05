using System;
using System.Collections.Generic;
using System.Diagnostics;
using NCalc;

namespace Pulsar4X.ECSLib
{
    public interface IComponentDesignAttribute
    {
        //void OnComponentInstantiation(Entity component);
        void OnComponentInstallation(Entity parentEntity, ComponentInstance componentInstance);
        //void OnComponentDeInstalation(Entity ship, Entity component);
    }

    public class ComponentDesignAtbData
    {
        public string Name;
        public string Description;
        public Type DataBlobType;
        internal ComponentDesigner ParentComponent;
        public double Value;
    }

    public class ComponentDesignAttribute
    {
        public string Name;
        public string Description;

        public GuiHint GuiHint;
        public Type DataBlobType;
        //public BaseDataBlob DataBlob;
        internal ComponentDesigner ParentComponent; 
        public ComponentDesignAttribute(ComponentDesigner parentComponent)
        {
            ParentComponent = parentComponent;
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
        private string _stringExpression;

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
                if (Result == null)
                    Evaluate();
                if (Result is int)
                    return (int)Result;
                if (Result is double)
                    return (int)(double)Result;
                else
                {
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
                if (Result == null)
                    Evaluate();
                if (Result is double)
                    return (double)Result;
                if (Result is int)
                    return (int)Result;
                else
                {
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
            _stringExpression = expressionString;
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
            _stringExpression = _expression.ParsedExpression.ToString();
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
            if (name == "Mass")
            {
                MakeThisDependant(_designer.MassFormula);
                args.Result = _designer.MassValue;
            }
            if (name == "Volume")
            {
                MakeThisDependant(_designer.VolumeFormula);
                args.Result = _designer.VolumeFormula;
            }
            if (name == "Crew")
            {
                MakeThisDependant(_designer.CrewFormula);
                args.Result = _designer.CrewReqValue;
            }
            if (name == "HTK")
            {
                MakeThisDependant(_designer.HTKFormula);
                args.Result = _designer.HTKValue;
            }
            if (name == "ResearchCost")
            {
                MakeThisDependant(_designer.ResearchCostFormula);
                args.Result = _designer.ResearchCostValue;
            }
            if (name == "MineralCosts")
            {
                foreach (var formula in _designer.MineralCostFormulas.Values)
                {
                    MakeThisDependant(formula);
                }
                args.Result = _designer.MineralCostValues;
            }
            if (name == "CreditCost")
            {
                MakeThisDependant(_designer.CreditCostFormula);
                args.Result = _designer.ResearchCostValue;
            }
            if (name == "GuidDict")
            {
                Dictionary<Guid, double> dict = new Dictionary<Guid, double>();
                foreach (var kvp in _designAttribute.GuidDictionary)
                {
                    //MakeThisDependant(kvp.Value);
                    dict.Add((Guid.Parse(kvp.Key.ToString())), kvp.Value.DResult);
                }
                args.Result = dict;
            }
        }


        /// <summary>
        /// extra custom functinos go in here.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="args"></param>
        private void NCalcPulsarFunctions(string name, FunctionArgs args)
        {
            if (name == "Ability")
            {
                int index = 0;
                try
                {
                    index = (int)args.Parameters[0].Evaluate();

                    ChainedExpression result = _designer.ComponentDesignAttributes[index].Formula;
                    if (result.Result == null)
                        result.Evaluate();
                    MakeThisDependant(result);
                    args.Result = result.Result;

                }
                catch (InvalidCastException e) { throw new Exception("Parameter must be an intiger. " + e); }
                catch (IndexOutOfRangeException e) { throw new Exception("This component does not have an ComponentAbilitySD at index " + index + ". " + e); }
            }
            if (name == "SetAbilityValue") //I might remove this..
            {
                int index = 0;
                try
                {
                    index = (int)args.Parameters[0].Evaluate();

                    ChainedExpression expression = _designer.ComponentDesignAttributes[index].Formula;
                    expression.SetResult = args.Parameters[1].Evaluate();

                }
                catch (InvalidCastException e) { throw new Exception("Parameter must be an intiger. " + e); }
                catch (IndexOutOfRangeException e) { throw new Exception("This component does not have an ComponentAbilitySD at index " + index + ". " + e); }
            }

            if (name == "EnumDict")
            {
                string typeAsString = (string)args.Parameters[0].Evaluate();
                Type type = Type.GetType(typeAsString);
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
                    dynamic key = enumConstants[(string)kvp.Key];
                    dict.Add(key, kvp.Value.DResult);
                }
                args.Result = dict;
            }

            if (name == "TechData")
            {

                Guid techGuid = Guid.Parse((string)args.EvaluateParameters()[0]);
                TechSD techSD = _staticDataStore.Techs[techGuid];
                args.Result = ResearchProcessor.DataFormula(_factionTechDB, techSD);
            }

            //Returns the tech level for the given guid
            if (name == "TechLevel")
            {

                Guid techGuid = Guid.Parse((string)args.EvaluateParameters()[0]);
                if (_factionTechDB.ResearchedTechs.ContainsKey(techGuid))
                    args.Result = _factionTechDB.ResearchedTechs[techGuid];
                else args.Result = 0;
            }
            //currently not used, but an future experiment to pass the CargoTypeSD as a parameter
            if (name == "CargoType")
            {
                Guid typeGuid = Guid.Parse((string)args.EvaluateParameters()[0]);
                CargoTypeSD typeSD = _staticDataStore.CargoTypes[typeGuid];
                args.Result = typeSD;
            }
            //used for datablob args for when a guid is required as a parameter
            if (name == "GuidString")
            {
                Guid typeGuid = Guid.Parse((string)args.EvaluateParameters()[0]);
                args.Result = typeGuid;
            }

            //This sets the DatablobArgs. it's up to the user to ensure the right number of args for a specific datablob
            //The datablob will be the one defined in designAbility.DataBlobType
            //TODO document blobs and what args they take!!
            if (name == "DataBlobArgs")
            {
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
            }


        }
    }
}