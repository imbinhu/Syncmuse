using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

using SyncMuseWPF.Model.Youtube;
using SyncMuseWPF.Model.Spotify;

namespace SyncMuseWPF.ViewModel
{
    /// <summary>
    /// Interaction logic for LoginView.xaml
    /// </summary>
    public partial class LoginViewModel : Window
    {
        YoutubeApi youtubeApi;
        SpotifyApi spotifyApi;
        public LoginViewModel()
        {
            InitializeComponent();
            youtubeApi = new YoutubeApi();
            spotifyApi = new SpotifyApi();
        }

        private void SpotifyLoginBtn_Click(object sender, RoutedEventArgs e)
        {
            SpotifyLogin();
        }

        private async void SpotifyLogin()
        {
            if (await spotifyApi.Authenticate(this))
            {
                Trace.WriteLine("success spotify");
                MainViewModel m = new MainViewModel();
                m.Show();
            }
            Trace.WriteLine("failed spotify");
            MainViewModel t = new MainViewModel();
            t.Show();
        }

        private void YoutubeLoginBtn_Click(object sender, RoutedEventArgs e)
        {
            YoutubeLogin();
        }

        private async void YoutubeLogin()
        {
            if (await youtubeApi.Authenticate(this))
            {
                YoutubeVideosResponse youtubeVideosResponse = await youtubeApi.LikedVideos();
                Trace.WriteLine("success youtube");
            }
            Trace.WriteLine("failed youtube");
        }
    }
}
