using System;
using ImGuiNET;
using Pulsar4X.DataStructures;
using Pulsar4X.SDL2UI;

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
    void SetAttributes();
    void Initialize(IntPtr windowHandle);
    void BeginFrame();
    void EndFrame();
    void Clear(float r, float g, float b, float a);

    void CreateTexture(RawBmp rawBmp, ref IntPtr texturePtr, string name);
    uint LoadTexture(IntPtr surfacePtr, string name);
    void DeleteTexture(string name);
    void DeleteTexture(uint textureId);

    IntPtr Get();

    void RenderLine(Shape[] shapes, Camera camera);

    uint CreateDefaultFontTexture(int width, int height, IntPtr pixels);
    void RenderImGui(ImDrawDataPtr drawData, int displayWidth, int displayHeight);
}