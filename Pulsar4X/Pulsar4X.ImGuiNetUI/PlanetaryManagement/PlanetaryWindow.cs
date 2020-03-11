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
        private enum PlanetarySubWindows{
        generalInfo, installations
        }
        private EntityState _lookedAtEntity;
        private PlanetarySubWindows _selectedSubWindow = PlanetarySubWindows.generalInfo;

        private PlanetaryWindow(EntityState entity)
        {
            //_flags = ImGuiWindowFlags.NoCollapse;

            _flags = ImGuiWindowFlags.AlwaysAutoResize;
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
            ImGui.SetNextWindowSize(new Vector2(400,400),ImGuiCond.Once);
            if (IsActive == true && ImGui.Begin("Planetary window: "+_lookedAtEntity.Name, ref IsActive, _flags))
            {
                if(ImGui.SmallButton("general info")){
                    _selectedSubWindow = PlanetarySubWindows.generalInfo;
                }
                ImGui.SameLine();
                if(ImGui.SmallButton("installations")){
                    _selectedSubWindow = PlanetarySubWindows.installations;
                }
                ImGui.BeginChild("data");
                switch(_selectedSubWindow){
                    case PlanetarySubWindows.generalInfo:
                        if(_lookedAtEntity.Entity.HasDataBlob<MassVolumeDB>()){
                            var tempMassVolume = _lookedAtEntity.Entity.GetDataBlob<MassVolumeDB>();
                            ImGui.Text("radius: "+ECSLib.Misc.StringifyDistance(tempMassVolume.RadiusInM));
                            ImGui.Text("mass: "+tempMassVolume.Mass.ToString() + " kg");
                            ImGui.Text("volume: " +tempMassVolume.VolumeM3.ToString() + " m^3");
                            ImGui.Text("density: "+tempMassVolume.Density + " kg/m^3");
                        }
                        if (_lookedAtEntity.Entity.HasDataBlob<ColonyInfoDB>())
                        {
                            ColonyInfoDB tempColonyInfo = _lookedAtEntity.Entity.GetDataBlob<ColonyInfoDB>();
                            ImGui.Text("populations: ");
                            foreach(var popPerSpecies in tempColonyInfo.Population){
                                ImGui.Text(popPerSpecies.Value.ToString()+" of species: ");
                                ImGui.SameLine();
                                if(popPerSpecies.Key.HasDataBlob<NameDB>()){
                                    ImGui.Text(popPerSpecies.Key.GetDataBlob<NameDB>().DefaultName);
                                }else {
                                    ImGui.Text("unknown.");
                                }
                            }
                        }
                        if(_lookedAtEntity.Entity.HasDataBlob<InstallationsDB>()){
                            InstallationsDB tempInstallations = _lookedAtEntity.Entity.GetDataBlob<InstallationsDB>();
                    
                        }
                   
                    
                    break;
                    case PlanetarySubWindows.installations:
                    break;
                    default:
                    break;
                }
                ImGui.EndChild();
                ImGui.End();
            }

        }
    }
}

