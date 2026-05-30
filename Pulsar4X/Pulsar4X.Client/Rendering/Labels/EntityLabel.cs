using Pulsar4X.Client.Interface.Widgets;
using Pulsar4X.Engine;
using Pulsar4X.Factions;
using Pulsar4X.Galaxy;
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
        private MassVolumeDB? _massVolumeDB = null;

        protected virtual void DrawExt(IntPtr rendererPtr, Camera camera) {}
        protected virtual void OnFrameUpdateExt(Matrix matrix, Camera camera) {}

        private SDL.Color _color;
        protected string _name = "??";

        private IntPtr _nameTexture = IntPtr.Zero;
        protected SDL.FRect _nameRect = new ();

        public RectangleF Rect = new ();

        // Start of the diagonal leader (just outside the body) and the elbow
        // where the 45° leader meets the horizontal underline beneath the label.
        private float _lineStartX;
        private float _lineStartY;
        private float _elbowX;
        private float _elbowY;

        // Minimum perpendicular offset (px) of the elbow from the body center,
        // extra gap added on top of the on-screen body radius so the leader
        // clears the body when zoomed in, and the small visible gap between
        // the body edge and the start of the leader.
        private const float MinLeaderOffset = 12f;
        private const float BodyEdgeGap = 18f;
        private const float BodyLineGap = 8f;

        private uint _padding = 0;
        public uint Padding {
            set {
                _padding = value;
                OnPaddingUpdate();
            }
            get {
                return _padding;
            }
        }

        private int _faction = Game.NeutralFactionId;
        public int Faction {
            set {
                _faction = value;
                OnEntityRenamed(null);
            }
            get {
                return _faction;
            }
        }

        private void OnPaddingUpdate()
        {
            Rect.Width = _nameRect.W + _padding * 2;
            Rect.Height = _nameRect.H + _padding * 2;
        }

        private Task OnEntityRenamed(Message message)
        {
            if (_nameDB != null)
                _name = _nameDB.GetName(_faction);

            if (Styles.SDLDefaultFont != IntPtr.Zero && !string.IsNullOrEmpty(_name))
            {
                SDL3.TTF.GetStringSize(Styles.SDLDefaultFont, _name, 0, out int w, out _);
                _nameRect.W = w;
            }

            OnPaddingUpdate();

            DestroyName();

            return Task.CompletedTask;
        }

        private void DestroyName()
        {
            if (_nameTexture == IntPtr.Zero)
                return;

            var p = _nameTexture;
            _nameTexture = IntPtr.Zero;
            SDL.DestroyTexture(p);
        }

        public EntityLabel(Entity entity)
        {
            _entity = entity;

            if (entity.TryGetDataBlob<NameDB>(out NameDB i))
                _nameDB = i;
            if (entity.TryGetDataBlob<PositionDB>(out PositionDB j))
                _positionDB = j;
            if (entity.TryGetDataBlob<MassVolumeDB>(out MassVolumeDB k))
                _massVolumeDB = k;

            SetColor();

            if(entity.Manager != null)
            {
                StarSystem starSys = (StarSystem)entity.Manager;
                _starSysGuid = starSys.ID;
            }

            if (Styles.SDLDefaultFont != IntPtr.Zero)
                _nameRect.H = SDL3.TTF.GetFontHeight(Styles.SDLDefaultFont);
            OnEntityRenamed(null);

            // Subscribe to name changes
            Func<Message, bool> filterById = msg => msg.EntityId == _entity.Id;
            MessagePublisher.Instance.Subscribe(MessageTypes.EntityRenamed, OnEntityRenamed, filterById);
        }

        ~EntityLabel()
        {
            DestroyName();
        }

        private void SetColor()
        {
            _color = (_entity.FactionOwnerID == Game.NeutralFactionId) ?
                Styles.NeutralColor.ToSDLColor() :
                (_state != null && _entity.FactionOwnerID != _state.Faction?.Id) ?
                Styles.BadColor.ToSDLColor() :
                Styles.Theme.Text.ToSDLColor();
        }

        protected GlobalUIState? _state = null;
        public void AttachState(GlobalUIState state)
        {
            _state = state;
            SetColor();
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
            var point = camera.ViewCoordinate_m(_positionDB.AbsolutePosition);

            float anchorX = (float)point.X;
            float anchorY = (float)point.Y;

            // Diagonal distance from body center to the elbow must clear the
            // body's on-screen radius. The leader rises by `offset` in both X
            // and Y, so its length along the diagonal is offset*sqrt(2).
            float viewRadius = _massVolumeDB != null
                ? camera.ViewDistance(_massVolumeDB.RadiusInAU)
                : 0f;
            const float invSqrt2 = 0.70710678f;
            float offset = MathF.Max(MinLeaderOffset, (viewRadius + BodyEdgeGap) * invSqrt2);

            // Start the leader a few pixels past the body edge so it doesn't touch.
            float startOffset = (viewRadius + BodyLineGap) * invSqrt2;
            _lineStartX = anchorX + startOffset;
            _lineStartY = anchorY + startOffset;

            // 45° leader down-right from the body, then horizontal under the label.
            _elbowX = anchorX + offset;
            _elbowY = anchorY + offset;

            _nameRect.X = (int)_elbowX;
            _nameRect.Y = (int)(_elbowY - _nameRect.H);

            Rect.Location = new (_nameRect.X - Padding, _nameRect.Y - Padding);

            OnFrameUpdateExt(matrix, camera);
        }

        private bool RenderName(IntPtr rendererPtr)
        {
            IntPtr textSurface = SDL3.TTF.RenderTextSolid(
                    Styles.SDLDefaultFont,
                    _name,
                    0,
                    _color);

            if (textSurface == IntPtr.Zero) {
                Trace.WriteLine("EntityLabel: failed to create surface");
                return false;
            }

            _nameTexture = SDL.CreateTextureFromSurface(rendererPtr, textSurface);

            if (_nameTexture == IntPtr.Zero) {
                SDL.DestroySurface(textSurface);

                Trace.WriteLine("EntityLabel: failed to create texture from surface");
                return false;
            }

            SDL.DestroySurface(textSurface);

            return true;
        }

        public void Draw(IntPtr rendererPtr, Camera camera)
        {
            if (rendererPtr == IntPtr.Zero ||
                    ! camera.IsOnScreen(Rect.X, Rect.Y, Rect.Width, Rect.Height))
                return;

            if (_pressed || _hovered)
            {
                byte r, g, b, a;
                SDL.GetRenderDrawColor(rendererPtr, out r, out g, out b, out a);

                if (_pressed)
                {
                    var c = Styles.Theme.ButtonActive;
                    SDL.SetRenderDrawColor(rendererPtr, c.R, c.G, c.B, c.A);
                }
                else if (_hovered)
                {
                    var c = Styles.Theme.ButtonHovered;
                    SDL.SetRenderDrawColor(rendererPtr, c.R, c.G, c.B, c.A);
                }

                SDL.FRect frect = new () {
                    X = Rect.X,
                    Y = Rect.Y,
                    W = Rect.Width,
                    H = Rect.Height
                };

                SDL.RenderFillRect(rendererPtr, frect);

                SDL.SetRenderDrawColor(rendererPtr, r, g, b ,a);
            }

            if (_nameTexture == IntPtr.Zero && ! RenderName(rendererPtr))
                return; // failure

            // Leader line: diagonal from body at 45° down-right, then horizontal under the label.
            byte lr, lg, lb, la;
            SDL.GetRenderDrawColor(rendererPtr, out lr, out lg, out lb, out la);
            SDL.SetRenderDrawColor(rendererPtr, _color.R, _color.G, _color.B, _color.A);
            SDL.RenderLine(rendererPtr, _lineStartX, _lineStartY, _elbowX, _elbowY);
            SDL.RenderLine(rendererPtr, _elbowX, _elbowY, _elbowX + _nameRect.W, _elbowY);
            SDL.SetRenderDrawColor(rendererPtr, lr, lg, lb, la);

            SDL.RenderTexture(rendererPtr, _nameTexture, IntPtr.Zero, in _nameRect);

            DrawExt(rendererPtr, camera);
        }
    }
}
