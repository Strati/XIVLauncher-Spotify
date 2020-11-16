using System;
using Dalamud.Plugin;
using XIVLauncherSpotify.Attributes;
using SpotifyAPI.Web;
using SpotifyAPI.Web.Auth;
using SpotifyAPI.Web.Models;
using SpotifyAPI.Web.Enums;
using System.Diagnostics;
using System.Net;
using System.Threading.Tasks;

namespace XIVLauncherSpotify
{
        public class Plugin : IDalamudPlugin
        {
            private DalamudPluginInterface pluginInterface;
            private PluginCommandManager<Plugin> commandManager;
            private Configuration config;
            private PluginUI ui;
            private static SpotifyWebAPI _spotify;


        public string Name => "Connect with Spotify";

            public void Initialize(DalamudPluginInterface pluginInterface)
            {
                this.pluginInterface = pluginInterface;

                this.config = (Configuration)this.pluginInterface.GetPluginConfig() ?? new Configuration();
                this.config.Initialize(this.pluginInterface);

                this.ui = new PluginUI();
                this.pluginInterface.UiBuilder.OnBuildUi += this.ui.Draw;

                this.commandManager = new PluginCommandManager<Plugin>(this, this.pluginInterface);
                
            }

        private bool IsAuthed()
        {
            if (config.token == null)
            {
                return false;
            }
            HttpWebRequest request = null;
            request = (HttpWebRequest)WebRequest.Create("https://api.spotify.com/v1/artists/6NtwaHZLhTUvERKFbFqu8S");
            request.Accept = "application/json";
            request.ContentType = "application/json";
            request.Headers.Add("Authorization: Bearer " + config.token);
            request.Method = "GET";
            try 
            {
                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            }
            catch (Exception)
            {
                return false;
            }
            return true;
        }


        private async void SpotifyAuth()
        {
            if (IsAuthed()) {
                return;
            }

            String ClientId = "2993fe290e1744158bdec14aa8016ebd";            

            ImplicitGrantAuth auth = new ImplicitGrantAuth(
              ClientId,
              "http://localhost:4002",
              "http://localhost:4002",
              Scope.UserReadPlaybackState | Scope.UserModifyPlaybackState | Scope.UserReadCurrentlyPlaying
            );
            auth.AuthReceived += async (sender, payload) =>
            {
                auth.Stop();
                _spotify = new SpotifyWebAPI()
                {
                    TokenType = payload.TokenType,
                    AccessToken = payload.AccessToken
                };
                config.token = _spotify.AccessToken;
                config.Save();
            };
            auth.Start();
            var chat = this.pluginInterface.Framework.Gui.Chat;
            chat.Print($"Popping web browser to authenticate...");
            Process.Start(auth.GetUri());
        }
    

        [Command("/xlsnp")]
        [HelpMessage("Display currently playing song.")]
        public async Task GetTrackname(string command, string args)
        {
            SpotifyAuth();
            var chat = this.pluginInterface.Framework.Gui.Chat;
            PlaybackContext context = await _spotify.GetPlayingTrackAsync();
            if (context.Item != null)
            {
                String songname = context.Item.Name;
                String artists = "";
                for (int i = 0; i < context.Item.Artists.Count; i++)
                {
                    if (i == context.Item.Artists.Count - 1) artists += context.Item.Artists[i].Name;
                    else artists += context.Item.Artists[i].Name + " - ";
                }
                chat.Print($"Now Playing: {artists} - {songname}");
            } else
            {
                chat.Print($"Not playing");
            }
        }

        [Command("/xlsv")]
        [HelpMessage("Set volume.")]
        public async Task SetVolume(string command, string args)
        {
            SpotifyAuth();
            var chat = this.pluginInterface.Framework.Gui.Chat;
            if (!int.TryParse(args, out var volume)) return;
            if (volume > 100 || volume < 0) return;
            ErrorResponse error = await _spotify.SetVolumeAsync(volume);
            chat.Print($"Spotify volume set to {volume}");
        }

        [Command("/xlsprevious")]
        [HelpMessage("Go to previous track.")]
        public async Task SeekPrevious(string command, string args)
        {
            SpotifyAuth();
            var chat = this.pluginInterface.Framework.Gui.Chat;
            ErrorResponse error = await _spotify.SkipPlaybackToPreviousAsync();
            chat.Print($"Seeking to previous track.");
        }

        [Command("/xlsnext")]
        [HelpMessage("Go to previous track.")]
        public async Task SeekNext(string command, string args)
        {
            SpotifyAuth();
            var chat = this.pluginInterface.Framework.Gui.Chat;
            ErrorResponse error = await _spotify.SkipPlaybackToNextAsync();
            chat.Print($"Seeking to next track.");
        }

        [Command("/xlsrestart")]
        [HelpMessage("Restart current track.")]
        public async Task SeekRestart(string command, string args)
        {
            SpotifyAuth();
            var chat = this.pluginInterface.Framework.Gui.Chat;
            ErrorResponse error = await _spotify.SeekPlaybackAsync(0);
            chat.Print($"Restarting current track.");
        }

        [Command("/xlstoggleplayback")]
        [HelpMessage("Toggle play/pause.")]
        public async Task TogglePlayback(string command, string args)
        {
            SpotifyAuth();
            var chat = this.pluginInterface.Framework.Gui.Chat;
            PlaybackContext context = await _spotify.GetPlaybackAsync();
            if (context.IsPlaying == true)
            {
                ErrorResponse error = await _spotify.PausePlaybackAsync();
                chat.Print($"Playback paused.");
            } else
            {
                ErrorResponse error = await _spotify.ResumePlaybackAsync(offset: "");
                chat.Print($"Playback resumed.");
            }
        }


        #region IDisposable Support
        protected virtual void Dispose(bool disposing)
            {
                if (!disposing) return;

                this.commandManager.Dispose();

                this.pluginInterface.SavePluginConfig(this.config);

                this.pluginInterface.UiBuilder.OnBuildUi -= this.ui.Draw;

                this.pluginInterface.Dispose();
            }

            public void Dispose()
            {
                Dispose(true);
                GC.SuppressFinalize(this);
            }
            #endregion
        }

}



