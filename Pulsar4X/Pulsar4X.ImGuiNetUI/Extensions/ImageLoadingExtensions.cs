using Pulsar4X.ECSLib;
using SDL2;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Pulsar4X.SDL2UI
{
    public static class ImageLoadingExtensions
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="_uiState">Global UI State Instance</param>
        /// <param name="imgName">Name you wish to refer to the image as</param>
        /// <param name="differentFilename">Different name of .bmp file in Resources</param>
        /// <returns></returns>
        public static IntPtr ImgByName(this GlobalUIState _uiState, string imgName, string differentFilename = "")
        {
            if (!_uiState.SDLImageDictionary.ContainsKey(imgName))
            {
                // Load the image
                var filename = differentFilename.IsNotNullOrEmpty() ? differentFilename : imgName;
                var path = Path.Combine("Resources", filename + ".bmp");

                IntPtr sdlSurface = SDL.SDL_LoadBMP(path);
                IntPtr sdltexture = SDL.SDL_CreateTextureFromSurface(_uiState.rendererPtr, sdlSurface);

                // Add to collection of loaded images
                _uiState.SDLImageDictionary.Add(imgName, sdltexture);
            }

            return _uiState.SDLImageDictionary[imgName];
        }

        #region Implementations for Specific Images
        public static IntPtr Img_Logo(this GlobalUIState _uiState)
        {
            //LoadImg("Logo", Path.Combine( rf,"PulsarLogo.bmp"));
            return _uiState.ImgByName("Logo", "PulsarLogo");
        }

        public static IntPtr Img_Play(this GlobalUIState _uiState)
        {
            //LoadImg("PlayImg", Path.Combine( rf,"Play.bmp"));
            return _uiState.ImgByName("PlayImg", "Play");
        }

        public static IntPtr Img_Pause(this GlobalUIState _uiState)
        {
            //LoadImg("PauseImg", Path.Combine( rf,"Pause.bmp"));
            return _uiState.ImgByName("PauseImg", "Pause");
        }

        public static IntPtr Img_OneStep(this GlobalUIState _uiState)
        {
            //LoadImg("OneStepImg", Path.Combine( rf,"OneStep.bmp"));
            return _uiState.ImgByName("OneStepImg", "OneStep");
        }

        public static IntPtr Img_Up(this GlobalUIState _uiState)
        {
            //LoadImg("UpImg", Path.Combine( rf,"UpArrow.bmp"));
            return _uiState.ImgByName("UpImg", "UpArrow");
        }

        public static IntPtr Img_Down(this GlobalUIState _uiState)
        {
            //LoadImg("DnImg", Path.Combine( rf,"DnArrow.bmp"));
            return _uiState.ImgByName("DnImg", "DnArrow");
        }

        public static IntPtr Img_Repeat(this GlobalUIState _uiState)
        {
            //LoadImg("RepeatImg", Path.Combine( rf,"RepeatIco.bmp"));
            return _uiState.ImgByName("RepeatImg", "RepeatIco");
        }

        public static IntPtr Img_Cancel(this GlobalUIState _uiState)
        {
            //LoadImg("CancelImg", Path.Combine( rf,"CancelIco.bmp"));
            return _uiState.ImgByName("CancelImg", "CancelIco");
        }

        public static IntPtr Img_DesComponent(this GlobalUIState _uiState)
        {
            //LoadImg("DesComp", Path.Combine(rf, "DesignComponentIco.bmp"));
            return _uiState.ImgByName("DesComp", "DesignComponentIco");
        }

        public static IntPtr Img_DesignShip(this GlobalUIState _uiState)
        {
            //LoadImg("DesShip", Path.Combine(rf, "DesignShipIco.bmp"));
            return _uiState.ImgByName("DesShip", "DesignShipIco");
        }

        public static IntPtr Img_DesignOrdnance(this GlobalUIState _uiState)
        {
            //LoadImg("DesOrd", Path.Combine(rf, "DesignOrdnanceIco.bmp"));
            return _uiState.ImgByName("DesOrd", "DesignOrdnanceIco");
        }

        public static IntPtr Img_GalaxyMap(this GlobalUIState _uiState)
        {
            //LoadImg("GalMap", Path.Combine(rf, "GalaxyMapIco.bmp"));
            return _uiState.ImgByName("GalMap", "GalaxyMapIco");
        }

        public static IntPtr Img_Research(this GlobalUIState _uiState)
        {
            //LoadImg("Research", Path.Combine(rf, "ResearchIco.bmp"));
            return _uiState.ImgByName("Research", "ResearchIco");
        }

        public static IntPtr Img_Power(this GlobalUIState _uiState)
        {
            //LoadImg("Power", Path.Combine(rf, "PowerIco.bmp"));
            return _uiState.ImgByName("Power", "PowerIco");
        }

        public static IntPtr Img_Ruler(this GlobalUIState _uiState)
        {
            //LoadImg("Ruler", Path.Combine(rf, "RulerIco.bmp"));
            return _uiState.ImgByName("Ruler", "RulerIco");
        }

        public static IntPtr Img_Cargo(this GlobalUIState _uiState)
        {
            //LoadImg("Cargo", Path.Combine(rf, "CargoIco.bmp"));
            return _uiState.ImgByName("Cargo", "CargoIco");
        }

        public static IntPtr Img_Firecon(this GlobalUIState _uiState)
        {
            //LoadImg("Firecon", Path.Combine(rf, "FireconIco.bmp"));
            return _uiState.ImgByName("Firecon", "FireconIco");
        }

        public static IntPtr Img_Industry(this GlobalUIState _uiState)
        {
            //LoadImg("Industry", Path.Combine(rf, "IndustryIco.bmp"));
            return _uiState.ImgByName("Industry", "IndustryIco");
        }

        public static IntPtr Img_Pin(this GlobalUIState _uiState)
        {
            //LoadImg("Pin", Path.Combine(rf, "PinIco.bmp"));
            return _uiState.ImgByName("Pin", "PinIco");
        }

        public static IntPtr Img_Rename(this GlobalUIState _uiState)
        {
            //LoadImg("Rename", Path.Combine(rf, "RenameIco.bmp"));
            return _uiState.ImgByName("Rename", "RenameIco");
        }

        public static IntPtr Img_Select(this GlobalUIState _uiState)
        {
            //LoadImg("Select", Path.Combine(rf, "SelectIco.bmp"));
            return _uiState.ImgByName("Select", "SelectIco");
        }

        public static IntPtr Img_Tree(this GlobalUIState _uiState)
        {
            //LoadImg("Tree", Path.Combine(rf, "TreeIco.bmp"));
            return _uiState.ImgByName("Tree", "TreeIco");
        }
        #endregion
    }
}
