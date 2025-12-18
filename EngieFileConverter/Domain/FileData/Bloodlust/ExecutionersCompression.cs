using System;

namespace Nyerguds.FileData.Bloodlust
{
    public static class ExecutionersCompression
    {
        public static Byte[] DecodeChunk(Byte[] comprData, ref Int32 address, Byte emptyValue, ref Byte[] maskBuffer, Byte maskBufferFill, out Boolean success)
        {
            if (comprData[address] != 0x10 || comprData[address + 3] != 0xFF)
            {
                success = false;
                return null;
            }
            Int32 width = comprData[address + 1];
            Int32 height = comprData[address + 2];
            Int32 imageSize = width * height;
            Byte[] outBuffer = new Byte[imageSize];
            // Initialise buffer
            for (Int32 i = 0; i < imageSize; ++i)
                outBuffer[i] = emptyValue;
            address += 4;
            // Giving anything non-null will trigger generating the mask.
            if (maskBuffer != null && maskBuffer.Length != imageSize)
                maskBuffer = new Byte[imageSize];
            success = DecodeIntoBuffer(comprData, ref address, width, height, outBuffer, width, height, 0, 0, ref maskBuffer, maskBufferFill);
            return outBuffer;
        }

        public static Boolean DecodeIntoBuffer(Byte[] inBuffer, ref Int32 inPtr, Int32 imgWidth, Int32 imgHeight, Byte[] outBuffer, Int32 outWidth, Int32 outHeight, Int32 paintX, Int32 paintY, ref Byte[] maskBuffer, Byte maskBufferFill)
        {
            // Only write mask if buffer matches exactly.
            Boolean writeMask = maskBuffer != null && maskBuffer.Length == imgWidth * imgHeight;
            Int32 dataEnd = inBuffer.Length;
            Int32 writeEnd = outBuffer.Length;
            // Prevent wraparound
            Int32 outMaxX = Math.Min(outWidth, paintX + imgWidth);
            Int32 curLineStart = paintY * outWidth + paintX;
            Boolean error = false;
            Int32 maskWritePos = 0;
            for (Int32 line = 0; line < imgHeight; line++)
            {
                if (line == 40) 
                { }
                Int32 writePos = curLineStart;
                Int32 curLineEnd = curLineStart + outMaxX;
                Int32 linewritePosTheor = 0;
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
                    Byte code = inBuffer[inPtr++];
                    Boolean isFill = (code & 0x80) != 0;
                    Int32 amount = code & 0x7F;
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
                    Int32 runEndTheor = writePos + amount;
                    Int32 runEnd = Math.Min(curLineEnd, runEndTheor);
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
                            Int32 maskrunEndTheor = maskWritePos + amount;
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
        public static Byte[] Decode(Byte[] inBuffer, Int32 inPtr, Int32 imgWidth, Int32 imgHeight)
        {
            // Only write mask if buffer matches exactly.
            Byte[] outBuffer = new Byte[imgWidth * imgHeight];
            Int32 readPos = inPtr;
            Int32 writePos = 0;
            Int32 dataEnd = inBuffer.Length;
            Int32 writeEnd = outBuffer.Length;
            while (writePos < writeEnd && readPos < dataEnd)
            {
                if (writePos >= writeEnd)
                    break;
                // Unexpected end of data.
                if (inPtr >= dataEnd)
                    return null;
                Byte code = inBuffer[inPtr++];
                Boolean isFill = (code & 0x80) != 0;
                Int32 amount = code & 0x7F;
                if (writePos + amount > writeEnd)
                    break;
                Int32 runEnd = writePos + amount;
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

        public static Byte[] EncodeToChunk(Byte[] image, Int32 imgWidth, Int32 imgHeight, Byte emptyValue)
        {
            if (image == null)
                throw new ArgumentNullException("image");
            if (image.Length == 0)
                throw new ArgumentException("Image size cannot be 0.", "image");
            if (imgWidth == 0)
                throw new ArgumentException("Image size cannot be 0.", "imgWidth");
            if (imgHeight == 0)
                throw new ArgumentException("Image size cannot be 0.", "imgHeight");
            Int32 imgLen = imgWidth * imgHeight;
            if (imgLen > image.Length)
                throw new ArgumentException("Given data is too small to contain given image size.", "image");
            if (imgWidth > 0xFF)
                throw new ArgumentException("Image width cannot exceed 255.", "imgWidth");
            if (imgHeight > 0xFF)
                throw new ArgumentException("Image height cannot exceed 255.", "image");
            // Worst-case scenario: 175%
            Byte[] outputBuffer = new Byte[imgLen * 7 / 4];
            Int32 outPtr = 0;
            // Initially indicates the start position of the current line. During processing, this becomes the end position.
            Int32 linePos = 0;
            for (Int32 y = 0; y < imgHeight; ++y)
            {
                Int32 inPtr = linePos;
                linePos += imgWidth;
                while (inPtr < linePos)
                {
                    Int32 beforeRunPos = inPtr;
                    Boolean isRepeat = image[inPtr] == emptyValue;
                    Int32 maxPos = Math.Min(linePos, inPtr + 0x7F);
                    if (isRepeat)
                    {
                        for (; inPtr < maxPos && image[inPtr] == emptyValue; ++inPtr) { }
                        outputBuffer[outPtr++] = (Byte)(0x80 | (inPtr - beforeRunPos));
                    }
                    else
                    {
                        // Reserve byte for inserting code later
                        Int32 codePos = outPtr++;
                        for (; inPtr < maxPos && image[inPtr] != emptyValue; ++inPtr)
                            outputBuffer[outPtr++] = image[inPtr];
                        outputBuffer[codePos] = (Byte)(inPtr - beforeRunPos);
                    }
                }
            }
            Byte[] output = new Byte[outPtr + 4];
            output[0] = 0x10;
            output[1] = (Byte)imgWidth;
            output[2] = (Byte)imgHeight;
            output[3] = 0xFF;
            Array.Copy(outputBuffer, 0, output, 4, outPtr);
            return output;
        }

        public static Byte[] EncodeToChunk(Byte[] image, Int32 imgWidth, Int32 imgHeight, Byte[] transMask, Byte maskTransValue)
        {
            if (image == null)
                throw new ArgumentNullException("image");
            if (image.Length == 0)
                throw new ArgumentException("Image size cannot be 0.", "image");
            if (imgWidth == 0)
                throw new ArgumentException("Image size cannot be 0.", "imgWidth");
            if (imgHeight == 0)
                throw new ArgumentException("Image size cannot be 0.", "imgHeight");
            Int32 imgLen = imgWidth * imgHeight;
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
            Byte[] outputBuffer = new Byte[imgLen * 7 / 4];
            Int32 outPtr = 0;
            // Initially indicates the start position of the current line. During processing, this becomes the end position.
            Int32 linePos = 0;
            for (Int32 y = 0; y < imgHeight; ++y)
            {
                Int32 inPtr = linePos;
                linePos += imgWidth;
                while (inPtr < linePos)
                {
                    Int32 beforeRunPos = inPtr;
                    Boolean isRepeat = transMask[inPtr] == maskTransValue;
                    Int32 maxPos = Math.Min(linePos, inPtr + 0x7F);
                    if (isRepeat)
                    {
                        for (; inPtr < maxPos && transMask[inPtr] == maskTransValue; ++inPtr) { }
                        outputBuffer[outPtr++] = (Byte)(0x80 | (inPtr - beforeRunPos));
                    }
                    else
                    {
                        // Reserve byte for inserting code later
                        Int32 codePos = outPtr++;
                        for (; inPtr < maxPos && transMask[inPtr] != maskTransValue; ++inPtr)
                            outputBuffer[outPtr++] = image[inPtr];
                        outputBuffer[codePos] = (Byte)(inPtr - beforeRunPos);
                    }
                }
            }
            Byte[] output = new Byte[outPtr + 4];
            output[0] = 0x10;
            output[1] = (Byte)imgWidth;
            output[2] = (Byte)imgHeight;
            output[3] = 0xFF;
            Array.Copy(outputBuffer, output, outPtr);
            return output;
        }
    }
}
