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
        private Expression _expression;
        private string _stringExpression; //used for debuging puroposes.
        internal List<ChainedExpression> DependantExpressions = new List<ChainedExpression>();

        /// <summary>
        /// returns Result as an object. consider using IntResult or DResult
        /// </summary>
        internal object Result { get; private set; }

        /// <summary>
        /// Returns Result as an Int. note that if the result was a double you will loose the fraction (ie 1.8 will be 1)
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
                    return (double)(int)Result;
                else
                {
                    throw new Exception("Unexpected Result data Type - not double or int");
                }
            }
        }

        internal ChainedExpression(string expressionString, ComponentDesignDB design, TechDB factionTech, StaticDataStore staticDataStore)
        {
            _staticDataStore = staticDataStore;
            _factionTechDB = factionTech;
            _design = design;
            NewExpression(expressionString);
        }

        internal void NewExpression(string expressionString)
        {
            _stringExpression = expressionString;
            _expression = new Expression(expressionString);
            _expression.EvaluateFunction += NCalcFunctions;
            _expression.Parameters["Size"] = _design.SizeValue;
            //_expression.Parameters["xSizex"] = new Expression("Size"); //this is a bit wacky, I don't fully understand it but "xSizex" has to be something that doesn't exsist or somethign.
            
            _expression.EvaluateParameter += delegate(string name, ParameterArgs args)
            {
                if (name == "Size")
                {
                    MakeThisDependant(_design.SizeFormula);
                    args.Result = _design.SizeValue;
                }
            };

            
        }

        internal void Evaluate()
        {
            Result = _expression.Evaluate();
            foreach (var dependant in DependantExpressions)
            {
                dependant.Evaluate();
            }
            
        }

        private void MakeThisDependant(ChainedExpression dependee)
        {
            if(!dependee.DependantExpressions.Contains(this))
                dependee.DependantExpressions.Add(this);
        }

        public void AddDependee(ChainedExpression dependant)
        {
            if(!DependantExpressions.Contains(dependant))
                DependantExpressions.Add(dependant);
        }

        private void NCalcFunctions(string name, FunctionArgs args)
        {
            if (name == "Ability")
            {
                int index;
                try
                {
                    index = (int)args.Parameters[0].Evaluate();
                }
                catch (Exception e) { throw new Exception("First arg must be in intiger" + e); }
                try
                {
                    ChainedExpression result = _design.ComponentDesignAbilities[index].Formula;
                    result.Evaluate();
                    args.Result = result.Result;

                }//todo catch specific exception
                catch (Exception e) { throw new Exception("This component does not have an ComponentAbilitySD at that index" + e); }
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
        }
    }
}