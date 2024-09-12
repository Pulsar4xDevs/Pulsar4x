using Newtonsoft.Json;
using Pulsar4X.Components;
using Pulsar4X.Datablobs;
using Pulsar4X.Engine;
using Pulsar4X.Interfaces;

namespace Pulsar4X.Atb;

public class GravSurveyAtb : IComponentDesignAttribute
{
    [JsonProperty]
    public uint Speed { get; set; } = 1;

    public GravSurveyAtb(int speed)
    {
        Speed = (uint)speed;
    }

    public void OnComponentInstallation(Entity parentEntity, ComponentInstance componentInstance)
    {
        if(parentEntity.TryGetDatablob<JPSurveyAbilityDB>(out var gravSurveyAbilityDB))
        {
            gravSurveyAbilityDB.Speed += Speed;
        }
        else
        {
            parentEntity.SetDataBlob<JPSurveyAbilityDB>(new JPSurveyAbilityDB() { Speed = Speed });
        }
    }

    public void OnComponentUninstallation(Entity parentEntity, ComponentInstance componentInstance)
    {
        if(parentEntity.TryGetDatablob<JPSurveyAbilityDB>(out var gravSurveyAbilityDB))
        {
            if(Speed >= gravSurveyAbilityDB.Speed)
            {
                parentEntity.RemoveDataBlob<JPSurveyAbilityDB>();
            }
            else
            {
                gravSurveyAbilityDB.Speed -= Speed;
            }
        }
    }

    public string AtbName()
    {
        return "Gravitational Surveyor";
    }

    public string AtbDescription()
    {
        return $"Speed {Speed}";
    }
}