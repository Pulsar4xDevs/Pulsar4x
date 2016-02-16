

using Eto;
using Eto.Drawing;
using Eto.Forms;
using OpenTK.Graphics;
using System;

namespace Pulsar4X.CrossPlatformUI
{
    [Handler(typeof(RenderCanvas.IHandler))]
    public class RenderCanvas : Control
    {
	    public RenderCanvas() :
			this(GraphicsMode.Default)
	    {
	    }

	    public RenderCanvas(GraphicsMode graphicsMode):
			this(graphicsMode, 3, 0, GraphicsContextFlags.Default)
	    {
		    
	    }

	    public RenderCanvas(GraphicsMode mode, int major, int minor, GraphicsContextFlags flags)
	    {
		    this.Handler.CreateWithParams(mode, major, minor, flags);
		    this.Initialize();
	    }

        static RenderCanvas ()
        {
            RegisterEvent<RenderCanvas>(c => c.OnGLInitalized(null), GLInitializedEvent);
            //RegisterEvent<RenderCanvas>(c => c.OnDrawNow(null, null), GLDrawNowEvent);
        }
        
        public const string GLShuttingDownEvent = "GL.ShuttingDown";
        public const string GLDrawNowEvent = "GL.DrawNow";
        public const string GLInitializedEvent = "GL.Initialized";
        public const string GLResizeEvent = "GL.Resize";

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
        public event EventHandler<EventArgs> GLResize
        {
            add { this.Properties.AddHandlerEvent(GLResizeEvent, value); }
            remove { this.Properties.RemoveEvent(GLResizeEvent, value); }
        }
        public event EventHandler<EventArgs> GLShuttingDown
        {
            add { this.Properties.AddHandlerEvent(GLShuttingDownEvent, value); }
            remove { this.Properties.RemoveEvent(GLShuttingDownEvent, value); }
        }

        public virtual void OnGLInitalized(EventArgs e)
        {
            this.Properties.TriggerEvent(GLInitializedEvent, this, e);
        }
        private void OnDrawNow(object sender, EventArgs e)
        {
            this.Properties.TriggerEvent(GLDrawNowEvent, this, e);
        }
        private void OnResize(object sender, EventArgs e)
        {
            this.Properties.TriggerEvent(GLResizeEvent, this, e);
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
            void OnInitialized(RenderCanvas w, EventArgs e);
            void OnShuttingDown(RenderCanvas w, EventArgs e);
            void OnDrawNow(RenderCanvas w, EventArgs e);
            void OnResize(RenderCanvas w, EventArgs e);
        }

        //PLATFORM CONTROL -> ETO WIDGET

        protected new class Callback : Control.Callback, ICallback
        {
            public void OnInitialized(RenderCanvas w, EventArgs e)
            {
                w.Platform.Invoke(() => w.OnGLInitalized(e));
            }

            public void OnShuttingDown(RenderCanvas w, EventArgs e)
            {
                w.Platform.Invoke(() => w.OnShuttingDown(w, e));
            }

            public void OnDrawNow(RenderCanvas w, EventArgs e)
            {
                w.Platform.Invoke(() => w.OnDrawNow(w, e));
            }

            public void OnResize(RenderCanvas w, EventArgs e)
            {
                w.Platform.Invoke(() => w.OnResize(w, e));
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