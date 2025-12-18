using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.InteropServices;

namespace Nyerguds.ImageManipulation
{
    /// <summary>
    /// Code written for StackOverflow and not actively used in projects. Currently set to not compile into the project.
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
        /// <param name="data">Two-dimensional Int32 array containing colours.</param>
        /// <returns>Image</returns>
        public static Bitmap FromTwoDimIntArray(Int32[,] data)
        {
            Int32 width = data.GetLength(0);
            Int32 height = data.GetLength(1);
            Int32 byteIndex = 0;
            Byte[] dataBytes = new Byte[height * width * 4];
            for (Int32 y = 0; y < height; y++)
            {
                for (Int32 x = 0; x < width; x++)
                {
                    // UInt32 0xAARRGGBB = Byte[] { BB, GG, RR, AA }
                    UInt32 val = (UInt32)data[x, y];
                    // This code clears out everything but a specific part of the value
                    // and then shifts the remaining piece down to the lowest byte
                    dataBytes[byteIndex + 0] = (Byte)(val & 0x000000FF); // B
                    dataBytes[byteIndex + 1] = (Byte)((val & 0x0000FF00) >> 08); // G
                    dataBytes[byteIndex + 2] = (Byte)((val & 0x00FF0000) >> 16); // R
                    dataBytes[byteIndex + 3] = (Byte)((val & 0xFF000000) >> 24); // A
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
        /// <param name="data">Two-dimensional Int32 array containing colour data of a greyscale image.</param>
        /// <returns>Image</returns>
        public static Bitmap FromTwoDimIntArrayGray(Int32[,] data)
        {
            Int32 width = data.GetLength(0);
            Int32 height = data.GetLength(1);
            Int32 byteIndex = 0;
            Byte[] dataBytes = new Byte[height * width];
            for (Int32 y = 0; y < height; y++)
            {
                for (Int32 x = 0; x < width; x++)
                {
                    // Int32 0xAARRGGBB = Byte[] { BB, GG, RR, AA }
                    // This uses the lowest byte, which is the blue component.
                    dataBytes[byteIndex] = (Byte)((UInt32)data[x, y] & 0xFF);
                    // More efficient than multiplying
                    byteIndex++;
                }
            }
            Color[] palette = new Color[256];
            for (Int32 i = 0; i < palette.Length; i++)
                palette[i] = Color.FromArgb(i, i, i);
            return ImageUtils.BuildImage(dataBytes, width, height, width, PixelFormat.Format8bppIndexed, palette, null);
        }

        /// <summary>
        /// Checks if a given image contains transparency.
        /// Written for a StackOverflow question.
        /// https://stackoverflow.com/a/39013496/395685
        /// </summary>
        /// <param name="bitmap">Input bitmap</param>
        /// <returns>True if pixels were found with an alpha value of less than 255.</returns>
        public static Boolean HasTransparency(Bitmap bitmap)
        {
            // not an alpha-capable color format. Note that GDI+ indexed images are alpha-capable on the palette.
            if (((ImageFlags)bitmap.Flags & ImageFlags.HasAlpha) == 0)
                return false;
            // Indexed format, and no alpha colours in the images palette: immediate pass.
            if ((bitmap.PixelFormat & PixelFormat.Indexed) != 0 && bitmap.Palette.Entries.All(c => c.A == 255))
                return false;
            Int32 stride;
            Byte[] data = ImageUtils.GetImageData(bitmap, out stride, PixelFormat.Format32bppArgb);
            Int32 height = bitmap.Height;
            Int32 width = bitmap.Height;
            Int32 curRowOffs = 0;
            for (Int32 y = 0; y < height; y++)
            {
                // Set offset to first alpha value on current row
                Int32 curOffs = curRowOffs + 3;
                for (Int32 x = 0; x < width; x++)
                {
                    if (data[curOffs] != 255)
                        return true;
                    curOffs += 4;
                }
                // Increase row offset
                curRowOffs += stride;
            }
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
            // Indexed format, and no non-gray colours in the images palette: immediate pass.
            if ((bitmap.PixelFormat & PixelFormat.Indexed) != 0 && bitmap.Palette.Entries.All(c => c.R == c.G && c.R == c.B))
                return true;
            Int32 stride;
            Byte[] data = ImageUtils.GetImageData(bitmap, out stride, PixelFormat.Format32bppArgb);
            Int32 curRowOffs = 0;
            Int32 height = bitmap.Height;
            Int32 width = bitmap.Height;
            for (Int32 y = 0; y < height; y++)
            {
                // Set offset to start of current row
                Int32 curOffs = curRowOffs;
                for (Int32 x = 0; x < width; x++)
                {
                    Byte b = data[curOffs];
                    Byte g = data[curOffs + 1];
                    Byte r = data[curOffs + 2];
                    Byte a = data[curOffs + 3];
                    // Increase offset to next colour
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
        /// <param name="width">Width</param>
        /// <param name="height">Height</param>
        /// <param name="colors">color palette</param>
        /// <param name="color1">Index for color 1</param>
        /// <param name="color2">Index for color 2</param>
        /// <returns></returns>
        public static Bitmap GenerateCheckerboardImage(Int32 width, Int32 height, Color[] colors, Byte color1, Byte color2)
        {
            if (width == 0 || height == 0)
                return null;
            Byte[] patternArray = new Byte[width * height];
            for (Int32 y = 0; y < height; y++)
            {
                for (Int32 x = 0; x < width; x++)
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
        /// <param name="lines"></param>
        /// <param name="startColumn"></param>
        /// <param name="maxValue"></param>
        /// <returns></returns>
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
            for (Int32 i = top; i < bottom; i++)
                values[i - top] = lines[i].Split(',');
            // Find width: maximum csv line length minus the amount of columns to skip.
            Int32 width = values.Max(line => line.Length) - startColumn;
            if (width <= 0)
                return null;
            // Create the array. Since it's 8-bit, this is one byte per pixel.
            Byte[] imageArray = new Byte[width * height];
            // Parse all values into the array
            // Y = lines, X = csv values
            for (Int32 y = 0; y < height; y++)
            {
                Int32 offset = y * width;
                // Skip indices before "startColumn". Target offset starts from the start of the line anyway.
                for (Int32 x = startColumn; x < values[y].Length; x++)
                {
                    Int32 val;
                    // Don't know if Trim is needed here. Depends on the file.
                    if (Int32.TryParse(values[y][x].Trim(), out val))
                        imageArray[offset] = (Byte)Math.Max(0, Math.Min(val, maxValue));
                    offset++;
                }
            }
            // generate gray palette for the given range, by calculating the factor to multiply by.
            Double mulFactor = 255d / maxValue;
            Color[] palette = new Color[maxValue + 1];
            for (Int32 i = 0; i <= maxValue; i++)
            {
                // Away from zero rounding: 2.4 => 2 ; 2.5 => 3
                Byte v = (Byte)Math.Round(i * mulFactor, MidpointRounding.AwayFromZero);
                palette[i] = Color.FromArgb(v, v, v);
            }
            return ImageUtils.BuildImage(imageArray, width, height, width, PixelFormat.Format8bppIndexed, palette, Color.White);
        }

        /// <summary>
        /// Creates high-quality grayscale version of an image.
        /// Written for a StackOverflow question.
        /// https://stackoverflow.com/a/49441191/395685
        /// </summary>
        /// <param name="img"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <returns></returns>
        public static Bitmap GetGrayImage(Image img, Int32 width, Int32 height)
        {
            // get image data
            Bitmap b = new Bitmap(img, width, height);
            BitmapData sourceData = b.LockBits(new Rectangle(0, 0, b.Width, b.Height), ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
            Int32 stride = sourceData.Stride;
            Byte[] data = new Byte[stride * b.Height];
            Marshal.Copy(sourceData.Scan0, data, 0, data.Length);
            // iterate
            for (Int32 y = 0; y < height; y++)
            {
                Int32 offset = y * stride;
                for (Int32 x = 0; x < width; x++)
                {
                    Byte colB = data[offset + 0]; // B
                    Byte colG = data[offset + 1]; // G
                    Byte colR = data[offset + 2]; // R
                    //Int32 ColA = data[offset + 3]; // A
                    Byte grayValue = ColorUtils.GetGreyValue(colR, colG, colB);
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
            for (Int32 y = 0; y < actualHeight; y++)
            {
                Int32 curPtr = y * stride;
                Int32 resPtr = y * actualStride;
                for (Int32 x = 0; x < actualWidth; x++)
                {
                    // Get correct colour components from sliding window
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
                    result[resPtr + 1] = (Byte)((greenCol1 + greenCol2) / 2);
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
            for (Int32 y = 0; y < height; y++)
            {
                Int32 curPtr = y * stride;
                Int32 resPtr = y * actualStride;
                for (Int32 x = 0; x < width; x++)
                {
                    // Get correct colour components from sliding window
                    Boolean isGreen = (x + y) % 2 == (greenFirst ? 0 : 1); // all corner colours and center are green.
                    Boolean isBlueRow = y % 2 == (blueRowFirst ? 0 : 1);
                    Byte valGreen;
                    Byte valRed;
                    Byte valBlue;
                    Byte pxCol = arr[curPtr];
                    Byte? tpCol1 = null;
                    Byte? tpCol2 = null;
                    Byte? tpCol3 = null;
                    Byte? lfCol = null;
                    Byte? rtCol = x == lastCol ? (Byte?)null : arr[curPtr + 1];
                    Byte? btCol1 = null;
                    Byte? btCol2 = y == lastRow ? (Byte?)null : arr[curPtr + stride];
                    Byte? btCol3 = y == lastRow || x == lastCol ? (Byte?)null : arr[curPtr + stride + 1];

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
            for (Int32 y = 0; y < height; y++)
            {
                Int32 curPtr = y * stride;
                Int32 resPtr = y * actualStride;
                for (Int32 x = 0; x < width; x++)
                {
                    // Get correct colour components from sliding window
                    Boolean isGreen = (x + y) % 2 == (greenFirst ? 0 : 1); // all corner colours and center are green.
                    Boolean isBlueRow = y % 2 == (blueRowFirst ? 0 : 1);
                    Byte valGreen;
                    Byte valRed;
                    Byte valBlue;

                    Byte cntrCol = arr[curPtr];
                    Byte? tplfCol = y == 0 || x == 0 ? (Byte?)null : arr[curPtr - stride - 1];
                    Byte? tpcnCol = y == 0 ? (Byte?)null : arr[curPtr - stride];
                    Byte? tprtCol = y == 0 || x == lastCol ? (Byte?)null : arr[curPtr - stride + 1];
                    Byte? cnlfCol = x == 0 ? (Byte?)null : arr[curPtr - 1];
                    Byte? cnrtCol = x == lastCol ? (Byte?)null : arr[curPtr + 1];
                    Byte? btlfCol = y == lastRow || x == 0 ? (Byte?)null : arr[curPtr + stride - 1];
                    Byte? btcnCol = y == lastRow ? (Byte?)null : arr[curPtr + stride];
                    Byte? btrtCol = y == lastRow || x == lastCol ? (Byte?)null : arr[curPtr + stride + 1];

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
        /// <returns>The average value, or 0x80 if no values were given?</returns>
        private static Byte GetAverageCol(params Byte?[] cols)
        {
            Int32 colsCount = 0;
            foreach (Byte? col in cols)
                if (col.HasValue) colsCount++;
            Int32 avgVal = 0;
            foreach (Byte? col in cols)
                avgVal += col.GetValueOrDefault();
            return colsCount == 0 ? (Byte)0x80 : (Byte)(avgVal / colsCount);
        }

        /// <summary>
        /// Extracts a channel as two-dimensional array.
        /// Written for a StackOverflow question.
        /// https://stackoverflow.com/a/50077006/395685
        /// </summary>
        /// <param name="image">Input image</param>
        /// <param name="channelNr">0 = B, 1 = G, 2 = R</param>
        /// <returns></returns>
        public static Int32[,] GetChannel(Bitmap image, Int32 channelNr)
        {
            if (channelNr >= 3 || channelNr < 0)
                throw new IndexOutOfRangeException();
            Int32 width = image.Width;
            Int32 height = image.Height;
            Int32 stride;
            Byte[] dataBytes = ImageUtils.GetImageData(image, out stride, PixelFormat.Format24bppRgb);
            Int32[,] channel = new Int32[height, width];
            for (Int32 y = 0; y < height; y++)
            {
                Int32 offset = y * stride;
                for (Int32 x = 0; x < width; x++)
                {
                    channel[y, x] = dataBytes[offset + channelNr];
                    offset += 3;
                }
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
        /// Creates an image from colour channels.
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
            for (Int32 y = 0; y < height; y++)
            {
                // use stride to get the start offset of each line
                Int32 offset = y * stride;
                for (Int32 x = 0; x < width; x++)
                {
                    PixelValues[offset + 0] = (Byte)blueChannel[y, x];
                    PixelValues[offset + 1] = (Byte)greenChannel[y, x];
                    PixelValues[offset + 2] = (Byte)redChannel[y, x];
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
            for (Int32 y = 0; y < h; y++)
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
    }
}