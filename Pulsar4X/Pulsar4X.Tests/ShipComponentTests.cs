using System;
using System.Collections.Generic;
using NUnit.Framework;
using Pulsar4X.ECSLib;

namespace Pulsar4X.Tests
{
    [TestFixture]
    [Description("Component Tests")]
    internal class ComponentTests
    {
        private Game _game;
        private EntityManager _entityManager;
        private Entity _faction;
        private Entity _colonyEntity;
        private MineralSD _duraniumSD;
        private MineralSD _corundiumSD;
        private StarSystem _starSystem;
        private Entity _shipClass;
        private Entity _ship;
        private Entity _engineComponent;
        [SetUp]
        public void Init()
        {
            _game = Game.NewGame("Test Game", DateTime.Now, 1);
            Tech();
            _faction = FactionFactory.CreateFaction(_game, "Terran");

            _starSystem = new StarSystem(_game, "Sol", -1);
            /////Ship Class/////
            _shipClass = ShipFactory.CreateNewShipClass(_game, _faction, "TestClass");




        }

        private void Tech()
        {
            TechSD enginePowerModMax = new TechSD();
            enginePowerModMax.ID = new Guid("b8ef73c7-2ef0-445e-8461-1e0508958a0e");
            enginePowerModMax.MaxLevel = 7;
            enginePowerModMax.ExpressionData = "[Level] * 1.5";
            enginePowerModMax.Name = "Maximum Engine Power Modifier";
            enginePowerModMax.Description = "";
            enginePowerModMax.Category = ResearchCategories.PowerAndPropulsion;
            enginePowerModMax.Cost = 1;
            enginePowerModMax.Requirements = new Dictionary<Guid, int>();

            _game.StaticData.Techs.Add(enginePowerModMax.ID, enginePowerModMax);

            TechSD enginePowerModMin = new TechSD();
            enginePowerModMin.ID = new Guid("08fa4c4b-0ddb-4b3a-9190-724d715694de");
            enginePowerModMin.MaxLevel = 7;
            enginePowerModMin.ExpressionData = "0.5 - [Level] * 0.1";
            enginePowerModMin.Name = "Minimum Engine Power Modifier";
            enginePowerModMin.Description = "";
            enginePowerModMin.Category = ResearchCategories.PowerAndPropulsion;
            enginePowerModMin.Cost = 1;
            enginePowerModMin.Requirements = new Dictionary<Guid, int>();

            _game.StaticData.Techs.Add(enginePowerModMin.ID, enginePowerModMin);
         
        }


    }
}
