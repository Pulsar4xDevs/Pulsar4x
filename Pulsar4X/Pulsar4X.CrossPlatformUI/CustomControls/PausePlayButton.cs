using Eto;
using Eto.Drawing;
using Eto.Forms;
using OpenTK.Graphics;
using System;
using System.Collections.Generic;

namespace Pulsar4X.CrossPlatformUI.CustomControls
{
    public class PausePlayButton : Button
    {
        /// <summary>
        /// List of available front Images for Pause/Play, enabled/disabled
        /// </summary>
        public readonly List<Image> frontImage = new List<Image>();
        private bool _isPaused = true;
        /// <summary>
        /// Gets/Sets if the current Button state is on paused: Changes the front image accordingly
        /// </summary>
        public bool isPaused {
            get
            {
                return _isPaused;
            }
            set
            {
                _isPaused = value;
                int imgSelector = Convert.ToInt32(isPaused) * 2 + Convert.ToInt32(Enabled);
                Image = frontImage[imgSelector];
            }
        }
        /// <summary>
        /// Overrides the parent Enabled variable with additionally changing the Image
        /// </summary>
        public override bool Enabled
        {
            get
            {
                return base.Enabled;
            }

            set
            {
                base.Enabled = value;
                int imgSelector = Convert.ToInt32(isPaused) * 2 + Convert.ToInt32(Enabled);
                Image = frontImage[imgSelector];
            }
        }

        /// <summary>
        /// Constructor that is loading the respective images for each state
        /// </summary>
        public PausePlayButton() : base()
        {
            frontImage.Add(Icon.FromResource("Pulsar4X.CrossPlatformUI.Resources.Icons.PausePlayButton.PauseDisabled.ico"));
            frontImage.Add(Icon.FromResource("Pulsar4X.CrossPlatformUI.Resources.Icons.PausePlayButton.Pause.ico"));
            frontImage.Add(Icon.FromResource("Pulsar4X.CrossPlatformUI.Resources.Icons.PausePlayButton.PlayDisabled.ico"));
            frontImage.Add(Icon.FromResource("Pulsar4X.CrossPlatformUI.Resources.Icons.PausePlayButton.Play.ico"));
            ImagePosition = ButtonImagePosition.Overlay;
        }

    }
}
