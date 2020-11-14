using ImGuiNET;

namespace XIVLauncherSpotify
{
    public class PluginUI
    {
        public bool IsVisible { get; set; }

        public void Draw()
        {
            if (!IsVisible)
                return;
        }
    }
}
