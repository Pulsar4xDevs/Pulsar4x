using System;
using System.Text;
using ImGuiSDL2CS;
using Pulsar4X.SDL2UI;

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
            vec4 pos = transform * vec4(aPos.xy, 0.0, 1.0);
            gl_Position = pos;
        }";

    // Fragment shader
    private const string FragmentShaderSource = @"
        #version 330 core
        layout (location = 0) out vec4 fragColor;
        uniform vec4 color;
        void main()
        {
            fragColor = color;
        }";

    public LineRenderer()
    {
        // Create and compile shaders
        uint vertexShader = CompileShader(GL.Enum.GL_VERTEX_SHADER, VertexShaderSource);
        uint fragmentShader = CompileShader(GL.Enum.GL_FRAGMENT_SHADER, FragmentShaderSource);

        Console.WriteLine($"Created shaders - vertex: {vertexShader}, fragment: {fragmentShader}");

        // Create program
        _shaderProgram = GL.CreateProgram();
        Console.WriteLine($"Created program: {_shaderProgram}");

        uint error = GL.GetError();
        Console.WriteLine($"Error after program creation: 0x{error:X}");

        GL.AttachShader(_shaderProgram, vertexShader);
        error = GL.GetError();
        Console.WriteLine($"Error after vertex attach: 0x{error:X}");

        GL.AttachShader(_shaderProgram, fragmentShader);
        error = GL.GetError();
        Console.WriteLine($"Error after fragment attach: 0x{error:X}");

        GL.LinkProgram(_shaderProgram);
        error = GL.GetError();
        Console.WriteLine($"Error after link: 0x{error:X}");

        // Check link status
        int linkStatus;
        GL.GetProgramiv(_shaderProgram, GL.Enum.GL_LINK_STATUS, out linkStatus);
        Console.WriteLine($"Program link status: {linkStatus}");

        // Try to use immediately
        GL.UseProgram(_shaderProgram);
        error = GL.GetError();
        Console.WriteLine($"Error after immediate use: 0x{error:X}");
        GL.UseProgram(0);

        // // Print active uniforms
        // int numUniforms;
        // GL.GetProgramiv(_shaderProgram, GL.Enum.GL_ACTIVE_UNIFORMS, out numUniforms);
        // Console.WriteLine($"Number of active uniforms: {numUniforms}");

        // // For each uniform, get its name
        // for (int i = 0; i < numUniforms; i++)
        // {
        //     StringBuilder name = new StringBuilder(128);
        //     int length, size;
        //     uint type;
        //     GL.GetActiveUniform(_shaderProgram, (uint)i, 128, out length, out size, out type, name);
        //     Console.WriteLine($"Uniform {i}: {name} (type: 0x{type:X})");
        // }

        // Get uniform locations
        _colorLocation = GL.GetUniformLocation(_shaderProgram, "color");
        _transformLocation = GL.GetUniformLocation(_shaderProgram, "transform");

        // Clean up shaders
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

    public void Draw(Shape[] shapes, Camera2 camera)
    {
        if (shapes == null || shapes.Length == 0)
            return;

        GL.Viewport(0, 0, (int)camera.ScreenSize.X, (int)camera.ScreenSize.Y);

        // Create and bind vertex buffer
        uint vbo;
        GL.GenBuffers(1, out vbo);
        GL.BindBuffer((uint)GL.Enum.GL_ARRAY_BUFFER, vbo);

        // Set up vertex attributes
        GL.VertexAttribPointer(0, 2, (uint)GL.Enum.GL_FLOAT, false, 2 * sizeof(float), IntPtr.Zero);
        GL.EnableVertexAttribArray(0);

        int previousProgram;
        GL.GetIntegerv(GL.Enum.GL_CURRENT_PROGRAM, out previousProgram);

        // Check if blending is enabled
        int blendEnabled;
        GL.GetIntegerv(GL.Enum.GL_BLEND, out blendEnabled);

        // Force color mask to all enabled
        GL.ColorMask(true, true, true, true);
        GL.Disable(GL.Enum.GL_COLOR_MATERIAL);

        // Enable blending if not enabled
        if (blendEnabled == 0)
        {
            GL.Enable(GL.Enum.GL_BLEND);
            GL.BlendFunc(GL.Enum.GL_SRC_ALPHA, GL.Enum.GL_ONE_MINUS_SRC_ALPHA);
        }

        GL.UseProgram(_shaderProgram);

        // Set camera transform
        GL.UniformMatrix4fv(_transformLocation, 1, false, camera.GetTransformMatrix());

        foreach (var shape in shapes)
        {
            GL.Uniform4f(_colorLocation,
                shape.Color.r / 255f,
                shape.Color.g / 255f,
                shape.Color.b / 255f,
                shape.Color.a / 255f);

            // Convert points to float array
            var vertices = new float[shape.Points.Length * 2];
            for (int i = 0; i < shape.Points.Length; i++)
            {
                vertices[i * 2] = Math.Clamp((float)shape.Points[i].X, float.MinValue, float.MaxValue);
                vertices[i * 2 + 1] = Math.Clamp((float)shape.Points[i].Y, float.MinValue, float.MaxValue);
            }

            GL.BufferData((uint)GL.Enum.GL_ARRAY_BUFFER, vertices.Length * sizeof(float), vertices, (uint)GL.Enum.GL_STATIC_DRAW);

            // Draw lines
            GL.DrawArrays((uint)GL.Enum.GL_LINE_STRIP, 0, shape.Points.Length);
        }

        // Restore previous state
        GL.UseProgram(previousProgram == 0 ? 0 : (uint)previousProgram);
        GL.DisableVertexAttribArray(0);
        GL.BindBuffer((uint)GL.Enum.GL_ARRAY_BUFFER, 0);
        // Restore blend state if we changed it
        if (blendEnabled == 0)
        {
            GL.Disable(GL.Enum.GL_BLEND);
        }
        // Clean up
        GL.DeleteBuffers(1, ref vbo);
    }

    public void Dispose()
    {
        GL.DeleteProgram(_shaderProgram);
    }

    private void CheckProgramStatus(string location)
    {
        int linkStatus;
        GL.GetProgramiv(_shaderProgram, GL.Enum.GL_LINK_STATUS, out linkStatus);

        int validateStatus;
        GL.GetProgramiv(_shaderProgram, GL.Enum.GL_VALIDATE_STATUS, out validateStatus);

        Console.WriteLine($"Program status at {location}:");
        Console.WriteLine($"  Program ID: {_shaderProgram}");
        Console.WriteLine($"  Link status: {linkStatus}");
        Console.WriteLine($"  Validate status: {validateStatus}");

        // Check if program exists
        GL.GetProgramiv(_shaderProgram, GL.Enum.GL_ACTIVE_ATTRIBUTES, out int numAttribs);
        uint error = GL.GetError();
        Console.WriteLine($"  Get attributes error: 0x{error:X}");
    }

    public float[] CreateOrthographicProjection(float width, float height)
    {
        // Create orthographic projection that maps (0,0) to top-left and (width,height) to bottom-right
        float[] matrix = new float[16];

        // Scale
        matrix[0] = 2.0f / width;   // X scale
        matrix[5] = 2.0f / height;  // Y scale
        matrix[10] = -2.0f;         // Z scale (not really used in 2D)
        matrix[15] = 1.0f;          // W component

        // Translation
        matrix[12] = -1.0f;         // X translation
        matrix[13] = -1.0f;         // Y translation
        matrix[14] = -1.0f;         // Z translation

        return matrix;
    }
}