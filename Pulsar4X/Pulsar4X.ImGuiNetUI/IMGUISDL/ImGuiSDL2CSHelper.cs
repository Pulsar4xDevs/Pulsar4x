using SDL2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ImGuiNET;
using System.IO;
using System.Runtime.InteropServices;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace ImGuiSDL2CS {
    public static class ImGuiSDL2CSHelper {

        private static bool _Initialized = false;
        public static bool Initialized => _Initialized;

        public static void Init() {
            if (_Initialized)
                return;
            _Initialized = true;

            ImGuiIOPtr io = ImGui.GetIO();



            io.KeyMap[(int)ImGuiKey.Tab] = (int) SDL.SDL_Keycode.SDLK_TAB;
            io.KeyMap[(int)ImGuiKey.LeftArrow] = (int) SDL.SDL_Scancode.SDL_SCANCODE_LEFT;
            io.KeyMap[(int)ImGuiKey.RightArrow] = (int) SDL.SDL_Scancode.SDL_SCANCODE_RIGHT;
            io.KeyMap[(int)ImGuiKey.UpArrow] = (int) SDL.SDL_Scancode.SDL_SCANCODE_UP;
            io.KeyMap[(int)ImGuiKey.DownArrow] = (int) SDL.SDL_Scancode.SDL_SCANCODE_DOWN;
            io.KeyMap[(int)ImGuiKey.PageUp] = (int) SDL.SDL_Scancode.SDL_SCANCODE_PAGEUP;
            io.KeyMap[(int)ImGuiKey.PageDown] = (int) SDL.SDL_Scancode.SDL_SCANCODE_PAGEDOWN;
            io.KeyMap[(int)ImGuiKey.Home] = (int) SDL.SDL_Scancode.SDL_SCANCODE_HOME;
            io.KeyMap[(int)ImGuiKey.End] = (int) SDL.SDL_Scancode.SDL_SCANCODE_END;
            io.KeyMap[(int)ImGuiKey.Delete] = (int) SDL.SDL_Keycode.SDLK_DELETE;
            io.KeyMap[(int)ImGuiKey.Backspace] = (int) SDL.SDL_Keycode.SDLK_BACKSPACE;
            io.KeyMap[(int)ImGuiKey.Enter] = (int) SDL.SDL_Keycode.SDLK_RETURN;
            io.KeyMap[(int)ImGuiKey.Escape] = (int) SDL.SDL_Keycode.SDLK_ESCAPE;
            io.KeyMap[(int)ImGuiKey.A] = (int) SDL.SDL_Keycode.SDLK_a;
            io.KeyMap[(int)ImGuiKey.C] = (int) SDL.SDL_Keycode.SDLK_c;
            io.KeyMap[(int)ImGuiKey.V] = (int) SDL.SDL_Keycode.SDLK_v;
            io.KeyMap[(int)ImGuiKey.X] = (int) SDL.SDL_Keycode.SDLK_x;
            io.KeyMap[(int)ImGuiKey.Y] = (int) SDL.SDL_Keycode.SDLK_y;
            io.KeyMap[(int)ImGuiKey.Z] = (int) SDL.SDL_Keycode.SDLK_z;


            //io.GetClipboardTextFn((userData) => SDL.SDL_GetClipboardText());

            //io.SetSetClipboardTextFn((userData, text) => SDL.SDL_SetClipboardText(text));

            // If no font added, add default font.
            if (io.Fonts.Fonts.Size == 0)
                io.Fonts.AddFontDefault();
        }

        public static void NewFrame(Vector2 size, Vector2 scale, ImVec2 mousePosition, uint mouseMask, ref float mouseWheel, bool[] mousePressed, ref double g_Time) {
            ImGuiIOPtr io = ImGui.GetIO();
            io.DisplaySize = size;
            io.DisplayFramebufferScale = scale;

            double currentTime = SDL.SDL_GetTicks() / 1000D;
            io.DeltaTime = g_Time > 0D ? (float) (currentTime - g_Time) : (1f/60f);
            g_Time = currentTime;

            //io.MousePosition = mousePosition;

            io.MouseDown[0] = mousePressed[0] || (mouseMask & SDL.SDL_BUTTON(SDL.SDL_BUTTON_LEFT)) != 0;
            io.MouseDown[1] = mousePressed[1] || (mouseMask & SDL.SDL_BUTTON(SDL.SDL_BUTTON_RIGHT)) != 0;
            io.MouseDown[2] = mousePressed[2] || (mouseMask & SDL.SDL_BUTTON(SDL.SDL_BUTTON_MIDDLE)) != 0;
            mousePressed[0] = mousePressed[1] = mousePressed[2] = false;

            io.MouseWheel = mouseWheel;
            mouseWheel = 0f;

            SDL.SDL_ShowCursor(io.MouseDrawCursor ? 0 : 1);

            ImGui.NewFrame();
        }

        public static void Render(Vector2 size) {
            ImGui.Render();
            //if (ImGui.GetIO().RenderDrawListsFn == IntPtr.Zero)
                RenderDrawData(ImGui.GetDrawData(), (int) Math.Round(size.X), (int) Math.Round(size.Y));
        }
        
        public static void RenderDrawData(ImDrawDataPtr drawData, int displayW, int displayH) {
            // We are using the OpenGL fixed pipeline to make the example code simpler to read!
            // Setup render state: alpha-blending enabled, no face culling, no depth testing, scissor enabled, vertex/texcoord/color pointers.
            int lastTexture; GL.GetIntegerv(GL.Enum.GL_TEXTURE_BINDING_2D, out lastTexture);
            Int4 lastViewport; GL.GetIntegerv4(GL.Enum.GL_VIEWPORT, out lastViewport);
            Int4 lastScissorBox; GL.GetIntegerv4(GL.Enum.GL_SCISSOR_BOX, out lastScissorBox);

            GL.PushAttrib(GL.Enum.GL_ENABLE_BIT | GL.Enum.GL_COLOR_BUFFER_BIT | GL.Enum.GL_TRANSFORM_BIT);
            GL.Enable(GL.Enum.GL_BLEND);
            GL.BlendFunc(GL.Enum.GL_SRC_ALPHA, GL.Enum.GL_ONE_MINUS_SRC_ALPHA);
            GL.Disable(GL.Enum.GL_CULL_FACE);
            GL.Disable(GL.Enum.GL_DEPTH_TEST);
            GL.Enable(GL.Enum.GL_SCISSOR_TEST);
            GL.EnableClientState(GL.Enum.GL_VERTEX_ARRAY);
            GL.EnableClientState(GL.Enum.GL_TEXTURE_COORD_ARRAY);
            GL.EnableClientState(GL.Enum.GL_COLOR_ARRAY);
            GL.Enable(GL.Enum.GL_TEXTURE_2D);

            GL.UseProgram(0);

            // Handle cases of screen coordinates != from framebuffer coordinates (e.g. retina displays)
            ImGuiIOPtr io = ImGui.GetIO();

            //ImGui.ScaleClipRects(drawData, io.DisplayFramebufferScale); imgui.net doesn't apear to have this

            // Setup orthographic projection matrix
            GL.Viewport(0, 0, displayW, displayH);
            GL.MatrixMode(GL.Enum.GL_PROJECTION);
            GL.PushMatrix();
            GL.LoadIdentity();
            GL.Ortho(
                0.0f,
                io.DisplaySize.X / io.DisplayFramebufferScale.X,
                io.DisplaySize.Y / io.DisplayFramebufferScale.Y,
                0.0f,
                -1.0f,
                1.0f
            );
            GL.MatrixMode(GL.Enum.GL_MODELVIEW);
            GL.PushMatrix();
            GL.LoadIdentity();

            // Render command lists

            for (int n = 0; n < drawData.CmdListsCount; n++) {
                ImDrawListPtr cmdList = drawData.CmdListsRange[n];
                //ImDrawList cmdList = drawData[n];
                ImPtrVector<ImDrawVertPtr> vtxBuffer = cmdList.VtxBuffer;
                ImVector<ushort> idxBuffer = cmdList.IdxBuffer;
                int posOffset = 0;
                int uvOffset = 8;
                int colOffset = 16;
                //GL.VertexPointer(
                GL.VertexPointer(2, GL.Enum.GL_FLOAT, Unsafe.SizeOf<ImDrawVert>(), new IntPtr((long) vtxBuffer.Data + posOffset));
                GL.TexCoordPointer(2, GL.Enum.GL_FLOAT, Unsafe.SizeOf<ImDrawVert>(), new IntPtr((long) vtxBuffer.Data + uvOffset));
                GL.ColorPointer(4, GL.Enum.GL_UNSIGNED_BYTE, Unsafe.SizeOf<ImDrawVert>(), new IntPtr((long) vtxBuffer.Data + colOffset));

                long idxBufferOffset = 0;
                for (int cmdi = 0; cmdi < cmdList.CmdBuffer.Size; cmdi++) {
                    ImDrawCmdPtr pcmd = cmdList.CmdBuffer[cmdi];
                    if (pcmd.UserCallback != IntPtr.Zero) {
                        throw new NotImplementedException();
                        //pcmd.InvokeUserCallback(ref cmdList, ref pcmd);
                    } else {
                        GL.BindTexture(GL.Enum.GL_TEXTURE_2D, (int) pcmd.TextureId);
                        GL.Scissor(
                            (int) pcmd.ClipRect.X,
                            (int) (io.DisplaySize.Y - pcmd.ClipRect.W),
                            (int) (pcmd.ClipRect.Z - pcmd.ClipRect.X),
                            (int) (pcmd.ClipRect.W - pcmd.ClipRect.Y)
                        );
                        GL.DrawElements(GL.Enum.GL_TRIANGLES, (int) pcmd.ElemCount, GL.Enum.GL_UNSIGNED_SHORT, new IntPtr((long) idxBuffer.Data + idxBufferOffset));
                    }
                    idxBufferOffset += pcmd.ElemCount * 2 /*sizeof(ushort)*/;
                }
            }

            // Restore modified state
            GL.DisableClientState(GL.Enum.GL_COLOR_ARRAY);
            GL.DisableClientState(GL.Enum.GL_TEXTURE_COORD_ARRAY);
            GL.DisableClientState(GL.Enum.GL_VERTEX_ARRAY);
            GL.BindTexture(GL.Enum.GL_TEXTURE_2D, lastTexture);
            GL.MatrixMode(GL.Enum.GL_MODELVIEW);
            GL.PopMatrix();
            GL.MatrixMode(GL.Enum.GL_PROJECTION);
            GL.PopMatrix();
            GL.PopAttrib();
            GL.Viewport(lastViewport.X, lastViewport.Y, lastViewport.Z, lastViewport.W);
            GL.Scissor(lastScissorBox.X, lastScissorBox.Y, lastScissorBox.Z, lastScissorBox.W);
        }

        public static bool HandleEvent(SDL.SDL_Event e, ref float mouseWheel, bool[] mousePressed) {
            ImGuiIOPtr io = ImGui.GetIO();
            switch (e.type) {
                case SDL.SDL_EventType.SDL_MOUSEWHEEL:
                    if (e.wheel.y > 0)
                        mouseWheel = 1;
                    if (e.wheel.y < 0)
                        mouseWheel = -1;
                    return true;
                case SDL.SDL_EventType.SDL_MOUSEBUTTONDOWN:
                    if (mousePressed == null)
                        return true;
                    if (e.button.button == SDL.SDL_BUTTON_LEFT && mousePressed.Length > 0)
                        mousePressed[0] = true;
                    if (e.button.button == SDL.SDL_BUTTON_RIGHT && mousePressed.Length > 1)
                        mousePressed[1] = true;
                    if (e.button.button == SDL.SDL_BUTTON_MIDDLE && mousePressed.Length > 2)
                        mousePressed[2] = true;
                    return true;
                case SDL.SDL_EventType.SDL_TEXTINPUT:
                    unsafe
                    {
                        // THIS IS THE ONLY UNSAFE THING LEFT!

                        ImGui.GetIO().AddInputCharactersUTF8(e.text.ToString()); //maybe?
                    }
                    return true;
                case SDL.SDL_EventType.SDL_KEYDOWN:
                case SDL.SDL_EventType.SDL_KEYUP:
                    int key = (int) e.key.keysym.sym & ~SDL.SDLK_SCANCODE_MASK;
                    io.KeysDown[key] = e.type == SDL.SDL_EventType.SDL_KEYDOWN;
                    SDL.SDL_Keymod keyModState = SDL.SDL_GetModState();

                    io.KeyShift = (keyModState & SDL.SDL_Keymod.KMOD_SHIFT) != 0;
                    io.KeyCtrl = (keyModState & SDL.SDL_Keymod.KMOD_CTRL) != 0;
                    io.KeyAlt = (keyModState & SDL.SDL_Keymod.KMOD_ALT) != 0;
                    io.KeySuper = (keyModState & SDL.SDL_Keymod.KMOD_GUI) != 0;
                    return true;
            }

            return true;
        }

    }
}
