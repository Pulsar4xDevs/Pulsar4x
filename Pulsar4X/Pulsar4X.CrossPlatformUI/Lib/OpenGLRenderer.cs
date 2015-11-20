using System;
using System.Collections.Generic;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;

namespace Pulsar4X.CrossPlatformUI
{
	public class OpenGLRenderer
	{
		private List<int> shaderList;

		int theProgram;

		int positionBufferObject;
		private int vao;

		readonly Vector4[] vertexPositions =
		{
			new Vector4(0.75f, 0.75f, 0.0f, 1.0f),
			new Vector4(0.75f, -0.75f, 0.0f, 1.0f),
			new Vector4(-0.75f, -0.75f, 0.0f, 1.0f),
		};

		private const string strVertexShader = @"#version 330
			layout(location = 0) in vec4 position;
			void main()
			{
			   gl_Position = position;
			}";

		private const string strFragmentShader = @"#version 330
			out vec4 outputColor;
			void main()
			{
			   outputColor = vec4(1.0f, 1.0f, 1.0f, 1.0f);
			}";

		public OpenGLRenderer ()
		{
            shaderList = new List<int>();
		}

		int CreateShader(ShaderType eShaderType, string strShaderFile)
		{
			int shader = GL.CreateShader(eShaderType);
			GL.ShaderSource(shader, strShaderFile);
			GL.CompileShader(shader);

			int status;
			GL.GetShader(shader, ShaderParameter.CompileStatus, out status);
			if (status == 0)
			{
				int infoLogLength;
				GL.GetShader(shader, ShaderParameter.InfoLogLength, out infoLogLength);

				string strInfoLog;
				GL.GetShaderInfoLog(shader, out strInfoLog);

				string strShaderType;
				switch (eShaderType)
				{
				case ShaderType.FragmentShader:
					strShaderType = "fragment";
					break;
				case ShaderType.VertexShader:
					strShaderType = "vertex";
					break;
				case ShaderType.GeometryShader:
					strShaderType = "geometry";
					break;
				default:
					throw new ArgumentOutOfRangeException("eShaderType");
				}

				//Debug.WriteLine("Compile failure in " + strShaderType + " shader:\n" + strInfoLog, "Error");
			}

			return shader;
		}

		int CreateProgram(List<int> shaderList)
		{
			int program = GL.CreateProgram();

			foreach (int shader in shaderList)
			{
				GL.AttachShader(program, shader);
			}

			GL.LinkProgram(program);

			int status;
			GL.GetProgram(program, GetProgramParameterName.LinkStatus, out status);
			if (status == 0)
			{
				int infoLogLength;
				GL.GetProgram(program, GetProgramParameterName.InfoLogLength, out infoLogLength);

				string strInfoLog;
				GL.GetProgramInfoLog(program, out strInfoLog);
				//Debug.WriteLine("Linker failure: " + strInfoLog, "Error");
				throw new Exception("Linker failure: " + strInfoLog);
			}

			foreach (int shader in shaderList)
			{
				GL.DetachShader(program, shader);
			}

			return program;
		}

		public void Initialize()
		{
			GL.PixelStore(PixelStoreParameter.PackAlignment, 1);
			GL.PixelStore(PixelStoreParameter.UnpackAlignment, 1);
			GL.ClearColor(0.0F, 0.0F, 0.0F, 1.0F);
			GL.Clear(ClearBufferMask.ColorBufferBit);
			GL.ShadeModel(ShadingModel.Smooth);
			GL.Hint(HintTarget.PerspectiveCorrectionHint, HintMode.Nicest);
			GL.Enable(EnableCap.DepthTest);

			InitializeProgram();
			InitializeVertexBuffer();

			GL.GenVertexArrays(1, out vao);
			GL.BindVertexArray(vao);
		}

		public void Draw() {
			GL.ClearColor(0.0f, 0.0f, 0.0f, 1.0F);
			GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
			GL.UseProgram(theProgram);

			GL.BindBuffer(BufferTarget.ArrayBuffer, positionBufferObject);
			GL.EnableVertexAttribArray(0);
			GL.VertexAttribPointer(0, 4, VertexAttribPointerType.Float, false, 0, 0);

			GL.DrawArrays(PrimitiveType.Triangles, 0, 3);

			GL.DisableVertexAttribArray(0);
			GL.UseProgram(0);
			GL.Flush();
		}

		public void Resize() {

		}

		public void Destroy() {

		}

		void InitializeProgram()
		{
			shaderList.Add(CreateShader(ShaderType.VertexShader, strVertexShader));
			shaderList.Add(CreateShader(ShaderType.FragmentShader, strFragmentShader));

			theProgram = CreateProgram(shaderList);

			foreach (int shader in shaderList)
			{
				GL.DeleteShader(shader);
			}
		}

		void InitializeVertexBuffer()
		{
			GL.GenBuffers(1, out positionBufferObject);
			GL.BindBuffer(BufferTarget.ArrayBuffer, positionBufferObject);
			GL.BufferData(BufferTarget.ArrayBuffer,
				(IntPtr)(vertexPositions.Length * Vector4.SizeInBytes),
				vertexPositions,
				BufferUsageHint.StaticDraw);
			GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
		}
	}
}

