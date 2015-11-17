using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Collections.ObjectModel;
using Eto.Forms;
using Eto.Drawing;
using Eto.Serialization.Json;
using Pulsar4X.ViewModel;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;

//#TODO Move the actual rendering stuff out into a partial and separate it from the VMs
//I might also want to make windows render with DirectX so that it plays nicer with wpf, 
//so we'll have to see if we can abstract that too
namespace Pulsar4X.CrossPlatformUI.Views
{
    public class SystemView : Panel
    {
        protected ListBox Systems;
        protected SystemVM CurrentSystem;

        protected Splitter Body;

        protected GLSurface gl_context;

        private UITimer timDraw;

        private bool drawPending = false;

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

        public SystemView(GameVM GameVM)
        {
            DataContext = GameVM;
            gl_context = new GLSurface(GraphicsMode.Default, 3, 3, GraphicsContextFlags.Default);
            shaderList = new List<int>();
            JsonReader.Load(this);

            Systems.BindDataContext(c => c.DataStore, (GameVM c) => c.StarSystems);
            Systems.ItemTextBinding = Binding.Property((SystemVM vm) => vm.Name);
            Systems.ItemKeyBinding = Binding.Property((SystemVM vm) => vm.ID).Convert((Guid ID) => ID.ToString());
            Systems.SelectedIndexChanged += loadSystem;

            Body.Panel2 = gl_context;
            gl_context.GLInitalized += InitializeCanvas;
            gl_context.GLDrawNow += DrawNow;
            gl_context.GLShuttingDown += TeardownCanvas;
            gl_context.GLResize += Resize;
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

        private void init()
        {

        }

        private void Draw()
        {
            drawPending = true;
        }

        private void timDraw_Elapsed(object sender, EventArgs e)
        {
            if (!drawPending || !gl_context.IsInitialized)
            {
                return;
            }

            gl_context.MakeCurrent();

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
            gl_context.SwapBuffers();

            drawPending = false;
        }

        public void loadSystem(object sender, EventArgs e)
        {
            CurrentSystem = (SystemVM)((ListBox)sender).SelectedValue;
            Draw();
        }

        public void InitializeCanvas(object sender, EventArgs e)
        {
            gl_context.MakeCurrent();

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

            timDraw = new UITimer { Interval = 0.013 }; // Every Millisecond.
            timDraw.Elapsed += timDraw_Elapsed;
            timDraw.Start();
        }

        public void DrawNow(object sender, EventArgs e)
        {
            Draw();
        }

        public void Resize(object sender, EventArgs e)
        {
            Draw();
        }

        public void TeardownCanvas(object sender, EventArgs e)
        {
        }
    }
}
