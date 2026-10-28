using Pulsar4X.Components;
using Pulsar4X.Engine;
using Pulsar4X.Interfaces;

namespace GameEngine.People;

/// <summary>The three spans a ship bridge can be bought as. Stored values map to admin levels 0, 6, and 8.</summary>
public enum ShipCommandSpan
{
    Body = 0,
    Well = 1,
    System = 2,
}

public enum AdminLevel
{
    Ship,
    TaskUnit,
    TaskGroup,
    TaskForce,
    Fleet,
    Colony,
    Planet,
    SOI,
    System,
    Sector,
    Empire
}

public class AdminSpaceAtb  : IComponentDesignAttribute
{
    public AdminLevel AdminLevel { get; set; }
    public int ConsoleSpace { get; set; }

    public AdminSpaceAtb(int level, double space)
    {
        AdminLevel = (AdminLevel)level;
        ConsoleSpace = (int)space;
    }
    public void OnComponentInstallation(Entity parentEntity, ComponentInstance componentInstance)
    {
        if (!parentEntity.TryGetDataBlob<AdminSpaceDB>(out var adminSpaceDB))
        {
            adminSpaceDB = new AdminSpaceDB();
            parentEntity.SetDataBlob(adminSpaceDB);
            AdminSpaceProcessor.CalcEntityAdminSpace(parentEntity, adminSpaceDB);
        }
    }

    public void OnComponentUninstallation(Entity parentEntity, ComponentInstance componentInstance)
    {
        throw new System.NotImplementedException();
    }

    public string AtbName()
    {
        return nameof(AdminSpaceAtb);
    }

    public string AtbDescription()
    {
        return "Space for Administration";
    }
}