using Newtonsoft.Json;
using Pulsar4X.Components;
using Pulsar4X.Datablobs;
using Pulsar4X.Engine;
using Pulsar4X.Interfaces;

namespace Pulsar4X.Atb;

public class GeoSurveyAtb : IComponentDesignAttribute
{
    [JsonProperty]
    public uint Speed { get; set; } = 1;

    public GeoSurveyAtb(int speed)
    {
        Speed = (uint)speed;
    }

    public void OnComponentInstallation(Entity parentEntity, ComponentInstance componentInstance)
    {
        if(parentEntity.TryGetDatablob<GeoSurveyAbilityDB>(out var geoSurveyAbilityDB))
        {
            geoSurveyAbilityDB.Speed += Speed;
        }
        else
        {
            parentEntity.SetDataBlob<GeoSurveyAbilityDB>(new GeoSurveyAbilityDB() { Speed = Speed });
        }
    }

    public void OnComponentUninstallation(Entity parentEntity, ComponentInstance componentInstance)
    {
        if(parentEntity.TryGetDatablob<GeoSurveyAbilityDB>(out var geoSurveyAbilityDB))
        {
            geoSurveyAbilityDB.Speed -= Speed;
            if(geoSurveyAbilityDB.Speed <= 0)
            {
                parentEntity.RemoveDataBlob<GeoSurveyAbilityDB>();
            }
        }
    }

    public string AtbName()
    {
        return "Geological Surveyor";
    }

    public string AtbDescription()
    {
        return $"Speed {Speed}";
    }
}