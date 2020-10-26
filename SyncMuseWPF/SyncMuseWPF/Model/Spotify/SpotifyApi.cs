﻿using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;

namespace SyncMuseWPF.Model.Spotify
{
    class SpotifyApi
    {
        private const string clientID = "7496cbf895f046ddba7ca532cda06e9c";
        private const string authorizationEndpoint = "https://accounts.google.com/o/oauth2/v2/auth";
        private const string tokenEndpoint = "https://accounts.spotify.com/api/token";
        private const string secretFileName = "spotify_secrets.json"; // temporary

        private static string refreshToken;
        private static string accessToken;

        private static HttpClient httpClient;

        public SpotifyApi()
        {
            httpClient = new HttpClient();
        }

        private void SaveSecrets()
        {
            Dictionary<string,string> secrets = new Dictionary<string, string>(){ { "access_token", accessToken },{ "refresh_token", refreshToken} };
            File.WriteAllText(AppDomain.CurrentDomain.BaseDirectory + @"\" + secretFileName, JsonConvert.SerializeObject(secrets));
        }

        private static int GetRandomUnusedPort()
        {
            var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            var port = ((IPEndPoint)listener.LocalEndpoint).Port;
            listener.Stop();
            return port;
        }

        public async Task<bool> Authenticate(Window mainWindow)
        {
            // Generates state and PKCE values.
            string state = randomDataBase64url(32);
            string code_verifier = randomDataBase64url(32);
            string code_challenge = base64urlencodeNoPadding(sha256(code_verifier));
            string code_challenge_method = "S256";

            // Creates a redirect URI using an available port on the loopback address.
            string redirectURI = string.Format("http://{0}:{1}/", IPAddress.Loopback, 62329);

            // Creates an HttpListener to listen for requests on that redirect URI.
            HttpListener httpListener = new HttpListener();
            httpListener.Prefixes.Add(redirectURI);
            //Trace.WriteLine("Listening..");
            httpListener.Start();

            // Creates the OAuth 2.0 authorization request.
            string authorizationRequest = string.Format("https://accounts.spotify.com/authorize?response_type=code&client_id={0}&redirect_uri={1}&scope={2}&state={3}&code_challenge={4}&code_challenge_method={5}",
                clientID,
                System.Uri.EscapeDataString(redirectURI),
                "user-follow-modify user-read-email playlist-read-private",
                state,
                code_challenge,
                code_challenge_method);

            // Opens request in the browser.
            ProcessStartInfo psi = new ProcessStartInfo
            {
                FileName = authorizationRequest,
                UseShellExecute = true
            };
            Process.Start(psi);

            // Waits for the OAuth authorization response.
            HttpListenerContext context = await httpListener.GetContextAsync();
            mainWindow.Activate();

            // Sends an HTTP response to the browser.
            string responseString = string.Format("<html><head></head><body>Please return to the app.</body></html>");
            byte[] buffer = System.Text.Encoding.UTF8.GetBytes(responseString);
            HttpListenerResponse response = context.Response;
            response.ContentLength64 = buffer.Length;
            Stream responseOutput = response.OutputStream;
            Task responseTask = responseOutput.WriteAsync(buffer, 0, buffer.Length).ContinueWith((task) =>
            {
                responseOutput.Close();
                httpListener.Stop();
                //Trace.WriteLine("HTTP server stopped.");
            });

            // Checks for errors.
            if (context.Request.QueryString.Get("error") != null)
            {
                //Trace.WriteLine(String.Format("OAuth authorization error: {0}.", context.Request.QueryString.Get("error")));
                return false;
            }
            if (context.Request.QueryString.Get("code") == null || context.Request.QueryString.Get("state") == null)
            {
                //Trace.WriteLine("Malformed authorization response. " + context.Request.QueryString);
                return false;
            }

            // extracts the code
            var code = context.Request.QueryString.Get("code");
            var incoming_state = context.Request.QueryString.Get("state");

            // Compares the receieved state to the expected value, to ensure that
            // this app made the request which resulted in authorization.
            if (incoming_state != state)
            {
                Trace.WriteLine(String.Format("Received request with invalid state ({0})", incoming_state));
                return false;
            }
            //Trace.WriteLine("Authorization code: " + code);

            // Starts the code exchange at the Token Endpoint.
            if (!await PerformCodeExchange(code, code_verifier, redirectURI))
            {
                return false;
            }
            SaveSecrets();
            return true;
            
        }

        private async Task<bool> PerformCodeExchange(string code, string code_verifier, string redirectURI)
        {
            //Trace.WriteLine("Exchanging code for tokens...");

            //Trace.WriteLine(code);
            // builds the  request
            string tokenRequestURI = "https://accounts.spotify.com/api/token";
            string tokenRequestBody = string.Format("code={0}&redirect_uri={1}&client_id={2}&code_verifier={3}&scope=&grant_type=authorization_code",
                code,
                System.Uri.EscapeDataString(redirectURI),
                clientID,
                code_verifier
                );

            HttpRequestMessage tokenRequestMessage = new HttpRequestMessage(HttpMethod.Post, tokenRequestURI);
            tokenRequestMessage.Content = new StringContent(tokenRequestBody, Encoding.UTF8, "application/x-www-form-urlencoded");

            try
            {
                HttpResponseMessage tokenResponseMessage = await httpClient.SendAsync(tokenRequestMessage);
                string responseText = await tokenResponseMessage.Content.ReadAsStringAsync();

                Dictionary<string, string> response = JsonConvert.DeserializeObject<Dictionary<string, string>>(responseText);
                if (response["access_token"] == null) 
                {
                    return false;
                }
                accessToken = response["access_token"];
                refreshToken = response["refresh_token"];
            }
            catch (HttpRequestException e)
            {
                return false;
            }

            return true;
           
        }

        public async Task<bool> RefreshToken()
        {
            string refreshTokenRequestURI = "https://accounts.spotify.com/api/token";

            HttpRequestMessage refreshTokenMessage = new HttpRequestMessage(HttpMethod.Post, refreshTokenRequestURI);

            refreshTokenMessage.Content = new StringContent(string.Format("client_id={0}&refresh_token={1}&grant_type=refresh_token", clientID, refreshToken), Encoding.UTF8, "application/x-www-form-urlencoded");

            try
            {
                HttpResponseMessage tokenResponseMessage = await httpClient.SendAsync(refreshTokenMessage);
                string responseText = await tokenResponseMessage.Content.ReadAsStringAsync();

                Dictionary<string, string> response = JsonConvert.DeserializeObject<Dictionary<string, string>>(responseText);
                if (response["access_token"] == null)
                {
                    return false;
                }
                accessToken = response["access_token"];
                refreshToken = response["refresh_token"];
            }
            catch (HttpRequestException e)
            {
                return false;
            }

            return true;
        }

        public async Task<SpotifyCurrentUserProfileResponse> UserProfile()
        {
            // builds the  request
            string playlistRequestURI = string.Format("https://api.spotify.com/v1/me");

            HttpRequestMessage playlistRequestMessage = new HttpRequestMessage(HttpMethod.Get, playlistRequestURI);
            playlistRequestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            HttpResponseMessage playlistResponseMessage = await httpClient.SendAsync(playlistRequestMessage);

            return JsonConvert.DeserializeObject<SpotifyCurrentUserProfileResponse>(await playlistResponseMessage.Content.ReadAsStringAsync());
        }
        public async Task<SpotifyUserPlaylistsResponse> Playlist(string userId)
        {
            userId = "8t9ifr7uvczma942eq5uzyehf";
            // builds the  request
            string playlistRequestURI = string.Format("https://api.spotify.com/v1/users/{0}/playlists", userId);

            Trace.WriteLine(accessToken);

            HttpRequestMessage playlistRequestMessage = new HttpRequestMessage(HttpMethod.Get, playlistRequestURI);
            playlistRequestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            HttpResponseMessage playlistResponseMessage = await httpClient.SendAsync(playlistRequestMessage);

            return JsonConvert.DeserializeObject<SpotifyUserPlaylistsResponse>(await playlistResponseMessage.Content.ReadAsStringAsync());
        }
        public async Task<SpotifyPlaylistItemsResponse> PlaylistItems(string playlistId)
        {
            string playlistRequestURI = string.Format("https://api.spotify.com/v1/playlists/{0}/tracks", playlistId);

            Trace.WriteLine(accessToken);

            HttpRequestMessage playlistRequestMessage = new HttpRequestMessage(HttpMethod.Get, playlistRequestURI);
            playlistRequestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            HttpResponseMessage playlistResponseMessage = await httpClient.SendAsync(playlistRequestMessage);

            return JsonConvert.DeserializeObject<SpotifyPlaylistItemsResponse>(await playlistResponseMessage.Content.ReadAsStringAsync());
        }

        public async Task<SpotifySearchItemResponse> SearchItem(string query)
        {
            query = Regex.Replace(query, "[^A-Za-z0-9 -]", "").ToLower();
            query = query.Replace("-", "").Replace("official mv", "").Replace("mv", "").Replace(" ", "%20");
            string playlistRequestURI = string.Format("https://api.spotify.com/v1/search?q={0}&type=track&market=IT", query);

            Trace.WriteLine(accessToken);

            HttpRequestMessage playlistRequestMessage = new HttpRequestMessage(HttpMethod.Get, playlistRequestURI);
            playlistRequestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            HttpResponseMessage playlistResponseMessage = await httpClient.SendAsync(playlistRequestMessage);

            return JsonConvert.DeserializeObject<SpotifySearchItemResponse>(await playlistResponseMessage.Content.ReadAsStringAsync());
        }

        #region Cryptography Methods
        public static string randomDataBase64url(uint length)
        {
            RNGCryptoServiceProvider rng = new RNGCryptoServiceProvider();
            byte[] bytes = new byte[length];
            rng.GetBytes(bytes);
            return base64urlencodeNoPadding(bytes);
        }

        /// <summary>
        /// Returns the SHA256 hash of the input string.
        /// </summary>
        /// <param name="inputStirng"></param>
        /// <returns></returns>
        public static byte[] sha256(string inputStirng)
        {
            byte[] bytes = Encoding.ASCII.GetBytes(inputStirng);
            SHA256Managed sha256 = new SHA256Managed();
            return sha256.ComputeHash(bytes);
        }

        /// <summary>
        /// Base64url no-padding encodes the given input buffer.
        /// </summary>
        /// <param name="buffer"></param>
        /// <returns></returns>
        public static string base64urlencodeNoPadding(byte[] buffer)
        {
            string base64 = Convert.ToBase64String(buffer);

            // Converts base64 to base64url.
            base64 = base64.Replace("+", "-");
            base64 = base64.Replace("/", "_");
            // Strips padding.
            base64 = base64.Replace("=", "");

            return base64;
        }
        #endregion
    }

    #region Response structs

    public class ExternalIds
    {
        public string isrc { get; set; }
    }
    struct ExternalUrls
    {
        public string spotify { get; set; }
    }
    struct Artist
    {
        public ExternalUrls external_urls { get; set; }
        public string href { get; set; }
        public string id { get; set; }
        public string name { get; set; }
        public string type { get; set; }
        public string uri { get; set; }
    }
    struct Image
    {
        public string height { get; set; }
        public string url { get; set; }
        public string width { get; set; }
    }
    #endregion
}
