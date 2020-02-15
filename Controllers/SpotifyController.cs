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

namespace dotnet_core_spotify_authentication.Controllers
{
    class SpotifyAuthentication
    {
        public string clientID = "yourClientId";
        public string clientSecret = "yourClientSeccret";
        public string redirectURL = "https://localhost:5000/callback";        
        public string playlistBaseUrl = "https://api.spotify.com/v1/users/userId/playlists";
        public string trackSearchBaseUrl = "https://api.spotify.com/v1/search";
        public string playlistAddTrackhBaseUrl = "https://api.spotify.com/v1/playlists/{playlist_id}/tracks";
    }

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


    [Route("")]
    public class SpotifyController : Controller
    {
        SpotifyAuthentication sAuth = new SpotifyAuthentication();
        private readonly ILogger logger = new LoggerConfiguration().WriteTo.File("log-.txt", rollingInterval: RollingInterval.Day).CreateLogger();

        [HttpGet]
        public ContentResult Get()
        {
            var qb = new QueryBuilder();
            qb.Add("response_type", "code");
            qb.Add("client_id", sAuth.clientID);
            qb.Add("scope", "user-read-private user-read-email playlist-modify-public playlist-modify-private");
            qb.Add("redirect_uri", sAuth.redirectURL);

            return new ContentResult
            {
                ContentType = "text/html",
                Content = @"
                    <!DOCTYPE html>
                    <html>
                        <head>
                            <meta charset=""utf-8"">
                            <meta name = ""viewport"" content = ""width=device-width, initial-scale=1, shrink-to-fit=no"" >
                            <title>Spotify Auth Example</title>
                            <link rel=""stylesheet"" href=""https://stackpath.bootstrapcdn.com/bootstrap/4.4.1/css/bootstrap.min.css"" integrity=""sha384-Vkoo8x4CGsO3+Hhxv8T/Q5PaXtkKtu6ug5TOeNV6gBiFeWPGFN9MuhOf23Q9Ifjh"" crossorigin=""anonymous"">
                        </head>

                        <body>
                            <a  class=""btn btn - primary"" href=""https://accounts.spotify.com/authorize/" + qb.ToQueryString().ToString() + @"""><button>Authenticate at Spotify</button></a>

                            <script src=""https://code.jquery.com/jquery-3.4.1.slim.min.js"" integrity=""sha384-J6qa4849blE2+poT4WnyKhv5vZF5SrPo0iEjwBvKU7imGFAV0wwj1yYfoRSJoZ+n"" crossorigin=""anonymous""></script>
                            <script src=""https://cdn.jsdelivr.net/npm/popper.js@1.16.0/dist/umd/popper.min.js"" integrity = ""sha384-Q6E9RHvbIyZFJoft+2mJbHaEWldlvI9IOYy5n3zV9zzTtmI3UksdQRVvoxMfooAo"" crossorigin = ""anonymous"" ></ script >
                            <script src=""https://stackpath.bootstrapcdn.com/bootstrap/4.4.1/js/bootstrap.min.js"" integrity = ""sha384-wfSDF2E50Y2D1uUdj0O3uMBJnjuUD4Ih7YwaYd1iqfktj0Uod8GCExl3Og8ifwB6"" crossorigin = ""anonymous"" ></script>  
                        </body>
                    </html>
                "
            };
        }

        [Route("/gettokenurl")]
        [HttpGet]
        public ContentResult GetTokenUrl()
        {
            var qb = new QueryBuilder();
            qb.Add("response_type", "code");
            qb.Add("client_id", sAuth.clientID);
            qb.Add("scope", "user-read-private user-read-email playlist-modify-public playlist-modify-private");
            qb.Add("redirect_uri", sAuth.redirectURL);

            return new ContentResult
            {
                ContentType = "text/plain",
                Content = "https://accounts.spotify.com/authorize/" + qb.ToQueryString().ToString()
            };
        }

        [Route("/callback")]
        public ActionResult Get(string code)
        {
            string responseString = "";

            if (code.Length > 0)
            {
                using (HttpClient client = new HttpClient())
                {
                    Console.WriteLine(Environment.NewLine + "Your basic bearer: " + Convert.ToBase64String(System.Text.ASCIIEncoding.ASCII.GetBytes(sAuth.clientID + ":" + sAuth.clientSecret)));
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(System.Text.ASCIIEncoding.ASCII.GetBytes(sAuth.clientID + ":" + sAuth.clientSecret)));

                    FormUrlEncodedContent formContent = new FormUrlEncodedContent(new[]
                    {
                        new KeyValuePair<string, string>("code", code),
                        new KeyValuePair<string, string>("redirect_uri", sAuth.redirectURL),
                        new KeyValuePair<string, string>("grant_type", "authorization_code"),
                    });

                    var response = client.PostAsync("https://accounts.spotify.com/api/token", formContent).Result;
                    response.EnsureSuccessStatusCode();

                    var responseContent = response.Content;
                    responseString = responseContent.ReadAsStringAsync().Result;
                    var spotifyAuth = JsonConvert.DeserializeObject<AccessToken>(responseString);
                    CookieSet("access_token", spotifyAuth.access_token, 9000);
                    responseString = spotifyAuth.access_token;
                    logger.Information($"Accestoken {responseString}");
                }
            }
            //return new ContentResult
            //{
            //    ContentType = "text/html",
            //    Content = $@"<html><body><span class=""normaltext"" id=""e1"">{responseString}</span></body></html>"
                
            //};
            return RedirectToAction("SelectFolder");
        }       
        
        [HttpPost]
        [Route("/selectedfolder")]
        public ContentResult SelectedFolder(string folderpath)
        {
            return new ContentResult
            {
                ContentType = "text/html",
                Content = $@"
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
                              Folder name: <input type=""text"" class=""form-control"" name=""foldername"" value=""{folderpath}"">                              
                              <div>                                 
                                <button type=""submit"" class=""btn btn-primary"" formaction=""/processfolder"">Submit to folder process</button>
                              </div>
                             </form>   
                            <script src=""https://code.jquery.com/jquery-3.4.1.slim.min.js"" integrity=""sha384-J6qa4849blE2+poT4WnyKhv5vZF5SrPo0iEjwBvKU7imGFAV0wwj1yYfoRSJoZ+n"" crossorigin=""anonymous""></script>
                            <script src=""https://cdn.jsdelivr.net/npm/popper.js@1.16.0/dist/umd/popper.min.js"" integrity = ""sha384-Q6E9RHvbIyZFJoft+2mJbHaEWldlvI9IOYy5n3zV9zzTtmI3UksdQRVvoxMfooAo"" crossorigin = ""anonymous"" ></ script >
                            <script src=""https://stackpath.bootstrapcdn.com/bootstrap/4.4.1/js/bootstrap.min.js"" integrity = ""sha384-wfSDF2E50Y2D1uUdj0O3uMBJnjuUD4Ih7YwaYd1iqfktj0Uod8GCExl3Og8ifwB6"" crossorigin = ""anonymous"" ></script>  
                        </body>
                    </html>
                "
            };
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
        public ContentResult ProcessFolder(string foldername, string access_token)
        {
            if (string.IsNullOrWhiteSpace(foldername)) throw new ArgumentException("Folder Name is empty");

            string albumName = foldername.Split('\\')[foldername.Split('\\').Length - 1];
            string playlistId = CreatePlayList(albumName, access_token);
            List<string> UrisAndTrackNamesList = GetTrackUri(foldername, access_token);
            var sbTracksAdded = new StringBuilder();
            if (UrisAndTrackNamesList.Count > 0)
            {
                AddTrackToPlaylistFunc(playlistId, UrisAndTrackNamesList[0], access_token);
                if (!string.IsNullOrWhiteSpace(UrisAndTrackNamesList[1]))
                {
                    string[] trackNames = UrisAndTrackNamesList[1].Split(',');
                    if (trackNames.Length > 0)
                    {
                        foreach (var name in trackNames)
                        {
                            sbTracksAdded.Append(name + " ;");
                        }
                    }
                }
            }
            else
            {
                logger.Error($"There is a problem getting track uris or tracknames");
                throw new Exception($"There is a problem getting track uris or tracknames");
            }
            logger.Information($"{albumName} album created successfully. Tracks added: {sbTracksAdded.ToString()}");
            return new ContentResult
            {
                ContentType = "application/json",
                Content = $"{albumName} album created successfully. Tracks added: {sbTracksAdded.ToString()}"
            };
        }


        public string CreatePlayList(string playlistname, string access_token)
        {
            if (string.IsNullOrWhiteSpace(playlistname)) throw new ArgumentException("Playlist name is empty");
            
            string responseString = "";
            string accessToken = "";

            if (Request.Cookies["access_token"] == null)
            {
                accessToken = access_token;
            }
            else
            {
                accessToken = Request.Cookies["access_token"];
            }

            var stringPayload = new
            {
                name = playlistname,
                description = playlistname
            };
            var bodyPayload = new StringContent(JsonConvert.SerializeObject(stringPayload), Encoding.UTF8, "application/json");
            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("Authorization", "Bearer " + accessToken);
                var response = client.PostAsync(sAuth.playlistBaseUrl, bodyPayload).Result;
                response.EnsureSuccessStatusCode();

                var responseContent = response.Content;
                responseString = responseContent.ReadAsStringAsync().Result;
                var playlist = JsonConvert.DeserializeObject<PlayList>(responseString);
                responseString = playlist.id;
            }
            logger.Information($"{playlistname} created successfully");
            Console.WriteLine($"{playlistname} created successfully");
            return responseString;

        }


        [HttpGet("gettrack/{trackname}")]
        public ContentResult GetTrack(string trackname, string access_token)
        {
            if (string.IsNullOrWhiteSpace(trackname)) throw new ArgumentException("Track name is empty");

            string responseString = "";
            string accessToken = "";

            if (Request.Cookies["access_token"] == null)
            {
                accessToken = access_token;
            }
            else
            {
                accessToken = Request.Cookies["access_token"];
            }

            var qb = new QueryBuilder();
            qb.Add("q", trackname);
            qb.Add("type", "track");
            qb.Add("limit", "1");

            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("Authorization", "Bearer " + accessToken);
                var trackUrl = sAuth.trackSearchBaseUrl + qb.ToQueryString().ToString();
                var response = client.GetAsync(trackUrl).Result;
                if (response.IsSuccessStatusCode)
                {
                    var responseContent = response.Content;
                    responseString = responseContent.ReadAsStringAsync().Result;
                    var results = JsonConvert.DeserializeObject<RootObject>(responseString);
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


        public List<string> GetTrackUri(string filepath, string access_token)
        {
            if (string.IsNullOrWhiteSpace(filepath)) throw new ArgumentException("Filepath is empty");

            string responseString = "";
            string accessToken = "";
            if (Request.Cookies["access_token"] == null)
            {
                accessToken = access_token;
            }
            else
            {
                accessToken = Request.Cookies["access_token"];
            }

            List<string> listOfUris = new List<string>();
            List<string> listOfTrackNames = new List<string>();
            List<string> listUriAndTrackNames = new List<string>();
            string path = filepath;

            string artist = filepath.Split('\\')[filepath.Split('\\').Length - 1].Split(' ')[0];
            List<string> fileNamesList = GetMp3FileNames(path, artist);

            if (fileNamesList.Count > 0)
            {
                foreach (var item in fileNamesList)
                {
                    var qb = new QueryBuilder();
                    qb.Add("q", $"artist:{artist} " + item);
                    qb.Add("type", "track");
                    qb.Add("limit", "1");

                    using (HttpClient client = new HttpClient())
                    {
                        client.DefaultRequestHeaders.Add("Authorization", "Bearer " + accessToken);
                        var trackUrl = sAuth.trackSearchBaseUrl + qb.ToQueryString().ToString();
                        var response = client.GetAsync(trackUrl).Result;
                        if (response.IsSuccessStatusCode)
                        {
                            var responseContent = response.Content;
                            responseString = responseContent.ReadAsStringAsync().Result;

                            var results = JsonConvert.DeserializeObject<RootObject>(responseString);
                            var tracks = results.tracks;
                            if (tracks.items.Count > 0)
                            {
                                listOfUris.Add(tracks.items[0].uri);
                                listOfTrackNames.Add(tracks.items[0].name);
                                Console.WriteLine($"Track {tracks.items[0].name} found.");
                            }
                        }
                        else
                        {
                            continue;
                        }
                    }
                }
                string joinedUris = string.Join(",", listOfUris);
                string joinedTrackNames = string.Join(",", listOfTrackNames);
                listUriAndTrackNames.Add(joinedUris);
                listUriAndTrackNames.Add(joinedTrackNames);
                return listUriAndTrackNames;
            }
            else
            {
                return new List<string>();
            }
        }

        private List<string> GetMp3FileNames(string path, string artist)
        {
            if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("Filepath is empty");
            if (artist == null) throw new ArgumentException("Artist is null");

            FileInfo[] filesInfoArray = new DirectoryInfo(path).GetFiles();
            List<string> fileNames = new List<string>();

            if (filesInfoArray.Length > 0)
            {
                foreach (var file in filesInfoArray)
                {
                    var fileName = Path.GetFileNameWithoutExtension(file.Name);

                    Regex reg = new Regex(@"[^\p{L}\p{N} ]");
                    fileName = reg.Replace(fileName, String.Empty);
                    fileName = Regex.Replace(fileName, @"[0-9]+", "");
                    fileName = fileName.Replace(artist, "", StringComparison.InvariantCultureIgnoreCase);
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

        public void AddTrackToPlaylistFunc(string playlistId, string uris, string access_token)
        {
            if (string.IsNullOrWhiteSpace(playlistId)) throw new ArgumentException("PlaylistId is empty");
            if (string.IsNullOrWhiteSpace(uris)) throw new ArgumentException("Uris is empty");

            string responseString = "";
            string accessToken = "";
            if (Request.Cookies["access_token"] == null)
            {
                accessToken = access_token;
            }
            else
            {
                accessToken = Request.Cookies["access_token"];
            }

            var qb = new QueryBuilder();
            qb.Add("uris", uris);
            //var stringPayload = new
            //{
            //    uris = Request.Cookies["trackUri"],                
            //};
            //var bodyPayload = new StringContent(JsonConvert.SerializeObject(stringPayload), Encoding.UTF8, "application/json");
            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("Authorization", "Bearer " + accessToken);
                var response = client.PostAsync(sAuth.playlistAddTrackhBaseUrl.Replace("{playlist_id}", playlistId) + qb.ToQueryString(), null).Result;
                response.EnsureSuccessStatusCode();
                var responseContent = response.Content;
                responseString = responseContent.ReadAsStringAsync().Result;
            }
        }


        [HttpGet("/addtracktoplaylist")]
        public ContentResult AddTrackToPlaylist()
        {
            string responseString = "";
            var qb = new QueryBuilder();
            qb.Add("uris", Request.Cookies["trackUri"]);
            //var stringPayload = new
            //{
            //    uris = Request.Cookies["trackUri"],                
            //};
            //var bodyPayload = new StringContent(JsonConvert.SerializeObject(stringPayload), Encoding.UTF8, "application/json");
            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("Authorization", "Bearer " + Request.Cookies["access_token"]);
                var response = client.PostAsync(sAuth.playlistAddTrackhBaseUrl.Replace("{playlist_id}", Request.Cookies["playlistID"]) + qb.ToQueryString(), null).Result;
                response.EnsureSuccessStatusCode();
                var responseContent = response.Content;
                responseString = responseContent.ReadAsStringAsync().Result;

            }
            return new ContentResult
            {
                ContentType = "application/json",
                Content = responseString
            };
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
