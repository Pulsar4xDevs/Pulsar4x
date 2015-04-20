using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Pulsar4X.ECSLib
{
    public class StaticDataStore
    {
        /// <summary>
        /// List which stores all the atmospheric gases.
        /// </summary>
        public WeightedList<AtmosphericGasSD> AtmosphericGases = new WeightedList<AtmosphericGasSD>();
        public Type AtmosphericGasesType;
        private const string AtmosphericGasesTypeString = "AtmosphericGases";


        public List<CommanderNameThemeSD> CommanderNameThemes = new List<CommanderNameThemeSD>();
        public Type CommanderNameThemesType;
        private const string CommanderNameThemesTypeString = "CommanderNameThemes";


        public List<MineralSD> Minerals = new List<MineralSD>();
        public Type MineralsType;
        private const string MineralsTypeString = "Minerals";


        public JDictionary<Guid, TechSD> Techs = new JDictionary<Guid, TechSD>();
        public Type TechsType;
        private const string TechsTypeString = "Techs";


        /// @todo add a whole bunch more static data.

        public StaticDataStore()
        {
            AtmosphericGasesType = AtmosphericGases.GetType();
            CommanderNameThemesType = CommanderNameThemes.GetType();
            MineralsType = Minerals.GetType();
            TechsType = Techs.GetType();
        }

        public void Store(WeightedList<AtmosphericGasSD> atmosphericGases)
        {
            if (atmosphericGases != null)
                AtmosphericGases.AddRange(atmosphericGases);
        }

        public void Store(List<CommanderNameThemeSD> commanderNameThemes)
        {
            if (commanderNameThemes != null)
                CommanderNameThemes.AddRange(commanderNameThemes);
        }

        public void Store(List<MineralSD> minerals)
        {
            if (minerals != null)
            {
                foreach (var min in minerals)
                {
                    int i = Minerals.FindIndex(x => x.ID == min.ID);
                    if (i > 0) // found existing element!
                        Minerals[i] = min;
                    else
                        Minerals.Add(min);
                }
            }
        }

        public void Store(JDictionary<Guid, TechSD> techs)
        {
            if (techs != null)
            {
                foreach (var tech in techs)
                    Techs[tech.Key] = tech.Value;  // replace existing value or insert a new one as required.
            }
        }



        public string GetTypeString(Type type)
        {
            if (type == AtmosphericGasesType)
            {
                return AtmosphericGasesTypeString;
            }
            else if (type == CommanderNameThemesType)
            {
                return CommanderNameThemesTypeString;
            }
            else if (type == MineralsType)
            {
                return MineralsTypeString;
            }
            else if (type == TechsType)
            {
                return TechsTypeString;
            }

            return null;
        }

        public Type GetType(string typeString)
        {
            switch (typeString)
            {
                case AtmosphericGasesTypeString:
                    return AtmosphericGasesType;
                case CommanderNameThemesTypeString:
                    return CommanderNameThemesType;
                case  MineralsTypeString:
                    return MineralsType;
                case TechsTypeString:
                    return TechsType;
                default:
                    return null;
            }
        }
    }
}