using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http.Extensions;
using Newtonsoft.Json;
using Microsoft.AspNetCore.Http;
using System.Text;
using System.IO;
using System.Text.RegularExpressions;
using Serilog;
using winamptospotifyweb.Models;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;

namespace winamptospotifyweb.Controllers
{
    class SpotifyWebAPIUrl
    {        
        public string redirectURL = "https://localhost:5000/callback";
        public string playlistBaseUrl = "https://api.spotify.com/v1/users/{UserId}/playlists";
        public string trackSearchBaseUrl = "https://api.spotify.com/v1/search";
        public string playlistAddTrackhBaseUrl = "https://api.spotify.com/v1/playlists/{playlist_id}/tracks";
        public string authorizationUrl = "https://accounts.spotify.com/authorize/";
        public string apiTokenUrl = "https://accounts.spotify.com/api/token";
    }

    [Route("")]
    public class SpotifyController : Controller
    {
        SpotifyWebAPIUrl sWebApiUrl = new SpotifyWebAPIUrl();
        private readonly SpotifyAPIDetails _SpotifyAPIDetails;
        private readonly ILogger logger = new LoggerConfiguration().WriteTo.File("log-.txt", rollingInterval: RollingInterval.Day).CreateLogger();

        public SpotifyController(IOptions<SpotifyAPIDetails> spotifyAPIDetails)
        {
            // We want to know if spotifyAPIDetails is null so we throw an exception if it is           
            _SpotifyAPIDetails = spotifyAPIDetails.Value ?? throw new ArgumentException(nameof(spotifyAPIDetails));
        }

        [HttpGet]
        public ContentResult Get()
        {
            var qb = new QueryBuilder();
            qb.Add("response_type", "code");
            qb.Add("client_id", _SpotifyAPIDetails.ClientID);
            qb.Add("scope", "user-read-private user-read-email playlist-modify-public playlist-modify-private");
            qb.Add("redirect_uri", sWebApiUrl.redirectURL);

            return new ContentResult
            {
                ContentType = "text/html",
                Content = @"
                    <!DOCTYPE html>
                    <html>
                        <head>
                            <meta charset=""utf-8"">
                            <meta name = ""viewport"" content = ""width=device-width, initial-scale=1, shrink-to-fit=no"" >
                            <title>Winamp To Spotify Web App</title>
                            <link rel=""stylesheet"" href=""https://stackpath.bootstrapcdn.com/bootstrap/4.4.1/css/bootstrap.min.css"" integrity=""sha384-Vkoo8x4CGsO3+Hhxv8T/Q5PaXtkKtu6ug5TOeNV6gBiFeWPGFN9MuhOf23Q9Ifjh"" crossorigin=""anonymous"">
                        </head>

                        <body>
                            <a  class=""btn btn - primary"" href="""+ sWebApiUrl.authorizationUrl + qb.ToQueryString().ToString() + @"""><button>Authenticate at Spotify</button></a>

                            <script src=""https://code.jquery.com/jquery-3.4.1.slim.min.js"" integrity=""sha384-J6qa4849blE2+poT4WnyKhv5vZF5SrPo0iEjwBvKU7imGFAV0wwj1yYfoRSJoZ+n"" crossorigin=""anonymous""></script>
                            <script src=""https://cdn.jsdelivr.net/npm/popper.js@1.16.0/dist/umd/popper.min.js"" integrity = ""sha384-Q6E9RHvbIyZFJoft+2mJbHaEWldlvI9IOYy5n3zV9zzTtmI3UksdQRVvoxMfooAo"" crossorigin = ""anonymous"" ></ script >
                            <script src=""https://stackpath.bootstrapcdn.com/bootstrap/4.4.1/js/bootstrap.min.js"" integrity = ""sha384-wfSDF2E50Y2D1uUdj0O3uMBJnjuUD4Ih7YwaYd1iqfktj0Uod8GCExl3Og8ifwB6"" crossorigin = ""anonymous"" ></script>  
                        </body>
                    </html>
                "
            };
        }

        [Route("/callback")]
        public async Task<ActionResult> Get(string code)
        {
            if (code.Length > 0)
            {
                using (HttpClient client = new HttpClient())
                {
                    Console.WriteLine(Environment.NewLine + "Your basic bearer: " + Convert.ToBase64String(System.Text.ASCIIEncoding.ASCII.GetBytes(_SpotifyAPIDetails.ClientID + ":" + _SpotifyAPIDetails.SecretID)));
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(System.Text.ASCIIEncoding.ASCII.GetBytes(_SpotifyAPIDetails.ClientID + ":" + _SpotifyAPIDetails.SecretID)));

                    FormUrlEncodedContent formContent = new FormUrlEncodedContent(new[]
                    {
                        new KeyValuePair<string, string>("code", code),
                        new KeyValuePair<string, string>("redirect_uri", sWebApiUrl.redirectURL),
                        new KeyValuePair<string, string>("grant_type", "authorization_code"),
                    });

                    var result = await client.PostAsync(sWebApiUrl.apiTokenUrl, formContent);
                    result.EnsureSuccessStatusCode();
                    var content = await result.Content.ReadAsStringAsync();
                    var spotifyAuth = JsonConvert.DeserializeObject<SpotifyJsonResponseWrapper.AccessToken>(content);
                    CookieSet("access_token", spotifyAuth.access_token, 9000);
                    logger.Information($"Accestoken {spotifyAuth.access_token}");
                }
            }

            return await Task.Run<ActionResult>(() => RedirectToAction("SelectFolder"));
        }

        [HttpGet]
        [Route("/selectfolder")]
        public ContentResult SelectFolder()
        {
            return new ContentResult
            {
                ContentType = "text/html",
                Content = @"
                    <!DOCTYPE html>
                    <html>
                        <head>
                            <meta charset=""utf-8"">
                            <meta name = ""viewport"" content = ""width=device-width, initial-scale=1, shrink-to-fit=no"" >
                            <title>Select a file to get folder name</title>
                            <link rel=""stylesheet"" href=""https://stackpath.bootstrapcdn.com/bootstrap/4.4.1/css/bootstrap.min.css"" integrity=""sha384-Vkoo8x4CGsO3+Hhxv8T/Q5PaXtkKtu6ug5TOeNV6gBiFeWPGFN9MuhOf23Q9Ifjh"" crossorigin=""anonymous"">
                        </head>                         
 
                          <body>
                            <form method=""post"">
                              <div class=""form-group"">                                
                                <label for=""foldernameId"">Folder name:</label> 
                                <input type=""text"" id=""foldernameId"" class=""form-control"" name=""foldername"">  
                                <button type=""submit"" class=""btn btn-primary"" formaction=""/processfolder"">Submit to folder process</button>                                
                             </form> 
                            <script src=""https://code.jquery.com/jquery-3.4.1.slim.min.js"" integrity=""sha384-J6qa4849blE2+poT4WnyKhv5vZF5SrPo0iEjwBvKU7imGFAV0wwj1yYfoRSJoZ+n"" crossorigin=""anonymous""></script>
                            <script src=""https://cdn.jsdelivr.net/npm/popper.js@1.16.0/dist/umd/popper.min.js"" integrity = ""sha384-Q6E9RHvbIyZFJoft+2mJbHaEWldlvI9IOYy5n3zV9zzTtmI3UksdQRVvoxMfooAo"" crossorigin = ""anonymous"" ></ script >
                            <script src=""https://stackpath.bootstrapcdn.com/bootstrap/4.4.1/js/bootstrap.min.js"" integrity = ""sha384-wfSDF2E50Y2D1uUdj0O3uMBJnjuUD4Ih7YwaYd1iqfktj0Uod8GCExl3Og8ifwB6"" crossorigin = ""anonymous"" ></script>  
                        </body>
                    </html>
                "
            };
        }

        [HttpPost]
        [Route("/processfolder")]
        public async Task<ContentResult> ProcessFolder(string foldername, string access_token)
        {
            if (string.IsNullOrWhiteSpace(foldername)) throw new ArgumentException("Folder Name is empty");

            string albumName = foldername.Split('\\')[foldername.Split('\\').Length - 1];
            string playlistId = await CreatePlayList(albumName);
            Console.WriteLine($"{foldername} will be processed...");
            TrackInfo trackInfo = await GetTrackUriAndNames(foldername);
            bool isTracksAdded = await AddTrackToPlaylistFunc(playlistId, trackInfo.TrackUri);
            var sbTracksAdded = new StringBuilder();
            if (isTracksAdded && trackInfo != null && !string.IsNullOrEmpty(trackInfo.TrackName))
            {
                foreach (string trackName in trackInfo.TrackName.TrimEnd(',').Split(','))
                {
                    sbTracksAdded.Append(trackName + Environment.NewLine);
                }
            }
            else
            {
                logger.Error($"There is a problem getting track uris or tracknames");
                throw new Exception($"There is a problem getting track uris or tracknames");
            }
            logger.Information($"{foldername} is processed.");
            logger.Information($"{albumName} album created successfully. {Environment.NewLine} Tracks added: {sbTracksAdded.ToString()}");
            return new ContentResult
            {
                ContentType = "application/json",
                Content = $"{albumName} album created successfully{Environment.NewLine}.Tracks added:{Environment.NewLine}{sbTracksAdded.ToString()}"
            };
        }

        private async Task<TrackInfo> GetTrackUriAndNames(string foldername)
        {
            Dictionary<string, string> trackInfoDict = await GetTrackUri(foldername);
            TrackInfo albumRelatedTrackInfos = new TrackInfo();
            StringBuilder trackNameStrBuilder = new StringBuilder();
            StringBuilder trackUriBuilder = new StringBuilder();
            
            if (trackInfoDict.Count > 0)
            {
                foreach (KeyValuePair<string, string> kv in trackInfoDict)
                {
                    trackNameStrBuilder.Append(kv.Value + ',');
                    trackUriBuilder.Append(kv.Key + ',');
                }
                albumRelatedTrackInfos.TrackName = trackNameStrBuilder.ToString();
                albumRelatedTrackInfos.TrackUri = trackUriBuilder.ToString();
                return albumRelatedTrackInfos;
            }
            else
            {
                logger.Error("There is a problem getting track uris or tracknames");
                throw new Exception("There is a problem getting track uris or tracknames");
            }
        }

        public async Task<string> CreatePlayList(string playlistname)
        {
            if (string.IsNullOrWhiteSpace(playlistname)) throw new ArgumentException("Playlist name is empty");
            string playlistId = "";                        
            var stringPayload = new
            {
                name = playlistname,
                description = playlistname
            };
            var bodyPayload = new StringContent(JsonConvert.SerializeObject(stringPayload), Encoding.UTF8, "application/json");
            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("Authorization", "Bearer " + Request.Cookies["access_token"]);
                var response = await client.PostAsync(sWebApiUrl.playlistBaseUrl.Replace("{UserId}",_SpotifyAPIDetails.UserID), bodyPayload);
                response.EnsureSuccessStatusCode();
                var responseContent = await response.Content.ReadAsStringAsync();
                var playlist = JsonConvert.DeserializeObject<SpotifyJsonResponseWrapper.PlayList>(responseContent);
                playlistId = playlist.id;
            }
            logger.Information($"{playlistname} created successfully");
            Console.WriteLine($"{playlistname} created successfully");
            return playlistId;

        }


        [HttpGet("gettrack/{trackname}")]
        public ContentResult GetTrack(string trackname)
        {
            if (string.IsNullOrWhiteSpace(trackname)) throw new ArgumentException("Track name is empty");
            var qb = new QueryBuilder();
            qb.Add("q", trackname);
            qb.Add("type", "track");
            qb.Add("limit", "1");

            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("Authorization", "Bearer " + Request.Cookies["access_token"]);
                var trackUrl = sWebApiUrl.trackSearchBaseUrl + qb.ToQueryString().ToString();
                var response = client.GetAsync(trackUrl).Result;
                if (response.IsSuccessStatusCode)
                {
                    var responseContent = response.Content;
                    string responseString = responseContent.ReadAsStringAsync().Result;
                    var results = JsonConvert.DeserializeObject<SpotifyJsonResponseWrapper.RootObject>(responseString);
                    var tracks = results.tracks;
                    var trackUri = tracks.items[0].uri;
                    return new ContentResult
                    {
                        ContentType = "application/json",
                        Content = tracks.items[0].name
                    };
                }
                else
                {
                    logger.Error($"Cannot get track {trackname}");
                    throw new Exception($"Cannot get track {trackname}");
                }
            }
        }


        public async Task<Dictionary<string, string>> GetTrackUri(string filepath)
        {
            if (string.IsNullOrWhiteSpace(filepath)) throw new ArgumentException("Filepath is empty");        
            Dictionary<string, string> trackInfoDict = new Dictionary<string, string>();
            string path = filepath;
            bool isArtistNameExistInFolderPath = false;
            string artist = filepath.Split('\\')[filepath.Split('\\').Length - 1].Split(' ')[0];
            List<string> fileNamesList = GetMp3FileNames(path, artist, ref isArtistNameExistInFolderPath);

            if (fileNamesList.Count > 0)
            {
                foreach (var item in fileNamesList)
                {
                    var qb = new QueryBuilder();
                    var queryTrackString = item;
                    if (isArtistNameExistInFolderPath)
                        queryTrackString += $" artist:{artist}";
                    qb.Add("q", queryTrackString);
                    qb.Add("type", "track");
                    qb.Add("limit", "1");

                    using (HttpClient client = new HttpClient())
                    {
                        client.DefaultRequestHeaders.Add("Authorization", "Bearer " + Request.Cookies["access_token"]);
                        var trackUrl = sWebApiUrl.trackSearchBaseUrl + qb.ToQueryString().ToString();
                        var response = await client.GetAsync(trackUrl);
                        if (response.IsSuccessStatusCode)
                        {
                            string responseString = await response.Content.ReadAsStringAsync();
                            var results = JsonConvert.DeserializeObject<SpotifyJsonResponseWrapper.RootObject>(responseString);
                            var tracks = results.tracks;
                            if (tracks.items.Count > 0)
                            {
                                trackInfoDict.TryAdd(tracks.items[0].uri, tracks.items[0].name);
                                Console.WriteLine($"Track {tracks.items[0].name} found.");
                            }
                        }
                        else
                        {
                            continue;
                        }
                    }
                }

                return trackInfoDict;
            }
            else
            {
                return new Dictionary<string, string>();
            }
        }

        private List<string> GetMp3FileNames(string path, string artist,ref bool isArtistNameExistInFolderPath)
        {
            if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("Filepath is empty");
            if (artist == null) throw new ArgumentException("Artist is null");

            FileInfo[] filesInfoArray = new DirectoryInfo(path).GetFiles("*.mp3");
            List<string> fileNames = new List<string>();

            if (filesInfoArray.Length > 0)
            {
                foreach (var file in filesInfoArray)
                {
                    var fileName = Path.GetFileNameWithoutExtension(file.Name);
                    if (!isArtistNameExistInFolderPath)
                        isArtistNameExistInFolderPath = fileName.ToLower().Contains(artist.ToLower());
                    Regex reg = new Regex(@"[^\p{L}\p{N} ]");
                    fileName = reg.Replace(fileName, String.Empty);
                    fileName = Regex.Replace(fileName, @"[0-9]+", "");
                    fileName = fileName.ToLower().Replace(artist.ToLower(), "", StringComparison.InvariantCultureIgnoreCase);
                    fileName = fileName.TrimStart();
                    fileName = fileName.TrimEnd();
                    fileNames.Add(fileName);
                }
            }
            else
            {
                logger.Error($"Cannot find any file in {path}");
                throw new Exception($"Cannot find any file in {path}");
            }
            return fileNames;
        }

        public async Task<bool> AddTrackToPlaylistFunc(string playlistId, string uris)
        {
            if (string.IsNullOrWhiteSpace(playlistId)) throw new ArgumentException("PlaylistId is empty");
            if (string.IsNullOrWhiteSpace(uris)) throw new ArgumentException("Uris is empty");                                    

            var qb = new QueryBuilder();
            qb.Add("uris", uris);
            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("Authorization", "Bearer " + Request.Cookies["access_token"]);
                var response = await client.PostAsync(sWebApiUrl.playlistAddTrackhBaseUrl.Replace("{playlist_id}", playlistId) + qb.ToQueryString(), null);
                response.EnsureSuccessStatusCode();
                string responseString = await response.Content.ReadAsStringAsync();
                if (!string.IsNullOrWhiteSpace(responseString))
                    return true;
                else
                    return false;
            }

        }

        /// <summary>  
        /// set the cookie  
        /// </summary>  
        /// <param name="key">key (unique indentifier)</param>  
        /// <param name="value">value to store in cookie object</param>  
        /// <param name="expireTime">expiration time</param>  
        public void CookieSet(string key, string value, int? expireTime)
        {
            CookieOptions option = new CookieOptions();

            if (expireTime.HasValue)
                option.Expires = DateTime.Now.AddMinutes(expireTime.Value);
            else
                option.Expires = DateTime.Now.AddMilliseconds(10);

            Response.Cookies.Append(key, value, option);
        }
    }
}
