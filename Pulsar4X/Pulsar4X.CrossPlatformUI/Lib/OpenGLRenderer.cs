using OpenTK;
using OpenTK.Graphics.OpenGL;
using Pulsar4X.ViewModel;
using System;
using System.Collections.Generic;
using Pulsar4X.ViewModel.SystemView;

namespace Pulsar4X.CrossPlatformUI
{
    public class OpenGLRenderer
	{
        private RenderVM RenderVM;
		private List<int> shaderList;

		int theProgram;
        int VP_Matrix_Unif;

        Matrix4 projection;

		private int vao;

		private const string strVertexShader = @"#version 330
			layout(location = 0) in vec3 vertex_pos;
            layout(location = 1) in vec3 instance_pos;

            uniform mat4 VP_Matrix;

			void main()
			{
			   gl_Position = VP_Matrix * vec4(vertex_pos + instance_pos, 1.0f);
			}";

		private const string strFragmentShader = @"#version 330
			out vec4 outputColor;
			void main()
			{
			   outputColor = vec4(1.0f, 1.0f, 1.0f, 1.0f);
			}";

		public OpenGLRenderer (RenderVM render_data)
		{
            shaderList = new List<int>();
            RenderVM = render_data;
            RenderVM.SceneLoaded += LoadScenes;
		}

        public void LoadScenes(object sender, EventArgs e)
        {
            foreach(var scene_kv in RenderVM.scenes)
            {
                var scene = scene_kv.Value;
                if (!scene.IsInitialized)
                {
                    GL.GenBuffers(1, out scene.mesh.vertex_buffer_id);
                    GL.BindBuffer(BufferTarget.ArrayBuffer, scene.mesh.vertex_buffer_id);
                    GL.BufferData(BufferTarget.ArrayBuffer, (IntPtr)(scene.mesh.vertices.Count * Vector3.SizeInBytes), scene.mesh.vertices.ToArray(), BufferUsageHint.StaticDraw);
                    // 1st attribute buffer : vertices
                    GL.EnableVertexAttribArray(0);
                    GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 0, 0);

                    GL.GenBuffers(1, out scene.mesh.index_buffer_id);
                    GL.BindBuffer(BufferTarget.ElementArrayBuffer, scene.mesh.index_buffer_id);
                    GL.BufferData(BufferTarget.ElementArrayBuffer, (IntPtr)(scene.mesh.indices.Count * sizeof(uint)), scene.mesh.indices.ToArray(), BufferUsageHint.StaticDraw);

                    // The VBO containing the positions and sizes of the particles
                    GL.GenBuffers(1, out scene.position_buffer_id);
                    GL.BindBuffer(BufferTarget.ArrayBuffer, scene.position_buffer_id);
                    // Initialize with empty (NULL) buffer : it will be updated later, each frame.
                    GL.BufferData(BufferTarget.ArrayBuffer, (IntPtr)(scene.position_data.Count * Vector3.SizeInBytes), IntPtr.Zero, BufferUsageHint.StreamDraw);
                    scene.IsInitialized = true;
                }
            }
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

		public void Initialize(int x, int y, int viewport_width, int viewport_height)
		{
            GL.Viewport(x, y, viewport_width, viewport_height);
            GL.PixelStore(PixelStoreParameter.PackAlignment, 1);
			GL.PixelStore(PixelStoreParameter.UnpackAlignment, 1);
			GL.ClearColor(0.0F, 0.0F, 0.0F, 1.0F);
			GL.Clear(ClearBufferMask.ColorBufferBit);
			GL.ShadeModel(ShadingModel.Smooth);
			GL.Hint(HintTarget.PerspectiveCorrectionHint, HintMode.Nicest);
			GL.Enable(EnableCap.DepthTest);

			InitializeProgram();
            VP_Matrix_Unif = GL.GetUniformLocation(theProgram, "VP_Matrix");
            RenderVM.Resize(viewport_width, viewport_height);
            
            GL.GenVertexArrays(1, out vao);
			GL.BindVertexArray(vao);

        }

		public void Draw(RenderVM data) {
			GL.ClearColor(System.Drawing.Color.MidnightBlue);
			GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
            
            GL.UseProgram(theProgram);
            foreach (var scene_kv in data.scenes)
            {
                var scene = scene_kv.Value;
                var view_projection = scene.camera.GetViewProjectionMatrix();
                GL.UniformMatrix4(VP_Matrix_Unif, 1, false, ref view_projection.Row0.X);
                GL.BindBuffer(BufferTarget.ArrayBuffer, scene.position_buffer_id);
                // Write null to avoid implicit sync
                GL.BufferData(BufferTarget.ArrayBuffer, (IntPtr)(scene.position_data.Count * Vector3.SizeInBytes), IntPtr.Zero, BufferUsageHint.StreamDraw);
                GL.BufferSubData(BufferTarget.ArrayBuffer, IntPtr.Zero, (IntPtr)(scene.position_data.Count * Vector3.SizeInBytes), scene.position_data.ToArray());

                // 2nd attribute buffer : instance position vectors
                GL.BindBuffer(BufferTarget.ArrayBuffer, scene.position_buffer_id);
                GL.EnableVertexAttribArray(1);
                GL.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false, 0, 0);

                GL.VertexAttribDivisor(1, 1); // positions : one per quad (its center) -> 1

                GL.DrawElementsInstanced(PrimitiveType.Triangles, scene.mesh.indices.Count, DrawElementsType.UnsignedInt, IntPtr.Zero, scene.position_data.Count);
            }
            GL.UseProgram(0);

			GL.Flush();
		}

		public void Resize(int x, int y, int viewport_width, int viewport_height) {
            GL.Viewport(x, y, viewport_width, viewport_height);
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
	}
}

