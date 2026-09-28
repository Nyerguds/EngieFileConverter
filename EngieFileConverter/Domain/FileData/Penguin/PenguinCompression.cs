using System;
using Nyerguds.Util;

namespace Nyerguds.FileData.Compression.Penguin
{
    /// <summary>
    /// This is the compression system used in All Dogs Go To Heaven. It is a flag-based RLE that,
    /// when encountering the flag value 0x1B, will read a big-endian short value indicating the
    /// amount of repeats, and then the byte value to repeat. This means one flag block takes up
    /// four bytes.
    /// </summary>
    /// <remarks>
    /// It can be seen that this system is very far from optimal, since two, three and even four
    /// repeating bytes will not result in actually reduced file size. To avoid increasing the
    /// size, the compression doesn't store repeats smaller than four bytes as flags. Note that
    /// 4-byte repeats are stored as repeat flags; perhaps processing a flag is faster than reading
    /// and evaluating four separate bytes.
    /// 
    /// This poor compression scheme means that for CGA graphics, you need a minimum of 20 pixels
    /// that all have the same bytes(so 5 identical repeating blocks of 4 pixels) before any real
    /// compression can occur. Recompressing all existing archives with an identical compression,
    /// but adjusted to use only one byte for the repeat, already gives a file size decrease of
    /// 10%. The large open spaces on some of the images, like the title screen and dance animation
    /// in the intro, do mean both bytes of the repeat length are used quite often, though one
    /// could wonder if some strategic cropping of the graphics wouldn't have had the same effect
    /// on most such scenes.
    /// 
    /// Of course, all this may be a balancing of processing power versus file size; using images
    /// that are the full screen width avoids extra calculations for the positioning, and 1-byte
    /// repeat lengths would require multiple repeat flags to fill up space that can be filled in a
    /// single operation now, which is most likely a single CPU instruction.Perhaps the potential
    /// file size decrease wasn't worth having more processor-intensive decompression. 
    /// </remarks>
    public static class PenguinCompression
    {
        public static byte[] DecompressDogsFlagRle(byte[] compressedData, int dataStart, int compSize, int decompSize, byte flag)
        {
            return DecompressDogsFlagRle(compressedData, dataStart, compSize, decompSize, flag, false);
        }

        public static byte[] DecompressDogsFlagRle(byte[] compressedData, int dataStart, int compSize, int decompSize, byte flag, bool oneByteRepeat)
        {
            int minSkip = oneByteRepeat ? 2 : 3;
            if (dataStart + compSize > compressedData.Length)
                throw new ArgumentException("Given data is too small for the given data boundaries.", "compressedData");
            byte[] frameData = new byte[decompSize];
            if (compSize == decompSize)
            {
                Array.Copy(compressedData, dataStart, frameData, 0, decompSize);
                return frameData;
            }
            int readPtr = dataStart;
            int writePtr = 0;
            int dataEnd = dataStart + compSize;
            while (readPtr < dataEnd)
            {
                byte value = compressedData[readPtr++];
                if (value == flag)
                {
                    if (readPtr + minSkip > dataEnd)
                        throw new ArgumentException("Decompression failed: input too small for repeat command.", "compressedData");
                    int repeat;
                    if (oneByteRepeat)
                        repeat = compressedData[readPtr++];
                    else
                    {
                        repeat = ArrayUtils.ReadUInt16FromByteArrayBe(compressedData, readPtr);
                        readPtr += 2;
                    }
                    byte repVal = compressedData[readPtr++];
                    if (writePtr + repeat > decompSize)
                        throw new ArgumentException("Decompression failed: output buffer too small.", "compressedData");
                    int writeEnd = writePtr + repeat;
                    for (; writePtr < writeEnd; ++writePtr)
                        frameData[writePtr] = repVal;
                }
                else
                {
                    if (writePtr >= decompSize)
                        throw new ArgumentException("Decompression failed: output buffer too small.", "compressedData");
                    frameData[writePtr++] = value;
                }
            }
            return frameData;
        }


        public static byte[] CompressDogsFlagRle(byte[] fileData, byte flag)
        {
            return CompressDogsFlagRle(fileData, flag, false);
        }

        public static byte[] CompressDogsFlagRle(byte[] fileData, byte flag, bool oneByteRepeat)
        {
            int dataLen = fileData.Length;
            int minBlock = oneByteRepeat ? 3 : 4;
            int maxRepeatVal = oneByteRepeat ? 0xFF : 0xFFFF;
            byte[] compressData = new byte[dataLen];
            int readPtr = 0;
            int writePtr = 0;
            bool overflow = false;
            while (readPtr < dataLen)
            {
                byte value = fileData[readPtr];
                int repeat = 1;
                for (; repeat < ushort.MaxValue && repeat + readPtr < dataLen && fileData[readPtr + repeat] == value && repeat < maxRepeatVal; ++repeat) { }
                if (writePtr + Math.Min(repeat, minBlock) >= dataLen)
                {
                    // Can probably never overflow; the algo only compresses if it reduces. But I guess this shaves off the final compression operation?
                    overflow = true;
                    break;
                }
                readPtr += repeat;
                if (repeat >= minBlock || value == flag)
                {
                    compressData[writePtr++] = flag;
                    if (!oneByteRepeat)
                        compressData[writePtr++] = (byte) ((repeat >> 8) & 0xFF);
                    compressData[writePtr++] = (byte) (repeat & 0xFF);
                    compressData[writePtr++] = value;
                }
                else
                {
                    int writeEnd = writePtr + repeat;
                    for (; writePtr < writeEnd; ++writePtr)
                        compressData[writePtr] = value;
                }
            }
            if (overflow || writePtr == dataLen)
            {
                Array.Copy(fileData, compressData, dataLen);
                return compressData;
            }
            byte[] newData = new byte[writePtr];
            Array.Copy(compressData, newData, writePtr);
            return newData;
        }
    }
}