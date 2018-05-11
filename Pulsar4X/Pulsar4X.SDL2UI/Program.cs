using System;
using System.Runtime.InteropServices;
using SDL2;
using ImGuiNET;
using ImGuiSDL2CS;
using System.Drawing;

namespace Pulsar4X.SDL2UI
{
    public class Program
    {
        static SDL2Window Instance;
        [STAThread]
        public static void Main()
        {

            Instance = new PulsarMainWindow();
            Instance.Run();
            Instance.Dispose();
        }
    }

    public class PulsarMainWindow : ImGuiSDL2CSWindow
    {
        private GlobalUIState _state = new GlobalUIState();
        private TextInputBuffer[] _TextInputBuffers;

        private MemoryEditor _MemoryEditor = new MemoryEditor();
        private byte[] _MemoryEditorData;

        private FileDialog _Dialog = new FileDialog(false, false, true, false, false, false);

        ImVec3 backColor = new ImVec3(0 / 255f, 0 / 255f, 28 / 255f);
         

        public PulsarMainWindow()
            : base("Pulsar4X")
        {
            _state.MainWinSize = this.Size;

            // Create any managed resources and set up the main game window here.
            _MemoryEditorData = new byte[1024];
            Random rnd = new Random();
            for (int i = 0; i < _MemoryEditorData.Length; i++)
            {
                _MemoryEditorData[i] = (byte)rnd.Next(255);
            }
            backColor = new ImVec3(0 / 255f, 0 / 255f, 28 / 255f);

            _state.MapRendering = new SystemMapRendering(this, _state);
        }

        public unsafe override void ImGuiLayout()
        {

            if (_state.MainMenu.IsActive)
                _state.MainMenu.Display();
            if (_state.NewGameOptions.IsActive)
                _state.NewGameOptions.Display();
        }


        public override void ImGuiRender()
        {
            GL.ClearColor(backColor.X, backColor.Y, backColor.Z, 1f);
            GL.Clear(GL.Enum.GL_COLOR_BUFFER_BIT);

            if (_state.IsGameLoaded)
                _state.MapRendering.Display();

            // Render ImGui on top of the rest.
            base.ImGuiRender();
        }
    }



    internal class GlobalUIState
    {
        internal ECSLib.Game Game;
        internal ECSLib.UIStateVM UIStateVM;
        internal bool IsGameLoaded {get {return Game != null;}}
        internal ECSLib.Entity Faction { get { return UIStateVM.FactionEntity; } }

        internal MainMenuItems MainMenu { get; }
        internal NewGameOptions NewGameOptions { get; }
        internal SystemMapRendering MapRendering { get; set; }

        internal ImVec2 MainWinSize { get; set; }

        internal GlobalUIState()
        {
            MainMenu = new MainMenuItems(this);
            NewGameOptions = new NewGameOptions(this);
        }
    }

    public class MainMenuItems : PulsarGuiWindow
    {

        bool _saveGame = false;
        GlobalUIState _state; 

        ImVec2 buttonSize = new ImVec2(184, 24);
        internal MainMenuItems(GlobalUIState state)
        {
            IsActive = true;
            _state = state;
            _flags = ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoTitleBar;
        }

        internal override void Display()
        {
            
            ImGui.SetNextWindowSize(new ImVec2(200, 100), ImGuiCond.FirstUseEver);
            ImGui.Begin("Pulsar4X Main Menu",  ref IsActive, _flags);

            if (ImGui.Button("Start a New Game", buttonSize))
            {
                _state.NewGameOptions.IsActive = true;
                this.IsActive = false;
            }
            if (_state.IsGameLoaded)
                if (ImGui.Button("Save Current Game", buttonSize))
                    _saveGame = !_saveGame;    
            ImGui.Button("Resume a Current Game", buttonSize);
            ImGui.Button("Connect to a Network Game", buttonSize);
            //if (_saveGame)

            ImGui.End();
        }
    }

    public abstract class PulsarGuiWindow
    {
        protected ImGuiWindowFlags _flags = ImGuiWindowFlags.Default;
        internal bool IsActive = false;
        internal abstract void Display();
    }
}
