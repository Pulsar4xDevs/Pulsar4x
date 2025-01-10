using Pulsar4X.Datablobs;

namespace Pulsar4X.JumpPoints;

public class JPSurveyDB : BaseDataBlob
{
    // The entity Id this entity is attempting to survey
    public int TargetId { get; set; }
}