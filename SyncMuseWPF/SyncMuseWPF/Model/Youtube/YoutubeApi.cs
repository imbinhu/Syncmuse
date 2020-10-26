using Newtonsoft.Json;
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
using System.Threading.Tasks;
using System.Windows;

namespace SyncMuseWPF.Model.Youtube
{
    class YoutubeApi
    {
        private const string clientID = "";
        private const string clientSecret = "";
        private const string apiKey = "";
        private const string authorizationEndpoint = "";
        private const string tokenEndpoint = "";
        private const string secretFileName = ""; // temporary


        private static string refreshToken;
        private static string accessToken;

        private static HttpClient httpClient;

        public YoutubeApi()
        {
            httpClient = new HttpClient();
        }
        private void SaveSecrets()
        {
            Dictionary<string, string> secrets = new Dictionary<string, string>() { { "access_token", accessToken }, { "refresh_token", refreshToken } };
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
            string redirectURI = string.Format("http://{0}:{1}/", IPAddress.Loopback, GetRandomUnusedPort());

            // Creates an HttpListener to listen for requests on that redirect URI.
            HttpListener httpListener = new HttpListener();
            httpListener.Prefixes.Add(redirectURI);
            //Trace.WriteLine("Listening..");
            httpListener.Start();

            // Creates the OAuth 2.0 authorization request.
            string authorizationRequest = string.Format("{0}?response_type=code&scope=https://www.googleapis.com/auth/youtube.readonly&redirect_uri={1}&client_id={2}&state={3}&code_challenge={4}&code_challenge_method={5}",
                authorizationEndpoint,
                System.Uri.EscapeDataString(redirectURI),
                clientID,
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
            string tokenRequestURI = "https://www.googleapis.com/oauth2/v4/token";
            string tokenRequestBody = string.Format("code={0}&redirect_uri={1}&client_id={2}&code_verifier={3}&client_secret={4}&scope=&grant_type=authorization_code",
                code,
                System.Uri.EscapeDataString(redirectURI),
                clientID,
                code_verifier,
                clientSecret
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

        public async void Playlist()
        {
            // builds the  request
            string playlistRequestURI = string.Format("https://www.googleapis.com/youtube/v3/playlists?part=snippet&maxResults=50&mine=true&key={0}", apiKey);

            Trace.WriteLine(accessToken);

            HttpRequestMessage playlistRequestMessage = new HttpRequestMessage(HttpMethod.Get, playlistRequestURI);
            playlistRequestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            HttpResponseMessage playlistResponseMessage = await httpClient.SendAsync(playlistRequestMessage);

            Trace.WriteLine(await playlistResponseMessage.Content.ReadAsStringAsync());

        }

        public async Task<YoutubeVideosResponse> LikedVideos()
        {
            string likedVideosRequestURI = string.Format("https://www.googleapis.com/youtube/v3/videos?part=snippet%2CcontentDetails%2Cstatistics&myRating=like&key={0}", apiKey);

            HttpRequestMessage likedVideosRequestMessage = new HttpRequestMessage(HttpMethod.Get, likedVideosRequestURI);
            likedVideosRequestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            HttpClient likedVideosRequest = new HttpClient();
            HttpResponseMessage likedVideosResponseMessage = await likedVideosRequest.SendAsync(likedVideosRequestMessage);

            return JsonConvert.DeserializeObject<YoutubeVideosResponse>(await likedVideosResponseMessage.Content.ReadAsStringAsync());
            
        }

        public async Task<bool> RefreshToken()
        {
            string refreshTokenRequestURI = "https://oauth2.googleapis.com/token";

            HttpRequestMessage refreshTokenMessage = new HttpRequestMessage(HttpMethod.Post, refreshTokenRequestURI);

            refreshTokenMessage.Content = new StringContent(string.Format(string.Format("client_id={0}&client_secret={1}&refresh_token={2}&grant_type=refresh_token", clientID, clientSecret, refreshToken), Encoding.UTF8, "application/x-www-form-urlencoded"));

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
}
