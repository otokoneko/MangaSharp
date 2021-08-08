using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace MangaSharp.Model
{
    public class YoudaoTranslator : ITranslator
    {
        #region Property

        private string AppId { get; }
        private string Token { get; }
        private IHttpClientFactory ClientFactory { get; }
        private ILogger Logger { get; }
        private const string ApiBase = "https://openapi.youdao.com/api";

        #endregion

        #region InnerClass

        public class Web
        {
            public string key { get; set; }
            public List<string> value { get; set; }
        }

        public class Dict
        {
            public string url { get; set; }
        }

        public class Webdict
        {
            public string url { get; set; }
        }

        public class Response
        {
            public string errorCode { get; set; }
            public string query { get; set; }
            public List<string> translation { get; set; }
            public List<Web> web { get; set; }
            public Dict dict { get; set; }
            public Webdict webdict { get; set; }
            public string l { get; set; }
            public string tSpeakUrl { get; set; }
            public string speakUrl { get; set; }
        }

        #endregion

        private static string Truncate(string q)
        {
            if (q == null)
            {
                return null;
            }
            var len = q.Length;
            return len <= 20 ? q : (q.Substring(0, 10) + len + q.Substring(len - 10, 10));
        }

        public YoudaoTranslator(YoudaoTranslatorConfig config, IHttpClientFactory clientFactory, ILogger<YoudaoTranslator> logger)
        {
            AppId = config.AppId;
            Token = config.Token;
            ClientFactory = clientFactory;
            Logger = logger;
        }

        public async Task<string> Translate(string content, string languageFrom, string languageTo)
        {
            var salt = DateTime.Now.Millisecond.ToString();
            var ts = (DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc));
            var millis = (long)ts.TotalMilliseconds;
            var curtime = Convert.ToString(millis / 1000);

            var signStr = AppId + Truncate(content) + salt + curtime + Token; ;
            var sign = HashUtils.Sha256(signStr);

            var payload = new Dictionary<string, string>()
            {
                {"q", content},
                {"from", languageFrom},
                {"to", languageTo},
                {"signType", "v3"},
                {"curtime", curtime },
                {"appKey", AppId },
                {"salt", salt },
                {"sign", sign }
            };

            var client = ClientFactory.CreateClient(nameof(YoudaoTranslator));
            var response = await client.PostAsync(ApiBase, new FormUrlEncodedContent(payload.ToList()));
            var text = await response.Content.ReadAsStringAsync();
            Logger.LogInformation($"Response content: {text}");
            var result = Newtonsoft.Json.JsonConvert.DeserializeObject<Response>(text);

            return result.errorCode == "0" ? string.Join(null, result.translation) : $"error code: {result.errorCode}";
        }

        public Task<string> Japanese2Chinese(string chinese)
        {
            return Translate(chinese, "ja", "zh-CHS");
        }
    }
}
