using System.Text;
using System.Threading.Tasks;
using System.Numerics;
using ImGuiNET;
using ImGuiSDL2CS;
using Pulsar4X.ECSLib;
using SDL2;

namespace Pulsar4X.SDL2UI
{
    class PlanetaryWindow : PulsarGuiWindow
    {

        private EntityState _lookedAtEntity;

        private PlanetaryWindow(EntityState entity)
        {
            //_flags = ImGuiWindowFlags.NoCollapse;


            onEntityChange(entity);
           
        }


        internal void onEntityChange(EntityState entity)
        {
            _lookedAtEntity = entity;
        }

        internal static PlanetaryWindow GetInstance(EntityState entity)
        {

            PlanetaryWindow thisItem;
            if (!_state.LoadedWindows.ContainsKey(typeof(PlanetaryWindow)))
            {
                thisItem = new PlanetaryWindow(entity);
            }
            else
            {
                thisItem = (PlanetaryWindow)_state.LoadedWindows[typeof(PlanetaryWindow)];
                thisItem.onEntityChange(entity);
            }


            return thisItem;


        }


        internal override void MapClicked(ECSLib.Vector3 worldPos_m, MouseButtons button)
        {

        }

        internal override void Display()
        {
            if (IsActive == true && ImGui.Begin("Planetary interface", ref IsActive, _flags))
            {
                if (_lookedAtEntity.Entity.HasDataBlob<ColonyInfoDB>())
                {

                }
            }

        }
    }
}

