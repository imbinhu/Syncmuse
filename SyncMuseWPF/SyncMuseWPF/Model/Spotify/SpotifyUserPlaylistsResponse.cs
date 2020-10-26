using System;
using System.Collections.Generic;
using System.Text;

namespace SyncMuseWPF.Model.Spotify
{
    struct SpotifyUserPlaylistsResponse
    {
        public string href { get; set; }
        public List<SpotifyUserPlaylistsResponseItem> items { get; set; }
        public int limit { get; set; }
        public object next { get; set; }
        public int offset { get; set; }
        public object previous { get; set; }
        public int total { get; set; }
    }

    struct Owner
    {
        public ExternalUrls externalUrls { get; set; }
        public string href { get; set; }
        public string id { get; set; }
        public string type { get; set; }
        public string uri { get; set; }
    }

    struct SpotifyUserPlaylistsResponseTracks
    {
        public string href { get; set; }
        public int total { get; set; }
    }

    internal struct SpotifyUserPlaylistsResponseItem
    {
        public bool collaborative { get; set; }
        public ExternalUrls externalUrls { get; set; }
        public string href { get; set; }
        public string id { get; set; }
        public List<Image> images { get; set; }
        public string name { get; set; }
        public Owner owner { get; set; }
        public bool @public { get; set; }
        public string snapshot_id { get; set; }
        public SpotifyUserPlaylistsResponseTracks tracks { get; set; }
        public string type { get; set; }
        public string uri { get; set; }
    }

}
