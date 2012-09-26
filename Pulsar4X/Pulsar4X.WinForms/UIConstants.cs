using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Pulsar4X.WinForms
{
    ////////////////////////////////////////////////////////////////////////////////////////////////////
    /// <summary>   Container Class for all User interface constants. </summary>
    ///
    /// <remarks>   Gregory.nott, 17/08/2012. </remarks>
    ////////////////////////////////////////////////////////////////////////////////////////////////////
    public class UIConstants
    {
        /// <summary> The no of user interface tab pages </summary>
        public const int NO_OF_UI_TAB_PAGES = 10;

        /// <summary>
        /// Index and Names for the Main UI Tabs.
        /// </summary>
        public class UITabs
        {
            /// <summary>
            /// Index for Game Start tab.
            /// </summary>
            public const int        GAME_START_SCREEN_INDEX = 0;
            /// <summary>
            /// Name/Title For Game Start tab.
            /// </summary>
            public const string     GAME_START_SCREEN_NAME = "Start";

            /// <summary>
            /// Index for System Map tab.
            /// </summary>
            public const int SYSTEM_MAP_INDEX = 3;
            /// <summary>
            /// Name/Title For System Map tab.
            /// </summary>
            public const string SYSTEM_MAP_NAME = "System Map";

            /// <summary>
            /// Index for System Generation and Display tab.
            /// </summary>
            public const int        SYSTEM_GENERATION_AND_DISPLAY_INDEX = 9;
            /// <summary>
            /// Name/Title for System Generation and Display tab.
            /// </summary>
            public const string     SYSTEM_GENERATION_AND_DISPLAY_NAME = "System Generation and Display";
 
        }

        /// <summary>
        /// Texture File Paths.
        /// </summary>
        public class Textures
        {
            public const string DEFAULT_PLANET_ICON = "./Resources/Textures/DefaultIcon.png";
            public const string DEFAULT_TEXTURE = "./Resources/Textures/DefaultTexture.png";
            public const string DEFAULT_GLFONT = "./Resources/Fonts/DejaVuSansMonoBold.xml";
        }


        /// <summary> 
        /// The default string texture height per character 
        /// </summary>
        public const int DEFAULT_STRING_TEXTURE_HEIGHT_PER_CHAR = 128;

        /// <summary> 
        /// The default string texture width per character 
        /// </summary>
        public const int DEFAULT_STRING_TEXTURE_WIDTH_PER_CHAR = 128;



        /// <summary>
        /// About Box Text.
        /// </summary>
        public const string ABOUT_BOX_TEXT =    "Pulsar4X\n" +
                                                "A Fan work recreation of Aurora4x in C#\n" +
                                                "Alpha 1\n\n" +
                                                "Contributors:\n Antagonist, clement, HailRyan, Nathan, SnopyDogy, Sublight\n\n" +
                                                "Testers:\n joe \n\n" +
                                                "Hosting Provided by: Eric Luken \n\n" + 
                                                "Forums:\n http://aurora2.pentarch.org/index.php/board,169.0.html \n\n" +
                                                "Wiki:\n http://pulsar4x.pentarch.org/wiki/index.php?title=Main_Page \n\n" +
                                                "GitHub Org:\n https://github.com/Pulsar4xDevs \n\n" +
                                                "Left: Image of the Crab Nebula (a Pulsar ;), Credit to NASA and Hubble";


        public const float ZOOM_DEFAULT_SCALLER = 1.0f;
        public const float ZOOM_IN_FACTOR = 1.5f;
        public const float ZOOM_OUT_FACTOR = 0.75f;
        public const float ZOOM_MINIMUM_VALUE = 0.00000000186264515f;       // at this scale we have zoomed out soo much that a multy star system is only a couple of pixels wide!!
        public const float ZOOM_MAXINUM_VALUE = 32768.0f;                   // At this scale we are looking a 0.3m per pixel.
        public const float DEFAULT_PAN_AMOUNT = 25.0f;
        public static OpenTK.Vector2 DEFAULT_TEXT_SIZE = new OpenTK.Vector2(16, 16);


    }
}
