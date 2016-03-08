using OpenTK;
using Pulsar4X.ECSLib;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Pulsar4X.ViewModel.SystemView
{
    /// <summary>
    /// This is the VM for the opengl window, it handles updating the scene manager and triggering draws.
    /// Do not be cavalier about creating and destroying this vm, it should be long lived as it does a lot
    /// of caching and building out data structures.
    /// </summary>
    public class RenderVM : IViewModel
    {
        private AuthenticationToken _authToken;
        private int viewport_width;
        private int viewport_height;
        private SystemVM active_system;
        public SystemVM ActiveSystem {
            get { return active_system; }
            set {
                active_system = value;
                LoadSystem(active_system);
            }
        }

        public Dictionary<String, Scene> scenes;

        public bool drawPending = false;

        public RenderVM(AuthenticationToken authToken)
        {
            scenes = new Dictionary<String, Scene>();
        }

        public void Resize(int viewport_width, int viewport_height)
        {
            this.viewport_width = viewport_width;
            this.viewport_height = viewport_height;
            foreach(var scene in scenes)
            {
                scene.Value.camera.UpdateProjectionMatrix((float)this.viewport_width, (float)this.viewport_height);
                drawPending = true;
            }
        }

        public void UpdateCameraPosition(Vector2 delta)
        {
            if (scenes.ContainsKey("system_space"))
            {
                var scene = scenes["system_space"];
                scene.camera.Move(delta);
                drawPending = true;
            }
        }

        public void UpdateCameraZoom(int delta)
        {
            if (scenes.ContainsKey("system_space"))
            {
                var scene = scenes["system_space"];
                scene.camera.Zoom(delta);
                drawPending = true;
            }
        }

        public event EventHandler SceneLoaded;
        public event PropertyChangedEventHandler PropertyChanged;

        private void LoadSystem(SystemVM system)
        {
            //create our quad --- TODO: Move this out into a structure or factory
            var vertices = new List<Vector3>();
            var indices = new List<uint>();
            Vector3[] tmp_vectors = {
                    new Vector3(-0.1f, 0.1f, 0f),
                    new Vector3(-0.1f, -0.1f, 0f),
                    new Vector3(0.1f, -0.1f, 0f),
                    new Vector3(0.1f, 0.1f, 0f)
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
            var position_data = new List<Vector3>();
            var scale_data = new List<float>();
            var cam = new Camera(viewport_width, viewport_height);
            cam.Position = new Vector3(
                            (float)ActiveSystem.ParentStar.Position.X,
                            (float)ActiveSystem.ParentStar.Position.Y,
                            -1f
                           );
            foreach (var star in ActiveSystem.StarList)
            {
                var pos = star.Position;
                position_data.Add(new Vector3((float)pos.X, (float)pos.Y, 0.0f));
            }

            foreach(var planet in ActiveSystem.PlanetList)
            {
                var pos = planet.Position;
                position_data.Add(new Vector3((float)pos.X, (float)pos.Y, 0.0f));
            }
            //scenes.Add("system_space", new Scene(position_data, scale_data, mesh, cam));
            scenes.Add("system_space", new Scene(system.StarSystem, _authToken, scale_data, cam));
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
        private Vector3 position = Vector3.Zero;
        public Vector3 Position {
            get { return position; }
            set {
                view_matrix_dirty = true;
                position = value;
            }
        }
        private Vector3 lookat = new Vector3(0f, 0f, 0f);
        public Vector3 LookAt {
            get { return lookat; }
            set {
                view_matrix_dirty = true;
                lookat = value;
            }
        }
        private Vector3 up = new Vector3(0, 1f, 0f);
        public float MoveSpeed = 0.2f;
        public float MouseSensitivity = 0.01f;
        private Matrix4 projection_matrix;
        private bool view_matrix_dirty = true;
        private Matrix4 view_matrix;
        private Matrix4 view_projection_matrix;

        public Camera(int viewport_width, int viewport_height)
        {
            UpdateProjectionMatrix(viewport_width, viewport_height);
        }

        public Matrix4 GetViewProjectionMatrix()
        {
            if (view_matrix_dirty)
            {
                //because we move the world and not the camera we invert the vector
                //so we can think as if we are moving the camera
                view_matrix = Matrix4.LookAt(position, new Vector3(position.X, position.Y, 0f), Vector3.UnitY);
                view_projection_matrix = view_matrix * projection_matrix;
                view_matrix_dirty = false;
            }
            return view_projection_matrix;
        }

        public void Move(Vector2 delta)
        {
            //first convert the delta to world space units
            var delta3 = new Vector3(delta.X, delta.Y, 0);
            //delta3 = Vector3.Transform(delta3, view_matrix);

            //we go in reverse because we're actually transforming the world
            position += delta3 * MoveSpeed * MouseSensitivity * position.Z;
            view_matrix_dirty = true;
        }

        public void Zoom(int delta)
        {
            position.Z += delta*0.1f;
            view_matrix_dirty = true;
        }

        internal void UpdateProjectionMatrix(float viewport_width, float viewport_height)
        {
            projection_matrix = Matrix4.CreatePerspectiveFieldOfView((float)(2 * Math.PI / 3), viewport_width / viewport_height, 0.1f, 1000f);
            //projection_matrix = Matrix4.CreateOrthographic(viewport_width, viewport_height, -10f, 10000f);
            view_matrix_dirty = true;
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
