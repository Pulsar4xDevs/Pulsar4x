using System;
using System.Collections.Generic;
using NCalc;

namespace Pulsar4X.ECSLib
{
    /// <summary>
    /// This is not really a procesor, consider moving this into Helpers.
    /// </summary>
    public class ChainedExpression
    {
        private StaticDataStore _staticDataStore;
        private FactionTechDB _factionTechDB;
        private ComponentDesignDB _design;
        private ComponentDesignAbilityDB _designAbility;
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
        /// <param name="design"></param>
        /// <param name="factionTech"></param>
        /// <param name="staticDataStore"></param>
        internal ChainedExpression(string expressionString, ComponentDesignDB design, FactionTechDB factionTech, StaticDataStore staticDataStore)
        {
            _staticDataStore = staticDataStore;
            _factionTechDB = factionTech;
            _design = design;
            ReplaceExpression(expressionString);
        }


        /// <summary>
        /// Primary Constructor for ComponentDesignAbilityDB
        /// </summary>
        /// <param name="expressionString"></param>
        /// <param name="designAbility"></param>
        /// <param name="factionTech"></param>
        /// <param name="staticDataStore"></param>
        internal ChainedExpression(string expressionString, ComponentDesignAbilityDB designAbility, FactionTechDB factionTech, StaticDataStore staticDataStore)
        {
            _staticDataStore = staticDataStore;
            _factionTechDB = factionTech;
            _design = designAbility.ParentComponent;
            _designAbility = designAbility;
            ReplaceExpression(expressionString);
        }

        /// <summary>
        /// a private constructor that is used internaly for a one use Expression 
        /// </summary>
        /// <param name="expression"></param>
        /// <param name="design"></param>
        /// <param name="factionTech"></param>
        /// <param name="staticDataStore"></param>
        private ChainedExpression(Expression expression, ComponentDesignDB design, FactionTechDB factionTech, StaticDataStore staticDataStore)
        {
            _staticDataStore = staticDataStore;
            _factionTechDB = factionTech;
            _design = design;
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
        private ChainedExpression(Expression expression, ComponentDesignAbilityDB designAbility, FactionTechDB factionTech, StaticDataStore staticDataStore)
        {
            _staticDataStore = staticDataStore;
            _factionTechDB = factionTech;
            _design = designAbility.ParentComponent;
            _designAbility = designAbility;
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

            _expression.Parameters["xSizex"] = new Expression("Size"); //unknown string will force it to look in the NCalcPulsarParameters or something
            //see http://ncalc.codeplex.com/wikipage?title=parameters&referringTitle=Home (Dynamic Parameters)
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
            if (name == "Size")
            {
                MakeThisDependant(_design.SizeFormula);
                args.Result = _design.SizeValue;
            }
            if (name == "Crew")
            {
                MakeThisDependant(_design.CrewFormula);
                args.Result = _design.CrewReqValue;
            }
            if (name == "HTK")
            {
                MakeThisDependant(_design.HTKFormula);
                args.Result = _design.HTKValue;
            }
            if (name == "ResearchCost")
            {
                MakeThisDependant(_design.ResearchCostFormula);
                args.Result = _design.ResearchCostValue;
            }
            if (name == "MineralCosts")
            {
                foreach (var formula in _design.MineralCostFormulas.Values)
                {
                    MakeThisDependant(formula);
                }
                args.Result = _design.MineralCostValues;
            }
            if (name == "CreditCost")
            {
                MakeThisDependant(_design.CreditCostFormula);
                args.Result = _design.ResearchCostValue;
            }
            if (name == "GuidDict")
            {
                Dictionary<Guid, double> dict = new Dictionary<Guid, double>();
                foreach (var kvp in _designAbility.GuidDictionary)
                {
                    //MakeThisDependant(kvp.Value);
                    dict.Add(kvp.Key,kvp.Value.DResult);     
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
                    
                    ChainedExpression result = _design.ComponentDesignAbilities[index].Formula;
                    if(result.Result == null)
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

                    ChainedExpression expression = _design.ComponentDesignAbilities[index].Formula;
                    expression.SetResult = args.Parameters[1].Evaluate();

                }
                catch (InvalidCastException e) { throw new Exception("Parameter must be an intiger. " + e); }
                catch (IndexOutOfRangeException e) { throw new Exception("This component does not have an ComponentAbilitySD at index " + index + ". " + e); }
            }

            if (name == "TechData")
            {

                Guid techGuid = Guid.Parse((string)args.EvaluateParameters()[0]);
                TechSD techSD = _staticDataStore.Techs[techGuid];
                args.Result = TechProcessor.DataFormula(_factionTechDB, techSD);
            }
            //This sets the DatablobArgs. it's up to the user to ensure the right number of args for a specific datablob
            //The datablob will be the one defined in designAbility.DataBlobType
            //TODO document blobs and what args they take!!
            if (name == "DataBlobArgs")
            {
                if(_designAbility.DataBlobType == null)
                    throw new Exception("This Ability does not have a DataBlob defined! define a datablob for this ability!");
                //_designAbility.DataBlobArgs = new List<double>();
                List<object> argList = new List<object>();
                foreach (var argParam in args.Parameters)
                {
                    ChainedExpression argExpression = new ChainedExpression(argParam, _designAbility, _factionTechDB, _staticDataStore);
                    _isDependant = false;
                    argExpression.Evaluate();
                    argList.Add(argExpression.Result);
                }
                _designAbility.DataBlobArgs = argList.ToArray();
                args.Result = argList;
            }


        }
    }
}