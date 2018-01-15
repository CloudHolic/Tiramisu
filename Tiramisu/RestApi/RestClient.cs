using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using NLog;
using Tiramisu.Entities;
using Tiramisu.Util;

namespace Tiramisu.RestApi
{
    public delegate void NotifySuccessfulStatusCodeResult(HttpStatusCode httpCode, string uri, string content, string response);
    public delegate void NotifyUnsuccessfulStatusCodeResult(HttpStatusCode httpCode, string uri, string content, string response);
    public delegate void NotifyErrorResult(Exception e, string uri);

    public sealed class RestClient
    {
        public event NotifySuccessfulStatusCodeResult SuccessfulStatusCodeResult;
        public event NotifyUnsuccessfulStatusCodeResult UnsuccessfulStatusCodeResult;
        public event NotifyErrorResult ErrorResult;

        private static volatile RestClient _instance;
        private static readonly object _lock = new object();
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        public static RestClient Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                        if (_instance == null)
                            _instance = new RestClient();
                    
                }
                return _instance;
            }
        }

        internal Task<T> DeleteAsync<T>(object p1, object p2)
        {
            throw new NotImplementedException();
        }

        public string Host { get; }

        private readonly Config _config;
        private readonly Uri _baseAddress;
        private readonly TimeSpan _timeout;
        private readonly JsonSerializerSettings _serializerSettings;
        private readonly JsonSerializerSettings _deserializerSettings;

        private RestClient()
        {
            _config = Config.LoadFromFile("config.json");
            Host = _config.OsuApi;

            _baseAddress = new Uri(Host);
            _timeout = TimeSpan.FromSeconds(10);

            _serializerSettings = new JsonSerializerSettings
            {
                ContractResolver = new LowerCasePropertyNamesContractResolver(),
                Converters = new List<JsonConverter> { new StringEnumConverter() },
                NullValueHandling = NullValueHandling.Ignore
            };

            _deserializerSettings = new JsonSerializerSettings
            {
                ContractResolver = new LowerCasePropertyNamesContractResolver(),
                Converters = new List<JsonConverter> { new StringEnumConverter() }
            };
        }

        private HttpClient CreateHttpClient()
        {
            var http = HttpClientFactory.Create(new HttpRequestHeaderInitializeHook(_config.OsuApiKey));

            ServicePointManager.ServerCertificateValidationCallback += (sender, certificate, chain, sslPolicyErrors) => true;

            http.BaseAddress = _baseAddress;
            http.Timeout = _timeout;
            return http;
        }

        #region Marshalling / Unmarshalling
        private T DeserializeObject<T>(string jsonString)
        {
            return JsonConvert.DeserializeObject<T>(jsonString, _deserializerSettings);
        }

        private string SerializeObject<T>(T obj)
        {
            if (obj == null)
                return string.Empty;

            var type = typeof(T);
            if (type == typeof(string))
                return obj as string;
            if (type == typeof(JObject))
                return (obj as JObject)?.ToString();

            return JsonConvert.SerializeObject(obj, typeof(T), _serializerSettings);
        }

        private T TypeConvert<T>(string jsonString)
        {
            var type = typeof(T);
            if (type == typeof(string))
                return (T)(object)jsonString;
            if (type == typeof(JObject))
                return (T)(object)JObject.Parse(jsonString);
            return DeserializeObject<T>(jsonString);
        }
        #endregion

        #region URL Parameter Builder
        private string AppendQueryParam(string url, Dictionary<string, string> queryParams)
        {
            var result = url;
            if (queryParams == null || queryParams.Count <= 0)
                return result;

            var sb = new StringBuilder("?");
            foreach (var pair in queryParams)
            {
                sb.Append(pair.Key);
                sb.Append("=");
                sb.Append(pair.Value);
                sb.Append("&");
            }
            sb.Remove(sb.Length - 1, 1);
            result = url + sb.ToString();
            return result;
        }
        #endregion

        #region GET Methods
        public async Task<T> GetAsync<T>(string url, Dictionary<string, string> queryParams = null, bool silent = true)
        {
            var http = CreateHttpClient();
            var uri = AppendQueryParam(url, queryParams);

            try
            {
                var response = await http.GetAsync(uri);
                if (response.IsSuccessStatusCode)
                {
                    var body = await response.Content.ReadAsStringAsync();
                    SuccessfulStatusCodeResult?.Invoke(response.StatusCode, uri, null, body);
                    return TypeConvert<T>(body);
                }
                else
                {
                    if (!silent)
                        UnsuccessfulStatusCodeResult?.Invoke(response.StatusCode, uri, null, await response.Content.ReadAsStringAsync());
                }
            }
            catch (Exception e)
            {
                if (!silent)
                    ErrorResult?.Invoke(e.GetBaseException(), uri);
                Log.Error(e, "Error while GetAsync.");
            }
            finally
            {
                http.Dispose();
            }

            return default(T);
        }
        #endregion

        #region POST Methods
        public async Task<T> PostAsync<T>(string url, T value, Dictionary<string, string> queryParams = null, bool silent = false)
        {
            return await PostAsync<T, T>(url, value, queryParams, silent);
        }

        public async Task<OUT> PostAsync<IN, OUT>(string url, IN input, Dictionary<string, string> queryParams = null, bool silent = false)
        {
            var http = CreateHttpClient();
            var uri = AppendQueryParam(url, queryParams);

            try
            {
                var content = SerializeObject(input);
                var response = await http.PostAsync(uri, new StringContent(string.IsNullOrEmpty(content) ? "{}" : content, Encoding.UTF8, "application/json"));
                if (response.IsSuccessStatusCode)
                {
                    var body = await response.Content.ReadAsStringAsync();
                    SuccessfulStatusCodeResult?.Invoke(response.StatusCode, uri, content, body);
                    return TypeConvert<OUT>(body);
                }
                else
                {
                    if (!silent)
                        UnsuccessfulStatusCodeResult?.Invoke(response.StatusCode, uri, content, await response.Content.ReadAsStringAsync());
                }
            }
            catch (Exception e)
            {
                if (!silent)
                    ErrorResult?.Invoke(e.GetBaseException(), uri);
                Log.Error(e, "Error while PostAsync.");
            }
            finally
            {
                http.Dispose();
            }

            return default(OUT);
        }

        public async Task<OUT> PostAsync<OUT>(string url, string path, List<KeyValuePair<string, string>> values = null, Dictionary<string, string> queryParams = null, bool silent = false)
        {
            var http = CreateHttpClient();
            var uri = AppendQueryParam(url, queryParams);

            try
            {
                using (var content = new MultipartFormDataContent())
                {
                    if (values != null)
                    {
                        foreach (var keyValuePair in values)
                            content.Add(new StringContent(keyValuePair.Value), $"\"{keyValuePair.Key}\"");
                    }

                    if (!string.IsNullOrEmpty(path))
                    {
                        var fileName = Path.GetFileName(path);
                        var extension = Path.GetExtension(fileName).Replace(".", "").ToLower();
                        var data = new StreamContent(File.OpenRead(path));
                        data.Headers.ContentDisposition = new ContentDispositionHeaderValue("form-data")
                        {
                            Name = "\"paperImage\"",
                            FileName = $"\"{fileName}\""
                        };
                        data.Headers.ContentType = new MediaTypeHeaderValue($"image/{extension}");
                        content.Add(data);
                    }

                    var response = await http.PostAsync(uri, content);
                    if (response.IsSuccessStatusCode)
                    {
                        var body = await response.Content.ReadAsStringAsync();
                        SuccessfulStatusCodeResult?.Invoke(response.StatusCode, uri, null, body);
                        return TypeConvert<OUT>(body);
                    }
                    else
                    {
                        if (!silent)
                            UnsuccessfulStatusCodeResult?.Invoke(response.StatusCode, uri, null, await response.Content.ReadAsStringAsync());
                    }
                }
            }
            catch (Exception e)
            {
                if (!silent)
                    ErrorResult?.Invoke(e.GetBaseException(), uri);
                Log.Error(e, "Error while PostAsync.");
            }
            finally
            {
                http.Dispose();
            }

            return default(OUT);
        }
        #endregion

        #region PUT Methods
        public async Task<T> PutAsync<T>(string url, T value, Dictionary<string, string> queryParams = null, bool silent = false)
        {
            return await PutAsync<T, T>(url, value, queryParams, silent);
        }

        public async Task<OUT> PutAsync<IN, OUT>(string url, IN input, Dictionary<string, string> queryParams = null, bool silent = false)
        {
            var http = CreateHttpClient();
            var uri = AppendQueryParam(url, queryParams);

            try
            {
                var content = SerializeObject(input);
                var response = await http.PutAsync(uri, new StringContent(string.IsNullOrEmpty(content) ? "{}" : content, Encoding.UTF8, "application/json"));
                if (response.IsSuccessStatusCode)
                {
                    var body = await response.Content.ReadAsStringAsync();
                    SuccessfulStatusCodeResult?.Invoke(response.StatusCode, uri, content, body);
                    return TypeConvert<OUT>(body);
                }
                else
                {
                    if (!silent)
                        UnsuccessfulStatusCodeResult?.Invoke(response.StatusCode, uri, content, await response.Content.ReadAsStringAsync());
                }
            }
            catch (Exception e)
            {
                if (!silent)
                    ErrorResult?.Invoke(e.GetBaseException(), uri);
                Log.Error(e, "Error while PutAsync.");
            }
            finally
            {
                http.Dispose();
            }

            return default(OUT);
        }
        #endregion

        #region PATCH Methods
        public async Task<T> PatchAsync<T>(string url, T value, Dictionary<string, string> queryParams = null, bool silent = false)
        {
            return await PatchAsync<T, T>(url, value, queryParams, silent);
        }

        public async Task<OUT> PatchAsync<IN, OUT>(string url, IN input, Dictionary<string, string> queryParams = null, bool silent = false)
        {
            var http = CreateHttpClient();
            var uri = AppendQueryParam(url, queryParams);

            try
            {
                var content = SerializeObject(input);
                var response = await http.PatchAsync(uri, new StringContent(string.IsNullOrEmpty(content) ? "{}" : content, Encoding.UTF8, "application/json"));
                if (response.IsSuccessStatusCode)
                {
                    var body = await response.Content.ReadAsStringAsync();
                    SuccessfulStatusCodeResult?.Invoke(response.StatusCode, uri, content, body);
                    return TypeConvert<OUT>(body);
                }
                else
                {
                    if (!silent)
                        UnsuccessfulStatusCodeResult?.Invoke(response.StatusCode, uri, content, await response.Content.ReadAsStringAsync());
                }
            }
            catch (Exception e)
            {
                if (!silent)
                    ErrorResult?.Invoke(e.GetBaseException(), uri);
                Log.Error(e, "Error while PatchAsync.");
            }
            finally
            {
                http.Dispose();
            }

            return default(OUT);
        }
        #endregion

        #region DELETE Methods
        public async Task<T> DeleteAsync<T>(string url, Dictionary<string, string> queryParams = null, bool silent = false)
        {
            var http = CreateHttpClient();
            var uri = AppendQueryParam(url, queryParams);

            try
            {
                var response = await http.DeleteAsync(uri);
                if (response.IsSuccessStatusCode)
                {
                    var body = await response.Content.ReadAsStringAsync();
                    SuccessfulStatusCodeResult?.Invoke(response.StatusCode, uri, null, body);
                    return TypeConvert<T>(body);
                }
                else
                {
                    if (!silent)
                        UnsuccessfulStatusCodeResult?.Invoke(response.StatusCode, uri, null, await response.Content.ReadAsStringAsync());
                }
            }
            catch (Exception e)
            {
                if (!silent)
                    ErrorResult?.Invoke(e.GetBaseException(), uri);
                Log.Error(e, "Error while DeleteAsync.");
            }
            finally
            {
                http.Dispose();
            }

            return default(T);
        }
        #endregion
    }
}