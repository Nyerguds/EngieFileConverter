using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Nyerguds.GameData.Westwood;
using Nyerguds.ImageManipulation;
using Nyerguds.Util;

namespace CnC64FileConverter.Domain.FileTypes
{
    public class FileFramesWwShpCc : SupportedFileType
    {
        public override FileClass FileClass { get { return FileClass.FrameSet; } }
        public override FileClass InputFileClass { get { return FileClass.FrameSet; } }
        public override FileClass FrameInputFileClass { get { return FileClass.Image8Bit; } }
        protected SupportedFileType[] m_FramesList;

        public override Int32 Width { get { return this.m_Width; } }
        public override Int32 Height { get { return this.m_Height; } }
        protected Int32 m_Width;
        protected Int32 m_Height;
        //protected String[] formats = new String[] { "Dune II", "Legend of Kyrandia", "C&C1/RA1", "Tiberian Sun" };
        /// <summary>Very short code name for this type.</summary>
        public override String ShortTypeName { get { return "Westwood Shape"; } }
        public override String[] FileExtensions { get { return new String[] { "shp" }; } }
        public override String ShortTypeDescription { get { return "Westwood Shape File"; } }
        public override Int32 ColorsInPalette { get { return this.m_HasPalette ? 0x100 : 0; } }
        public override Int32 BitsPerPixel { get { return 8; } }
        protected Boolean m_HasPalette;
        protected ShpVersion m_Version = ShpVersion.Cnc;

        /// <summary>Retrieves the sub-frames inside this file.</summary>
        public override SupportedFileType[] Frames { get { return this.m_FramesList; } }
        /// <summary>See this as nothing but a container for frames, as opposed to a file that just has the ability to visualize its data as frames. Types with frames where this is set to false wil not get an index -1 in the frames list.</summary>
        public override Boolean IsFramesContainer { get { return true; } }
        /// <summary> This is a container-type that builds a full image from its frames to show on the UI, which means this type can be used as single-image source.</summary>
        public override Boolean HasCompositeFrame { get { return false; } }
        /// <summary>Array of Booleans which defines for the palette which indices are transparent.</summary>
        public override Boolean[] TransparencyMask { get { return new Boolean[] {true}; } }

        public override SaveOption[] GetSaveOptions(SupportedFileType fileToSave, String targetFileName)
        {
            // This code can pre-check whether all frames are non-null and the same dimensions.
            // Might want to start doing this kind of pre-checks for more types; this is actually the perfect time to do them.
            /*/
            ShpVersion type = ShpVersion.Cnc;
            Boolean d2type = false;
            Boolean splitshadows = false;
            FileFramesWwShpCc toSave = fileToSave as FileFramesWwShpCc;
            if (toSave != null)
            {
                type = toSave.m_Version;
                if (type == ShpVersion.Dune2)
                    d2type = true;
                splitshadows = type == ShpVersion.Ts;
            }
            //*/
            SaveOption[] opts = new SaveOption[0];
            //opts[0] = new SaveOption("TYPE", SaveOptionType.ChoicesList, "Type", String.Join(",", formats), ((Int32)type).ToString());
            //This will depend on input, not options.
            //opts[1] = new SaveOption("INDFR", SaveOptionType.Boolean, "Individual frame sizes (Dune only)", null, d2type ? "1" : "0");
            //opts[1] = new SaveOption("REMAP", SaveOptionType.Boolean, "Remap sections (Dune only)", null, d2type ? "1" : "0");
            //opts[2] = new SaveOption("SPLIT", SaveOptionType.Boolean, "Split shadows into separate frames (TS only)", null, splitshadows ? "1" : "0");
            return opts;
        }

        public override void LoadFile(Byte[] fileData)
        {
            this.LoadFromFileData(fileData, null);
        }

        public override void LoadFile(Byte[] fileData, String filename)
        {
            this.LoadFromFileData(fileData, filename);
            this.SetFileNames(filename);
        }
        
        protected void LoadFromFileData(Byte[] fileData, String sourcePath)
        {
            // Loop over all versions.
            ShpVersion[] versions = Enum.GetValues(typeof(ShpVersion)).Cast<ShpVersion>().ToArray();
            Int32 lenmax = versions.Length - 1;
            for (Int32 i = 0; i <= lenmax; i++)
            {
                try
                {
                    this.LoadFromFileData(fileData, sourcePath, versions[i]);
                    break;
                }
                catch (HeaderParseException)
                {
                    // Only catches the specific header file size check. If there are more items in the enum,
                    // continue the detection process. If it's the last one, just throw the exception.
                    // It subclasses FileTypeLoadException, so the global autodetect process will catch it.
                    if (i == lenmax)
                        throw;
                }
            }
        }

        protected void LoadFromFileData(Byte[] fileData, String sourcePath, ShpVersion loadVersion)
        {
            // Throw a HeaderParseException from the moment it's detected as a specific type that's not the requested one.
            // OffsetInfo / ShapeFileHeader
            Int32 hdrSize = Marshal.SizeOf(typeof(ShapeFileHeader));
            if (fileData.Length < hdrSize)
                throw new HeaderParseException("Not long enough for header.");
            ShapeFileHeader hdr = ArrayUtils.StructFromByteArray<ShapeFileHeader>(fileData);
            Int32 nrOfFrames = hdr.Frames;
            if (nrOfFrames == 0) // Can be TS SHP; it identifies with an empty first byte IIRC.
                throw new HeaderParseException("Not a C&C1/RA1 SHP! file");
            if (hdr.Width == 0 || hdr.Height == 0)
                throw new HeaderParseException("Illegal values in header!");
            this.m_HasPalette = (hdr.Flags & 1) != 0;
            Byte[][] frames = new Byte[nrOfFrames][];
            OffsetInfo[] offsets = new OffsetInfo[nrOfFrames];
            Dictionary<UInt32, Int32> offsetIndices = new Dictionary<UInt32, Int32>();
            Int32 offsSize = Marshal.SizeOf(typeof (OffsetInfo));
            if (fileData.Length < hdrSize + offsSize * (nrOfFrames + 1))
                throw new HeaderParseException("Header is too small to contain the frames index!");
            // TODO maybe load palette?
            if (this.m_Palette == null)
                this.m_Palette = PaletteUtils.GenerateGrayPalette(8, null, false);
            this.m_FramesList = new SupportedFileType[nrOfFrames];
            this.m_Width = hdr.Width;
            this.m_Height = hdr.Height;
            // Frames decompression
            Int32 curOffs = hdrSize;
            Int32 frameSize = hdr.Width*hdr.Height;
            OffsetInfo currentFrame = ArrayUtils.ReadStructFromByteArray<OffsetInfo>(fileData, curOffs);
            Int32 frameOffs = this.GetOffset(currentFrame.Offset);
            for (Int32 i = 0; i < nrOfFrames; i++)
            {
                offsetIndices.Add(currentFrame.Offset, i);
                offsets[i] = currentFrame;
                curOffs += offsSize;
                OffsetInfo nextFrame = ArrayUtils.ReadStructFromByteArray<OffsetInfo>(fileData, curOffs);
                Int32 frameOffsEnd = this.GetOffset(nextFrame.Offset);
                Byte frameOffsFormat = this.GetFormat(currentFrame.Offset);
                //Int32 dataLen = frameOffsEnd - frameOffs;
                if (frameOffs > fileData.Length || frameOffsEnd > fileData.Length)
                    throw new HeaderParseException("File is too small to contain all frame data!");
                Byte[] frame = new Byte[frameSize];
                Int32 refIndex = -1;
                Int32 refIndex20 = -1;
                if (frameOffsFormat == 0x80)
                {
                    WWCompression.LcwUncompress(fileData, ref frameOffs, frame);
                }
                else
                {
                    if (frameOffsFormat == 0x20)
                    {
                        // Don't actually need this, but I do the integrity checks.
                        refIndex20 = this.GetOffset(currentFrame.Reference);
                        Byte refFormat = this.GetFormat(currentFrame.Reference);
                        if ((refFormat != 0x48) || (refIndex20 >= i || this.GetFormat(offsets[refIndex20].Offset) != 0x40))
                            throw new FileTypeLoadException("Bad frame reference information for frame " + i + "\".");
                        refIndex = i - 1;
                    }
                    else if (frameOffsFormat == 0x40)
                    {
                        if (!offsetIndices.TryGetValue(currentFrame.Reference, out refIndex))
                            refIndex = -1;
                    }
                    else
                        throw new FileTypeLoadException("Unknown frame type \"" + frameOffsFormat.ToString("X2") + "\".");
                    if (refIndex == -1)
                        throw new FileTypeLoadException("No reference found for XOR frame!");
                    if (refIndex >= i)
                        throw new FileTypeLoadException("XOR cannot reference later frames!");
                    frames[refIndex].CopyTo(frame, 0);
                    WWCompression.ApplyXorDelta(frame, fileData, frameOffs, frameOffsEnd);
                }
                frames[i] = frame;
                // Convert frame data to image and frame object
                Bitmap curFrImg = ImageUtils.BuildImage(frame, this.m_Width, this.m_Height, this.m_Width, PixelFormat.Format8bppIndexed, this.m_Palette, null);
                FileImageFrame framePic = new FileImageFrame();
                framePic.LoadFileFrame(this, this.ShortTypeName, curFrImg, sourcePath, i);
                framePic.SetBitsPerColor(this.BitsPerPixel);
                framePic.SetColorsInPalette(this.ColorsInPalette);
                framePic.SetTransparencyMask(this.TransparencyMask);
                framePic.SetExtraInfo("Compression: " + (frameOffsFormat == 0x80 ? "LCW" : ("XOR" + (frameOffsFormat == 0x20 ? " chained from frame " + refIndex20 : (" with frame " + refIndex)))));
                this.m_FramesList[i] = framePic;

                if (frameOffsEnd == fileData.Length)
                    break;

                // Prepare for next loop
                currentFrame = nextFrame;
                frameOffs = frameOffsEnd;
            }
        }

        protected Int32 GetOffset(UInt32 offsetInfo)
        {
            return (Int32) (offsetInfo & 0xFFFFFF);
        }

        protected Byte GetFormat(UInt32 offsetInfo)
        {
            return (Byte)((offsetInfo >> 24) & 0xFF);
        }
        
        public override Byte[] SaveToBytesAsThis(SupportedFileType fileToSave, SaveOption[] saveOptions)
        {
            throw new NotImplementedException();
        }

        public static void PreCheckSplitShadows(SupportedFileType file, Byte sourceShadowIndex, Byte destShadowIndex, Boolean forCombine)
        {
            if (file == null)
                throw new NotSupportedException("No source given!");
            if (!file.IsFramesContainer || file.Frames.Length == 0)
                throw new NotSupportedException("File contains no frames!");
            if ((file.FrameInputFileClass & FileClass.ImageIndexed) != 0)
                return;
            if (forCombine && file.Frames.Length % 2 != 0)
                throw new NotSupportedException("File does not contains an even number of frames!");
            foreach (SupportedFileType frame in file.Frames)
            {
                if (frame == null || frame.GetBitmap() == null)
                    throw new NotSupportedException("Empty frames found!");
                if ((frame.FileClass & FileClass.Image8Bit) == 0)
                    throw new NotSupportedException("All frames need to be 8-bit paletted!");
                Bitmap bm = frame.GetBitmap();
                if (bm == null)
                    throw new NotSupportedException("This operation is not supported for types with empty frames!");
                Int32 bpp = Image.GetPixelFormatSize(bm.PixelFormat);
                if (bpp > 8)
                    throw new NotSupportedException("Non-paletted frames found!");
                Int32 colors = bm.Palette.Entries.Length;
                if (colors > sourceShadowIndex)
                    throw new NotSupportedException("Not all frames have enough colours to contain the source shadow index!");
                if (forCombine && colors > destShadowIndex)
                    throw new NotSupportedException("Not all frames have enough colours to contain the destination shadow index!");
            }
        }

        public static FileFrames SplitShadows(SupportedFileType file, Byte sourceShadowIndex, Byte destShadowIndex)
        {
            PreCheckSplitShadows(file, sourceShadowIndex, destShadowIndex, false);
            FileFrames newfile = new FileFrames();
            Int32 framesOffs = file.Frames.Length;
            SupportedFileType[] shadowFrames = new SupportedFileType[framesOffs];
            Boolean shadowFound = false;
            for (Int32 i = 0; i < file.Frames.Length; i++)
            {
                SupportedFileType frame = file.Frames[i];
                
                String folder = null;
                String name = String.Empty;
                String ext = String.Empty;
                if (frame.LoadedFile != null)
                {
                    name = Path.GetFileNameWithoutExtension(frame.LoadedFile);
                    ext = Path.GetExtension(frame.LoadedFile);
                    folder = Path.GetDirectoryName(frame.LoadedFile);
                }
                else if (frame.LoadedFileName != null)
                {
                    name = Path.GetFileNameWithoutExtension(frame.LoadedFileName);
                    ext = Path.GetExtension(frame.LoadedFileName);
                }
                if (folder == null && !String.IsNullOrEmpty(file.LoadedFile))
                    folder = Path.GetDirectoryName(file.LoadedFile);
                
                Bitmap bm = frame.GetBitmap();
                Int32 width = frame.Width;
                Int32 height = frame.Height;
                Int32 stride;
                Byte[] imageData = ImageUtils.GetImageData(bm, out stride);
                Boolean shadowInFrame = imageData.Contains(sourceShadowIndex);
                if (!shadowFound && shadowInFrame)
                    shadowFound = true;
                Byte[] imageDataShadow;
                if (!shadowFound)
                    imageDataShadow = new Byte[imageData.Length];
                else
                {
                    Int32 bpp = Image.GetPixelFormatSize(bm.PixelFormat);
                    if (bpp < 8)
                        imageData = ImageUtils.ConvertTo8Bit(imageData, width, height, 0, bpp, bpp != 1, ref stride);
                    imageDataShadow = new Byte[imageData.Length];
                    for (Int32 y = 0; y < height; y++)
                    {
                        Int32 offs = y*stride;
                        for (Int32 x = 0; x < width; x++)
                        {
                            if (imageData[offs] == sourceShadowIndex)
                            {
                                imageData[offs] = 0;
                                imageDataShadow[offs] = destShadowIndex;
                            }
                            offs++;
                        }
                    }
                    if (bpp < 8)
                    {
                        Int32 stride2 = stride;
                        imageData = ImageUtils.ConvertFrom8Bit(imageData, width, height, bpp, bpp != 1, ref stride2);
                        stride2 = stride;
                        imageDataShadow = ImageUtils.ConvertFrom8Bit(imageDataShadow, width, height, bpp, bpp != 1, ref stride2);
                    }
                }
                Bitmap imageNoShadows = ImageUtils.BuildImage(imageData, width, height, stride, bm.PixelFormat, bm.Palette.Entries, null);
                String nameNoShadows = name + ext;
                if (folder != null)
                    nameNoShadows = Path.Combine(folder, nameNoShadows);
                FileImageFrame frameNoShadows = new FileImageFrame();
                frameNoShadows.LoadFileFrame(newfile, file.ShortTypeName, imageNoShadows, nameNoShadows, i);
                frameNoShadows.SetBitsPerColor(frame.BitsPerPixel);
                frameNoShadows.SetColorsInPalette(frame.ColorsInPalette);
                newfile.AddFrame(frameNoShadows);
                
                Bitmap imageOnlyShadows = ImageUtils.BuildImage(imageDataShadow, width, height, stride, bm.PixelFormat, bm.Palette.Entries, null);
                String nameOnlyShadows = name + "_s" + ext;
                if (folder != null)
                    nameOnlyShadows = Path.Combine(folder, nameOnlyShadows);
                FileImageFrame frameOnlyShadows = new FileImageFrame();
                frameOnlyShadows.LoadFileFrame(newfile, file.ShortTypeName, imageOnlyShadows, nameOnlyShadows, i);
                frameOnlyShadows.SetBitsPerColor(frame.BitsPerPixel);
                frameOnlyShadows.SetColorsInPalette(frame.ColorsInPalette);
                shadowFrames[i] = frameOnlyShadows;
            }
            foreach (SupportedFileType shadowFrame in shadowFrames)
                newfile.AddFrame(shadowFrame);
            return newfile;
        }

        public static FileFrames CombineShadows(SupportedFileType file, Byte sourceShadowIndex, Byte destShadowIndex)
        {
            return null;
        }

        private struct ShapeFileHeader
        {
            public UInt16 Frames;
            public UInt16 XPos;
            public UInt16 YPos;
            public UInt16 Width;
            public UInt16 Height;
            public UInt16 DeltaSize;
            public UInt16 Flags;
        }

        private struct OffsetInfo
        {
            public UInt32 Offset;
            public UInt32 Reference;
        }

    }



    public enum ShpVersion
    {
        //Dune2,
        //Kyrandia,
        Cnc,
        //TibSun
    }

}