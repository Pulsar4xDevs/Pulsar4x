using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;

namespace Pulsar4X.Entities
{
    /// <summary>
    /// Theme that is chose by player when setting up Race for a game.
    /// Contains names for:
    /// -Ship Classes
    /// -Star Systems
    /// -Commander Ranks
    /// 
    /// WHen requesting Names, it will provide the next available name in the list. If
    /// it has reached the end of the list, it will loop to the front.
    /// 
    /// If no Names are defined for a category, it will return "Name Unavailable"
    /// 
    /// </summary>
    public class Theme
    {
        protected const string NAME_UNAVAILABLE = "Name Unavailable";
        
        public Guid Id { get; set; }
        public string Name { get; set; }

        [JsonIgnore]
        public bool HasClassNames
        {
            get { return (ClassNames != null && ClassNames.Count > 0); }
        }
        [JsonIgnore]
        public bool HasRankNames
        {
            get { return (RankNames != null && RankNames.Count > 0); }
        }
        [JsonIgnore]
        public bool HasSystemNames
        {
            get { return (SystemNames != null && SystemNames.Count > 0); }
        }
        
        /// <summary>
        /// Use GetNextClassName to retrieve Class names for the user
        /// </summary>
        public Queue<string> ClassNames { get; set; }

        /// <summary>
        /// Use GetNextSystemName to retrieve System names for the user
        /// </summary>
        public Queue<string> SystemNames { get; set; }

        /// <summary>
        /// Use GetRanks to retrieve Ranks for the user
        /// </summary>
        public List<ThemeRank> RankNames { get; set; }
        
        /// <summary>
        /// Returns the next Class name for the Theme
        /// </summary>
        /// <returns></returns>
        public string GetNextClassName()
        {
            if (ClassNames == null || ClassNames.Count == 0)
                return NAME_UNAVAILABLE;

            string nextName;
            do
            {
                nextName = ClassNames.Dequeue();
            } while (string.IsNullOrEmpty(nextName));

            //put the name at the end so we keep looping
            ClassNames.Enqueue(nextName);
            return nextName;
        }

        /// <summary>
        /// Returns the next System Name for the theme
        /// </summary>
        /// <returns></returns>
        public string GetNextSystemName()
        {
            if (SystemNames == null || SystemNames.Count == 0)
                return NAME_UNAVAILABLE;

            string nextName;
            do
            {
                nextName = SystemNames.Dequeue();
            } while (string.IsNullOrEmpty(nextName));

            //put the name at the end so we keep looping
            SystemNames.Enqueue(nextName);
            return nextName;
        }

        public List<ThemeRank> GetRanks(CommanderTypes commanderType)
        {
            return RankNames.Where(r => r.CommanderType == commanderType)
                                 .OrderBy(r => r.Rank)
                                 .ToList();
        }
    }

    public class ThemeRank
    {
        public int Rank { get; set; }
        public string Name { get; set; }
        public CommanderTypes CommanderType { get; set; }
    }
}
