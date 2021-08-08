using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace MangaSharp.Model
{
    public class BaiduTranslator : ITranslator
    {
        #region Property

        private string AppId { get; }
        private string Token { get; }
        private bool Action { get; set; }
        private IHttpClientFactory ClientFactory { get; }
        private ILogger Logger { get; }
        private const string ApiBase = "https://api.fanyi.baidu.com/api/trans/vip/translate";

        #endregion

        #region InnerClass

        public class TransResult
        {
            public string src { get; set; }
            public string dst { get; set; }
        }

        public class Response
        {
            public string error_code { get; set; }
            public string error_msg { get; set; }
            public string from { get; set; }
            public string to { get; set; }
            public List<TransResult> trans_result { get; set; }
        }

        #endregion

        public BaiduTranslator(BaiduTranslatorConfig config, IHttpClientFactory clientFactory, ILogger<BaiduTranslator> logger)
        {
            AppId = config.AppId;
            Token = config.Token;
            Action = config.Action;
            ClientFactory = clientFactory;
            Logger = logger;
        }

        public async Task<string> Translate(string content, string languageFrom, string languageTo)
        {
            var salt = new Random().Next().ToString();
            var sign = HashUtils.Md5(AppId + content + salt + Token).ToLower();
            var requestUrl = $"{ApiBase}?q={content}&from={languageFrom}&to={languageTo}&appid={AppId}&salt={salt}&sign={sign}&action={(Action ? 1 : 0)}";
            var client = ClientFactory.CreateClient(nameof(BaiduTranslator));
            var response = await client.GetStringAsync(requestUrl);
            Logger.LogInformation($"Response content: {response}");
            var result = Newtonsoft.Json.JsonConvert.DeserializeObject<Response>(response);
            return result.error_code == null ? result.trans_result[0].dst : result.error_msg;
        }

        public Task<string> Japanese2Chinese(string chinese)
        {
            return Translate(chinese, "jp", "zh");
        }
    }
}
