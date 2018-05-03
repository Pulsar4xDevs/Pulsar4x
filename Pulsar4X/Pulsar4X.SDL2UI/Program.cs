using System;
using System.Runtime.InteropServices;
using SDL2;
using ImGuiNET;
using ImGuiSDL2CS;


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

        public PulsarMainWindow()
            : base("Pulsar4X")
        {

            // Create any managed resources and set up the main game window here.
            _MemoryEditorData = new byte[1024];
            Random rnd = new Random();
            for (int i = 0; i < _MemoryEditorData.Length; i++)
            {
                _MemoryEditorData[i] = (byte)rnd.Next(255);
            }

        }

        public unsafe override void ImGuiLayout()
        {
            
            // 1. Show a simple window
            // Tip: if we don't call ImGui.Begin()/ImGui.End() the widgets appears in a window automatically called "Debug"
            {
                
                ImGui.SetNextWindowSize(new ImVec2(200, 100), ImGuiCond.FirstUseEver);
                if (_state.MainMenu.IsActive)
                    _state.MainMenu.Display();
                if (_state.NewGameOptions.IsActive)
                    _state.NewGameOptions.Display();
            }
        }
    }

    internal class GlobalUIState
    {
        internal ECSLib.Game Game;
        internal bool IsGameLoaded
        {
            get
            {
                return Game != null;
            }
        }


        internal MainMenuItems MainMenu { get; }
        internal NewGameOptions NewGameOptions { get; }

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

        internal MainMenuItems(GlobalUIState state)
        {
            IsActive = true;
            _state = state;
        }

        internal override void Display()
        {
            
            ImGui.SetNextWindowSize(new ImVec2(200, 100), ImGuiCond.FirstUseEver);

            ImGui.Begin("Pulsar4X Main Menu", ref IsActive, ImGuiWindowFlags.NoTitleBar);

            if (ImGui.Button("Start a New Game"))
            {
                _state.NewGameOptions.IsActive = true;
                this.IsActive = false;
            }
            if (_state.IsGameLoaded)
                if (ImGui.Button("Save Current Game"))
                    _saveGame = !_saveGame;    
            ImGui.Button("Resume a Current Game");
            ImGui.Button("Connect to a Network Game");
            //if (_saveGame)

            ImGui.End();
        }
    }

    public abstract class PulsarGuiWindow
    {
        internal bool IsActive = false;
        internal abstract void Display();
    }
}
