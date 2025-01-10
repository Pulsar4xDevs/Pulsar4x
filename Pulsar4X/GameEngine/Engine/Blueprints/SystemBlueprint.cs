using System.Collections.Generic;

namespace Pulsar4X.Blueprints;

public class SystemBlueprint : Blueprint
{
    public struct SurveyRingValue
    {
        public uint RingRadiusInAU { get; set; }
        public uint Count { get; set; }
    }

    public string Name { get; set; }
    public int? Seed { get; set; }
    public List<string> Stars { get; set; }
    public List<string> Bodies { get; set; }
    public List<SurveyRingValue> SurveyRings { get; set; }
}