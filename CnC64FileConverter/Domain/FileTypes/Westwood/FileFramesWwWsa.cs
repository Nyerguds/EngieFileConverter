using Nyerguds.ImageManipulation;
using Nyerguds.Util;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using Nyerguds.GameData.Westwood;

namespace CnC64FileConverter.Domain.FileTypes
{

    public class FileFramesWwWsa : SupportedFileType
    {

        public override FileClass FileClass { get { return FileClass.FrameSet; } }
        public override FileClass InputFileClass { get { return FileClass.FrameSet; } }
        public override FileClass FrameInputFileClass { get { return FileClass.Image8Bit; } }
        protected SupportedFileType[] m_FramesList;

        public override Int32 Width { get { return m_Width; }  }
        public override Int32 Height { get { return m_Height; } }
        protected Int32 m_Width;
        protected Int32 m_Height;
        protected String[] formats = new String[] { "Dune II", "C&C", "Monopoly" };
        /// <summary>Very short code name for this type.</summary>
        public override String ShortTypeName { get { return "Westwood WSA"; } }
        public override String[] FileExtensions { get { return new String[] { "wsa" }; } }
        public override String ShortTypeDescription { get { return "Westwood Animation File"; } }
        public override Int32 ColorsInPalette { get { return this.m_HasPalette? 0x100 : 0; } }
        public override Int32 BitsPerColor { get { return 8; } }
        protected Boolean m_HasPalette;
        protected WsaVersion m_Version = WsaVersion.Cnc;
        protected Boolean m_HasLoopFrame;
        protected Boolean m_DamagedLoopFrame;
        protected Boolean m_Continues;

        /// <summary>Retrieves the sub-frames inside this file.</summary>
        public override SupportedFileType[] Frames { get { return m_FramesList; } }
        /// <summary>See this as nothing but a container for frames, as opposed to a file that just has the ability to visualize its data as frames. Types with frames where this is set to false wil not get an index -1 in the frames list.</summary>
        public override Boolean IsFramesContainer { get { return true; } }
        /// <summary> This is a container-type that builds a full image from its frames to show on the UI, which means this type can be used as single-image source.</summary>
        public override Boolean HasCompositeFrame { get { return false; } }
        
        public override SaveOption[] GetSaveOptions(SupportedFileType fileToSave, String targetFileName)
        {
            // If it is a non-image format which does contain colours, offer to save with palette
            Boolean hasColors = fileToSave != null && !(fileToSave is FileImage) && fileToSave.ColorsInPalette != 0;
            WsaVersion type = WsaVersion.Cnc;
            Boolean loop = true;
            Boolean trim = true;
            Boolean continues = false;
            Boolean ignoreLast = false;
            if (fileToSave is FileFramesWwWsa)
            {
                FileFramesWwWsa toSave = (FileFramesWwWsa)fileToSave;
                type = toSave.m_Version;
                loop = toSave.m_HasLoopFrame;
                if (type == WsaVersion.Dune2)
                    trim = false;
                continues = toSave.m_Continues;
                ignoreLast = toSave.m_DamagedLoopFrame;
            }
            SaveOption[] opts = new SaveOption[ignoreLast ? 6 : 5];
            opts[0] = new SaveOption("PAL", SaveOptionType.Boolean, "Include palette", hasColors ? "1" : "0");
            opts[1] = new SaveOption("TYPE", SaveOptionType.ChoicesList, "Type", String.Join(",", formats), ((Int32)type).ToString());
            opts[2] = new SaveOption("LOOP", SaveOptionType.Boolean, "Loop", null, loop ? "1" : "0");
            opts[3] = new SaveOption("CONT", SaveOptionType.Boolean, "Don't save initial frame", null, continues ? "1" : "0");
            opts[4] = new SaveOption("CROP", SaveOptionType.Boolean, "Crop to X and Y offsets (C&C type only)", null, trim ? "1" : "0");
            if (ignoreLast)
                opts[5] = new SaveOption("CUT", SaveOptionType.Boolean, "Ignore input loop frame", String.Join(",", formats), "1");
            return opts;
        }

        public override void LoadFile(Byte[] fileData)
        {
            LoadFromFileData(fileData, null);
        }

        public override void LoadFile(String filename)
        {
            Byte[] fileData = File.ReadAllBytes(filename);
            LoadFromFileData(fileData, filename);
            SetFileNames(filename);
        }

        protected void LoadFromFileData(Byte[] fileData, String sourcePath)
        {
            Int32 datalen = fileData.Length;
            if (datalen < 14)
                throw new FileTypeLoadException("Bad header size.");
            UInt16 nrOfFrames = (UInt16)ArrayUtils.ReadIntFromByteArray(fileData, 0, 2, true);
            UInt16 xPos = (UInt16)ArrayUtils.ReadIntFromByteArray(fileData, 2, 2, true);
            UInt16 yPos = (UInt16)ArrayUtils.ReadIntFromByteArray(fileData, 4, 2, true);
            UInt16 width = (UInt16)ArrayUtils.ReadIntFromByteArray(fileData, 6, 2, true);
            UInt16 height = (UInt16)ArrayUtils.ReadIntFromByteArray(fileData, 8, 2, true);
            Int32 hdrOffset = 10;
            Int32 deltaLen = 2;
            // If the type is Dune 2, the "width" value actually contains the buffer size, so it's practically impossible this is below 320.
            if ((width > 320 || height > 200 || width == 0 || height == 0) && xPos != 0 && yPos != 0)
            {
                width = xPos;
                height = yPos;
                xPos=0;
                yPos=0;
                hdrOffset -= 4;
                m_Version = WsaVersion.Dune2;
            }
            else if (width <= 320 && height <= 200)
            {
                m_Version = WsaVersion.Cnc;
            }
            else
            {
                //m_Version = WsaVersion.Poly;
                //deltaLen = 4;
                throw new FileTypeLoadException("Invalid image dimensions!");
            }
            String generalInfo = "Version: " + m_Version;
            if (m_Version != WsaVersion.Dune2)
                generalInfo += "\nX-offset = " + xPos + "\nY-offset = " + yPos;
            this.ExtraInfo = generalInfo;
            if (width == 0 || height == 0)
                throw new FileTypeLoadException("Invalid image dimensions!");
            m_Width = width;
            m_Height = height;

            UInt32 deltaBufferSize = ArrayUtils.ReadIntFromByteArray(fileData, hdrOffset, deltaLen, true);
            UInt16 flags = (UInt16)ArrayUtils.ReadIntFromByteArray(fileData, hdrOffset + deltaLen, 2, true);
            Int32 dataIndexOffset = hdrOffset + 2 + deltaLen;
            Int32 dataOffs = dataIndexOffset + (nrOfFrames + 2) * 4;
            this.m_HasPalette = (flags & 1) == 1;
            if (this.m_HasPalette)
            {
                if (fileData.Length < dataOffs + 0x300)
                    throw new FileTypeLoadException("File is not longe enough for color palette!");
                Byte[] pal = new Byte[0x300];
                Array.Copy(fileData, dataOffs, pal, 0, 0x300);
                try
                {
                    this.m_Palette = ColorUtils.GetEightBitColorPalette(ColorUtils.ReadSixBitPalette(pal));
                }
                catch (NotSupportedException e)
                {
                    throw new FileTypeLoadException("Error loading color palette: " + e.Message, e);
                }
            }
            if (this.m_Palette == null)
                this.m_Palette = PaletteUtils.GenerateGrayPalette(8, null, false);
            m_FramesList = new SupportedFileType[nrOfFrames + 1];
            Byte[] frameData = new Byte[width * height];
            Byte[] frame0Data = new Byte[width * height];
            Byte[] xorData = new Byte[deltaBufferSize + 37];
            m_HasLoopFrame = false;
            for (Int32 i = 0; i < nrOfFrames + 2; i++)
            {
                String specificInfo = String.Empty;
                UInt32 frameOffset = ArrayUtils.ReadIntFromByteArray(fileData, dataIndexOffset, 4, true);
                dataIndexOffset += 4;
                UInt32 frameOffsetReal = frameOffset;
                if (this.m_HasPalette)
                    frameOffsetReal += 0x300;

                if (i == nrOfFrames + 1)
                {
                    m_HasLoopFrame = frameOffset != 0;
                    break;
                }
                if (frameOffset == 0 || frameOffsetReal == fileData.Length)
                {
                    if (frameOffset == 0 && i == 0)
                    {
                        m_Continues = true;
                        Array.Clear(frameData, 0, frameData.Length);
                        specificInfo = "\nContinues from a previous file";
                        this.ExtraInfo += specificInfo;
                    }
                    else
                        continue;
                }
                else
                {
                    Int32 refOff = (Int32)frameOffsetReal;
                    try
                    {
                        Int32 uncLen = WWCompression.LcwUncompress(fileData, ref refOff, xorData);
                        WWCompression.ApplyXorDelta(frameData, xorData, uncLen);
                    }
                    catch (Exception ex)
                    {
                        throw new FileTypeLoadException("LCW Decompression failed: " + ex.Message, ex);
                    }
                    if (i == 0)
                        Array.Copy(frameData, frame0Data, frameData.Length);
                }
                Byte[] finalFrameData = frameData;
                Int32 finalWidth = width + xPos;
                Int32 finalHeight = height + yPos;
                if (xPos > 0 || yPos > 0)
                {
                    finalFrameData = ImageUtils.Change8BitStride(frameData, width, height, finalWidth, true, 0);
                    finalFrameData = ImageUtils.ChangeHeight(finalFrameData, finalWidth, height, finalHeight, true, 0);
                }
                Bitmap curFrImg = ImageUtils.BuildImage(finalFrameData, finalWidth, finalHeight, finalWidth, PixelFormat.Format8bppIndexed, this.m_Palette, null);
                FileImageFrame frame = new FileImageFrame();
                frame.LoadFileFrame(this, this.ShortTypeName, curFrImg, sourcePath, i);
                frame.SetBitsPerColor(this.BitsPerColor);
                frame.SetColorsInPalette(this.ColorsInPalette);
                frame.SetExtraInfo(generalInfo + specificInfo);
                this.m_FramesList[i] = frame;
            }
            m_DamagedLoopFrame = false;
            if (m_HasLoopFrame)
            {
                m_DamagedLoopFrame = !frameData.SequenceEqual(frame0Data);
                this.ExtraInfo += "\nHas loop frame";
                if (m_DamagedLoopFrame)
                    this.ExtraInfo += " (but doesn't match)";
            }
            if (!m_DamagedLoopFrame)
            {
                SupportedFileType[] newFramesList = new SupportedFileType[nrOfFrames];
                Array.Copy(m_FramesList, newFramesList, nrOfFrames);
                m_FramesList = newFramesList;
            }
            else
            {
                FileImageFrame frame = (FileImageFrame)this.m_FramesList[nrOfFrames];
                frame.SetExtraInfo(frame.ExtraInfo + "\nLoop frame (damaged?)");
            }
            if (this.m_FramesList.Length == 1)
                this.m_LoadedImage = m_FramesList[0].GetBitmap();
        }

        public override Byte[] SaveToBytesAsThis(SupportedFileType fileToSave, SaveOption[] saveOptions)
        {
            // Preliminary checks
            if (!fileToSave.IsFramesContainer || fileToSave.Frames == null)
            {
                FileFrames frameSave = new FileFrames();
                frameSave.AddFrame(fileToSave);
                fileToSave = frameSave;
            }
            if (fileToSave.Frames.Length == 0)
                throw new NotSupportedException("No frames found in source data!");
            Int32 width = -1;
            Int32 height = -1;
            Color[] palette = null;
            foreach (SupportedFileType frame in fileToSave.Frames)
            {
                if (frame == null)
                    throw new NotSupportedException("WSA can't handle empty frames!");
                if (frame.BitsPerColor != 8)
                    throw new NotSupportedException("Not all frames in input type are 8-bit images!");
                if (width == -1 && height == -1)
                {
                    width = frame.Width;
                    height = frame.Height;
                }
                else if (width != frame.Width || height != frame.Height)
                    throw new NotSupportedException("Not all frames in input type are the same size!");
                if (palette == null)
                    palette = frame.GetColors();
            }
            // Save options
            Boolean asPaletted = GeneralUtils.IsTrueValue(SaveOption.GetSaveOptionValue(saveOptions, "PAL"));
            Int32 type;
            WsaVersion saveType;
            if (!Int32.TryParse(SaveOption.GetSaveOptionValue(saveOptions, "TYPE"), out type) || !Enum.IsDefined(typeof (WsaVersion), type))
                saveType = WsaVersion.Cnc;
            else
                saveType = (WsaVersion)type;
            Boolean loop = GeneralUtils.IsTrueValue(SaveOption.GetSaveOptionValue(saveOptions, "LOOP"));
            Boolean crop = saveType != WsaVersion.Dune2 && GeneralUtils.IsTrueValue(SaveOption.GetSaveOptionValue(saveOptions, "CROP"));
            Boolean cut = GeneralUtils.IsTrueValue(SaveOption.GetSaveOptionValue(saveOptions, "CUT"));
            Boolean continues = GeneralUtils.IsTrueValue(SaveOption.GetSaveOptionValue(saveOptions, "CONT"));


            // Fetch and compress data
            Int32 readFrames = fileToSave.Frames.Length;
            Int32 writeFrames = readFrames;
            if (cut)
            {
                readFrames--;
                writeFrames--;
            }
            // Technically can't happen... I think.
            if (readFrames == 0)
                throw new NotSupportedException("No frames in source data!");
            if (loop)
                writeFrames++;
            Byte[][] framesDataUnc = new Byte[writeFrames][];
            Byte[] firstFrameData = continues ? new Byte[width * height] : null;
            for (Int32 i = 0; i < writeFrames; i++)
            {
                Byte[] frameDataRaw;
                if (i < readFrames)
                {
                    Bitmap bm = fileToSave.Frames[i].GetBitmap();
                    Int32 stride;
                    frameDataRaw = ImageUtils.GetImageData(bm, out stride);
                    frameDataRaw = ImageUtils.CollapseStride(frameDataRaw, width, height, 8, ref stride);
                }
                else
                    frameDataRaw = firstFrameData;
                if (firstFrameData == null)
                    firstFrameData = frameDataRaw;
                framesDataUnc[i] = frameDataRaw;
            }
            // crop logic.
            Int32 xOffset = 0;
            Int32 yOffset = 0;
            if (crop)
            {
                Int32 minYTrimStart = Int32.MaxValue;
                Int32 maxYTrimEnd = 0;
                Int32 minXTrimStart = Int32.MaxValue;
                Int32 maxXTrimEnd = 0;
                // No need to process the final frame if it's a copy of the first.
                Int32 checkEnd = writeFrames;
                if (loop)
                    checkEnd--;
                for (Int32 i = 0; i < checkEnd; i++)
                {
                    Int32 yOffs = 0;
                    Int32 trHeight = height;
                    ImageUtils.OptimizeYHeight(framesDataUnc[i], width, ref trHeight, ref yOffs, true, 0, 0xFFFF);
                    minYTrimStart = Math.Min(minYTrimStart, yOffs);
                    maxYTrimEnd = Math.Max(maxYTrimEnd, yOffs + trHeight);

                    Int32 trWidth = width;
                    Int32 xOffs = 0;
                    ImageUtils.OptimizeXWidth(framesDataUnc[i], ref trWidth, trHeight, ref xOffs, true, 0, 0xFFFF);
                    minXTrimStart = Math.Min(minXTrimStart, xOffs);
                    maxXTrimEnd = Math.Max(maxXTrimEnd, xOffs + trWidth);
                }
                if ((minYTrimStart != Int32.MaxValue && maxYTrimEnd != 0 && minXTrimStart != Int32.MaxValue && maxXTrimEnd != 0)
                    && (minXTrimStart != 0 || maxXTrimEnd != width || minYTrimStart != 0 || maxYTrimEnd != height))
                {                    
                    xOffset = minXTrimStart;
                    yOffset = minYTrimStart;
                    Int32 newWidth = maxXTrimEnd - xOffset;
                    Int32 newheight = maxYTrimEnd - yOffset;
                    for (Int32 i = 0; i < writeFrames; i++)
                    {
                        Int32 outStride;
                        framesDataUnc[i] = ImageUtils.CopyFrom8bpp(framesDataUnc[i], width, height, width, out outStride, new Rectangle(xOffset, yOffset, newWidth, newheight));
                    }
                    width = newWidth;
                    height = newheight;
                }
            }
            Byte[][] framesData = new Byte[writeFrames][];
            Int32 deltaBufferSize = 0;
            Byte[] previousFrame = new Byte[width*height];
            for (Int32 i = 0; i < writeFrames; i++)
            {
                Byte[] frameData = WWCompression.GenerateXorDelta(framesDataUnc[i], previousFrame);
                deltaBufferSize = Math.Max(frameData.Length, deltaBufferSize);
                frameData = WWCompression.LcwCompress(frameData);
                framesData[i] = frameData;
                previousFrame = framesDataUnc[i];
            }
            // To ensure the file size is correct
            if (continues && framesData.Length > 0)
                framesData[0] = new Byte[0];
            // I dunno lol just following specs.
            deltaBufferSize = Math.Max(0, deltaBufferSize - 37);
            Int32 headerSize = 14;
            if (saveType == WsaVersion.Dune2)
                headerSize -= 4;
            Int32 indexSize = (readFrames + 2) * 4;
            Int32 paletteSize = asPaletted ? 0x300 : 0;
            Int32 dataSize = framesData.Sum(x => x.Length);
            Int32 fileSize = headerSize + indexSize + paletteSize + dataSize;
            Byte[] fileData = new Byte[fileSize];
            Int32 curOffs = headerSize + indexSize;
            Int32[] frameOffsets = new Int32[readFrames + 2];
            // Initial offset. Set to 0 if there is no first frame.
            frameOffsets[0] = continues ? 0 : curOffs;
            for (Int32 i = 0; i < writeFrames; i++)
            {
                curOffs += framesData[i].Length;
                frameOffsets[i + 1] = curOffs;
            }
            // Write header
            Int32 offset = 0;
            ArrayUtils.WriteIntToByteArray(fileData, offset, 2, true, (UInt32)readFrames);
            offset += 2;
            if (saveType == WsaVersion.Cnc)
            {
                ArrayUtils.WriteIntToByteArray(fileData, offset, 2, true, (UInt32)xOffset);
                offset += 2;
                ArrayUtils.WriteIntToByteArray(fileData, offset, 2, true, (UInt32)yOffset);
                offset += 2;
            }
            ArrayUtils.WriteIntToByteArray(fileData, offset, 2, true, (UInt32)width);
            offset += 2;
            ArrayUtils.WriteIntToByteArray(fileData, offset, 2, true, (UInt32)height);
            offset += 2;
            ArrayUtils.WriteIntToByteArray(fileData, offset, 2, true, (UInt32)deltaBufferSize);
            offset += 2;
            ArrayUtils.WriteIntToByteArray(fileData, offset, 2, true, (UInt32)(asPaletted ? 1 : 0));
            offset += 2;
            foreach (Int32 frOffs in frameOffsets)
            {
                ArrayUtils.WriteIntToByteArray(fileData, offset, 4, true, (UInt32)frOffs);
                offset += 4;
            }
            if (asPaletted)
            {
                ColorSixBit[] sixBitPal = ColorUtils.GetSixBitColorPalette(palette);
                Byte[] sixBitPalBytes = ColorUtils.GetSixBitPaletteData(sixBitPal);
                Array.Copy(sixBitPalBytes, 0, fileData, offset, Math.Min(0x300, sixBitPalBytes.Length));
                offset += 0x300;
            }
            foreach (Byte[] frame in framesData)
            {
                Array.Copy(frame, 0, fileData, offset, frame.Length);
                offset += frame.Length;
            }
            return fileData;
        }
    }

    public enum WsaVersion
    {
        Dune2 = 0,
        Cnc = 1,
        Poly=2,
    }
}