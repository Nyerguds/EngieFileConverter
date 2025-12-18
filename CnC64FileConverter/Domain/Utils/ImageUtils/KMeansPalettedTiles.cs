using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using Nyerguds.Util;
using SimplePaletteQuantizer.Helpers;
using SimplePaletteQuantizer.Quantizers;
using SimplePaletteQuantizer.Quantizers.XiaolinWu;

namespace Nyerguds.ImageManipulation
{
    /// <summary>
    /// TData = Byte[], byte data for one tile. Easiest way to make the images to color-quantize.
    /// Uavg = Color[], as 16-colour palette
    /// Approach method is deviation from palette.
    /// </summary>
    public class KMeansPalettedTiles : KMeans<Byte[], Color[]>
    {
        private Byte[][] m_Frames;
        private Int32 m_FramesWidth;
        private Int32 m_FramesHeight;
        private Int32 m_singleTileSize;
        private Color[] m_palette;
        private IColorQuantizer quantizer;

        public Int32[] DoClustering(Int32 numClusters, out Color[][] means)
        {
            return this.Cluster(this.m_Frames, numClusters, out means);
        }

        public KMeansPalettedTiles(Byte[][] rawData, Int32 framesWidth, Int32 framesHeight, Color[] palette)
        {
            this.m_Frames = rawData;
            this.m_FramesWidth = framesWidth;
            this.m_FramesHeight = framesHeight;
            this.m_singleTileSize = this.m_FramesWidth * this.m_FramesHeight;
            this.m_palette = palette;
            this.quantizer = new WuColorQuantizer();
        }

        protected override void ClearMeans(Color[][] means, Int32 clusterNumber)
        {
            means[clusterNumber] = new Color[16];
        }

        /// <summary>
        /// Calculate palette for cluster
        /// </summary>
        /// <param name="subData"></param>
        /// <returns></returns>
        protected override Color[] CalculateClusterAverage(Byte[][] subData)
        {
            Int32 fullImageHeight = subData.Length * this.m_FramesHeight;
            Int32 fullImageWidth = this.m_FramesWidth;
            // Make full image
            Byte[] clusterImageData = new Byte[fullImageWidth * fullImageHeight];
            for (Int32 i = 0; i < subData.Length; i++)
                Array.Copy(subData[i], 0, clusterImageData, m_singleTileSize * i, m_singleTileSize);
            Color[] pal;
            using (Bitmap clusterImage = ImageUtils.BuildImage(clusterImageData, fullImageWidth, fullImageHeight, fullImageWidth, PixelFormat.Format8bppIndexed, this.m_palette, Color.Black))
            {
                ImageBuffer ib = new ImageBuffer(clusterImage, ImageLockMode.ReadOnly);
                pal = ib.SynthetizePalette(this.quantizer, 16, 1).ToArray();
            }
            return pal;
        }

        /// <summary>
        /// Calculate distance of picture to palette.
        /// </summary>
        /// <param name="dataEntry">Data entry to check</param>
        /// <param name="mean">Colour palette to approach</param>
        /// <returns>The distance of the image colours to the palette.</returns>
        protected override Double CalculateDistance(Byte[] dataEntry, Color[] mean)
        {
            Double[] matchedDataDistance = new Double[mean.Length];
            Int32[] matchedDataCount = new Int32[mean.Length];
            // Only do this per colour, not per pixel, or the most common colours will completely outweigh the less common ones.
            Byte[] dataColors = dataEntry.Distinct().ToArray();
            foreach (Byte col in dataColors)
            {
                // 1. Get closest match. Not gonna reference existing palette match function since I'll get the distance out of this straight away.
                Int32 colorMatch = 0;
                Int32 leastDistance = Int32.MaxValue;
                Int32 red = this.m_palette[col].R;
                Int32 green = this.m_palette[col].G;
                Int32 blue = this.m_palette[col].B;
                for (Int32 j = 0; j < mean.Length; j++)
                {
                    Color paletteColor = mean[j];
                    Int32 redDistance = paletteColor.R - red;
                    Int32 greenDistance = paletteColor.G - green;
                    Int32 blueDistance = paletteColor.B - blue;
                    Int32 distance = (redDistance * redDistance) + (greenDistance * greenDistance) + (blueDistance * blueDistance);
                    if (distance >= leastDistance)
                        continue;
                    colorMatch = j;
                    leastDistance = distance;
                    if (distance == 0)
                        break;
                }
                // Add distance into array and increase counter, so averages can be made at the end.
                matchedDataDistance[colorMatch] += Math.Sqrt(leastDistance);
                matchedDataCount[colorMatch]++;
            }
            Double totalDistance = 0;
            for (Int32 i = 0; i < mean.Length; i++)
            {
                Int32 count = matchedDataCount[i];
                if (count > 1)
                    matchedDataDistance[i] /= count;
                totalDistance += matchedDataDistance[i];
            }
            // Technically should be divided by mean.Length, but that'll be the same every time so it'll just add needless processing.
            return totalDistance;
        }
    }
}