using System;
using Nyerguds.FileData.Compression;
using Nyerguds.ImageManipulation;

namespace Nyerguds.FileData.Dynamix
{
    /// <summary>
    /// Dynamix compression / decompression class. Offers functionality to decompress chunks using RLE or LZW decompression,
    /// and has functions to compress to RLE.
    /// </summary>
    public class DynamixCompression
    {

        public static byte[] EnrichFourBit(byte[] vgaData, byte[] binData)
        {
            int len = vgaData.Length;
            byte[] fullData = new byte[len * 2];
            // ENRICHED 4-BIT IMAGE LOGIC
            // Basic principle: The data in the VGA chunk is already perfectly viewable as 4-bit image. The color palettes
            // are designed so each block of 16 colors consists of different tints of the same color. The 16-color palette
            // for the VGA chunk alone can be constructed by taking a palette slice where each color is 16 entries apart.

            // This VGA data [AB] gets "ennobled" to 8-bit by adding detail data [ab] from the BIN chunk, to get bytes [Aa Bb].
            for (int i = 0; i < len; ++i)
            {
                int offs = i * 2;
                // This can be written much simpler, but I expanded it to clearly show each step.
                byte vgaPix = vgaData[i]; // 0xAB
                byte binPix = binData[i]; // 0xab
                byte vgaPixHi = (byte)((vgaPix & 0xF0) >> 4); // 0x0A
                byte binPixHi = (byte)((binPix & 0xF0) >> 4); // 0x0a
                byte finalPixHi = (byte)((vgaPixHi << 4) + binPixHi); // Aa
                byte vgaPixLo = (byte)(vgaPix & 0x0F); // 0x0B
                byte binPixLo = (byte)(binPix & 0x0F); // 0x0b
                byte finalPixLo = (byte)((vgaPixLo << 4) + binPixLo); // Bb
                // Final result: AB + ab == [Aa Bb]
                fullData[offs] = finalPixHi;
                fullData[offs + 1] = finalPixLo;
            }
            return fullData;
        }

        public static void SplitEightBit(byte[] imageData, out byte[] vgaData, out byte[] binData)
        {
            int len = imageData.Length;
            vgaData = new byte[(len + 1) / 2];
            binData = new byte[(len + 1) / 2];
            for (int i = 0; i < len; ++i)
            {
                byte pixData = imageData[i];
                int pixHi = pixData & 0xF0;
                int pixLo = pixData & 0x0F;
                if (i % 2 == 0)
                    pixLo = pixLo << 4;
                else
                    pixHi = pixHi >> 4;
                int pixOffs = i / 2;
                vgaData[pixOffs] |= (byte)pixHi;
                binData[pixOffs] |= (byte)pixLo;
            }
        }

        /// <summary>
        /// Decompresses Dynamix chunk data. The chunk data should start with the compression
        /// type byte, followed by a 32-bit integer specifying the decompressed length.
        /// </summary>
        /// <param name="chunkData">Chunk data to decompress.</param>
        /// <returns>The decompressed data.</returns>
        public static byte[] DecodeChunk(byte[] chunkData)
        {
            if (chunkData.Length < 5)
                throw new ArgumentException("Chunk is too short to read compression header.");
            byte compression = chunkData[0];
            int decompressedLength = chunkData[4] << 24 | chunkData[3] << 16 | chunkData[2] << 8 | chunkData[1];
            return Decode(chunkData, 5, null, compression, decompressedLength);
        }

        /// <summary>
        /// Decompresses Dynamix data.
        /// </summary>
        /// <param name="buffer">Buffer to decompress.</param>
        /// <param name="startOffset">Start offset of the data in the buffer.</param>
        /// <param name="endOffset">End offset of the data in the buffer.</param>
        /// <param name="compression">Compression type: 0 for decompressed, 1 for RLE, 2 for LZA.</param>
        /// <param name="decompressedSize">The decompressed size.</param>
        /// <returns>The decompressed data.</returns>
        public static byte[] Decode(byte[] buffer, int? startOffset, int? endOffset, int compression, int decompressedSize)
        {
            int start = startOffset ?? 0;
            int end = endOffset ?? buffer.Length;
            if (end < start)
                throw new ArgumentException("End offset cannot be smaller than start offset.", "endOffset");
            if (start < 0 || start > buffer.Length)
                throw new ArgumentOutOfRangeException("startOffset");
            if (end < 0 || end > buffer.Length)
                throw new ArgumentOutOfRangeException("endOffset");
            switch (compression)
            {
                case 0:
                    byte[] outBuff = new byte[decompressedSize];
                    int len = Math.Min(end - start, decompressedSize);
                    Array.Copy(buffer, start, outBuff, 0, len);
                    return outBuff;
                case 1:
                    return RleDecode(buffer, (uint)start, (uint)end, decompressedSize, true);
                case 2:
                    return LzwDecode(buffer, start, end, decompressedSize);
                case 3:
                    return LzssDecode(buffer, start, end, decompressedSize);
                default:
                    throw new ArgumentException("Unknown compression type: \"" + compression + "\".", "compression");
            }
        }

        /// <summary>
        /// Decodes Run-Length Encoded (RLE) data.
        /// </summary>
        /// <param name="buffer">Buffer to read from.</param>
        /// <param name="startOffset">Start offset.</param>
        /// <param name="endOffset">End offset.</param>
        /// <param name="decompressedSize">Decompressed size.</param>
        /// <returns>The decompressed data.</returns>
        public static byte[] RleDecode(byte[] buffer, uint? startOffset, uint? endOffset, int decompressedSize, bool abortOnError)
        {
            byte[] outputBuffer = new byte[decompressedSize];
            // Uses standard RLE implementation.
            RleCompressionHighBitRepeat rle = new RleCompressionHighBitRepeat();
            rle.RleDecodeData(buffer, startOffset, endOffset, ref outputBuffer, abortOnError);
            return outputBuffer;
        }

        /// <summary>
        /// Applies Run-Length Encoding (RLE) to the given data.
        /// </summary>
        /// <param name="buffer">Input buffer.</param>
        /// <returns>The run-length encoded data.</returns>
        public static byte[] RleEncode(byte[] buffer)
        {
            // Uses standard RLE implementation.
            RleCompressionHighBitRepeat rle = new RleCompressionHighBitRepeat();
            return rle.RleEncodeData(buffer);
        }

        /// <summary>
        /// Decodes LZW data.
        /// </summary>
        /// <param name="buffer">Buffer to read from.</param>
        /// <param name="startOffset">Start offset.</param>
        /// <param name="endOffset">End offset.</param>
        /// <param name="decompressedSize">Decompressed size.</param>
        /// <returns>The decompressed data.</returns>
        public static byte[] LzwDecode(byte[] buffer, int? startOffset, int? endOffset, int decompressedSize)
        {
            DynamixLzwDecoder lzwDec = new DynamixLzwDecoder();
            byte[] outputBuffer = new byte[decompressedSize];
            lzwDec.LzwDecode(buffer, startOffset, endOffset, outputBuffer);
            return outputBuffer;
        }

        /// <summary>
        /// Applies LZW Encoding to the given data.
        /// </summary>
        /// <param name="buffer">Input buffer.</param>
        /// <returns>The run-length encoded data.</returns>
        public static byte[] LzwEncode(byte[] buffer)
        {
            throw new NotSupportedException("Sierra/Dynamix LZSS compression is currently not supported.");
            //DynamixLzwEncoder enc= new DynamixLzwEncoder();
            //return enc.Compress(buffer);
        }

        /// <summary>
        /// Decodes LZSS Huffman data.
        /// </summary>
        /// <param name="buffer">Buffer to read from.</param>
        /// <param name="startOffset">Start offset.</param>
        /// <param name="endOffset">End offset.</param>
        /// <param name="decompressedSize">Decompressed size.</param>
        /// <returns>The decompressed data.</returns>
        public static byte[] LzssDecode(byte[] buffer, int? startOffset, int? endOffset, int decompressedSize)
        {
            return LzssHuffDecoder.LzssDecode(buffer, startOffset, endOffset, decompressedSize);
        }

        /// <summary>
        /// Applies LZW Encoding to the given data.
        /// </summary>
        /// <param name="buffer">Input buffer.</param>
        /// <returns>The run-length encoded data.</returns>
        public static byte[] LzssEncode(byte[] buffer)
        {
            throw new NotSupportedException("Sierra/Dynamix LZSS compression is currently not supported.");
            //LzssHuffDecoder enc = new LzssHuffDecoder();
            //return null; // enc.Encode(buffer, null, null);
        }

        /// <summary>
        /// Decompresses the data from an SCN segment.
        /// </summary>
        /// <param name="buffer">The buffer to read from.</param>
        /// <param name="startOffset">Start offset. Null defaults to the buffer start.</param>
        /// <param name="endOffset">End offset. Null defaults to the buffer end.</param>
        /// <param name="width">Width of the image to decompress.</param>
        /// <param name="height">Height of the image to decompress.</param>
        /// <param name="bpp">Bits per pixel. If given as 8, the output will always return as 8-bit. Otherwide, it will be 4 unless higher-value data is detected.</param>
        /// <returns>The decompressed image.</returns>
        public static byte[] ScnDecode(byte[] buffer, int? startOffset, int? endOffset, int width, int height, ref int bpp)
        {
            // This function will write everything to an 8-bit buffer, and only convert it back afterwards.
            int decompressedSize = width * height;
            byte[] bufferOut = new byte[decompressedSize];
            int dataStart = startOffset ?? 0;
            int inPtr = dataStart;
            int inPtrEnd = (endOffset.HasValue ? Math.Min(endOffset.Value, buffer.Length) : buffer.Length);
            int outPtr = 0;
            int bufLen = inPtrEnd - inPtr;
            // Force it to 4 if it's not 8 to avoid illegal values.
            if (bpp != 8)
                bpp = 4;
            if (bufLen == 0)
                return bufferOut;
            byte addValue = buffer[inPtr++];
            // If the add value is more then 0x0F, the resulting image is 8-bit. The compressed content in the image is still only 4-bit,
            // but 8-bit images can have a data range of 0-F and ALSO transparency.
            if (addValue != 0xFF && addValue > 0x0F)
                bpp = 8;
            bool endLoop = false;
            int lastCommandPtr = 0;
            while (true)
            {
                if (inPtr >= inPtrEnd)
                    throw new ArgumentException(BuildScnDecodeErr(dataStart, lastCommandPtr, "No \"end of data\" marker found when decompressing SCN data."), "buffer");
                int curLine1 = outPtr / width;
                lastCommandPtr = inPtr;
                byte code = buffer[inPtr++];
                int command = code >> 6;
                int arg = code & 0x3F;
                switch (command)
                {
                    case 0: // Skip entire line length, minus [arg] pixels.
                        // Detect 2-byte end. Not strictly necessary, since it'd just get detected in the handling of command '1' in the next loop anyway.
                        if (arg == 0 && inPtr < inPtrEnd && buffer[inPtr] == 0x40)
                        {
                            endLoop = true;
                            break;
                        }
                        // check for joined commands
                        int arg2 = -1;
                        if (inPtr < inPtrEnd && buffer[inPtr] != 0 && buffer[inPtr] >> 6 == 0)
                            arg2 = (buffer[inPtr++] & 0x3F);
                        arg = (arg2 == -1) ? arg : (arg2 << 6) | arg;
                        if (arg > width)
                            throw new ArgumentException(BuildScnDecodeErr(dataStart, lastCommandPtr, "line skip command with value larger than image width."), "buffer");
                        outPtr += width - arg;
                        break;
                    case 1: // skip pixels. This does not use joined commands. If larger than 63 it will  just have multiple commands.
                        if (arg == 0)
                            endLoop = true;
                        else
                            outPtr += arg;
                        break;
                    case 2: // repeat pixels
                        //if (addValue == 0xFF)
                        //    throw new ArgumentException(BuildScnDecodeErr(dataStart, lastCommandPtr, "repeat command not supported for empty images."), "buffer");
                        if (inPtr >= inPtrEnd)
                            throw new ArgumentException(BuildScnDecodeErr(dataStart, lastCommandPtr, "can't read pixel to repeat."), "buffer");
                        byte repeatByte = (byte) (buffer[inPtr++] + addValue);
                        if (repeatByte > 0x0F && bpp == 4)
                            bpp = 8;
                        int repEnd = outPtr + arg;
                        if (repEnd > decompressedSize)
                            throw new ArgumentException(BuildScnDecodeErr(dataStart, lastCommandPtr, "repeat command attempted to write outside output buffer."), "buffer");
                        for (; outPtr < repEnd; ++outPtr)
                            bufferOut[outPtr] = repeatByte;
                        break;
                    case 3: // copy pixels
                        //if (addValue == 0xFF)
                        //    throw new ArgumentException(BuildScnDecodeErr(dataStart, lastCommandPtr, "copy command not supported for empty images."), "buffer");
                        if (arg == 0)
                            break;
                        int stride = ((arg * 4) + 7) / 8;
                        int skippedBytes = stride;
                        if (inPtr + stride > inPtrEnd)
                            throw new ArgumentException(BuildScnDecodeErr(dataStart, lastCommandPtr, "input buffer too small to read full copy command."), "buffer");
                        byte[] toWrite = ImageUtils.ConvertTo8Bit(buffer, arg, 1, inPtr, 4, true, ref stride);
                        inPtr += skippedBytes;
                        if (outPtr + arg > decompressedSize)
                            throw new ArgumentException(BuildScnDecodeErr(dataStart, lastCommandPtr, "copy command attempted to write outside output buffer."), "buffer");
                        for (int i = 0; i < arg && outPtr < decompressedSize; ++i)
                        {
                            byte copyByte = (byte) (toWrite[i] + addValue);
                            if (copyByte > 0x0F && bpp == 4)
                                bpp = 8;
                            bufferOut[outPtr++] = copyByte;
                        }
                        break;
                }
                if (endLoop)
                    break;
                int curLine2 = outPtr / width;
                // Checking if the encoding obeys the "no line wraparound" rules. There are three criteria that need to be true before it is allowed to fail:
                // - The line number progressed
                // - The command is not 0
                // - * The write pointer crossed over the end of the line
                //    -OR-
                //   * It ended up exactly at the end of the line, and there is no image end on the next command, and there is no line skip on the next command.
                if (curLine2 > curLine1 && command != 0 && (outPtr % width != 0 || (inPtr != inPtrEnd && buffer[inPtr] != 0x40 && (buffer[inPtr] >> 6) != 0)))
                    throw new ArgumentException(BuildScnDecodeErr(dataStart, lastCommandPtr, "Illegal line wrap detected."), "buffer");
            }
            return bpp == 8 ? bufferOut : ImageUtils.ConvertFrom8Bit(bufferOut, width, height, bpp, true);
        }

        private static string BuildScnDecodeErr(int dataStart, int lastCommandPtr, string message)
        {
            return "Bad data in SCN chunk [section 0x" + dataStart.ToString("X") + ", offset 0x" + lastCommandPtr.ToString("X") + "]: " + message;
        }

        /// <summary>
        /// Compresses the input image using the Dynamix SCN chunk compression.
        /// </summary>
        /// <param name="buffer">Image bytes buffer, with compact (minimum stride) data.</param>
        /// <param name="width">Width of the image.</param>
        /// <param name="height">Height of the image.</param>
        /// <param name="bpp">Bits per pixel of the input image. Can be 8-bit, as long as the non-0 values in the image are a consecutive range no longer than 16 values.</param>
        /// <param name="addFinalLineWrap">True to add a final line wrap at the end of the image contents.</param>
        /// <returns></returns>
        public static byte[] ScnEncode(byte[] buffer, int width, int height, int bpp, bool addFinalLineWrap)
        {
            // Maximum amount of identical pixels that will be stored in a non-repeat command.
            // This is 2 bytes, which would be the same length when saved as a repeat command or as part of an existing copy range.
            const int maxNonRepeat = 4;
            // The maximum line skip that can be stored is the combined 6-bit values of two skip commands, so, 12 bits.
            const int maxWidth = (1 << 12) - 1;
            if (width > maxWidth)
                throw new ArgumentException("SCN compression can't handle widths greater than " + maxWidth + ".", "width");

            byte[] buffer8Bit = bpp == 8 ? buffer : ImageUtils.ConvertTo8Bit(buffer, width, height, 0, bpp, true);
            byte maxVal = 0;
            byte minVal = 0xFF;
            bool allEmpty = true;
            for (int i = 0; i < buffer8Bit.Length; ++i)
            {
                byte curVal = buffer8Bit[i];
                if (curVal == 0)
                    continue;
                allEmpty = false;
                if (curVal < minVal)
                    minVal = curVal;
                if (curVal < maxVal)
                    maxVal = curVal;
            }
            if (allEmpty)
                minVal = 0xFF;
            else if (maxVal - minVal > 0xF)
                throw new ArgumentException("The non-0 data in the given image is not limited to a range of 16 consecutive values.", "buffer");

            // Can't be arsed to calculate worst case. This should be fine.
            byte[] outbuffer = new byte[buffer8Bit.Length * 3];
            int inPtr = 0;
            int inPtrEnd = buffer8Bit.Length;
            int outPtr = 0;
            outbuffer[outPtr++] = minVal;
            // Serves as maximum value for any operations
            int nextLineOffs = width;
            // Copy can handle a 2-repeat in just 2 bytes. Prioritise copy over repeat.
            // Need a repetition of at least 3 to make a repeat command worth it.
            while (inPtr < inPtrEnd)
            {
                byte curVal = buffer8Bit[inPtr++];
                int currentRepeat = 1;
                while (inPtr < nextLineOffs && buffer8Bit[inPtr] == curVal)
                {
                    currentRepeat++;
                    inPtr++;
                }
                byte? nextVal = inPtr < nextLineOffs ? buffer8Bit[inPtr] : (byte?) null;
                if (curVal != 0)
                {
                    // Repeat: written in one chunk. Either if the threshold value for not saving as copy is reached,
                    // or if the following part is a 00 and it's more than one byte to write it as copy.
                    // This will prevent "A A A" from being written as "C3 AA A0" rather than "83 0A".
                    if (currentRepeat >= maxNonRepeat || (currentRepeat > 2 && (!nextVal.HasValue || nextVal.Value == 0)))
                    {
                        while (currentRepeat >= maxNonRepeat)
                        {
                            int writeAmount = Math.Min(currentRepeat, 0x3F);
                            outbuffer[outPtr++] = (byte) (writeAmount | 0x80);
                            outbuffer[outPtr++] = (byte) (curVal - minVal);
                            currentRepeat -= writeAmount;
                        }
                        // Leave this for the next loop.
                        if (currentRepeat > 0)
                            inPtr -= currentRepeat;
                    }
                    else
                    {
                        int startPtr = inPtr - currentRepeat;
                        // Optimisation: take non-repeating bytes using an uneven amount as maximum. If this results in an even final amount of bytes,
                        // then any such uneven ranges ended up compensating for the spare dangling nibbles of the rest of the range.
                        int lookPtr = GetNonRepeatingRange(buffer8Bit, startPtr, curVal, nextLineOffs, maxNonRepeat + 1);
                        // Not even: take non-repeating normally.
                        if (lookPtr - startPtr > maxNonRepeat && (lookPtr - startPtr) % 2 != 0)
                            lookPtr = GetNonRepeatingRange(buffer8Bit, startPtr, curVal, nextLineOffs, maxNonRepeat);
                        int length = lookPtr - startPtr;
                        byte[] toCopy = new byte[length];
                        for (int i = 0; i < length; ++i)
                            toCopy[i] = (byte) (buffer8Bit[startPtr + i] - minVal);
                        int stride = length;
                        toCopy = ImageUtils.ConvertFrom8Bit(toCopy, length, 1, 4, true, ref stride);
                        outbuffer[outPtr++] = (byte) (length | 0xC0);
                        Array.Copy(toCopy, 0, outbuffer, outPtr, stride);
                        outPtr += stride;
                        inPtr = lookPtr;
                        currentRepeat = 0;
                    }
                }
                else if (inPtr < nextLineOffs)
                {
                    //Zeroes are NEVER handled with normal copy/repeat commands.
                    while (currentRepeat > 0)
                    {
                        int writeAmount = Math.Min(currentRepeat, 0x3F);
                        outbuffer[outPtr++] = (byte) (writeAmount | 0x40);
                        currentRepeat -= writeAmount;
                    }
                }
                // No "else": after anything that's written, it is checked if line ends need to be placed.
                if (inPtr >= nextLineOffs)
                {
                    if (inPtr == inPtrEnd)
                        break;
                    int linesToAdd = 1;
                    // Line skip: 00 command. In case more than one line is skipped, this needs to align itself to the point
                    // at or before where the data restarts on the next non-empty line.

                    // Three cases:
                    // - Current line ends on zero: (meaning, amount of repeated zeroes is already stored in 'currentRepeat')
                    //    * Look for end of zeroes, apply normal Skip&Align logic when real end is found.
                    // - Current line ends on non-zero, next line starts with zero:
                    //    * Ensure current pointer is treated as end of the previous line and not the start of the next
                    //    * Current line ended with non-zero (filled to end):
                    // - Current line ends on non-zero, next line starts with non-zero:
                    //    * Normal single line skip covering the entire line length.

                    // Check for zeroes on the next line to include
                    int toSubtract;
                    // Current line ends on non-zero, next line starts with zero.
                    bool atLineEnd = false;
                    if (curVal != 0 && buffer8Bit[inPtr] == 0)
                    {
                        // Reset to treat as "repeated zeroes already stored", but with a boolean indicating to treat the start differently.
                        curVal = 0;
                        currentRepeat = 0;
                        atLineEnd = true;
                    }
                    // Current line ends on zero, meaning, amount of repeated zeroes is already stored in 'currentRepeat':
                    if (curVal == 0)
                    {
                        // Continue scanning for zeroes over the entire image
                        while (inPtr < inPtrEnd && buffer8Bit[inPtr] == 0)
                        {
                            currentRepeat++;
                            inPtr++;
                        }
                        // End of data reached: the rest of the image is transparent. Break off the whole operation.
                        if (inPtr >= inPtrEnd)
                            break;
                        // Get the line numbers and X-coordinates to align to the start of the next data.
                        int start = inPtr - currentRepeat;
                        int startX = start % width;
                        int startLines = start / width;
                        // Special case: if the normal data ended at the end of a line, treat this as
                        // last offset on last line, instead of offset 0 on next line.
                        if (atLineEnd)
                        {
                            startLines--;
                            startX = width;
                        }
                        int end = inPtr;
                        int endX = end % width;
                        int endLines = end / width;
                        int diffX = startX - endX;
                        linesToAdd = endLines - startLines;
                        if (diffX >= 0)
                        {
                            // Align on end-X, then write the rest of the 'linesToAdd' as full like skip commands.
                            toSubtract = diffX;
                        }
                        else
                        {
                            // Write all 'linesToAdd' as full line skips. The next loop will fill the rest with a 0-repeat command.
                            toSubtract = 0;
                            inPtr = (endLines * width) + startX;
                        }
                    }
                    else
                    {
                        // Current is non-zero, next one is non-zero. Write a line skip that spans the entire image width.
                        toSubtract = width;
                    }
                    outbuffer[outPtr++] = (byte) (toSubtract & 0x3F);
                    if (toSubtract > 0x3F)
                        outbuffer[outPtr++] = (byte) ((toSubtract >> 6) & 0x3F);
                    for (int i = 1; i < linesToAdd; ++i)
                        outbuffer[outPtr++] = 0x00;
                    nextLineOffs += width * linesToAdd;
                }
            }
            if (addFinalLineWrap)
                outbuffer[outPtr++] = 0;
            // Add read end marker
            outbuffer[outPtr++] = 0x40;
            byte[] outbufFinal = new byte[outPtr];
            Array.Copy(outbuffer, outbufFinal, outPtr);
            return outbufFinal;
        }

        private static int GetNonRepeatingRange(byte[] buffer8Bit, int startPtr, int curVal, int nextLineOffs, int maxNonRepeat)
        {
            int lookPtr = startPtr;
            int beforeAbortLoopPtr = startPtr;
            int currentRepeat = 0;
            int prevVal = curVal;
            // Since this condition is at the start of the loop, and the loop body increases lookPtr,
            // the length needs to be checked as strictly smaller than 0x3F.
            while (currentRepeat <= maxNonRepeat & curVal != 0 && lookPtr < nextLineOffs && (lookPtr - startPtr) < 0x3F)
            {
                curVal = buffer8Bit[lookPtr];
                if (prevVal == curVal)
                    currentRepeat++;
                else
                {
                    // New value. Back up start point.
                    beforeAbortLoopPtr = lookPtr;
                    currentRepeat = 1;
                }
                lookPtr++;
                prevVal = curVal;
            }
            // Skip back to ptr just before a detected 3-byte repeat or 0 bytes
            if (currentRepeat > maxNonRepeat || curVal == 0)
            {
                lookPtr = beforeAbortLoopPtr;
            }
            return lookPtr;
        }

        /// <summary>Switches index 00 and FF on indexed image data, to compensate for this oddity in the MA8 chunks.</summary>
        /// <param name="imageData">Image data to process.</param>
        public static void SwitchBackground(byte[] imageData)
        {
            int len = imageData.Length;
            for (int i = 0; i < len; ++i)
            {
                if (imageData[i] == 0x00)
                    imageData[i] = 0xFF;
                else if (imageData[i] == 0xFF)
                    imageData[i] = 0x00;
            }
        }

    }
}