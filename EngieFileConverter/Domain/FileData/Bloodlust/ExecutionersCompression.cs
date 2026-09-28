using System;

namespace Nyerguds.FileData.Bloodlust
{
    public static class ExecutionersCompression
    {
        public static byte[] DecodeChunk(byte[] comprData, ref int address, byte emptyValue, ref byte[] maskBuffer, byte maskBufferFill, out bool success)
        {
            if (comprData[address] != 0x10 || comprData[address + 3] != 0xFF)
            {
                success = false;
                return null;
            }
            int width = comprData[address + 1];
            int height = comprData[address + 2];
            int imageSize = width * height;
            byte[] outBuffer = new byte[imageSize];
            // Initialise buffer
            for (int i = 0; i < imageSize; ++i)
                outBuffer[i] = emptyValue;
            address += 4;
            // Giving anything non-null will trigger generating the mask.
            if (maskBuffer != null && maskBuffer.Length != imageSize)
                maskBuffer = new byte[imageSize];
            success = DecodeIntoBuffer(comprData, ref address, width, height, outBuffer, width, height, 0, 0, ref maskBuffer, maskBufferFill);
            return outBuffer;
        }

        public static bool DecodeIntoBuffer(byte[] inBuffer, ref int inPtr, int imgWidth, int imgHeight, byte[] outBuffer, int outWidth, int outHeight, int paintX, int paintY, ref byte[] maskBuffer, byte maskBufferFill)
        {
            // Only write mask if buffer matches exactly.
            bool writeMask = maskBuffer != null && maskBuffer.Length == imgWidth * imgHeight;
            int dataEnd = inBuffer.Length;
            int writeEnd = outBuffer.Length;
            // Prevent wraparound
            int outMaxX = Math.Min(outWidth, paintX + imgWidth);
            int curLineStart = paintY * outWidth + paintX;
            bool error = false;
            int maskWritePos = 0;
            for (int line = 0; line < imgHeight; ++line)
            {
                if (line == 40) 
                { }
                int writePos = curLineStart;
                int curLineEnd = curLineStart + outMaxX;
                int linewritePosTheor = 0;
                if (writePos >= writeEnd)
                    break;
                while (linewritePosTheor < imgWidth)
                {
                    // Unexpected end of data.
                    if (inPtr >= dataEnd)
                    {
                        error = true;
                        break;
                    }
                    byte code = inBuffer[inPtr++];
                    bool isFill = (code & 0x80) != 0;
                    int amount = code & 0x7F;
                    if (isFill && amount == 0x7F)
                    {
                        //amount = curLineEnd - writePos;
                    }
                    // No more space to write; skip writing, keep decoding.
                    if (writePos + amount > writeEnd)
                    {
                        linewritePosTheor += amount;
                        continue;
                    }
                    int runEndTheor = writePos + amount;
                    int runEnd = Math.Min(curLineEnd, runEndTheor);
                    if (runEndTheor != runEnd)
                    {

                    }
                    if (isFill)
                    {
                        // Skip space
                        writePos = runEnd;
                        if (writeMask)
                        {
                            maskWritePos = line * imgWidth + linewritePosTheor;
                            int maskrunEndTheor = maskWritePos + amount;
                            for (; maskWritePos < maskrunEndTheor; ++maskWritePos)
                                maskBuffer[maskWritePos] = maskBufferFill;
                        }
                    }
                    else
                    {
                        if (inPtr + amount > dataEnd)
                        {
                            error = true;
                            break;
                        }
                        for (; writePos < runEnd; ++writePos)
                            outBuffer[writePos] = inBuffer[inPtr++];
                        // Account for line cutoff
                        inPtr += runEndTheor - runEnd;
                        // KEep track of this in case there is a premature end of the data.
                        if (writeMask)
                            maskWritePos += amount;
                    }
                    linewritePosTheor += amount;
                }
                if (error)
                    break;
                curLineStart += outWidth;
            }
            if (error)
            {
                if (writeMask)
                {
                    for (; maskWritePos < writeEnd; ++maskWritePos)
                        maskBuffer[maskWritePos] = maskBufferFill;
                }
                return false;

            }
            return true;
        }

        /// <summary>Old method. Decodes without header, and without taking image width into account.</summary>
        public static byte[] Decode(byte[] inBuffer, int inPtr, int imgWidth, int imgHeight)
        {
            // Only write mask if buffer matches exactly.
            byte[] outBuffer = new byte[imgWidth * imgHeight];
            int readPos = inPtr;
            int writePos = 0;
            int dataEnd = inBuffer.Length;
            int writeEnd = outBuffer.Length;
            while (writePos < writeEnd && readPos < dataEnd)
            {
                if (writePos >= writeEnd)
                    break;
                // Unexpected end of data.
                if (inPtr >= dataEnd)
                    return null;
                byte code = inBuffer[inPtr++];
                bool isFill = (code & 0x80) != 0;
                int amount = code & 0x7F;
                if (writePos + amount > writeEnd)
                    break;
                int runEnd = writePos + amount;
                if (isFill)
                {
                    // Skip space
                    writePos = runEnd;
                    for (; writePos < runEnd; ++writePos)
                        outBuffer[writePos] = 0xFF;
                }
                else
                {
                    if (inPtr + amount > dataEnd)
                        return null;
                    for (; writePos < runEnd; ++writePos)
                        outBuffer[writePos] = inBuffer[inPtr++];
                }
            }
            return outBuffer;
        }

        public static byte[] EncodeToChunk(byte[] image, int imgWidth, int imgHeight, byte emptyValue)
        {
            if (image == null)
                throw new ArgumentNullException("image");
            if (image.Length == 0)
                throw new ArgumentException("Image size cannot be 0.", "image");
            if (imgWidth == 0)
                throw new ArgumentException("Image size cannot be 0.", "imgWidth");
            if (imgHeight == 0)
                throw new ArgumentException("Image size cannot be 0.", "imgHeight");
            int imgLen = imgWidth * imgHeight;
            if (imgLen > image.Length)
                throw new ArgumentException("Given data is too small to contain given image size.", "image");
            if (imgWidth > 0xFF)
                throw new ArgumentException("Image width cannot exceed 255.", "imgWidth");
            if (imgHeight > 0xFF)
                throw new ArgumentException("Image height cannot exceed 255.", "image");
            // Worst-case scenario: 175%
            byte[] outputBuffer = new byte[imgLen * 7 / 4];
            int outPtr = 0;
            // Initially indicates the start position of the current line. During processing, this becomes the end position.
            int linePos = 0;
            for (int y = 0; y < imgHeight; ++y)
            {
                int inPtr = linePos;
                linePos += imgWidth;
                while (inPtr < linePos)
                {
                    int beforeRunPos = inPtr;
                    bool isRepeat = image[inPtr] == emptyValue;
                    int maxPos = Math.Min(linePos, inPtr + 0x7F);
                    if (isRepeat)
                    {
                        for (; inPtr < maxPos && image[inPtr] == emptyValue; ++inPtr) { }
                        outputBuffer[outPtr++] = (byte)(0x80 | (inPtr - beforeRunPos));
                    }
                    else
                    {
                        // Reserve byte for inserting code later
                        int codePos = outPtr++;
                        for (; inPtr < maxPos && image[inPtr] != emptyValue; ++inPtr)
                            outputBuffer[outPtr++] = image[inPtr];
                        outputBuffer[codePos] = (byte)(inPtr - beforeRunPos);
                    }
                }
            }
            byte[] output = new byte[outPtr + 4];
            output[0] = 0x10;
            output[1] = (byte)imgWidth;
            output[2] = (byte)imgHeight;
            output[3] = 0xFF;
            Array.Copy(outputBuffer, 0, output, 4, outPtr);
            return output;
        }

        public static byte[] EncodeToChunk(byte[] image, int imgWidth, int imgHeight, byte[] transMask, byte maskTransValue)
        {
            if (image == null)
                throw new ArgumentNullException("image");
            if (image.Length == 0)
                throw new ArgumentException("Image size cannot be 0.", "image");
            if (imgWidth == 0)
                throw new ArgumentException("Image size cannot be 0.", "imgWidth");
            if (imgHeight == 0)
                throw new ArgumentException("Image size cannot be 0.", "imgHeight");
            int imgLen = imgWidth * imgHeight;
            if (imgLen > image.Length)
                throw new ArgumentException("Given data is too small to contain given image size.", "image");
            if (imgWidth > 0xFF)
                throw new ArgumentException("Image width cannot exceed 255.", "imgWidth");
            if (imgHeight > 0xFF)
                throw new ArgumentException("Image height cannot exceed 255.", "image");
            if (transMask == null)
                throw new ArgumentNullException("transMask");
            if (transMask.Length != imgLen)
                throw new ArgumentException("Transparency mask size does not equal image size.", "transMask");
            byte[] outputBuffer = new byte[imgLen * 7 / 4];
            int outPtr = 0;
            // Initially indicates the start position of the current line. During processing, this becomes the end position.
            int linePos = 0;
            for (int y = 0; y < imgHeight; ++y)
            {
                int inPtr = linePos;
                linePos += imgWidth;
                while (inPtr < linePos)
                {
                    int beforeRunPos = inPtr;
                    bool isRepeat = transMask[inPtr] == maskTransValue;
                    int maxPos = Math.Min(linePos, inPtr + 0x7F);
                    if (isRepeat)
                    {
                        for (; inPtr < maxPos && transMask[inPtr] == maskTransValue; ++inPtr) { }
                        outputBuffer[outPtr++] = (byte)(0x80 | (inPtr - beforeRunPos));
                    }
                    else
                    {
                        // Reserve byte for inserting code later
                        int codePos = outPtr++;
                        for (; inPtr < maxPos && transMask[inPtr] != maskTransValue; ++inPtr)
                            outputBuffer[outPtr++] = image[inPtr];
                        outputBuffer[codePos] = (byte)(inPtr - beforeRunPos);
                    }
                }
            }
            byte[] output = new byte[outPtr + 4];
            output[0] = 0x10;
            output[1] = (byte)imgWidth;
            output[2] = (byte)imgHeight;
            output[3] = 0xFF;
            Array.Copy(outputBuffer, output, outPtr);
            return output;
        }
    }
}
