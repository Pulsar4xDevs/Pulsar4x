using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using NCalc;
using Pulsar4X.Blueprints;
using Pulsar4X.Datablobs;
using Pulsar4X.Engine;

namespace Pulsar4X.Components
{
    public class ChainedExpression
    {
        private FactionDataStore _factionDataStore;
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
                    case long val:
                        return Convert.ToInt32(val);
                    case int val:
                        return val;
                    default:
                        throw new Exception("Unexpected Result data Type " + Result.GetType() + " is not double or int");

                }
            }
        }

        /// <summary>
        /// Returns Result as a Long. note that if the result was a double you will loose the fraction (ie 1.8 will be 1)
        /// Getting this will fire the Evaluate if Result is null (but won't know if it's old)
        /// </summary>
        internal long LongResult
        {
            get
            {
                switch (Result)
                {
                    case null:
                        Evaluate();
                        if (Result is null)
                            throw new Exception("Result type is unexpectedly null");
                        else
                            return LongResult;
                    case double val:
                        return (long)val;
                    case float val:
                        return (long)val;
                    case int val:
                        return Convert.ToInt64(val);
                    case long val:
                        return val;
                    default:
                        throw new Exception("Unexpected Result data Type " + Result.GetType() + " is not double or long");

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


        internal bool BoolResult
        {
            get{
                switch (Result)
                {
                 case null:
                     Evaluate();
                     if(Result is null)
                        throw new Exception("Result type is unexpectedly null");
                     else
                         return BoolResult;
                 case bool val:
                     return val;
                 default:
                     throw new Exception("Unexpected Result data Type " + Result.GetType() + " is not a boolian value");

                }
            }
        }

        internal string StrResult
        {
            get{
                switch (Result)
                {
                    case null:
                        Evaluate();
                        if(Result is null)
                            throw new Exception("Result type is unexpectedly null");
                        else
                            return StrResult;
                    case string val:
                        return val;
                    default:
                        try
                        {
                            return (string)Result;
                        }
                        catch
                        {
                            throw new Exception("Unexpected Result data Type " + Result.GetType() + " could not be cast to a string");
                        }

                }
            }
        }

        internal Guid GuidResult
        {
            get
            {
                if(Result is null) Evaluate();

                if(Result is null)
                {
                    throw new Exception("Result is null");
                }
                else if(Result is Guid)
                {
                    return (Guid)Result;
                }
                else if(Result is string)
                {
                    return Guid.Parse(Result.ToString());
                }
                else
                {
                    throw new Exception("Unexpected Result data Type " + Result.GetType() + " could not be cast to a string");
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

        internal ChainedExpression(string expressionString, ComponentDesigner designer, FactionDataStore factionTech, FactionTechDB factionTechDB)
        {
            _factionDataStore = factionTech;
            _factionTechDB = factionTechDB;
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
        internal ChainedExpression(string expressionString, ComponentDesignAttribute designAbility, FactionDataStore factionTech, FactionTechDB factionTechDB)
        {
            _factionDataStore = factionTech;
            _factionTechDB = factionTechDB;
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
        private ChainedExpression(Expression expression, ComponentDesigner designer, FactionDataStore factionTech, FactionTechDB factionTechDB)
        {
            _factionDataStore = factionTech;
            _factionTechDB = factionTechDB;
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
        private ChainedExpression(Expression expression, ComponentDesignAttribute designAbility, FactionDataStore factionTech, FactionTechDB factionTechDB)
        {
            _factionDataStore = factionTech;
            _factionTechDB = factionTechDB;
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
        /// it's better to use the string version of this, as that will store the original string.
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
        /// https://github.com/ncalc/ncalc/wiki/Parameters
        /// </summary>
        private void SetupExpression()
        {
            _expression.EvaluateFunction += NCalcPulsarFunctions;
            _expression.EvaluateParameter += NCalcPulsarParameters;

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
                case "Pi":
                    args.Result = Math.PI;
                    break;

                case "Mass":
                    MakeThisDependant(_designer.MassFormula); //we do this so that when the mass value changes, whatever formula is referencing mass gets updated also.
                    args.Result = (double)_designer.MassValue; //this is the resulting value from the mass value.
                    break;

                case "Volume_km3":

                    MakeThisDependant(_designer.VolumeFormula);
                    args.Result = _designer.VolumeM3Value;
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
                    var dict = new Dictionary<string, double>();
                    foreach (var kvp in _designAttribute.GuidDictionary)
                    {
                        dict.Add(kvp.Key.ToString(), kvp.Value.DResult);
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
            string techGuid;
            string typeGuid;

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

                        throw new Exception("Cannot find an ability named " + key + ", in " + _designer.Name + " " + e);
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
                    techGuid = (string)args.EvaluateParameters()[0];
                    Tech techSD = _factionDataStore.Techs[techGuid];
                    args.Result = _factionTechDB.TechDataFormula(techSD);
                    break;

                //Returns the tech level for the given guid
                case "TechLevel":
                    techGuid = (string)args.EvaluateParameters()[0];
                    if (_factionDataStore.Techs.ContainsKey(techGuid))
                        args.Result = _factionDataStore.Techs[techGuid].Level;
                    else args.Result = 0;
                    break;
                //currently not used, but an future experiment to pass the CargoTypeSD as a parameter
                case "CargoType":
                    typeGuid = (string)args.EvaluateParameters()[0];
                    CargoTypeBlueprint typeSD = _factionDataStore.CargoTypes[typeGuid];
                    args.Result = typeSD;
                    break;
                //used for datablob args for when a guid is required as a parameter
                case "GuidString":
                    typeGuid = (string)args.EvaluateParameters()[0];
                    args.Result = typeGuid;
                    break;

                //This sets the DatablobArgs. it's up to the user to ensure the right number of args for a specific datablob
                //The datablob will be the one defined in designAbility.AttributeType
                //TODO document blobs and what args they take!!
                case "AtbConstrArgs":
                    if (_designAttribute.AttributeType == null)
                        throw new Exception( _designAttribute.Name +" does not have a type defined! define an AttributeType for this Attribute!");
                    //_designAbility.AtbConstrArgs = new List<double>();
                    List<object> argList = new List<object>();
                    foreach (var argParam in args.Parameters)
                    {
                        ChainedExpression argExpression = new ChainedExpression(argParam, _designAttribute, _factionDataStore, _factionTechDB);
                        _isDependant = false;
                        argExpression.Evaluate();
                        argList.Add(argExpression.Result);
                    }

                    _designAttribute.AtbConstrArgs = argList.ToArray();
                    args.Result = argList;
                    break;
                case "ExhaustVelocityLookup":
                    var cargo = (ProcessedMaterialBlueprint)_factionDataStore.CargoGoods.GetAny((string)args.EvaluateParameters()[0]);
                    Expression dataExpression = new Expression(cargo.Formulas["ExhaustVelocity"]);
                    args.Result = dataExpression.Evaluate();
                    break;
            }
        }
    }
}