using System;
using System.Collections.Generic;
using Nyerguds.ImageManipulation;
using Nyerguds.Util;

namespace Nyerguds.GameData.Dynamix
{
    public class AgosCompression
    {
        /*/
        // Elvira 1 - English DOS Floppy
        AGOSGameDescription Elvira1DOSFloppy =
        {
            ADGameDescription desc = 
            {
                const char *gameId = "elvira1";
                const char *extra = "Floppy";
                ADGameFileDescription filesDescriptions[] = 
                {
                    // const char *fileName, uint16 fileType, const char *md5, int32 fileSize
                    { "gamepc",   GAME_BASEFILE, "a49e132a1f18306dd5d1ec2fe435e178", 135332},
                    { "icon.dat", GAME_ICONFILE, "fda48c9da7f3e72d0313e2f5f760fc45", 56448},
                    { "tbllist",  GAME_TBLFILE,  "319f6b227c7822a551f57d24e70f8149", 368},
                    { NULL,       0,             NULL,                               0}
                },
                Common::Language language = Common::EN_ANY;
                Common::Platform platform = Common::kPlatformDOS;
                uint32 flags = ADGF_NO_FLAGS;
                const char *guiOptions = GUIO1(GUIO_NOSPEECH);
            },
            int gameType = GType_ELVIRA1;
            int gameId = GID_ELVIRA1;
            uint32 features = GF_OLD_BUNDLE; // Old bundle games (ScummEngine_v3old and subclasses).
        };
        //*/
        
        public static void RleDecode(Byte[] buffer, Int32? startOffset, Int32? endOffset, Byte[] bufferOut, Int32 height, Int32 stride)
        {
            Int32 inPtr = startOffset ?? 0;
            Int32 inPtrEnd = endOffset.HasValue ? Math.Min(endOffset.Value, buffer.Length) : buffer.Length;
            Int32 outPtr = 0;

            // AGOS RLE implementation:
            // highest bit not set = followed by range of repeating bytes. Amount is (code + 1)
            // highest bit set = followed by range of non-repeating bytes Amount is (0x100 - code)
            // The bytes are written to the output array vertically; column by column.

            while (outPtr < bufferOut.Length && inPtr < inPtrEnd)
            {
                // get next code
                Int32 code = buffer[inPtr++];
                if (code == -1)
                    return;
                // RLE run
                if ((code & 0x80) == 0)
                {
                    if (inPtr >= inPtrEnd)
                        return;
                    Int32 run = code + 1;
                    Int32 rle = buffer[inPtr++];
                    for (UInt32 lcv = 0; lcv < run; lcv++)
                    {
                        if (outPtr >= bufferOut.Length)
                            return;
                        bufferOut[outPtr % height * stride + outPtr / height] = (Byte)rle;
                        outPtr++;
                    }
                }
                // raw run
                else
                {
                    Int32 run = 0x100-code;
                    for (UInt32 lcv = 0; lcv < run; lcv++)
                    {
                        if (inPtr >= inPtrEnd)
                            return;
                        Int32 data = buffer[inPtr++];
                        if (outPtr >= bufferOut.Length)
                            return;
                        bufferOut[outPtr % height * stride + outPtr / height] = (Byte)data;
                        outPtr++;
                    }
                }
            }
        }


        /// <summary>
        /// Applies Run-Length Encoding (RLE) to the given data.
        /// </summary>
        /// <param name="buffer">Input buffer</param>
        /// <param name="height">Height of the image in the buffer</param>
        /// <param name="stride">Stride of the image in the buffer</param>
        /// <returns>The run-length encoded data</returns>
        public static Byte[] RleEncode(Byte[] buffer, Int32 height, Int32 stride)
        {
            return RleEncode(buffer, 2, height, stride);
        }

        /// <summary>
        /// Applies Run-Length Encoding (RLE) to the given data.
        /// </summary>
        /// <param name="buffer">Input buffer</param>
        /// <param name="minimumRepeating">Minimum amount of repeating bytes before compression is applied.</param>
        /// <param name="height">Height of the image in the buffer</param>
        /// <param name="stride">Stride of the image in the buffer</param>
        /// <returns>The run-length encoded data</returns>
        public static Byte[] RleEncode(Byte[] buffer, Int32 minimumRepeating, Int32 height, Int32 stride)
        {
            if (minimumRepeating < 2)
                minimumRepeating = 2;
            Int32 inPtr = 0;
            Int32 outPtr = 0;
            // Ensure big enough buffer. Sanity check will be done afterwards.
            Byte[] bufferOut = new Byte[(buffer.Length * 3) / 2];

            // AGOS RLE implementation:
            // highest bit not set = followed by range of repeating bytes. Code is (Amount - 1)
            // highest bit set = followed by range of non-repeating bytes Code is (0x100 - Amount)
            // The bytes are read from the input array vertically; column by column.

            Int32 len = buffer.Length;
            Boolean repeatDetected = false;
            while (inPtr < len)
            {
                if (repeatDetected || HasRepeatingAhead(buffer, len, inPtr, minimumRepeating, height, stride))
                {
                    repeatDetected = false;
                    // Found more than (minimumRepeating) bytes. Worth compressing. Apply run-length encoding.
                    Int32 start = inPtr;
                    // Can go up to 0x80 since the final value is decremented by 1.
                    Int32 end = Math.Min(start + 0x80, len);
                    Byte cur = buffer[inPtr % height * stride + inPtr / height];
                    // Already checked these
                    inPtr += minimumRepeating;
                    // Increase inptr to the last repeated.
                    for (; inPtr < end && buffer[inPtr % height * stride + inPtr / height] == cur; inPtr++) { }
                    bufferOut[outPtr++] = (Byte)(inPtr - start - 1);
                    bufferOut[outPtr++] = cur;
                }
                else
                {
                    while (!repeatDetected && inPtr < len)
                    {
                        Int32 start = inPtr;
                        Int32 end = Math.Min(start + 0x7F, len);
                        for (; inPtr < end; inPtr++)
                        {
                            // detected bytes to compress after this one: abort.
                            if (!HasRepeatingAhead(buffer, len, inPtr, minimumRepeating, height, stride))
                                continue;
                            repeatDetected = true;
                            break;
                        }
                        bufferOut[outPtr++] = (Byte)(0x100 - (inPtr - start));
                        for (Int32 i = start; i < inPtr; i++)
                            bufferOut[outPtr++] = buffer[i % height * stride + i / height];
                    }
                }
            }
            Byte[] finalOut = new Byte[outPtr];
            Array.Copy(bufferOut, 0, finalOut, 0, outPtr);
            return finalOut;
        }

        public static Boolean HasRepeatingAhead(Byte[] buffer, Int32 max, Int32 ptr, Int32 minAmount, Int32 height, Int32 stride)
        {
            if (ptr + minAmount - 1 >= max)
                return false;
            Byte cur = buffer[ptr % height * stride + ptr / height];
            for (Int32 i = 1; i < minAmount; i++)
                if (buffer[(ptr + 1) % height * stride + (ptr + 1) / height] != cur)
                    return false;
            return true;
        }

    }
}