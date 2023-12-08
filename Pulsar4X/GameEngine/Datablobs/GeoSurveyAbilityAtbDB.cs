using Newtonsoft.Json;
using Pulsar4X.Components;
using Pulsar4X.Engine;
using Pulsar4X.Interfaces;

namespace Pulsar4X.Datablobs;

public class GeoSurveyAbilityAtbDB : BaseDataBlob, IComponentDesignAttribute
{
    [JsonProperty]
    public int Speed { get; set; } = 1;

    public GeoSurveyAbilityAtbDB(int speed)
    {
        Speed = speed;
    }

    public void OnComponentInstallation(Entity parentEntity, ComponentInstance componentInstance)
    {
    }

    public void OnComponentUninstallation(Entity parentEntity, ComponentInstance componentInstance)
    {
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