using SyncMuseWPF.Model.Spotify;
using SyncMuseWPF.Model.Youtube;
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

namespace SyncMuseWPF.View
{
    /// <summary>
    /// Interaction logic for LoginView.xaml
    /// </summary>
    public partial class LoginView : Window
    {
        YoutubeApi youtubeApi;
        SpotifyApi spotifyApi;
        public LoginView()
        {
            InitializeComponent();
            youtubeApi = new YoutubeApi();
            spotifyApi = new SpotifyApi();
        }

        private void SpotifyLoginBtn_Click(object sender, RoutedEventArgs e)
        {
           if (await spotifyApi.Authenticate(this))
            {
                spotifyApi.UserProfile();
                SpotifyUserPlaylistsResponse r = await spotifyApi.Playlist((await spotifyApi.UserProfile()).id);
                r.items.ForEach(x => Trace.WriteLine(x.name));
                SpotifyPlaylistItemsResponse s = await spotifyApi.PlaylistItems(r.items[0].id);
                s.items.ForEach(x => Trace.WriteLine(x.track.name));
                SpotifySearchItemResponse e = await spotifyApi.SearchItem("[MV] 이달의 소녀(LOONA) Why Not?");
                Trace.WriteLine("Canzone");
                Trace.WriteLine(e.tracks.items[0].name);
                Trace.WriteLine("success spotify");
                MainView m = new MainView();
                m.Show();
            }
            else
            {
                Trace.WriteLine("failed spotify");
                MainView t = new MainView();
                t.Show();
            }
        }

        private void YoutubeLoginBtn_Click(object sender, RoutedEventArgs e)
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
