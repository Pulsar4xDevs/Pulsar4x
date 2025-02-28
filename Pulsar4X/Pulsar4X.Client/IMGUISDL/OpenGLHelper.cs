using SDL3;
using System;
using System.Runtime.InteropServices;
using System.Text;

namespace ImGuiSDL2CS
{
    // Even smaller than MiniTK, only offering the bare minimum required for ImGuiSDL2CS.
    public unsafe static class GL {

        public static void LoadFunctions()
        {
            // Explicitly load modern functions we need
            CreateShader = _<glCreateShader>();
            ShaderSource = _<glShaderSource>();
            CompileShader = _<glCompileShader>();
            GetShaderiv = _<glGetShaderiv>();
            GetShaderInfoLog = _<glGetShaderInfoLog>();
            CreateProgram = _<glCreateProgram>();
            AttachShader = _<glAttachShader>();
            LinkProgram = _<glLinkProgram>();
            UseProgram = _<glUseProgram>();
            GetUniformLocation = _<glGetUniformLocation>();
            DeleteShader = _<glDeleteShader>();
            Uniform4f = _<glUniform4f>();
            GenBuffers = _<glGenBuffers>();
            BindBuffer = _<glBindBuffer>();
            BufferData = _<glBufferData>();
            VertexAttribPointer = _<glVertexAttribPointer>();
            EnableVertexAttribArray = _<glEnableVertexAttribArray>();
            DrawArrays = _<glDrawArrays>();
            DeleteBuffers = _<glDeleteBuffers>();
            DisableVertexAttribArray = _<glDisableVertexAttribArray>();
            GetProgramiv = _<glGetProgramiv>();
            GetActiveUniform = _<glGetActiveUniform>();
            ValidateProgram = _<glValidateProgram>();
            GetProgramInfoLog = _<glGetProgramInfoLog>();
            UniformMatrix4fv = _<glUniformMatrix4fv>();
            ColorMask = _<glColorMask>();
            GetFloatv = _<glGetFloatv>();

            // Print any errors
            uint error = GetError();
            if (error != 0)
            {
                Console.WriteLine($"GL error during function loading: 0x{error:X}");
            }

            // Print OpenGL version
            string version = GetString(Enum.GL_VERSION);
            Console.WriteLine($"OpenGL Version: {version}");
        }

        private static T _<T>() where T : class {
            string name = typeof(T).Name;
            int indexOfSplit = name.IndexOf("__");
            if (indexOfSplit != -1)
                name = name.Substring(0, indexOfSplit);
            SDL.FunctionPointer ptr = SDL.GLGetProcAddress(name);

            // If that fails, try with ARB prefix
            if (ptr == null)
            {
                string arbName = "ARB" + name;
                Console.WriteLine($"Trying ARB variant: {arbName}");
                ptr = SDL.GLGetProcAddress(arbName);
            }
            if (ptr == null)
            {
                Console.WriteLine($"Failed to load GL function: {name}");
                return null;
            }

            IntPtr nativePtr = Marshal.GetFunctionPointerForDelegate(ptr);

            Console.WriteLine($"LOADED: {name}");
            return Marshal.GetDelegateForFunctionPointer<T>(nativePtr);
        }

        // In no particular order
        public enum Enum : int {
            GL_TEXTURE_BINDING_2D = 0x8069,
            GL_VIEWPORT = 0x0BA2,
            GL_SCISSOR_BOX = 0x0C10,
            GL_ENABLE_BIT = 0x00002000,
            GL_TRANSFORM_BIT = 0x00001000,
            GL_BLEND = 0x0BE2,
            GL_STENCIL_BUFFER_BIT = 0x00000400,
            GL_COLOR_BUFFER_BIT = 0x00004000,
            GL_FALSE = 0,
            GL_TRUE = 1,
            GL_POINTS = 0x0000,
            GL_LINES = 0x0001,
            GL_LINE_LOOP = 0x0002,
            GL_LINE_STRIP = 0x0003,
            GL_TRIANGLES = 0x0004,
            GL_TRIANGLE_STRIP = 0x0005,
            GL_TRIANGLE_FAN = 0x0006,
            GL_QUADS = 0x0007,
            GL_NEVER = 0x0200,
            GL_LESS = 0x0201,
            GL_EQUAL = 0x0202,
            GL_LEQUAL = 0x0203,
            GL_GREATER = 0x0204,
            GL_NOTEQUAL = 0x0205,
            GL_GEQUAL = 0x0206,
            GL_ALWAYS = 0x0207,
            GL_ZERO = 0,
            GL_ONE = 1,
            GL_SRC_COLOR = 0x0300,
            GL_ONE_MINUS_SRC_COLOR = 0x0301,
            GL_SRC_ALPHA = 0x0302,
            GL_ONE_MINUS_SRC_ALPHA = 0x0303,
            GL_DST_ALPHA = 0x0304,
            GL_ONE_MINUS_DST_ALPHA = 0x0305,
            GL_DST_COLOR = 0x0306,
            GL_ONE_MINUS_DST_COLOR = 0x0307,
            GL_SRC_ALPHA_SATURATE = 0x0308,
            GL_NONE = 0,
            GL_CULL_FACE = 0x0B44,
            GL_DEPTH_TEST = 0x0B71,
            GL_SCISSOR_TEST = 0x0C11,
            GL_VERTEX_ARRAY = 0x8074,
            GL_TEXTURE_COORD_ARRAY = 0x8078,
            GL_COLOR_ARRAY = 0x8076,
            GL_TEXTURE_1D = 0x0DE0,
            GL_TEXTURE_2D = 0x0DE1,
            GL_TEXTURE_3D = 0x806F,
            GL_TEXTURE_WIDTH = 0x1000,
            GL_TEXTURE_HEIGHT = 0x1001,
            GL_TEXTURE_BORDER_COLOR = 0x1004,
            GL_DONT_CARE = 0x1100,
            GL_FASTEST = 0x1101,
            GL_NICEST = 0x1102,
            GL_BYTE = 0x1400,
            GL_UNSIGNED_BYTE = 0x1401,
            GL_SHORT = 0x1402,
            GL_UNSIGNED_SHORT = 0x1403,
            GL_INT = 0x1404,
            GL_UNSIGNED_INT = 0x1405,
            GL_FLOAT = 0x1406,
            GL_MODELVIEW = 0x1700,
            GL_PROJECTION = 0x1701,
            GL_TEXTURE = 0x1702,
            GL_COLOR = 0x1800,
            GL_DEPTH = 0x1801,
            GL_STENCIL = 0x1802,
            GL_STENCIL_INDEX = 0x1901,
            GL_DEPTH_COMPONENT = 0x1902,
            GL_RED = 0x1903,
            GL_GREEN = 0x1904,
            GL_BLUE = 0x1905,
            GL_ALPHA = 0x1906,
            GL_RGB = 0x1907,
            GL_RGBA = 0x1908,
            GL_RGB8 = 0x8051,
            GL_RGBA8 = 0x8058,
            GL_BGR = 0x80E0,
            GL_BGRA = 0x80E1,
            GL_POINT = 0x1B00,
            GL_LINE = 0x1B01,
            GL_FILL = 0x1B02,
            GL_KEEP = 0x1E00,
            GL_REPLACE = 0x1E01,
            GL_INCR = 0x1E02,
            GL_DECR = 0x1E03,
            GL_VENDOR = 0x1F00,
            GL_RENDERER = 0x1F01,
            GL_VERSION = 0x1F02,
            GL_EXTENSIONS = 0x1F03,
            GL_NEAREST = 0x2600,
            GL_LINEAR = 0x2601,
            GL_NEAREST_MIPMAP_NEAREST = 0x2700,
            GL_LINEAR_MIPMAP_NEAREST = 0x2701,
            GL_NEAREST_MIPMAP_LINEAR = 0x2702,
            GL_LINEAR_MIPMAP_LINEAR = 0x2703,
            GL_TEXTURE_MAG_FILTER = 0x2800,
            GL_TEXTURE_MIN_FILTER = 0x2801,
            GL_TEXTURE_WRAP_S = 0x2802,
            GL_TEXTURE_WRAP_T = 0x2803,
            GL_REPEAT = 0x2901,
            GL_UNPACK_ROW_LENGTH = 0x0CF2,
            GL_CLAMP_TO_EDGE = 0x812F,
            GL_VERTEX_SHADER = 0x8B31,
            GL_FRAGMENT_SHADER = 0x8B30,
            GL_ARRAY_BUFFER = 0x8892,
            GL_STATIC_DRAW = 0x88E4,
            GL_COMPILE_STATUS = 0x8B81,
            GL_CURRENT_PROGRAM = 0x8B8D,
            GL_ACTIVE_UNIFORMS = 0x8B86,
            GL_LINK_STATUS = 0x8B82,
            GL_NUM_EXTENSIONS = 0x821D,
            GL_VALIDATE_STATUS = 0x8B83,
            GL_COLOR_WRITEMASK = 0x0C23,
            GL_CURRENT_COLOR = 0x0B00,
            GL_COLOR_MATERIAL = 0x0B57,
            GL_ACTIVE_ATTRIBUTES = 0x8B89,
        }

        public enum GetTextureParameter : uint
        {
            TextureWidth = 0x1000,                  // GL_TEXTURE_WIDTH
            TextureHeight = 0x1001,                 // GL_TEXTURE_HEIGHT
            TextureDepth = 0x8071,                  // GL_TEXTURE_DEPTH
            TextureInternalFormat = 0x1003,         // GL_TEXTURE_INTERNAL_FORMAT
            TextureRedSize = 0x805C,                // GL_TEXTURE_RED_SIZE
            TextureGreenSize = 0x805D,              // GL_TEXTURE_GREEN_SIZE
            TextureBlueSize = 0x805E,               // GL_TEXTURE_BLUE_SIZE
            TextureAlphaSize = 0x805F,              // GL_TEXTURE_ALPHA_SIZE
            TextureDepthSize = 0x884A,              // GL_TEXTURE_DEPTH_SIZE
            TextureCompressed = 0x86A1,             // GL_TEXTURE_COMPRESSED
            TextureCompressedImageSize = 0x86A0,    // GL_TEXTURE_COMPRESSED_IMAGE_SIZE
        }

        public delegate IntPtr glGetString(Enum pname);
        private static glGetString _GetString = _<glGetString>();
        public static string GetString(Enum pname)
            => new string((sbyte*) _GetString(pname));

        public delegate void glGetIntegerv(Enum pname, out int param);
        public static glGetIntegerv GetIntegerv = _<glGetIntegerv>();
        public delegate void glGetIntegerv__4(Enum pname, out Int4 param);
        public static glGetIntegerv__4 GetIntegerv4 = _<glGetIntegerv__4>();

        public delegate void glEnable(Enum cap);
        public static glEnable Enable = _<glEnable>();

        public delegate void glDisable(Enum cap);
        public static glDisable Disable = _<glDisable>();

        public delegate void glViewport(int x, int y, int width, int height);
        public static glViewport Viewport = _<glViewport>();

        public delegate void glPushAttrib(Enum mask);
        public static glPushAttrib PushAttrib = _<glPushAttrib>();

        public delegate void glPopAttrib();
        public static glPopAttrib PopAttrib = _<glPopAttrib>();

        public delegate void glBlendFunc(Enum src, Enum dst);
        public static glBlendFunc BlendFunc = _<glBlendFunc>();

        public delegate void glEnableClientState(Enum array);
        public static glEnableClientState EnableClientState = _<glEnableClientState>();

        public delegate void glDisableClientState(Enum array);
        public static glDisableClientState DisableClientState = _<glDisableClientState>();

        public delegate void glUseProgram(uint program);
        public static glUseProgram UseProgram = _<glUseProgram>();

        public delegate void glMatrixMode(Enum mode);
        public static glMatrixMode MatrixMode = _<glMatrixMode>();

        public delegate void glPushMatrix();
        public static glPushMatrix PushMatrix = _<glPushMatrix>();

        public delegate void glPopMatrix();
        public static glPopMatrix PopMatrix = _<glPopMatrix>();

        public delegate void glLoadIdentity();
        public static glLoadIdentity LoadIdentity = _<glLoadIdentity>();

        public delegate void glOrtho(double left, double right, double bottom, double top, double zNear, double zFar);
        public static glOrtho Ortho = _<glOrtho>();

        public delegate void glVertexPointer(int size, Enum type, int stride, IntPtr pointer);
        public static glVertexPointer VertexPointer = _<glVertexPointer>();

        public delegate void glTexCoordPointer(int size, Enum type, int stride, IntPtr pointer);
        public static glTexCoordPointer TexCoordPointer = _<glTexCoordPointer>();

        public delegate void glColorPointer(int size, Enum type, int stride, IntPtr pointer);
        public static glColorPointer ColorPointer = _<glColorPointer>();

        public delegate void glBindTexture(Enum target, uint texture);
        public static glBindTexture BindTexture = _<glBindTexture>();

        public delegate void glScissor(int x, int y, int width, int height);
        public static glScissor Scissor = _<glScissor>();

        public delegate void glDrawElements(Enum mode, int count, Enum type, IntPtr indices);
        public static glDrawElements DrawElements = _<glDrawElements>();

        public delegate void glClearColor(float r, float g, float b, float a);
        public static glClearColor ClearColor = _<glClearColor>();

        public delegate void glClear(Enum mask);
        public static glClear Clear = _<glClear>();

        public delegate void glGenTextures(int n, out uint textures);
        public static glGenTextures GenTextures = _<glGenTextures>();

        public delegate void glTexParameteri(Enum target, Enum pname, int param);
        public static glTexParameteri TexParameteri = _<glTexParameteri>();

        public delegate void glPixelStorei(Enum pname, int param);
        public static glPixelStorei PixelStorei = _<glPixelStorei>();

        public delegate void glTexImage2D(
            Enum target,
            int level,
            int internalFormat,
            int width,
            int height,
            int border,
            Enum format,
            Enum type,
            IntPtr pixels
        );
        public static glTexImage2D TexImage2D = _<glTexImage2D>();

        public delegate void glDeleteTextures(int n, ref uint textures);
        public static glDeleteTextures DeleteTextures = _<glDeleteTextures>();

        public delegate uint glGetError();
        public static glGetError GetError = _<glGetError>();

        public delegate uint glCreateShader(uint type);
        public static glCreateShader CreateShader = _<glCreateShader>();
        public delegate void glShaderSource(uint shader, int count, string[] source, int[] length);
        public static glShaderSource ShaderSource = _<glShaderSource>();
        public delegate void glCompileShader(uint shader);
        public static glCompileShader CompileShader = _<glCompileShader>();
        public delegate uint glCreateProgram();
        public static glCreateProgram CreateProgram = _<glCreateProgram>();
        public delegate void glAttachShader(uint program, uint shader);
        public static glAttachShader AttachShader = _<glAttachShader>();
        public delegate void glLinkProgram(uint program);
        public static glLinkProgram LinkProgram = _<glLinkProgram>();
        public delegate int glGetUniformLocation(uint program, string name);
        public static glGetUniformLocation GetUniformLocation = _<glGetUniformLocation>();
        public delegate void glUniformMatrix4fv(int location, int count, bool transpose, float[] value);
        public static glUniformMatrix4fv UniformMatrix4fv = _<glUniformMatrix4fv>();
        public delegate void glDeleteShader(uint shader);
        public static glDeleteShader DeleteShader = _<glDeleteShader>();
        public delegate void glColor4f(float red, float green, float blue, float alpha);
        public static glColor4f Color4f = _<glColor4f>();
        public delegate void glUniform4f(int location, float v0, float v1, float v2, float v3);
        public static glUniform4f Uniform4f = _<glUniform4f>();
        public delegate void glGenBuffers(int n, out uint buffers);
        public static glGenBuffers GenBuffers = _<glGenBuffers>();

        public delegate void glBindBuffer(uint target, uint buffer);
        public static glBindBuffer BindBuffer = _<glBindBuffer>();
        public delegate void glBufferData(uint target, int size, float[] data, uint usage);
        public static glBufferData BufferData = _<glBufferData>();
        public delegate void glVertexAttribPointer(uint index, int size, uint type, bool normalized, int stride, IntPtr pointer);
        public static glVertexAttribPointer VertexAttribPointer = _<glVertexAttribPointer>();
        public delegate void glEnableVertexAttribArray(uint index);
        public static glEnableVertexAttribArray EnableVertexAttribArray = _<glEnableVertexAttribArray>();
        public delegate void glDrawArrays(uint mode, int first, int count);
        public static glDrawArrays DrawArrays = _<glDrawArrays>();
        public delegate void glDeleteBuffers(int n, ref uint buffers);
        public static glDeleteBuffers DeleteBuffers = _<glDeleteBuffers>();
        public delegate void glDeleteProgram(uint program);
        public static glDeleteProgram DeleteProgram = _<glDeleteProgram>();
        public delegate void glGetShaderiv(uint shader, Enum pname, out int param);
        public static glGetShaderiv GetShaderiv = _<glGetShaderiv>();
        public delegate void glGetShaderInfoLog(uint shader, int bufSize, out int length, StringBuilder infoLog);
        public static glGetShaderInfoLog GetShaderInfoLog = _<glGetShaderInfoLog>();
        public delegate void glColor4ub(byte red, byte green, byte blue, byte alpha);
        public static glColor4ub Color4ub = _<glColor4ub>();
        public delegate void glBegin(uint mode);
        public static glBegin Begin = _<glBegin>();
        public delegate void glVertex2d(double x, double y);
        public static glVertex2d Vertex2d = _<glVertex2d>();
        public delegate void glEnd();
        public static glEnd End = _<glEnd>();
        public delegate void glDisableVertexAttribArray(uint index);
        public static glDisableVertexAttribArray DisableVertexAttribArray = _<glDisableVertexAttribArray>();
        public delegate void glGetActiveUniform(uint program, uint index, int bufSize, out int length, out int size, out uint type, StringBuilder name);
        public static glGetActiveUniform GetActiveUniform = _<glGetActiveUniform>();
        public delegate void glGetProgramiv(uint program, Enum pname, out int param);
        public static glGetProgramiv GetProgramiv = _<glGetProgramiv>();
        public delegate IntPtr glGetStringi(Enum name, uint index);
        public static glGetStringi GetStringi = _<glGetStringi>();
        public delegate void glValidateProgram(uint program);
        public static glValidateProgram ValidateProgram = _<glValidateProgram>();

        public delegate void glGetProgramInfoLog(uint program, int maxLength, out int length, StringBuilder infoLog);
        public static glGetProgramInfoLog GetProgramInfoLog = _<glGetProgramInfoLog>();
        public delegate void glColorMask(bool red, bool green, bool blue, bool alpha);
        public static glColorMask ColorMask = _<glColorMask>();

        public delegate void glGetFloatv(Enum pname, out float param);
        public static glGetFloatv GetFloatv = _<glGetFloatv>();

        public delegate void glGetTexLevelParameteriv(uint target, int level, uint pname, out int param);
        public static glGetTexLevelParameteriv GetTexLevelParameteri = _<glGetTexLevelParameteriv>();
        public delegate void glGetTexLevelParameterfv(uint target, int level, uint pname, out float param);
        public static glGetTexLevelParameterfv GetTexLevelParameterf = _<glGetTexLevelParameterfv>();
        public delegate void glTexSubImage2D(Enum target, int level, int xoffset, int yoffset, int width, int height, Enum format, Enum type, IntPtr pixels);
        public static glTexSubImage2D TexSubImage2D = _<glTexSubImage2D>();

        /*
        public delegate void gl();
        public static gl  = _<gl>();
        */

    }
}
