using CnC64FileConverter.Domain.HeightMap;
using CnC64FileConverter.Domain.FileTypes;
using Nyerguds.ImageManipulation;
using Nyerguds.Util;
using Nyerguds.Util.UI;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace CnC64FileConverter.UI
{
    public partial class FrmCnC64FileConverter : Form
    {
        private const String PROG_NAME = "C&C64 File Converter";
        private const String PROG_AUTHOR = "Created by Nyerguds";
        private const Int32 PALETTE_DIM = 226;

        private String m_StartupParamPath;
        private Boolean m_Loading;
        private List<PaletteDropDownInfo> m_DefaultPalettes;
        private List<PaletteDropDownInfo> m_ReadPalettes;
        private Int32[] m_CustomColors;
        private SupportedFileType m_LoadedFile;
        private Color m_BackgroundFillColor = Color.Fuchsia;

        private SupportedFileType GetShownFile()
        {
            if (this.m_LoadedFile == null)
                return null;
            Boolean hasFrames = this.m_LoadedFile.Frames != null && this.m_LoadedFile.Frames.Length > 0;
            return hasFrames && this.numFrame.Value != -1 ? (this.m_LoadedFile.Frames.Length > this.numFrame.Value ? this.m_LoadedFile.Frames[(Int32) this.numFrame.Value] : null) : this.m_LoadedFile;
        }

        public FrmCnC64FileConverter()
        {
            this.InitializeComponent();
            this.Text = GetTitle(true);
            this.picImage.BackColor = this.m_BackgroundFillColor;
            this.lblTransparentColorVal.BackColor = this.m_BackgroundFillColor;
            PalettePanel.InitPaletteControl(0, this.palColorViewer, new Color[0], PALETTE_DIM);
            this.m_DefaultPalettes = this.LoadDefaultPalettes();
            this.m_ReadPalettes = this.LoadExtraPalettes();
            this.RefreshPalettes(false, false);
            ContextMenu cmCopyPreview = new ContextMenu();
            MenuItem mniCopy = new MenuItem("Copy");
            mniCopy.Click += new EventHandler(this.PicImage_CopyPreview);
            cmCopyPreview.MenuItems.Add(mniCopy);
            this.picImage.ContextMenu = cmCopyPreview;
        }

        public static String GetTitle(Boolean withAuthor)
        {
            String title = PROG_NAME + " " + GeneralUtils.ProgramVersion();
            if (withAuthor)
                title += " - " + PROG_AUTHOR;
            return title;
        }


        public FrmCnC64FileConverter(String[] args)
            : this()
        {
            if (args.Length > 0 && File.Exists(args[0]))
                this.m_StartupParamPath = args[0];
        }

        public List<PaletteDropDownInfo> LoadDefaultPalettes()
        {
            List<PaletteDropDownInfo> palettes = new List<PaletteDropDownInfo>();
            palettes.Add(new PaletteDropDownInfo("Black/White", 1, new Color[] {Color.Black, Color.White}, null, -1, false, false));
            palettes.Add(new PaletteDropDownInfo("White/Black", 1, new Color[] {Color.White, Color.Black}, null, -1, false, false));
            palettes.Add(new PaletteDropDownInfo("Black/Red", 1, new Color[] {Color.Black, Color.Red}, null, -1, false, false));
            palettes.Add(new PaletteDropDownInfo("Grayscale B->W", 4, PaletteUtils.GenerateGrayPalette(4, null, false), null, -1, false, false));
            //palettes.Add(new PaletteDropDownInfo("Heights Blue->Red", 4, PaletteUtils.GenerateRainbowPalette(4, false, false, true, 0, 160.0 / 240.0), null, -1, false, false));
            palettes.Add(new PaletteDropDownInfo("Grayscale W->B", 4, PaletteUtils.GenerateGrayPalette(4, null, true), null, -1, false, false));
            palettes.Add(new PaletteDropDownInfo("Rainbow", 4, PaletteUtils.GenerateRainbowPalette(4, -1, null, false), null, -1, false, false));
            //palettes.Add(new PaletteDropDownInfo("Windows palette", 4, PaletteUtils.GenerateDefWindowsPalette(4, false, false), null, -1, false, false));
            palettes.Add(new PaletteDropDownInfo("Grayscale B->W", 8, PaletteUtils.GenerateGrayPalette(8, null, false), null, -1, false, false));
            palettes.Add(new PaletteDropDownInfo("Heights Blue->Red", 8, PaletteUtils.GenerateRainbowPalette(8, -1, null, true, 0, 160, true), null, -1, false, false));
            //palettes.Add(new PaletteDropDownInfo("Grayscale W->B", 8, PaletteUtils.GenerateGrayPalette(8, false, true), null, -1, false, false));
            palettes.Add(new PaletteDropDownInfo("Rainbow", 8, PaletteUtils.GenerateRainbowPalette(8, -1, null, false), null, -1, false, false));
            //palettes.Add(new PaletteDropDownInfo("Windows palette", 8, PaletteUtils.GenerateDefWindowsPalette(8, false, false), null, -1, false, false));
            return palettes;
        }

        private void PicImage_CopyPreview(Object sender, EventArgs e)
        {
            this.CopyToClipboard();
        }

        private void tsmiCopy_Click(Object sender, EventArgs e)
        {
            this.CopyToClipboard();
        }

        private void CopyToClipboard()
        {
            Image image = this.picImage.Image;
            if (image == null)
                return;
            using (Bitmap bm = new Bitmap(image))
            using (Bitmap bmnt = ImageUtils.PaintOn32bpp(image, this.m_BackgroundFillColor))
                ClipboardImage.SetClipboardImage(bm, bmnt, null);
        }

        private void Frm_DragEnter(Object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
                e.Effect = DragDropEffects.Copy;
        }

        private void Frm_DragDrop(Object sender, DragEventArgs e)
        {
            String[] files = (String[]) e.Data.GetData(DataFormats.FileDrop);
            if (files.Length != 1)
                return;
            String path = files[0];
            SupportedFileType[] preferredTypes = FileDialogGenerator.IdentifyByExtension<SupportedFileType>(SupportedFileType.AutoDetectTypes, path);
            this.LoadFile(path, null, preferredTypes);
        }

        private void LoadFile(String path, SupportedFileType selectedType, SupportedFileType[] preferredTypes)
        {
            SupportedFileType oldLoaded = this.m_LoadedFile;
            this.m_Loading = true;
            Byte[] fileData = null;
            try
            {
                FileTypeLoadException error = null;
                Boolean isEmptyFile = false;
                try
                {
                    try
                    {
                        fileData = File.ReadAllBytes(path);
                        isEmptyFile = fileData.Length == 0;
                    }
                    catch (Exception e)
                    {
                        error = new FileTypeLoadException("Could not access file!\n\n" + e.Message, e);
                        this.m_LoadedFile = null;
                    }
                    if (!isEmptyFile && error == null)
                    {
                        // Load from chosen type.
                        if (selectedType != null)
                        {
                            try
                            {
                                selectedType.LoadFile(fileData, path);
                                this.m_LoadedFile = selectedType;
                            }
                            catch (FileTypeLoadException e)
                            {
                                this.m_LoadedFile = null;
                                e.AttemptedLoadedType = selectedType.ShortTypeName;
                                error = e;
                                // autodetect is possible. Set type to null.
                                if (preferredTypes != null && preferredTypes.Length > 1)
                                    selectedType = null;
                            }
                        }
                        //Autodetect logic.
                        if (selectedType == null)
                        {
                            List<FileTypeLoadException> loadErrors;
                            this.m_LoadedFile = SupportedFileType.LoadFileAutodetect(fileData, path, preferredTypes, out loadErrors, error != null);
                            if (this.m_LoadedFile != null)
                                error = null;
                            else
                            {
                                if (error != null)
                                    loadErrors.Insert(0, error);
                                String errors = String.Join("\n", loadErrors.Select(er => er.AttemptedLoadedType + ": " + er.Message).ToArray());
                                String filename = path == null ? String.Empty : (" of \"" + Path.GetFileName(path) + "\"");
                                MessageBox.Show(this, "File type" + filename + " could not be identified. Errors returned by all attempts:\n\n" + errors, GetTitle(false), MessageBoxButtons.OK, MessageBoxIcon.Warning);
                                return;
                            }
                            /*/
                            Bitmap bmp = m_LoadedFile.GetBitmap();
                            if (bmp != null)
                            {
                                Int32 stride;
                                Byte[] blackAndWhite = ImageUtils.GetImageData(bmp, out stride, PixelFormat.Format1bppIndexed);
                                Bitmap bwBitmap = ImageUtils.BuildImage(blackAndWhite, bmp.Width, bmp.Height, stride, PixelFormat.Format1bppIndexed, new[] { Color.Black, Color.White }, null);
                                FileImagePng png = new FileImagePng();
                                png.LoadFile(bwBitmap, path);
                                SupportedFileType oldFile = m_LoadedFile;
                                m_LoadedFile = png;
                                //oldFile.Dispose();
                            }
                            //*/
                        }
                    }
                }
                catch (Exception ex)
                {
                    error = new FileTypeLoadException(ex.Message, ex);
                    this.m_LoadedFile = null;
                }
                List<String> filesChain = null;
                if (!isEmptyFile && error == null && this.m_LoadedFile.IsFramesContainer && (filesChain = this.m_LoadedFile.GetFilesToLoadMissingData(path)) != null && filesChain.Count > 0)
                {
                    const String loadQuestion = "The file \"{0}\" seems to be missing a starting point. Would you like to load it from \"{1}\"{2}?";
                    const String loadQuestionChain = " (chained through {0})";
                    String firstPath = filesChain.First();
                    String[] chain = filesChain.Skip(1).Select(pth => "\"" + Path.GetFileName(pth) + "\"").ToArray();
                    String chainQuestion = chain.Length == 0 ? String.Empty : String.Format(loadQuestionChain, String.Join(", ", chain));
                    String loadQuestionFormat = String.Format(loadQuestion, Path.GetFileName(path), Path.GetFileName(firstPath), chainQuestion);
                    if (MessageBox.Show(this, loadQuestionFormat, GetTitle(false), MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                    {
                        // quick way to enable the frames detection in the next part, if I do ever want to support real animation chaining.
                        filesChain = null;
                    }
                    else
                    {
                        this.m_LoadedFile.ReloadFromMissingData(fileData, path, filesChain);
                    }
                }
                if (filesChain == null && (isEmptyFile || error == null))
                {
                    SupportedFileType detectSource = this.m_LoadedFile;
                    if (isEmptyFile && preferredTypes.Length == 1)
                        detectSource = preferredTypes[0];
                    SupportedFileType frames = this.CheckForFrames(path, detectSource);
                    if (ReferenceEquals(frames, detectSource) && isEmptyFile)
                    {
                        this.m_LoadedFile = null;
                        if (detectSource != null)
                        {
                            try
                            {
                                detectSource.Dispose();
                            }
                            catch (Exception)
                            {
                                /* ignore */
                            }
                        }
                    }
                    else
                        this.m_LoadedFile = frames;
                }
                this.AutoSetZoom();
                this.ReloadUi(true);
                if (!ReferenceEquals(this.m_LoadedFile, oldLoaded))
                {
                    try
                    {
                        oldLoaded.Dispose();
                    }
                    catch (Exception)
                    {
                        /* ignore */
                    }
                }
                if (error != null)
                    MessageBox.Show(this, "File loading failed: " + error.Message, GetTitle(false), MessageBoxButtons.OK, MessageBoxIcon.Warning);
                if (this.m_LoadedFile == null && isEmptyFile)
                    MessageBox.Show(this, "File loading failed: The file is empty!", GetTitle(false), MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            finally
            {
                this.m_Loading = false;
            }
        }

        /// <summary>
        /// Checks if the current type is frameless, and if so, checks if it is part of a numerical range of frames.
        /// If so, and all found frames can be loaded as the identified type, the program asks to load all the files
        /// as frames, instead of loading one file as image.
        /// </summary>
        /// <param name="path">path that was opened.</param>
        /// <param name="currentType">Currently loaded file from the given path.</param>
        /// <returns>A generic SupportedType object filled with the frames, or the original 'currentType' object if the detect failed or was aborted.</returns>
        private SupportedFileType CheckForFrames(String path, SupportedFileType currentType)
        {
            String minName;
            String maxName;
            Boolean hasEmptyFrames;
            Boolean isChainedFramesType;
            SupportedFileType fr = FileFrames.CheckForFrames(path, currentType, out minName, out maxName, out hasEmptyFrames, out isChainedFramesType);
            if (fr == null)
                return currentType;
            String emptywarning = hasEmptyFrames ? "\nSome of these frames are empty files. Not every save format supports empty frames." : String.Empty;
            String loadFrames = isChainedFramesType ? "Load the frames from all files" : "load it as frames";
            String message = "The file appears to be part of a range (" + minName + " - " + maxName + ")." + emptywarning + "\n\nDo you wish to " + loadFrames + "?";
            if (MessageBox.Show(this, message, GetTitle(false), MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
            {
                currentType.Dispose();
                return fr;
            }
            fr.Dispose();
            return currentType;
        }

        private void AutoSetZoom()
        {
            if (this.m_LoadedFile == null)
                return;
            // Set image invisible to remove scrollbars.
            this.picImage.Visible = false;
            Int32 maxWidth = this.pnlImageScroll.ClientSize.Width;
            Int32 maxHeight = this.pnlImageScroll.ClientSize.Height;
            Int32 minZoomFactor = Int32.MaxValue;
            // Build list of images to check
            List<Bitmap> framesToCheck = new List<Bitmap>();
            framesToCheck.Add(this.m_LoadedFile.GetBitmap());
            if (this.m_LoadedFile.Frames != null)
                framesToCheck.AddRange(this.m_LoadedFile.Frames.Select(x => x == null ? null : x.GetBitmap()));
            foreach (Bitmap image in framesToCheck)
            {
                Int32 zoomFactor = image == null ? Int32.MaxValue : Math.Max(1, Math.Min(maxWidth / image.Width, maxHeight / image.Height));
                minZoomFactor = Math.Min(zoomFactor, minZoomFactor);
            }
            if (minZoomFactor == Int32.MaxValue)
                minZoomFactor = 1;
            this.numZoom.EnteredValue = this.numZoom.Constrain(minZoomFactor);
        }

        private void ReloadUi(Boolean fromNewFile)
        {
            Boolean hasFrames = this.m_LoadedFile != null && this.m_LoadedFile.Frames != null && this.m_LoadedFile.Frames.Length > 0;
            this.lblFrame.Enabled = hasFrames;
            this.numFrame.Enabled = hasFrames;
            this.numFrame.Minimum = -1;
            if (!hasFrames)
            {
                this.numFrame.Value = -1;
                this.numFrame.Maximum = -1;
                this.lblNrOfFrames.Visible = false;
            }
            else
            {
                if (fromNewFile)
                    this.numFrame.Value = -1;
                Int32 last = this.m_LoadedFile.Frames.Length - 1;
                this.numFrame.Maximum = last;
                this.lblNrOfFrames.Visible = true;
                this.lblNrOfFrames.Text = "/ " + last;
                if (last >= 0 && !this.m_LoadedFile.IsFramesContainer)
                    this.numFrame.Minimum = 0;
            }
            SupportedFileType loadedFile = this.GetShownFile();
            Boolean hasFile = loadedFile != null;
            this.tsmiSave.Enabled = hasFile;
            this.tsmiSaveFrames.Enabled = this.m_LoadedFile != null && (this.m_LoadedFile.FileClass & FileClass.FrameSet) != 0;
            this.tsmiCopy.Enabled = hasFile;
            // C&C64 toolsets
            this.tsmiToHeightMap.Enabled = loadedFile is FileMapWwCc1Pc;
            this.tsmiToPlateaus.Enabled = loadedFile is FileMapWwCc1Pc;
            this.tsmiToHeightMapAdv.Enabled = loadedFile is FileMapWwCc1Pc;
            this.tsmiTo65x65HeightMap.Enabled = hasFile && loadedFile.GetBitmap() != null && loadedFile.Width == 64 && loadedFile.Height == 64;
            // 4 is the supertype; 8 the derivative
            this.tsmiTilesetsToFrames.Enabled = loadedFile is FileTilesWwCc1N64Bpp4;
            this.tsmiTilesetsToTilesetFiles.Enabled = loadedFile is FileTilesWwCc1N64Bpp4;

            if (!hasFile)
            {
                String emptystr = "---";
                this.lblValFilename.Text = emptystr;
                this.lblValType.Text = emptystr;
                this.toolTip1.SetToolTip(this.lblValType, null);
                this.lblValWidth.Text = emptystr;
                this.lblValHeight.Text = emptystr;
                this.lblValColorFormat.Text = emptystr;
                this.lblValColorsInPal.Text = emptystr;
                this.lblValInfo.Text = String.Empty;
                this.cmbPalettes.Enabled = false;
                this.cmbPalettes.SelectedIndex = 0;
                this.btnResetPalette.Enabled = false;
                this.btnSavePalette.Enabled = false;
                this.picImage.Image = null;
                this.RefreshImage(false);
                PalettePanel.InitPaletteControl(0, this.palColorViewer, new Color[0], PALETTE_DIM);
                this.palColorViewer.MaxColors = 0;
                return;
            }
            Int32 bpc = loadedFile.BitsPerPixel;
            this.lblValFilename.Text = GeneralUtils.DoubleFirstAmpersand(loadedFile.LoadedFileName);
            this.lblValType.Text = GeneralUtils.DoubleFirstAmpersand(loadedFile.ShortTypeDescription);
            this.toolTip1.SetToolTip(this.lblValType, this.lblValType.Text);
            this.lblValWidth.Text = loadedFile.Width.ToString();
            this.lblValHeight.Text = loadedFile.Height.ToString();
            this.lblValColorFormat.Text = bpc == 0 ? "N/A" : (bpc + " BPP" + (bpc == 4 || bpc == 8 ? " (paletted)" : String.Empty));
            Color[] palette = loadedFile.GetColors();
            Int32 exposedColours = loadedFile.ColorsInPalette;
            Int32 actualColors = palette == null ? 0 : palette.Length;
            Boolean needsPalette = bpc != 0 && bpc <= 8 && exposedColours != actualColors;
            this.lblValColorsInPal.Text = actualColors + (needsPalette ? " (" + exposedColours + " in file)" : String.Empty);
            this.lblValInfo.Text = GeneralUtils.DoubleFirstAmpersand(loadedFile.ExtraInfo);
            this.cmbPalettes.Enabled = needsPalette;
            Bitmap image = loadedFile.GetBitmap();
            this.picImage.Image = image;
            this.RefreshPalettes(false, false);
            this.RefreshImage(false);
            if (needsPalette && fromNewFile)
                this.CmbPalettes_SelectedIndexChanged(null, null);
            else
                this.RefreshColorControls();
        }

        private ColorStatus GetColorStatus()
        {
            SupportedFileType loadedFile = this.GetShownFile();
            if (loadedFile == null)
                return ColorStatus.None;
            Color[] cols = loadedFile.GetColors();
            // High-colored image, or no image at all: palette is not applicable.
            if (cols == null || cols.Length == 0)
                return ColorStatus.None;
            // Indexed image without internal palette. This assumes cols.length > 0, but that's already enforced by the previous check.
            if (loadedFile.ColorsInPalette == 0)
                return ColorStatus.External;
            // Only left over case is an image with an internal palette.
            return ColorStatus.Internal;
        }

        public List<PaletteDropDownInfo> GetPalettes(Int32 bpp, Boolean reloadFiles)
        {
            List<PaletteDropDownInfo> allPalettes = this.m_DefaultPalettes.Where(p => p.BitsPerPixel == bpp).ToList();
            if (reloadFiles)
                this.m_ReadPalettes = this.LoadExtraPalettes();
            allPalettes.AddRange(this.m_ReadPalettes.Where(p => p.BitsPerPixel == bpp));
            return allPalettes;
        }

        public List<PaletteDropDownInfo> LoadExtraPalettes()
        {
            List<PaletteDropDownInfo> palettes = new List<PaletteDropDownInfo>();
            String appFolder = Path.GetDirectoryName(Application.ExecutablePath);
            FileInfo[] files = new DirectoryInfo(appFolder).GetFiles("*.pal").OrderBy(x => x.Name).ToArray();
            foreach (FileInfo file in files)
                palettes.AddRange(PaletteDropDownInfo.LoadSubPalettesInfoFromPalette(file, false, false, true));
            return palettes;
        }

        private void FrmCnC64FileConverter_Shown(Object sender, EventArgs e)
        {
            if (this.m_StartupParamPath != null)
                this.LoadFile(this.m_StartupParamPath, null, null);
            else
                this.ReloadUi(true);
        }

        private void TsmiSave_Click(Object sender, EventArgs e)
        {
            this.Save(false);
        }

        private void TsmiSaveFrames_Click(Object sender, EventArgs e)
        {
            this.Save(true);
        }

        private void Save(Boolean frames)
        {
            if (this.m_LoadedFile == null)
                return;
            SupportedFileType selectedItem;
            Boolean hasFrames = this.m_LoadedFile.Frames != null && this.m_LoadedFile.Frames.Length > 0;
            Boolean isFrame = !frames && hasFrames && this.numFrame.Value != -1;
            SupportedFileType loadedFile = isFrame ? this.m_LoadedFile.Frames[(Int32) this.numFrame.Value] : this.m_LoadedFile;
            Type selectType = frames ? typeof (FileImagePng) : loadedFile.GetType();
            Type[] saveTypes = SupportedFileType.SupportedSaveTypes;
            FileClass loadedFileType = loadedFile.FileClass;
            FileClass frameFileType = FileClass.None;
            if (hasFrames && !isFrame)
            {
                SupportedFileType first = this.m_LoadedFile.Frames.FirstOrDefault(x => x != null && x.GetBitmap() != null);
                if (first != null)
                    frameFileType = first.FileClass;
            }

            List<Type> filteredTypes = new List<Type>();
            foreach (Type saveType in saveTypes)
            {
                SupportedFileType tmpsft = (SupportedFileType) Activator.CreateInstance(saveType);
                FileClass diff = tmpsft.InputFileClass & (frames ? frameFileType : loadedFileType);
                if ((diff & ~FileClass.FrameSet) != 0 || (!frames && (tmpsft.FrameInputFileClass & frameFileType) != 0))
                    filteredTypes.Add(saveType);
            }
            if (filteredTypes.Count == 0)
            {
                String message = "No types found for saving this data.";
                if (hasFrames && !isFrame)
                    message += "\nTry exporting as frames instead.";
                MessageBox.Show(this, message, GetTitle(false), MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (!filteredTypes.Contains(selectType))
            {
                Type newSelectType = filteredTypes.FirstOrDefault(x => selectType.IsSubclassOf(x));
                if (newSelectType == null)
                    newSelectType = filteredTypes.FirstOrDefault(x => x.IsSubclassOf(selectType));
                if (newSelectType == null)
                    newSelectType = typeof (FileImagePng);
                selectType = newSelectType;
            }
            String filename = FileDialogGenerator.ShowSaveFileFialog(this, selectType, filteredTypes.ToArray(), false, true, loadedFile.LoadedFile, out selectedItem);
            if (filename == null || selectedItem == null)
                return;
            try
            {
                SaveOption[] saveOptions = selectedItem.GetSaveOptions(loadedFile, filename);
                if (saveOptions != null && saveOptions.Length > 0)
                {
                    SaveOptionInfo soi = new SaveOptionInfo();
                    soi.Name = GeneralUtils.DoubleFirstAmpersand("Extra save options for " + selectedItem.ShortTypeDescription);
                    soi.Properties = saveOptions;
                    FrmExtraOptions extraopts = new FrmExtraOptions();
                    extraopts.Init(soi);
                    if (extraopts.ShowDialog(this) != DialogResult.OK)
                        return;
                    saveOptions = extraopts.GetSaveOptions();
                }
                if (!frames)
                    selectedItem.SaveAsThis(loadedFile, filename, saveOptions);
                else
                {
                    if (loadedFile.Frames == null)
                        return;
                    String framename = Path.Combine(Path.GetDirectoryName(filename), Path.GetFileNameWithoutExtension(filename));
                    String extension = Path.GetExtension(filename);
                    for (Int32 i = 0; i < loadedFile.Frames.Length; i++)
                    {
                        SupportedFileType frame = loadedFile.Frames[i];
                        String framePath = framename + "-" + i.ToString("D5") + extension;
                        if (frame.GetBitmap() == null) // Allow empty frames as empty files.
                            File.WriteAllBytes(framePath, new Byte[0]);
                        else
                            selectedItem.SaveAsThis(frame, framePath, saveOptions);
                    }
                }
            }
            catch (NotSupportedException ex)
            {
                String message = "Cannot save " + (frames ? "frame of " : String.Empty) + "type " + loadedFile.ShortTypeName
                                 + " as type " + selectedItem.ShortTypeName + (String.IsNullOrEmpty(ex.Message) ? "." : ":\n" + ex.Message);
                MessageBox.Show(this, message, GetTitle(false), MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void TsmiExit_Click(Object sender, EventArgs e)
        {
            this.Close();
        }

        private void TsmiOpen_Click(Object sender, EventArgs e)
        {
            SupportedFileType selectedItem;
            String openPath = null;
            if (this.m_LoadedFile != null)
                openPath = this.m_LoadedFile.LoadedFile;
            String filename = FileDialogGenerator.ShowOpenFileFialog(this, null, SupportedFileType.SupportedOpenTypes, openPath, "images", null, out selectedItem);
            if (filename == null)
                return;
            SupportedFileType[] preferredTypes = null;
            if (selectedItem == null)
                preferredTypes = FileDialogGenerator.IdentifyByExtension<SupportedFileType>(SupportedFileType.AutoDetectTypes, filename);
            else
            {
                SupportedFileType curr = selectedItem;
                Type currType = curr.GetType();
                Type[] subTypes = SupportedFileType.AutoDetectTypes.Where(x => currType.IsAssignableFrom(x)).ToArray();
                if (subTypes.Length > 0 && subTypes[0] != currType)
                {
                    List<SupportedFileType> subTypeObjs = new List<SupportedFileType>(FileDialogGenerator.GetItemsList<SupportedFileType>(subTypes));
                    SupportedFileType[] extNarrowedTypes = FileDialogGenerator.IdentifyByExtension<SupportedFileType>(subTypes, filename);
                    if (extNarrowedTypes.Length == 1)
                        selectedItem = extNarrowedTypes[0];
                    subTypeObjs.RemoveAll(x => x.GetType() == selectedItem.GetType());
                    if (selectedItem.GetType() != currType && subTypeObjs.All(x => x.GetType() != currType))
                        subTypeObjs.Add(curr);
                    preferredTypes = subTypeObjs.ToArray();
                }
            }
            this.LoadFile(filename, selectedItem, preferredTypes);
        }

        private void NumZoom_ValueChanged(Object sender, EventArgs e)
        {
            this.RefreshImage(true);
        }

        private void RefreshImage(Boolean adaptZoom)
        {
            Bitmap bm = (Bitmap) this.picImage.Image;
            Boolean loadOk = bm != null;
            this.picImage.Visible = loadOk;
            // Centering zoom code: save all information before image resize
            Int32 currentZoom = (Int32) this.numZoom.Value;
            Int32 oldWidth = this.picImage.Width;
            Int32 oldHeight = this.picImage.Height;
            Int32 newWidth = loadOk ? bm.Width * currentZoom : 100;
            Int32 newHeight = loadOk ? bm.Height * currentZoom : 100;
            Int32 frameLeftVal = this.pnlImageScroll.DisplayRectangle.X;
            Int32 frameUpVal = this.pnlImageScroll.DisplayRectangle.Y;
            Int32 prevZoom = oldWidth * currentZoom / newWidth;
            Int32 visibleCenterXOld = Math.Min(oldWidth, this.pnlImageScroll.ClientRectangle.Width) / 2;
            Int32 visibleCenterYOld = Math.Min(oldHeight, this.pnlImageScroll.ClientRectangle.Height) / 2;

            this.picImage.Width = newWidth;
            this.picImage.Height = newHeight;
            this.picImage.PerformLayout();

            if (!adaptZoom || !loadOk || prevZoom <= 0 || prevZoom == currentZoom)
                return;
            // Centering zoom code: Image resized. Apply zoom centering.
            // ClientRectangle data is fetched again since it changes when scrollbars appear and disappear.
            Int32 visibleCenterXNew = Math.Min(newWidth, this.pnlImageScroll.ClientRectangle.Width) / 2;
            Int32 visibleCenterYNew = Math.Min(newHeight, this.pnlImageScroll.ClientRectangle.Height) / 2;
            Int32 viewCenterActualX = (-frameLeftVal + visibleCenterXOld) / prevZoom;
            Int32 viewCenterActualY = (-frameUpVal + visibleCenterYOld) / prevZoom;
            Int32 viewCenterNewX = (-frameLeftVal + visibleCenterXNew) / prevZoom;
            Int32 viewCenterNewY = (-frameUpVal + visibleCenterYNew) / prevZoom;
            Int32 frameLeftValNew = visibleCenterXNew - (viewCenterActualX * currentZoom);
            Int32 frameUpValNew = visibleCenterYNew - (viewCenterActualY * currentZoom);
            this.pnlImageScroll.SetDisplayRectLoc(frameLeftValNew, frameUpValNew);
            this.pnlImageScroll.PerformLayout();
        }

        private void RefreshColorControls()
        {
            SupportedFileType loadedFile = this.GetShownFile();
            Boolean fileLoaded = loadedFile != null;
            ColorStatus cs = this.GetColorStatus();
            this.btnSavePalette.Enabled = fileLoaded && cs != ColorStatus.None;
            this.cmbPalettes.Enabled = cs == ColorStatus.External;
            // Ignore this if the palette is handled by the dropdown
            Boolean resetEnabled;
            Color[] pal;
            switch (cs)
            {
                case ColorStatus.Internal:
                    resetEnabled = loadedFile.ColorsChanged();
                    pal = loadedFile.GetColors();
                    break;
                case ColorStatus.External:
                    PaletteDropDownInfo currentPal = this.cmbPalettes.SelectedItem as PaletteDropDownInfo;
                    resetEnabled = currentPal != null && currentPal.IsChanged();
                    if (fileLoaded) // && currentPal.Colors.Length != loadedFile.GetColors().Length)
                        pal = loadedFile.GetColors();
                    else
                        pal = currentPal.Colors;
                    break;
                default:
                    resetEnabled = false;
                    pal = new Color[0];
                    break;
            }
            this.btnResetPalette.Enabled = resetEnabled;
            Int32 bpp;
            if (loadedFile != null && cs != ColorStatus.None)
            {
                bpp = loadedFile.BitsPerPixel;
                // Fix for palettes larger than the colour depth would normally allow (can happen on png)
                while (1 << bpp < pal.Length)
                    bpp *= 2;
                //if (bpp == 2)
                //    bpp = 4;
                bpp = Math.Min(8, bpp);
            }
            else
            {
                bpp = 0;
            }
            PalettePanel.InitPaletteControl(bpp, this.palColorViewer, pal, PALETTE_DIM);
        }

        private void PicImage_Click(Object sender, EventArgs e)
        {
            this.pnlImageScroll.Focus();
        }

        private void LblTransparentColorVal_Click(Object sender, EventArgs e)
        {
            ColorDialog cdl = new ColorDialog();
            cdl.Color = this.m_BackgroundFillColor;
            cdl.FullOpen = true;
            cdl.CustomColors = this.m_CustomColors;
            DialogResult res = cdl.ShowDialog(this);
            this.m_CustomColors = cdl.CustomColors;
            if (res == DialogResult.OK || res == DialogResult.Yes)
            {
                this.m_BackgroundFillColor = cdl.Color;
                this.lblTransparentColorVal.BackColor = this.m_BackgroundFillColor;
                this.picImage.BackColor = this.m_BackgroundFillColor;
                this.NumZoom_ValueChanged(null, null);
            }
        }

        private void numFrame_ValueChanged(Object sender, EventArgs e)
        {
            if (this.m_LoadedFile != null && this.m_LoadedFile.Frames != null && this.m_LoadedFile.Frames.Length > 0)
                this.ReloadUi(false);
        }

        private void TsmiToHeightMapAdv_Click(Object sender, EventArgs e)
        {
            this.GenerateHeightMap(true);
        }

        private void TsmiToHeightMap_Click(Object sender, EventArgs e)
        {
            this.GenerateHeightMap(false);
        }

        private void GenerateHeightMap(Boolean selectHeightMap)
        {
            if (!(this.m_LoadedFile is FileMapWwCc1Pc))
                return;
            String loadedPath = this.m_LoadedFile.LoadedFile;
            String baseFileName = Path.Combine(Path.GetDirectoryName(loadedPath), Path.GetFileNameWithoutExtension(loadedPath));
            String pngFileName = baseFileName + ".png";
            Bitmap plateauImage = null;
            if (selectHeightMap)
            {
                SupportedFileType selectedType;
                //String plateauFileName = Path.Combine(Path.GetDirectoryName(m_Filename), Path.GetFileNameWithoutExtension(m_Filename)) + "_lvl.png";
                String filename = FileDialogGenerator.ShowOpenFileFialog(this, "Select height levels image", new Type[] {typeof (FileImage)}, pngFileName, "images", null, out selectedType);
                if (filename == null)
                    return;
                try
                {
                    if (selectedType == null)
                        selectedType = new FileImage();
                    Byte[] fileData = File.ReadAllBytes(filename);
                    selectedType.LoadFile(fileData, filename);
                    plateauImage = selectedType.GetBitmap();
                }
                catch (Exception e)
                {
                    MessageBox.Show(this, "Could not load file as " + selectedType.ShortTypeDescription + ":\n\n" + e.Message, GetTitle(false), MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                if (plateauImage.Width != 64 || plateauImage.Height != 64)
                {
                    MessageBox.Show(this, "Height levels image needs to be 64x64!", GetTitle(false), MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
            }
            FileMapWwCc1Pc map = (FileMapWwCc1Pc) this.m_LoadedFile;
            this.m_LoadedFile = HeightMapGenerator.GenerateHeightMapImage64x64(map, plateauImage, null);
            this.ReloadUi(false);
        }

        private void TsmiTo65x65HeightMap_Click(Object sender, EventArgs e)
        {
            if (this.m_LoadedFile == null || this.m_LoadedFile.Width != 64 || this.m_LoadedFile.Height != 64 || !(this.m_LoadedFile is FileImage))
                return;
            String baseFileName = Path.Combine(Path.GetDirectoryName(this.m_LoadedFile.LoadedFile), Path.GetFileNameWithoutExtension(this.m_LoadedFile.LoadedFile));
            String imgFileName = baseFileName + ".img";
            Bitmap bm = HeightMapGenerator.GenerateHeightMapImage65x65(this.m_LoadedFile.GetBitmap());
            //Byte[] imageData = ImageUtils.GetSavedImageData(bm, ref imgFileName);
            FileImgWwN64 file = new FileImgWwN64();
            file.LoadGrayImage(bm, Path.GetFileName(imgFileName), imgFileName);
            this.m_LoadedFile = file;
            this.ReloadUi(false);
        }

        private void TsmiToPlateaus_Click(Object sender, EventArgs e)
        {
            if (!(this.m_LoadedFile is FileMapWwCc1Pc))
                return;
            this.m_LoadedFile = HeightMapGenerator.GeneratePlateauImage64x64((FileMapWwCc1Pc) this.m_LoadedFile, "_lvl");
            this.ReloadUi(false);
        }

        private void CmbPalettes_SelectedIndexChanged(Object sender, EventArgs e)
        {
            if (this.GetColorStatus() != ColorStatus.External)
                return;
            PaletteDropDownInfo currentPal = this.cmbPalettes.SelectedItem as PaletteDropDownInfo;
            Color[] targetPal;
            if (currentPal == null)
            {
                if (!this.btnSavePalette.Enabled)
                    this.btnSavePalette.Enabled = true;
                targetPal = PaletteUtils.GenerateGrayPalette(8, null, false);
            }
            else
            {
                targetPal = currentPal.Colors;
                Int32 bpp = currentPal.BitsPerPixel;
                if (this.btnSavePalette.Enabled && bpp == 1)
                    this.btnSavePalette.Enabled = false;
                else if (!this.btnSavePalette.Enabled && bpp != 1)
                    this.btnSavePalette.Enabled = true;
                this.btnResetPalette.Enabled = currentPal.IsChanged();
            }
            SupportedFileType loadedFile = this.GetShownFile();
            if (loadedFile == null)
                this.picImage.Image = null;
            else
            {
                loadedFile.SetColors(targetPal);
                this.picImage.Image = loadedFile.GetBitmap();
            }
            this.RefreshImage(false);
            this.RefreshColorControls();
        }

        private void BtnResetPalette_Click(Object sender, EventArgs e)
        {
            ColorStatus cs = this.GetColorStatus();
            if (cs == ColorStatus.None)
                return;
            Color[] colors;
            switch (cs)
            {
                case ColorStatus.Internal:
                    this.GetShownFile().ResetColors();
                    colors = this.GetShownFile().GetColors();
                    break;
                case ColorStatus.External:
                    PaletteDropDownInfo currentPal = this.cmbPalettes.SelectedItem as PaletteDropDownInfo;
                    if (currentPal == null)
                        return;
                    if (currentPal.SourceFile != null && currentPal.Entry >= 0)
                    {
                        DialogResult dr = MessageBox.Show("This will remove all changes you have made to the palette since it was loaded!\n\nAre you sure you want to continue?", GetTitle(false), MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                        if (dr != DialogResult.Yes)
                            return;
                    }
                    currentPal.Revert();
                    colors = currentPal.Colors;
                    this.GetShownFile().SetColors(colors);
                    break;
                default:
                    return;
            }
            SupportedFileType shownFile = this.GetShownFile();
            this.picImage.Image = shownFile.GetBitmap();
            this.RefreshImage(false);
            this.RefreshColorControls();
        }

        private void BtnSavePalette_Click(Object sender, EventArgs e)
        {
            ColorStatus cs = this.GetColorStatus();
            if (cs == ColorStatus.None)
                return;
            SupportedFileType loadedFile = this.GetShownFile();
            if (loadedFile.BitsPerPixel < 4)
                return;
            PaletteDropDownInfo currentPal;
            switch (cs)
            {
                case ColorStatus.External:
                    currentPal = this.cmbPalettes.SelectedItem as PaletteDropDownInfo;
                    if (currentPal == null)
                        return;
                    break;
                case ColorStatus.Internal:
                    currentPal = new PaletteDropDownInfo(null, loadedFile.BitsPerPixel, loadedFile.GetColors(), null, -1, false, false);
                    break;
                default:
                    return;
            }
            FrmManagePalettes palSave = new FrmManagePalettes(currentPal.BitsPerPixel);
            palSave.Icon = this.Icon;
            palSave.Title = GetTitle(false);
            palSave.PaletteToSave = currentPal;
            palSave.StartPosition = FormStartPosition.CenterParent;
            DialogResult dr = palSave.ShowDialog(this);
            if (dr != DialogResult.OK || cs == ColorStatus.Internal)
            {
                this.RefreshPalettes(true, true);
                this.RefreshColorControls();
                return;
            }
            // If null, it was a simple immediate overwrite, without the management box ever popping up, so
            // just consider the current entry "saved".
            if (palSave.PaletteToSave == null)
                currentPal.ClearRevert();
            else
            {
                // Get source position, reload all, then loop through to check which one to reselect.
                this.RefreshPalettes(true, true);
                String source = palSave.PaletteToSave.SourceFile;
                Int32 index = palSave.PaletteToSave.Entry;
                foreach (PaletteDropDownInfo pdd in this.cmbPalettes.Items)
                {
                    if (pdd.SourceFile != source || pdd.Entry != index)
                        continue;
                    this.cmbPalettes.SelectedItem = pdd;
                    break;
                }
            }
        }

        private void RefreshPalettes(Boolean forced, Boolean reloadFiles)
        {
            Int32 oldBpp = -1;
            PaletteDropDownInfo currentPal = this.cmbPalettes.SelectedItem as PaletteDropDownInfo;
            if (currentPal != null)
                oldBpp = currentPal.BitsPerPixel;
            SupportedFileType shown = this.GetShownFile();
            if (this.GetColorStatus() == ColorStatus.Internal)
            {
                // Shows text on the disabled control.
                this.cmbPalettes.DataSource = null;
                this.cmbPalettes.Items.Clear();
                this.cmbPalettes.Items.Add(this.GetColorStatus() == ColorStatus.Internal ? "Inbuilt palette" : "None");
                this.cmbPalettes.SelectedIndex = 0;
                return;
            }
            Int32 bpp = shown == null ? 0 : shown.BitsPerPixel;
            // Don't reload if it was the same :)
            if (oldBpp != -1 && oldBpp == bpp && !forced)
                return;
            Int32 index = -1;
            List<PaletteDropDownInfo> bppPalettes = this.GetPalettes(bpp, reloadFiles);
            if (forced && oldBpp != -1 && oldBpp == bpp && currentPal != null)
                index = bppPalettes.FindIndex(x => x.Name == currentPal.Name);
            if (bppPalettes.Count == 0)
                bppPalettes.Add(new PaletteDropDownInfo("None", -1, PaletteUtils.GenerateGrayPalette(8, null, false), null, -1, false, false));
            this.cmbPalettes.DataSource = bppPalettes;
            if (index >= 0)
                this.cmbPalettes.SelectedIndex = index;
        }

        private void PnlImageScroll_MouseScroll(Object sender, MouseEventArgs e)
        {
            Keys k = ModifierKeys;
            if ((k & Keys.Control) != 0)
            {
                this.numZoom.EnteredValue = this.numZoom.Constrain(this.numZoom.EnteredValue + (e.Delta / 120));
                HandledMouseEventArgs args = e as HandledMouseEventArgs;
                if (args != null)
                    args.Handled = true;
            }
        }

        private void PalColorViewer_ColorLabelMouseDoubleClick(Object sender, PaletteClickEventArgs e)
        {
            SupportedFileType loadedFile = this.GetShownFile();
            if (e.Button != MouseButtons.Left)
                return;
            PalettePanel palpanel = sender as PalettePanel;
            if (palpanel == null)
                return;
            Int32 colindex = e.Index;
            ColorDialog cdl = new ColorDialog();
            cdl.Color = e.Color;
            cdl.FullOpen = true;
            cdl.CustomColors = this.m_CustomColors;
            DialogResult res = cdl.ShowDialog(this);
            this.m_CustomColors = cdl.CustomColors;
            if (res == DialogResult.OK)
            {
                this.SetPaletteColor(palpanel, colindex, cdl.Color, loadedFile);
            }
        }

        private void SetPaletteColor(PalettePanel palpanel, Int32 colindex, Color color, SupportedFileType loadedFile)
        {
            ColorStatus cs = this.GetColorStatus();
            palpanel.Palette[colindex] = color;
            if (cs != ColorStatus.None)
            {
                Color[] pal = loadedFile.GetColors();
                if (pal.Length > colindex)
                    pal[colindex] = color;
                loadedFile.SetColors(pal);
                if (cs == ColorStatus.External)
                {
                    PaletteDropDownInfo currentPal = this.cmbPalettes.SelectedItem as PaletteDropDownInfo;
                    if (currentPal != null && currentPal.Colors.Length > colindex)
                        currentPal.Colors[colindex] = color;
                }
                SupportedFileType shownFile = this.GetShownFile();
                this.picImage.Image = shownFile.GetBitmap();
            }
            this.RefreshImage(false);
            this.RefreshColorControls();
        }


        private void PalColorViewer_ColorLabelMouseClick(Object sender, PaletteClickEventArgs e)
        {
            if (e.Button != MouseButtons.Right)
                return;
            ContextMenu cm = new ContextMenu();
            if (this.palColorViewer.Palette.Length <= e.Index)
                return;
            MenuItem miEd = new MenuItem("Edit...", this.EditColor);
            miEd.Tag = e.Index;
            cm.MenuItems.Add(miEd);
            MenuItem miTr = new MenuItem("Set transparent", this.SetColorTransparent);
            miTr.Tag = e.Index;
            cm.MenuItems.Add(miTr);
            MenuItem miOp = new MenuItem("Set opaque", this.SetColorOpaque);
            miOp.Tag = e.Index;
            cm.MenuItems.Add(miOp);
            MenuItem miAl = new MenuItem("Set alpha...", this.SetColorAlpha);
            miAl.Tag = e.Index;
            cm.MenuItems.Add(miAl);

            cm.Show((Control)sender, e.Location);
        }

        private void EditColor(Object sender, EventArgs e)
        {
            MenuItem cm = sender as MenuItem;
            if (cm == null)
                return;
            if (!(cm.Tag is Int32))
                return;
            Int32 index = (Int32)cm.Tag;
            ColorDialog cdl = new ColorDialog();
            cdl.Color = this.palColorViewer.Palette[index];
            cdl.FullOpen = true;
            cdl.CustomColors = this.m_CustomColors;
            DialogResult res = cdl.ShowDialog(this);
            this.m_CustomColors = cdl.CustomColors;
            if (res != DialogResult.OK)
                return;
            SupportedFileType loadedFile = this.GetShownFile();
            this.SetPaletteColor(this.palColorViewer, index, cdl.Color, loadedFile);
        }

        private void SetColorTransparent(Object sender, EventArgs e)
        {
            this.SetPalColorAlpha(sender, e, 0);
        }
        
        private void SetColorOpaque(Object sender, EventArgs e)
        {
            this.SetPalColorAlpha(sender, e, 255);
        }

        private void SetColorAlpha(Object sender, EventArgs e)
        {
            MenuItem cm = sender as MenuItem;
            if (cm == null)
                return;
            if (!(cm.Tag is Int32))
                return;
            Int32 index = (Int32)cm.Tag;
            Color col = this.palColorViewer.Palette[index];
            FrmSetAlpha alphaForm = new FrmSetAlpha(col.A);
            if (alphaForm.ShowDialog(this) != DialogResult.OK)
                return;
            col = Color.FromArgb(alphaForm.Alpha, col);
            SupportedFileType loadedFile = this.GetShownFile();
            this.SetPaletteColor(this.palColorViewer, index, col, loadedFile);
        }

        private void SetPalColorAlpha(Object sender, EventArgs e, Int32 alpha)
        {
            MenuItem cm = sender as MenuItem;
            if (cm == null)
                return;
            if (!(cm.Tag is Int32))
                return;
            Int32 index = (Int32)cm.Tag;
            Color col = this.palColorViewer.Palette[index];
            col = Color.FromArgb(alpha, col);
            SupportedFileType loadedFile = this.GetShownFile();
            this.SetPaletteColor(this.palColorViewer, index, col, loadedFile);
        }

        private enum ColorStatus
        {
            None,
            Internal,
            External
        }

        private void TsmiSplitShadows_Click(Object sender, EventArgs e)
        {
            if (this.m_LoadedFile == null)
                return;
            try
            {
                SupportedFileType oldFile = this.m_LoadedFile;
                this.m_LoadedFile = FileFramesWwShpCc.SplitShadows(this.m_LoadedFile, 4, 4);
                this.ReloadUi(true);
                oldFile.Dispose();
            }
            catch (NotSupportedException ex)
            {
                MessageBox.Show(this, ex.Message, GetTitle(false), MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void tsmiCombineShadows_Click(Object sender, EventArgs e)
        {
#if DEBUG
            TestBed();
#endif
        }

#if DEBUG
        private void TestBed()
        {
            ViewKortExeIcons();
        }

        private void ViewKortExeIcons()
        {
            // any test code can come here
            Byte[] oneBppImage = new Byte[] {
                0xFF, 0x1F, 0xFF, 0x0F, 0xFF, 0x07, 0xFF, 0x03, 0xFF, 0x01, 0xFF, 0x00, 0x7F, 0x00, 0x3F, 0x00, 
                0x1F, 0x00, 0x3F, 0x00, 0xFF, 0x01, 0xFF, 0x01, 0xFF, 0xE0, 0xFF, 0xF0, 0xFF, 0xF8, 0xFF, 0xF8, 
                0x00, 0x00, 0x00, 0x40, 0x00, 0x60, 0x00, 0x70, 0x00, 0x78, 0x00, 0x7C, 0x00, 0x7E, 0x00, 0x7F, 
                0x80, 0x7F, 0x00, 0x7C, 0x00, 0x4C, 0x00, 0x06, 0x00, 0x06, 0x00, 0x03, 0x00, 0x03, 0x00, 0x00, 
                0xF0, 0xFF, 0xE0, 0xFF, 0xC0, 0xFF, 0x81, 0xFF, 0x03, 0xFF, 0x07, 0x06, 0x0F, 0x00, 0x1F, 0x00, 
                0x3F, 0x80, 0x7F, 0xC0, 0xFF, 0xE0, 0xFF, 0xF1, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 
                0x00, 0x00, 0x06, 0x00, 0x0C, 0x00, 0x18, 0x00, 0x30, 0x00, 0x60, 0x00, 0xC0, 0x70, 0x80, 0x39, 
                0x00, 0x1F, 0x00, 0x0E, 0x00, 0x04, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 
                0x1F, 0xF0, 0x0F, 0xE0, 0x07, 0xC0, 0x03, 0x80, 0x41, 0x04, 0x61, 0x0C, 0x81, 0x03, 0x81, 0x03, 
                0x81, 0x03, 0x61, 0x0C, 0x41, 0x04, 0x03, 0x80, 0x07, 0xC0, 0x0F, 0xE0, 0x10, 0xF0, 0xFF, 0xFF, 
                0x00, 0x00, 0xC0, 0x07, 0x20, 0x09, 0x10, 0x11, 0x08, 0x21, 0x04, 0x40, 0x04, 0x40, 0x3C, 0x78, 
                0x04, 0x40, 0x04, 0x40, 0x08, 0x21, 0x10, 0x11, 0x20, 0x09, 0xC0, 0x07, 0x00, 0x00, 0x00, 0x00, 
                0xFF, 0xF3, 0xFF, 0xE1, 0xFF, 0xE1, 0xFF, 0xE1, 0xFF, 0xE1, 0x49, 0xE0, 0x00, 0xE0, 0x00, 0x80, 
                0x00, 0x00, 0x00, 0x00, 0xFC, 0x07, 0xF8, 0x07, 0xF9, 0x9F, 0xF1, 0x8F, 0x03, 0xC0, 0x00, 0xE0, 
                0x00, 0x0C, 0x00, 0x12, 0x00, 0x12, 0x00, 0x12, 0x00, 0x12, 0xB6, 0x13, 0x49, 0x12, 0x49, 0x72, 
                0x49, 0x92, 0x01, 0x90, 0x01, 0x90, 0x01, 0x80, 0x02, 0x40, 0x02, 0x40, 0x04, 0x20, 0xF8, 0x1F, 
                0xFF, 0x8F, 0xFF, 0x07, 0xFF, 0x03, 0xFF, 0x01, 0xFB, 0x80, 0x71, 0xC0, 0x31, 0xE0, 0x11, 0xF0, 
                0x01, 0xF8, 0x03, 0xFC, 0x07, 0xFE, 0x03, 0xFF, 0x01, 0xF8, 0x20, 0xF0, 0x70, 0xF8, 0xF9, 0xFF, 
                0x00, 0x00, 0x00, 0x70, 0x00, 0x78, 0x00, 0x5C, 0x00, 0x2E, 0x04, 0x17, 0x84, 0x0B, 0xC4, 0x05, 
                0xEC, 0x02, 0x78, 0x01, 0xB0, 0x00, 0x68, 0x00, 0xD4, 0x00, 0x8A, 0x07, 0x04, 0x00, 0x00, 0x00, 
                0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 
                0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 
                0x00, 0x00, 0x6A, 0x69, 0x4C, 0x49, 0x4C, 0x49, 0x6A, 0x6D, 0x00, 0x00, 0xE0, 0x0E, 0xA0, 0x04, 
                0xA0, 0x04, 0xE0, 0x04, 0x00, 0x00, 0xAE, 0x6E, 0xA4, 0x4A, 0xE4, 0x4A, 0xA4, 0x6E, 0x00, 0x00 };

            Color[] palette = new Color[4];
            palette[0] = Color.Black;
            palette[1] = Color.FromArgb(0, 255, 0, 255);
            palette[2] = Color.White;
            palette[3] = Color.Red;
            ColorPalette pal = BitmapHandler.GetPalette(palette);
            Int32 frames = oneBppImage.Length / 64;
            Int32 fullWidth = 16;
            Int32 fullHeight = frames * 16;
            Int32 fullStride = 16;
            Byte[] fullImage = new Byte[fullStride * fullHeight];
            FileFrames framesContainer = new FileFrames();
            for (Int32 i = 0; i < frames; i ++)
            {
                Int32 start = i * 64;
                Int32 start2 = start + 32;
                Byte[] curImage1 = new Byte[32];
                for (Int32 j = 0; j < 32; j += 2)
                {
                    curImage1[j] = oneBppImage[start + j + 1];
                    curImage1[j + 1] = oneBppImage[start + j];
                }
                Int32 stride1 = 2;
                curImage1 = ImageUtils.ConvertTo8Bit(curImage1, 16, 16, 0, 1, true, ref stride1);

                Byte[] curImage2 = new Byte[32];
                for (Int32 j = 0; j < 32; j += 2)
                {
                    curImage2[j] = oneBppImage[start2 + j + 1];
                    curImage2[j + 1] = oneBppImage[start2 + j];
                }
                Int32 stride2 = 2;
                curImage2 = ImageUtils.ConvertTo8Bit(curImage2, 16, 16, 0, 1, true, ref stride2);

                Byte[] imageFinal = new Byte[256];
                Int32 strideFinal = 16;
                for (Int32 j = 0; j < 256; j++)
                {
                    imageFinal[j] = (Byte)((curImage2[j] << 1) | curImage1[j]);
                }
                ImageUtils.PasteOn8bpp(fullImage, fullWidth, fullHeight, fullWidth, imageFinal, 16, 16, strideFinal, new Rectangle(0, i * 16, 16, 16), null, true);
                imageFinal = ImageUtils.ConvertFrom8Bit(imageFinal, 16, 16, 4, true, ref strideFinal);
                Bitmap frameImage = ImageUtils.BuildImage(imageFinal, 16,16,strideFinal, PixelFormat.Format4bppIndexed, palette, Color.Empty);
                frameImage.Palette = pal;
                

                FileImageFrame frame = new FileImageFrame();
                frame.LoadFileFrame(framesContainer, "Icon", frameImage, "Icon" + i.ToString("D3") + ".png", -1);
                frame.SetBitsPerColor(2);
                frame.SetColorsInPalette(4);
                framesContainer.AddFrame(frame);
            }
            fullImage = ImageUtils.ConvertFrom8Bit(fullImage, fullWidth, fullHeight, 4, true, ref fullStride);
            Bitmap composite = ImageUtils.BuildImage(fullImage, fullWidth, fullHeight, fullStride, PixelFormat.Format4bppIndexed, palette, Color.Empty);
            composite.Palette = pal;
            framesContainer.SetCompositeFrame(composite);
            framesContainer.SetBitsPerColor(2);
            framesContainer.SetColorsInPalette(4);
            framesContainer.SetCommonPalette(true);
            SupportedFileType oldFile = this.m_LoadedFile;
            this.m_LoadedFile = framesContainer;
            this.AutoSetZoom();
            this.ReloadUi(true);
            if (oldFile != null)
                oldFile.Dispose();
        }
#endif
    }
}
