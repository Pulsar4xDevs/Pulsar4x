using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pulsar4X.ECSLib
{
    public class TechDB : BaseDataBlob
    {
        /// <summary>
        /// list of technolagies that have been fully researched.
        /// techs will be added to this list by the processor once research is compleate.
        /// </summary>
        public List<Guid> ResearchedTechs{get;set;}

        /// <summary>
        /// dictionary of technowlagies that are availible to research, or are being researched. 
        /// techs will get added to this dict as they become availible by the processor.
        /// </summary>
        public Dictionary<TechSD, int> ResearchableTechs {get;set;}

        /// <summary>
        /// a list of techs not yet meeting the reqirements to research
        /// </summary>
        public List<TechSD> UnavailableTechs { get; set; } 

        /// <summary>
        /// Constructor for datablob, this should only be used when a new faction is created.
        /// </summary>
        /// <param name="alltechs">a list of all possible techs in game</param>
        public TechDB(List<TechSD> alltechs)
        {
            UnavailableTechs = alltechs.ToList();
            ResearchedTechs = new List<Guid>();
            ResearchableTechs = new Dictionary<TechSD, int>();
        }

        public TechDB(TechDB techDB)
        {
            UnavailableTechs = techDB.UnavailableTechs.ToList();
            ResearchedTechs = techDB.ResearchedTechs.ToList();
            ResearchableTechs = new Dictionary<TechSD, int>(techDB.ResearchableTechs);
        }

        public TechDB()
        {
            UnavailableTechs = new List<TechSD>();
            ResearchedTechs = new List<Guid>();
            ResearchableTechs = new Dictionary<TechSD, int>();
        }

        public override object Clone()
        {
            return new TechDB(this);
        }
    }
}
