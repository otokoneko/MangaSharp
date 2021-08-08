using System.Collections.Generic;
using System.Dynamic;

namespace MangaSharp.Model
{
    public class TranslateResult
    {
        public string text { get; set; }
        public string translatedText { get; set; }
    }

    public class Dim
    {
        public int cols { get; set; }
        public int rows { get; set; }
    }

    public class BoundingRect
    {
        public int x { get; set; }
        public int y { get; set; }
        public int width { get; set; }
        public int height { get; set; }
    }

    public class Ballon
    {
        public string resultURL { get; set; }
        public string originalURL { get; set; }
        public string filledMaskURL { get; set; }
        public BoundingRect boundingRect { get; set; }
        public int textRectCount { get; set; }
        public Dictionary<int, BoundingRect> textRects { get; set; }

        public dynamic Convert()
        {
            dynamic ballon = new ExpandoObject();
            ballon.resultURL = resultURL;
            ballon.originalURL = originalURL;
            ballon.filledMaskURL = filledMaskURL;
            ballon.boundingRect = boundingRect;
            ballon.textRectCount = textRectCount;
            ballon.textRect = new Dictionary<string, object>();

            foreach (var (i, rect) in textRects)
            {
                ballon.textRect[i.ToString()] = rect;
            }

            return ballon;
        }
    }

    public class Root
    {
        public string fileName { get; set; }
        public string id { get; set; }
        public int balloonCount { get; set; }
        public Dim dim { get; set; }
        public Dictionary<int, Ballon> Ballons { get; set; }
        public string background { get; set; }

        public dynamic Convert()
        {
            dynamic root = new ExpandoObject();
            root.fileName = fileName;
            root.id = id;
            root.balloonCount = balloonCount;
            root.dim = dim;
            root.background = background;
            foreach (var (i, ballon) in Ballons)
            {
                ((IDictionary<string, object>)root)[i.ToString()] = ballon.Convert();
            }

            return root;
        }
    }
}