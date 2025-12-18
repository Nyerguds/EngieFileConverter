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
                {"gif", new Byte[] { 0x47, 0x49, 0x46, 0x38 }},
                {"ico", new Byte[] { 0x00, 0x00, 0x01, 0x00 }},
                {"jpg", new Byte[] { 0xFF, 0xD8, 0xFF }},
                {"mp3", new Byte[] { 255, 251, 48 }},
                {"pdf", new Byte[] { 37, 80, 68, 70, 45, 49, 46 }},
                {"png", new Byte[] { 137, 80, 78, 71, 13, 10, 26, 10, 0, 0, 0, 13, 73, 72, 68, 82 }},
                {"rar", new Byte[] { 82, 97, 114, 33, 26, 7, 0 }},
                {"swf", new Byte[] { 70, 87, 83 }},
                {"tiff", new Byte[] { 73, 73, 42, 0 }},
                {"torrent", new Byte[] { 100, 56, 58, 97, 110, 110, 111, 117, 110, 99, 101 }},
                {"ttf", new Byte[] { 0x00, 0x01, 0x00, 0x00, 0x00 }},
                {"zip", new Byte[] { 80, 75, 3, 4 }},
                {"pcx", new Byte[] { 0x0A }},
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
            return GetMimeType(file, 0);
        }

        public static Boolean MatchesMimeType(Byte[] input, String mimeType)
        {
            Byte[] identifier;
            if (!KNOWN_TYPES.TryGetValue(mimeType, out identifier))
                throw new ArgumentException("Unknown type.", "mimeType");
            return (input.Length >= identifier.Length && !input.Take(identifier.Length).SequenceEqual(identifier));
        }

        public static String[] GetMimeType(Byte[] input, int readOffset)
        {
            String type = null;
            foreach (KeyValuePair<String, Byte[]> pair in KNOWN_TYPES)
            {
                Byte[] value = pair.Value;
                if (!input.Skip(readOffset).Take(value.Length).SequenceEqual(value))
                    continue;
                type = pair.Key;
                break;
            }
            return GetMimeTypeFromExtension(type);
        }

    }
}
