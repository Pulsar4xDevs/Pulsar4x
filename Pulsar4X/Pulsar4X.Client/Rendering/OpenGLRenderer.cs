using System;
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

    public void Dispose()
    {
        if(_glContext != IntPtr.Zero)
        {
            SDL.SDL_GL_DeleteContext(_glContext);
            _glContext = IntPtr.Zero;
        }
    }
}