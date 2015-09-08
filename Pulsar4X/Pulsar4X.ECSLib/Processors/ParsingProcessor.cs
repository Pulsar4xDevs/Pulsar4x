using System;
using System.Collections.Generic;
using NCalc;

namespace Pulsar4X.ECSLib
{
    internal class ParsingProcessor
    {
        private StaticDataStore _staticDataStore;
        private TechDB _factionTechDB;
        private ComponentDesignDB _componentDesign;
        
        

        internal ParsingProcessor(StaticDataStore staticDataStore, TechDB factionTech, ComponentDesignDB componentDesign)
        {
            _staticDataStore = staticDataStore;
            _factionTechDB = factionTech;
            _componentDesign = componentDesign;
        }

        internal Expression NewExpression(string expressionString, ComponentDesignDB design)
        {
            Expression expression = new Expression(expressionString);
            expression.Parameters["Size"] = design.SizeValue;
            //todo add more Parameters?

            expression.EvaluateFunction += NCalcFunctions;

            return expression;
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
                    Expression result = _componentDesign.ComponentDesignAbilities[index].Formula;
                    args.Result = result.Evaluate();
                }
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