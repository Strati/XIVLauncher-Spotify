using System;
using Dalamud.Plugin;
using XIVLauncherSpotify.Attributes;
using SpotifyAPI.Web;
using SpotifyAPI.Web.Auth;
using System.Threading.Tasks;
using SpotifyAPI.Web.Models;
using SpotifyAPI.Web.Enums;
using System.Text;
using System.Diagnostics;

namespace XIVLauncherSpotify
{
        public class Plugin : IDalamudPlugin
        {
            private DalamudPluginInterface pluginInterface;
            private PluginCommandManager<Plugin> commandManager;
            private Configuration config;
            private PluginUI ui;
            private static SpotifyWebAPI _spotify;
        private static String oauth = "BQDcAsClsOtcVfZljXceuSvn2uOP8Ze32GuCsFEyXFh2dP6NeZxmp2dqtBFMDfpJdz6GoHxBvArTaAqCb1H-8Z2Orb456GCq-kVDixg_H1DlAcmePxFaiVTfOxspQKhVCRdjWDbD1upKAAJtnZUAOWFBjeHWU2So4lbKOAO33o-K";


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

        static async void SpotifyAuth(string[] args)
        {
            String ClientId = "2993fe290e1744158bdec14aa8016ebd";
            String RedirectUri = "http://localhost:4002";
            String scope = "user-read-playback-state user-modify-playback-state user-read-currently-playing";

            ImplicitGrantAuth auth = new ImplicitGrantAuth(
              ClientId,
              "http://localhost:4002",
              "http://localhost:4002",
              Scope.UserReadPlaybackState | Scope.UserModifyPlaybackState | Scope.UserReadCurrentlyPlaying
            );
            auth.AuthReceived += async (sender, payload) =>
            {
                auth.Stop(); // `sender` is also the auth instance
                _spotify = new SpotifyWebAPI()
                {
                    TokenType = payload.TokenType,
                    AccessToken = payload.AccessToken
                };
                // Do requests with API client
            };
            auth.Start(); // Starts an internal HTTP Server

            StringBuilder builder = new StringBuilder("https://accounts.spotify.com/authorize/?");
            builder.Append("client_id=" + ClientId);
            builder.Append($"&response_type=code");
            builder.Append("&redirect_uri=" + RedirectUri);
            builder.Append("&scope=" + scope);
            builder.Append("&show_dialog=false");
            String uri = Uri.EscapeUriString(builder.ToString());
            Process.Start(uri);
        }
    }




        [Command("/xlsnp")]
        [HelpMessage("Display currently playing song.")]
        public void GetTrackname(string command, string args)
        {
            var chat = this.pluginInterface.Framework.Gui.Chat;
            PlaybackContext context = _spotify.GetPlayingTrack();
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
        public void SetVolume(string command, string args)
        {
            _spotify = new SpotifyWebAPI()
            {
                AccessToken = oauth,
                TokenType = "Bearer"
            };
            var chat = this.pluginInterface.Framework.Gui.Chat;
            if (!int.TryParse(args, out var volume)) return;
            if (volume > 100 || volume < 0) return;
            ErrorResponse error = _spotify.SetVolume(volume);
            chat.Print($"Spotify volume set to {volume}");
        }

        [Command("/xlsprevious")]
        [HelpMessage("Go to previous track.")]
        public void SeekPrevious(string command, string args)
        {
            _spotify = new SpotifyWebAPI()
            {
                AccessToken = oauth,
                TokenType = "Bearer"
            };
            var chat = this.pluginInterface.Framework.Gui.Chat;
            ErrorResponse error = _spotify.SkipPlaybackToPrevious();
            chat.Print($"Seeking to previous track.");
        }

        [Command("/xlsnext")]
        [HelpMessage("Go to previous track.")]
        public void SeekNext(string command, string args)
        {
            _spotify = new SpotifyWebAPI()
            {
                AccessToken = oauth,
                TokenType = "Bearer"
            };
            var chat = this.pluginInterface.Framework.Gui.Chat;
            ErrorResponse error = _spotify.SkipPlaybackToNext();
            chat.Print($"Seeking to next track.");
        }

        [Command("/xlsrestart")]
        [HelpMessage("Restart current track.")]
        public void SeekRestart(string command, string args)
        {
            _spotify = new SpotifyWebAPI()
            {
                AccessToken = oauth,
                TokenType = "Bearer"
            };
            var chat = this.pluginInterface.Framework.Gui.Chat;
            ErrorResponse error = _spotify.SeekPlayback(0);
            chat.Print($"Restarting current track.");
        }

        [Command("/xlstoggleplayback")]
        [HelpMessage("Restart current track.")]
        public void TogglePlayback(string command, string args)
        {
            _spotify = new SpotifyWebAPI()
            {
                AccessToken = oauth,
                TokenType = "Bearer"
            };
            var chat = this.pluginInterface.Framework.Gui.Chat;
            PlaybackContext context = _spotify.GetPlayback();
            if (context.IsPlaying == true)
            {
                ErrorResponse error = _spotify.PausePlayback();
                chat.Print($"Playback paused.");
            } else
            {
                ErrorResponse error = _spotify.ResumePlayback(offset: "");
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



