using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Pulsar4X.UI
{
    /// <summary>
    /// Constant UI Values
    /// </summary>
    public class UIConstants
    {
        /// <summary>
        /// About Box Text.
        /// </summary>
        public const string ABOUT_BOX_TEXT = "Pulsar4X\n" +
                                                "A Fan work recreation of Aurora4x in C#\n" +
                                                "Alpha 2\n\n" +
                                                "Contributors:\n Antagonist, Clement, HailRyan, Nathan, SnopyDogy, Sublight, Se5a\n\n" +
                                                "Testers:\n joe \n\n" +
                                                "Hosting Provided by: Erik Luken \n\n" +
                                                "Forums:\n http://aurora2.pentarch.org/index.php/board,169.0.html \n\n" +
                                                "Wiki:\n http://pulsar4x.pentarch.org/wiki/index.php?title=Main_Page \n\n" +
                                                "GitHub Org:\n https://github.com/Pulsar4xDevs \n\n" +
                                                "Thanks to:\n Brock Greman for planetary motion examples \n\n" +
                                                "";

        /// <summary> 
        /// The default string texture height per character 
        /// </summary>
        public const int DEFAULT_STRING_TEXTURE_HEIGHT_PER_CHAR = 128;

        /// <summary> 
        /// The default string texture width per character 
        /// </summary>
        public const int DEFAULT_STRING_TEXTURE_WIDTH_PER_CHAR = 128;

        /// <summary> Default Zoom level. </summary>
        public const float ZOOM_DEFAULT_SCALLER = 1.0f;

        /// <summary> the rate at which we Zoom in. </summary>
        public const float ZOOM_IN_FACTOR = 1.5f;

        /// <summary> the rate at which zoom out </summary>
        public const float ZOOM_OUT_FACTOR = 0.75f;

        /// <summary> minimum zoom level, at this scale 1 pixel = ~3,542AU  </summary>
        public const float ZOOM_MINIMUM_VALUE = 0.0005f;
        /// <summary> maximum zoom level, at this scale 1 pixel = ~2.6Km  </summary> 
        public const float ZOOM_MAXINUM_VALUE = 100000000.0f;

        /// <summary> the rate at which we panm the screen (in pixels)  </summary> 
        public const float DEFAULT_PAN_AMOUNT = 25.0f;

        /// <summary> Default size of Text labels on the System map.  </summary> 
        public static OpenTK.Vector2 DEFAULT_TEXT_SIZE = new OpenTK.Vector2(16, 16);

        /// <summary> Pulsar Logo file path.  </summary> 
        public const string PULSAR4X_LOGO = "./Resources/Textures/PulsarLogo.png";

        /// <summary>
        /// Texture File Paths.
        /// </summary>
        public class Textures
        {
            public const string DEFAULT_PLANET_ICON = "./Resources/Textures/DefaultIcon.png";
            public const string DEFAULT_TASKGROUP_ICON = "./Resources/Textures/DefaultTGIcon.png";
            public const string DEFAULT_JUMPPOINT_ICON = "./Resources/Textures/DefaultJPIcon.png";
            public const string DEFAULT_TEXTURE = "./Resources/Textures/DefaultTexture.png";
            public const string DEFAULT_GLFONT = "./Resources/Fonts/PulsarFont.xml";
            public const string DEFAULT_GLFONT2 = "./Resources/Fonts/DejaVuSansMonoBold.xml";

        }

        public static class EconomicsPage
        {
            /// <summary>
            /// Construction ID and Types are for the industrial tab combo box. this should probably just be moved to Constants.cs proper and not be here.
            /// </summary>
            public enum ConstructionID
            {
                Installations,
                Missiles,
                Fighters,
                BasicComponents,
                ElectronicShieldComponents,
                EngineComponents,
                SensorsFCComponents,
                TransportIndustryComponents,
                WeaponsSupportComponents,
                BuildPDCOrbitalHabitat,
                PrefabPDC,
                AssemblePDC,
                RefitPDC,
                MaintenanceSupplies,
                Count
            }
            public static String[] ConstructionTypes = { "Installations", "Missiles", "Fighters", "Basic Components", "Electronic / Shield Components", "Engine / Jump Engine Components", 
                                                         "Sensor / Fire Control Components", "Transport / Industry Components", "Weapon / Support Components", 
                                                         "Build PDC / Orbital Habitat", "Prefab PDC", "Assemble PDC", "Refit PDC", "Maintenance Supplies" };   
        }

        public static class Armor
        {
            public static String[] ArmorTypes = { "Conventional", "Duranium", "High Density Duranium", "Composite", "Ceramic Composite",
                                                  "Laminate Composite", "Compressed Carbon", "Biphased Carbide", "Crystaline Composite", "Superdense", "Bonded Superdense",
                                                  "Coherent Superdense", "Collapsium" };
        }

    }
}
