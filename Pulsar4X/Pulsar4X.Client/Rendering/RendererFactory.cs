// using System;

// namespace Pulsar4X.Client.Rendering;

// public static class RendererFactory
// {
//     public static IRenderer CreateRenderer(RendererType rendererType)
//     {
//         return rendererType switch
//         {
//             RendererType.OpenGL => new OpenGLRenderer(),
//             _ => throw new ArgumentException("Unsupported renderer backend")
//         };
//     }
// }