using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ImGuiNET;
using ImGuiSDL2CS;
using Pulsar4X.DataStructures;
using Pulsar4X.SDL2UI;
using SDL2;

namespace Pulsar4X.Client.Rendering;

public class OpenGLRenderer : IRenderer
{
    private IntPtr _glContext;
    private IntPtr _windowHandle;
    private LineRenderer _lineRenderer;
    private IntPtr _previousContext = IntPtr.Zero;

    private List<uint> _textures = new ();

    public void SetAttributes()
    {
        SDL.SDL_GL_SetAttribute(SDL.SDL_GLattr.SDL_GL_RED_SIZE, 8);
        SDL.SDL_GL_SetAttribute(SDL.SDL_GLattr.SDL_GL_GREEN_SIZE, 8);
        SDL.SDL_GL_SetAttribute(SDL.SDL_GLattr.SDL_GL_BLUE_SIZE, 8);
        SDL.SDL_GL_SetAttribute(SDL.SDL_GLattr.SDL_GL_ALPHA_SIZE, 8);
        SDL.SDL_GL_SetAttribute(SDL.SDL_GLattr.SDL_GL_DOUBLEBUFFER, 1);
        SDL.SDL_GL_SetAttribute(SDL.SDL_GLattr.SDL_GL_DEPTH_SIZE, 24);
        SDL.SDL_GL_SetAttribute(SDL.SDL_GLattr.SDL_GL_STENCIL_SIZE, 8);
#if WINDOWS
        SDL.SDL_GL_SetAttribute(SDL.SDL_GLattr.SDL_GL_CONTEXT_MAJOR_VERSION, 4);
        SDL.SDL_GL_SetAttribute(SDL.SDL_GLattr.SDL_GL_CONTEXT_MINOR_VERSION, 6);
#endif
        SDL.SDL_GL_SetAttribute(SDL.SDL_GLattr.SDL_GL_CONTEXT_PROFILE_MASK, (int)SDL.SDL_GLprofile.SDL_GL_CONTEXT_PROFILE_CORE);
    }

    public void Initialize(IntPtr windowHandle)
    {
        // Print all OpenGL extensions
        GL.GetIntegerv(GL.Enum.GL_NUM_EXTENSIONS, out var numExtensions);
        Console.WriteLine($"Number of OpenGL extensions: {numExtensions}");
        Console.WriteLine("Looking for shader-related extensions:");
        for (int i = 0; i < numExtensions; i++)
        {
            IntPtr extPtr = GL.GetStringi(GL.Enum.GL_EXTENSIONS, (uint)i);
            string? ext = System.Runtime.InteropServices.Marshal.PtrToStringAnsi(extPtr);
            if (ext != null && (ext.Contains("shader") || ext.Contains("SHADER")))
            {
                Console.WriteLine($"Found extension: {ext}");
            }
        }

        _windowHandle = windowHandle;

        // Create the OpenGL context
        _glContext = SDL.SDL_GL_CreateContext(_windowHandle);
        if(_glContext == IntPtr.Zero)
        {
            throw new Exception($"Failed to create OpenGL context: {SDL.SDL_GetError()}");
        }

        // Make the OpenGL context current
        int makeCurrentResult = SDL.SDL_GL_MakeCurrent(_windowHandle, _glContext);
        if (makeCurrentResult < 0)
        {
            throw new Exception($"GL_MakeCurrent failed: {SDL.SDL_GetError()}");
        }

        // After context creation
        string version = GL.GetString(GL.Enum.GL_VERSION);
        Console.WriteLine($"OpenGL Version: {version}");

        // Load OpenGL functions
        GL.LoadFunctions();

        _lineRenderer = new LineRenderer();
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

    public IntPtr Get()
    {
        return _glContext;
    }

    public void CreateTexture(RawBmp rawBmp, ref IntPtr texturePtr)
    {
        IntPtr pixels;
        unsafe
        {
            fixed (byte* ptr = rawBmp.ByteArray)
            {
                pixels = new IntPtr(ptr);
            }
        }

        // Delete any existing texture
        DeleteTexture((uint)texturePtr);

        // Create the surface
        IntPtr sdlSurface = SDL.SDL_CreateRGBSurfaceFrom(
                                pixels,
                                rawBmp.Width,
                                rawBmp.Height,
                                rawBmp.Depth * 8,
                                rawBmp.Stride,
                                0xff000000,
                                0x00ff0000,
                                0x0000ff00,
                                0x000000ff);


        texturePtr = (IntPtr)LoadTexture(sdlSurface);
        SDL.SDL_FreeSurface(sdlSurface);
    }

    public uint LoadTexture(IntPtr surfacePtr)
    {
        // Convert the SDL_Surface pointer to a managed structure
        var surface = Marshal.PtrToStructure<SDL.SDL_Surface>(surfacePtr);
        var format = Marshal.PtrToStructure<SDL.SDL_PixelFormat>(surface.format);

        // Check if pixels exist
        if (surface.pixels == IntPtr.Zero)
        {
            throw new Exception("Surface contains no pixel data");
        }

        // Check dimensions
        if (surface.w <= 0 || surface.h <= 0)
        {
            throw new Exception($"Invalid texture dimensions: {surface.w}x{surface.h}");
        }

        // Generate texture ID
        uint textureId;
        GL.GenTextures(1, out textureId);

        // Check if texture generation failed
        if (textureId == 0)
        {
            throw new Exception("Failed to generate texture ID");
        }

        CheckGLError("LoadTexture (GenTextures)");

        GL.BindTexture(GL.Enum.GL_TEXTURE_2D, textureId);
        CheckGLError("LoadTexture (BindTexture)");

        // Set texture parameters
        GL.TexParameteri(GL.Enum.GL_TEXTURE_2D, GL.Enum.GL_TEXTURE_MIN_FILTER, (int)GL.Enum.GL_LINEAR);
        GL.TexParameteri(GL.Enum.GL_TEXTURE_2D, GL.Enum.GL_TEXTURE_MAG_FILTER, (int)GL.Enum.GL_LINEAR);
        GL.TexParameteri(GL.Enum.GL_TEXTURE_2D, GL.Enum.GL_TEXTURE_WRAP_S, (int)GL.Enum.GL_CLAMP_TO_EDGE);
        GL.TexParameteri(GL.Enum.GL_TEXTURE_2D, GL.Enum.GL_TEXTURE_WRAP_T, (int)GL.Enum.GL_CLAMP_TO_EDGE);

        uint glFormat;
        switch (format.BitsPerPixel)
        {
            case 32:
                glFormat = (uint)GL.Enum.GL_RGBA;
                break;
            case 24:
                glFormat = (uint)GL.Enum.GL_RGB;
                break;
            default:
                throw new Exception($"Unsupported bits per pixel: {format.BitsPerPixel}");
        }

        // Upload texture data to GPU
        try
        {
            GL.TexImage2D(
                GL.Enum.GL_TEXTURE_2D,
                0,
                (int)glFormat,
                surface.w,
                surface.h,
                0,
                (GL.Enum)glFormat,
                GL.Enum.GL_UNSIGNED_BYTE,
                surface.pixels
            );
        }
        catch (Exception e)
        {
            // Clean up on failure
            GL.DeleteTextures(1, ref textureId);
            throw new Exception($"Failed to create texture: {e.Message}", e);
        }

        // Generate mipmaps (optional, not implemented yet)
        //GL.GenerateMipmap(GL.Enum.GL_TEXTURE_2D);

        // Check for errors
        CheckGLError("LoadTexture (TexImage2D)");

        // Keep track of the loaded textures, if name is already used replace the texture
        if(!_textures.Contains(textureId))
        {
            _textures.Add(textureId);
        }

        return textureId;
    }

    public void DeleteTexture(uint textureId)
    {
        if(textureId == IntPtr.Zero) return;

        GL.DeleteTextures(1, ref textureId);

        // Remove textureId
        _textures.Remove(textureId);
    }

    public uint CreateDefaultFontTexture(int width, int height, IntPtr pixels)
    {
        // Create OpenGL texture
        GL.GenTextures(1, out uint fontTextureID);
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
                    GL.BindTexture(GL.Enum.GL_TEXTURE_2D, (uint)pcmd.TextureId);
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
                    GL.BindTexture(GL.Enum.GL_TEXTURE_2D, (uint)pcmd.TextureId);

                    //CheckGLError("RenderImGui");
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
        GL.BindTexture(GL.Enum.GL_TEXTURE_2D, (uint)lastTexture);
        GL.MatrixMode(GL.Enum.GL_MODELVIEW);
        GL.PopMatrix();
        GL.MatrixMode(GL.Enum.GL_PROJECTION);
        GL.PopMatrix();
        GL.PopAttrib();
        GL.Viewport(lastViewport.X, lastViewport.Y, lastViewport.Z, lastViewport.W);
        GL.Scissor(lastScissorBox.X, lastScissorBox.Y, lastScissorBox.Z, lastScissorBox.W);
    }

    public void RenderLine(Shape[] shapes, Camera camera)
    {
        // Save the current OpenGL context state
        _previousContext = SDL.SDL_GL_GetCurrentContext();

        // Make the OpenGL context current
        SDL.SDL_GL_MakeCurrent(_windowHandle, _glContext);

        // Render the lines
        _lineRenderer.Draw(shapes, camera);

        // Restore the previous context
        SDL.SDL_GL_MakeCurrent(_windowHandle, _previousContext);
    }

    public void Dispose()
    {
        if(_textures.Count > 0)
        {
            foreach(var texture in _textures)
            {
                uint textureId = texture;
                GL.DeleteTextures(1, ref textureId);
            }
            _textures.Clear();
        }

        if(_glContext != IntPtr.Zero)
        {
            SDL.SDL_GL_DeleteContext(_glContext);
            _glContext = IntPtr.Zero;
        }
    }

    private void CheckGLError(string operation)
    {
        uint error = GL.GetError();
        if (error != 0)
        {
            string errorMsg = error switch
            {
                0x0500 => "GL_INVALID_ENUM",
                0x0501 => "GL_INVALID_VALUE",
                0x0502 => "GL_INVALID_OPERATION",
                0x0505 => "GL_OUT_OF_MEMORY",
                _ => $"Unknown error: 0x{error:X4}"
            };
            throw new Exception($"OpenGL error during {operation}: {errorMsg}");
        }
    }
}