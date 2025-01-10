using System;
using System.Runtime.CompilerServices;
using ImGuiNET;
using ImGuiSDL2CS;
using SDL2;

namespace Pulsar4X.Client.Rendering;

public class OpenGLRenderer : IRenderer
{
    private IntPtr _glContext;
    private IntPtr _windowHandle;

    public void Initialize(IntPtr windowHandle)
    {
        _windowHandle = windowHandle;

        // Set the OpenGL context attributes
        SDL.SDL_GL_SetAttribute(SDL.SDL_GLattr.SDL_GL_CONTEXT_MAJOR_VERSION, 4);
        SDL.SDL_GL_SetAttribute(SDL.SDL_GLattr.SDL_GL_CONTEXT_MINOR_VERSION, 5);
        SDL.SDL_GL_SetAttribute(SDL.SDL_GLattr.SDL_GL_CONTEXT_PROFILE_MASK, (int)SDL.SDL_GLprofile.SDL_GL_CONTEXT_PROFILE_CORE);

        // Create the OpenGL context
        _glContext = SDL.SDL_GL_CreateContext(_windowHandle);
        if(_glContext == IntPtr.Zero)
        {
            throw new Exception($"Failed to create OpenGL context: {SDL.SDL_GetError()}");
        }

        // Make the OpenGL context current
        SDL.SDL_GL_MakeCurrent(_windowHandle, _glContext);
    }

    public void BeginFrame()
    {
        GL.Clear(GL.Enum.GL_COLOR_BUFFER_BIT);
    }

    public void EndFrame()
    {
        SDL.SDL_GL_SwapWindow(_windowHandle);
    }

    public void Clear(float r, float g, float b, float a)
    {
        GL.ClearColor(r, g, b, a);
    }

    public int CreateDefaultFontTexture(int width, int height, IntPtr pixels)
    {
        // Create OpenGL texture
        GL.GenTextures(1, out int fontTextureID);
        GL.BindTexture(GL.Enum.GL_TEXTURE_2D, fontTextureID);
        GL.TexParameteri(GL.Enum.GL_TEXTURE_2D, GL.Enum.GL_TEXTURE_MIN_FILTER, (int) GL.Enum.GL_LINEAR);
        GL.TexParameteri(GL.Enum.GL_TEXTURE_2D, GL.Enum.GL_TEXTURE_MAG_FILTER, (int) GL.Enum.GL_LINEAR);
        GL.PixelStorei(GL.Enum.GL_UNPACK_ROW_LENGTH, 0);
        GL.TexImage2D(
            GL.Enum.GL_TEXTURE_2D,
            0,
            (int) GL.Enum.GL_ALPHA,
            width,
            height,
            0,
            GL.Enum.GL_ALPHA,
            GL.Enum.GL_UNSIGNED_BYTE,
            pixels
        );

        return fontTextureID;
    }

    public void RenderImGui(ImDrawDataPtr drawData, int displayWidth, int displayHeight)
    {
        // We are using the OpenGL fixed pipeline to make the example code simpler to read!
        // Setup render state: alpha-blending enabled, no face culling, no depth testing, scissor enabled, vertex/texcoord/color pointers.
        GL.GetIntegerv(GL.Enum.GL_TEXTURE_BINDING_2D, out int lastTexture);
        GL.GetIntegerv4(GL.Enum.GL_VIEWPORT, out Int4 lastViewport);
        GL.GetIntegerv4(GL.Enum.GL_SCISSOR_BOX, out Int4 lastScissorBox);

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
        GL.Viewport(0, 0, displayWidth, displayHeight);
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

        for (int n = 0; n < drawData.CmdListsCount; n++)
        {
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
            for (int cmdi = 0; cmdi < cmdList.CmdBuffer.Size; cmdi++)
            {
                ImDrawCmdPtr pcmd = cmdList.CmdBuffer[cmdi];

                if (pcmd.UserCallback != IntPtr.Zero)
                {
                    throw new NotImplementedException();
                    //pcmd.InvokeUserCallback(ref cmdList, ref pcmd);
                }
                else if (io.Fonts.TexID == pcmd.TextureId)
                {
                    GL.BindTexture(GL.Enum.GL_TEXTURE_2D, (int)pcmd.TextureId);
                    GL.Scissor(
                        (int)pcmd.ClipRect.X,
                        (int)(io.DisplaySize.Y - pcmd.ClipRect.W),
                        (int)(pcmd.ClipRect.Z - pcmd.ClipRect.X),
                        (int)(pcmd.ClipRect.W - pcmd.ClipRect.Y)
                    );
                    GL.DrawElements(GL.Enum.GL_TRIANGLES, (int)pcmd.ElemCount, GL.Enum.GL_UNSIGNED_SHORT, new IntPtr((long)idxBuffer.Data + idxBufferOffset));

                }
                else
                {
                    float w, h;
                    var txid = pcmd.TextureId;
                    var sdlid = SDL.SDL_GL_BindTexture(pcmd.TextureId, out w, out h);

                    string errstr = SDL.SDL_GetError();
                    GL.Scissor(
                    (int)pcmd.ClipRect.X,
                    (int)(io.DisplaySize.Y - pcmd.ClipRect.W),
                    (int)(pcmd.ClipRect.Z - pcmd.ClipRect.X),
                    (int)(pcmd.ClipRect.W - pcmd.ClipRect.Y)
                    );

                    GL.DrawElements(GL.Enum.GL_TRIANGLES, (int)pcmd.ElemCount, GL.Enum.GL_UNSIGNED_SHORT, new IntPtr((long)idxBuffer.Data + idxBufferOffset));

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

    public void Dispose()
    {
        if(_glContext != IntPtr.Zero)
        {
            SDL.SDL_GL_DeleteContext(_glContext);
            _glContext = IntPtr.Zero;
        }
    }
}