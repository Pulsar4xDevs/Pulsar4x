using Eto.Forms;
using Eto.Serialization.Json;

namespace Pulsar4X.CrossPlatformUI.Views
{
    public class Startup : Panel
    {
        public Startup()
        {
            JsonReader.Load(this);
        }
    }
}
