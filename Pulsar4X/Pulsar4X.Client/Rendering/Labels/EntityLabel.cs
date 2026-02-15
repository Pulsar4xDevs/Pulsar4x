using Pulsar4X.Client.Interface.Widgets;
using Pulsar4X.Engine;
using Pulsar4X.Factions;
using Pulsar4X.Interfaces;
using Pulsar4X.Messaging;
using Pulsar4X.Movement;
using Pulsar4X.Names;
using Pulsar4X.Input;
using SDL3;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Numerics;
using System.Threading.Tasks;
using System;

namespace Pulsar4X.Client
{
    public class EntityLabel : IPointerHandler, IShape, IInteractable
    {
        private Entity _entity;
        public Entity Entity {
            get { return _entity; }
        }

        public byte Priority { get { return 120; } }

        protected string? _starSysGuid;

        private NameDB? _nameDB = null;
        private PositionDB? _positionDB = null;

        protected virtual void DrawExt(IntPtr rendererPtr, Camera camera) {}

        private SDL.Color _color;
        protected string _name = "??";

        public RectangleF Rect = new ();

        private int _faction = Game.NeutralFactionId;
        public int Faction {
            set {
                _faction = value;
                _name = _nameDB.GetName(_faction);
            }
            get {
                return _faction;
            }
        }

        private Task OnEntityRenamed(Message message)
        {
            _name = _nameDB.GetName(_faction);
            return Task.CompletedTask;
        }

        public EntityLabel(Entity entity)
        {
            _entity = entity;

            if (entity.TryGetDataBlob<NameDB>(out NameDB i))
                _nameDB = i;
            if (entity.TryGetDataBlob<PositionDB>(out PositionDB j))
                _positionDB = j;

            // TODO: better colors
            var clr = (_entity.FactionOwnerID == Game.NeutralFactionId) ?
                Styles.NeutralColor :
                Styles.StandardText;
            _color = Helpers.Vector4ToSDLColor(clr);

            if(entity.Manager != null)
            {
                StarSystem starSys = (StarSystem)entity.Manager;
                _starSysGuid = starSys.ID;
            }

            Rect.Height = SDL3.TTF.GetFontHeight(Styles.SDLDefaultFont);

            // Subscribe to name changes
            Func<Message, bool> filterById = msg => msg.EntityId == _entity.Id;
            MessagePublisher.Instance.Subscribe(MessageTypes.EntityRenamed, OnEntityRenamed, filterById);
        }

        protected GlobalUIState? _state = null;
        public void AttachState(GlobalUIState state)
        {
            _state = state;
        }

        private bool _hovered = false;
        public virtual bool OnPointerEnter(SDL.Event sevent)
        {
            _hovered = true;
            return true;
        }
        public virtual bool OnPointerExit(SDL.Event sevent)
        {
            /* If pointer moves moves out of a label and then comes back while
             * the button is still pressed, then OnPointerUp does still fire
             * even though _pressed is false. It's kinda difficult to do that,
             * unless you're doing it on purpose. It doesn't break anything,
             * but the label doesn't change to the correct color.
             */
            _pressed = false;

            _hovered = false;
            return true;
        }

        private bool _pressed = false;
        public virtual bool OnPointerDown(SDL.Event sevent)
        {
            _pressed = true;
            return true;
        }
        public virtual bool OnPointerUp(SDL.Event sevent)
        {
            _pressed = false;

            if (_state == null || _starSysGuid == null)
                return true; // Still mark handled
            var state = _state!;
            var starSys = _starSysGuid!;

            if (sevent.Button.Button == 1)
                state.EntityClicked(Entity.Id, starSys, MouseButtons.Primary);
            else if (sevent.Button.Button == 3)
                state.EntityClicked(Entity.Id, starSys, MouseButtons.Alt);
            return true;
        }

        public virtual bool Contains(System.Drawing.PointF point)
        {
            return Rect.Contains(point);
        }

        public void OnFrameUpdate(Matrix matrix, Camera camera)
        {
            int h;
            int w;
            SDL3.TTF.GetStringSize(Styles.SDLDefaultFont, _name, 0, out w, out h);
            Rect.Width = w;

            var point = camera.ViewCoordinate_m(_positionDB.AbsolutePosition);
            Rect.X = point.X;
            Rect.Y = point.Y;
        }

        public void Draw(IntPtr rendererPtr, Camera camera)
        {
            if (rendererPtr == IntPtr.Zero ||
                    ! camera.IsOnScreen(Rect.X, Rect.Y, Rect.Width, Rect.Height))
                return;

            // TODO: Move these somewhere else
            SDL.Color tp = new () {
                R = 0,
                G = 0,
                B = 0,
                A = 0
            };
            SDL.Color pressclr = new () {
                R = 128,
                G = 255,
                B = 0,
                A = 255 / 2
            };
            SDL.Color hoverclr = new () {
                R = 0,
                G = 128,
                B = 128,
                A = 255 / 2
            };

            IntPtr surface = SDL3.TTF.RenderTextShaded(
                    Styles.SDLDefaultFont,
                    _name,
                    0,
                    _color,
                    (_pressed) ? pressclr : (_hovered) ? hoverclr : tp);

            if (surface == IntPtr.Zero) {
                Trace.WriteLine("EntityLabel: failed to create surface");
                return;
            }

            IntPtr texture = SDL.CreateTextureFromSurface(rendererPtr, surface);

            if (texture == IntPtr.Zero) {
                SDL.DestroySurface(surface);

                Trace.WriteLine("EntityLabel: failed to create texture from surface");
                return;
            }

            SDL.FRect frect = new () {
                X = (int)Rect.X,
                Y = (int)Rect.Y,
                W = Rect.Width,
                H = Rect.Height
            };

            SDL.RenderTexture(rendererPtr, texture, IntPtr.Zero, in frect);

            SDL.DestroyTexture(texture);
            SDL.DestroySurface(surface);

            DrawExt(rendererPtr, camera);
        }

        // TODO: Calculate this based on icon size. Option for top, bottom, left, right maybe?
        public void ApplyIconOffset() {
            var icon = new SizeF(-Rect.Width / 2, Rect.Height);
            Rect.Location = PointF.Add(Rect.Location, icon);
        }
    }
}
