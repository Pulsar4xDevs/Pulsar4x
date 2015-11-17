

using System;
using Eto;
using Eto.Drawing;
using Eto.Forms;
using OpenTK.Graphics;
using OpenTK;

namespace Pulsar4X.CrossPlatformUI
{
    [Handler(typeof(GLSurface.IHandler))]
    public class GLSurface : Control
    {
	    public GLSurface() :
			this(GraphicsMode.Default)
	    {
	    }

	    public GLSurface(GraphicsMode graphicsMode):
			this(graphicsMode, 3, 0, GraphicsContextFlags.Default)
	    {
		    
	    }

	    public GLSurface(GraphicsMode mode, int major, int minor, GraphicsContextFlags flags)
	    {
		    this.Handler.CreateWithParams(mode, major, minor, flags);
		    this.Initialize();
	    }

        static GLSurface ()
        {
            RegisterEvent<GLSurface>(c => c.OnGLInitalized(null), GLInitializedEvent);
        }
        
        public const string GLShuttingDownEvent = "GL.ShuttingDown";
        public const string GLDrawNowEvent = "GL.DrawNow";
        public const string GLInitializedEvent = "GL.Initialized";

        //public event EventHandler<EventArgs> Click
        public event EventHandler<EventArgs> GLInitalized
        {
            add { this.Properties.AddHandlerEvent(GLInitializedEvent, value); }
            remove { this.Properties.RemoveEvent(GLInitializedEvent, value); }
        }
        public event EventHandler<EventArgs> GLDrawNow
        {
            add { this.Properties.AddHandlerEvent(GLDrawNowEvent, value); }
            remove { this.Properties.RemoveEvent(GLDrawNowEvent, value); }
        }
        public event EventHandler<EventArgs> GLShuttingDown
        {
            add { this.Properties.AddHandlerEvent(GLDrawNowEvent, value); }
            remove { this.Properties.RemoveEvent(GLDrawNowEvent, value); }
        }

        public virtual void OnGLInitalized(EventArgs e)
        {
            this.Properties.TriggerEvent(GLInitializedEvent, this, e);
        }
        private void OnDrawNow(object sender, EventArgs e)
        {
            this.Properties.TriggerEvent(GLInitializedEvent, this, e);
        }
        public virtual void OnShuttingDown(object obj, EventArgs e)
        {
            this.Properties.TriggerEvent(GLShuttingDownEvent, this, e);
        }



        private new IHandler Handler{get{return (IHandler)base.Handler;}}

        // interface to the platform implementations

        // ETO WIDGET -> Platform Control

        [AutoInitialize(false)]
        public new interface IHandler : Control.IHandler
		{
			void CreateWithParams(GraphicsMode mode, int major, int minor, GraphicsContextFlags flags);

            Size GLSize { get; set; }
            bool IsInitialized { get; }

            void MakeCurrent();
            void SwapBuffers();
        }

        public new interface ICallback : Control.ICallback
        {
            void OnInitialized(GLSurface w, EventArgs e);
            void OnShuttingDown(GLSurface w, EventArgs e);
            void OnDrawNow(GLSurface w, EventArgs e);
        }

        //PLATFORM CONTROL -> ETO WIDGET

        protected new class Callback : Control.Callback, ICallback
        {
            public void OnInitialized(GLSurface w, EventArgs e)
            {
                w.Platform.Invoke(() => w.OnGLInitalized(e));
            }

            public void OnShuttingDown(GLSurface w, EventArgs e)
            {
                w.Platform.Invoke(() => w.OnShuttingDown(w, e));
            }

            public void OnDrawNow(GLSurface w, EventArgs e)
            {
                w.Platform.Invoke(() => w.OnDrawNow(w, e));
            }
        }

        //Gets an instance of an object used to perform callbacks to the widget from handler implementations

        static readonly object callback = new Callback();

        protected override object GetCallback()
        {
            return callback;
        }

        //public event EventHandler Click;

        public Size GLSize {
            get { return this.Handler.GLSize; } 
            set { this.Handler.GLSize = value; }
        }

        public bool IsInitialized {
            get { return this.Handler.IsInitialized; }
        }

        public virtual void MakeCurrent() 
        {
            this.Handler.MakeCurrent ();
        }

        public virtual void SwapBuffers() 
        {
            this.Handler.SwapBuffers ();
        }
    }
}