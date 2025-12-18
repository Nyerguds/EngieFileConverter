using System;

namespace Nyerguds.FileData.IGC
{
    /// <summary>
    /// Encodes and decodes the bit-mask based compression of the Interactive Girls Club images.
    ///
    /// The compression removes vertical repeats in image data and marks the remaining bytes in a
    /// bit mask. It seems to be designed as preprocessing step for RLE compression; in repeating
    /// dithering patterns of 8, 4 or 2 pixels wide, both the remaining bytes and the packed masks
    /// often end up being repeating byte values, allowing better RLE compression.
    /// </summary>
    /// <remarks>A big thanks to CTPAX-X Team for helping me figure out the format.</remarks>
    public static class IgcBitMaskCompression
    {
        /// <summary>
        /// Encodes to the bit-mask based compression of the Interactive Girls Club images.
        /// </summary>
        /// <param name="imageData">Image data.</param>
        /// <param name="stride">Amount of bytes in one pixel row in the image.</param>
        /// <param name="height">Height of the image.</param>
        /// <returns>The compressed image data with added bit masks.</returns>
        public static Byte[] BitMaskCompress(Byte[] imageData, Int32 stride, Int32 height)
        {
            Int32 inputLen = stride * height;
            if (inputLen > imageData.Length)
                throw new ArgumentException("Error compressing image: array too small to contain an image of the given dimensions!", "imageData");
            Int32 maskLength = (stride + 7) / 8;
            // Worst case: no duplicate pixels at all, means original size plus (height - 1) masks.
            Int32 outputLen = inputLen + maskLength * (height - 1);
            Byte[] imageDataCompr = new Byte[outputLen];
            // Copy first row to imageData
            Array.Copy(imageData, 0, imageDataCompr, 0, stride);
            // Set pointers to initial values after the first row.
            Int32 prevRowPtr = 0;
            Int32 inPtr = stride;
            Int32 writePtr = stride;
            for (Int32 y = 1; y < height; ++y)
            {
                // Set start of mask.
                Int32 bitmaskPtr = writePtr;
                // Set start of data.
                writePtr += maskLength;
                for (Int32 x = 0; x < stride; ++x)
                {
                    Byte val = imageData[inPtr + x];
                    // If identical, do nothing; mask is left on 0, data is not added.
                    if (imageData[prevRowPtr + x] == val)
                        continue;
                    // If new data, set mask bit, and write value. Downshift 0x80 because the bits are in big-endian order.
                    imageDataCompr[bitmaskPtr + x / 8] |= (Byte) (0x80 >> (x & 7));
                    imageDataCompr[writePtr++] = val;
                }
                prevRowPtr += stride;
                inPtr += stride;
            }
            Byte[] finalData = new Byte[writePtr];
            Array.Copy(imageDataCompr, 0, finalData, 0, writePtr);
            return finalData;
        }

        /// <summary>
        /// Decodes the bit-mask based compression of the Interactive Girls Club images.
        /// </summary>
        /// <param name="bitMaskData">Image data with bit masks.</param>
        /// <param name="stride">Amount of bytes in one pixel row in the image.</param>
        /// <param name="height">Height of the image.</param>
        /// <returns>The uncompressed stride*height image data.</returns>
        public static Byte[] BitMaskDecompress(Byte[] bitMaskData, Int32 stride, Int32 height)
        {
            Int32 inputLen = bitMaskData.Length;
            if (inputLen < stride)
                throw new ArgumentException("Not enough data to decompress image.", "bitMaskData");
            Int32 outputLen = stride * height;
            Byte[] imageData = new Byte[outputLen];
            Int32 maskLength = (stride + 7) / 8;
            // Copy first row to imageData
            Array.Copy(bitMaskData, 0, imageData, 0, stride);
            // Set pointers to initial values after the first row.
            Int32 prevRowPtr = 0;
            Int32 writePtr = stride;
            Int32 inPtr = stride;
            for (Int32 y = 1; y < height; ++y)
            {
                if (inputLen < inPtr + maskLength)
                    throw new ArgumentException("Error decompressing image.", "bitMaskData");
                // Set start of mask.
                Int32 bitmaskPtr = inPtr;
                // Set start of data.
                inPtr += maskLength;
                for (Int32 x = 0; x < stride; ++x)
                {
                    // Check bit in bit mask. Upshift and check 0x80 because the bits are in big-endian order.
                    if (((bitMaskData[bitmaskPtr + x / 8] << (x & 7)) & 0x80) != 0)
                    {
                        if (inPtr >= inputLen)
                            throw new ArgumentException("Error decompressing image.", "bitMaskData");
                        // Copy from RLE-uncompressed data
                        imageData[writePtr] = bitMaskData[inPtr++];
                    }
                    else
                    {
                        // Copy from previous row.
                        imageData[writePtr] = imageData[prevRowPtr + x];
                    }
                    writePtr++;
                }
                prevRowPtr += stride;
            }
            return imageData;
        }

    }
}