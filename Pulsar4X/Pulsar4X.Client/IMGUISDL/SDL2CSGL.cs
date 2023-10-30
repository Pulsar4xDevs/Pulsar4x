using SDL2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ImGuiNET;
using System.IO;
using System.Runtime.InteropServices;

namespace ImGuiSDL2CS {
    // Even smaller than MiniTK, only offering the bare minimum required for ImGuiSDL2CS.
    public unsafe static class GL {

        private static T _<T>() where T : class {
            string name = typeof(T).Name;
            int indexOfSplit = name.IndexOf("__");
            if (indexOfSplit != -1)
                name = name.Substring(0, indexOfSplit);
            IntPtr ptr = SDL.SDL_GL_GetProcAddress(name);
            if (ptr == IntPtr.Zero)
                return null;
            return Marshal.GetDelegateForFunctionPointer(ptr, typeof(T)) as T;
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
            GL_UNPACK_ROW_LENGTH = 0x0CF2
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

        public delegate void glBindTexture(Enum target, int texture);
        public static glBindTexture BindTexture = _<glBindTexture>();

        public delegate void glScissor(int x, int y, int width, int height);
        public static glScissor Scissor = _<glScissor>();

        public delegate void glDrawElements(Enum mode, int count, Enum type, IntPtr indices);
        public static glDrawElements DrawElements = _<glDrawElements>();

        public delegate void glClearColor(float r, float g, float b, float a);
        public static glClearColor ClearColor = _<glClearColor>();

        public delegate void glClear(Enum mask);
        public static glClear Clear = _<glClear>();

        public delegate void glGenTextures(int n, out int textures);
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

        /*
        public delegate void gl();
        public static gl  = _<gl>();
        */

    }
}
