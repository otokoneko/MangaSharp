using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace MangaSharp.Model
{
    public class CaiyunTranslator : ITranslator
    {
        #region Property

        private string Token { get; }
        private IHttpClientFactory ClientFactory { get; }
        private ILogger Logger { get; }
        private const string ApiBase = "https://api.interpreter.caiyunai.com/v1/translator";

        #endregion

        #region InnerClass

        public class Payload
        {
            public List<string> source { get; set; }
            public string trans_type { get; set; }
            public string request_id { get; set; }
            public bool detect { get; set; }
        }

        public class Response
        {
            public double confidence { get; set; }
            public int rc { get; set; }
            public string message { get; set; }
            public List<object> src_tgt { get; set; }
            public List<string> target { get; set; }
        }

        #endregion

        public CaiyunTranslator(CaiyunTranslatorConfig config, IHttpClientFactory clientFactory, ILogger<CaiyunTranslator> logger)
        {
            Token = config.Token;
            ClientFactory = clientFactory;
            Logger = logger;
        }

        public async Task<string> Translate(string content, string languageFrom, string languageTo)
        {
            var payload = new Payload
            {
                source = new List<string>() { content },
                trans_type = $"{languageFrom}2{languageTo}",
                request_id = HashUtils.Md5($"{content}-{languageFrom}-{languageTo}"),
                detect = false
            };

            var client = ClientFactory.CreateClient(nameof(CaiyunTranslator));
            client.DefaultRequestHeaders.Add("x-authorization", $"token {Token}");
            var response = await client.PostAsync(ApiBase, JsonContent.Create(payload));
            var text = await response.Content.ReadAsStringAsync();
            Logger.LogInformation($"Response content: {text}");
            var result = Newtonsoft.Json.JsonConvert.DeserializeObject<Response>(text);
            return result.message ?? result.target.FirstOrDefault();
        }

        public Task<string> Japanese2Chinese(string chinese)
        {
            return Translate(chinese, "ja", "zh");
        }
    }
}
