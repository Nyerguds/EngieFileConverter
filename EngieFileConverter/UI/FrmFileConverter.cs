using Nyerguds.ImageManipulation;
using Nyerguds.Util;
using Nyerguds.Util.UI;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using EngieFileConverter.Domain.FileTypes;
using EngieFileConverter.Domain.HeightMap;

namespace EngieFileConverter.UI
{
    public partial class FrmFileConverter : Form
    {
        private const String PROG_NAME = "Engie File Converter";
        private const String PROG_AUTHOR = "Created by Nyerguds";
        private const Int32 PALETTE_DIM = 226;

        private String m_StartupParamPath;
        private List<PaletteDropDownInfo> m_DefaultPalettes;
        private List<PaletteDropDownInfo> m_ReadPalettes;
        private SupportedFileType m_LoadedFile;
        private String m_LastOpenedFolder;

        private SupportedFileType GetShownFile()
        {
            if (this.m_LoadedFile == null)
                return null;
            Boolean hasFrames = this.m_LoadedFile.Frames != null && this.m_LoadedFile.Frames.Length > 0;
            return hasFrames && this.numFrame.Value != -1 ? (this.m_LoadedFile.Frames.Length > this.numFrame.Value ? this.m_LoadedFile.Frames[(Int32) this.numFrame.Value] : null) : this.m_LoadedFile;
        }

        public FrmFileConverter()
        {
            this.InitializeComponent();
            this.Text = GetTitle(true);
            PalettePanel.InitPaletteControl(0, this.palColorViewer, new Color[0], PALETTE_DIM);
            this.m_DefaultPalettes = this.LoadDefaultPalettes();
            this.m_ReadPalettes = this.LoadExtraPalettes();
            this.RefreshPalettes(false, false);
#if DEBUG
            this.tsmiTestBed.Visible = true;
#endif
        }

        public static String GetTitle(Boolean withAuthor)
        {
            String title = PROG_NAME + " " + GeneralUtils.ProgramVersion();
            if (withAuthor)
                title += " - " + PROG_AUTHOR;
            return title;
        }


        public FrmFileConverter(String[] args)
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

        private void tsmiCopy_Click(Object sender, EventArgs e)
        {
            this.pzpImage.CopyToClipboard();
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
            this.m_LastOpenedFolder = Path.GetDirectoryName(path);
            this.LoadFile(path, null, preferredTypes);
        }

        private void LoadFile(String path, SupportedFileType selectedType, SupportedFileType[] preferredTypes)
        {
            SupportedFileType oldLoaded = this.m_LoadedFile;
            Byte[] fileData = null;
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
                            MessageBox.Show(this, "File type of " + filename + " could not be identified. Errors returned by all attempts:\n\n" + errors, GetTitle(false), MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            return;
                        }
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
            SupportedFileType fr = FileFrames.CheckForFrames(path, currentType, out minName, out maxName, out hasEmptyFrames);
            if (fr == null)
                return currentType;
            String emptywarning = hasEmptyFrames ? "\nSome of these frames are empty files. Not every save format supports empty frames." : String.Empty;
            String message = "The file appears to be part of a range (" + minName + " - " + maxName + ")." + emptywarning + "\n\nDo you wish to load the frames from all files?";
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
            this.pzpImage.ImageVisible = false;
            Size maxSize = this.pzpImage.MaxImageSize;
            Int32 maxWidth = maxSize.Width;
            Int32 maxHeight = maxSize.Height;
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
            this.pzpImage.ZoomFactor = minZoomFactor;
        }

        private void ReloadUi(Boolean fromNewFile)
        {
            Boolean hasFrames = this.m_LoadedFile != null && this.m_LoadedFile.Frames != null && this.m_LoadedFile.Frames.Length > 0;
            this.lblFrame.Enabled = hasFrames;
            this.numFrame.Enabled = hasFrames;
            this.numFrame.Minimum = -1;
            Int32 frames = 0;
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
                frames = this.m_LoadedFile.Frames.Length;
                Int32 last = frames - 1;
                this.numFrame.Maximum = last;
                this.lblNrOfFrames.Visible = true;
                this.lblNrOfFrames.Text = "/ " + last;
                if (last >= 0 && !this.m_LoadedFile.IsFramesContainer)
                    this.numFrame.Minimum = 0;
            }
            SupportedFileType loadedFile = this.GetShownFile();
            Boolean hasFile = loadedFile != null;
            this.tsmiSave.Enabled = hasFile;
            Boolean canExportFrames = this.m_LoadedFile != null && (this.m_LoadedFile.FileClass & FileClass.FrameSet) != 0;
            this.tsmiSaveFrames.Enabled = canExportFrames;
            this.tsmiFramesToSingleImage.Enabled = canExportFrames;
            this.tsmiCopy.Enabled = hasFile;

            // General frame tools
            this.tsmiImageToFrames.Enabled = hasFile && loadedFile.GetBitmap() != null;
            this.tsmiFramesToSingleImage.Enabled = canExportFrames;
            // C&C64 toolsets
            this.tsmiToHeightMap.Enabled = loadedFile is FileMapWwCc1Pc;
            this.tsmiToPlateaus.Enabled = loadedFile is FileMapWwCc1Pc;
            this.tsmiToHeightMapAdv.Enabled = loadedFile is FileMapWwCc1Pc;
            this.tsmiTo65x65HeightMap.Enabled = hasFile && loadedFile.GetBitmap() != null && loadedFile.Width == 64 && loadedFile.Height == 64;
            // Tiberian Sun shadow tools
            this.tsmiCombineShadows.Enabled = hasFrames && frames > 0 && frames % 2 == 0;
            this.tsmiSplitShadows.Enabled = hasFrames && frames > 0;
            // General animations "paste on frames" option.
            this.tsmiPasteOnFrames.Enabled = hasFrames && frames > 0;
            
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
                this.pzpImage.Image = null;
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
            this.pzpImage.Image = image;
            this.RefreshPalettes(true, fromNewFile);
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

        public List<PaletteDropDownInfo> GetPalettes(Int32 bpp, Boolean reloadFiles, Boolean[] typeTransModifier)
        {
            List<PaletteDropDownInfo> allPalettes = this.m_DefaultPalettes.Where(p => p.BitsPerPixel == bpp).ToList();
            if (reloadFiles)
                this.m_ReadPalettes = this.LoadExtraPalettes();
            allPalettes.AddRange(this.m_ReadPalettes.Where(p => p.BitsPerPixel == bpp));
            foreach (PaletteDropDownInfo info in allPalettes)
                info.Colors = PaletteUtils.ApplyTransparencyGuide(info.Colors, typeTransModifier);
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

        private void FrmFileConverter_Shown(Object sender, EventArgs e)
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
            String filename = FileDialogGenerator.ShowOpenFileFialog(this, null, SupportedFileType.SupportedOpenTypes, this.m_LastOpenedFolder, "images", null, out selectedItem);
            if (filename == null)
                return;
            this.m_LastOpenedFolder = Path.GetDirectoryName(filename);
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

        private Boolean[] GetCurrentTypeTransparencyMask()
        {
            return this.m_LoadedFile == null ? null : this.m_LoadedFile.TransparencyMask;
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
                    resetEnabled = currentPal != null && currentPal.IsChanged(this.GetCurrentTypeTransparencyMask());
                    if (fileLoaded) // && currentPal.Colors.Length != loadedFile.GetColors().Length)
                        pal = loadedFile.GetColors();
                    else
                        pal = currentPal != null ? currentPal.Colors : new Color[0];
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
        
        private void numFrame_ValueChanged(Object sender, EventArgs e)
        {
            if (this.m_LoadedFile != null && this.m_LoadedFile.Frames != null && this.m_LoadedFile.Frames.Length > 0)
                this.ReloadUi(false);
        }

        private void CmbPalettes_SelectedIndexChanged(Object sender, EventArgs e)
        {
            if (this.GetColorStatus() != ColorStatus.External)
                return;
            PaletteDropDownInfo currentPal = this.cmbPalettes.SelectedItem as PaletteDropDownInfo;
            Color[] targetPal;
            SupportedFileType loadedFile = this.GetShownFile();
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
                this.btnResetPalette.Enabled = currentPal.IsChanged(this.GetCurrentTypeTransparencyMask());
            }
            if (loadedFile == null)
                this.pzpImage.Image = null;
            else
            {
                loadedFile.SetColors(targetPal);
                this.pzpImage.Image = loadedFile.GetBitmap();
            }
            this.pzpImage.RefreshImage();
            this.RefreshColorControls();
        }

        private void BtnResetPalette_Click(Object sender, EventArgs e)
        {
            ColorStatus cs = this.GetColorStatus();
            if (cs == ColorStatus.None)
                return;
            switch (cs)
            {
                case ColorStatus.Internal:
                    this.GetShownFile().ResetColors();
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
                    currentPal.Revert(this.GetCurrentTypeTransparencyMask());
                    Color[] colors = currentPal.Colors;
                    this.GetShownFile().SetColors(colors);
                    break;
                default:
                    return;
            }
            SupportedFileType shownFile = this.GetShownFile();
            this.pzpImage.Image = shownFile.GetBitmap();
            this.pzpImage.RefreshImage();
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
            palSave.SuggestedSaveName = this.m_LoadedFile.LoadedFile ?? this.m_LoadedFile.LoadedFileName;
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
            List<PaletteDropDownInfo> bppPalettes = this.GetPalettes(bpp, reloadFiles, this.GetCurrentTypeTransparencyMask());
            if (forced && oldBpp != -1 && oldBpp == bpp && currentPal != null)
                index = bppPalettes.FindIndex(x => x.Name == currentPal.Name);
            if (bppPalettes.Count == 0)
                bppPalettes.Add(new PaletteDropDownInfo("None", -1, PaletteUtils.GenerateGrayPalette(8, null, false), null, -1, false, false));
            this.cmbPalettes.DataSource = bppPalettes;
            if (index >= 0)
                this.cmbPalettes.SelectedIndex = index;
        }

        protected override Boolean ProcessCmdKey(ref Message msg, Keys keyData)
        {
            // override of menu shortcuts to allow copying and pasting text in the preview text field and numeric up/down controls.
            Boolean isCtrlC = keyData == (Keys.Control | Keys.C);
            Boolean isCtrlV = keyData == (Keys.Control | Keys.V);
            Boolean isCtrlX = keyData == (Keys.Control | Keys.X);
            Boolean isCtrlA = keyData == (Keys.Control | Keys.A);
            Boolean isCtrlZ = keyData == (Keys.Control | Keys.Z);
            if (!isCtrlC && !isCtrlV && !isCtrlX && !isCtrlA && !isCtrlZ)
                return base.ProcessCmdKey(ref msg, keyData);
            TextBox tb = this.ActiveControl as TextBox;
            EnhNumericUpDown num = this.ActiveControl as EnhNumericUpDown;
            if (tb == null && num == null)
                return base.ProcessCmdKey(ref msg, keyData);
            if (tb == null)
            {
                if (isCtrlC)
                {
                    if (String.IsNullOrEmpty(num.SelectedText))
                        return base.ProcessCmdKey(ref msg, keyData);
                    Clipboard.SetText(num.SelectedText);
                }
                else if (isCtrlV)
                {
                    num.SelectedText = Clipboard.GetText();
                }
                else if (isCtrlX)
                {
                    Clipboard.SetText(num.SelectedText);
                    num.SelectedText = String.Empty;
                }
                else if (isCtrlA)
                {
                    num.SelectAll();
                }
                else // if (isCtrlZ)
                    num.TextBox.Undo();
            }
            else
            {
                if (isCtrlC)
                {
                    if (String.IsNullOrEmpty(tb.SelectedText))
                        return base.ProcessCmdKey(ref msg, keyData);
                    Clipboard.SetText(tb.SelectedText);
                }
                else if (isCtrlV)
                {
                    tb.SelectedText = Clipboard.GetText();
                }
                else if (isCtrlX)
                {
                    Clipboard.SetText(tb.SelectedText);
                    tb.SelectedText = String.Empty;
                }
                else if (isCtrlA)
                {
                    tb.SelectionStart = 0;
                    tb.SelectionLength = tb.TextLength;
                }
                else // if (isCtrlZ)
                    tb.Undo();
            }
            return true;
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
            cdl.CustomColors = this.pzpImage.CustomColors;
            DialogResult res = cdl.ShowDialog(this);
            this.pzpImage.CustomColors = cdl.CustomColors;
            if (res == DialogResult.OK)
                this.SetPaletteColor(palpanel, colindex, cdl.Color, loadedFile);
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
                this.pzpImage.Image = shownFile.GetBitmap();
            }
            this.pzpImage.RefreshImage();
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
            cdl.CustomColors = this.pzpImage.CustomColors;
            DialogResult res = cdl.ShowDialog(this);
            this.pzpImage.CustomColors = cdl.CustomColors;
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
        
        private void TsmiImageToFramesClick(Object sender, EventArgs e)
        {
            SupportedFileType shownFile = this.GetShownFile();
            if (shownFile == null)
                return;
            Bitmap image = shownFile.GetBitmap();

            List<PaletteDropDownInfo> allPalettes = new List<PaletteDropDownInfo>();
            allPalettes.AddRange(this.m_DefaultPalettes);
            allPalettes.AddRange(this.m_ReadPalettes);

            FrmFramesCutter frameCutter = new FrmFramesCutter(image, this.pzpImage.CustomColors, allPalettes.ToArray());
            frameCutter.CustomColors = this.pzpImage.CustomColors;
            DialogResult dr = frameCutter.ShowDialog();
            this.pzpImage.CustomColors = frameCutter.CustomColors;

            if (dr != DialogResult.OK)
                return;
            String imagePath = shownFile.LoadedFile;
            if (String.IsNullOrEmpty(imagePath))
                imagePath = shownFile.LoadedFileName;
            Int32 frameWidth = frameCutter.FrameWidth;
            Int32 frameHeight = frameCutter.FrameHeight;
            Int32 maxFrames = frameCutter.Frames;
            Color? trimColor = frameCutter.TrimColor;
            Int32? trimIndex = frameCutter.TrimIndex;
            Int32 matchBpp = frameCutter.MatchBpp;
            Color[] matchPalette = frameCutter.MatchPalette;
            FileFrames frames = FileFrames.CutImageIntoFrames(image, imagePath, frameWidth, frameHeight, maxFrames, trimColor, trimIndex, matchBpp, matchPalette);
            SupportedFileType oldFile = this.m_LoadedFile;
            this.m_LoadedFile = frames;
            this.AutoSetZoom();
            this.ReloadUi(true);
            oldFile.Dispose();
        }
        
        private void TsmiFramesToSingleImageClick(Object sender, EventArgs e)
        {
            if (this.m_LoadedFile == null || this.m_LoadedFile.Frames == null || this.m_LoadedFile.Frames.Length == 0)
                return;
            Bitmap[] frameImages = this.m_LoadedFile.Frames.Select(fr => fr.GetBitmap()).ToArray();


            PixelFormat highestPf = PixelFormat.Undefined;
            Int32 highestBpp = 0;
            Color[] palette = null;
            foreach (Bitmap img in frameImages)
            {
                if (img == null)
                    continue;
                PixelFormat curPf = img.PixelFormat;
                Int32 curBpp = Image.GetPixelFormatSize(curPf);
                if (curBpp <= highestBpp)
                    continue;
                highestPf = curPf;
                highestBpp = curBpp;
                if (highestBpp <= 8)
                    palette = img.Palette.Entries;
            }
            if (highestBpp == 0)
                return;
            Int32 maxWidth = frameImages.Select(p => p == null ? 0 : p.Width).Max();
            Int32 maxHeight = frameImages.Select(p => p == null ? 0 : p.Height).Max();

            Int32 frames = frameImages.Length;
            
            SaveOption[] so = new SaveOption[4];
            Boolean hasAlpha = true;
            Boolean hasSimpleTrans = false;
            String paletteStr = null;
            if (highestBpp == 16)
            {
                hasAlpha = false;
                hasSimpleTrans = (highestPf & PixelFormat.Alpha) != 0;
            }
            else if (highestBpp > 8 && (highestPf & PixelFormat.Alpha) == 0)
            {
                hasAlpha = false;
            }
            else if (highestBpp <= 8 && palette != null)
            {
                paletteStr = String.Join(",", palette.Select(c => ColorUtils.HexStringFromColor(c, false)).ToArray());
            }

            so[0] = new SaveOption("FRW", SaveOptionType.Number, "Frame width",maxWidth + ",", maxWidth.ToString());
            so[1] =     new SaveOption("FRH", SaveOptionType.Number, "Frame height",maxHeight + ",", maxHeight.ToString());
            so[2] =     new SaveOption("FPL", SaveOptionType.Number, "Frames per line","1," + frames, ((Int32)Math.Sqrt(frames)).ToString());
            if (highestBpp <= 8)
                so[3] = new SaveOption("BGI", SaveOptionType.Palette, "Background colour around frames", highestBpp + "|" + paletteStr, "0");
            else
                so[3] = new SaveOption("BGC", SaveOptionType.Color, "Background colour around frames", hasAlpha ? "A" : hasSimpleTrans ? "T" : String.Empty, "#00000000");

            SaveOptionInfo soi = new SaveOptionInfo();
            soi.Name = "Frames to single image";
            soi.Properties = so;
            FrmExtraOptions extraopts = new FrmExtraOptions();
            extraopts.Size = extraopts.MinimumSize;
            extraopts.Init(soi);
            if (extraopts.ShowDialog(this) != DialogResult.OK)
                return;
            so = extraopts.GetSaveOptions();
            Int32 frameWidth;
            Int32.TryParse(SaveOption.GetSaveOptionValue(so, "FRW"), out frameWidth);
            Int32 frameHeight;
            Int32.TryParse(SaveOption.GetSaveOptionValue(so, "FRH"), out frameHeight);
            Int32 framesPerLine;
            Int32.TryParse(SaveOption.GetSaveOptionValue(so, "FPL"), out framesPerLine);
            Byte fillPalIndex = 0;
            Color fillColor = Color.Empty;
            if (highestBpp <= 8)
                Byte.TryParse(SaveOption.GetSaveOptionValue(so, "BGI"), out fillPalIndex);
            else
                fillColor = ColorUtils.ColorFromHexString(SaveOption.GetSaveOptionValue(so, "BGC"));

            Bitmap bm = ImageUtils.BuildImageFromFrames(frameImages, frameWidth, frameHeight, framesPerLine, fillPalIndex, fillColor);
            SupportedFileType oldFile = this.m_LoadedFile;
            FileImagePng returnImg = new FileImagePng();
            returnImg.LoadFile(bm, this.m_LoadedFile.LoadedFile);
            this.m_LoadedFile = returnImg;
            this.AutoSetZoom();
            this.ReloadUi(true);
            oldFile.Dispose();
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
            FileMapWwCc1Pc map = this.m_LoadedFile as FileMapWwCc1Pc;
            if (map == null)
                return;
            String loadedPath = this.m_LoadedFile.LoadedFile;
            String baseFileName = Path.Combine(Path.GetDirectoryName(loadedPath), Path.GetFileNameWithoutExtension(loadedPath));
            String pngFileName = baseFileName + ".png";
            Bitmap plateauImage = null;
            if (selectHeightMap)
            {
                SupportedFileType selectedType;
                //String plateauFileName = Path.Combine(Path.GetDirectoryName(m_Filename), Path.GetFileNameWithoutExtension(m_Filename)) + "_lvl.png";

                String filename = FileDialogGenerator.ShowOpenFileFialog(this, "Select height levels image", new Type[] { typeof(FileImage) }, pngFileName, "images", null, out selectedType);
                this.m_LastOpenedFolder = Path.GetDirectoryName(filename);
                if (filename == null)
                    return;
                if (selectedType == null)
                    selectedType = new FileImage();
                try
                {
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
            SupportedFileType oldFile = this.m_LoadedFile;
            this.m_LoadedFile = HeightMapGenerator.GenerateHeightMapImage64x64(map, plateauImage, null);
            this.ReloadUi(false);
            oldFile.Dispose();
        }

        private void TsmiTo65x65HeightMap_Click(Object sender, EventArgs e)
        {
            FileImage image = this.m_LoadedFile as FileImage;
            if (image == null || image.Width != 64 || image.Height != 64)
                return;
            String baseFileName = Path.Combine(Path.GetDirectoryName(image.LoadedFile), Path.GetFileNameWithoutExtension(image.LoadedFile));
            String imgFileName = baseFileName + ".img";
            Bitmap bm = HeightMapGenerator.GenerateHeightMapImage65x65(image.GetBitmap());
            //Byte[] imageData = ImageUtils.GetSavedImageData(bm, ref imgFileName);
            FileImgWwN64 file = new FileImgWwN64();
            file.LoadGrayImage(bm, Path.GetFileName(imgFileName), imgFileName);
            SupportedFileType oldFile = this.m_LoadedFile;
            this.m_LoadedFile = file;
            this.ReloadUi(false);
            oldFile.Dispose();
        }

        private void TsmiToPlateaus_Click(Object sender, EventArgs e)
        {
            FileMapWwCc1Pc map = this.m_LoadedFile as FileMapWwCc1Pc;
            if (map == null)
                return;
            SupportedFileType oldFile = this.m_LoadedFile;
            this.m_LoadedFile = HeightMapGenerator.GeneratePlateauImage64x64(map, "_lvl");
            this.ReloadUi(false);
            oldFile.Dispose();
        }

        private void TsmiSplitShadows_Click(Object sender, EventArgs e)
        {
            if (this.m_LoadedFile == null || this.m_LoadedFile.Frames == null || this.m_LoadedFile.Frames.Length == 0)
                return;
            try
            {
                SupportedFileType oldFile = this.m_LoadedFile;
                this.m_LoadedFile = FileFramesWwShpTs.SplitShadows(this.m_LoadedFile, 4, 1);
                this.ReloadUi(true);
                oldFile.Dispose();
            }
            catch (NotSupportedException ex)
            {
                MessageBox.Show(this, ex.Message, GetTitle(false), MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void TsmiCombineShadows_Click(Object sender, EventArgs e)
        {
            if (this.m_LoadedFile == null || this.m_LoadedFile.Frames == null || this.m_LoadedFile.Frames.Length == 0)
                return;
            try
            {
                SupportedFileType oldFile = this.m_LoadedFile;
                this.m_LoadedFile = FileFramesWwShpTs.CombineShadows(this.m_LoadedFile, 1, 4);
                this.ReloadUi(true);
                oldFile.Dispose();
            }
            catch (NotSupportedException ex)
            {
                MessageBox.Show(this, ex.Message, GetTitle(false), MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            
        }

        private void TsmiPasteOnFrames_Click(Object sender, EventArgs e)
        {
            if (this.m_LoadedFile == null || this.m_LoadedFile.Frames == null || this.m_LoadedFile.Frames.Length == 0)
                return;
            try
            {
                Int32 frames = this.m_LoadedFile.Frames.Length;
                Int32 maxWidth = this.m_LoadedFile.Frames.Max(fr => fr == null ? 0 : fr.Width);
                Int32 maxHeight = this.m_LoadedFile.Frames.Max(fr => fr == null ? 0 : fr.Height);
                FrmPasteOnFrames pasteBox = new FrmPasteOnFrames(frames, maxWidth, maxHeight, this.m_LoadedFile.BitsPerPixel, this.m_LastOpenedFolder);
                DialogResult dr = pasteBox.ShowDialog(this);
                this.m_LastOpenedFolder = pasteBox.LastSelectedFolder;
                if (dr != DialogResult.OK)
                    return;
                // Processing code.
                FileFrames newfile = FileFrames.PasteImageOnFrames(this.m_LoadedFile, pasteBox.Image, pasteBox.Coords, pasteBox.FrameRange, pasteBox.KeepIndices);
                pasteBox.Image.Dispose();
                SupportedFileType oldFile = this.m_LoadedFile;
                this.m_LoadedFile = newfile;
                this.ReloadUi(false);                
                oldFile.Dispose();
            }
            catch (NotSupportedException ex)
            {
                MessageBox.Show(this, ex.Message, GetTitle(false), MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void TsmiTestBed(Object sender, EventArgs e)
        {
#if DEBUG
            // any test code can be linked in here
            this.ViewKortExeIcons();
#endif
        }

#if DEBUG
        private void ViewKortExeIcons()
        {
            // Icons data from inside the King Arthur's K.O.R.T. exe file.
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
            palette[1] = Color.FromArgb(0, Color.Fuchsia);
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
                frame.SetFileClass(FileClass.Image4Bit);
                frame.SetColorsInPalette(4);
                framesContainer.AddFrame(frame);
            }
            fullImage = ImageUtils.ConvertFrom8Bit(fullImage, fullWidth, fullHeight, 4, true, ref fullStride);
            Bitmap composite = ImageUtils.BuildImage(fullImage, fullWidth, fullHeight, fullStride, PixelFormat.Format4bppIndexed, palette, Color.Empty);
            composite.Palette = pal;
            framesContainer.SetCompositeFrame(composite);
            framesContainer.SetBitsPerColor(2);
            framesContainer.SetPalette(pal.Entries);
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
