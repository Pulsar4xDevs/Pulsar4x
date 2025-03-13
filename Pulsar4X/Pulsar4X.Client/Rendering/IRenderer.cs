// using System;
// using ImGuiNET;
// using Pulsar4X.DataStructures;

// namespace Pulsar4X.Client.Rendering;

// public enum RendererType
// {
//     OpenGL,
//     // TODO: someone can write these renderers if they desire :D
//     //Vulkan,
//     //DirectX
// }

// public interface IRenderer : IDisposable
// {
//     void SetAttributes();
//     void Initialize(IntPtr windowHandle);
//     void BeginFrame();
//     void EndFrame();
//     void Clear(float r, float g, float b, float a);

//     void CreateTexture(ref IntPtr texture, int width, int height, IntPtr pixels, PixelFormat pixelFormat = PixelFormat.RGBA8888, TextureFilter textureFilter = TextureFilter.Linear);
//     void CreateTexture(RawBmp rawBmp, ref IntPtr texturePtr, PixelFormat pixelFormat = PixelFormat.RGBA8888, TextureFilter textureFilter = TextureFilter.Linear);
//     uint CreateTexture(IntPtr surfacePtr, TextureFilter textureFilter = TextureFilter.Linear);
//     void DeleteTexture(uint textureId);
//     void UpdateTexture(ref IntPtr texture, int width, int height, IntPtr pixels);
//     (int width, int height) GetTextureDimensions(IntPtr texture);

//     IntPtr Get();

//     void RenderLine(Shape[] shapes, Camera camera);

//     uint CreateDefaultFontTexture(int width, int height, IntPtr pixels);
//     void RenderImGui(ImDrawDataPtr drawData, int displayWidth, int displayHeight);
// }