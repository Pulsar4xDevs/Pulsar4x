using System;

namespace Pulsar4X.Client.Rendering;

public enum RendererType
{
    OpenGL,
    // TODO: someone can write these renderers if they desire :D
    //Vulkan,
    //DirectX
}

public interface IRenderer : IDisposable
{
    void Initialize(IntPtr windowHandle);
    void BeginFrame();
    void EndFrame();
    void Clear(float r, float g, float b, float a);

    int CreateDefaultFontTexture(int width, int height, IntPtr pixels);
}