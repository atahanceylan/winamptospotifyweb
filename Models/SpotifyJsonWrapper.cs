using System.Collections.Generic;

namespace winamptospotifyweb.Models
{
    public static class SpotifyJsonResponseWrapper
    {
        public class AccessToken
        {
            public string access_token { get; set; }
        }

        public class PlayList
        {
            public string id { get; set; }
        }

        public class Item
        {

            public List<string> available_markets { get; set; }
            public int disc_number { get; set; }
            public int duration_ms { get; set; }
            public bool @explicit { get; set; }
            public string href { get; set; }
            public string id { get; set; }
            public bool is_local { get; set; }
            public string name { get; set; }
            public int popularity { get; set; }
            public string preview_url { get; set; }
            public int track_number { get; set; }
            public string type { get; set; }
            public string uri { get; set; }
        }

        public class Tracks
        {
            public string href { get; set; }
            public List<Item> items { get; set; }
            public int limit { get; set; }
            public string next { get; set; }
            public int offset { get; set; }
            public object previous { get; set; }
            public int total { get; set; }
        }

        public class RootObject
        {
            public Tracks tracks { get; set; }
        }

    }
}
