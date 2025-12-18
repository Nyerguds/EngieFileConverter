using Nyerguds.Util;
using System;
using System.IO;
using Nyerguds.ImageManipulation;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using Nyerguds.GameData.Mythos;

namespace CnC64FileConverter.Domain.FileTypes
{
    public class FileFramesMythosVda : FileFramesMythosVgs
    {
        public override String ShortTypeName { get { return "Mythos Visage Animation"; } }
        public override String ShortTypeDescription { get { return "Mythos Visage Animation file"; } }
        public override String[] FileExtensions { get { return new String[] { "vda", "vdx" }; } }
        public override Boolean[] TransparencyMask { get { return null; } }

        public override void LoadFile(Byte[] fileData)
        {
            this.LoadFile(fileData, null);
        }

        public override FileFrames ChainLoadFiles(ref String[] fileNames, String originalPath)
        {
            // strategy: check original file for palette and start frame (meaning, no transparency and full 320x200).
            //-If it has no start frame, check files before it. Continue looking while the files have the same palette, and no start frame.
            // If palettes differ or no file with an actual start frame was found, return null.
            //-If it has a palette (or one with a palette was found by the logic above),
            // load more files after it, until the end or until finding another one with a palette.
            // If no frames without palette were found
            //-Change frameNames to the collection of actually used fules.

            return null;
        }

        public override void LoadFile(Byte[] fileData, String filename)
        {
            // Todo: chained loading to fill in missing start frames.
            // The principle is the same as the frames detection, only the logic
            // here specifically gives the start frame of the previous file with
            // the BuildVideo of the next one, and merges all frames.

            Boolean fromVdx = false;
            Byte[] vdaBytes = null;
            Byte[] vdxBytes = null;
            String vdaName = null;
            //String vdxName = null;
            if (filename != null)
            {
                if (filename.EndsWith(".VDX", StringComparison.InvariantCultureIgnoreCase))
                {
                    fromVdx = true;
                    vdaName = Path.Combine(Path.GetDirectoryName(filename), Path.GetFileNameWithoutExtension(filename) + ".VDA");
                    if (!File.Exists(vdaName))
                        throw new FileTypeLoadException("Can't load a video from .VDX file without a .VDA!");
                    vdxBytes = fileData;
                    vdaBytes = File.ReadAllBytes(vdaName);
                    //vdxName = filename;
                }
                else
                {
                    vdaName = filename;
                    String vdxName = Path.Combine(Path.GetDirectoryName(filename), Path.GetFileNameWithoutExtension(filename) + ".VDX");
                    vdaBytes = fileData;
                    if (File.Exists(vdxName))                        
                        vdxBytes = File.ReadAllBytes(vdxName);
                    //else
                    //    vdxName = null;
                }
            }
            else
            {
                if (this.CheckForVdx(fileData))
                    throw new FileTypeLoadException("Can't load a video from .VDX file without filename!");
                vdaBytes = fileData;
            }
            if (vdaName != null)
            {
                this.SetFileNames(vdaName);
                if (vdxBytes != null)
                    this.LoadedFileName += "/VDX";
            }
            List<Point> framesXY;
            this.LoadFromFileData(vdaBytes, vdaName, false, false, true, vdxBytes != null, out framesXY);
            this.m_Palette = PaletteUtils.ApplyTransparencyGuide(this.m_Palette, null);
            if (vdxBytes != null)
            {
                if (fromVdx)
                    this.ExtraInfo = "Loaded from VDX\n" + (this.ExtraInfo ?? String.Empty);
                List<SupportedFileType> framesList = this.BuildAnimation(vdaName, vdxBytes, this.m_FramesList, framesXY);
                FileFramesMythosVgs vgs = new FileFramesMythosVgs();
                this.m_FramesList = framesList;
                // this will be done later.
            }
        }

        private List<SupportedFileType> BuildAnimation(String sourcePath, Byte[] framesInfo, List<SupportedFileType> allChunks, List<Point> framesXY)
        {
            Boolean noStart = false;
            Boolean[] transMask = this.CreateTransparencyMask();
            List<SupportedFileType> framesList = new List<SupportedFileType>();
            Int32 offset = 0;
            Byte[] imageData = null;
            Int32 imageWidth = 320;
            Int32 imageHeight = 200;
            Byte[] imageDataBase = Enumerable.Repeat(TransparentIndex, imageWidth * imageHeight).ToArray();
            
            Int32 chunks = 0;
            while (offset + 2 <= framesInfo.Length)
            {
                UInt16 curVal = (UInt16)ArrayUtils.ReadIntFromByteArray(framesInfo, offset, 2, true);
                if (curVal == 0xFFFE)
                    break;
                if (curVal == 0xFFFF)
                {
                    if (imageData == null)
                    {
                        imageData = imageDataBase.ToArray();
                        noStart = true;
                    }
                    if (noStart && framesList.Count == 0)
                    {
                        PaletteUtils.ApplyTransparencyGuide(this.m_Palette, transMask);
                    }
                    Bitmap curImage = ImageUtils.BuildImage(imageData, imageWidth, imageHeight, imageWidth, PixelFormat.Format8bppIndexed, this.m_Palette, null);
                    FileImageFrame frame = new FileImageFrame();
                    frame.LoadFileFrame(this, this.ShortTypeName, curImage, sourcePath, framesList.Count);
                    frame.SetColorsInPalette(this.m_PaletteSet ? this.m_Palette.Length : 0);
                    frame.SetTransparencyMask(noStart ? transMask : null);
                    // diff images test: change back after tests.
                    //frame.SetTransparencyMask(framesList.Count > 0 ? transMask : null);
                    frame.SetColors(this.m_Palette, this);
                    frame.SetExtraInfo(CHUNKS + chunks);
                    framesList.Add(frame);
                    chunks = 0;
                    offset += 2;
                }
                else
                {
                    Int32 frameNumber = curVal & 0x7FFF;
                    if (allChunks.Count <= frameNumber)
                        throw new FileLoadException("Video frames file references more frames than available in graphics file!");
                    Int32 xOffset;
                    Int32 yOffset;
                    if ((curVal & 0x8000) != 0)
                    {
                        if (offset + 6 >= framesInfo.Length)
                            throw new FileLoadException("Illegal data order in video frames file!");
                        xOffset = (UInt16) ArrayUtils.ReadIntFromByteArray(framesInfo, offset + 2, 2, true);
                        yOffset = (UInt16) ArrayUtils.ReadIntFromByteArray(framesInfo, offset + 4, 2, true);
                        offset += 4;
                    }
                    else
                    {
                        if (framesXY.Count < frameNumber)
                            throw new FileLoadException("Video frames file references more frames than available in the graphics file!");
                        xOffset = framesXY[frameNumber].X;
                        yOffset = framesXY[frameNumber].Y;
                    }
                    Bitmap currentImage = allChunks[frameNumber].GetBitmap();
                    Int32 stride;
                    Int32 width = currentImage.Width;
                    Int32 height = currentImage.Height;
                    Byte[] currentFrameData = ImageUtils.GetImageData(currentImage, out stride);
                    if (imageData == null)
                    {
                        if (xOffset == 0 && yOffset == 0 && width == 320 && height == 200)
                        {
                            imageData = currentFrameData;
                            // To skip paint operation.
                            currentFrameData = null;
                        }
                        else
                        {
                            noStart = true;
                            imageData = imageDataBase.ToArray();
                        }
                    }
                    if (currentFrameData != null)
                    {
                        // diff images test: change back after tests.
                        //if (this.m_Palette[255].A != 0)
                        //    PaletteUtils.ApplyTransparencyGuide(this.m_Palette, transMask);
                        if (imageWidth < xOffset + width || imageHeight < yOffset + height)
                            throw new FileLoadException("Illegal data in video frames file: paint coordinates out of bounds!");
                        // diff images test: change back to imageData after tests.
                        //if (chunks == 0)
                        //    imageData = ImageUtils.PasteOn8bpp(imageDataBase, imageWidth, imageHeight, imageWidth, currentFrameData, width, height, stride, new Rectangle(xOffset, yOffset, width, height), transMask, false);
                        //else
                        ImageUtils.PasteOn8bpp(imageData, imageWidth, imageHeight, imageWidth, currentFrameData, width, height, stride, new Rectangle(xOffset, yOffset, width, height), transMask, true);
                    }
                    chunks++;
                    offset += 2;
                }
            }
            return framesList;
        }

        protected Boolean CheckForVdx(Byte[] fileData)
        {
            if (fileData.Length <= 8 || fileData.Length % 2 != 0)
                return false;
            UInt16 int0 = (UInt16)ArrayUtils.ReadIntFromByteArray(fileData, 0, 2, true);
            UInt16 int2 = (UInt16)ArrayUtils.ReadIntFromByteArray(fileData, 2, 2, true);
            UInt16 intl4 = (UInt16)ArrayUtils.ReadIntFromByteArray(fileData, fileData.Length - 4, 2, true);
            UInt16 intl2 = (UInt16)ArrayUtils.ReadIntFromByteArray(fileData, fileData.Length - 2, 2, true);
            if (int0 == 0 && int2 == 0xFFFF && intl4 == 0xFFFF && intl2 == 0xFFFE)
                return true;
            return false;
        }
        
        /// <summary>
        /// Saves the given file as this type.
        /// </summary>
        /// <param name="fileToSave">The input file to convert.</param>
        /// <param name="savePath">The path to save to.</param>
        /// <param name="saveOptions">Extra options for customising the save process. Request the list from GetSaveOptions.</param>
        public override void SaveAsThis(SupportedFileType fileToSave, String savePath, SaveOption[] saveOptions)
        {
            String vdaName;
            String vdxName;
            if (savePath.EndsWith(".VDX", StringComparison.InvariantCultureIgnoreCase))
            {
                vdaName = Path.Combine(Path.GetDirectoryName(savePath), Path.GetFileNameWithoutExtension(savePath) + ".VDA");
                vdxName = savePath;
            }
            else // No explicit check on VDA.
            {
                vdaName = savePath;
                vdxName = Path.Combine(Path.GetDirectoryName(savePath), Path.GetFileNameWithoutExtension(savePath) + ".VDX");
            }
            Byte[] vdxFile;
            Byte[] data = this.SaveToBytesAsThis(fileToSave, saveOptions, out vdxFile);
            File.WriteAllBytes(vdaName, data);
            if (vdxFile != null)
                File.WriteAllBytes(vdxName, vdxFile);
        }
        
        public override SaveOption[] GetSaveOptions(SupportedFileType fileToSave, String targetFileName)
        {
            if (fileToSave == null)
                return null;
            if (fileToSave.Frames.Length == 0)
                throw new NotSupportedException("This format needs at least one frame.");
            if (fileToSave.BitsPerPixel != 8)
                throw new NotSupportedException("This format needs 8bpp images.");
            Int32 firstFrameW = fileToSave.Frames[0].Width;
            Int32 firstFrameH = fileToSave.Frames[0].Height;
            foreach (SupportedFileType frame in fileToSave.Frames.Skip(1))
            {
                if (frame.Width != firstFrameW || frame.Height != firstFrameH)
                    throw new NotSupportedException("All frames should have the same dimensions.");
            }
            Int32 compression = 0;
            FileFramesMythosVgs fileVgs = fileToSave as FileFramesMythosVgs;
            if (fileVgs != null)
                compression = fileVgs.CompressionType;
            if (compression < 0 || compression > this.compressionTypes.Length)
                compression = 0;
            return new SaveOption[]
            {
                new SaveOption("OPT", SaveOptionType.Boolean, "Optimise to chunks", "1"),
                new SaveOption("CUT", SaveOptionType.Boolean, "Leave off the first frame (difference frames only)", "0"),
                new SaveOption("CMP", SaveOptionType.ChoicesList, "Compression type", String.Join(",", this.compressionTypes), compression.ToString()),
            };
        }

        public override Byte[] SaveToBytesAsThis(SupportedFileType fileToSave, SaveOption[] saveOptions)
        {
            Byte[] vdxFile;
            return SaveToBytesAsThis(fileToSave, saveOptions, out vdxFile);
        }

        public Byte[] SaveToBytesAsThis(SupportedFileType fileToSave, SaveOption[] saveOptions, out Byte[] vdxFile)
        {
            // todo check on frame count
            List<VideoChunk> chunks = new List<VideoChunk>();

            Boolean optimise = GeneralUtils.IsTrueValue(SaveOption.GetSaveOptionValue(saveOptions, "OPT"));
            Boolean cutfurstFrame = GeneralUtils.IsTrueValue(SaveOption.GetSaveOptionValue(saveOptions, "cut"));
            Int32 compressionType;
            Int32.TryParse(SaveOption.GetSaveOptionValue(saveOptions, "CMP"), out compressionType);
            if (compressionType < 0 || compressionType > 2)
                compressionType = 0;
            Bitmap origImage = fileToSave.Frames[0].GetBitmap();
            Int32 origWidth = origImage.Width;
            Int32 origHeight = origImage.Height;
            Int32 fullImageStride;
            Byte[] previousImageData = ImageUtils.GetImageData(origImage, out fullImageStride);
            Int32 previousImageWidth = origWidth;
            Int32 previousImageHeight = origHeight;
            Int32 previousImageStride = fullImageStride;
            List<UInt16> writeValues = new List<UInt16>();
            if (!cutfurstFrame)
            {
                chunks.Add(new VideoChunk() { ImageData = previousImageData, ImageRect = new Rectangle(0, 0, origWidth, origHeight) });
                writeValues.Add(0);
                writeValues.Add(0xFFFF);
            }
            foreach (SupportedFileType frame in fileToSave.Frames.Skip(1))
            {
                Int32 stride;
                Bitmap currentImage = frame.GetBitmap();
                Int32 width = currentImage.Width;
                Int32 height = currentImage.Height;
                Byte[] imageData = ImageUtils.GetImageData(currentImage, out stride);
                Byte[] imageDataOpt = imageData.ToArray();
                Int32 prevOffs = 0;
                Int32 frameOffs = 0;
                Int32 maxWidth = Math.Min(width, previousImageWidth);
                Int32 maxHeight = Math.Min(height, previousImageHeight);
                for (Int32 y = 0; y < maxHeight; y++)
                {
                    Int32 curFrameOffs = frameOffs;
                    Int32 curPrevOffs = prevOffs;
                    for (Int32 x = 0; x < maxWidth; x++)
                    {
                        if (imageData[curFrameOffs] == previousImageData[curPrevOffs])
                            imageDataOpt[curFrameOffs] = TransparentIndex;
                        curFrameOffs++;
                        curPrevOffs ++;
                    }
                    frameOffs += stride;
                    prevOffs += previousImageStride;
                }
                if (optimise)
                {
                    List<List<Point>> blobs = ImageUtils.FindBlobs(imageDataOpt, width, height, (bytes, y, x) => bytes[y * stride + x] != TransparentIndex, true, 0, true);

                    foreach (List<Point> blob in blobs)
                    {
                        Rectangle rect = ImageUtils.GetBlobBounds(blob);
                        Byte[] img = ImageUtils.CopyFrom8bpp(imageDataOpt, width, height, stride, rect);
                        VideoChunk chunk = new VideoChunk() {ImageData = img, ImageRect = rect};
                        Int32[] found = Enumerable.Range(0, chunks.Count).Where(c => chunk.Equals(chunks[c])).ToArray();
                        if (found.Length > 0)
                        {
                            UInt16 index = (UInt16) found[0];
                            Rectangle r = chunks[index].ImageRect;
                            if (r == rect)
                                writeValues.Add(index);
                            else
                            {
                                writeValues.Add((UInt16) (index | 0x8000));
                                writeValues.Add((UInt16) (rect.X));
                                writeValues.Add((UInt16) (rect.Y));
                            }
                        }
                        else
                        {
                            Int32 index = chunks.Count;
                            chunks.Add(chunk);
                            writeValues.Add((UInt16) index);
                        }
                    }
                }
                else
                {
                    // optimize diff frame by cropping it.
                    Int32 index = chunks.Count;
                    Int32 xOffset = 0;
                    Int32 yOffset = 0;
                    Int32 newWidth = width;
                    Int32 newHeight = height;
                    imageDataOpt = ImageUtils.CollapseStride(imageDataOpt, newWidth, newHeight, 8, ref stride);
                    imageDataOpt = ImageUtils.OptimizeXWidth(imageDataOpt, ref newWidth, newHeight, ref xOffset, true, TransparentIndex, 0xFF, true);
                    imageDataOpt = ImageUtils.OptimizeYHeight(imageDataOpt, newWidth, ref newHeight, ref yOffset, true, TransparentIndex, 0xFFFF, true);
                    chunks.Add(new VideoChunk() {ImageData = imageDataOpt, ImageRect = new Rectangle(xOffset, yOffset, newWidth, newHeight)});
                    writeValues.Add((UInt16) index);
                }
                writeValues.Add(0xFFFF);
                previousImageData = imageData;
                previousImageWidth = width;
                previousImageHeight = height;
                previousImageStride = stride;
            }
            vdxFile = new Byte[writeValues.Count * 2];
            for (Int32 i = 0; i < writeValues.Count; i++)
                ArrayUtils.WriteIntToByteArray(vdxFile, i << 1, 2, true, writeValues[i]);
            // Compress chunks
            foreach (VideoChunk chunk in chunks)
            {
                Byte[] compressedBytes = null;
                if (compressionType > 0)
                {
                    MythosCompression mc = new MythosCompression();
                    if (compressionType == 1)
                        compressedBytes = mc.FlagRleEncode(chunk.ImageData, 0xFE, chunk.ImageRect.Width, 8);
                    else if (compressionType == 2)
                        compressedBytes = mc.CollapsedTransparencyEncode(chunk.ImageData, TransparentIndex, chunk.ImageRect.Width, 8);
                }
                /*/
                // Debug code to trace failed compression.
                if (compressedBytes != null)
                {
                    MythosCompression mc = new MythosCompression();
                    Byte[] imageData = null;
                    if (compressionType == 1)
                        imageData = mc.FlagRleDecode(compressedBytes, null, null, chunk.ImageData.Length, true);
                    else if (compressionType == 2)
                        imageData = mc.CollapsedTransparencyDecode(compressedBytes, null, null, chunk.ImageData.Length, chunk.ImageRect.Width, TransparentIndex, true);
                    if (imageData == null)
                    {
                    }
                }
                //*/
                if (compressedBytes != null && compressedBytes.Length < chunk.ImageData.Length)
                {
                    chunk.ImageData = compressedBytes;
                    chunk.Compressed = true;
                }
            }
            // Add palette
            FileFramesMythosPal pal = new FileFramesMythosPal();
            Byte[] palData = pal.SaveToBytesAsThis(fileToSave, null);

            // Full length: headers and data for all chunks.
            Int32 fullLength = palData.Length + chunks.Count * 0x08 + chunks.Sum(x => x.ImageData.Length);
            Byte[] vdmFile = new Byte[fullLength];
            Array.Copy(palData, 0, vdmFile, 0, palData.Length);
            Int32 offset = palData.Length;
            foreach (VideoChunk chunk in chunks)
            {
                ArrayUtils.WriteIntToByteArray(vdmFile, offset + 0, 2, true, (UInt16) (chunk.ImageRect.Width - 1));
                ArrayUtils.WriteIntToByteArray(vdmFile, offset + 2, 2, true, (UInt16) (chunk.ImageRect.Height - 1));
                vdmFile[offset + 4] = (Byte) (chunk.Compressed ? 0x02 : 0x00);
                ArrayUtils.WriteIntToByteArray(vdmFile, offset + 5, 2, true, (UInt16)(chunk.ImageRect.X));
                vdmFile[offset + 7] = (Byte) (chunk.ImageRect.Y & 0xFF);
                offset += 8;
                Byte[] chunkData = chunk.ImageData;
                Int32 dataLen = chunkData.Length;
                Array.Copy(chunkData, 0, vdmFile, offset, dataLen);
                offset += dataLen;
            }
            return vdmFile;
        }

        private class VideoChunk: IEqualityComparer<VideoChunk>
        {
            public Byte[] ImageData { get; set; }
            public Rectangle ImageRect { get; set; }
            public Boolean Compressed { get; set; }

            public Boolean Equals(VideoChunk x, VideoChunk y)
            {
                if (x == null)
                    return y == null;
                if (y == null)
                    return false;
                return x.ImageRect.Width == y.ImageRect.Width && x.ImageRect.Height == y.ImageRect.Height && x.ImageData.SequenceEqual(y.ImageData);
            }

            public Int32 GetHashCode(VideoChunk obj)
            {
                Byte[] imageBytes = new Byte[ImageData.Length + 8];
                ArrayUtils.WriteIntToByteArray(imageBytes, 0, 4, true, (UInt32) ImageRect.Width);
                ArrayUtils.WriteIntToByteArray(imageBytes, 4, 4, true, (UInt32) ImageRect.Height);
                return (Int32)Crc32.ComputeChecksum(imageBytes);
            }

            public override Boolean Equals(Object obj)
            {
                VideoChunk objVc = obj as VideoChunk;
                return objVc != null && this.Equals(this, objVc);
            }

            public override Int32 GetHashCode()
            {
                return this.GetHashCode(this);
            }
        }
    }
}