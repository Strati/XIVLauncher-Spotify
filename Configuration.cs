using Dalamud.Configuration;
using Dalamud.Plugin;
using Newtonsoft.Json;
using SpotifyAPI.Web;

namespace XIVLauncherSpotify
{
    public class Configuration : IPluginConfiguration
    {

        public string token { get; set; }
        public int Version { get; set; }

        // Add any other properties or methods here.
        [JsonIgnore] private DalamudPluginInterface pluginInterface;

        public void Initialize(DalamudPluginInterface pluginInterface)
        {
            this.pluginInterface = pluginInterface;
        }

        public void Save()
        {
            this.pluginInterface.SavePluginConfig(this);
        }
    }
}
