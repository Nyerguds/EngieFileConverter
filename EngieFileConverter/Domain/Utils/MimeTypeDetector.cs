using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Nyerguds.Util
{
    public static class MimeTypeDetector
    {
        // The magic bytes for this actually identify any OLE type.
        private const string TYPE_DOC = "doc";
        private const string MIME_DOC = "application/msword";
        // All modern office files are technically just zip files with xml content.
        private const string TYPE_ZIP = "zip";
        private const string MIME_ZIP = "application/zip";

        private static Dictionary<string, byte[][]> MAGIC_BYTES = new Dictionary<string, byte[][]>(StringComparer.OrdinalIgnoreCase)
        {
            {"7z",      new byte[][] {new byte[] { 0x37, 0x7A, 0xBC, 0xAF, 0x27, 0x1C }}},
            {"accdb",   new byte[][] {new byte[] { 0x53, 0x74, 0x61, 0x6E, 0x64, 0x61, 0x72, 0x64, 0x20, 0x41, 0x43, 0x45, 0x20, 0x44, 0x42 }}},
            // Prefixing a 0 sized array indicates it is followed by a boolean mask before the real data.
            {"avi",     new byte[][] {new byte[0], new byte[] { 0x01, 0x01, 0x01, 0x01, 0x00, 0x00, 0x00, 0x00, 0x01, 0x01, 0x01, 0x01},
                                                   new byte[] { 0x52, 0x49, 0x46, 0x46, 0x00, 0x00, 0x00, 0x00, 0x41, 0x56, 0x49, 0x20 }}},
            {"bmp",     new byte[][] {new byte[] { 0x42, 0x4D }}},
            {"cab",     new byte[][] {new byte[] { 0x4D, 0x53, 0x43, 0x46 }}},
            {TYPE_DOC,  new byte[][] {new byte[] { 0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1 }}},
            {"exe",     new byte[][] {new byte[] { 0x4D, 0x5A }}},
            {"gif",     new byte[][] {new byte[] { 0x47, 0x49, 0x46, 0x38 }}},
            {"gz",      new byte[][] {new byte[] { 0x1F, 0x8B }}},
            {"ico",     new byte[][] {new byte[] { 0x00, 0x00, 0x01, 0x00 }}},
            {"jpg",     new byte[][] {new byte[] { 0xFF, 0xD8, 0xFF }}},
            {"mp3",     new byte[][] {new byte[] { 0xFF, 0xFB, 0x30 }, new byte[] { 0x49, 0x44, 0x33 } }},
            {"mp4",     new byte[][] {new byte[0], new byte[] { 0x00, 0x00, 0x00, 0x00, 0x01, 0x01, 0x01, 0x01},
                                                   new byte[] { 0x00, 0x00, 0x00, 0x00, 0x66, 0x74, 0x79, 0x70 }}},
            {"pcx",     new byte[][] {new byte[] { 0x0A, 0x00, 0x01 }, new byte[] { 0x0A, 0x02, 0x01 }, new byte[] { 0x0A, 0x03, 0x01 }, new byte[] { 0x0A, 0x04, 0x01 }, new byte[] { 0x0A, 0x05, 0x01 }}},
            {"pdf",     new byte[][] {new byte[] { 0x25, 0x50, 0x44, 0x46, 0x2D, 0x31, 0x2E }}},
            {"png",     new byte[][] {new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00, 0x00, 0x00, 0x0D, 0x49, 0x48, 0x44, 0x52 }}},
            {"rar",     new byte[][] {new byte[] { 0x52, 0x61, 0x72, 0x21, 0x1A, 0x07, 0x00 }, new byte[] { 0x52, 0x61, 0x72, 0x21, 0x1A, 0x07, 0x01, 0x00 } }},
            {"rtf",     new byte[][] {new byte[] { 0x7B, 0x5C, 0x72, 0x74, 0x66 }}},
            {"swf",     new byte[][] {new byte[] { 0x46, 0x57, 0x53 }, new byte[] { 0x43, 0x57, 0x53 }, new byte[] { 0x5A, 0x57, 0x53 }}},
            {"tiff",    new byte[][] {new byte[] { 0x49, 0x49, 0x2A, 0x00 }, new byte[] { 0x4D, 0x4D, 0x00, 0x2A } }},
            {"torrent", new byte[][] {new byte[] { 0x64, 0x38, 0x3A, 0x61, 0x6E, 0x6E, 0x6F, 0x75, 0x6E, 0x63, 0x65 }}},
            {"ttf",     new byte[][] {new byte[] { 0x00, 0x01, 0x00, 0x00, 0x00 }}},
            {"wav",     new byte[][] {new byte[0], new byte[] { 0x01, 0x01, 0x01, 0x01, 0x00, 0x00, 0x00, 0x00, 0x01, 0x01, 0x01, 0x01},
                                                   new byte[] { 0x52, 0x49, 0x46, 0x46, 0x00, 0x00, 0x00, 0x00, 0x57, 0x41, 0x56, 0x45 }}},
            {"webp",    new byte[][] {new byte[0], new byte[] { 0x01, 0x01, 0x01, 0x01, 0x00, 0x00, 0x00, 0x00, 0x01, 0x01, 0x01, 0x01},
                                                   new byte[] { 0x52, 0x49, 0x46, 0x46, 0x00, 0x00, 0x00, 0x00, 0x57, 0x45, 0x42, 0x50 }}},
            {TYPE_ZIP,  new byte[][] {new byte[] { 0x50, 0x4B, 0x03, 0x04 }, new byte[] { 0x50, 0x4B, 0x05, 0x06 }, new byte[] { 0x50, 0x4B, 0x07, 0x08 }}},
        };

        private static Dictionary<string, string> MIME_TYPES = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            {"7z",     "application/x-7z-compressed"},
            {"avi",    "video/x-msvideo"},
            {"bmp",    "image/bmp"},
            {"cab",    "application/vnd.ms-cab-compressed"},
            {TYPE_DOC, MIME_DOC},
            {"exe",    "application/x-msdownload"},
            {"gif",    "image/gif"},
            {"gz",     "application/gzip"},
            {"ico",    "image/vnd.microsoft.icon"},
            {"jpeg",   "image/jpeg"},
            {"jpg",    "image/jpeg"},
            {"mp3",    "audio/mpeg"},
            {"mp4",    "video/mp4"},
            {"pcx",    "image/vnd.zbrush.pcx"},
            {"pdf",    "application/pdf"},
            {"png",    "image/png"},
            {"rar",    "application/vnd.rar"},
            {"rtf",    "application/rtf"},
            {"swf",    "application/x-shockwave-flash"},
            {"tiff",   "image/tiff"},
            {"torrent","application/x-bittorrent"},
            {"ttf",    "font/ttf"},
            {"wav",    "audio/wav"},
            {"webp",   "image/webp"},
            {TYPE_ZIP, MIME_ZIP},
        };

        /// <summary>
        /// These all identify as the DOC type when using the pattern matching.
        /// </summary>
        private static Dictionary<string, string> MIME_TYPES_OLE = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            {TYPE_DOC, MIME_DOC},
            {"dot",    MIME_DOC},
            {"ppt",    "application/vnd.ms-powerpoint"},
            {"pot",    "application/vnd.ms-powerpoint"},
            {"pps",    "application/vnd.ms-powerpoint"},
            {"ppa",    "application/vnd.ms-powerpoint"},
            {"vsd",    "application/vnd.visio"},
            {"mdb",    "application/x-msaccess"},
            {"mpp",    "application/vnd.ms-project"},
            {"pub",    "application/x-mspublisher"},
            {"xls",    "application/vnd.ms-excel"},
            {"xlt",    "application/vnd.ms-excel"},
            {"xla",    "application/vnd.ms-excel"},
        };

        /// <summary>
        /// These all identify as the ZIP type when using the pattern matching.
        /// </summary>
        private static Dictionary<string, string> MIME_TYPES_ZIP = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            {TYPE_ZIP, MIME_ZIP},
            {"docx",   "application/vnd.openxmlformats-officedocument.wordprocessingml.document"},
            {"dotx",   "application/vnd.openxmlformats-officedocument.wordprocessingml.template"},
            {"docm",   "application/vnd.ms-word.document.macroEnabled.12"},
            {"dotm",   "application/vnd.ms-word.template.macroEnabled.12"},
            {"xlsx",   "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"},
            {"xltx",   "application/vnd.openxmlformats-officedocument.spreadsheetml.template"},
            {"xlsm",   "application/vnd.ms-excel.sheet.macroEnabled.12"},
            {"xltm",   "application/vnd.ms-excel.template.macroEnabled.12"},
            {"xlam",   "application/vnd.ms-excel.addin.macroEnabled.12"},
            {"xlsb",   "application/vnd.ms-excel.sheet.binary.macroEnabled.12"},
            {"pptx",   "application/vnd.openxmlformats-officedocument.presentationml.presentation"},
            {"potx",   "application/vnd.openxmlformats-officedocument.presentationml.template"},
            {"ppsx",   "application/vnd.openxmlformats-officedocument.presentationml.slideshow"},
            {"pptm",   "application/vnd.ms-powerpoint.presentation.macroEnabled.12"},
            {"potm",   "application/vnd.ms-powerpoint.template.macroEnabled.12"},
            {"ppsm",   "application/vnd.ms-powerpoint.slideshow.macroEnabled.12"},
            {"ppam",   "application/vnd.ms-powerpoint.addin.macroEnabled.12"},
            {"odt",    "application/vnd.oasis.opendocument.text"},
            {"ods",    "application/vnd.oasis.opendocument.spreadsheet"},
            {"odp",    "application/vnd.oasis.opendocument.presentation"},
        };

        private static readonly int BYTESTOREAD = MAGIC_BYTES.Values.Max(x => x.Length);

        /// <summary>
        /// The known types that can be used by the 
        /// </summary>
        public static string[] KnownTypes 
        {
            get
            {
                return MIME_TYPES
                    .Concat(MIME_TYPES_OLE)
                    .Concat(MIME_TYPES_ZIP)
                    .Select(x => x.Key)
                    .Distinct()
                    .OrderBy(k => k)
                    .ToArray();
            }
        }

        /// <summary>
        /// Gets a mime type from extension.
        /// </summary>
        /// <param name="extension">Extension of the file.</param>
        /// <returns>The found mime type, or "application/octet-stream" if it failed.</returns>
        public static string[] GetMimeTypeFromExtension(string extension)
        {
            string mimetype;
            if (extension != null)
            {
                if (MIME_TYPES.TryGetValue(extension, out mimetype))
                    return new string[] { extension, mimetype };
                if (MIME_TYPES_ZIP.TryGetValue(extension, out mimetype))
                    return new string[] { extension, mimetype };
                if (MIME_TYPES_OLE.TryGetValue(extension, out mimetype))
                    return new string[] { extension, mimetype };
            }
            return new string[] { "dat", "application/octet-stream" };
        }

        /// <summary>
        /// If a file identifies as "doc", it can be any of the OLE formats.
        /// Use this function to refine the result based on the file extension.
        /// </summary>
        /// <param name="extension">File extension</param>
        /// <returns>The type, based on the file extension.</returns>
        public static string[] GetOleMimeTypeFromExtension(string extension)
        {
            string mimetype;
            if (extension != null && MIME_TYPES_OLE.TryGetValue(extension, out mimetype))
                return new string[] { extension, mimetype };
            return new string[] { TYPE_DOC, MIME_DOC };
        }

        /// <summary>
        /// If a file identifies as "zip", it can be any of the modern Office formats.
        /// Use this function to refine the result based on the file extension.
        /// </summary>
        /// <param name="extension">File extension</param>
        /// <returns>The type, based on the file extension.</returns>
        public static string[] GetZipMimeTypeFromExtension(string extension)
        {
            string mimetype;
            if (extension != null && MIME_TYPES_ZIP.TryGetValue(extension, out mimetype))
                return new string[] { extension, mimetype };
            return new string[] { TYPE_ZIP, MIME_ZIP };
        }

        /// <summary>
        /// Attempts to identify the type, first based on the file content, then using the file extension
        /// of the given filename. See <see cref="KnownTypes"/> for the list of known extensions.
        /// </summary>
        /// <param name="inputPath">Input path.</param>
        /// <returns>A two-string array, containing a short type identifier, and the content type string.</returns>
        public static string[] GetMimeType(string inputPath)
        {
            byte[] file = new byte[BYTESTOREAD];
            using (FileStream fs = new FileStream(inputPath, FileMode.Open))
            {
                fs.Position = 0;
                int actualRead = 0;
                do actualRead += fs.Read(file, actualRead, BYTESTOREAD - actualRead);
                while (actualRead != BYTESTOREAD && fs.Position < fs.Length);
            }
            return GetMimeType(file, inputPath, 0);
        }

        /// <summary>
        /// Attempts to identify the type, first based on the content, then using the file extension
        /// of the given filename. See <see cref="KnownTypes"/> for the list of known extensions.
        /// </summary>
        /// <param name="input">Input bytes.</param>
        /// <param name="filename">Input filename (optional).</param>
        /// <param name="readOffset">Read offset in the bytes (optional).</param>
        /// <returns>A two-string array, containing a short type identifier, and the content type string.</returns>
        public static string[] GetMimeType(byte[] input, string filename = null, int readOffset = 0)
        {
            string[] mime = GetMimeType(input, readOffset);
            bool isDoc = mime[0] == TYPE_DOC;
            bool isZip = mime[0] == TYPE_ZIP;
            bool isUnk = mime[0] == "dat";
            if ((!isDoc && !isZip && !isUnk) || filename == null)
            {
                return mime;
            }
            string ext = Path.GetExtension(filename);
            if (isUnk)
            {
                mime = GetMimeTypeFromExtension(ext);
            }
            else if (isDoc)
            {
                mime = GetOleMimeTypeFromExtension(ext);
            }
            else if (isZip)
            {
                mime = GetZipMimeTypeFromExtension(ext);
            }
            return mime;
        }

        /// <summary>
        /// Attempts to identify the type based on the content. Note that without filename info,
        /// this cannot distinguish between different OLE or ZIP based formats, and will
        /// respectively return "doc" and "zip" for those.
        /// </summary>
        /// <param name="input">Input bytes.</param>
        /// <param name="readOffset">Read offset in the bytes (optional).</param>
        /// <returns>A two-string array, containing a short type identifier, and the content type string.</returns>
        public static string[] GetMimeType(byte[] input, int readOffset = 0)
        {
            string type = null;
            foreach (KeyValuePair<string, byte[][]> pair in MAGIC_BYTES)
            {
                if (MatchesBytePattern(input, pair.Value, readOffset))
                {
                    type = pair.Key;
                    break;
                }
            }
            return GetMimeTypeFromExtension(type);
        }

        /// <summary>
        /// Checks whether the input matches any known mime type. The extension can help to distinguish
        /// different types of Office formats that all identify as either OLE objects or zip files.
        /// </summary>
        /// <param name="input">input bytes</param>
        /// <param name="extension">The file extension</param>
        /// <param name="readOffset">Read offset in the data.</param>
        /// <returns>True if this data passes the magic values checks for that type.</returns>
        /// <exception cref="ArgumentException">The type (extension) was unknown. Check <see cref="KnownTypes"/> for the accepted list.</exception>
        public static bool MatchesMimeType(byte[] input, string extension, int readOffset = 0)
        {
            // Filter out special types.
            if (MIME_TYPES_OLE.ContainsKey(extension)) extension = TYPE_DOC;
            if (MIME_TYPES_ZIP.ContainsKey(extension)) extension = TYPE_ZIP;
            byte[][] identifiers;
            if (!MAGIC_BYTES.TryGetValue(extension, out identifiers))
                throw new ArgumentException("Unknown type.", "extension");
            return MatchesBytePattern(input, identifiers, readOffset);
        }

        private static bool MatchesBytePattern(byte[] input, byte[][] patterns, int readOffset = 0)
        {
            bool[] mask = null;
            for (int id = 0; id < patterns.Length; ++id)
            {
                byte[] pattern = patterns[id];
                // Check if the pattern is prefixed with a boolean mask.
                if (pattern.Length == 0 && patterns.Length > id + 2 && patterns[id + 1].Length == patterns[id + 2].Length)
                {
                    byte[] boolmask = patterns[id + 1];
                    mask = new bool[boolmask.Length];
                    for (int i = 0; i < mask.Length; ++i)
                    {
                        mask[i] = boolmask[i] != 0;
                    }
                    id += 2;
                    pattern = patterns[id];
                }
                else
                {
                    mask = null;
                }
                // Check length
                if (readOffset + input.Length < pattern.Length)
                {
                    continue;
                }
                // Check data
                bool aborted = false;
                for (int i = 0; i < pattern.Length; ++i)
                {
                    // Skip unmasked entries.
                    if (mask != null && !mask[i])
                    {
                        continue;
                    }
                    if (input[readOffset + i] != pattern[i])
                    {
                        aborted = true;
                        break;
                    }
                }
                if (!aborted)
                {
                    return true;
                }
            }
            return false;
        }
    }
}
