using System;
using System.Diagnostics;
using System.Net.Http;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Newtonsoft.Json.Linq;

namespace UpdateCheckerApp
{
    public partial class MainWindow : Window
    {
        public string localVersion = "";
        public string latestVersion = "";

        public enum UpdateStatus
        {
            Checking,
            UpToDate,
            UpdateAvailable,
            Error,
        };

        public UpdateStatus Status { get; set; }

        public MainWindow()
        {
            InitializeComponent();

            getLocalVersion();
        }

        private async void DownloadUpdateButtonClick(object sender, RoutedEventArgs e)
        {
            string downloadUrl =
                "https://github.com/hedge-dev/UnleashedRecomp/releases/download/"
                + latestVersion
                + "/UnleashedRecomp-Windows.zip";

            UpdateStatusText.Text = "Downloading update...";
            DownloadUpdateButton.IsEnabled = false;

            using (HttpClient client = new HttpClient())
            {
                try
                {
                    byte[] data = await client.GetByteArrayAsync(downloadUrl);

                    if (!System.IO.Directory.Exists(@".\tmp"))
                    {
                        System.IO.Directory.CreateDirectory(@".\tmp");
                    }

                    string filePath = @".\tmp\UnleashedRecomp-Windows.zip";

                    if (System.IO.File.Exists(filePath))
                    {
                        System.IO.File.Delete(filePath);
                    }

                    System.IO.File.WriteAllBytes(filePath, data);

                    UpdateStatusText.Text = "Update downloaded!";

                    System.IO.Compression.ZipFile.ExtractToDirectory(
                        filePath,
                        @".\",
                        overwriteFiles: true
                    );

                    System.IO.File.Delete(filePath);
                    System.IO.Directory.Delete(@".\tmp", true);
                    UpdateStatusText.Text = "Update installed!";
                }
                catch (Exception ex)
                {
                    UpdateStatusText.Text = "Error downloading update.";
                    LoggingText.Text = $"Error downloading update: {ex.Message}";
                    DownloadUpdateButton.IsEnabled = true;
                }
            }
        }

        private async void OnCheckUpdateButtonClick(object sender, RoutedEventArgs e)
        {
            getLocalVersion();

            UpdateStatusText.Text = "Checking for updates...";

            bool updateAvailable = await CheckForUpdatesAsync();

            if (updateAvailable)
            {
                Status = UpdateStatus.UpdateAvailable;
                DownloadUpdateButton.IsVisible = true;
            }
            else
            {
                Status = UpdateStatus.UpToDate;
            }

            switch (Status)
            {
                case UpdateStatus.Error:
                    UpdateStatusText.Text = "Error checking for updates.";
                    break;
                case UpdateStatus.UpToDate:
                    UpdateStatusText.Text = "Your software is up to date!";
                    UpdateStatusText.Foreground = new Avalonia.Media.SolidColorBrush(
                        Avalonia.Media.Colors.Green
                    );
                    break;
                case UpdateStatus.UpdateAvailable:
                    UpdateStatusText.Text = "Update available!";
                    DownloadUpdateButton.IsEnabled = true;
                    UpdateStatusText.Foreground = new Avalonia.Media.SolidColorBrush(
                        Avalonia.Media.Colors.Blue
                    );
                    break;
            }
        }

        private async Task<bool> CheckForUpdatesAsync()
        {
            string apiUrl =
                "https://api.github.com/repos/hedge-dev/UnleashedRecomp/releases/latest";

            using (HttpClient client = new HttpClient())
            {
                try
                {
                    client.DefaultRequestHeaders.Add("User-Agent", "UnleashedRecomp");

                    string response = await client.GetStringAsync(apiUrl);
                    JObject json = JObject.Parse(response);
                    latestVersion = json["tag_name"]?.ToString() ?? "0.0.0.0";

                    LoggingText.Text = $"Latest online version: {latestVersion}";

                    return string.Compare(
                            localVersion,
                            latestVersion,
                            StringComparison.OrdinalIgnoreCase
                        ) < 0;
                }
                catch (Exception ex)
                {
                    LoggingText.Text = $"Error checking updates: {ex.Message}";
                    Status = UpdateStatus.Error;
                    return false;
                }
            }
        }

        private string getLocalVersion()
        {
            string filePath = @".\UnleashedRecomp.exe";

            if (!System.IO.File.Exists(filePath))
            {
                CurrentVersion.Text =
                    "Error: UnleashedRecomp.exe not found.\nMake sure to put this file in the same directory as the game.";
                CurrentVersion.Foreground = new Avalonia.Media.SolidColorBrush(
                    Avalonia.Media.Colors.Red
                );

                CheckUpdateButton.IsVisible = false;
                return "";
            }
            else
            {
                FileVersionInfo versionInfo = FileVersionInfo.GetVersionInfo(filePath);
                localVersion = versionInfo.FileVersion ?? "0.0.0.0";
                CurrentVersion.Text = $"Current version: {localVersion}";
                return localVersion;
            }
        }
    }
}
