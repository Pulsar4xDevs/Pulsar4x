using System;
using System.Text;
using ImGuiSDL2CS;
using Pulsar4X.SDL2UI;
using SDL2;

namespace Pulsar4X.Client.Rendering;

public class LineRenderer : IDisposable
{
    private readonly uint _shaderProgram;
    private readonly int _colorLocation;
    private readonly int _transformLocation;

    // Vertex shader
    private const string VertexShaderSource = @"
        #version 330 core
        layout(location = 0) in vec2 aPos;
        uniform mat4 transform;

        void main()
        {
            gl_Position = transform * vec4(aPos, 0.0, 1.0);
        }";

    // Fragment shader
    private const string FragmentShaderSource = @"
        #version 330 core
        uniform vec4 color;
        out vec4 FragColor;
        void main()
        {
            FragColor = color;
        }";

    public LineRenderer()
    {
        IntPtr glContext = SDL.SDL_GL_GetCurrentContext();
        Console.WriteLine($"GL Context: {glContext != IntPtr.Zero}");

        // Create and compile shaders
        uint vertexShader = CompileShader(GL.Enum.GL_VERTEX_SHADER, VertexShaderSource);
        uint fragmentShader = CompileShader(GL.Enum.GL_FRAGMENT_SHADER, FragmentShaderSource);

        // Create and link program
        _shaderProgram = GL.CreateProgram();

        Console.WriteLine($"Shader program ID: {_shaderProgram}");

        GL.AttachShader(_shaderProgram, vertexShader);
        GL.AttachShader(_shaderProgram, fragmentShader);
        GL.LinkProgram(_shaderProgram);

        // After program linking, check if it was successful
        int linkStatus;
        GL.GetProgramiv(_shaderProgram, GL.Enum.GL_LINK_STATUS, out linkStatus);
        Console.WriteLine($"Program link status: {linkStatus}");

        if (linkStatus == 0)
        {
            // Get the error log
            StringBuilder infoLog = new StringBuilder(1024);
            int length;
            GL.GetShaderInfoLog(_shaderProgram, 1024, out length, infoLog);
            Console.WriteLine($"Program linking failed: {infoLog}");
        }

        // Add validation check
        GL.ValidateProgram(_shaderProgram);
        int validateStatus;
        GL.GetProgramiv(_shaderProgram, GL.Enum.GL_VALIDATE_STATUS, out validateStatus);
        Console.WriteLine($"Program validate status: {validateStatus}");

        if (validateStatus == 0)
        {
            StringBuilder infoLog = new StringBuilder(1024);
            int length;
            GL.GetProgramInfoLog(_shaderProgram, 1024, out length, infoLog);
            Console.WriteLine($"Program validation failed: {infoLog}");
        }

        // Get uniform location
        _colorLocation = GL.GetUniformLocation(_shaderProgram, "color");
        Console.WriteLine($"Color uniform location: {_colorLocation}");

        // Print active uniforms
        int numUniforms;
        GL.GetProgramiv(_shaderProgram, GL.Enum.GL_ACTIVE_UNIFORMS, out numUniforms);
        Console.WriteLine($"Number of active uniforms: {numUniforms}");

        // For each uniform, get its name
        for (int i = 0; i < numUniforms; i++)
        {
            StringBuilder name = new StringBuilder(128);
            int length, size;
            uint type;
            GL.GetActiveUniform(_shaderProgram, (uint)i, 128, out length, out size, out type, name);
            Console.WriteLine($"Uniform {i}: {name} (type: 0x{type:X})");
        }

        // Get uniform locations
        //_colorLocation = GL.GetUniformLocation(_shaderProgram, "color");
        _transformLocation = GL.GetUniformLocation(_shaderProgram, "transform");

        // Clean up shaders after linking
        if (GL.DeleteShader == null)
        {
            Console.WriteLine("DeleteShader function not loaded!");
            return;
        }
        GL.DeleteShader(vertexShader);
        GL.DeleteShader(fragmentShader);
    }

    private uint CompileShader(GL.Enum type, string source)
    {
        // Debug OpenGL state
        uint error = GL.GetError();
        Console.WriteLine($"Initial GL error state: 0x{error:X}");

        // First, let's print the actual values we're passing:
        Console.WriteLine($"SHADER type value: 0x{type:X}");

        uint shader = GL.CreateShader((uint)type);

        error = GL.GetError();
        Console.WriteLine($"After CreateShader - GL error: 0x{error:X}, shader ID: {shader}");

        // If we failed to create the shader, let's check if OpenGL is properly initialized
        if (shader == 0)
        {
            // Get OpenGL version string
            string version = GL.GetString(GL.Enum.GL_VERSION);
            Console.WriteLine($"OpenGL Version: {version}");

            // Get OpenGL vendor
            string vendor = GL.GetString(GL.Enum.GL_VENDOR);
            Console.WriteLine($"OpenGL Vendor: {vendor}");

            // Print the actual enum value being passed
            Console.WriteLine($"Shader type value being passed: 0x{(uint)type:X}");
            return 0;
        }

        GL.ShaderSource(shader, 1, new[] { source }, null);
        GL.CompileShader(shader);

        // Check compilation
        int success;
        GL.GetShaderiv(shader, GL.Enum.GL_COMPILE_STATUS, out success);
        if (success == 0)
        {
            StringBuilder infoLog = new StringBuilder(1024);
            int length;
            GL.GetShaderInfoLog(shader, 1024, out length, infoLog);
            Console.WriteLine($"Shader compilation failed: {infoLog}");
            return 0;
        }

        return shader;
    }

    public void Draw(Shape[] shapes, Camera camera)
    {
        if (shapes == null || shapes.Length == 0)
            return;

        int previousProgram;
        GL.GetIntegerv(GL.Enum.GL_CURRENT_PROGRAM, out previousProgram);

        uint error = GL.GetError();
        //Console.WriteLine($"Error before UseProgram: 0x{error:X}");

        GL.UseProgram(_shaderProgram);

        error = GL.GetError();
        //Console.WriteLine($"Error after UseProgram: 0x{error:X}");
        //Console.WriteLine($"Using program: {_shaderProgram}");

        // Set camera transform
        //float[] transform = camera.GetTransformMatrix();
        //GL.UniformMatrix4fv(_transformLocation, 1, false, transform);

        foreach (var shape in shapes)
        {
            // Debug color values
            // Console.WriteLine($"Raw color: R={shape.Color.r}, G={shape.Color.g}, B={shape.Color.b}, A={shape.Color.a}");
            // Console.WriteLine($"Normalized color: R={shape.Color.r/255f:F3}, G={shape.Color.g/255f:F3}, B={shape.Color.b/255f:F3}, A={shape.Color.a/255f:F3}");
            // Console.WriteLine($"Color uniform location: {_colorLocation}");

            // Check for GL errors before setting uniform
            error = GL.GetError();
            //Console.WriteLine($"GL error before color uniform: 0x{error:X}");

            GL.Uniform4f(_colorLocation,
                shape.Color.r / 255f,
                shape.Color.g / 255f,
                shape.Color.b / 255f,
                shape.Color.a / 255f);

            error = GL.GetError();
            //Console.WriteLine($"GL error after color uniform: 0x{error:X}");

            // Convert points to float array
            var vertices = new float[shape.Points.Length * 2];
            for (int i = 0; i < shape.Points.Length; i++)
            {
                vertices[i * 2] = Math.Clamp((float)shape.Points[i].X, float.MinValue, float.MaxValue);
                vertices[i * 2 + 1] = Math.Clamp((float)shape.Points[i].Y, float.MinValue, float.MaxValue);
            }

            // Create and bind vertex buffer
            uint vbo;
            GL.GenBuffers(1, out vbo);
            GL.BindBuffer((uint)GL.Enum.GL_ARRAY_BUFFER, vbo);
            GL.BufferData((uint)GL.Enum.GL_ARRAY_BUFFER, vertices.Length * sizeof(float), vertices, (uint)GL.Enum.GL_STATIC_DRAW);

            // Set up vertex attributes
            GL.VertexAttribPointer(0, 2, (uint)GL.Enum.GL_FLOAT, false, 2 * sizeof(float), IntPtr.Zero);
            GL.EnableVertexAttribArray(0);

            // Draw lines
            GL.DrawArrays((uint)GL.Enum.GL_LINE_STRIP, 0, shape.Points.Length);

            // Clean up
            GL.DeleteBuffers(1, ref vbo);
        }

        // Restore previous state
        GL.UseProgram(previousProgram == 0 ? 0 : (uint)previousProgram);
        GL.DisableVertexAttribArray(0);
        GL.BindBuffer((uint)GL.Enum.GL_ARRAY_BUFFER, 0);
    }

    public void Dispose()
    {
        GL.DeleteProgram(_shaderProgram);
    }
}