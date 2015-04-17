using System;
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

        public List<CommanderNameThemeSD> CommanderNameThemes = new List<CommanderNameThemeSD>();
        public Type CommanderNameThemesType;

        public List<MineralSD> Minerals = new List<MineralSD>();
        public Type MineralsType;

        /// @todo add a whole bunch more static data.

        public StaticDataStore()
        {
            AtmosphericGasesType = AtmosphericGases.GetType();
            CommanderNameThemesType = CommanderNameThemes.GetType();
            MineralsType = Minerals.GetType();
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
                Minerals.AddRange(minerals);
        }
    }
}