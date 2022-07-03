using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Numerics;
using ImGuiNET;
using ImGuiSDL2CS;
using Pulsar4X.ECSLib;
using SDL2;

namespace Pulsar4X.SDL2UI
{
    class DistanceRuler : PulsarGuiWindow
    {
        //measuring variables
        //measuring booleans
        private bool _measuring = false;
        private bool _firstClickDone = false;
        private float _zoomLevelAtFirstClick = 0;
        //~measuring booleans
        private Orbital.Vector3 _firstClick;
        private System.Numerics.Vector2 _firstClickInViewCoord;
        //~measuring variables
        private void _stopMeasuring()
        {
            _measuring = false;
            _firstClickDone = false;
        }


        private DistanceRuler() {
            //_flags = ImGuiWindowFlags.NoCollapse;
        }

        

        internal static DistanceRuler GetInstance() {

            DistanceRuler thisItem;
            if (!_uiState.LoadedWindows.ContainsKey(typeof(DistanceRuler)))
            {
                thisItem = new DistanceRuler();
            }
            else
            {
                thisItem = (DistanceRuler)_uiState.LoadedWindows[typeof(DistanceRuler)];
            }
             

            return thisItem;


        }


        internal override void MapClicked(Orbital.Vector3 worldPos_m, MouseButtons button)
        {
            base.MapClicked(worldPos_m, button);
            if (true)//button == MouseButtons::Primary)
            {
                //first checks if measuting
                if (_measuring)
                {
                    //if measuring register first click
                    if (!_firstClickDone)
                    {
                        _zoomLevelAtFirstClick = _uiState.Camera.ZoomLevel;
                        _firstClick = worldPos_m;
                        _firstClickInViewCoord = ImGui.GetMousePos();
                        _firstClickDone = true;
                    }
                    //if first registered then the click after stops the measuting, resetting all measuring booleans(marked with comments above)
                    else
                    {
                        _stopMeasuring();
                    }
                }
            }
           
        }
        internal override void Display()
        {
            if(IsActive == true && ImGui.Begin("Map Scale", ref IsActive, _flags))//Lets the user close the ruler
            {
                //displays the size in meters of the current screen area account for zoom and window dimensions
                var windowCornerInWorldCoordinate = _uiState.Camera.WorldCoordinate_m((int)_uiState.MainWinSize.X, (int)_uiState.MainWinSize.Y);
                ImGui.Text("Current screen is:");
                ImGui.Text(ECSLib.Stringify.Distance(((windowCornerInWorldCoordinate.X - _uiState.Camera.CameraWorldPosition_m.X)*2))+" wide.");
                ImGui.Text(ECSLib.Stringify.Distance((-(windowCornerInWorldCoordinate.Y - _uiState.Camera.CameraWorldPosition_m.Y)*2))+" tall.");
                //ImGui.Text((_uiState.Camera.WorldCoordinate_m((int)_uiState.Camera.ViewPortSize.X, (int)_uiState.Camera.ViewPortSize.Y).X - _uiState.Camera.CameraWorldPosition_m.X).ToString());
                var mpp = _uiState.Camera.WorldDistance_m(1);
                /* I can't math. why can't I math?
                var lightMetersPerS = 299792458;
                var lightsecondsPerMeter = 3.33564e-9;
                var lspp = lightsecondsPerMeter * mpp;
                var ppls = mpp / 299792458;
                ImGui.Text("Meters Per Pixel: " + mpp);
                ImGui.Text("Light Seconds Per Pixel: " + lspp);
                ImGui.Text("Pixels Per Light Second: " + ppls);
                */
                //the measure button, when clicked class starts listening for first mouse click to start measuring stick, wherever the mouse goes after that is the other end of the measuring stick.
                if (ImGui.Button("Measure"))
                {
                    _measuring = true;
                }
                //if the first click hasnt been done but the Measure button has been clicked, then display tooltip indicating you are measuring.
                if (_measuring && !_firstClickDone)
                {
                    ImGui.SetTooltip("measuring...");
                }
                //if the first click has already been done, then start showing distance and draw line between first click and the latest mouse position
                else if (_firstClickDone)
                {
                    if(_zoomLevelAtFirstClick != _uiState.Camera.ZoomLevel){
                        _stopMeasuring();
                    }else{
                        Orbital.Vector3 lastMousePos = _uiState.Camera.MouseWorldCoordinate_m();
                        System.Numerics.Vector2 lastMousePosInViewCoord = ImGui.GetMousePos();

                        SDL.SDL_SetRenderDrawColor(_uiState.rendererPtr, 255,255,255,255);
                        SDL.SDL_RenderDrawLine(_uiState.rendererPtr, (int)_firstClickInViewCoord.X, (int)_firstClickInViewCoord.Y, (int)lastMousePosInViewCoord.X, (int)lastMousePosInViewCoord.Y);
                        double metricDistance = Math.Sqrt(Math.Pow(_firstClick.X - lastMousePos.X, 2) + Math.Pow(_firstClick.Y - lastMousePos.Y, 2));
                        double lightseconds = metricDistance / 299792458;
                        string tooltipString = Stringify.Distance(metricDistance) + "\r\n" + lightseconds + "ls";
                        ImGui.SetTooltip(tooltipString);
                    }
                }

                ImGui.End();
            }
            else
            {
                _stopMeasuring();
            }

        }

        public override void OnGameTickChange(DateTime newDate)
        {
        }

        public override void OnSystemTickChange(DateTime newDate)
        {
        }

        public override void OnSelectedSystemChange(StarSystem newStarSys)
        {
            throw new NotImplementedException();
        }
    }
}
