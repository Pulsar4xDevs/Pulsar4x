using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using Pulsar4X.Engine;

namespace BenchmarkProject;

[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net80)]
public class Benchmarks
{
    private Game? _game;
    private Entity? _playerFaction;
    private string _startingSystemId;

    [GlobalSetup]
    public void Setup()
    {
        string[] modsToLoad = new[] { "../../../../Data/basemod/modInfo.json" };
        NewGameSettings newGameSettings = new NewGameSettings
        {
            GameName = "Benchmarks",
            MaxSystems = 2,
            SMPassword = "",
            CreatePlayerFaction = true,
            DefaultFactionName = "UEF",
            DefaultPlayerPassword = "",
            DefaultSolStart = true,
            MasterSeed = 1234
        };
        
        _game = GameFactory.CreateGame(modsToLoad, newGameSettings);
        (_playerFaction, _startingSystemId) = DefaultStartFactory.LoadFromJson(_game, "../../../../Data/basemod/defaultStart.json");
    }

    [Benchmark]
    public void BenchmarkDefaultTimeStep()
    {
        // Run a single time step with the default tick length
        _game?.TimePulse.TimeStep();
    }

    [Benchmark]
    public void Benchmark30DaysTimeStep()
    {
        if (_game == null) return;
        
        _game.TimePulse.Ticklength = TimeSpan.FromDays(30);
        _game.TimePulse.TimeStep();
    }
    
    [Benchmark]
    public void Benchmark365DaysTimeStep()
    {
        if (_game == null) return;
        
        _game.TimePulse.Ticklength = TimeSpan.FromDays(365);
        _game.TimePulse.TimeStep();
    }
}