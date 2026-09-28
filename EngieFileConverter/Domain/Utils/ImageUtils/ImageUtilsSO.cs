#if DEBUG
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace Nyerguds.ImageManipulation
{
    /// <summary>
    /// Code written for StackOverflow and not actively used in projects. Currently set to only compile in debug mode.
    /// If code from this actually gets used it should be moved into the main ImageUtils class.
    /// </summary>
    /// <remarks>
    /// Some of this code is written to be independent from the functions in the ImageUtils class, especially when it comes to
    /// getting data from an image. Obviously, if moved back, these functions can be rewired to use GetImageData, BuildImage, etc.
    /// </remarks>
    public static class ImageUtilsSO
    {

        /// <summary>
        /// Create bitmap from two-dimensional Int32 array.
        /// Written for a StackOverflow question.
        /// https://stackoverflow.com/a/49057879/395685
        /// </summary>
        /// <param name="data">Two-dimensional Int32 array containing colors.</param>
        /// <returns>Image.</returns>
        public static Bitmap FromTwoDimIntArray(int[,] data)
        {
            int width = data.GetLength(0);
            int height = data.GetLength(1);
            int byteIndex = 0;
            byte[] dataBytes = new byte[height * width * 4];
            for (int y = 0; y < height; ++y)
            {
                for (int x = 0; x < width; ++x)
                {
                    // UInt32 0xAARRGGBB = Byte[] { BB, GG, RR, AA }
                    uint val = (uint) data[x, y];
                    // This code clears out everything but a specific part of the value
                    // and then shifts the remaining piece down to the lowest byte
                    dataBytes[byteIndex + 0] = (byte) (val & 0x000000FF); // B
                    dataBytes[byteIndex + 1] = (byte) ((val & 0x0000FF00) >> 08); // G
                    dataBytes[byteIndex + 2] = (byte) ((val & 0x00FF0000) >> 16); // R
                    dataBytes[byteIndex + 3] = (byte) ((val & 0xFF000000) >> 24); // A
                    // More efficient than multiplying
                    byteIndex += 4;
                }
            }
            return ImageUtils.BuildImage(dataBytes, width, height, width, PixelFormat.Format32bppArgb, null, null);
        }

        /// <summary>
        /// Create bitmap from two-dimensional Int32 array containing greyscale data.
        /// Written for a StackOverflow question.
        /// https://stackoverflow.com/a/49057879/395685
        /// </summary>
        /// <param name="data">Two-dimensional Int32 array containing color data of a greyscale image.</param>
        /// <returns>Image.</returns>
        public static Bitmap FromTwoDimIntArrayGray(int[,] data)
        {
            int width = data.GetLength(0);
            int height = data.GetLength(1);
            int byteIndex = 0;
            byte[] dataBytes = new byte[height * width];
            for (int y = 0; y < height; ++y)
            {
                for (int x = 0; x < width; ++x)
                {
                    // Int32 0xAARRGGBB = Byte[] { BB, GG, RR, AA }
                    // This uses the lowest byte, which is the blue component.
                    dataBytes[byteIndex] = (byte) ((uint) data[x, y] & 0xFF);
                    // More efficient than multiplying
                    byteIndex++;
                }
            }
            Color[] palette = new Color[0x100];
            for (int i = 0; i < 0x100; ++i)
                palette[i] = Color.FromArgb(i, i, i);
            return ImageUtils.BuildImage(dataBytes, width, height, width, PixelFormat.Format8bppIndexed, palette, null);
        }

        /// <summary>
        /// Checks if a given image contains transparency.
        /// Written for a StackOverflow question.
        /// https://stackoverflow.com/a/53688608/395685
        /// </summary>
        /// <param name="bitmap">Input bitmap.</param>
        /// <returns>True if pixels were found with an alpha value of less than 255.</returns>
        public static bool HasTransparency(Bitmap bitmap)
        {
            // Not an alpha-capable color format. Note that GDI+ indexed images are alpha-capable on the palette.
            if (((ImageFlags) bitmap.Flags & ImageFlags.HasAlpha) == 0)
                return false;
            // Indexed format, and no alpha colors in the images palette: immediate pass.
            if ((bitmap.PixelFormat & PixelFormat.Indexed) != 0 && bitmap.Palette.Entries.All(c => c.A == 255))
                return false;
            // Get the byte data 'as 32-bit ARGB'. This offers a converted version of the image data without modifying the original image.
            BitmapData data = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            int len = bitmap.Height * data.Stride;
            byte[] bytes = new byte[len];
            Marshal.Copy(data.Scan0, bytes, 0, len);
            bitmap.UnlockBits(data);
            // Check the alpha bytes in the data. Since the data is little-endian, the actual byte order is [BB GG RR AA]
            for (int i = 3; i < len; i += 4)
                if (bytes[i] != 255)
                    return true;
            return false;
        }

        /// <summary>
        /// Test if an image is greyscale.
        /// Written for a StackOverflow question.
        /// https://stackoverflow.com/a/51154678/395685
        /// </summary>
        /// <param name="bitmap">Bitmap to check.</param>
        /// <returns>True if all visible pixels in the image are greyscale.</returns>
        public static bool IsGrayscale(Bitmap bitmap)
        {
            // Indexed format, and no non-gray colors in the images palette: immediate pass.
            if ((bitmap.PixelFormat & PixelFormat.Indexed) != 0 && bitmap.Palette.Entries.All(c => c.R == c.G && c.R == c.B))
                return true;
            int stride;
            byte[] data = ImageUtils.GetImageData(bitmap, out stride, PixelFormat.Format32bppArgb);
            int curRowOffs = 0;
            int height = bitmap.Height;
            int width = bitmap.Height;
            for (int y = 0; y < height; ++y)
            {
                // Set offset to start of current row
                int curOffs = curRowOffs;
                for (int x = 0; x < width; ++x)
                {
                    byte b = data[curOffs];
                    byte g = data[curOffs + 1];
                    byte r = data[curOffs + 2];
                    byte a = data[curOffs + 3];
                    // Increase offset to next color
                    curOffs += 4;
                    if (a == 0)
                        continue;
                    if (r != g || r != b)
                        return false;
                }
                // Increase row offset
                curRowOffs += stride;
            }
            return true;
        }

        /// <summary>
        /// Generates an 8-bit checkerboard pattern image.
        /// Written for a StackOverflow question.
        /// https://stackoverflow.com/a/50024853/395685
        /// </summary>
        /// <param name="width">The width of the generated image.</param>
        /// <param name="height">The height of the generated image.</param>
        /// <param name="colors">Color palette.</param>
        /// <param name="color1">Index for color 1.</param>
        /// <param name="color2">Index for color 2.</param>
        /// <returns>The checkerboard pattern image.</returns>
        public static Bitmap GenerateCheckerboardImage(int width, int height, Color[] colors, byte color1, byte color2)
        {
            if (width == 0 || height == 0)
                return null;
            byte[] patternArray = new byte[width * height];
            for (int y = 0; y < height; ++y)
            {
                for (int x = 0; x < width; ++x)
                {
                    int offset = x + y * height;
                    patternArray[offset] = (((x + y) % 2 == 0) ? color1 : color2);
                }
            }
            return ImageUtils.BuildImage(patternArray, width, height, width, PixelFormat.Format8bppIndexed, colors, Color.Empty);
        }

        /// <summary>
        /// Builds a gray image from CSV data.
        /// Written for a StackOverflow question.
        /// https://stackoverflow.com/a/49377762/395685
        /// </summary>
        /// <param name="lines">The CSV lines.</param>
        /// <param name="startColumn">Start column.</param>
        /// <param name="maxValue">Maximum value to stretch out to 255.</param>
        /// <returns>The image.</returns>
        public static Bitmap GrayImageFromCsv(string[] lines, int startColumn, int maxValue)
        {
            // maxValue cannot exceed 255
            maxValue = Math.Min(maxValue, 255);
            // Read lines; this gives us the data, and the height.
            //String[] lines = File.ReadAllLines(path);
            if (lines == null || lines.Length == 0)
                return null;
            int bottom = lines.Length;
            // Trim any empty lines from the start and end.
            while (bottom > 0 && lines[bottom - 1].Trim().Length == 0)
                bottom--;
            if (bottom == 0)
                return null;
            int top = 0;
            while (top < bottom && lines[top].Trim().Length == 0)
                top++;
            int height = bottom - top;
            // This removes the top-bottom stuff; the new array is compact.
            string[][] values = new string[height][];
            for (int i = top; i < bottom; ++i)
                values[i - top] = lines[i].Split(',');
            // Find width: maximum csv line length minus the amount of columns to skip.
            int width = values.Max(line => line.Length) - startColumn;
            if (width <= 0)
                return null;
            // Create the array. Since it's 8-bit, this is one byte per pixel.
            byte[] imageArray = new byte[width * height];
            // Parse all values into the array
            // Y = lines, X = csv values
            for (int y = 0; y < height; ++y)
            {
                int offset = y * width;
                // Skip indices before "startColumn". Target offset starts from the start of the line anyway.
                string[] yValues = values[y];
                int yValuesLen = yValues.Length;
                for (int x = startColumn; x < yValuesLen; ++x)
                {
                    int val;
                    // Don't know if Trim is needed here. Depends on the file.
                    if (Int32.TryParse(yValues[x].Trim(), out val))
                        imageArray[offset] = (byte) Math.Max(0, Math.Min(val, maxValue));
                    offset++;
                }
            }
            // generate gray palette for the given range, by calculating the factor to multiply by.
            double mulFactor = 255d / maxValue;
            Color[] palette = new Color[maxValue + 1];
            for (int i = 0; i <= maxValue; ++i)
            {
                // Away from zero rounding: 2.4 => 2 ; 2.5 => 3
                byte v = (byte) Math.Round(i * mulFactor, MidpointRounding.AwayFromZero);
                palette[i] = Color.FromArgb(v, v, v);
            }
            return ImageUtils.BuildImage(imageArray, width, height, width, PixelFormat.Format8bppIndexed, palette, Color.White);
        }

        /// <summary>
        /// Creates high-quality grayscale version of an image.
        /// Written for a StackOverflow question.
        /// https://stackoverflow.com/a/49441191/395685
        /// </summary>
        /// <param name="image">Input image.</param>
        /// <param name="width">Scaling width.</param>
        /// <param name="height">Scaling height.</param>
        /// <returns>The new image.</returns>
        public static Bitmap GetGrayImage(Image image, int width, int height)
        {
            // get image data
            Bitmap b = new Bitmap(image, width, height);
            BitmapData sourceData = b.LockBits(new Rectangle(0, 0, b.Width, b.Height), ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
            int stride = sourceData.Stride;
            byte[] data = new byte[stride * b.Height];
            Marshal.Copy(sourceData.Scan0, data, 0, data.Length);
            // iterate
            for (int y = 0; y < height; ++y)
            {
                int offset = y * stride;
                for (int x = 0; x < width; ++x)
                {
                    byte colB = data[offset + 0]; // B
                    byte colG = data[offset + 1]; // G
                    byte colR = data[offset + 2]; // R
                    //Int32 ColA = data[offset + 3]; // A
                    byte grayValue = GetGreyValue(colR, colG, colB);
                    data[offset + 0] = grayValue; // B
                    data[offset + 1] = grayValue; // G
                    data[offset + 2] = grayValue; // R
                    data[offset + 3] = 0xFF; // A
                    offset += 4;
                }
            }
            Marshal.Copy(data, 0, sourceData.Scan0, data.Length);
            b.UnlockBits(sourceData);
            return b;
        }

        public static Color GetGreyColor(Color color)
        {
            byte grey = GetGreyValue(color.R, color.G, color.B);
            return Color.FromArgb(grey, grey, grey);
        }

        public static byte GetGreyValue(Color color)
        {
            return GetGreyValue(color.R, color.G, color.B);
        }

        public static byte GetGreyValue(byte red, byte green, byte blue)
        {
            double redFactor = 0.2126d * Math.Pow(red, 2.2d);
            double grnFactor = 0.7152d * Math.Pow(green, 2.2d);
            double bluFactor = 0.0722d * Math.Pow(blue, 2.2d);
            double grey = Math.Pow(redFactor + grnFactor + bluFactor, 1d / 2.2);
            return (byte) Math.Max(0, Math.Min(255, Math.Round(grey, MidpointRounding.AwayFromZero)));
        }


        /// <summary>
        /// Takes Bayer sensor image with red, green and blue pixels, and extracts their values into an 8-bit image.
        /// Written for a StackOverflow question.
        /// https://stackoverflow.com/a/50038800/395685
        /// </summary>
        /// <param name="image">Bitmap with sensor data.</param>
        /// <param name="greenFirst">Indicates whether green is the first encountered pixel on the image.</param>
        /// <param name="blueRowFirst">Indicates whether the blue pixels are on the first or second row.</param>
        /// <returns>An 8-bit image.</returns>
        public static Bitmap BayerGridToGray(Bitmap image, bool greenFirst, bool blueRowFirst)
        {
            int stride;
            byte[] arr = GetImageData(image, out stride, PixelFormat.Format24bppRgb);
            int width = image.Width;
            int height = image.Height;

            byte[] result = new byte[width * height];
            for (int y = 0; y < height; ++y)
            {
                int curPtr = y * stride;
                int resPtr = y * width;
                for (int x = 0; x < width; ++x)
                {
                    // Get correct color components from sliding window
                    bool isGreen = (x + y) % 2 == (greenFirst ? 0 : 1);
                    bool blueRow = y % 2 == (blueRowFirst ? 0 : 1);
                    // BGR
                    byte blue = arr[curPtr + 0];
                    byte green = arr[curPtr + 1];
                    byte red = arr[curPtr + 2];
                    byte val = isGreen ? green : blueRow ? blue : red;

                    // Blue
                    result[resPtr + 0] = val;
                    // Green
                    result[resPtr + 1] = val;
                    // Red
                    result[resPtr + 2] = val;
                    curPtr += 3;
                    resPtr += 3;
                }
            }
            Bitmap resultImg = BuildImage(result, width, height, width, PixelFormat.Format8bppIndexed);
            ColorPalette palette = resultImg.Palette;
            for (int i = 0; i < 256; ++i)
                palette.Entries[i] = Color.FromArgb(i, i, i);
            return resultImg;
        }

        /// <summary>
        /// Build Bayer image from 8-bit sensor array. This function lacks end-of-row compensation
        /// algorithms, and will just return an image one pixel smaller than the given data.
        /// Written for a StackOverflow question.
        /// https://stackoverflow.com/a/50038800/395685
        /// </summary>
        /// <param name="arr">Array of sensor data.</param>
        /// <param name="width">Width of sensor data array.</param>
        /// <param name="height">Height of sensor data array.</param>
        /// <param name="stride">Stride of sensor data array.</param>
        /// <param name="greenFirst">Indicates whether green is the first encountered pixel on the image.</param>
        /// <param name="blueRowFirst">Indicates whether the blue pixels are on the first or second row.</param>
        /// <returns>The decoded image.</returns>
        public static byte[] BayerToRgb2x2Orig(byte[] arr, ref int width, ref int height, ref int stride, bool greenFirst, bool blueRowFirst)
        {
            int actualWidth = width - 1;
            int actualHeight = height - 1;
            int actualStride = actualWidth * 3;
            byte[] result = new byte[actualStride * actualHeight];
            for (int y = 0; y < actualHeight; ++y)
            {
                int curPtr = y * stride;
                int resPtr = y * actualStride;
                for (int x = 0; x < actualWidth; ++x)
                {
                    // Get correct color components from sliding window
                    bool isGreen = (x + y) % 2 == (greenFirst ? 0 : 1);
                    bool blueRow = y % 2 == (blueRowFirst ? 0 : 1);
                    byte cornerCol1 = isGreen ? arr[curPtr + 1] : arr[curPtr];
                    byte cornerCol2 = isGreen ? arr[curPtr + stride] : arr[curPtr + stride + 1];
                    byte greenCol1 = isGreen ? arr[curPtr] : arr[curPtr + 1];
                    byte greenCol2 = isGreen ? arr[curPtr + stride + 1] : arr[curPtr + stride];
                    byte blueCol = blueRow ? cornerCol1 : cornerCol2;
                    byte redCol = blueRow ? cornerCol2 : cornerCol1;
                    // 24bpp RGB is saved as [B, G, R].
                    // Blue
                    result[resPtr + 0] = blueCol;
                    // Green
                    result[resPtr + 1] = (byte) ((greenCol1 + greenCol2) / 2);
                    // Red
                    result[resPtr + 2] = redCol;
                    curPtr++;
                    resPtr += 3;
                }
            }
            height = actualHeight;
            width = actualWidth;
            stride = actualStride;
            return result;
        }

        /// <summary>
        /// Build Bayer image from 8-bit sensor array. This function fixes the last row by just copying the previous pixel.
        /// Written for a StackOverflow question.
        /// https://stackoverflow.com/a/50038800/395685
        /// </summary>
        /// <param name="arr">Array of sensor data.</param>
        /// <param name="width">Width of sensor data array.</param>
        /// <param name="height">Height of sensor data array.</param>
        /// <param name="stride">Stride of sensor data array.</param>
        /// <param name="greenFirst">Indicates whether green is the first encountered pixel on the image.</param>
        /// <param name="blueRowFirst">Indicates whether the blue pixels are on the first or second row.</param>
        /// <returns>The decoded image.</returns>
        public static byte[] BayerToRgb2x2CopyExpand(byte[] arr, int width, int height, ref int stride, bool greenFirst, bool blueRowFirst)
        {
            int processWidth = width;
            int processHeight = height;
            if (width > 1 && height > 1)
            {
                arr = ImageUtils.ChangeStride(arr, stride, height, width + 1, false, 0);
                stride = width + 1;
                processWidth = width + 1;
                byte[] lastColB = ImageUtils.CopyFrom8bpp(arr, width, height, stride, new Rectangle(width - 2, 0, 1, height));
                ImageUtils.PasteOn8bpp(arr, processWidth, height, stride, lastColB, 1, height, 1, new Rectangle(width, 0, 1, height), null, true);
                arr = ImageUtils.ChangeHeight(arr, stride, height, height + 1, false, 0);
                processHeight = height + 1;
                byte[] lastRowB = ImageUtils.CopyFrom8bpp(arr, processWidth, processHeight, stride, new Rectangle(0, height - 2, processWidth, 1));
                ImageUtils.PasteOn8bpp(arr, processWidth, processHeight, stride, lastRowB, processWidth, 1, processWidth, new Rectangle(0, height, processWidth, 1), null, true);
            }
            int lastCol = processWidth;
            int lastRow = processHeight;
            int actualStride = width * 3;
            byte[] result = new byte[actualStride * height];
            for (int y = 0; y < height; ++y)
            {
                int curPtr = y * stride;
                int resPtr = y * actualStride;
                for (int x = 0; x < width; ++x)
                {
                    // Get correct color components from sliding window
                    bool isGreen = (x + y) % 2 == (greenFirst ? 0 : 1); // all corner colors and center are green.
                    bool isBlueRow = y % 2 == (blueRowFirst ? 0 : 1);
                    byte valGreen;
                    byte valRed;
                    byte valBlue;
                    byte pxCol = arr[curPtr];
                    byte? tpCol1 = null;
                    byte? tpCol2 = null;
                    byte? tpCol3 = null;
                    byte? lfCol = null;
                    byte? rtCol = x == lastCol ? (byte?) null : arr[curPtr + 1];
                    byte? btCol1 = null;
                    byte? btCol2 = y == lastRow ? (byte?) null : arr[curPtr + stride];
                    byte? btCol3 = y == lastRow || x == lastCol ? (byte?) null : arr[curPtr + stride + 1];

                    if (isGreen)
                    {
                        valGreen = GetAverageCol(tpCol1, tpCol3, btCol1, btCol3, pxCol);
                        byte verVal = GetAverageCol(tpCol2, btCol2);
                        byte horVal = GetAverageCol(lfCol, rtCol);
                        valRed = isBlueRow ? verVal : horVal;
                        valBlue = isBlueRow ? horVal : verVal;
                    }
                    else
                    {
                        valGreen = GetAverageCol(tpCol2, rtCol, btCol2, lfCol);
                        byte cornerCol = GetAverageCol(tpCol1, tpCol3, btCol1, btCol3);
                        valRed = isBlueRow ? cornerCol : pxCol;
                        valBlue = isBlueRow ? pxCol : cornerCol;
                    }
                    result[resPtr + 0] = valBlue;
                    result[resPtr + 1] = valGreen;
                    result[resPtr + 2] = valRed;
                    curPtr++;
                    resPtr += 3;
                }
            }
            stride = actualStride;
            return result;
        }

        /// <summary>
        /// Build Bayer image from 8-bit sensor array. Experimental version that uses a 3x3 window.
        /// Written for a StackOverflow question.
        /// https://stackoverflow.com/a/50038800/395685
        /// </summary>
        /// <param name="arr">Array of sensor data.</param>
        /// <param name="width">Width of sensor data array.</param>
        /// <param name="height">Height of sensor data array.</param>
        /// <param name="stride">Stride of sensor data array.</param>
        /// <param name="greenFirst">Indicates whether green is the first encountered pixel on the image.</param>
        /// <param name="blueRowFirst">Indicates whether the blue pixels are on the first or second row.</param>
        /// <returns>The decoded image.</returns>
        public static byte[] BayerToRgb3x3(byte[] arr, int width, int height, ref int stride, bool greenFirst, bool blueRowFirst)
        {
            int lastCol = width - 1;
            int lastRow = height - 1;
            int actualStride = width * 3;
            byte[] result = new byte[actualStride * height];
            for (int y = 0; y < height; ++y)
            {
                int curPtr = y * stride;
                int resPtr = y * actualStride;
                for (int x = 0; x < width; ++x)
                {
                    // Get correct color components from sliding window
                    bool isGreen = (x + y) % 2 == (greenFirst ? 0 : 1); // all corner colors and center are green.
                    bool isBlueRow = y % 2 == (blueRowFirst ? 0 : 1);
                    byte valGreen;
                    byte valRed;
                    byte valBlue;

                    byte cntrCol = arr[curPtr];
                    byte? tplfCol = y == 0 || x == 0 ? (byte?) null : arr[curPtr - stride - 1];
                    byte? tpcnCol = y == 0 ? (byte?) null : arr[curPtr - stride];
                    byte? tprtCol = y == 0 || x == lastCol ? (byte?) null : arr[curPtr - stride + 1];
                    byte? cnlfCol = x == 0 ? (byte?) null : arr[curPtr - 1];
                    byte? cnrtCol = x == lastCol ? (byte?) null : arr[curPtr + 1];
                    byte? btlfCol = y == lastRow || x == 0 ? (byte?) null : arr[curPtr + stride - 1];
                    byte? btcnCol = y == lastRow ? (byte?) null : arr[curPtr + stride];
                    byte? btrtCol = y == lastRow || x == lastCol ? (byte?) null : arr[curPtr + stride + 1];

                    if (isGreen)
                    {
                        valGreen = GetAverageCol(tplfCol, tprtCol, btlfCol, btrtCol, cntrCol);
                        byte verVal = GetAverageCol(tpcnCol, btcnCol);
                        byte horVal = GetAverageCol(cnlfCol, cnrtCol);
                        valRed = isBlueRow ? verVal : horVal;
                        valBlue = isBlueRow ? horVal : verVal;
                    }
                    else
                    {
                        valGreen = GetAverageCol(tpcnCol, cnrtCol, btcnCol, cnlfCol);
                        byte cornerCol = GetAverageCol(tplfCol, tprtCol, btlfCol, btrtCol);
                        valRed = isBlueRow ? cornerCol : cntrCol;
                        valBlue = isBlueRow ? cntrCol : cornerCol;
                    }
                    result[resPtr + 0] = valBlue;
                    result[resPtr + 1] = valGreen;
                    result[resPtr + 2] = valRed;
                    curPtr++;
                    resPtr += 3;
                }
            }
            stride = actualStride;
            return result;
        }

        /// <summary>
        /// Processing function for Bayer decoding.
        /// </summary>
        /// <param name="cols">Bytes to take the average from.</param>
        /// <returns>The average value, or 0x80 if no values were given.</returns>
        private static byte GetAverageCol(params byte?[] cols)
        {
            int colsCount = 0;
            int colsLength = cols.Length;
            for (int i = 0; i < colsLength; ++i)
                if (cols[i].HasValue) colsCount++;
            int avgVal = 0;
            for (int i = 0; i < colsLength; ++i)
                avgVal += cols[i].GetValueOrDefault();
            return colsCount == 0 ? (byte) 0x80 : (byte) (avgVal / colsCount);
        }

        /// <summary>
        /// Build Bayer image from 8-bit sensor array. Fills in left and bottom row with copied content.
        /// </summary>
        /// <param name="arr">Array of sensor data.</param>
        /// <param name="width">Width of sensor data array.</param>
        /// <param name="height">Height of sensor data array.</param>
        /// <param name="stride">Stride of sensor data array.</param>
        /// <param name="greenFirst">Indicates whether green is the first encountered pixel on the image.</param>
        /// <param name="blueRowFirst">Indicates whether the blue pixels are on the first or second row.</param>
        /// <returns>The decoded image.</returns>
        public static byte[] BayerToRgb2x2Expand(byte[] arr, ref int width, ref int height, ref int stride, bool greenFirst, bool blueRowFirst)
        {
            int processWidth = width - 1;
            int processHeight = height - 1;
            int lastWidth = width - 2;
            int lastHeight = height - 2;
            int newStride = width * 3;
            byte[] result = new byte[newStride * height];
            for (int y = 0; y < processHeight; ++y)
            {
                int curPtr = y * stride;
                int resPtr = y * newStride;
                for (int x = 0; x < processWidth; ++x)
                {
                    // Get correct color components from sliding window
                    bool isGreen = (x + y) % 2 == (greenFirst ? 0 : 1);
                    bool blueRow = y % 2 == (blueRowFirst ? 0 : 1);
                    byte cornerCol1 = isGreen ? arr[curPtr + 1] : arr[curPtr];
                    byte cornerCol2 = isGreen ? arr[curPtr + stride] : arr[curPtr + stride + 1];
                    byte greenCol1 = isGreen ? arr[curPtr] : arr[curPtr + 1];
                    byte greenCol2 = isGreen ? arr[curPtr + stride + 1] : arr[curPtr + stride];
                    byte redCol = blueRow ? cornerCol2 : cornerCol1;
                    byte greenCol = (byte) ((greenCol1 + greenCol2) / 2);
                    byte blueCol = blueRow ? cornerCol1 : cornerCol2;
                    // 24bpp RGB is saved as [B, G, R].
                    result[resPtr + 0] = blueCol;
                    result[resPtr + 1] = greenCol;
                    result[resPtr + 2] = redCol;
                    // fill last column
                    if (x == lastWidth)
                    {
                        result[resPtr + 3] = blueCol;
                        result[resPtr + 4] = greenCol;
                        result[resPtr + 5] = redCol;
                    }
                    // fill last row
                    if (y == lastHeight)
                    {
                        result[resPtr + newStride + 0] = blueCol;
                        result[resPtr + newStride + 1] = greenCol;
                        result[resPtr + newStride + 2] = redCol;
                    }
                    // fill last pixel
                    if (x == lastWidth && y == lastHeight)
                    {
                        result[resPtr + newStride + 3] = blueCol;
                        result[resPtr + newStride + 4] = greenCol;
                        result[resPtr + newStride + 5] = redCol;
                    }
                    curPtr++;
                    resPtr += 3;
                }
            }
            stride = newStride;
            return result;
        }

        /// <summary>
        /// Extracts a channel as two-dimensional array.
        /// Written for a StackOverflow question.
        /// https://stackoverflow.com/a/50077006/395685
        /// </summary>
        /// <param name="image">Input image.</param>
        /// <param name="channelNr">0 = B, 1 = G, 2 = R, 3 = A.</param>
        /// <returns>The requested channel, as two-dimensional Int32 array.</returns>
        public static byte[] GetChannelBytes(Bitmap image, int channelNr)
        {
            if (channelNr >= 4 || channelNr < 0)
                throw new IndexOutOfRangeException();
            int width = image.Width;
            int height = image.Height;
            int stride;
            byte[] dataBytes = ImageUtils.GetImageData(image, out stride, PixelFormat.Format32bppArgb);
            byte[] channel = new byte[height * width];
            int readLineOffs = 0;
            int writeLineOffs = 0;
            for (int y = 0; y < height; ++y)
            {
                int readOffs = readLineOffs;
                int writeOffs = writeLineOffs;
                for (int x = 0; x < width; ++x)
                {
                    channel[writeOffs] = dataBytes[readOffs + channelNr];
                    readOffs += 4;
                    writeOffs++;
                }
                readLineOffs += stride;
                writeLineOffs += width;
                ;

            }
            return channel;
        }

        /// <summary>
        /// Extracts a channel as two-dimensional array.
        /// Written for a StackOverflow question.
        /// https://stackoverflow.com/a/50077006/395685
        /// </summary>
        /// <param name="image">Input image.</param>
        /// <param name="channelNr">0 = B, 1 = G, 2 = R.</param>
        /// <returns>The requested channel, as two-dimensional Int32 array.</returns>
        public static int[,] GetChannel(Bitmap image, int channelNr)
        {
            if (channelNr >= 3 || channelNr < 0)
                throw new IndexOutOfRangeException();
            int width = image.Width;
            int height = image.Height;
            int stride;
            byte[] dataBytes = ImageUtils.GetImageData(image, out stride, PixelFormat.Format24bppRgb);
            int[,] channel = new int[height, width];
            int readLineOffs = 0;
            for (int y = 0; y < height; ++y)
            {
                int readOffs = readLineOffs;
                for (int x = 0; x < width; ++x)
                {
                    channel[y, x] = dataBytes[readOffs + channelNr];
                    readOffs += 3;
                }
                readLineOffs += stride;
            }
            return channel;
        }

        /// <summary>
        /// Resizes a channel by skipping pixels.
        /// Written for a StackOverflow question.
        /// https://stackoverflow.com/a/50077006/395685
        /// </summary>
        /// <param name="origChannel">channel data.</param>
        /// <param name="lossfactor">Loss factor: amount to divide original image dimensions by.</param>
        /// <returns>The reduced channel.</returns>
        public static int[,] ReduceChannel(int[,] origChannel, int lossfactor)
        {
            int newHeight = origChannel.GetLength(0) / lossfactor;
            int newWidth = origChannel.GetLength(1) / lossfactor;
            // to avoid rounding errors
            int origHeight = newHeight * lossfactor;
            int origWidth = newWidth * lossfactor;
            int[,] newChannel = new int[newHeight, newWidth];
            int newY = 0;
            for (int y = 1; y < origHeight; y += lossfactor)
            {
                int newX = 0;
                for (int x = 1; x < origWidth; x += lossfactor)
                {
                    newChannel[newY, newX] = origChannel[y, x];
                    newX++;
                }
                newY++;
            }
            return newChannel;
        }

        /// <summary>
        /// Creates an image from color channels.
        /// Written for a StackOverflow question.
        /// https://stackoverflow.com/a/50077006/395685
        /// </summary>
        /// <param name="redChannel">Red channel data.</param>
        /// <param name="greenChannel">Green channel data.</param>
        /// <param name="blueChannel">Blue channel data.</param>
        /// <returns>The final image.</returns>
        public static Bitmap CreateImageFromChannels(int[,] redChannel, int[,] greenChannel, int[,] blueChannel)
        {
            int width = greenChannel.GetLength(1);
            int height = greenChannel.GetLength(0);
            Bitmap result = new Bitmap(width, height, PixelFormat.Format24bppRgb);
            Rectangle rect = new Rectangle(0, 0, width, height);
            BitmapData bmpData = result.LockBits(rect, ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);
            int stride = bmpData.Stride;
            // stride is the actual line width in bytes.
            int bytes = stride * height;
            byte[] PixelValues = new byte[bytes];
            for (int y = 0; y < height; ++y)
            {
                // use stride to get the start offset of each line
                int offset = y * stride;
                for (int x = 0; x < width; ++x)
                {
                    PixelValues[offset + 0] = (byte) blueChannel[y, x];
                    PixelValues[offset + 1] = (byte) greenChannel[y, x];
                    PixelValues[offset + 2] = (byte) redChannel[y, x];
                    offset += 3;
                }
            }
            Marshal.Copy(PixelValues, 0, bmpData.Scan0, bytes);
            result.UnlockBits(bmpData);
            return result;
        }

        /// <summary>
        /// Reduces a bitmap to 1 bit per pixel.
        /// Written for a StackOverflow question.
        /// https://stackoverflow.com/a/51143150/395685
        /// </summary>
        /// <param name="source">Source image to reduce.</param>
        /// <returns>Pure black and white 1bpp version of the input image.</returns>
        public static Bitmap ConvertTo1Bpp(Bitmap source)
        {
            PixelFormat sourcePf = source.PixelFormat;
            if ((sourcePf & PixelFormat.Indexed) == 0 || Image.GetPixelFormatSize(sourcePf) == 1)
                return BitmapTo1Bpp(source);
            using (Bitmap bm32 = new Bitmap(source))
                return BitmapTo1Bpp(bm32);
        }

        /// <summary>
        /// Reduces a bitmap to 1 bit per pixel.
        /// Written for a StackOverflow question.
        /// https://stackoverflow.com/a/51143150/395685
        /// </summary>
        /// <param name="source">Source image to reduce.</param>
        /// <returns>Pure black and white 1bpp version of the input image.</returns>
        public static Bitmap BitmapTo1Bpp(Bitmap source)
        {
            Rectangle rect = new Rectangle(0, 0, source.Width, source.Height);
            Bitmap dest = new Bitmap(rect.Width, rect.Height, PixelFormat.Format1bppIndexed);
            dest.SetResolution(source.HorizontalResolution, source.VerticalResolution);
            BitmapData sourceData = source.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format1bppIndexed);
            BitmapData targetData = dest.LockBits(rect, ImageLockMode.WriteOnly, PixelFormat.Format1bppIndexed);
            int actualDataWidth = (rect.Width + 7) / 8;
            int h = source.Height;
            int origStride = sourceData.Stride;
            int targetStride = targetData.Stride;
            byte[] imageData = new byte[actualDataWidth];
            long sourcePos = sourceData.Scan0.ToInt64();
            long destPos = targetData.Scan0.ToInt64();
            // Copy line by line, skipping by stride but copying actual data width
            for (int y = 0; y < h; ++y)
            {
                Marshal.Copy(new IntPtr(sourcePos), imageData, 0, actualDataWidth);
                Marshal.Copy(imageData, 0, new IntPtr(destPos), actualDataWidth);
                sourcePos += origStride;
                destPos += targetStride;
            }
            dest.UnlockBits(targetData);
            source.UnlockBits(sourceData);
            return dest;
        }

        /// <summary>
        /// From https://stackoverflow.com/q/52900883/395685
        /// </summary>
        public static Bitmap GetSierpinski(int width, int height)
        {
            int len = height * width;
            Point p1 = new Point(0, 0);
            Point p2 = new Point(width, 0);
            Point p3 = new Point(width / 2, height);
            Random r = new Random();
            Point p = new Point(r.Next(0, width), r.Next(0, width));
            byte[] data = new byte[len];
            for (long i = 0; i < len; ++i)
            {
                Point tp;
                switch (r.Next(0, 3))
                {
                    case 0:
                        tp = new Point((p1.X + p.X) / 2, (p1.Y + p.Y) / 2);
                        break;
                    case 1:
                        tp = new Point((p2.X + p.X) / 2, (p2.Y + p.Y) / 2);
                        break;
                    default:
                        tp = new Point((p3.X + p.X) / 2, (p3.Y + p.Y) / 2);
                        break;
                }
                data[tp.Y * width + tp.X] = 1;
                p = tp;
            }
            return ImageUtils.BuildImage(data, width, height, width, PixelFormat.Format8bppIndexed, new[] {Color.Black, Color.White}, Color.Black);
        }

        public static byte GetIndexedPixel(Bitmap b, int x, int y)
        {
            if ((b.PixelFormat & PixelFormat.Indexed) == 0) throw new ArgumentException("Image does not have an indexed format.");
            if (x < 0 || x >= b.Width) throw new ArgumentOutOfRangeException("x", String.Format("x should be in 0-{0}", b.Width));
            if (y < 0 || y >= b.Height) throw new ArgumentOutOfRangeException("y", String.Format("y should be in 0-{0}", b.Height));
            BitmapData data = null;
            try
            {
                data = b.LockBits(new Rectangle(x, y, 1, 1), ImageLockMode.ReadOnly, b.PixelFormat);
                byte[] pixel = new byte[1];
                Marshal.Copy(data.Scan0, pixel, 0, 1);
                return pixel[0];
            }
            finally
            {
                try
                {
                    if (data != null) b.UnlockBits(data);
                }
                catch (Exception)
                {
                    /* Ignorz */
                }
            }
        }

        public static Bitmap IntFffToBitmap(int[] array, int width, int height)
        {
            int len = width * height;
            if (len < array.Length)
                throw new ArgumentException("Array is not long enough for the given width and height.", "array");
            byte[] pixels = new byte[len * 4];
            int bytePtr = 0;
            for (int i = 0; i < len; ++i)
            {
                int val = array[i];
                // "ARGB" is big-endian, meaning the bytes are in order [B, G, R, A].
                // I'm just assuming they are in the int in the same order.
                pixels[bytePtr++] = /*B*/ (byte) ((val | 0x00F) << 8); // 000-00F range: shift up to 0-240
                pixels[bytePtr++] = /*G*/ (byte) ((val | 0x0F0)); // 000-0F0 range: OK for byte range
                pixels[bytePtr++] = /*R*/ (byte) ((val | 0xF00) >> 8); // 000-F00 range: shift down to 0-240
                pixels[bytePtr++] = /*A*/ 0xFF;
            }
            Bitmap bitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb);
            BitmapData targetData = bitmap.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.WriteOnly, bitmap.PixelFormat);
            // 32 bpp means it aligns perfectly to the internal stride of
            // multiples of 4 bytes, so this can be done in one copy operation.
            Marshal.Copy(pixels, 0, targetData.Scan0, len);
            bitmap.UnlockBits(targetData);
            return bitmap;
        }

        /// <summary>
        /// Creates an Icon object from an array of Image objects.
        /// Written for a StackOverflow question.
        /// https://stackoverflow.com/q/54801185/395685
        /// Using code from the FileIcon class.
        /// </summary>
        /// <param name="images"></param>
        /// <param name="contents"></param>
        /// <returns></returns>
        public static Icon ConvertImagesToIco(Image[] images, out byte[] contents)
        {
            if (images == null)
                throw new ArgumentNullException("images");
            int imgCount = images.Length;
            if (imgCount == 0)
                throw new ArgumentException("No images given.", "images");
            if (imgCount > 0xFFFF)
                throw new ArgumentException("Too many images.", "images");
            using (MemoryStream ms = new MemoryStream())
            using (BinaryWriter iconWriter = new BinaryWriter(ms))
            {
                byte[][] frameBytes = new byte[imgCount][];
                // 0-1 reserved, 0
                iconWriter.Write((short) 0);
                // 2-3 image type, 1 = icon, 2 = cursor
                iconWriter.Write((short) 1);
                // 4-5 number of images
                iconWriter.Write((short) imgCount);
                int offset = 6 + (16 * imgCount);
                for (int i = 0; i < imgCount; ++i)
                {
                    // Get image data
                    Image curFrame = images[i];
                    if (curFrame.Width > 256 || curFrame.Height > 256)
                        throw new ArgumentException("Image too large.", "images");
                    // for these three, 0 is interpreted as 256,
                    // so the cast reducing 256 to 0 is no problem.
                    byte width = (byte) curFrame.Width;
                    byte height = (byte) curFrame.Height;
                    byte colors = (byte) curFrame.Palette.Entries.Length;
                    int bpp;
                    byte[] frameData;
                    using (MemoryStream pngMs = new MemoryStream())
                    {
                        curFrame.Save(pngMs, ImageFormat.Png);
                        frameData = pngMs.ToArray();
                    }
                    // Get the color depth to save in the icon info. This needs to be
                    // fetched explicitly, since png does not support certain types
                    // like 16bpp, so it will convert to the nearest valid on save.
                    byte colDepth = frameData[24];
                    byte colType = frameData[25];
                    // I think .Net saving only supports 2, 3 and 6 anyway.
                    switch (colType)
                    {
                        case 2:
                            bpp = 3 * colDepth;
                            break; // RGB
                        case 6:
                            bpp = 4 * colDepth;
                            break; // ARGB
                        default:
                            bpp = colDepth;
                            break; // Indexed & greyscale
                    }
                    frameBytes[i] = frameData;
                    int imageLen = frameData.Length;
                    // Write image entry
                    // 0 image width.
                    iconWriter.Write(width);
                    // 1 image height.
                    iconWriter.Write(height);
                    // 2 number of colors.
                    iconWriter.Write(colors);
                    // 3 reserved
                    iconWriter.Write((byte) 0);
                    // 4-5 color planes
                    iconWriter.Write((short) 0);
                    // 6-7 bits per pixel
                    iconWriter.Write((short) bpp);
                    // 8-11 size of image data
                    iconWriter.Write(imageLen);
                    // 12-15 offset of image data
                    iconWriter.Write(offset);
                    offset += imageLen;
                }
                for (int i = 0; i < imgCount; ++i)
                {
                    // Write image data
                    // png data must contain the whole png data file
                    iconWriter.Write(frameBytes[i]);
                }
                iconWriter.Flush();
                contents = ms.ToArray();
                ms.Position = 0;
                return new Icon(ms);
            }
        }

        public static int GetLastClearLine(byte[] sourceData, int stride, int width, int height, Color checkColor)
        {
            // Get color as UInt32 in advance.
            uint checkColVal = (uint) checkColor.ToArgb();
            // Use MemoryStream with BinaryReader since it can read UInt32 from a byte array directly.
            using (MemoryStream ms = new MemoryStream(sourceData))
            using (BinaryReader sr = new BinaryReader(ms))
            {
                for (int y = height - 1; y >= 0; --y)
                {
                    // Set position in the memory stream to the start of the current row.
                    ms.Position = stride * y;
                    // Put loop variable outside "if" so it is retained after the loop.
                    int x;
                    // Increment loop variable from 0 to width, reading 32-bit values.
                    for (x = 0; x < width; ++x)
                        // Read UInt32 for a whole 32bpp ARGB pixel; compare with check value.
                        if (sr.ReadUInt32() != checkColVal)
                            break;
                    // Test if the loop went through the full width before aborting.
                    if (x == width)
                        return y;
                }
            }
            return -1;
        }

        public static void TilePatterns(string materialsFolder, int width, int height, string resultFolder)
        {
            //For every material image, calls the fusion method below.
            foreach (string materialImagePath in Directory.GetFiles(materialsFolder))
            {
                try
                {
                    using (Bitmap materialImage = new Bitmap(materialImagePath))
                    using (Bitmap result = TilePattern(materialImage, width, height, Color.Black))
                        result.Save(Path.Combine(resultFolder, Path.GetFileNameWithoutExtension(materialImagePath) + ".png"), ImageFormat.Png);
                }
                catch
                {
                    // Ignore
                }
            }
        }

        public static Bitmap TilePattern(Bitmap pattern, int width, int height, Color fillColor)
        {
            int patternWidth = pattern.Width;
            int patternHeight = pattern.Height;
            // No transparency allowed on the background image
            Bitmap result = new Bitmap(width, height, PixelFormat.Format24bppRgb);
            result.SetResolution(pattern.HorizontalResolution, pattern.VerticalResolution);
            using (Graphics g = Graphics.FromImage(result))
            {
                if ((fillColor.ToArgb() & 0xFFFFFF) != 0)
                {
                    using (Brush b = new SolidBrush(Color.FromArgb(0xFF, fillColor)))
                        g.FillRectangle(b, 0, 0, width, height);
                }
                for (int y = 0; y < height; y += patternHeight)
                {
                    for (int x = 0; x < width; x += patternWidth)
                    {
                        g.DrawImage(pattern, new Point(x, y));
                    }
                }
            }
            return result;
        }

        public static void BakeImages(string whiteFilePath, string materialsFolder, string resultFolder)
        {
            int width;
            int height;
            int stride;
            // extract bytes of shape & alpha image
            byte[] shapeImageBytes;
            using (Bitmap shapeImage = new Bitmap(whiteFilePath))
            {
                width = shapeImage.Width;
                height = shapeImage.Height;
                // extract bytes of shape & alpha image
                shapeImageBytes = GetImageData(shapeImage, out stride, PixelFormat.Format32bppArgb);
            }
            using (Bitmap blackImage = ExtractBlackImage(shapeImageBytes, width, height, stride))
            {
                //For every material image, calls the fusion method below.
                foreach (string materialImagePath in Directory.GetFiles(materialsFolder))
                {
                    using (Bitmap patternImage = new Bitmap(materialImagePath))
                        //using (Bitmap result = ApplyAlphaToImage(shapeImageBytes, width, height, stride, patternImage))
                    using (Bitmap materialImage = TilePattern(patternImage, width, height, Color.Black))
                    using (Bitmap result = ApplyAlphaToImage(shapeImageBytes, width, height, stride, materialImage))
                    {
                        if (result == null)
                            continue;
                        // paint black lines image onto alpha-adjusted pattern image.
                        using (Graphics g = Graphics.FromImage(result))
                            g.DrawImage(blackImage, 0, 0);
                        result.Save(Path.Combine(resultFolder, Path.GetFileNameWithoutExtension(materialImagePath) + ".png"), ImageFormat.Png);
                    }
                }
            }
        }

        public static Bitmap ExtractBlackImage(byte[] shapeImageBytes, int width, int height, int stride)
        {
            // Create black lines image.
            byte[] imageBytesBlack = new byte[shapeImageBytes.Length];
            // Line start offset is set to 3 to immediately get the alpha component.
            int lineOffsImg = 3;
            for (int y = 0; y < height; ++y)
            {
                int curOffs = lineOffsImg;
                for (int x = 0; x < width; ++x)
                {
                    // copy either alpha or inverted brightness (whichever is lowest)
                    // from the shape image onto black lines image as alpha, effectively
                    // only retaining the visible black lines from the shape image.
                    // I use curOffs - 1 (red) because it's the simplest operation.
                    byte alpha = shapeImageBytes[curOffs];
                    byte invBri = (byte) (255 - shapeImageBytes[curOffs - 1]);
                    imageBytesBlack[curOffs] = Math.Min(alpha, invBri);
                    // Adjust offset to next pixel.
                    curOffs += 4;
                }
                // Adjust line offset to next line.
                lineOffsImg += stride;
            }
            // Make the black lines images out of the byte array.
            return BuildImage(imageBytesBlack, width, height, stride, PixelFormat.Format32bppArgb);
        }

        public static Bitmap ApplyAlphaToImage(byte[] alphaImageBytes, int width, int height, int stride, Bitmap texture)
        {
            if (texture.Width != width || texture.Height != height)
                return null;
            // extract bytes of pattern image. Stride should be the same.
            int patternStride;
            byte[] imageBytesPattern = ImageUtils.GetImageData(texture, out patternStride, PixelFormat.Format32bppArgb);
            if (patternStride != stride)
                return null;
            // Line start offset is set to 3 to immediately get the alpha component.
            int lineOffsImg = 3;
            for (int y = 0; y < height; ++y)
            {
                int curOffs = lineOffsImg;
                for (int x = 0; x < width; ++x)
                {
                    // copy alpha from shape image onto pattern image.
                    imageBytesPattern[curOffs] = alphaImageBytes[curOffs];
                    // Adjust offset to next pixel.
                    curOffs += 4;
                }
                // Adjust line offset to next line.
                lineOffsImg += stride;
            }
            // Make a image out of the byte array, and return it.
            return BuildImage(imageBytesPattern, width, height, stride, PixelFormat.Format32bppArgb);
        }

        /// <summary>
        /// Written for SO question:
        /// https://stackoverflow.com/questions/62364153/c-sharp-icon-created-looks-fine-but-windows-directory-thumbnails-dont-look-rig
        /// </summary>
        /// <param name="imagePaths"></param>
        /// <param name="icoDirPath"></param>
        public static void WriteImagesToIcons(List<string> imagePaths, string icoDirPath)
        {
            // Change this to whatever you prefer.
            InterpolationMode scalingMode = InterpolationMode.HighQualityBicubic;
            //imagePaths => all images which I am converting to ico files
            imagePaths.ForEach(imgPath =>
            {
                // The correct way of replacing an extension
                string icoPath = Path.Combine(icoDirPath, Path.GetFileNameWithoutExtension(imgPath) + ".ico");
                using (Bitmap orig = new Bitmap(imgPath))
                using (Bitmap squared = orig.CopyToSquareCanvas(Color.Transparent))
                using (Bitmap resize16 = squared.Resize(16, 16, scalingMode))
                using (Bitmap resize32 = squared.Resize(32, 32, scalingMode))
                using (Bitmap resize48 = squared.Resize(48, 48, scalingMode))
                using (Bitmap resize64 = squared.Resize(64, 64, scalingMode))
                using (Bitmap resize96 = squared.Resize(96, 96, scalingMode))
                using (Bitmap resize128 = squared.Resize(128, 128, scalingMode))
                using (Bitmap resize192 = squared.Resize(192, 192, scalingMode))
                using (Bitmap resize256 = squared.Resize(256, 256, scalingMode))
                {
                    Image[] includedSizes = new Image[]
                        { resize16, resize32, resize48, resize64, resize96, resize128, resize192, resize256 };
                    ConvertImagesToIco(includedSizes, icoPath);
                    // Alt using byte array:
                    //Byte[] icoFile = ConvertImagesToIco(includedSizes);
                    //File.WriteAllBytes(icoPath, icoFile);
                }
            });
        }

        public static Bitmap CopyToSquareCanvas(this Bitmap source, Color canvasBackground)
        {
            int maxSide = source.Width > source.Height ? source.Width : source.Height;
            Bitmap bitmapResult = new Bitmap(maxSide, maxSide, PixelFormat.Format32bppArgb);
            using (Graphics graphicsResult = Graphics.FromImage(bitmapResult))
            {
                graphicsResult.Clear(canvasBackground);
                int xOffset = (maxSide - source.Width) / 2;
                int yOffset = (maxSide - source.Height) / 2;
                graphicsResult.DrawImage(source, new Rectangle(xOffset, yOffset, source.Width, source.Height));
            }
            return bitmapResult;
        }

        public static Bitmap Resize(this Bitmap source, int width, int height, InterpolationMode scalingMode)
        {
            Bitmap result = new Bitmap(width, height, PixelFormat.Format32bppArgb);
            using (Graphics g = Graphics.FromImage(result))
            {
                // Set desired interpolation mode here
                g.InterpolationMode = scalingMode;
                // Nearest Neighbor hard-pixel scaling needs this adjusted to work correctly
                if (scalingMode == InterpolationMode.NearestNeighbor)
                    g.PixelOffsetMode = PixelOffsetMode.Half;
                g.DrawImage(source, new Rectangle(0, 0, width, height), new Rectangle(0, 0, source.Width, source.Height), GraphicsUnit.Pixel);
            }
            return result;
        }

        public static byte[] ConvertImagesToIco(Image[] images)
        {
            if (images == null)
                throw new ArgumentNullException("images");
            int imgCount = images.Length;
            if (imgCount == 0)
                throw new ArgumentException("No images given.", "images");
            if (imgCount > 0xFFFF)
                throw new ArgumentException("Too many images.", "images");
            using (MemoryStream ms = new MemoryStream())
            using (BinaryWriter iconWriter = new BinaryWriter(ms))
            {
                byte[][] frameBytes = new byte[imgCount][];
                // 0-1 reserved, 0
                iconWriter.Write((short)0);
                // 2-3 image type, 1 = icon, 2 = cursor
                iconWriter.Write((short)1);
                // 4-5 number of images
                iconWriter.Write((short)imgCount);
                // Calculate header size for first image data offset.
                int offset = 6 + (16 * imgCount);
                for (int i = 0; i < imgCount; ++i)
                {
                    // Get image data
                    Image curFrame = images[i];
                    if (curFrame.Width > 256 || curFrame.Height > 256)
                        throw new ArgumentException("Image too large.", "images");
                    // for these three, 0 is interpreted as 256,
                    // so the cast reducing 256 to 0 is no problem.
                    byte width = (byte)curFrame.Width;
                    byte height = (byte)curFrame.Height;
                    byte colors = (byte)curFrame.Palette.Entries.Length;
                    int bpp;
                    byte[] frameData;
                    using (MemoryStream pngMs = new MemoryStream())
                    {
                        curFrame.Save(pngMs, ImageFormat.Png);
                        frameData = pngMs.ToArray();
                    }
                    // Get the color depth to save in the icon info. This needs to be
                    // fetched explicitly, since png does not support certain types
                    // like 16bpp, so it will convert to the nearest valid on save.
                    byte colDepth = frameData[24];
                    byte colType = frameData[25];
                    // I think .Net saving only supports color types 2, 3 and 6 anyway.
                    switch (colType)
                    {
                        case 2: bpp = 3 * colDepth; break; // RGB
                        case 6: bpp = 4 * colDepth; break; // ARGB
                        default: bpp = colDepth; break; // Indexed & greyscale
                    }
                    frameBytes[i] = frameData;
                    int imageLen = frameData.Length;
                    // Write image entry
                    // 0 image width. 
                    iconWriter.Write(width);
                    // 1 image height.
                    iconWriter.Write(height);
                    // 2 number of colors.
                    iconWriter.Write(colors);
                    // 3 reserved
                    iconWriter.Write((byte)0);
                    // 4-5 color planes
                    iconWriter.Write((short)0);
                    // 6-7 bits per pixel
                    iconWriter.Write((short)bpp);
                    // 8-11 size of image data
                    iconWriter.Write(imageLen);
                    // 12-15 offset of image data
                    iconWriter.Write(offset);
                    offset += imageLen;
                }
                for (int i = 0; i < imgCount; ++i)
                {
                    // Write image data
                    // png data must contain the whole png data file
                    iconWriter.Write(frameBytes[i]);
                }
                return ms.ToArray();
            }
        }

        public static void ConvertImagesToIco(Image[] images, string outputPath)
        {
            if (images == null)
                throw new ArgumentNullException("images");
            int imgCount = images.Length;
            if (imgCount == 0)
                throw new ArgumentException("No images given.", "images");
            if (imgCount > 0xFFFF)
                throw new ArgumentException("Too many images.", "images");
            using (FileStream fs = new FileStream(outputPath, FileMode.Create, FileAccess.Write))
            using (BinaryWriter iconWriter = new BinaryWriter(fs))
            {
                byte[][] frameBytes = new byte[imgCount][];
                // 0-1 reserved, 0
                iconWriter.Write((short)0);
                // 2-3 image type, 1 = icon, 2 = cursor
                iconWriter.Write((short)1);
                // 4-5 number of images
                iconWriter.Write((short)imgCount);
                // Calculate header size for first image data offset.
                int offset = 6 + (16 * imgCount);
                for (int i = 0; i < imgCount; ++i)
                {
                    // Get image data
                    Image curFrame = images[i];
                    if (curFrame.Width > 256 || curFrame.Height > 256)
                        throw new ArgumentException("Image too large.", "images");
                    // for these three, 0 is interpreted as 256,
                    // so the cast reducing 256 to 0 is no problem.
                    byte width = (byte)curFrame.Width;
                    byte height = (byte)curFrame.Height;
                    byte colors = (byte)curFrame.Palette.Entries.Length;
                    int bpp;
                    byte[] frameData;
                    using (MemoryStream pngMs = new MemoryStream())
                    {
                        curFrame.Save(pngMs, ImageFormat.Png);
                        frameData = pngMs.ToArray();
                    }
                    // Get the color depth to save in the icon info. This needs to be
                    // fetched explicitly, since png does not support certain types
                    // like 16bpp, so it will convert to the nearest valid on save.
                    byte colDepth = frameData[24];
                    byte colType = frameData[25];
                    // I think .Net saving only supports color types 2, 3 and 6 anyway.
                    switch (colType)
                    {
                        case 2: bpp = 3 * colDepth; break; // RGB
                        case 6: bpp = 4 * colDepth; break; // ARGB
                        default: bpp = colDepth; break; // Indexed & greyscale
                    }
                    frameBytes[i] = frameData;
                    int imageLen = frameData.Length;
                    // Write image entry
                    // 0 image width. 
                    iconWriter.Write(width);
                    // 1 image height.
                    iconWriter.Write(height);
                    // 2 number of colors.
                    iconWriter.Write(colors);
                    // 3 reserved
                    iconWriter.Write((byte)0);
                    // 4-5 color planes
                    iconWriter.Write((short)0);
                    // 6-7 bits per pixel
                    iconWriter.Write((short)bpp);
                    // 8-11 size of image data
                    iconWriter.Write(imageLen);
                    // 12-15 offset of image data
                    iconWriter.Write(offset);
                    offset += imageLen;
                }
                for (int i = 0; i < imgCount; ++i)
                {
                    // Write image data
                    // png data must contain the whole png data file
                    iconWriter.Write(frameBytes[i]);
                }
                iconWriter.Flush();
            }
        }

        public static void ConvertToIco(Image img, string file, int size)
        {
            Icon icon;
            using (var msImg = new MemoryStream())
            using (var msIco = new MemoryStream())
            {
                img.Save(msImg, ImageFormat.Png);
                using (var bw = new BinaryWriter(msIco))
                {
                    bw.Write((short) 0); //0-1 reserved
                    bw.Write((short) 1); //2-3 image type, 1 = icon, 2 = cursor
                    bw.Write((short) 1); //4-5 number of images
                    bw.Write((byte) size); //6 image width
                    bw.Write((byte) size); //7 image height
                    bw.Write((byte) 0); //8 number of colors
                    bw.Write((byte) 0); //9 reserved
                    bw.Write((short) 0); //10-11 color planes
                    bw.Write((short) 32); //12-13 bits per pixel
                    bw.Write((int) msImg.Length); //14-17 size of image data
                    bw.Write(22); //18-21 offset of image data
                    bw.Write(msImg.ToArray()); // write image data
                    bw.Flush();
                    bw.Seek(0, SeekOrigin.Begin);
                    icon = new Icon(msIco);
                }
            }
            using (var fs = new FileStream(file, FileMode.Create, FileAccess.Write))
                icon.Save(fs);
        }

        public static byte[] GetImageData(Bitmap sourceImage, out int stride, PixelFormat desiredPixelFormat)
        {
            int width = sourceImage.Width;
            int height = sourceImage.Height;
            BitmapData sourceData = sourceImage.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.ReadOnly, desiredPixelFormat);
            stride = sourceData.Stride;
            byte[] data = new byte[stride * height];
            Marshal.Copy(sourceData.Scan0, data, 0, data.Length);
            sourceImage.UnlockBits(sourceData);
            return data;
        }

        public static Bitmap BuildImage(byte[] sourceData, int width, int height, int stride, PixelFormat pixelFormat)
        {
            Bitmap newImage = new Bitmap(width, height, pixelFormat);
            BitmapData targetData = newImage.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.WriteOnly, newImage.PixelFormat);
            int newDataWidth = ((Image.GetPixelFormatSize(pixelFormat) * width) + 7) / 8;
            int targetStride = targetData.Stride;
            long scan0 = targetData.Scan0.ToInt64();
            for (int y = 0; y < height; ++y)
                Marshal.Copy(sourceData, y * stride, new IntPtr(scan0 + y * targetStride), newDataWidth);
            newImage.UnlockBits(targetData);
            return newImage;
        }


#if UNSAFE
        public static unsafe Byte GetIndexedPixelUnsafe(Bitmap b, Int32 x, Int32 y)
        {
            if (x < 0 || x >= b.Width) throw new ArgumentOutOfRangeException("x", String.Format("x should be in 0-{0}", b.Width));
            if (y < 0 || y >= b.Height) throw new ArgumentOutOfRangeException("y", String.Format("y should be in 0-{0}", b.Height));
            BitmapData data = b.LockBits(new Rectangle(0, 0, b.Width, b.Height), ImageLockMode.ReadOnly, b.PixelFormat);
            try
            {
                Byte* scan0 = (Byte*)data.Scan0;
                return scan0[x + y * data.Stride];
            }
            finally
            {
                if (data != null) b.UnlockBits(data);
            }
        }
#endif

        /// <summary>
        /// Written for https://stackoverflow.com/q/65844918/395685
        /// Finds the two most prominent colors in an image, and uses them as extremes
        /// for matching all pixels on the image to a palette fading between the two.
        /// </summary>
        /// <param name="image">Image to reduce.</param>
        /// <param name="substitutePalette">Substitute final palette with grayscale.</param>
        /// <param name="bgWhite">If changed to grayscale, true if the background should be the white color. If not, it will be the black one.</param>
        /// <returns>
        /// An 8-bit image with the image content of the input reduced to grayscale,
        /// with the found two most found colors as black and white.
        /// </returns>
        public static Bitmap ReduceToTwoColorFade(Bitmap image, bool substitutePalette, bool bgWhite)
        {
            if (!substitutePalette)
                bgWhite = false;
            // Get data out of the image, using LockBits and Marshal.Copy
            int width = image.Width;
            int height = image.Height;
            // LockBits can actually -convert- the image data to the requested color depth.
            // 32 bpp is the easiest to get the color components out.
            BitmapData sourceData = image.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            // Not really needed for 32bpp, but technically the stride does not always match the
            // amount of used data on each line, since the stride gets rounded up to blocks of 4.
            int stride = sourceData.Stride;
            byte[] imgBytes = new byte[stride * height];
            Marshal.Copy(sourceData.Scan0, imgBytes, 0, imgBytes.Length);
            image.UnlockBits(sourceData);
            // Make color population histogram
            int lineOffset = 0;
            Dictionary<uint, int> histogram = new Dictionary<uint, int>();
            for (int y = 0; y < height; ++y)
            {
                int offset = lineOffset;
                for (int x = 0; x < width; ++x)
                {
                    // Optional check: only handle if not mostly-transparent
                    if (imgBytes[offset + 3] > 0x7F)
                    {
                        // Get color values from bytes, without alpha.
                        // Little-endian: UInt32 0xAARRGGBB = Byte[] { BB, GG, RR, AA }
                        uint val = (uint)((0xFF << 24) | (imgBytes[offset + 2] << 16) | (imgBytes[offset + 1] << 8) | imgBytes[offset + 0]);
                        if (histogram.ContainsKey(val))
                            histogram[val] = histogram[val] + 1;
                        else
                            histogram[val] = 1;
                    }
                    offset += 4;
                }
                lineOffset += stride;
            }
            // Sort the histogram. This requires System.Linq
            KeyValuePair<uint, int>[] histoSorted = histogram.OrderByDescending(c => c.Value).ToArray();
            // Since we filter on alpha, getting a result is not 100% guaranteed.
            Color colBackgr = histoSorted.Length < 1 ? Color.Black : Color.FromArgb((int)histoSorted[0].Key);
            // if less than 2 colors, just default it to the same.
            Color colContent = histoSorted.Length < 2 ? colBackgr : Color.FromArgb((int)histoSorted[1].Key);
            // Make a new 256-color palette, making a fade between these two colors, for feeding into GetClosestPaletteIndexMatch later
            Color[] matchPal = new Color[0x100];
            Color toBlack = bgWhite ? colContent : colBackgr;
            Color toWhite = bgWhite ? colBackgr : colContent;
            int rFirst = toBlack.R;
            int gFirst = toBlack.G;
            int bFirst = toBlack.B;
            double rDif = (toBlack.R - toWhite.R) / 255.0;
            double gDif = (toBlack.G - toWhite.G) / 255.0;
            double bDif = (toBlack.B - toWhite.B) / 255.0;
            for (int i = 0; i < 0x100; ++i)
                matchPal[i] = Color.FromArgb(
                    Math.Min(0xFF, Math.Max(0, rFirst - (int)Math.Round(rDif * i, MidpointRounding.AwayFromZero))),
                    Math.Min(0xFF, Math.Max(0, gFirst - (int)Math.Round(gDif * i, MidpointRounding.AwayFromZero))),
                    Math.Min(0xFF, Math.Max(0, bFirst - (int)Math.Round(bDif * i, MidpointRounding.AwayFromZero))));
            // Ensure start and end point are correct, and not mangled by small rounding errors.
            matchPal[0x00] = toBlack;
            matchPal[0xFF] = toWhite;
            // Small extra: ignore duplicates of the highest color, to ensure that
            // all matches of the highest color itself actually end up on index 0xFF.
            List<int> ignoreIndices = new List<int>();
            for (int i = 0; i < 0xFF; ++i)
                if (matchPal[i] == toWhite)
                    ignoreIndices.Add(i);
            // The 8-bit stride is simply the width in this case.
            int stride8Bit = width;
            // Make 8-bit array to store the result
            byte[] imgBytes8Bit = new byte[stride8Bit * height];
            // Reset offset for a new loop through the image data
            lineOffset = 0;
            // Make new offset vars for a loop through the 8-bit image data
            int lineOffset8Bit = 0;
            for (int y = 0; y < height; ++y)
            {
                int offset = lineOffset;
                int offset8Bit = lineOffset8Bit;
                for (int x = 0; x < width; ++x)
                {
                    int toWrite;
                    // If transparent, revert to background color.
                    if (imgBytes[offset + 3] <= 0x7F)
                    {
                        toWrite = bgWhite ? 0xFF : 0x00;
                    }
                    else
                    {
                        Color col = Color.FromArgb(imgBytes[offset + 2], imgBytes[offset + 1], imgBytes[offset + 0]);
                        toWrite = ColorUtils.GetClosestPaletteIndexMatch(col, matchPal, ignoreIndices);
                    }
                    // Write the found color index to the 8-bit byte array.
                    imgBytes8Bit[offset8Bit] = (byte)toWrite;
                    offset += 4;
                    offset8Bit++;
                }
                lineOffset += stride;
                lineOffset8Bit += stride8Bit;
            }
            // Make new 8-bit image and copy the data into it.
            Bitmap newBm = new Bitmap(width, height, PixelFormat.Format8bppIndexed);
            BitmapData targetData = newBm.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.WriteOnly, newBm.PixelFormat);
            //  get minimum data width for the pixel format.
            int newDataWidth = ((Image.GetPixelFormatSize(newBm.PixelFormat) * width) + 7) / 8;
            // Note that this Stride will most likely NOT match the image width; it is rounded up to the
            // next multiple of 4 bytes. For that reason, we copy the data per line, and not as one block.
            int targetStride = targetData.Stride;
            long scan0 = targetData.Scan0.ToInt64();
            for (int y = 0; y < height; ++y)
                Marshal.Copy(imgBytes8Bit, y * stride8Bit, new IntPtr(scan0 + y * targetStride), newDataWidth);
            newBm.UnlockBits(targetData);
            // Set final image palette to grayscale fade.
            // 'Image.Palette' makes a COPY of the palette when accessed.
            // So copy it out, modify it, then copy it back in.
            ColorPalette pal = newBm.Palette;
            if (substitutePalette)
            {
                for (int i = 0; i < 0x100; ++i)
                    pal.Entries[i] = Color.FromArgb(i, i, i);
            }
            else
            {
                for (int i = 0; i < 0x100; ++i)
                    pal.Entries[i] = matchPal[i];
            }
            newBm.Palette = pal;
            return newBm;
        }

    }
}

#endif
