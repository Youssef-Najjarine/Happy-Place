using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Metadata.Profiles.Exif;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace HappyWorld.HappyPlace;

public static class TestImageGenerator {
    // Methods - Real Image Formats

    public static byte[] CreateJpeg(int widthPixels, int heightPixels) {
        using var image = new Image<Rgba32>(widthPixels, heightPixels);
        image.Mutate(x => x.BackgroundColor(Color.Red));
        using var stream = new MemoryStream();
        image.SaveAsJpeg(stream, new JpegEncoder { Quality = 85 });
        return stream.ToArray();
    }

    public static byte[] CreatePng(int widthPixels, int heightPixels) {
        using var image = new Image<Rgba32>(widthPixels, heightPixels);
        image.Mutate(x => x.BackgroundColor(Color.Green));
        using var stream = new MemoryStream();
        image.SaveAsPng(stream);
        return stream.ToArray();
    }

    public static byte[] CreateWebp(int widthPixels, int heightPixels) {
        using var image = new Image<Rgba32>(widthPixels, heightPixels);
        image.Mutate(x => x.BackgroundColor(Color.Blue));
        using var stream = new MemoryStream();
        image.SaveAsWebp(stream);
        return stream.ToArray();
    }

    public static byte[] CreateGif(int widthPixels, int heightPixels) {
        using var image = new Image<Rgba32>(widthPixels, heightPixels);
        image.Mutate(x => x.BackgroundColor(Color.Yellow));
        using var stream = new MemoryStream();
        image.SaveAsGif(stream);
        return stream.ToArray();
    }

    public static byte[] CreateBmp(int widthPixels, int heightPixels) {
        using var image = new Image<Rgba32>(widthPixels, heightPixels);
        image.Mutate(x => x.BackgroundColor(Color.Purple));
        using var stream = new MemoryStream();
        image.SaveAsBmp(stream);
        return stream.ToArray();
    }

    public static byte[] CreateTiff(int widthPixels, int heightPixels) {
        using var image = new Image<Rgba32>(widthPixels, heightPixels);
        image.Mutate(x => x.BackgroundColor(Color.Orange));
        using var stream = new MemoryStream();
        image.SaveAsTiff(stream);
        return stream.ToArray();
    }

    public static byte[] CreatePngWithTransparency(int widthPixels, int heightPixels) {
        using var image = new Image<Rgba32>(widthPixels, heightPixels, new Rgba32(255, 0, 0, 128));
        using var stream = new MemoryStream();
        image.SaveAsPng(stream);
        return stream.ToArray();
    }

    public static byte[] CreateJpegWithExifGpsData(int widthPixels, int heightPixels) {
        using var image = new Image<Rgba32>(widthPixels, heightPixels);
        image.Metadata.ExifProfile = new ExifProfile();
        Rational[] latitudeValues = [new(37), new(46), new(30)];
        Rational[] longitudeValues = [new(122), new(25), new(10)];
        image.Metadata.ExifProfile.SetValue(ExifTag.GPSLatitude, latitudeValues);
        image.Metadata.ExifProfile.SetValue(ExifTag.GPSLongitude, longitudeValues);
        image.Metadata.ExifProfile.SetValue(ExifTag.GPSLatitudeRef, "N");
        image.Metadata.ExifProfile.SetValue(ExifTag.GPSLongitudeRef, "W");
        image.Mutate(x => x.BackgroundColor(Color.Cyan));
        using var stream = new MemoryStream();
        image.SaveAsJpeg(stream);
        return stream.ToArray();
    }

    // Methods - Invalid Or Malicious Payloads

    public static byte[] CreateSvg() {
        string svgContent = "<?xml version=\"1.0\" encoding=\"UTF-8\"?><svg xmlns=\"http://www.w3.org/2000/svg\" width=\"100\" height=\"100\"><rect width=\"100\" height=\"100\" fill=\"red\"/></svg>";
        return System.Text.Encoding.UTF8.GetBytes(svgContent);
    }

    public static byte[] CreatePlainText() {
        return System.Text.Encoding.UTF8.GetBytes("This is just plain text and definitely not an image.");
    }

    public static byte[] CreateBogusBytesWithJpegMagicHeader() {
        byte[] bytes = new byte[200];
        bytes[0] = 0xFF;
        bytes[1] = 0xD8;
        bytes[2] = 0xFF;
        for (int i = 3; i < bytes.Length; i++) {
            bytes[i] = (byte)(i % 256);
        }
        return bytes;
    }

    public static byte[] CreateOversizedDummyBytes(int totalSizeInBytes) {
        byte[] bytes = new byte[totalSizeInBytes];
        bytes[0] = 0xFF;
        bytes[1] = 0xD8;
        bytes[2] = 0xFF;
        bytes[3] = 0xE0;
        return bytes;
    }
}
