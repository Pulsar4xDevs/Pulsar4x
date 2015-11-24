using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using Pulsar4X.ECSLib;
using Pulsar4X.Sceen;
using System.Diagnostics;
using OpenTK;
using Vector4 = OpenTK.Vector4;

namespace Pulsar4X.ViewModel
{
    /// <summary>
    /// This is the VM for the opengl window, it handles updating the scene manager and triggering draws.
    /// Do not be cavalier about creating and destroying this vm, it should be long lived as it does a lot
    /// of caching and building out data structures.
    /// </summary>
    public class RenderVM : IViewModel
    {
        private SystemVM active_system;
        public SystemVM ActiveSystem {
            get { return active_system; }
            set {
                active_system = value;
                LoadSystem(active_system);
            }
        }

        public List<Scene> scenes;

        public bool drawPending = false;

        public RenderVM()
        {
            scenes = new List<Scene>();
        }

        public void Draw()
        {

        }

        public void Initialize(object sender, EventArgs e)
        {

        }

        public void Draw(object sender, EventArgs e)
        {
            Draw();
        }

        public void Resize(object sender, EventArgs e)
        {
            Draw();
        }

        public void Teardown(object sender, EventArgs e)
        {

        }

        public event EventHandler SceneLoaded;
        public event PropertyChangedEventHandler PropertyChanged;

        private void LoadSystem(SystemVM system)
        {
            List<Scene> temp_scenes = new List<Scene>();
            //create our quad --- TODO: Move this out into a structure
            List<Vector3> vertices = new List<Vector3>();
            List<uint> indices = new List<uint>();
            Vector3[] tmp_vectors = {
                    new Vector3(-0.5f, 0.5f, 0f),
                    new Vector3(-0.5f, -0.5f, 0f),
                    new Vector3(0.5f, -0.5f, 0f),
                    new Vector3(0.5f, 0.5f, 0f)
                };
        
            vertices.AddRange(tmp_vectors);

            uint[] tmp_indices = {
                0, 1, 2,
                2, 3, 0
            };
            indices.AddRange(tmp_indices);
            //this is our base instance
            var mesh = new Mesh(vertices, indices);
            //create instance specific data
            List<Vector3> position_data = new List<Vector3>();
            List<float> scale_data = new List<float>();
            foreach (var star in ActiveSystem.StarList)
            {
                var pos = star.SystemPosition;
                position_data.Add(new Vector3(3*(float)pos.X, 3*(float)pos.Y, 0.0f));
            }

            foreach(var planet in ActiveSystem.PlanetList)
            {
                var pos = planet.Position;
                position_data.Add(new Vector3(3*(float)pos.X, 3*(float)pos.Y, 0.0f));
            }
            temp_scenes.Add(new Scene(position_data, scale_data, mesh));
            scenes = temp_scenes;
            OnSceneLoaded();
            drawPending = true;
        }

        private void OnSceneLoaded()
        {
            EventHandler handler = SceneLoaded;
            if (null != handler) handler(this, EventArgs.Empty);
        }

        [NotifyPropertyChangedInvocator]
        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public void Refresh(bool partialRefresh = false)
        {

        }
    }

    public class Camera
    {
        public Vector3 Position = Vector3.Zero;
        public Vector3 Orientation = new Vector3(0f, 0f, (float)Math.PI);
        public float MoveSpeed = 0.2f;
        public float MouseSensitivity = 0.01f;

        public Matrix4 GetViewMatrix()
        {
            Vector3 lookat = new Vector3();

            lookat.X = (float)(Math.Sin((float)Orientation.X) * Math.Cos((float)Orientation.Y));
            lookat.Y = (float)Math.Sin((float)Orientation.Y);
            lookat.Z = (float)(Math.Cos((float)Orientation.X) * Math.Cos((float)Orientation.Y));

            return Matrix4.LookAt(Position, Position + lookat, Vector3.UnitY);
        }
    }

    public class Scene
    {
        public List<Vector3> position_data;
        public List<float> scale_data;
        public Mesh mesh;
        public bool IsInitialized = false;
        public int position_buffer_id;

        public Scene(List<Vector3> position_data, List<float> scale_data, Mesh mesh)
        {
            this.position_data = position_data;
            this.scale_data = scale_data;
            this.mesh = mesh;
        }
    }

    public class Mesh
    {
        public List<Vector3> vertices;
        public List<uint> indices;
        public int vertex_buffer_id;
        public int index_buffer_id;

        public Mesh(List<Vector3> vertices, List<uint> indices)
        {
            this.vertices = vertices;
            this.indices = indices;
        }
    }

    class Quad
    {

    }


}
