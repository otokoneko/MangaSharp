using MangaSharp.Model;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace MangaSharp.Controllers
{
    [ApiController, Route("mangaEditor")]
    public class MangaEditorController : Controller
    {
        [HttpPost, Route("translate")]
        public async Task<ActionResult> Translate([FromForm] IFormCollection form, [FromServices] ITranslator translator)
        {
            var id = form["id"];
            var fname = form["fname"];
            var lang = form["lang"];
            var path = $"wwwroot/image/{id}/{fname}";

            using var image = await Image.LoadAsync(path);
            await using var ms = new MemoryStream();

            image.Mutate(x =>
            {
                x.Pad(image.Width + 80, image.Height + 60, Color.White);
            });

            image.Save(ms, PngFormat.Instance);

            using var engine = new Tesseract.TesseractEngine("tessdata", "jpn_vert", Tesseract.EngineMode.LstmOnly);
            using var pic = Tesseract.Pix.LoadFromMemory(ms.ToArray());
            using var page = engine.Process(pic, Tesseract.PageSegMode.SingleBlockVertText);

            var text = page.GetText();

            var result = new TranslateResult
            {
                text = text,
                translatedText = string.IsNullOrEmpty(text) ? "" : await translator.Japanese2Chinese(text.Replace("\n", ""))
            };

            return Json(result);
        }

        [HttpPost, Route("upload")]
        public async Task<ActionResult> Upload([FromForm] IFormCollection form, [FromServices] TextSegmentation textSegmentation)
        {
            var file = form.Files.GetFile("files");
            if (file == null) return Content("{}");

            var ms = new MemoryStream();
            await file.CopyToAsync(ms);

            ms.Seek(0, SeekOrigin.Begin);
            using var image = Image.Load<Rgb24>(ms);

            ms.Seek(0, SeekOrigin.Begin);
            var md5 = HashUtils.Md5(ms);
            await ms.DisposeAsync();

            Directory.CreateDirectory($"wwwroot/image/{md5}");

            var width = image.Width;
            var height = image.Height;

            using var opencvImage = ImageUtils.Convert(image);

            (var dict, var mask) = textSegmentation.Segmentate(image);

            using var opencvMask = new Emgu.CV.Image<Emgu.CV.Structure.Gray, byte>(mask);
            using var opencvInpainted = opencvImage.InPaint(opencvMask, 10);

            var root = new Root
            {
                background = "",
                dim = new Dim
                {
                    cols = width,
                    rows = height
                },
                id = md5,
                fileName = "",
                Ballons = new Dictionary<int, Ballon>(),
                balloonCount = dict.Count,
            };

            for (var i = 0; i < dict.Count; i++)
            {
                var rect = dict[i];
                var boundingRect = new BoundingRect
                {
                    x = rect.MinX,
                    y = rect.MinY,
                    width = rect.MaxX - rect.MinX + 1,
                    height = rect.MaxY - rect.MinY + 1,
                };
                var ballon = new Ballon
                {
                    boundingRect = boundingRect,
                    textRectCount = 1,
                    textRects = new Dictionary<int, BoundingRect>(),
                    filledMaskURL = $"/image/{md5}/{i}_f.png",
                    originalURL = $"/image/{md5}/{i}_ori.png",
                    resultURL = $"/image/{md5}/{i}_ori.png",
                };
                ballon.textRects[0] = boundingRect;
                root.Ballons[i] = ballon;

                var ori = image.Clone(x => x
                    .Crop(new Rectangle(boundingRect.x, boundingRect.y, boundingRect.width, boundingRect.height))
                );
                ori.Save($"./wwwroot/image/{md5}/{i}_ori.png");

                opencvInpainted.ROI = new System.Drawing.Rectangle(boundingRect.x, boundingRect.y, boundingRect.width, boundingRect.height);

                opencvInpainted.Save($"./wwwroot/image/{md5}/{i}_f.png");
            }

            return Content(Newtonsoft.Json.JsonConvert.SerializeObject(root.Convert()));
        }
    }
}
