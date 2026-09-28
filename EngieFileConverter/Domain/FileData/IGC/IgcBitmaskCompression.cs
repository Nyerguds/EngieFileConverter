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
        public static byte[] BitMaskCompress(byte[] imageData, int stride, int height)
        {
            int inputLen = stride * height;
            if (inputLen > imageData.Length)
                throw new ArgumentException("Error compressing image: array too small to contain an image of the given dimensions.", "imageData");
            int maskLength = (stride + 7) / 8;
            // Worst case: no duplicate pixels at all, means original size plus (height - 1) masks.
            int outputLen = inputLen + maskLength * (height - 1);
            byte[] imageDataCompr = new byte[outputLen];
            // Copy first row to imageData
            Array.Copy(imageData, 0, imageDataCompr, 0, stride);
            // Set pointers to initial values after the first row.
            int prevRowPtr = 0;
            int inPtr = stride;
            int writePtr = stride;
            for (int y = 1; y < height; ++y)
            {
                // Set start of mask.
                int bitmaskPtr = writePtr;
                // Set start of data.
                writePtr += maskLength;
                for (int x = 0; x < stride; ++x)
                {
                    byte val = imageData[inPtr + x];
                    // If identical, do nothing; mask is left on 0, data is not added.
                    if (imageData[prevRowPtr + x] == val)
                        continue;
                    // If new data, set mask bit, and write value. Downshift 0x80 because the bits are in big-endian order.
                    imageDataCompr[bitmaskPtr + x / 8] |= (byte) (0x80 >> (x & 7));
                    imageDataCompr[writePtr++] = val;
                }
                prevRowPtr += stride;
                inPtr += stride;
            }
            byte[] finalData = new byte[writePtr];
            Array.Copy(imageDataCompr, 0, finalData, 0, writePtr);
            return finalData;
        }

        /// <summary>
        /// Decodes the bit-mask based compression of the Interactive Girls Club images.
        /// </summary>
        /// <param name="bitMaskData">Image data with bit masks.</param>
        /// <param name="stride">Amount of bytes in one pixel row in the image.</param>
        /// <param name="height">Height of the image.</param>
        /// <returns>The decompressed stride*height image data.</returns>
        public static byte[] BitMaskDecompress(byte[] bitMaskData, int stride, int height)
        {
            int inputLen = bitMaskData.Length;
            if (inputLen < stride)
                throw new ArgumentException("Not enough data to decompress image.", "bitMaskData");
            int outputLen = stride * height;
            byte[] imageData = new byte[outputLen];
            int maskLength = (stride + 7) / 8;
            // Copy first row to imageData
            Array.Copy(bitMaskData, 0, imageData, 0, stride);
            // Set pointers to initial values after the first row.
            int prevRowPtr = 0;
            int writePtr = stride;
            int inPtr = stride;
            for (int y = 1; y < height; ++y)
            {
                if (inputLen < inPtr + maskLength)
                    throw new ArgumentException("Error decompressing image.", "bitMaskData");
                // Set start of mask.
                int bitmaskPtr = inPtr;
                // Set start of data.
                inPtr += maskLength;
                for (int x = 0; x < stride; ++x)
                {
                    // Check bit in bit mask. Upshift and check 0x80 because the bits are in big-endian order.
                    if (((bitMaskData[bitmaskPtr + x / 8] << (x & 7)) & 0x80) != 0)
                    {
                        if (inPtr >= inputLen)
                            throw new ArgumentException("Error decompressing image.", "bitMaskData");
                        // Copy from RLE-decompressed data
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