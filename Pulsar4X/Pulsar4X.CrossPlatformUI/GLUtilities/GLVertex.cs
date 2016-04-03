using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using OpenTK;

namespace Pulsar4X.CrossPlatformUI.GLUtilities
{
    /// <summary>
    /// This is a basic class used to repersent a vertex position in 3D. It can be used for 2D if desired.
    /// The Vertex position is relative to the whole object it is part of. i.e. if this is one of 4 verticies making up a quad,
    /// then the first vertex position might be (-0.5, -0.5), assuming the object is 1x1 and we are positioning it based on its centre.
    /// Color is the color for this vertex in RGBA 32 bit format.
    /// UV is the corrosponding coordinantes on the texture to which this vertex maps, from 0 to 1.
    /// </summary>
    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]    // Forces memory layout.
    public struct GLVertex
    {
        /// <summary>
        /// The Vertix position Relative to the position of the whole object of which this vertex is a part of.
        /// </summary>lo
        public OpenTK.Vector4 m_v4Position;

        /// <summary>
        /// The Color of the vertex. We are using a Half size float because each chanle only needs 8 bits of data, 
        /// the half size float provide 10bits, i.e. more than enough, while still saving on copying 2 bytes of data
        /// to the graphics card each call (or 8 bytes for a quad!!).
        /// </summary>
        public OpenTK.Vector4 m_v4Color;

        /// <summary>
        /// The Texture "UV" coordinates for this vertex.
        /// </summary>
        public OpenTK.Vector2 m_v2UV;

        //public GLVertex()
        //{
        //    m_v3Position.X = 0;
        //    m_v3Position.Y = 0;
        //    m_v3Position.Z = 0;

        //    m_v4Color.X = (OpenTK.Half)0;
        //    m_v4Color.Y = (OpenTK.Half)0;
        //    m_v4Color.Z = (OpenTK.Half)0;
        //    m_v4Color.W = (OpenTK.Half)0;

        //    m_v2UV.X = 0;
        //    m_v2UV.Y = 0;
        //}

        public GLVertex(OpenTK.Vector4 a_v4Position, OpenTK.Vector4 a_v4Color, OpenTK.Vector2 a_v2UV)
        {
            m_v4Position = a_v4Position;
            m_v4Color = a_v4Color;
            m_v2UV = a_v2UV;
        }

        public GLVertex(OpenTK.Vector4 a_v4Position, Color4 a_oColor, OpenTK.Vector2 a_v2UV)
        {
            m_v4Position = a_v4Position;
            // We order the color RGBA because this is the order expected by OpenGL/Graphics card.
            m_v4Color.X = a_oColor.R / 255.0f;
            m_v4Color.Y = a_oColor.G / 255.0f;
            m_v4Color.Z = a_oColor.B / 255.0f;
            m_v4Color.W = a_oColor.A / 255.0f;
            m_v2UV = a_v2UV;
        }

        public void SetColor(Color4 a_oColor)
        {
            m_v4Color.X = a_oColor.R / 255.0f;
            m_v4Color.Y = a_oColor.G / 255.0f;
            m_v4Color.Z = a_oColor.B / 255.0f;
            m_v4Color.W = a_oColor.A / 255.0f;
        }

        /// <summary>
        /// Calculates and returns the Size in Bytes of this struct.
        /// </summary>
        /// <returns> The Size In Bytes of a GLVertex.</returns>
        static public int SizeInBytes()
        {
            // the below code was used to test that this function was working, it is. 
            //int temp = Vector3.SizeInBytes + Vector4h.SizeInBytes + Vector2.SizeInBytes;
            //GLVertex temp3 = new GLVertex();
            //int temp2 = System.Runtime.InteropServices.Marshal.SizeOf(temp3);
            return (Vector4.SizeInBytes + Vector4.SizeInBytes + Vector2.SizeInBytes);
        }

        // Note the below code can be used to test that this struct is Bittable. It return true as of 26/8/12.
        //GLUtilities.GLVertex temp = new GLUtilities.GLVertex();
        //bool test = OpenTK.BlittableValueType.Check<GLUtilities.GLVertex>(temp);
    }
}
