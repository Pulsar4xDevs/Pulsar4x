using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pulsar4X.ECSLib.DataBlobs
{
    public class TechDB : BaseDataBlob
    {
        /// <summary>
        /// list of technolagies that have been fully researched.
        /// techs will be added to this list by the processor once research is compleate.
        /// </summary>
        public List<TechSD> ResearchedTechs{get;set;}

        /// <summary>
        /// dictionary of technowlagies that are availible to research, or are being researched. 
        /// techs will get added to this dict as they become availible by the processor.
        /// </summary>
        public Dictionary<TechSD, int> ResearchableTechs {get;set;}

        /// <summary>
        /// 
        /// </summary>
        public TechDB()
        {
            ResearchedTechs = new List<TechSD>();
            ResearchableTechs = new Dictionary<TechSD, int>();
        }

        public TechDB(TechDB techDB)
        {
            ResearchedTechs = techDB.ResearchedTechs.ToList();
            ResearchableTechs = new Dictionary<TechSD, int>(techDB.ResearchableTechs);
        }
    }
}
