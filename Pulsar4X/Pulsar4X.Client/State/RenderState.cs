using SDL3;

namespace Pulsar4X.Client;

public class RenderState
{
    public SDL.BlendMode BlendMode { get; set; } = SDL.BlendMode.Blend;
    public byte Red { get; set; }
    public byte Green { get; set; }
    public byte Blue { get; set; }
    public byte Alpha { get; set; }

}