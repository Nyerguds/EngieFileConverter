using System;
using System.IO;
using Nyerguds.Util;

namespace Nyerguds.FileData.Compression.Penguin
{
    public static class PenguinCompression
    {
        public static Byte[] DecompressDogsFlagRle(Byte[] compressedData, Int32 dataStart, Int32 compSize, Int32 decompSize, Byte flag)
        {
            if (dataStart + compSize > compressedData.Length)
                throw new ArgumentException("Given data is too small for the given data boundaries.", "compressedData");
            Byte[] frameData = new Byte[decompSize];
            if (compSize == decompSize)
            {
                Array.Copy(compressedData, dataStart, frameData, 0, decompSize);
                return frameData;
            }            
            Int32 readPtr = dataStart;
            Int32 writePtr = 0;
            Int32 dataEnd = dataStart + compSize;
            while (readPtr < dataEnd)
            {
                Byte value = compressedData[readPtr++];
                if (value == flag)
                {
                    if (readPtr + 3 > dataEnd)
                        throw new ArgumentException("Decompression failed: input too small for repeat command.", "compressedData");
                    Int32 repeat = ArrayUtils.ReadUInt16FromByteArrayBe(compressedData, readPtr);
                    readPtr += 2;
                    Byte repVal = compressedData[readPtr++];
                    if (writePtr + repeat > decompSize)
                        throw new ArgumentException("Decompression failed: output buffer too small.", "compressedData");
                    Int32 writeEnd = writePtr + repeat;
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


        public static Byte[] CompressDogsFlagRle(Byte[] fileData, Byte flag)
        {
            Int32 dataLen = fileData.Length;
            Byte[] compressData = new Byte[dataLen];
            Int32 readPtr = 0;
            Int32 writePtr = 0;
            Boolean overflow = false;
            while (readPtr < dataLen)
            {
                Byte value = fileData[readPtr];
                Int32 repeat = 1;
                for (; repeat < UInt16.MaxValue && repeat + readPtr < dataLen && fileData[readPtr + repeat] == value; ++repeat) { }
                if (writePtr + Math.Min(repeat, 4) >= dataLen)
                {
                    overflow = true;
                    break;
                }
                readPtr += repeat;
                if (repeat >= 4 || value == flag)
                {
                    compressData[writePtr++] = flag;
                    compressData[writePtr++] = (Byte) ((repeat >> 8) & 0xFF);
                    compressData[writePtr++] = (Byte) (repeat & 0xFF);
                    compressData[writePtr++] = value;
                }
                else
                {
                    Int32 writeEnd = writePtr + repeat;
                    for (; writePtr < writeEnd; ++writePtr)
                        compressData[writePtr] = value;
                }
            }
            if (overflow || writePtr == dataLen)
            {
                Array.Copy(fileData, compressData, dataLen);
                return compressData;
            }
            Byte[] newData = new Byte[writePtr];
            Array.Copy(compressData, newData, writePtr);
            return newData;
        }
    }
}