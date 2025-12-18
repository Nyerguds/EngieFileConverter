using System;
using System.Drawing;
using System.Drawing.Imaging;
using Nyerguds.FileData.Bloodlust;
using Nyerguds.ImageManipulation;
using Nyerguds.Util;

namespace EngieFileConverter.Domain.FileTypes
{
    public class FileImageExecM : FileImageExec
    {
        public override String IdCode { get { return "ExImgM"; } }
        public override String LongTypeName { get { return "Executioner Image (Masked)"; } }
        public override Boolean DecodeMask { get { return true; } }

        // TODO change input to also accept masks
    }

    public class FileImageExec : SupportedFileType
    {
        public override FileClass FileClass { get { return FileClass.Image8Bit; } }
        public override FileClass InputFileClass { get { return FileClass.Image8Bit | FileClass.FrameSet; } }
        public override FileClass FrameInputFileClass { get { return FileClass.Image8Bit; } }

        public override String IdCode { get { return "ExImg"; } }
        /// <summary>Very short code name for this type.</summary>
        public override String ShortTypeName { get { return "Executioner Image"; } }
        public override String[] FileExtensions { get { return new String[] { "vol" }; } }
        public override String LongTypeName { get { return "Executioner Image"; } }
        public override Boolean NeedsPalette { get { return true; } }
        public override Int32 BitsPerPixel { get { return 8; } }
        protected SupportedFileType[] m_FramesList;
        /// <summary>Retrieves the sub-frames inside this file.</summary>
        public override SupportedFileType[] Frames { get { return this.m_FramesList; } }

        public virtual Boolean DecodeMask { get { return false; } }

        /// <summary>Array of Booleans which defines for the palette which indices are transparent.</summary>
        public override Boolean[] TransparencyMask
        {
            get
            {
                Boolean[] transMask = new Boolean[0x100];
                transMask[0xFF] = true;
                return transMask;
            }
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
            if (fileData.Length < 4)
                throw new FileTypeLoadException(ERR_NO_HEADER);
            if (fileData[0] != 0x10 || fileData[3] != 0xFF)
                throw new FileTypeLoadException(ERR_BAD_HEADER_DATA);
            Int32 width = fileData[1];
            Int32 height = fileData[2];
            if (width == 0 || height == 0)
                throw new FileTypeLoadException(ERR_DIM_ZERO);
            Int32 ptr = 0;

            Byte[] mask = this.DecodeMask ? new Byte[width * height] : null;
            Byte fillValue = (Byte) (this.DecodeMask ? 0x00 : 0xFF);
            Boolean success;
            Byte[] imageData = ExecutionersCompression.DecodeChunk(fileData, ref ptr, fillValue, ref mask, 0x01, out success);
            //Byte[] imageData = Decode(fileData, 4, width, height);
            //System.IO.File.WriteAllBytes(sourcePath + ".dec", imageData);
            if (imageData == null)
                throw new FileTypeLoadException(ERR_DECOMPR);
            this.ExtraInfo = success ? null : "Premature end of data.";
            //if (fileData.Length != ptr && fileData.Length != ptr + 1)
            //    throw new FileTypeLoadException(ERR_BAD_SIZE);
            this.m_Palette = PaletteUtils.GenerateGrayPalette(8, this.TransparencyMask, false);
            Bitmap image = ImageUtils.BuildImage(imageData, width, height, width, PixelFormat.Format8bppIndexed, this.m_Palette, Color.Black);
            if (!this.DecodeMask)
            {
                this.m_LoadedImage = image;
                
            }
            else
            {
                this.m_FramesList = new SupportedFileType[2];
                FileImageFrame framePic = new FileImageFrame();
                framePic.LoadFileFrame(this, this, image, sourcePath, 0);
                framePic.SetBitsPerColor(this.BitsPerPixel);
                framePic.SetFileClass(this.FrameInputFileClass);
                framePic.SetNeedsPalette(this.NeedsPalette);
                framePic.SetExtraInfo(this.ExtraInfo);
                this.m_FramesList[0] = framePic;
                Bitmap imageMask = ImageUtils.BuildImage(mask, width, height, width, PixelFormat.Format8bppIndexed, this.m_Palette, Color.Black);
                FileImageFrame frameMask = new FileImageFrame();
                frameMask.LoadFileFrame(this, this, imageMask, sourcePath, 0);
                frameMask.SetBitsPerColor(this.BitsPerPixel);
                frameMask.SetFileClass(this.FrameInputFileClass);
                frameMask.SetNeedsPalette(this.NeedsPalette);
                frameMask.SetExtraInfo("Transparency mask");
                this.m_FramesList[1] = frameMask;
                this.m_LoadedImage = null;
            }
        }

        public override Option[] GetSaveOptions(SupportedFileType fileToSave, String targetFileName)
        {
            this.PerformPreliminaryChecks(fileToSave);
            // No options if the file is not a frames container.
            if (fileToSave.IsFramesContainer)
                return null;
            return new Option[]
            {
                new Option("TID", OptionInputType.Number, "Index to use as transparency","0,255", "255"),
            };
        }

        protected Bitmap[] PerformPreliminaryChecks(SupportedFileType fileToSave)
        {            
            if (fileToSave == null)
                throw new ArgumentException(ERR_EMPTY_FILE, "fileToSave");
            Bitmap image;
            if (!fileToSave.IsFramesContainer)
            {
                if ((image = fileToSave.GetBitmap()) == null)
                    throw new ArgumentException(ERR_EMPTY_FILE, "fileToSave");
                if (image.PixelFormat != PixelFormat.Format8bppIndexed)
                    throw new ArgumentException(String.Format(ERR_INPUT_XBPP, 8), "fileToSave");
                if (image.Width > 255 || image.Height > 255)
                    throw new ArgumentException("Image dimensions cannot exceed 255.", "fileToSave");
                return new Bitmap[] {image};
            }
            const String framesErr = "The only frame input accepted by this type is a single frame plus its mask.";
            if (fileToSave.Frames.Length != 2)
                throw new ArgumentException(framesErr, "fileToSave");
            SupportedFileType frame0 = fileToSave.Frames[0];
            SupportedFileType frame1 = fileToSave.Frames[1];
            if (frame0 == null || frame1 == null)
                throw new ArgumentException(framesErr, "fileToSave");
            Bitmap mask;
            if ((image = frame0.GetBitmap()) == null || (mask = frame1.GetBitmap()) == null)
                throw new ArgumentException(framesErr, "fileToSave");
            if (image.PixelFormat != PixelFormat.Format8bppIndexed || mask.PixelFormat != PixelFormat.Format8bppIndexed)
                throw new ArgumentException(String.Format(ERR_INPUT_XBPP, 8), "fileToSave");
            if (image.Width > 255 || image.Height > 255)
                throw new ArgumentException("Image dimensions cannot exceed 255.", "fileToSave");
            if (image.Width != mask.Width || image.Height != mask.Height)
                throw new ArgumentException("The dimensions of the mask image and the frame must be identical.",
                    "fileToSave");
            Byte[] maskBytes = ImageUtils.GetImageData(mask, true);
            Int32 len = maskBytes.Length;
            for (Int32 i = 0; i < len; ++i)
                if (maskBytes[i] > 1)
                    throw new ArgumentException(
                        "Mask image should only contain 0 and 1 values, with 1 indicating masked pixels.",
                        "fileToSave");
            return new Bitmap[] {image, mask};
        }

        public override Byte[] SaveToBytesAsThis(SupportedFileType fileToSave, Option[] saveOptions)
        {
            Bitmap[] bms = this.PerformPreliminaryChecks(fileToSave);
            Bitmap image = bms[0];
            if (fileToSave.IsFramesContainer)
            {
                Bitmap mask = bms[1];
                Byte[] maskBytes = ImageUtils.GetImageData(mask, true);
                Byte[] imageBytes = ImageUtils.GetImageData(image, true);
                return ExecutionersCompression.EncodeToChunk(imageBytes, image.Width, image.Height, maskBytes, 1);
            }
            else
            {
                Int32 transIndex;
                Int32.TryParse(Option.GetSaveOptionValue(saveOptions, "TID"), out transIndex);
                Byte[] imageBytes = ImageUtils.GetImageData(image, true);
                return ExecutionersCompression.EncodeToChunk(imageBytes, image.Width, image.Height, (Byte)transIndex);
            }
        }
    }
}