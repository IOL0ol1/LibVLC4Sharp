using System;
using System.Threading.Tasks;
using System.Windows;
using LibVLCSharp.Core;

namespace LibVLCSharp.WPF.Sample
{
    public partial class MainWindow : Window
    {
        /// <summary>
        /// Optional: absolute path to a folder containing a LibVLC **4.x** build
        /// (libvlc.dll + libvlccore.dll + the plugins\ folder). Leave empty to let
        /// LibVLC.Initialize() auto-discover them next to the .exe. See README in this folder.
        /// </summary>
        private const string VlcDirectory = @"";

        // Managed Core wrappers (no raw libvlc_*_t* pointers in the sample anymore).
        private LibVLC _vlc;
        private MediaPlayer _player;

        public MainWindow()
        {
            InitializeComponent();
            Loaded += OnLoaded;
            Closed += OnClosed;
        }

        private async void OnLoaded(object sender, RoutedEventArgs e)
        {
            // Creating the LibVLC instance is slow on a cold start (LibVLC scans plugins\ to build its
            // cache), so run it off the UI thread to keep the window responsive. The Open/Play buttons
            // stay disabled (set in XAML) until the instance is ready.
            try
            {
                _vlc = await Task.Run(() =>
                {
                    if (!string.IsNullOrEmpty(VlcDirectory))
                        LibVLC.UsePath(VlcDirectory);
                    else
                        LibVLC.Initialize(); // auto-discover (app dir / runtimes/<rid>/native)

                    return new LibVLC();
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show(this,
                    "Failed to initialize LibVLC.\n\n" + ex.Message +
                    "\n\nDownload a LibVLC 4.x build and either set VlcDirectory in MainWindow.xaml.cs " +
                    "or drop libvlc.dll + libvlccore.dll + plugins\\ next to the .exe.",
                    "LibVLC", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            OpenButton.IsEnabled = true;
            PlayButton.IsEnabled = true;
        }

        private void OnPlay(object sender, RoutedEventArgs e)
        {
            if (_vlc == null) return;
            var mrl = Mrl.Text?.Trim();
            if (string.IsNullOrEmpty(mrl)) return;

            StopInternal();

            bool isUrl = mrl.Contains("://");
            // The player takes its own reference on the media, so the local wrapper can be disposed
            // immediately after construction (mirrors libvlc_media_release in the raw API).
            using var media = isUrl ? Media.FromLocation(mrl) : Media.FromPath(mrl);

            _player = _vlc.CreateMediaPlayer(media);

            // Install the output callbacks (via the managed MediaPlayer DP) BEFORE play.
            Video.MediaPlayer = _player;
            _player.Play();
        }

        private void OnStop(object sender, RoutedEventArgs e) => StopInternal();

        private void StopInternal()
        {
            if (_player != null)
            {
                Video.MediaPlayer = null;   // detach the output callbacks before releasing the player
                _player.Stop();
                _player.Dispose();
                _player = null;
            }
        }

        private void OnOpenFile(object sender, RoutedEventArgs e)
        {
            var dlg = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "Media|*.mp4;*.mkv;*.avi;*.mov;*.mp3;*.flac;*.wav;*.ts;*.webm|All files|*.*"
            };
            if (dlg.ShowDialog(this) == true)
            {
                Mrl.Text = dlg.FileName;
                OnPlay(sender, e);
            }
        }

        private void OnOverlayClick(object sender, RoutedEventArgs e) =>
            MessageBox.Show(this, "The overlay receives input and composites over the video — airspace solved.",
                "Overlay", MessageBoxButton.OK, MessageBoxImage.Information);

        private void OnClosed(object sender, EventArgs e)
        {
            StopInternal();
            Video.Dispose();
            _vlc?.Dispose();
            _vlc = null;
        }
    }
}
