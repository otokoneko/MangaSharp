﻿using Microsoft.Extensions.Logging;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace MangaSharp.Model
{
    public class TextSegmentation
    {
        private ILogger Logger { get; }
        private string ModelPath { get; }
        private bool Gpu { get; }

        public TextSegmentation(ILogger<TextSegmentation> logger, AppConfiguration appConfiguration)
        {
            Logger = logger;
            ModelPath = appConfiguration.TextSegmentationModel.Path;
            Gpu = appConfiguration.TextSegmentationModel.Gpu;

            if (!File.Exists(ModelPath))
            {
                logger.LogError($"Model path \"{ModelPath}\" not exists!");
            }
        }

        private DenseTensor<bool> Inference(InferenceSession session, Image<Rgb24> image, int width, int height)
        {
            var w = (int)Math.Ceiling((float)width / 2) * 2;
            var h = (int)Math.Ceiling((float)height / 2) * 2;

            image.Mutate(x =>
            {
                x.Pad(w, h, Color.White);
            });

            using var stream = new MemoryStream();
            image.Save(stream, PngFormat.Instance);

            Tensor<float> input = new DenseTensor<float>(new[] { 1, 3, image.Height, image.Width });

            for (var y = 0; y < image.Height; y++)
            {
                var pixelSpan = image.GetPixelRowSpan(y);
                for (var x = 0; x < image.Width; x++)
                {
                    input[0, 0, y, x] = pixelSpan[x].R;
                    input[0, 1, y, x] = pixelSpan[x].R;
                    input[0, 2, y, x] = pixelSpan[x].R;
                }
            }

            var inputs = new List<NamedOnnxValue>
            {
                NamedOnnxValue.CreateFromTensor("image", input)
            };

            var sw = new Stopwatch();

            sw.Start();

            using var results = session.Run(inputs);

            sw.Stop();

            Logger.LogInformation($"Inference time: {sw.ElapsedMilliseconds} ms");

            var result = results.First();

            var mask = result.AsTensor<bool>().ToDenseTensor();

            var white = new Rgb24(0xff, 0xff, 0xff);
            for (var y = 0; y < image.Height; y++)
            {
                var pixelSpan = image.GetPixelRowSpan(y);
                for (var x = 0; x < image.Width; x++)
                {
                    if (!mask[0, 0, y, x])
                    {
                        pixelSpan[x] = white;
                    }
                }
            }

            return mask;
        }

        public Tuple<Dictionary<int, Rect>, byte[,,]> Segmentate(Image<Rgb24> image)
        {
            var width = image.Width;
            var height = image.Height;
            Logger.LogInformation($"Image size: {width}x{height}");

            var options = new SessionOptions();

            if (Gpu)
                options.AppendExecutionProvider_CUDA(0);

            var session = new InferenceSession(ModelPath, options);
            var mask = Inference(session, image, width, height);
            session.Dispose();

            var points = new List<int>();

            var maskImage = new byte[height, width, 1];

            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    if (!mask[0, 0, y, x]) continue;
                    maskImage[y, x, 0] = 255;
                    points.Add(x);
                    points.Add(y);
                }
            }

            var number = points.Count / 2;
            var clusters = new int[number];

            Logger.LogInformation($"Number of text points: {number}");

            Dbscan.Dbscan2d(points.ToArray(), clusters, number, 20, 10);

            var dict = new Dictionary<int, Rect>();
            for (var i = 0; i < number; i++)
            {
                var cluster = clusters[i];
                if (cluster < 0)
                    continue;
                var x = points[i * 2];
                var y = points[i * 2 + 1];
                if (!dict.ContainsKey(cluster))
                {
                    dict.Add(cluster, new Rect(x, y, x, y));
                }
                dict[cluster].MinX = Math.Min(x, dict[cluster].MinX);
                dict[cluster].MinY = Math.Min(y, dict[cluster].MinY);
                dict[cluster].MaxX = Math.Max(x, dict[cluster].MaxX);
                dict[cluster].MaxY = Math.Max(y, dict[cluster].MaxY);
            }

            Logger.LogInformation($"Number of text clusters: {dict.Count}");

            return new Tuple<Dictionary<int, Rect>, byte[,,]>(dict, maskImage);
        }

        public class Rect
        {
            public int MinX { get; set; }
            public int MinY { get; set; }
            public int MaxX { get; set; }
            public int MaxY { get; set; }

            public Rect(int minX, int minY, int maxX, int maxY)
            {
                MinX = minX;
                MinY = minY;
                MaxX = maxX;
                MaxY = maxY;
            }
        }
    }
}
