using System.Runtime.InteropServices;

namespace ImGuiNET
{
    // Those didn't exist in vanilla ImGui.NET, but we're dropping System.Numerics
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct ImVec2
    {

        public static ImVec2 Zero => new ImVec2(0f, 0f);
        public static ImVec2 One => new ImVec2(1f, 1f);

        public float x, y;

        public float X
        {
            get
            {
                return x;
            }
            set
            {
                x = value;
            }
        }

        public float Y
        {
            get
            {
                return y;
            }
            set
            {
                y = value;
            }
        }

        public ImVec2(float x, float y)
        {
            this.x = x;
            this.y = y;
        }

    }

    // ImVec3 doesn't exist, but is required as a replacement for ImVec3.
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct ImVec3
    {

        public float x, y, z;

        public float X
        {
            get
            {
                return x;
            }
            set
            {
                x = value;
            }
        }

        public float Y
        {
            get
            {
                return y;
            }
            set
            {
                y = value;
            }
        }

        public float Z
        {
            get
            {
                return z;
            }
            set
            {
                z = value;
            }
        }

        public ImVec3(float x, float y, float z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }

    }

    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct ImVec4
    {

        public float x, y, z, w;

        public float X
        {
            get
            {
                return x;
            }
            set
            {
                x = value;
            }
        }

        public float Y
        {
            get
            {
                return y;
            }
            set
            {
                y = value;
            }
        }

        public float Z
        {
            get
            {
                return z;
            }
            set
            {
                z = value;
            }
        }

        public float W
        {
            get
            {
                return w;
            }
            set
            {
                w = value;
            }
        }

        public ImVec4(float x, float y, float z, float w)
        {
            this.x = x;
            this.y = y;
            this.z = z;
            this.w = w;
        }

    }

    [StructLayout(LayoutKind.Sequential)]
    public struct Int2
    {
        public readonly int X, Y;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct Int3
    {
        public readonly int X, Y, Z;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct Int4
    {
        public readonly int X, Y, Z, W;
    }
}
