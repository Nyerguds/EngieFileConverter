using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Nyerguds.Util
{
    public static class MimeTypeDetector
    {
        private static Dictionary<String, Byte[]> KNOWN_TYPES = new Dictionary<String, Byte[]>()
            {
                {"bmp", new Byte[] { 66, 77 }},
                {"doc", new Byte[] { 208, 207, 17, 224, 161, 177, 26, 225 }},
                {"exe", new Byte[] { 77, 90 }},
                {"gif", new Byte[] { 71, 73, 70, 56 }},
                {"ico", new Byte[] { 0, 0, 1, 0 }},
                {"jpg", new Byte[] { 255, 216, 255 }},
                {"mp3", new Byte[] { 255, 251, 48 }},
                {"pdf", new Byte[] { 37, 80, 68, 70, 45, 49, 46 }},
                {"png", new Byte[] { 137, 80, 78, 71, 13, 10, 26, 10, 0, 0, 0, 13, 73, 72, 68, 82 }},
                {"rar", new Byte[] { 82, 97, 114, 33, 26, 7, 0 }},
                {"swf", new Byte[] { 70, 87, 83 }},
                {"tiff", new Byte[] { 73, 73, 42, 0 }},
                {"torrent", new Byte[] { 100, 56, 58, 97, 110, 110, 111, 117, 110, 99, 101 }},
                {"ttf", new Byte[] { 0, 1, 0, 0, 0 }},
                {"zip", new Byte[] { 80, 75, 3, 4 }},
                {"pcx", new Byte[] { 10 }},
            };

        private static Dictionary<String, String> MIME_TYPES = new Dictionary<String, String>()
            {
                {"bmp", "image/bmp"},
                {"doc", "application/msword"},
                {"exe", "application/x-msdownload"},
                {"gif", "image/gif"},
                {"ico", "image/x-icon"},
                {"jpg", "image/jpeg"},
                {"jpeg", "image/jpeg"},
                {"mp3", "audio/mpeg"},
                {"pcx", "image/vnd.zbrush.pcx"},
                {"pdf", "application/pdf"},
                {"png", "image/png"},
                {"rar", "application/x-rar-compressed"},
                {"swf", "application/x-shockwave-flash"},
                {"tiff", "image/tiff"},
                {"torrent", "application/x-bittorrent"},
                {"ttf", "application/x-font-ttf"},
                {"zip", "application/x-zip-compressed"},
            };

        private static readonly Int32 BYTESTOREAD = KNOWN_TYPES.Values.Max(x => x.Length);

        public static String[] GetMimeTypeFromExtension(String extension)
        {
            String mimetype;
            if (extension != null && MIME_TYPES.TryGetValue(extension, out mimetype))
                return new String[] { extension, mimetype };
            return new String[] { "dat", "application/octet-stream" };
        }

        public static String[] GetMimeType(String inputPath)
        {
            Byte[] file = new Byte[BYTESTOREAD];
            using (FileStream fs = new FileStream(inputPath, FileMode.Open))
            {
                fs.Position = 0;
                Int32 actualRead = 0;
                do actualRead += fs.Read(file, actualRead, BYTESTOREAD - actualRead);
                while (actualRead != BYTESTOREAD && fs.Position < fs.Length);
            }
            return GetMimeType(file);
        }

        public static String[] GetMimeType(Byte[] input)
        {
            String type = null;
            foreach (KeyValuePair<String, Byte[]> pair in KNOWN_TYPES)
            {
                Byte[] value = pair.Value;
                if (!input.Take(value.Length).SequenceEqual(value))
                    continue;
                type = pair.Key;
                break;
            }
            return GetMimeTypeFromExtension(type);
        }

    }
}
