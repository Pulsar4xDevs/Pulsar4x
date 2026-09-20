using System;
using System.Numerics;
using ImGuiNET;
using SDL3;

namespace Pulsar4X.Client;

/// <summary>
/// Implementation of ImGui for SDL3 Renderer backend.
/// https://github.com/ocornut/imgui/blob/master/backends/imgui_impl_sdlrenderer3.h
/// </summary>
public class ImGuiSDL3Renderer : IDisposable
{
    struct BackupSDLRendererState
    {
        public SDL.Rect Viewport;
        public bool ViewportEnabled;
        public bool ClipEnabled;
        public SDL.Rect ClipRect;
    }

    public SDL.Rect DefaultClipRect = new();
    public readonly nint Renderer;
    private nint _fontTexture = IntPtr.Zero;

    public ImGuiSDL3Renderer(nint renderer)
    {
        Renderer = renderer;

        ImGuiIOPtr io = ImGui.GetIO();
        io.BackendFlags |= ImGuiBackendFlags.RendererHasVtxOffset;
        io.BackendFlags |= ImGuiBackendFlags.RendererHasTextures;

        // Mark that we support the new texture system
        io.Fonts.RendererHasTextures = true;
    }

    public void Dispose()
    {
        DestroyDeviceObjects();
    }

    public unsafe void NewFrame()
    {
        // Handle texture updates for the new ImGui 1.92+ texture system
        ImGuiIOPtr io = ImGui.GetIO();
        var texList = io.Fonts.TexList;

        for (int i = 0; i < texList.Size; i++)
        {
            var texData = texList[i];
            var status = texData.Status;

            if (status == ImTextureStatus.WantCreate || status == ImTextureStatus.WantUpdates)
            {
                // Create or update the texture
                int width = texData.Width;
                int height = texData.Height;
                byte* pixels = (byte*)texData.Pixels;

                if (width > 0 && height > 0 && pixels != null)
                {
                    // Create SDL texture
                    var surface = SDL.CreateSurfaceFrom(width, height, SDL.PixelFormat.RGBA8888, (IntPtr)pixels, width * 4);
                    if (surface != IntPtr.Zero)
                    {
                        var texture = SDL.CreateTextureFromSurface(Renderer, surface);
                        if (texture != IntPtr.Zero)
                        {
                            SDL.UpdateTexture(texture, IntPtr.Zero, (IntPtr)pixels, width * 4);
                            SDL.SetTextureBlendMode(texture, SDL.BlendMode.Blend);
                            SDL.SetTextureScaleMode(texture, SDL.ScaleMode.Nearest);

                            // Store the texture ID
                            texData.SetTexID(texture);
                            texData.SetStatus(ImTextureStatus.OK);

                            // Track font texture for cleanup
                            if (i == 0)
                                _fontTexture = texture;
                        }
                        SDL.DestroySurface(surface);
                    }
                }
            }
            else if (status == ImTextureStatus.WantDestroy)
            {
                var texId = texData.TexID;
                if (texId != IntPtr.Zero)
                {
                    SDL.DestroyTexture(texId);
                    texData.SetTexID(IntPtr.Zero);
                }
                texData.SetStatus(ImTextureStatus.Destroyed);
            }
        }
    }

    public void RenderDrawData(ImDrawDataPtr drawData)
    {
        // Skip if no data to render
        unsafe
        {
            if (drawData.NativePtr == null || drawData.CmdListsCount == 0)
                return;
        }

        // Get display size & framebuffer scale
        Vector2 renderScale = new(
            drawData.FramebufferScale.X,
            drawData.FramebufferScale.Y
        );

        int fbWidth = (int)(drawData.DisplaySize.X * renderScale.X);
        int fbHeight = (int)(drawData.DisplaySize.Y * renderScale.Y);
        if (fbWidth <= 0 || fbHeight <= 0)
            return;

        // Backup SDL renderer state
        BackupSDLRendererState oldState = BackupRendererState();

        // Set up render state
        SetupRenderState();

        // Set render state in platform IO
        ImGuiPlatformIOPtr platformIo = ImGui.GetPlatformIO();
        platformIo.Renderer_RenderState = Renderer;

        Vector2 clipOffset = drawData.DisplayPos;

        // Render command lists
        for (int n = 0; n < drawData.CmdListsCount; n++)
        {
            ImDrawListPtr cmdList = drawData.CmdLists[n];

            for (int cmdIndex = 0; cmdIndex < cmdList.CmdBuffer.Size; cmdIndex++)
            {
                ImDrawCmdPtr cmd = cmdList.CmdBuffer[cmdIndex];

                if (cmd.UserCallback != IntPtr.Zero)
                {
                    // User callback not implemented
                    continue;
                }

                // Apply clipping rectangle
                Vector4 clipRect = cmd.ClipRect;
                SDL.Rect r = CalculateClipRect(clipRect, clipOffset, renderScale, fbWidth, fbHeight);
                SDL.SetRenderClipRect(Renderer, r);

                // Get texture
                nint texId = cmd.GetTexID();

                // Convert ImGui vertices to SDL vertices
                if (!RenderDrawCommand(cmdList, cmd, texId, renderScale))
                {
                    Console.WriteLine($"Failed to render ImGui draw command: {SDL.GetError()}");
                }
            }
        }

        // Reset render state
        platformIo.Renderer_RenderState = IntPtr.Zero;

        // Restore renderer state
        RestoreRendererState(oldState);
    }

    private unsafe bool RenderDrawCommand(ImDrawListPtr drawList, ImDrawCmdPtr cmd, nint texId, Vector2 scale)
    {
        // Get indices and vertices for this command
        int indexOffset = (int)cmd.IdxOffset;
        int vertexOffset = (int)cmd.VtxOffset;
        int elemCount = (int)cmd.ElemCount;

        // Create SDL vertices just for the vertices used by this command
        // Determine the vertex range by looking at the indices
        ushort minVertexIdx = ushort.MaxValue;
        ushort maxVertexIdx = 0;

        for (int i = 0; i < elemCount; i++)
        {
            ushort idx = drawList.IdxBuffer[indexOffset + i];
            minVertexIdx = Math.Min(minVertexIdx, idx);
            maxVertexIdx = Math.Max(maxVertexIdx, idx);
        }

        // Adjust for the vertex offset
        minVertexIdx = (ushort)(minVertexIdx + vertexOffset);
        maxVertexIdx = (ushort)(maxVertexIdx + vertexOffset);

        // Calculate the number of vertices we need
        int numVertices = maxVertexIdx - minVertexIdx + 1;

        // Create arrays for the vertices and indices we're going to use
        SDL.Vertex[] vertices = new SDL.Vertex[numVertices];
        int[] indices = new int[elemCount];

        // Convert only the vertices we need
        for (int i = 0; i < numVertices; i++)
        {
            int vertIdx = minVertexIdx + i;
            ImDrawVertPtr srcVert = drawList.VtxBuffer[vertIdx];

            // Convert from ImGui ABGR color to SDL RGBA color
            uint col = srcVert.col;
            byte r = (byte)((col >> 0) & 0xFF);  // Extract R from the least significant byte
            byte g = (byte)((col >> 8) & 0xFF);  // Extract G
            byte b = (byte)((col >> 16) & 0xFF); // Extract B
            byte a = (byte)((col >> 24) & 0xFF); // Extract A from the most significant byte

            vertices[i] = new SDL.Vertex
            {
                Position = new SDL.FPoint { X = srcVert.pos.X, Y = srcVert.pos.Y },
                Color = new SDL.FColor { R = r / 255f, G = g / 255f, B = b / 255f, A = a / 255f },
                TexCoord = new SDL.FPoint { X = srcVert.uv.X, Y = srcVert.uv.Y }
            };
        }

        // Adjust indices to be relative to our new vertex array
        for (int i = 0; i < elemCount; i++)
        {
            ushort originalIdx = drawList.IdxBuffer[indexOffset + i];
            indices[i] = (ushort)(originalIdx - (minVertexIdx - vertexOffset));
        }

        // Call your SDL.RenderGeometry wrapper with the managed arrays
        return SDL.RenderGeometry(
            Renderer,
            texId,
            vertices,
            numVertices,
            indices,
            elemCount
        );
    }

    private void SetupRenderState()
    {
        SDL.SetRenderViewport(Renderer, 0);
        SDL.SetRenderClipRect(Renderer, IntPtr.Zero);
        SDL.SetRenderDrawBlendMode(Renderer, SDL.BlendMode.Blend);
    }

    private BackupSDLRendererState BackupRendererState()
    {
        BackupSDLRendererState state = new();
        state.ViewportEnabled = SDL.RenderViewportSet(Renderer);
        state.ClipEnabled = SDL.RenderClipEnabled(Renderer);
        SDL.GetRenderViewport(Renderer, out state.Viewport);
        SDL.GetRenderClipRect(Renderer, out state.ClipRect);
        return state;
    }

    private void RestoreRendererState(BackupSDLRendererState state)
    {
        if(state.ViewportEnabled)
            SDL.SetRenderViewport(Renderer, state.Viewport);
        else
            SDL.SetRenderViewport(Renderer, IntPtr.Zero);
        if(state.ClipEnabled)
            SDL.SetRenderClipRect(Renderer, state.ClipRect);
        else
            SDL.SetRenderClipRect(Renderer, IntPtr.Zero);
    }

    private SDL.Rect CalculateClipRect(Vector4 clipRect, Vector2 clipOffset, Vector2 scale, int fbWidth, int fbHeight)
    {
        Vector2 clipMin = new((clipRect.X - clipOffset.X) * scale.X, (clipRect.Y - clipOffset.Y) * scale.Y);
        Vector2 clipMax = new((clipRect.Z - clipOffset.X) * scale.X, (clipRect.W - clipOffset.Y) * scale.Y);

        // Clamp to framebuffer bounds
        clipMin.X = Math.Max(0, clipMin.X);
        clipMin.Y = Math.Max(0, clipMin.Y);
        clipMax.X = Math.Min(fbWidth, clipMax.X);
        clipMax.Y = Math.Min(fbHeight, clipMax.Y);

        SDL.Rect r = new()
        {
            X = (int)clipMin.X,
            Y = (int)clipMin.Y,
            W = (int)(clipMax.X - clipMin.X),
            H = (int)(clipMax.Y - clipMin.Y)
        };

        return r;
    }

    private bool CreateDeviceObjects()
    {
        //return CreateFontsTexture();
        return true;
    }

    private void DestroyDeviceObjects()
    {
        DestroyFontsTexture();
    }

    private void DestroyFontsTexture()
    {
        if(_fontTexture != IntPtr.Zero)
        {
            SDL.DestroyTexture(_fontTexture);
            _fontTexture = IntPtr.Zero;
        }
    }
}