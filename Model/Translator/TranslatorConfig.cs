namespace MangaSharp.Model
{
    public class TranslatorConfig
    {
        public string Default { get; set; }
        public BaiduTranslatorConfig Baidu { get; set; }
        public CaiyunTranslatorConfig Caiyun { get; set; }
        public YoudaoTranslatorConfig Youdao { get; set; }
    }

    public class BaiduTranslatorConfig
    {
        public string AppId { get; set; }
        public string Token { get; set; }
        public bool Action { get; set; }
    }

    public class CaiyunTranslatorConfig
    {
        public string Token { get; set; }
    }

    public class YoudaoTranslatorConfig
    {
        public string AppId { get; set; }
        public string Token { get; set; }
    }
}
