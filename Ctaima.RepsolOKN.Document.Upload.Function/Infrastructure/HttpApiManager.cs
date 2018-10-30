using IdentityModel.Client;
using Microsoft.Azure.WebJobs.Host;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace Ctaima.RepsolOKN.Document.Upload.Function.Infrastructure
{
    public class HttpApiManager
    {
        public string UrlAuthorize { get; set; }
        public string ClientId { get; set; }
        public string ClientSecret { get; set; }
        public string Scope { get; set; }

        public TokenResponse Token { get; set; }

        public string Url { get; set; }
        public string TenantId { get; set; }

        public async Task Authorize(TraceWriter log)
        {
            try
            {
                // discover endpoints from metadata
                var disco = await DiscoveryClient.GetAsync(UrlAuthorize);
                if (disco.IsError)
                {
                    log.Error($"Error: {disco.Error}");
                }

                // request token
                var tokenClient = new TokenClient(disco.TokenEndpoint, ClientId, ClientSecret);
                Token = await tokenClient.RequestClientCredentialsAsync(Scope);

                if (Token.IsError)
                {
                    log.Error($"Error: {Token.Error}");
                }

                log.Info($"tokenResponse.Json {Token.Json}");
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                log.Error($"Error: {e.Message}");
                throw;
            }

        }

        public async Task<string> GetAsync(string path, TraceWriter log)
        {
            try
            {
                if (Token == null)
                {
                    await Authorize(log);
                }

                using (var client = new HttpClient() { BaseAddress = new Uri(Url) })
                {
                    client.SetBearerToken(Token.AccessToken);
                    if (!string.IsNullOrEmpty(TenantId))
                        client.DefaultRequestHeaders.Add("tenantId", TenantId);

                    var result = await client.GetAsync(path);
                    result.EnsureSuccessStatusCode();
                    var response = await result.Content.ReadAsStringAsync();

                    log.Info($"Result post: {response}");

                    return response;
                }
            }
            catch (Exception e)
            {
                log.Error($"Error: {e.Message}");
                throw;
            }
        }

        public async Task<string> GetAsync(string path, string bodyJson, TraceWriter log)
        {
            try
            {
                if (Token == null)
                {
                    await Authorize(log);
                }
                using (var client = new HttpClient())
                {
                    var request = new HttpRequestMessage
                    {
                        RequestUri = new Uri(Url),
                        Method = HttpMethod.Get,
                        Content = new ByteArrayContent(Encoding.UTF8.GetBytes(bodyJson)),
                    };

                    request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json"); // change as necessary 

                    if (!string.IsNullOrEmpty(TenantId))
                        client.DefaultRequestHeaders.Add("tenantId", TenantId);

                    var result = client.SendAsync(request).Result;
                    result.EnsureSuccessStatusCode();

                    var responseBody = await result.Content.ReadAsStringAsync().ConfigureAwait(false);
                    return responseBody;
                }
            }
            catch (Exception e)
            {
                log.Error($"Error: {e.Message}");
                throw;
            }
        }

        public async Task<string> PostAsync(string path, string bodyJson, TraceWriter log)
        {
            try
            {
                if (Token == null)
                {
                    await Authorize(log);
                }
                using (var client = new HttpClient() { BaseAddress = new Uri(Url) })
                {
                    client.SetBearerToken(Token.AccessToken);
                    if (!string.IsNullOrEmpty(TenantId))
                        client.DefaultRequestHeaders.Add("tenantId", TenantId);

                    var result = await client.PostAsync(path, new ByteArrayContent(Encoding.UTF8.GetBytes(bodyJson)));
                    result.EnsureSuccessStatusCode();
                    var response = await result.Content.ReadAsStringAsync();

                    log.Info($"Result post: {response}");

                    return response;
                }
            }
            catch (Exception e)
            {
                log.Error($"Sending {path}. Error: {e.Message}");
                throw new Exception($"Sending {path}. Error: {e.Message}");
            }
        }



        public async Task<string> PostAsync(string path, MultipartFormDataContent form, TraceWriter log)
        {
            try
            {
                if (Token == null)
                {
                    await Authorize(log);
                }
                using (var client = new HttpClient() { BaseAddress = new Uri(Url) })
                {
                    client.SetBearerToken(Token.AccessToken);
                    if (!string.IsNullOrEmpty(TenantId))
                        client.DefaultRequestHeaders.Add("tenantId", TenantId);

                    HttpResponseMessage result = await client.PostAsync(path, form);

                    result.EnsureSuccessStatusCode();
                    client.Dispose();

                    string response = result.Content.ReadAsStringAsync().Result;

                    log.Info($"Result post: {response}");

                    return response;
                }
            }
            catch (Exception e)
            {
                log.Error($"Error: {e.Message}");
                throw;
            }
        }

    }

}
