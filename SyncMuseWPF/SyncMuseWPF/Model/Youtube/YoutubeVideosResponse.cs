using System;
using System.Collections.Generic;
using System.Security.RightsManagement;
using System.Text;
using System.Windows.Documents;

namespace SyncMuseWPF.Model.Youtube
{
    struct YoutubeVideosResponse
    {
        public string kind { get; set; }
        public string etag { get; set; }
        public List<Item> items { get; set; }
    }
    struct Item
    {
        public string kind { get; set; }
        public string etag { get; set; }
        public string id { get; set; }
        public Snippet snippet { get; set; }
    }
    struct Snippet
    {
        public string publishedAt { get; set; }
        public string channelId { get; set; }
        public string title { get; set; }
        public string description { get; set; }
    }
    struct Thumbnails
    {
        public Default @default { get; set; }
        public Medium medium { get; set; }
        public High high { get; set; }
        public Standard standard { get; set; }
        public Maxres maxres { get; set; }
    }
    struct Default
    {
        public string url { get; set; }
        public int width { get; set; }
        public int height { get; set; }
    }
    struct Medium
    {
        public string url { get; set; }
        public int width { get; set; }
        public int height { get; set; }
    }
    struct High
    {
        public string url { get; set; }
        public int width { get; set; }
        public int height { get; set; }
    }
    struct Standard
    {
        public string url { get; set; }
        public int width { get; set; }
        public int height { get; set; }
    }
    struct Maxres
    {
        public string url { get; set; }
        public int width { get; set; }
        public int height { get; set; }
    }
}
