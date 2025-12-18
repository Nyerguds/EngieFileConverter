using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace Nyerguds.ImageManipulation
{
    /// <summary>
    /// Blob detection class. Originally written for StackOverflow question https://stackoverflow.com/q/50277978/395685 but never posted there since it's a homework question.
    /// Answer link is https://stackoverflow.com/a/50282882/395685
    /// </summary>
    public static class BlobDetection
    {
        //Example code

        /*/
        /// <summary>
        /// Detects darker or brighter spots on the image by brightness threshold, and returns their center points.
        /// </summary>
        /// <param name="image">Input image.</param>
        /// <param name="detectDark">Detect dark spots. False to detect bright drops.</param>
        /// <param name="brightnessThreshold">Brightness threshold needed to see a pixel as "bright".</param>
        /// <param name="mergeThreshold">The found spots are merged based on their square bounds. This is the amount of added pixels when checking these bounds. Use -1 to disable all merging.</param>
        /// <param name="getEdgesOnly">True to make the returned lists only contain the edges of the blobs. This saves a lot of memory.</param>
        /// <returns>A list of points indicating the centers of all found spots.</returns>
        public static List<Point> FindPoints(Bitmap image, Boolean detectDark, Single brightnessThreshold, Int32 mergeThreshold, Boolean getEdgesOnly)
        {
            List<List<Point>> blobs = FindBlobs(image, detectDark, brightnessThreshold, mergeThreshold, getEdgesOnly);
            return blobs.Select(GetBlobCenter).ToList();
        }

        /// <summary>
        /// Detects darker or brighter spots on the image by brightness threshold, and returns their list of points.
        /// </summary>
        /// <param name="image">Input image.</param>
        /// <param name="detectDark">Detect dark spots. False to detect bright drops.</param>
        /// <param name="brightnessThreshold">Brightness threshold. Use -1 to attempt automatic levelling.</param>
        /// <param name="mergeThreshold">The found spots are merged based on their square bounds. This is the amount of added pixels when checking these bounds. Use -1 to disable all merging.</param>
        /// <param name="getEdgesOnly">True to make the returned lists only contain the edges of the blobs. This saves a lot of memory.</param>
        /// <returns>A list of points indicating the centers of all found spots.</returns>
        public static List<List<Point>> FindBlobs(Bitmap image, Boolean detectDark, Single brightnessThreshold, Int32 mergeThreshold, Boolean getEdgesOnly)
        {
            Boolean detectVal = !detectDark;
            Int32 width = image.Width;
            Int32 height = image.Height;
            // Binarization: get 32-bit data
            Int32 stride;
            Byte[] data = ImageUtils.GetImageData(image, out stride, PixelFormat.Format32bppArgb);
            // Binarization: get brightness
            Single[,] brightness = new Single[height, width];
            Int32 offset = 0;
            Byte groups = 255;
            for (Int32 y = 0; y < height; y++)
            {
                // use stride to get the start offset of each line
                Int32 usedOffset = offset;
                for (Int32 x = 0; x < width; x++)
                {
                    // get colour
                    Byte blu = data[usedOffset + 0];
                    Byte grn = data[usedOffset + 1];
                    Byte red = data[usedOffset + 2];
                    Color c = Color.FromArgb(red, grn, blu);
                    brightness[y, x] = c.GetBrightness();
                    usedOffset += 4;
                }
                offset += stride;
            }
            if (brightnessThreshold < 0)
            {
                Dictionary<Byte, Int32> histogram = new Dictionary<Byte, Int32>();
                for (Int32 y = 0; y < height; y++)
                {
                    for (Int32 x = 0; x < width; x++)
                    {
                        Byte val = (Byte)(brightness[y, x] * groups);
                        Int32 num;
                        histogram.TryGetValue(val, out num);
                        histogram[val] = num + 1;
                    }
                }
                List<KeyValuePair<Byte, Int32>> sortedHistogram = histogram.OrderBy(x => x.Value).ToList();
                sortedHistogram.Reverse();
                sortedHistogram = sortedHistogram.Take(groups * 9 / 10).ToList();
                Byte maxBrightness = sortedHistogram.Max(x => x.Key);
                Byte minBrightness = sortedHistogram.Min(x => x.Key);
                // [............m.............T.............M............]
                // still not very good... need to find some way to detect image highlights. Probably needs K-means clustering...
                brightnessThreshold = (minBrightness + (maxBrightness - minBrightness) * .5f) / groups;
            }
            // Binarization: convert to 1-byte-per-pixel array of 1/0 values based on a brightness threshold
            Boolean[,] dataBw = new Boolean[height, width];
            for (Int32 y = 0; y < height; y++)
                for (Int32 x = 0; x < width; x++)
                    dataBw[y, x] = brightness[y, x] > brightnessThreshold;

            // Detect blobs.
            // Could technically simplify the required Func<> to remove the imgData and directly reference dataBw, but meh.
            Func<Boolean[,], Int32, Int32, Boolean> clearsThreshold = (imgData, yVal, xVal) => imgData[yVal, xVal] == detectVal;
            return FindBlobs(dataBw, width, height, clearsThreshold, true, mergeThreshold, getEdgesOnly);
        }
        //*/

        /// <summary>
        /// Detects a list of all blobs in the image, and merges any with bounds that intersect with each other according to the 'mergeThreshold' parameter.
        /// </summary>
        /// <typeparam name="T">Type of the list to detect equal neighbours in.</typeparam>
        /// <param name="data">Image data array. It is processed as one pixel per coordinate.</param>
        /// <param name="width">Image width.</param>
        /// <param name="height">Image height.</param>
        /// <param name="clearsThreshold">Function to check if the pixel at the given coordinates clears the threshold. Should be of the format (imgData, yVal, xVal) => Boolean.</param>
        /// <param name="allEightEdges">When scanning for pixels to add to the blob, scan all eight surrounding pixels rather than just top, left, bottom, right.</param>
        /// <param name="mergeThreshold">The found spots are merged based on their square bounds. This is the amount of added pixels when checking these bounds. Use -1 to disable all merging.</param>
        /// <param name="getEdgesOnly">True to make the lists in 'blobs' only contain the edge points of the blobs. The 'inBlobs' items will still have all points marked.</param>
        public static List<List<Point>> FindBlobs<T>(T data, Int32 width, Int32 height, Func<T, Int32, Int32, Boolean> clearsThreshold, Boolean allEightEdges, Int32 mergeThreshold, Boolean getEdgesOnly)
        {
            List<Boolean[,]> inBlobs;
            List<List<Point>> blobs = FindBlobs(data, width, height, clearsThreshold, allEightEdges, getEdgesOnly, out inBlobs);
            MergeBlobs(blobs, width, height, null, mergeThreshold);
            return blobs;
        }


        /// <summary>
        /// Detects a list of all blobs in the image, and merges any with bounds that intersect with each other according to the 'mergeThreshold' parameter.
        /// Returns the result as Boolean[,] array.
        /// </summary>
        /// <typeparam name="T">Type of the list to detect equal neighbours in.</typeparam>
        /// <param name="data">Image data array. It is processed as one pixel per coordinate.</param>
        /// <param name="width">Image width.</param>
        /// <param name="height">Image height.</param>
        /// <param name="clearsThreshold">Function to check if the pixel at the given coordinates clears the threshold. Should be of the format (imgData, yVal, xVal) => Boolean.</param>
        /// <param name="allEightEdges">When scanning for pixels to add to the blob, scan all eight surrounding pixels rather than just top, left, bottom, right.</param>
        /// <param name="mergeThreshold">The found spots are merged based on their square bounds. This is the amount of added pixels when checking these bounds. Use -1 to disable all merging.</param>
        /// <param name="getEdgesOnly">True to make the lists in 'blobs' only contain the edge points of the blobs. The 'inBlobs' items will still have all points marked.</param>
        public static List<Boolean[,]> FindBlobsAsBooleans<T>(T data, Int32 width, Int32 height, Func<T, Int32, Int32, Boolean> clearsThreshold, Boolean allEightEdges, Int32 mergeThreshold, Boolean getEdgesOnly)
        {
            List<Boolean[,]> inBlobs;
            List<List<Point>> blobs = FindBlobs(data, width, height, clearsThreshold, allEightEdges, getEdgesOnly, out inBlobs);
            MergeBlobs(blobs, width, height, inBlobs, mergeThreshold);
            return inBlobs;
        }

        /// <summary>
        /// Detects a list of all blobs in the image. Does no merging.
        /// </summary>
        /// <typeparam name="T">Type of the list to detect equal neighbours in.</typeparam>
        /// <param name="data">Image data array. It is processed as one pixel per coordinate.</param>
        /// <param name="width">Image width.</param>
        /// <param name="height">Image height.</param>
        /// <param name="clearsThreshold">Function to check if the pixel at the given coordinates clears the threshold. Should be of the format (imgData, yVal, xVal) => Boolean.</param>
        /// <param name="allEightEdges">When scanning for pixels to add to the blob, scan all eight surrounding pixels rather than just top, left, bottom, right.</param>
        /// <param name="getEdgesOnly">True to make the lists in 'blobs' only contain the edge points of the blobs. The 'inBlobs' items will still have all points marked.</param>
        public static List<List<Point>> FindBlobs<T>(T data, Int32 width, Int32 height, Func<T, Int32, Int32, Boolean> clearsThreshold, Boolean allEightEdges, Boolean getEdgesOnly)
        {
            List<Boolean[,]> inBlobs;
            List<List<Point>> blobs = FindBlobs(data, width, height, clearsThreshold, allEightEdges, getEdgesOnly, out inBlobs);
            return blobs;
        }

        /// <summary>
        /// Detects a list of all blobs in the image, returning both the blobs and the boolean representations of the blobs. Does no merging.
        /// </summary>
        /// <typeparam name="T">Type of the list to detect equal neighbours in.</typeparam>
        /// <param name="data">Image data array. It is processed as one pixel per coordinate.</param>
        /// <param name="width">Image width.</param>
        /// <param name="height">Image height.</param>
        /// <param name="clearsThreshold">Function to check if the pixel at the given coordinates clears the threshold. Should be of the format (imgData, yVal, xVal) => Boolean.</param>
        /// <param name="allEightEdges">When scanning for pixels to add to the blob, scan all eight surrounding pixels rather than just top, left, bottom, right.</param>
        /// <param name="getEdgesOnly">True to make the lists in 'blobs' only contain the edge points of the blobs. The 'inBlobs' items will still have all points marked.</param>
        /// <param name="inBlobs">Output parameter for receiving the blobs as boolean[,] arrays.</param>
        public static List<List<Point>> FindBlobs<T>(T data, Int32 width, Int32 height, Func<T, Int32, Int32, Boolean> clearsThreshold, Boolean allEightEdges, Boolean getEdgesOnly, out List<Boolean[,]> inBlobs)
        {
            List<List<Point>> blobs = new List<List<Point>>();
            inBlobs = new List<Boolean[,]>();
            for (Int32 y = 0; y < height; y++)
                for (Int32 x = 0; x < width; x++)
                    AddBlobForPoint(x, y, data, width, height, clearsThreshold, blobs, inBlobs, allEightEdges, getEdgesOnly);
            return blobs;
        }

        /// <summary>
        /// Merge any blobs that fall in each other's square bounds, to reduce the amount of stray pixels.
        /// Bounds are inflated by the amount of pixels specified in mergeThreshold.
        /// </summary>
        /// <param name="blobs">The collection of blobs. The objects in this are adapted.</param>
        /// <param name="width">width of full image. Use -1 to detect from blob bounds.</param>
        /// <param name="height">Height of full image. Use -1 to detect from blob bounds.</param>
        /// <param name="inBlobs">Boolean arrays that contain whether pixels are in a blob. If not null, these are adapted too.</param>
        /// <param name="mergeThreshold">The found blobs are merged based on their square bounds. This is the amount of added pixels when checking these bounds. Use -1 to disable all merging.</param>
        public static void MergeBlobs(List<List<Point>> blobs, Int32 width, Int32 height, List<Boolean[,]> inBlobs, Int32 mergeThreshold)
        {
            if (width == -1 || height == -1)
            {
                width = -1;
                height = -1;
                foreach (List<Point> blob in blobs)
                {
                    foreach (Point point in blob)
                    {
                        if (width < point.X)
                            width = point.X;
                        if (height < point.Y)
                            height = point.Y;
                    }
                }
                // because width and height are sizes, not highest coordinates.
                width++;
                height++;
            }
            Boolean continueMerge = mergeThreshold >= 0;
            List<Rectangle> collBounds = new List<Rectangle>();
            List<Rectangle> collBoundsInfl = new List<Rectangle>();
            Rectangle imageBounds = new Rectangle(0, 0, width, height);
            if (continueMerge)
            {
                foreach (Rectangle rect in blobs.Select(GetBlobBounds))
                {
                    collBounds.Add(rect);
                    Rectangle rectInfl = Rectangle.Inflate(rect, mergeThreshold, mergeThreshold);
                    collBoundsInfl.Add(Rectangle.Intersect(imageBounds, rectInfl));
                }
            }
            Int32 blobsCount = blobs.Count;
            while (continueMerge)
            {
                continueMerge = false;
                for (Int32 i = 0; i < blobsCount; i++)
                {
                    List<Point> blob1 = blobs[i];
                    if (blob1.Count == 0)
                        continue;
                    Boolean[,] inBlob1 = inBlobs == null ? null : inBlobs[i];
                    Rectangle checkBounds = collBoundsInfl[i];
                    for (Int32 j = 0; j < blobsCount; j++)
                    {
                        if (i == j)
                            continue;
                        List<Point> blob2 = blobs[j];
                        if (blob2.Count == 0)
                            continue;
                        // collBounds corresponds to blobs in length.
                        Rectangle bounds2 = collBounds[j];
                        if (!checkBounds.IntersectsWith(bounds2))
                            continue;
                        // should be safe without checks; there are already
                        // checks against duplicates in these collections.
                        continueMerge = true;
                        blob1.AddRange(blob2);
                        // Mark all points on boolean array. Easier to use the points list for this instead of the second inBlobs array.
                        if (inBlob1 != null)
                            foreach (Point p in blob2)
                                inBlob1[p.Y, p.X] = true;
                        Rectangle rect1New = GetBlobBounds(blob1);
                        collBounds[i] = rect1New;
                        Rectangle rect1NewInfl = Rectangle.Inflate(rect1New, mergeThreshold, mergeThreshold);
                        collBoundsInfl[i] = Rectangle.Intersect(imageBounds, rect1NewInfl);
                        blob2.Clear();
                        // don't bother clearing inBlob2 or colbounds[j]; they don't get referenced anymore,
                        // and the cleared blob's boolean array gets filtered out at the end.
                    }
                }
            }
            // Filter out removed entries.
            Int32[] nonEmptyIndices = Enumerable.Range(0, blobsCount).Where(i => blobs[i].Count > 0).ToArray();
            // Nothing to remove.
            if (nonEmptyIndices.Length == blobsCount)
                return;
            if (inBlobs != null)
            {
                List<Boolean[,]> trimmedInBlobs = new List<Boolean[,]>();
                foreach (Int32 i in nonEmptyIndices)
                    trimmedInBlobs.Add(inBlobs[i]);
                inBlobs.Clear();
                inBlobs.AddRange(trimmedInBlobs);
            }
            List<List<Point>> trimmedBlobs = new List<List<Point>>();
            foreach (Int32 i in nonEmptyIndices)
                trimmedBlobs.Add(blobs[i]);
            blobs.Clear();
            blobs.AddRange(trimmedBlobs);
        }

        public static Point GetBlobCenter(List<Point> blob)
        {
            if (blob.Count == 0)
                return Point.Empty;
            Rectangle bounds = GetBlobBounds(blob);
            return new Point(bounds.X + (bounds.Width - 1) / 2, bounds.Y + (bounds.Height - 1) / 2);
        }

        /// <summary>
        /// If the current point clears the threshold and is not already present in the given blobs collection, this builds a list of all points
        /// adjacent to the current point and adds it to the list of blobs. Loop this over every pixel of an image to detect all blobs.
        /// </summary>
        /// <typeparam name="T">Type of the list to detect equal neighbours in. This system allows any kind of data to be taken as input.</typeparam>
        /// <param name="pointX">X-coordinate of the current point.</param>
        /// <param name="pointY">Y-coordinate of the current point.</param>
        /// <param name="data">Image data array. It is processed as one pixel per coordinate.</param>
        /// <param name="width">Image width.</param>
        /// <param name="height">Image height.</param>
        /// <param name="clearsThreshold">Function to check if the pixel at the given coordinates clears the threshold. Should be of the format (imgData, yVal, xVal) => Boolean.</param>
        /// <param name="blobs">Current list of point collections to add new detected blobs to.</param>
        /// <param name="inBlobs">Current list of point collections represented as boolean arrays, for very quick checks to see if a set of coordinates is in a collection.</param>
        /// <param name="allEightEdges">When scanning for pixels to add to the blob, scan all eight surrounding pixels rather than just top, left, bottom, right.</param>
        /// <param name="getEdgesOnly">True to make the lists in 'blobs' only contain the edge points of the blobs. The 'inBlobs' items will still have all points marked.</param>
        public static void AddBlobForPoint<T>(Int32 pointX, Int32 pointY, T data, Int32 width, Int32 height, Func<T, Int32, Int32, Boolean> clearsThreshold, List<List<Point>> blobs, List<Boolean[,]> inBlobs, Boolean allEightEdges, Boolean getEdgesOnly)
        {
            // If the point does not clear the threshold, abort.
            if (!clearsThreshold(data, pointY, pointX))
                return;
            // if the point is already in any of the collections, abort. The inBlobs collection reduces this to just one simple array index check per existing blob.
            foreach (Boolean[,] inCheckBlob in inBlobs)
                if (inCheckBlob[pointY, pointX])
                    return;
            // Initialize blob
            List<Point> blob = new List<Point>();
            // existence check optimisation in the form of a boolean grid that is kept synced with the points in the collection.
            Boolean[,] inBlob = new Boolean[height, width];
            // setting up all variables to use, making sure nothing needs to be fetched inside the loops
            List<Point> currentEdge = new List<Point>();
            Int32 lastX = width - 1;
            Int32 lastY = height - 1;
            List<Point> nextEdge = new List<Point>();
            Boolean[,] inNextEdge = new Boolean[height, width];
            Int32 clearLen = inNextEdge.Length;
            // starting point
            currentEdge.Add(new Point(pointX, pointY));
            // Start looking.
            while (currentEdge.Count > 0)
            {
                // 1. Add current edge collection to the blob.
                // Memory-unoptimised: add all points.
                if (!getEdgesOnly)
                    blob.AddRange(currentEdge);
                foreach (Point p in currentEdge)
                {
                    Int32 x = p.X;
                    Int32 y = p.Y;
                    inBlob[y, x] = true;
                    // Memory-optimised: add edge points only. inBlob will still contain all points.
                    if (getEdgesOnly &&
                        (x == 0 || y == 0 || x == lastX || y == lastY
                         || !clearsThreshold(data, y - 1, x)
                         || !clearsThreshold(data, y, x - 1)
                         || !clearsThreshold(data, y, x + 1)
                         || !clearsThreshold(data, y + 1, x)))
                        blob.Add(p);
                }
                // 2. Search all neighbouring pixels of the current neighbours list.
                foreach (Point ep in currentEdge)
                {
                    // 3. gets all (4 or 8) neighbouring pixels.
                    List<Point> neighbours = GetNeighbours(ep.X, ep.Y, lastX, lastY, allEightEdges);
                    foreach (Point p in neighbours)
                    {
                        Int32 x = p.X;
                        Int32 y = p.Y;
                        // 4. If the point is not already in the blob or in the new edge collection, and clears the threshold, add it to the new edge collection.
                        if (!inBlob[y, x] && !inNextEdge[y, x] && clearsThreshold(data, y, x))
                        {
                            nextEdge.Add(p);
                            inNextEdge[y, x] = true;
                        }
                    }
                }
                // 5. Replace edge collection contents with new edge collection.
                currentEdge.Clear();
                currentEdge.AddRange(nextEdge);
                nextEdge.Clear();
                Array.Clear(inNextEdge, 0, clearLen);
            }
            blobs.Add(blob);
            inBlobs.Add(inBlob);
        }

        /// <summary>
        /// Gets the list of neighbouring points around one point in an image.
        /// </summary>
        /// <param name="x">X-coordinate of the point to get neighbours of.</param>
        /// <param name="y">Y-coordinate of the point to get neighbours of.</param>
        /// <param name="lastX">Last valid X-coordinate on the image.</param>
        /// <param name="lastY">Last valid Y-coordinate on the image.</param>
        /// <param name="allEight">True to include diagonal neighbours.</param>
        /// <returns>The list of all valid neighbours around the given coordinate.</returns>
        private static List<Point> GetNeighbours(Int32 x, Int32 y, Int32 lastX, Int32 lastY, Boolean allEight)
        {
            // Init to max value top optimise list expand operations.
            List<Point> neighbours = new List<Point>(allEight ? 8 : 4);
            //Direct neighbours
            if (y > 0)
                neighbours.Add(new Point(x, y - 1));
            if (x > 0)
                neighbours.Add(new Point(x - 1, y));
            if (x < lastX)
                neighbours.Add(new Point(x + 1, y));
            if (y < lastY)
                neighbours.Add(new Point(x, y + 1));
            if (!allEight)
                return neighbours;
            // Diagonals.
            if (x > 0 && y > 0)
                neighbours.Add(new Point(x - 1, y - 1));
            if (x < lastX && y > 0)
                neighbours.Add(new Point(x + 1, y - 1));
            if (x > 0 && y < lastY)
                neighbours.Add(new Point(x - 1, y + 1));
            if (x < lastX && y < lastY)
                neighbours.Add(new Point(x + 1, y + 1));
            return neighbours;
        }

        public static Rectangle GetBlobBounds(List<Point> blob)
        {
            if (blob.Count == 0)
                return new Rectangle(0, 0, 0, 0);
            Int32 minX = Int32.MaxValue;
            Int32 maxX = 0;
            Int32 minY = Int32.MaxValue;
            Int32 maxY = 0;
            foreach (Point p in blob)
            {
                minX = Math.Min(minX, p.X);
                maxX = Math.Max(maxX, p.X);
                minY = Math.Min(minY, p.Y);
                maxY = Math.Max(maxY, p.Y);
            }
            return new Rectangle(minX, minY, maxX - minX + 1, maxY - minY + 1);
        }

        public static List<Point> GetBlobEdgePoints(List<Point> blob, Int32 imageWidth, Int32 imageHeight)
        {
            Boolean[,] pointInList = new Boolean[imageHeight, imageWidth];
            foreach (Point p in blob)
                pointInList[p.Y, p.X] = true;
            List<Point> edgePoints = new List<Point>();
            Int32 lastX = imageWidth - 1;
            Int32 lastY = imageHeight - 1;
            foreach (Point p in blob)
            {
                Int32 x = p.X;
                Int32 y = p.Y;
                // Image edge is obviously a blob edge too.
                if (x == 0 || y == 0 || x == lastX || y == lastY
                    || !pointInList[y - 1, x]
                    || !pointInList[y, x - 1]
                    || !pointInList[y, x + 1]
                    || !pointInList[y + 1, x])
                    edgePoints.Add(p);
            }
            return edgePoints;
        }
    }
}