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
        public static Bitmap FromTwoDimIntArray(Int32[,] data)
        {
            Int32 width = data.GetLength(0);
            Int32 height = data.GetLength(1);
            Int32 byteIndex = 0;
            Byte[] dataBytes = new Byte[height * width * 4];
            for (Int32 y = 0; y < height; ++y)
            {
                for (Int32 x = 0; x < width; ++x)
                {
                    // UInt32 0xAARRGGBB = Byte[] { BB, GG, RR, AA }
                    UInt32 val = (UInt32) data[x, y];
                    // This code clears out everything but a specific part of the value
                    // and then shifts the remaining piece down to the lowest byte
                    dataBytes[byteIndex + 0] = (Byte) (val & 0x000000FF); // B
                    dataBytes[byteIndex + 1] = (Byte) ((val & 0x0000FF00) >> 08); // G
                    dataBytes[byteIndex + 2] = (Byte) ((val & 0x00FF0000) >> 16); // R
                    dataBytes[byteIndex + 3] = (Byte) ((val & 0xFF000000) >> 24); // A
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
        public static Bitmap FromTwoDimIntArrayGray(Int32[,] data)
        {
            Int32 width = data.GetLength(0);
            Int32 height = data.GetLength(1);
            Int32 byteIndex = 0;
            Byte[] dataBytes = new Byte[height * width];
            for (Int32 y = 0; y < height; ++y)
            {
                for (Int32 x = 0; x < width; ++x)
                {
                    // Int32 0xAARRGGBB = Byte[] { BB, GG, RR, AA }
                    // This uses the lowest byte, which is the blue component.
                    dataBytes[byteIndex] = (Byte) ((UInt32) data[x, y] & 0xFF);
                    // More efficient than multiplying
                    byteIndex++;
                }
            }
            Color[] palette = new Color[0x100];
            for (Int32 i = 0; i < 0x100; ++i)
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
        public static Boolean HasTransparency(Bitmap bitmap)
        {
            // Not an alpha-capable color format. Note that GDI+ indexed images are alpha-capable on the palette.
            if (((ImageFlags) bitmap.Flags & ImageFlags.HasAlpha) == 0)
                return false;
            // Indexed format, and no alpha colors in the images palette: immediate pass.
            if ((bitmap.PixelFormat & PixelFormat.Indexed) != 0 && bitmap.Palette.Entries.All(c => c.A == 255))
                return false;
            // Get the byte data 'as 32-bit ARGB'. This offers a converted version of the image data without modifying the original image.
            BitmapData data = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            Int32 len = bitmap.Height * data.Stride;
            Byte[] bytes = new Byte[len];
            Marshal.Copy(data.Scan0, bytes, 0, len);
            bitmap.UnlockBits(data);
            // Check the alpha bytes in the data. Since the data is little-endian, the actual byte order is [BB GG RR AA]
            for (Int32 i = 3; i < len; i += 4)
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
        public static Boolean IsGrayscale(Bitmap bitmap)
        {
            // Indexed format, and no non-gray colors in the images palette: immediate pass.
            if ((bitmap.PixelFormat & PixelFormat.Indexed) != 0 && bitmap.Palette.Entries.All(c => c.R == c.G && c.R == c.B))
                return true;
            Int32 stride;
            Byte[] data = ImageUtils.GetImageData(bitmap, out stride, PixelFormat.Format32bppArgb);
            Int32 curRowOffs = 0;
            Int32 height = bitmap.Height;
            Int32 width = bitmap.Height;
            for (Int32 y = 0; y < height; ++y)
            {
                // Set offset to start of current row
                Int32 curOffs = curRowOffs;
                for (Int32 x = 0; x < width; ++x)
                {
                    Byte b = data[curOffs];
                    Byte g = data[curOffs + 1];
                    Byte r = data[curOffs + 2];
                    Byte a = data[curOffs + 3];
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
        public static Bitmap GenerateCheckerboardImage(Int32 width, Int32 height, Color[] colors, Byte color1, Byte color2)
        {
            if (width == 0 || height == 0)
                return null;
            Byte[] patternArray = new Byte[width * height];
            for (Int32 y = 0; y < height; ++y)
            {
                for (Int32 x = 0; x < width; ++x)
                {
                    Int32 offset = x + y * height;
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
        public static Bitmap GrayImageFromCsv(String[] lines, Int32 startColumn, Int32 maxValue)
        {
            // maxValue cannot exceed 255
            maxValue = Math.Min(maxValue, 255);
            // Read lines; this gives us the data, and the height.
            //String[] lines = File.ReadAllLines(path);
            if (lines == null || lines.Length == 0)
                return null;
            Int32 bottom = lines.Length;
            // Trim any empty lines from the start and end.
            while (bottom > 0 && lines[bottom - 1].Trim().Length == 0)
                bottom--;
            if (bottom == 0)
                return null;
            Int32 top = 0;
            while (top < bottom && lines[top].Trim().Length == 0)
                top++;
            Int32 height = bottom - top;
            // This removes the top-bottom stuff; the new array is compact.
            String[][] values = new String[height][];
            for (Int32 i = top; i < bottom; ++i)
                values[i - top] = lines[i].Split(',');
            // Find width: maximum csv line length minus the amount of columns to skip.
            Int32 width = values.Max(line => line.Length) - startColumn;
            if (width <= 0)
                return null;
            // Create the array. Since it's 8-bit, this is one byte per pixel.
            Byte[] imageArray = new Byte[width * height];
            // Parse all values into the array
            // Y = lines, X = csv values
            for (Int32 y = 0; y < height; ++y)
            {
                Int32 offset = y * width;
                // Skip indices before "startColumn". Target offset starts from the start of the line anyway.
                String[] yValues = values[y];
                Int32 yValuesLen = yValues.Length;
                for (Int32 x = startColumn; x < yValuesLen; ++x)
                {
                    Int32 val;
                    // Don't know if Trim is needed here. Depends on the file.
                    if (Int32.TryParse(yValues[x].Trim(), out val))
                        imageArray[offset] = (Byte) Math.Max(0, Math.Min(val, maxValue));
                    offset++;
                }
            }
            // generate gray palette for the given range, by calculating the factor to multiply by.
            Double mulFactor = 255d / maxValue;
            Color[] palette = new Color[maxValue + 1];
            for (Int32 i = 0; i <= maxValue; ++i)
            {
                // Away from zero rounding: 2.4 => 2 ; 2.5 => 3
                Byte v = (Byte) Math.Round(i * mulFactor, MidpointRounding.AwayFromZero);
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
        public static Bitmap GetGrayImage(Image image, Int32 width, Int32 height)
        {
            // get image data
            Bitmap b = new Bitmap(image, width, height);
            BitmapData sourceData = b.LockBits(new Rectangle(0, 0, b.Width, b.Height), ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
            Int32 stride = sourceData.Stride;
            Byte[] data = new Byte[stride * b.Height];
            Marshal.Copy(sourceData.Scan0, data, 0, data.Length);
            // iterate
            for (Int32 y = 0; y < height; ++y)
            {
                Int32 offset = y * stride;
                for (Int32 x = 0; x < width; ++x)
                {
                    Byte colB = data[offset + 0]; // B
                    Byte colG = data[offset + 1]; // G
                    Byte colR = data[offset + 2]; // R
                    //Int32 ColA = data[offset + 3]; // A
                    Byte grayValue = GetGreyValue(colR, colG, colB);
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
            Byte grey = GetGreyValue(color.R, color.G, color.B);
            return Color.FromArgb(grey, grey, grey);
        }

        public static Byte GetGreyValue(Color color)
        {
            return GetGreyValue(color.R, color.G, color.B);
        }

        public static Byte GetGreyValue(Byte red, Byte green, Byte blue)
        {
            Double redFactor = 0.2126d * Math.Pow(red, 2.2d);
            Double grnFactor = 0.7152d * Math.Pow(green, 2.2d);
            Double bluFactor = 0.0722d * Math.Pow(blue, 2.2d);
            Double grey = Math.Pow(redFactor + grnFactor + bluFactor, 1d / 2.2);
            return (Byte) Math.Max(0, Math.Min(255, Math.Round(grey, MidpointRounding.AwayFromZero)));
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
        public static Bitmap BayerGridToGray(Bitmap image, Boolean greenFirst, Boolean blueRowFirst)
        {
            Int32 stride;
            Byte[] arr = GetImageData(image, out stride, PixelFormat.Format24bppRgb);
            Int32 width = image.Width;
            Int32 height = image.Height;

            Byte[] result = new Byte[width * height];
            for (Int32 y = 0; y < height; ++y)
            {
                Int32 curPtr = y * stride;
                Int32 resPtr = y * width;
                for (Int32 x = 0; x < width; ++x)
                {
                    // Get correct color components from sliding window
                    Boolean isGreen = (x + y) % 2 == (greenFirst ? 0 : 1);
                    Boolean blueRow = y % 2 == (blueRowFirst ? 0 : 1);
                    // BGR
                    Byte blue = arr[curPtr + 0];
                    Byte green = arr[curPtr + 1];
                    Byte red = arr[curPtr + 2];
                    Byte val = isGreen ? green : blueRow ? blue : red;

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
            for (Int32 i = 0; i < 256; ++i)
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
        public static Byte[] BayerToRgb2x2Orig(Byte[] arr, ref Int32 width, ref Int32 height, ref Int32 stride, Boolean greenFirst, Boolean blueRowFirst)
        {
            Int32 actualWidth = width - 1;
            Int32 actualHeight = height - 1;
            Int32 actualStride = actualWidth * 3;
            Byte[] result = new Byte[actualStride * actualHeight];
            for (Int32 y = 0; y < actualHeight; ++y)
            {
                Int32 curPtr = y * stride;
                Int32 resPtr = y * actualStride;
                for (Int32 x = 0; x < actualWidth; ++x)
                {
                    // Get correct color components from sliding window
                    Boolean isGreen = (x + y) % 2 == (greenFirst ? 0 : 1);
                    Boolean blueRow = y % 2 == (blueRowFirst ? 0 : 1);
                    Byte cornerCol1 = isGreen ? arr[curPtr + 1] : arr[curPtr];
                    Byte cornerCol2 = isGreen ? arr[curPtr + stride] : arr[curPtr + stride + 1];
                    Byte greenCol1 = isGreen ? arr[curPtr] : arr[curPtr + 1];
                    Byte greenCol2 = isGreen ? arr[curPtr + stride + 1] : arr[curPtr + stride];
                    Byte blueCol = blueRow ? cornerCol1 : cornerCol2;
                    Byte redCol = blueRow ? cornerCol2 : cornerCol1;
                    // 24bpp RGB is saved as [B, G, R].
                    // Blue
                    result[resPtr + 0] = blueCol;
                    // Green
                    result[resPtr + 1] = (Byte) ((greenCol1 + greenCol2) / 2);
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
        public static Byte[] BayerToRgb2x2CopyExpand(Byte[] arr, Int32 width, Int32 height, ref Int32 stride, Boolean greenFirst, Boolean blueRowFirst)
        {
            Int32 processWidth = width;
            Int32 processHeight = height;
            if (width > 1 && height > 1)
            {
                arr = ImageUtils.ChangeStride(arr, stride, height, width + 1, false, 0);
                stride = width + 1;
                processWidth = width + 1;
                Byte[] lastColB = ImageUtils.CopyFrom8bpp(arr, width, height, stride, new Rectangle(width - 2, 0, 1, height));
                ImageUtils.PasteOn8bpp(arr, processWidth, height, stride, lastColB, 1, height, 1, new Rectangle(width, 0, 1, height), null, true);
                arr = ImageUtils.ChangeHeight(arr, stride, height, height + 1, false, 0);
                processHeight = height + 1;
                Byte[] lastRowB = ImageUtils.CopyFrom8bpp(arr, processWidth, processHeight, stride, new Rectangle(0, height - 2, processWidth, 1));
                ImageUtils.PasteOn8bpp(arr, processWidth, processHeight, stride, lastRowB, processWidth, 1, processWidth, new Rectangle(0, height, processWidth, 1), null, true);
            }
            Int32 lastCol = processWidth;
            Int32 lastRow = processHeight;
            Int32 actualStride = width * 3;
            Byte[] result = new Byte[actualStride * height];
            for (Int32 y = 0; y < height; ++y)
            {
                Int32 curPtr = y * stride;
                Int32 resPtr = y * actualStride;
                for (Int32 x = 0; x < width; ++x)
                {
                    // Get correct color components from sliding window
                    Boolean isGreen = (x + y) % 2 == (greenFirst ? 0 : 1); // all corner colors and center are green.
                    Boolean isBlueRow = y % 2 == (blueRowFirst ? 0 : 1);
                    Byte valGreen;
                    Byte valRed;
                    Byte valBlue;
                    Byte pxCol = arr[curPtr];
                    Byte? tpCol1 = null;
                    Byte? tpCol2 = null;
                    Byte? tpCol3 = null;
                    Byte? lfCol = null;
                    Byte? rtCol = x == lastCol ? (Byte?) null : arr[curPtr + 1];
                    Byte? btCol1 = null;
                    Byte? btCol2 = y == lastRow ? (Byte?) null : arr[curPtr + stride];
                    Byte? btCol3 = y == lastRow || x == lastCol ? (Byte?) null : arr[curPtr + stride + 1];

                    if (isGreen)
                    {
                        valGreen = GetAverageCol(tpCol1, tpCol3, btCol1, btCol3, pxCol);
                        Byte verVal = GetAverageCol(tpCol2, btCol2);
                        Byte horVal = GetAverageCol(lfCol, rtCol);
                        valRed = isBlueRow ? verVal : horVal;
                        valBlue = isBlueRow ? horVal : verVal;
                    }
                    else
                    {
                        valGreen = GetAverageCol(tpCol2, rtCol, btCol2, lfCol);
                        Byte cornerCol = GetAverageCol(tpCol1, tpCol3, btCol1, btCol3);
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
        public static Byte[] BayerToRgb3x3(Byte[] arr, Int32 width, Int32 height, ref Int32 stride, Boolean greenFirst, Boolean blueRowFirst)
        {
            Int32 lastCol = width - 1;
            Int32 lastRow = height - 1;
            Int32 actualStride = width * 3;
            Byte[] result = new Byte[actualStride * height];
            for (Int32 y = 0; y < height; ++y)
            {
                Int32 curPtr = y * stride;
                Int32 resPtr = y * actualStride;
                for (Int32 x = 0; x < width; ++x)
                {
                    // Get correct color components from sliding window
                    Boolean isGreen = (x + y) % 2 == (greenFirst ? 0 : 1); // all corner colors and center are green.
                    Boolean isBlueRow = y % 2 == (blueRowFirst ? 0 : 1);
                    Byte valGreen;
                    Byte valRed;
                    Byte valBlue;

                    Byte cntrCol = arr[curPtr];
                    Byte? tplfCol = y == 0 || x == 0 ? (Byte?) null : arr[curPtr - stride - 1];
                    Byte? tpcnCol = y == 0 ? (Byte?) null : arr[curPtr - stride];
                    Byte? tprtCol = y == 0 || x == lastCol ? (Byte?) null : arr[curPtr - stride + 1];
                    Byte? cnlfCol = x == 0 ? (Byte?) null : arr[curPtr - 1];
                    Byte? cnrtCol = x == lastCol ? (Byte?) null : arr[curPtr + 1];
                    Byte? btlfCol = y == lastRow || x == 0 ? (Byte?) null : arr[curPtr + stride - 1];
                    Byte? btcnCol = y == lastRow ? (Byte?) null : arr[curPtr + stride];
                    Byte? btrtCol = y == lastRow || x == lastCol ? (Byte?) null : arr[curPtr + stride + 1];

                    if (isGreen)
                    {
                        valGreen = GetAverageCol(tplfCol, tprtCol, btlfCol, btrtCol, cntrCol);
                        Byte verVal = GetAverageCol(tpcnCol, btcnCol);
                        Byte horVal = GetAverageCol(cnlfCol, cnrtCol);
                        valRed = isBlueRow ? verVal : horVal;
                        valBlue = isBlueRow ? horVal : verVal;
                    }
                    else
                    {
                        valGreen = GetAverageCol(tpcnCol, cnrtCol, btcnCol, cnlfCol);
                        Byte cornerCol = GetAverageCol(tplfCol, tprtCol, btlfCol, btrtCol);
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
        private static Byte GetAverageCol(params Byte?[] cols)
        {
            Int32 colsCount = 0;
            Int32 colsLength = cols.Length;
            for (Int32 i = 0; i < colsLength; ++i)
                if (cols[i].HasValue) colsCount++;
            Int32 avgVal = 0;
            for (Int32 i = 0; i < colsLength; ++i)
                avgVal += cols[i].GetValueOrDefault();
            return colsCount == 0 ? (Byte) 0x80 : (Byte) (avgVal / colsCount);
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
        public static Byte[] BayerToRgb2x2Expand(Byte[] arr, ref Int32 width, ref Int32 height, ref Int32 stride, Boolean greenFirst, Boolean blueRowFirst)
        {
            Int32 processWidth = width - 1;
            Int32 processHeight = height - 1;
            Int32 lastWidth = width - 2;
            Int32 lastHeight = height - 2;
            Int32 newStride = width * 3;
            Byte[] result = new Byte[newStride * height];
            for (Int32 y = 0; y < processHeight; ++y)
            {
                Int32 curPtr = y * stride;
                Int32 resPtr = y * newStride;
                for (Int32 x = 0; x < processWidth; ++x)
                {
                    // Get correct color components from sliding window
                    Boolean isGreen = (x + y) % 2 == (greenFirst ? 0 : 1);
                    Boolean blueRow = y % 2 == (blueRowFirst ? 0 : 1);
                    Byte cornerCol1 = isGreen ? arr[curPtr + 1] : arr[curPtr];
                    Byte cornerCol2 = isGreen ? arr[curPtr + stride] : arr[curPtr + stride + 1];
                    Byte greenCol1 = isGreen ? arr[curPtr] : arr[curPtr + 1];
                    Byte greenCol2 = isGreen ? arr[curPtr + stride + 1] : arr[curPtr + stride];
                    Byte redCol = blueRow ? cornerCol2 : cornerCol1;
                    Byte greenCol = (Byte) ((greenCol1 + greenCol2) / 2);
                    Byte blueCol = blueRow ? cornerCol1 : cornerCol2;
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
        public static Byte[] GetChannelBytes(Bitmap image, Int32 channelNr)
        {
            if (channelNr >= 4 || channelNr < 0)
                throw new IndexOutOfRangeException();
            Int32 width = image.Width;
            Int32 height = image.Height;
            Int32 stride;
            Byte[] dataBytes = ImageUtils.GetImageData(image, out stride, PixelFormat.Format32bppArgb);
            Byte[] channel = new Byte[height * width];
            Int32 readLineOffs = 0;
            Int32 writeLineOffs = 0;
            for (Int32 y = 0; y < height; ++y)
            {
                Int32 readOffs = readLineOffs;
                Int32 writeOffs = writeLineOffs;
                for (Int32 x = 0; x < width; ++x)
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
        public static Int32[,] GetChannel(Bitmap image, Int32 channelNr)
        {
            if (channelNr >= 3 || channelNr < 0)
                throw new IndexOutOfRangeException();
            Int32 width = image.Width;
            Int32 height = image.Height;
            Int32 stride;
            Byte[] dataBytes = ImageUtils.GetImageData(image, out stride, PixelFormat.Format24bppRgb);
            Int32[,] channel = new Int32[height, width];
            Int32 readLineOffs = 0;
            for (Int32 y = 0; y < height; ++y)
            {
                Int32 readOffs = readLineOffs;
                for (Int32 x = 0; x < width; ++x)
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
        public static Int32[,] ReduceChannel(Int32[,] origChannel, Int32 lossfactor)
        {
            Int32 newHeight = origChannel.GetLength(0) / lossfactor;
            Int32 newWidth = origChannel.GetLength(1) / lossfactor;
            // to avoid rounding errors
            Int32 origHeight = newHeight * lossfactor;
            Int32 origWidth = newWidth * lossfactor;
            Int32[,] newChannel = new Int32[newHeight, newWidth];
            Int32 newY = 0;
            for (Int32 y = 1; y < origHeight; y += lossfactor)
            {
                Int32 newX = 0;
                for (Int32 x = 1; x < origWidth; x += lossfactor)
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
        public static Bitmap CreateImageFromChannels(Int32[,] redChannel, Int32[,] greenChannel, Int32[,] blueChannel)
        {
            Int32 width = greenChannel.GetLength(1);
            Int32 height = greenChannel.GetLength(0);
            Bitmap result = new Bitmap(width, height, PixelFormat.Format24bppRgb);
            Rectangle rect = new Rectangle(0, 0, width, height);
            BitmapData bmpData = result.LockBits(rect, ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);
            Int32 stride = bmpData.Stride;
            // stride is the actual line width in bytes.
            Int32 bytes = stride * height;
            Byte[] PixelValues = new Byte[bytes];
            for (Int32 y = 0; y < height; ++y)
            {
                // use stride to get the start offset of each line
                Int32 offset = y * stride;
                for (Int32 x = 0; x < width; ++x)
                {
                    PixelValues[offset + 0] = (Byte) blueChannel[y, x];
                    PixelValues[offset + 1] = (Byte) greenChannel[y, x];
                    PixelValues[offset + 2] = (Byte) redChannel[y, x];
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
            Int32 actualDataWidth = (rect.Width + 7) / 8;
            Int32 h = source.Height;
            Int32 origStride = sourceData.Stride;
            Int32 targetStride = targetData.Stride;
            Byte[] imageData = new Byte[actualDataWidth];
            Int64 sourcePos = sourceData.Scan0.ToInt64();
            Int64 destPos = targetData.Scan0.ToInt64();
            // Copy line by line, skipping by stride but copying actual data width
            for (Int32 y = 0; y < h; ++y)
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
        public static Bitmap GetSierpinski(Int32 width, Int32 height)
        {
            Int32 len = height * width;
            Point p1 = new Point(0, 0);
            Point p2 = new Point(width, 0);
            Point p3 = new Point(width / 2, height);
            Random r = new Random();
            Point p = new Point(r.Next(0, width), r.Next(0, width));
            Byte[] data = new Byte[len];
            for (Int64 i = 0; i < len; ++i)
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

        public static Byte GetIndexedPixel(Bitmap b, Int32 x, Int32 y)
        {
            if ((b.PixelFormat & PixelFormat.Indexed) == 0) throw new ArgumentException("Image does not have an indexed format!");
            if (x < 0 || x >= b.Width) throw new ArgumentOutOfRangeException("x", String.Format("x should be in 0-{0}", b.Width));
            if (y < 0 || y >= b.Height) throw new ArgumentOutOfRangeException("y", String.Format("y should be in 0-{0}", b.Height));
            BitmapData data = null;
            try
            {
                data = b.LockBits(new Rectangle(x, y, 1, 1), ImageLockMode.ReadOnly, b.PixelFormat);
                Byte[] pixel = new Byte[1];
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

        public static Bitmap IntFffToBitmap(Int32[] array, Int32 width, Int32 height)
        {
            Int32 len = width * height;
            if (len < array.Length)
                throw new ArgumentException("Array is not long enough for the given width and height!", "array");
            Byte[] pixels = new Byte[len * 4];
            Int32 bytePtr = 0;
            for (int i = 0; i < len; ++i)
            {
                Int32 val = array[i];
                // "ARGB" is big-endian, meaning the bytes are in order [B, G, R, A].
                // I'm just assuming they are in the int in the same order.
                pixels[bytePtr++] = /*B*/ (Byte) ((val | 0x00F) << 8); // 000-00F range: shift up to 0-240
                pixels[bytePtr++] = /*G*/ (Byte) ((val | 0x0F0)); // 000-0F0 range: OK for byte range
                pixels[bytePtr++] = /*R*/ (Byte) ((val | 0xF00) >> 8); // 000-F00 range: shift down to 0-240
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
        public static Icon ConvertImagesToIco(Image[] images, out Byte[] contents)
        {
            if (images == null)
                throw new ArgumentNullException("images");
            Int32 imgCount = images.Length;
            if (imgCount == 0)
                throw new ArgumentException("No images given!", "images");
            if (imgCount > 0xFFFF)
                throw new ArgumentException("Too many images!", "images");
            using (MemoryStream ms = new MemoryStream())
            using (BinaryWriter iconWriter = new BinaryWriter(ms))
            {
                Byte[][] frameBytes = new Byte[imgCount][];
                // 0-1 reserved, 0
                iconWriter.Write((Int16) 0);
                // 2-3 image type, 1 = icon, 2 = cursor
                iconWriter.Write((Int16) 1);
                // 4-5 number of images
                iconWriter.Write((Int16) imgCount);
                Int32 offset = 6 + (16 * imgCount);
                for (Int32 i = 0; i < imgCount; ++i)
                {
                    // Get image data
                    Image curFrame = images[i];
                    if (curFrame.Width > 256 || curFrame.Height > 256)
                        throw new ArgumentException("Image too large!", "images");
                    // for these three, 0 is interpreted as 256,
                    // so the cast reducing 256 to 0 is no problem.
                    Byte width = (Byte) curFrame.Width;
                    Byte height = (Byte) curFrame.Height;
                    Byte colors = (Byte) curFrame.Palette.Entries.Length;
                    Int32 bpp;
                    Byte[] frameData;
                    using (MemoryStream pngMs = new MemoryStream())
                    {
                        curFrame.Save(pngMs, ImageFormat.Png);
                        frameData = pngMs.ToArray();
                    }
                    // Get the color depth to save in the icon info. This needs to be
                    // fetched explicitly, since png does not support certain types
                    // like 16bpp, so it will convert to the nearest valid on save.
                    Byte colDepth = frameData[24];
                    Byte colType = frameData[25];
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
                    Int32 imageLen = frameData.Length;
                    // Write image entry
                    // 0 image width.
                    iconWriter.Write(width);
                    // 1 image height.
                    iconWriter.Write(height);
                    // 2 number of colors.
                    iconWriter.Write(colors);
                    // 3 reserved
                    iconWriter.Write((Byte) 0);
                    // 4-5 color planes
                    iconWriter.Write((Int16) 0);
                    // 6-7 bits per pixel
                    iconWriter.Write((Int16) bpp);
                    // 8-11 size of image data
                    iconWriter.Write(imageLen);
                    // 12-15 offset of image data
                    iconWriter.Write(offset);
                    offset += imageLen;
                }
                for (Int32 i = 0; i < imgCount; ++i)
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

        public static Int32 GetLastClearLine(Byte[] sourceData, Int32 stride, Int32 width, Int32 height, Color checkColor)
        {
            // Get color as UInt32 in advance.
            UInt32 checkColVal = (UInt32) checkColor.ToArgb();
            // Use MemoryStream with BinaryReader since it can read UInt32 from a byte array directly.
            using (MemoryStream ms = new MemoryStream(sourceData))
            using (BinaryReader sr = new BinaryReader(ms))
            {
                for (Int32 y = height - 1; y >= 0; --y)
                {
                    // Set position in the memory stream to the start of the current row.
                    ms.Position = stride * y;
                    // Put loop variable outside "if" so it is retained after the loop.
                    Int32 x;
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

        public static void TilePatterns(String materialsFolder, Int32 width, Int32 height, String resultFolder)
        {
            //For every material image, calls the fusion method below.
            foreach (String materialImagePath in Directory.GetFiles(materialsFolder))
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

        public static Bitmap TilePattern(Bitmap pattern, Int32 width, Int32 height, Color fillColor)
        {
            Int32 patternWidth = pattern.Width;
            Int32 patternHeight = pattern.Height;
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
                for (Int32 y = 0; y < height; y += patternHeight)
                {
                    for (Int32 x = 0; x < width; x += patternWidth)
                    {
                        g.DrawImage(pattern, new Point(x, y));
                    }
                }
            }
            return result;
        }

        public static void BakeImages(String whiteFilePath, String materialsFolder, String resultFolder)
        {
            Int32 width;
            Int32 height;
            Int32 stride;
            // extract bytes of shape & alpha image
            Byte[] shapeImageBytes;
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
                foreach (String materialImagePath in Directory.GetFiles(materialsFolder))
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

        public static Bitmap ExtractBlackImage(Byte[] shapeImageBytes, Int32 width, Int32 height, Int32 stride)
        {
            // Create black lines image.
            Byte[] imageBytesBlack = new Byte[shapeImageBytes.Length];
            // Line start offset is set to 3 to immediately get the alpha component.
            Int32 lineOffsImg = 3;
            for (Int32 y = 0; y < height; ++y)
            {
                Int32 curOffs = lineOffsImg;
                for (Int32 x = 0; x < width; ++x)
                {
                    // copy either alpha or inverted brightness (whichever is lowest)
                    // from the shape image onto black lines image as alpha, effectively
                    // only retaining the visible black lines from the shape image.
                    // I use curOffs - 1 (red) because it's the simplest operation.
                    Byte alpha = shapeImageBytes[curOffs];
                    Byte invBri = (Byte) (255 - shapeImageBytes[curOffs - 1]);
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

        public static Bitmap ApplyAlphaToImage(Byte[] alphaImageBytes, Int32 width, Int32 height, Int32 stride, Bitmap texture)
        {
            if (texture.Width != width || texture.Height != height)
                return null;
            // extract bytes of pattern image. Stride should be the same.
            Int32 patternStride;
            Byte[] imageBytesPattern = ImageUtils.GetImageData(texture, out patternStride, PixelFormat.Format32bppArgb);
            if (patternStride != stride)
                return null;
            // Line start offset is set to 3 to immediately get the alpha component.
            Int32 lineOffsImg = 3;
            for (Int32 y = 0; y < height; ++y)
            {
                Int32 curOffs = lineOffsImg;
                for (Int32 x = 0; x < width; ++x)
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
        public static void WriteImagesToIcons(List<String> imagePaths, String icoDirPath)
        {
            // Change this to whatever you prefer.
            InterpolationMode scalingMode = InterpolationMode.HighQualityBicubic;
            //imagePaths => all images which I am converting to ico files
            imagePaths.ForEach(imgPath =>
            {
                // The correct way of replacing an extension
                String icoPath = Path.Combine(icoDirPath, Path.GetFileNameWithoutExtension(imgPath) + ".ico");
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
            Int32 maxSide = source.Width > source.Height ? source.Width : source.Height;
            Bitmap bitmapResult = new Bitmap(maxSide, maxSide, PixelFormat.Format32bppArgb);
            using (Graphics graphicsResult = Graphics.FromImage(bitmapResult))
            {
                graphicsResult.Clear(canvasBackground);
                Int32 xOffset = (maxSide - source.Width) / 2;
                Int32 yOffset = (maxSide - source.Height) / 2;
                graphicsResult.DrawImage(source, new Rectangle(xOffset, yOffset, source.Width, source.Height));
            }
            return bitmapResult;
        }

        public static Bitmap Resize(this Bitmap source, Int32 width, Int32 height, InterpolationMode scalingMode)
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

        public static Byte[] ConvertImagesToIco(Image[] images)
        {
            if (images == null)
                throw new ArgumentNullException("images");
            Int32 imgCount = images.Length;
            if (imgCount == 0)
                throw new ArgumentException("No images given!", "images");
            if (imgCount > 0xFFFF)
                throw new ArgumentException("Too many images!", "images");
            using (MemoryStream ms = new MemoryStream())
            using (BinaryWriter iconWriter = new BinaryWriter(ms))
            {
                Byte[][] frameBytes = new Byte[imgCount][];
                // 0-1 reserved, 0
                iconWriter.Write((Int16)0);
                // 2-3 image type, 1 = icon, 2 = cursor
                iconWriter.Write((Int16)1);
                // 4-5 number of images
                iconWriter.Write((Int16)imgCount);
                // Calculate header size for first image data offset.
                Int32 offset = 6 + (16 * imgCount);
                for (Int32 i = 0; i < imgCount; ++i)
                {
                    // Get image data
                    Image curFrame = images[i];
                    if (curFrame.Width > 256 || curFrame.Height > 256)
                        throw new ArgumentException("Image too large!", "images");
                    // for these three, 0 is interpreted as 256,
                    // so the cast reducing 256 to 0 is no problem.
                    Byte width = (Byte)curFrame.Width;
                    Byte height = (Byte)curFrame.Height;
                    Byte colors = (Byte)curFrame.Palette.Entries.Length;
                    Int32 bpp;
                    Byte[] frameData;
                    using (MemoryStream pngMs = new MemoryStream())
                    {
                        curFrame.Save(pngMs, ImageFormat.Png);
                        frameData = pngMs.ToArray();
                    }
                    // Get the color depth to save in the icon info. This needs to be
                    // fetched explicitly, since png does not support certain types
                    // like 16bpp, so it will convert to the nearest valid on save.
                    Byte colDepth = frameData[24];
                    Byte colType = frameData[25];
                    // I think .Net saving only supports color types 2, 3 and 6 anyway.
                    switch (colType)
                    {
                        case 2: bpp = 3 * colDepth; break; // RGB
                        case 6: bpp = 4 * colDepth; break; // ARGB
                        default: bpp = colDepth; break; // Indexed & greyscale
                    }
                    frameBytes[i] = frameData;
                    Int32 imageLen = frameData.Length;
                    // Write image entry
                    // 0 image width. 
                    iconWriter.Write(width);
                    // 1 image height.
                    iconWriter.Write(height);
                    // 2 number of colors.
                    iconWriter.Write(colors);
                    // 3 reserved
                    iconWriter.Write((Byte)0);
                    // 4-5 color planes
                    iconWriter.Write((Int16)0);
                    // 6-7 bits per pixel
                    iconWriter.Write((Int16)bpp);
                    // 8-11 size of image data
                    iconWriter.Write(imageLen);
                    // 12-15 offset of image data
                    iconWriter.Write(offset);
                    offset += imageLen;
                }
                for (Int32 i = 0; i < imgCount; ++i)
                {
                    // Write image data
                    // png data must contain the whole png data file
                    iconWriter.Write(frameBytes[i]);
                }
                return ms.ToArray();
            }
        }

        public static void ConvertImagesToIco(Image[] images, String outputPath)
        {
            if (images == null)
                throw new ArgumentNullException("images");
            Int32 imgCount = images.Length;
            if (imgCount == 0)
                throw new ArgumentException("No images given!", "images");
            if (imgCount > 0xFFFF)
                throw new ArgumentException("Too many images!", "images");
            using (FileStream fs = new FileStream(outputPath, FileMode.Create, FileAccess.Write))
            using (BinaryWriter iconWriter = new BinaryWriter(fs))
            {
                Byte[][] frameBytes = new Byte[imgCount][];
                // 0-1 reserved, 0
                iconWriter.Write((Int16)0);
                // 2-3 image type, 1 = icon, 2 = cursor
                iconWriter.Write((Int16)1);
                // 4-5 number of images
                iconWriter.Write((Int16)imgCount);
                // Calculate header size for first image data offset.
                Int32 offset = 6 + (16 * imgCount);
                for (Int32 i = 0; i < imgCount; ++i)
                {
                    // Get image data
                    Image curFrame = images[i];
                    if (curFrame.Width > 256 || curFrame.Height > 256)
                        throw new ArgumentException("Image too large!", "images");
                    // for these three, 0 is interpreted as 256,
                    // so the cast reducing 256 to 0 is no problem.
                    Byte width = (Byte)curFrame.Width;
                    Byte height = (Byte)curFrame.Height;
                    Byte colors = (Byte)curFrame.Palette.Entries.Length;
                    Int32 bpp;
                    Byte[] frameData;
                    using (MemoryStream pngMs = new MemoryStream())
                    {
                        curFrame.Save(pngMs, ImageFormat.Png);
                        frameData = pngMs.ToArray();
                    }
                    // Get the color depth to save in the icon info. This needs to be
                    // fetched explicitly, since png does not support certain types
                    // like 16bpp, so it will convert to the nearest valid on save.
                    Byte colDepth = frameData[24];
                    Byte colType = frameData[25];
                    // I think .Net saving only supports color types 2, 3 and 6 anyway.
                    switch (colType)
                    {
                        case 2: bpp = 3 * colDepth; break; // RGB
                        case 6: bpp = 4 * colDepth; break; // ARGB
                        default: bpp = colDepth; break; // Indexed & greyscale
                    }
                    frameBytes[i] = frameData;
                    Int32 imageLen = frameData.Length;
                    // Write image entry
                    // 0 image width. 
                    iconWriter.Write(width);
                    // 1 image height.
                    iconWriter.Write(height);
                    // 2 number of colors.
                    iconWriter.Write(colors);
                    // 3 reserved
                    iconWriter.Write((Byte)0);
                    // 4-5 color planes
                    iconWriter.Write((Int16)0);
                    // 6-7 bits per pixel
                    iconWriter.Write((Int16)bpp);
                    // 8-11 size of image data
                    iconWriter.Write(imageLen);
                    // 12-15 offset of image data
                    iconWriter.Write(offset);
                    offset += imageLen;
                }
                for (Int32 i = 0; i < imgCount; ++i)
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

        public static Byte[] GetImageData(Bitmap sourceImage, out Int32 stride, PixelFormat desiredPixelFormat)
        {
            Int32 width = sourceImage.Width;
            Int32 height = sourceImage.Height;
            BitmapData sourceData = sourceImage.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.ReadOnly, desiredPixelFormat);
            stride = sourceData.Stride;
            Byte[] data = new Byte[stride * height];
            Marshal.Copy(sourceData.Scan0, data, 0, data.Length);
            sourceImage.UnlockBits(sourceData);
            return data;
        }

        public static Bitmap BuildImage(Byte[] sourceData, Int32 width, Int32 height, Int32 stride, PixelFormat pixelFormat)
        {
            Bitmap newImage = new Bitmap(width, height, pixelFormat);
            BitmapData targetData = newImage.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.WriteOnly, newImage.PixelFormat);
            Int32 newDataWidth = ((Image.GetPixelFormatSize(pixelFormat) * width) + 7) / 8;
            Int32 targetStride = targetData.Stride;
            Int64 scan0 = targetData.Scan0.ToInt64();
            for (Int32 y = 0; y < height; ++y)
                Marshal.Copy(sourceData, y * stride, new IntPtr(scan0 + y * targetStride), newDataWidth);
            newImage.UnlockBits(targetData);
            return newImage;
        }


#if UNSAFE
        public static unsafe Byte GetIndexedPixelUnsafe(Bitmap b, Int32 x, Int32 y)
        {
            if (x < 0 || x >= b.Width) throw new ArgumentOutOfRangeException("x", string.Format("x should be in 0-{0}", b.Width));
            if (y < 0 || y >= b.Height) throw new ArgumentOutOfRangeException("y", string.Format("y should be in 0-{0}", b.Height));
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
        public static Bitmap ReduceToTwoColorFade(Bitmap image, Boolean substitutePalette, Boolean bgWhite)
        {
            if (!substitutePalette)
                bgWhite = false;
            // Get data out of the image, using LockBits and Marshal.Copy
            Int32 width = image.Width;
            Int32 height = image.Height;
            // LockBits can actually -convert- the image data to the requested color depth.
            // 32 bpp is the easiest to get the color components out.
            BitmapData sourceData = image.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            // Not really needed for 32bpp, but technically the stride does not always match the
            // amount of used data on each line, since the stride gets rounded up to blocks of 4.
            Int32 stride = sourceData.Stride;
            Byte[] imgBytes = new Byte[stride * height];
            Marshal.Copy(sourceData.Scan0, imgBytes, 0, imgBytes.Length);
            image.UnlockBits(sourceData);
            // Make color population histogram
            Int32 lineOffset = 0;
            Dictionary<UInt32, Int32> histogram = new Dictionary<UInt32, Int32>();
            for (Int32 y = 0; y < height; ++y)
            {
                Int32 offset = lineOffset;
                for (Int32 x = 0; x < width; ++x)
                {
                    // Optional check: only handle if not mostly-transparent
                    if (imgBytes[offset + 3] > 0x7F)
                    {
                        // Get color values from bytes, without alpha.
                        // Little-endian: UInt32 0xAARRGGBB = Byte[] { BB, GG, RR, AA }
                        UInt32 val = (UInt32)((0xFF << 24) | (imgBytes[offset + 2] << 16) | (imgBytes[offset + 1] << 8) | imgBytes[offset + 0]);
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
            KeyValuePair<UInt32, Int32>[] histoSorted = histogram.OrderByDescending(c => c.Value).ToArray();
            // Since we filter on alpha, getting a result is not 100% guaranteed.
            Color colBackgr = histoSorted.Length < 1 ? Color.Black : Color.FromArgb((Int32)histoSorted[0].Key);
            // if less than 2 colors, just default it to the same.
            Color colContent = histoSorted.Length < 2 ? colBackgr : Color.FromArgb((Int32)histoSorted[1].Key);
            // Make a new 256-color palette, making a fade between these two colors, for feeding into GetClosestPaletteIndexMatch later
            Color[] matchPal = new Color[0x100];
            Color toBlack = bgWhite ? colContent : colBackgr;
            Color toWhite = bgWhite ? colBackgr : colContent;
            Int32 rFirst = toBlack.R;
            Int32 gFirst = toBlack.G;
            Int32 bFirst = toBlack.B;
            Double rDif = (toBlack.R - toWhite.R) / 255.0;
            Double gDif = (toBlack.G - toWhite.G) / 255.0;
            Double bDif = (toBlack.B - toWhite.B) / 255.0;
            for (Int32 i = 0; i < 0x100; ++i)
                matchPal[i] = Color.FromArgb(
                    Math.Min(0xFF, Math.Max(0, rFirst - (Int32)Math.Round(rDif * i, MidpointRounding.AwayFromZero))),
                    Math.Min(0xFF, Math.Max(0, gFirst - (Int32)Math.Round(gDif * i, MidpointRounding.AwayFromZero))),
                    Math.Min(0xFF, Math.Max(0, bFirst - (Int32)Math.Round(bDif * i, MidpointRounding.AwayFromZero))));
            // Ensure start and end point are correct, and not mangled by small rounding errors.
            matchPal[0x00] = toBlack;
            matchPal[0xFF] = toWhite;
            // Small extra: ignore duplicates of the highest color, to ensure that
            // all matches of the highest color itself actually end up on index 0xFF.
            List<Int32> ignoreIndices = new List<Int32>();
            for (Int32 i = 0; i < 0xFF; ++i)
                if (matchPal[i] == toWhite)
                    ignoreIndices.Add(i);
            // The 8-bit stride is simply the width in this case.
            Int32 stride8Bit = width;
            // Make 8-bit array to store the result
            Byte[] imgBytes8Bit = new Byte[stride8Bit * height];
            // Reset offset for a new loop through the image data
            lineOffset = 0;
            // Make new offset vars for a loop through the 8-bit image data
            Int32 lineOffset8Bit = 0;
            for (Int32 y = 0; y < height; ++y)
            {
                Int32 offset = lineOffset;
                Int32 offset8Bit = lineOffset8Bit;
                for (Int32 x = 0; x < width; ++x)
                {
                    Int32 toWrite;
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
                    imgBytes8Bit[offset8Bit] = (Byte)toWrite;
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
            Int32 newDataWidth = ((Image.GetPixelFormatSize(newBm.PixelFormat) * width) + 7) / 8;
            // Note that this Stride will most likely NOT match the image width; it is rounded up to the
            // next multiple of 4 bytes. For that reason, we copy the data per line, and not as one block.
            Int32 targetStride = targetData.Stride;
            Int64 scan0 = targetData.Scan0.ToInt64();
            for (Int32 y = 0; y < height; ++y)
                Marshal.Copy(imgBytes8Bit, y * stride8Bit, new IntPtr(scan0 + y * targetStride), newDataWidth);
            newBm.UnlockBits(targetData);
            // Set final image palette to grayscale fade.
            // 'Image.Palette' makes a COPY of the palette when accessed.
            // So copy it out, modify it, then copy it back in.
            ColorPalette pal = newBm.Palette;
            if (substitutePalette)
            {
                for (Int32 i = 0; i < 0x100; ++i)
                    pal.Entries[i] = Color.FromArgb(i, i, i);
            }
            else
            {
                for (Int32 i = 0; i < 0x100; ++i)
                    pal.Entries[i] = matchPal[i];
            }
            newBm.Palette = pal;
            return newBm;
        }

    }
}

#endif
