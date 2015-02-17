using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Pulsar4X.UI.Handlers
{
    public class HandlerBase
    {
        public Pulsar4X.Entities.Faction CurrentFaction
        {
            get { return GameState.Instance.FactionCurrent; }
        }


        /// <summary>
        /// Misspelling intentional to keep this in line with systemMap's misspelling.
        /// </summary>
        private Pulsar4X.Entities.TaskGroupTN m_oCurrnetTaskGroup;
        public Pulsar4X.Entities.TaskGroupTN CurrentTaskGroup
        {
            get { return m_oCurrnetTaskGroup; }
            set
            {
                if (value != m_oCurrnetTaskGroup)
                {
                    m_oCurrnetTaskGroup = value;
                    Refresh();
                }
            }
        }

        /// <summary>
        /// Which planetary population is selected.
        /// </summary>
        private Pulsar4X.Entities.Population m_oCurrnetPopulation;
        public Pulsar4X.Entities.Population CurrentPopulation
        {
            get { return m_oCurrnetPopulation; }
            set
            {
                if (value != m_oCurrnetPopulation)
                {
                    m_oCurrnetPopulation = value;
                    Refresh();
                }
            }
        }

        public virtual void Refresh()
        { 
        }
    }
}
