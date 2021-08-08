using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace MangaSharp.Model
{
    public static class HashUtils
    {
        private static string Hash(string input, HashAlgorithm hashAlgorithm)
        {
            var hash = hashAlgorithm.ComputeHash(Encoding.Default.GetBytes(input));
            return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
        }

        private static string Hash(Stream input, HashAlgorithm hashAlgorithm)
        {
            var hash = hashAlgorithm.ComputeHash(input);
            return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
        }

        public static string Md5(string input)
        {
            using var md5 = MD5.Create();
            return Hash(input, md5);
        }

        public static string Md5(Stream input)
        {
            using var md5 = MD5.Create();
            return Hash(input, md5);
        }

        public static string Sha256(string input)
        {
            using var sha256 = SHA256.Create();
            return Hash(input, sha256);
        }
    }

    public static class ImageUtils
    {
        public static Emgu.CV.Image<Emgu.CV.Structure.Bgr, byte> Convert(SixLabors.ImageSharp.Image<SixLabors.ImageSharp.PixelFormats.Rgb24> image)
        {
            var opencvImage = new Emgu.CV.Image<Emgu.CV.Structure.Bgr, byte>(image.Width, image.Height);
            var data = opencvImage.Data;
            for (var y = 0; y < image.Height; y++)
            {
                var pixelSpan = image.GetPixelRowSpan(y);
                for (var x = 0; x < image.Width; x++)
                {
                    data[y, x, 0] = pixelSpan[x].R;
                    data[y, x, 1] = pixelSpan[x].G;
                    data[y, x, 2] = pixelSpan[x].B;
                }
            }
            return opencvImage;
        }
    }
}