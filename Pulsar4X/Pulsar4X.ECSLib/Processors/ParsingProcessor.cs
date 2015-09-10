using System;
using System.Collections.Generic;
using NCalc;

namespace Pulsar4X.ECSLib
{
    internal class ChainedExpression
    {
        private StaticDataStore _staticDataStore;
        private TechDB _factionTechDB;
        private ComponentDesignDB _design;
        private ComponentDesignAbilityDB _designAbility;
        private Expression _expression;
        
        // ReSharper disable once NotAccessedField.Local (Used for debuging puroposes. though maybe it could be public and shown in the UI?)
        private string _stringExpression;
        internal List<ChainedExpression> DependantExpressions = new List<ChainedExpression>();

        /// <summary>
        /// returns Result as an object. consider using IntResult or DResult
        /// </summary>
        internal object Result { get; private set; }

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
                    throw new Exception("Unexpected Result data Type - not double or int");  
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
                    throw new Exception("Unexpected Result data Type - not double or int");
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
        internal ChainedExpression(string expressionString, ComponentDesignDB design, TechDB factionTech, StaticDataStore staticDataStore)
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
        internal ChainedExpression(string expressionString, ComponentDesignAbilityDB designAbility, TechDB factionTech, StaticDataStore staticDataStore)
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
        private ChainedExpression(Expression expression, ComponentDesignDB design, TechDB factionTech, StaticDataStore staticDataStore)
        {
            _staticDataStore = staticDataStore;
            _factionTechDB = factionTech;
            _design = design;
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
            if (!dependee.DependantExpressions.Contains(this))
                dependee.DependantExpressions.Add(this);
        }

        /// <summary>
        /// adds teh given exression to this expressions dependant list
        /// </summary>
        /// <param name="dependant"></param>
        public void AddDependee(ChainedExpression dependant)
        {
            if (!DependantExpressions.Contains(dependant))
                DependantExpressions.Add(dependant);
        }


        /// <summary>
        /// sets the function and parameter stuff.
        /// </summary>
        private void SetupExpression()
        {
            _expression.EvaluateFunction += NCalcPulsarFunctions;
            _expression.EvaluateParameter += NCalcPulsarParameters;

            _expression.Parameters["xBaseSizex"] = new Expression("BaseSize"); //unknown string will force it to look in the NCalcPulsarParameters or something
            _expression.Parameters["xFinalSizex"] = new Expression("FinalSize"); //unknown string will force it to look in the NCalcPulsarParameters or something
            //see http://ncalc.codeplex.com/wikipage?title=parameters&referringTitle=Home (Dynamic Parameters)

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
            if (name == "BaseSize")
            {
                MakeThisDependant(_design.SizeBaseFormula);
                args.Result = _design.SizeBaseValue;
            }
            if (name == "FinalSize")
            {
                MakeThisDependant(_design.SizeBaseFormula);
                args.Result = _design.FinalSize;
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
                int index;
                try
                {
                    index = (int)args.Parameters[0].Evaluate();
                }
                catch (InvalidCastException e) { throw new Exception("Parameter must be an intiger. " + e); }
                try
                {
                    
                    ChainedExpression result = _design.ComponentDesignAbilities[index].Formula;
                    if(result.Result == null)
                        result.Evaluate();
                    MakeThisDependant(result); 
                    args.Result = result.Result;

                }//todo catch specific exception
                catch (IndexOutOfRangeException e) { throw new Exception("This component does not have an ComponentAbilitySD at index " + index + ". " + e); }
            }
            if (name == "TechObject")
            {
                Guid techGuid = new Guid(args.Parameters[0].ToString());
                TechSD techSD = _staticDataStore.Techs[techGuid];
                args.Result = techSD;
            }
            if (name == "TechData")
            {

                Guid techGuid = Guid.Parse((string)args.EvaluateParameters()[0]);
                TechSD techSD = _staticDataStore.Techs[techGuid];
                args.Result = TechProcessor.DataFormula(_factionTechDB, techSD);
            }
            if (name == "TechList")
            {
                List<TechSD> list = new List<TechSD>();
                foreach (Expression item in args.Parameters)
                {
                    list.Add((TechSD)item.Evaluate());
                }
                args.Result = list;
            }
            if (name == "Size")
            {
                ChainedExpression finalSize = new ChainedExpression(args.Parameters[0], _design, _factionTechDB, _staticDataStore);
                _design.FinalSizeFormula = finalSize;
                args.Result = _design.FinalSize;
            }
            if (name == "DataBlobArgs")
            {
                _designAbility.DataBlobArgs = new List<double>();
                foreach (var argParam in args.Parameters)
                {
                    ChainedExpression argExpression = new ChainedExpression(argParam, _design, _factionTechDB, _staticDataStore);
                    _designAbility.DataBlobArgs.Add(argExpression.DResult);
                }
            }
        }
    }
}