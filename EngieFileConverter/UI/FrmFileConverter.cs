using Nyerguds.ImageManipulation;
using Nyerguds.Util;
using Nyerguds.Util.UI;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using EngieFileConverter.Domain.FileTypes;
using EngieFileConverter.Domain.HeightMap;
using System.Text;

namespace EngieFileConverter.UI
{
    public partial class FrmFileConverter : Form
    {
        public delegate void InvokeDelegateReload(SupportedFileType newFile, Boolean asNew, Boolean resetZoom);
        public delegate DialogResult InvokeDelegateMessageBox(String message, MessageBoxButtons buttons, MessageBoxIcon icon);
        public delegate void InvokeDelegateTwoArgs(Object arg1, Object arg2);
        public delegate void InvokeDelegateSingleArg(Object value);
        public delegate void InvokeDelegateEnableControls(Boolean enabled, String processingLabel);

        private const String PROG_NAME = "Engie File Converter";
        private const String PROG_AUTHOR = "Created by Nyerguds";
        private const Int32 PALETTE_DIM = 226;

        private String m_StartupParamPath;
        private List<PaletteDropDownInfo> m_DefaultPalettes;
        private List<PaletteDropDownInfo> m_ReadPalettes;
        private SupportedFileType m_LoadedFile;
        private String m_LastOpenedFolder;
        private Thread m_ProcessingThread;
        private Label m_BusyStatusLabel;
        private Boolean m_Loading;

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
            PalettePanel.InitPaletteControl(8, this.palColorViewer, new Color[256], PALETTE_DIM);
            this.palColorViewer.Visible = false;
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
            Object[] arrParams =
            {//Arguments: func returning SupportedFileType, reload as new, reset auto-zoom, process type indication string.
                new Func<SupportedFileType>(()=> this.LoadFileProc(path, selectedType, preferredTypes)),
                true, true, "Loading"
            };
            this.m_ProcessingThread = new Thread(this.ExecuteThreaded);
            this.m_ProcessingThread.Start(arrParams);
        }

        private SupportedFileType LoadFileProc(String path, SupportedFileType selectedType, SupportedFileType[] preferredTypes)
        {
            SupportedFileType loadedFile = null;
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
                }
                if (!isEmptyFile && error == null)
                {
                    // Load from chosen type.
                    if (selectedType != null)
                    {
                        try
                        {
                            selectedType.LoadFile(fileData, path);
                            loadedFile = selectedType;
                        }
                        catch (FileTypeLoadException e)
                        {
                            loadedFile = null;
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
                        loadedFile = SupportedFileType.LoadFileAutodetect(fileData, path, preferredTypes, error != null, out loadErrors);
                        if (loadedFile != null)
                            error = null;
                        else
                        {
                            if (error != null)
                                loadErrors.Insert(0, error);
                            String errors = String.Join("\n", loadErrors.Select(er => er.AttemptedLoadedType + ": " + er.Message).ToArray());
                            String filename = path == null ? String.Empty : (" of \"" + Path.GetFileName(path) + "\"");
                            String message = "File type of " + filename + " could not be identified. Errors returned by all attempts:\n\n" + errors;
                            this.Invoke(new InvokeDelegateMessageBox(this.ShowMessageBox), message, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            return null;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                error = new FileTypeLoadException(ex.Message, ex);
                loadedFile = null;
            }
            List<String> filesChain = null;
            if (!isEmptyFile && error == null && loadedFile.IsFramesContainer && (filesChain = loadedFile.GetFilesToLoadMissingData(path)) != null && filesChain.Count > 0)
            {
                const String loadQuestion = "The file \"{0}\" seems to be missing a starting point. Would you like to load it from \"{1}\"{2}?";
                const String loadQuestionChain = " (chained through {0})";
                String firstPath = filesChain.First();
                String[] chain = filesChain.Skip(1).Select(pth => "\"" + Path.GetFileName(pth) + "\"").ToArray();
                String chainQuestion = chain.Length == 0 ? String.Empty : String.Format(loadQuestionChain, String.Join(", ", chain));
                String loadQuestionFormat = String.Format(loadQuestion, Path.GetFileName(path), Path.GetFileName(firstPath), chainQuestion);
                DialogResult dr = (DialogResult)this.Invoke(new InvokeDelegateMessageBox(this.ShowMessageBox), loadQuestionFormat, MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (dr != DialogResult.Yes)
                {
                    // quick way to enable the frames detection in the next part, if I do ever want to support real animation chaining.
                    filesChain = null;
                }
                else
                {
                    loadedFile.ReloadFromMissingData(fileData, path, filesChain);
                }
            }
            if (filesChain == null && (isEmptyFile || error == null))
            {
                SupportedFileType detectSource = loadedFile;
                if (isEmptyFile && preferredTypes.Length == 1)
                    detectSource = preferredTypes[0];
                SupportedFileType frames = this.CheckForFrames(path, detectSource);
                if (ReferenceEquals(frames, detectSource) && isEmptyFile)
                {
                    if (detectSource != null)
                    {
                        try { detectSource.Dispose(); }
                        catch { /* ignore */ }
                    }
                }
                else
                    loadedFile = frames;
            }
            if (error != null)
                this.Invoke(new InvokeDelegateMessageBox(this.ShowMessageBox), "File loading failed: " + error.Message, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            if (loadedFile == null && isEmptyFile)
                this.Invoke(new InvokeDelegateMessageBox(this.ShowMessageBox), "File loading failed: The file is empty!", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return loadedFile;
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
            StringBuilder message = new StringBuilder("The file appears to be part of a range (").Append(minName).Append(" - ").Append(maxName).Append(").");
            if (hasEmptyFrames)
                message.Append("\nSome of these frames are empty files. Not every save format supports empty frames.");
            message.Append("\n\nDo you wish to load the frames from all files?");
            DialogResult dr = (DialogResult)this.Invoke(new InvokeDelegateMessageBox(this.ShowMessageBox), message.ToString(), MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (dr == DialogResult.Yes)
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
            List<SupportedFileType> framesToCheck = new List<SupportedFileType>();
            framesToCheck.Add(this.m_LoadedFile);
            if (this.m_LoadedFile.Frames != null)
                framesToCheck.AddRange(this.m_LoadedFile.Frames);
            Int32 nrToCheck = framesToCheck.Count;
            for (Int32 i = 0; i < nrToCheck; ++i)
            {
                SupportedFileType sf = framesToCheck[i];
                Bitmap image;
                if (sf == null || (image = sf.GetBitmap()) == null)
                    continue;
                Int32 zoomFactor = Math.Max(1, Math.Min(maxWidth / image.Width, maxHeight / image.Height));
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
                this.lblValSize.Text = emptystr;
                this.lblValColorFormat.Text = emptystr;
                this.lblValColorsInPal.Text = emptystr;
                this.lblValInfo.Text = String.Empty;
                this.cmbPalettes.Enabled = false;
                this.cmbPalettes.SelectedIndex = 0;
                this.btnResetPalette.Enabled = false;
                this.btnSavePalette.Enabled = false;
                this.pzpImage.Image = null;
                PalettePanel.InitPaletteControl(8, this.palColorViewer, new Color[256], PALETTE_DIM);
                this.palColorViewer.Visible = false;
                this.RemoveProcessingLabel();
                return;
            }
            Int32 bpc = loadedFile.BitsPerPixel;
            this.lblValFilename.Text = GeneralUtils.DoubleFirstAmpersand(loadedFile.LoadedFileName);
            this.lblValType.Text = GeneralUtils.DoubleFirstAmpersand(loadedFile.ShortTypeDescription);
            this.toolTip1.SetToolTip(this.lblValType, this.lblValType.Text);
            this.lblValSize.Text = loadedFile.Width + "×" + loadedFile.Height;
            this.lblValColorFormat.Text = bpc == 0 ? "N/A" : (bpc + " BPP" + (bpc == 4 || bpc == 8 ? " (paletted)" : String.Empty));
            Color[] palette = loadedFile.GetColors();
            Int32 exposedColours = loadedFile.ColorsInPalette;
            Int32 actualColors = palette == null ? 0 : palette.Length;
            Boolean needsPalette = loadedFile.NeedsPalette();
            this.lblValColorsInPal.Text = actualColors + (needsPalette ? " (" + exposedColours + " in file)" : String.Empty);
            this.lblValInfo.Text = GeneralUtils.DoubleFirstAmpersand(loadedFile.ExtraInfo);
            this.cmbPalettes.Enabled = needsPalette;
            Bitmap image = loadedFile.GetBitmap();
            this.pzpImage.Image = image;
            this.RefreshPalettes(fromNewFile, fromNewFile);
            if (needsPalette && fromNewFile)
                this.CmbPalettes_SelectedIndexChanged(null, null);
            else
                this.RefreshColorControls();
            this.RemoveProcessingLabel();
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
                info.Colors = PaletteUtils.ApplyPalTransparencyMask(info.Colors, typeTransModifier);
            return allPalettes;
        }

        public List<PaletteDropDownInfo> LoadExtraPalettes()
        {
            List<PaletteDropDownInfo> palettes = new List<PaletteDropDownInfo>();
            String appFolder = Path.GetDirectoryName(Application.ExecutablePath);
            FileInfo[] files = new DirectoryInfo(appFolder).GetFiles("*.pal").OrderBy(x => x.Name).ToArray();
            Int32 filesLength = files.Length;
            for (Int32 i = 0; i < filesLength; ++i)
                palettes.AddRange(PaletteDropDownInfo.LoadSubPalettesInfoFromPalette(files[i], false, false, true));
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
            Int32 nrOfSaveTypes = saveTypes.Length;
            FileClass loadedFileType = loadedFile.FileClass;
            FileClass frameFileType = FileClass.None;
            if (hasFrames && !isFrame)
            {
                SupportedFileType first = this.m_LoadedFile.Frames.FirstOrDefault(x => x != null && x.GetBitmap() != null);
                if (first != null)
                    frameFileType = first.FileClass;
            }
            List<Type> filteredTypes = new List<Type>();
            for (Int32 i = 0; i < nrOfSaveTypes; ++i)
            {
                Type saveType = saveTypes[i];
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
            SaveOption[] saveOptions;
            try
            {
                saveOptions = selectedItem.GetSaveOptions(loadedFile, filename);
                if (saveOptions != null && saveOptions.Length > 0)
                {
                    SaveOptionInfo soi = new SaveOptionInfo();
                    soi.Name = GeneralUtils.DoubleFirstAmpersand("Extra save options for " + selectedItem.ShortTypeDescription);
                    soi.Properties = saveOptions;
                    FrmExtraOptions frmExtraOpts = new FrmExtraOptions();
                    frmExtraOpts.Init(soi);
                    if (frmExtraOpts.ShowDialog(this) != DialogResult.OK)
                        return;
                    saveOptions = frmExtraOpts.GetSaveOptions();
                }
            }
            catch (NotSupportedException ex)
            {
                String message = "Cannot save " + (frames ? "frame of " : String.Empty) + "type " + loadedFile.ShortTypeName
                                 + " as type " + selectedItem.ShortTypeName + (String.IsNullOrEmpty(ex.Message) ? "." : ":\n" + ex.Message);
                MessageBox.Show(this, message, GetTitle(false), MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            Object[] arrParams =
                {//Arguments: func returning SupportedFileType, reload as new, reset auto-zoom, process type indication string.
                    new Func<SupportedFileType>(()=> this.SaveFile(frames, loadedFile, selectedItem, filename, saveOptions)),
                    false, false, "Saving"
                };
            this.m_ProcessingThread = new Thread(this.ExecuteThreaded);
            this.m_ProcessingThread.Start(arrParams);
        }

        private SupportedFileType SaveFile(Boolean frames, SupportedFileType loadedFile, SupportedFileType selectedItem, String filename, SaveOption[] saveOptions)
        {
            try
            {
                if (!frames)
                    selectedItem.SaveAsThis(loadedFile, filename, saveOptions);
                else
                {
                    if (loadedFile.Frames == null)
                        return null;
                    String framename = Path.Combine(Path.GetDirectoryName(filename), Path.GetFileNameWithoutExtension(filename));
                    String extension = Path.GetExtension(filename);
                    for (Int32 i = 0; i < loadedFile.Frames.Length; ++i)
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
                this.Invoke(new InvokeDelegateMessageBox(this.ShowMessageBox), message, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            return null;
        }

        private void TsmiExit_Click(Object sender, EventArgs e)
        {
            this.Close();
        }


        protected void FrmFileConverter_FormClosing(Object sender, FormClosingEventArgs e)
        {
            if (!this.m_Loading || m_ProcessingThread == null || !m_ProcessingThread.IsAlive)
                return;
            DialogResult result = ShowMessageBox("Operations are in progress! Are you sure you want to quit?", MessageBoxButtons.YesNo, MessageBoxIcon.Exclamation);
            if (result == DialogResult.Yes)
            {
                m_ProcessingThread.Abort();
            }
            else
            {
                // abort the closing of the form.
                e.Cancel = true;
            }
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
            if (loadedFile != null && cs != ColorStatus.None && loadedFile.BitsPerPixel != 0)
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
            Boolean showPal = bpp > 0 && bpp <= 8;
            palColorViewer.Visible = showPal;
            if (showPal)
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
            if (colindex >= (1 << loadedFile.BitsPerPixel))
                return;
            if (palpanel.Palette.Length <= colindex)
            {
                Color[] oldPal = palpanel.Palette;
                Color[] newPal = new Color[colindex + 1];
                Array.Copy(oldPal, newPal, oldPal.Length);
                palpanel.Palette = newPal;
            }
            palpanel.Palette[colindex] = color;
            if (cs != ColorStatus.None)
            {
                Color[] pal = loadedFile.GetColors();
                if (pal.Length <= colindex)
                {
                    Color[] newPal = new Color[colindex + 1];
                    Array.Copy(pal, newPal, pal.Length);
                    pal = newPal;
                }
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
            if(this.palColorViewer.Palette.Length <= index)
                return;
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
            if (this.palColorViewer.Palette.Length <= index)
                return;
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

            Object[] arrParams =
            {//Arguments: func returning SupportedFileType, reload as new, reset auto-zoom, process type indication string.
                new Func<SupportedFileType>(()=> FileFrames.CutImageIntoFrames(image, imagePath, frameWidth, frameHeight, maxFrames, trimColor, trimIndex, matchBpp, matchPalette, false)),
                true, true, "Splitting into frames"
            };
            this.m_ProcessingThread = new Thread(this.ExecuteThreaded);
            this.m_ProcessingThread.Start(arrParams);
        }

        private void TsmiFramesToSingleImageClick(Object sender, EventArgs e)
        {
            if (this.m_LoadedFile == null)
                return;
            SupportedFileType[] frames = m_LoadedFile.Frames;
            Int32 nrOfframes;
            if (frames == null || (nrOfframes = frames.Length) == 0)
                return;
            Bitmap[] frameImages = new Bitmap[nrOfframes];
            PixelFormat highestPf = PixelFormat.Undefined;
            Int32 highestBpp = 0;
            Color[] palette = null;
            Int32 maxWidth = 0;
            Int32 maxHeight = 0;
            for (Int32 i = 0; i < nrOfframes; ++i)
            {
                Bitmap img = frames[i].GetBitmap();
                if (img == null)
                    continue;
                frameImages[i] = img;
                if (img.Width > maxWidth)
                    maxWidth = img.Width;
                if (img.Height > maxHeight)
                    maxHeight = img.Height;
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
            so[0] = new SaveOption("FRW", SaveOptionType.Number, "Frame width", maxWidth + ",", maxWidth.ToString());
            so[1] = new SaveOption("FRH", SaveOptionType.Number, "Frame height", maxHeight + ",", maxHeight.ToString());
            so[2] = new SaveOption("FPL", SaveOptionType.Number, "Frames per line", "1," + nrOfframes, ((Int32)Math.Sqrt(nrOfframes)).ToString());
            if (highestBpp <= 8)
                so[3] = new SaveOption("BGI", SaveOptionType.Palette, "Background colour around frames", highestBpp + "|" + paletteStr, "0");
            else
                so[3] = new SaveOption("BGC", SaveOptionType.Color, "Background colour around frames", hasAlpha ? "A" : hasSimpleTrans ? "T" : String.Empty, "#00000000");
            SaveOptionInfo soi = new SaveOptionInfo();
            soi.Name = "Frames to single image";
            soi.Properties = so;
            FrmExtraOptions extraopts = new FrmExtraOptions();

            try
            {
                extraopts.Size = extraopts.MinimumSize;
                extraopts.Init(soi);
                if (extraopts.ShowDialog(this) != DialogResult.OK)
                    return;
            }
            catch (NotSupportedException ex)
            {
                MessageBox.Show(this, "Error initializing conversion options: " + ex.Message, GetTitle(false), MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
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
            Object[] arrParams =
            {//Arguments: func returning SupportedFileType, reload as new, reset auto-zoom, process type indication string.
                new Func<SupportedFileType>(() => this.FramesToSingleImage(frameImages, frameWidth, frameHeight, framesPerLine, fillPalIndex, fillColor)),
                true, true, "Combining frames"
            };
            this.m_ProcessingThread = new Thread(this.ExecuteThreaded);
            this.m_ProcessingThread.Start(arrParams);
        }

        private SupportedFileType FramesToSingleImage(Bitmap[] images, Int32 framesWidth, Int32 framesHeight, Int32 framesPerLine, Byte backFillPalIndex, Color backFillColor)
        {
            Bitmap bm = ImageUtils.BuildImageFromFrames(images, framesWidth, framesHeight, framesPerLine, backFillPalIndex, backFillColor);
            FileImagePng returnImg = new FileImagePng();
            returnImg.LoadFile(bm, this.m_LoadedFile.LoadedFile);
            return returnImg;
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
                    MessageBox.Show(this, "Height levels image needs to be 64×64!", GetTitle(false), MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
            }
            Object[] arrParams =
            {//Arguments: func returning SupportedFileType, reload as new, reset auto-zoom, process type indication string.
                new Func<SupportedFileType>(() => HeightMapGenerator.GenerateHeightMapImage64x64(map, plateauImage, null)),
                true, true, "Generating height map"
            };
            this.m_ProcessingThread = new Thread(this.ExecuteThreaded);
            this.m_ProcessingThread.Start(arrParams);
        }

        private void TsmiTo65x65HeightMap_Click(Object sender, EventArgs e)
        {
            FileImage image = this.m_LoadedFile as FileImage;
            if (image == null || image.Width != 64 || image.Height != 64)
                return;
            String baseFileName = Path.Combine(Path.GetDirectoryName(image.LoadedFile), Path.GetFileNameWithoutExtension(image.LoadedFile));
            String imgFileName = baseFileName + ".img";
            Object[] arrParams =
            {//Arguments: func returning SupportedFileType, reload as new, reset auto-zoom, process type indication string.
                new Func<SupportedFileType>(()=> Make65x65HeightMap(image, imgFileName)),
                false, false, "Creating height map"
            };
            this.m_ProcessingThread = new Thread(this.ExecuteThreaded);
            this.m_ProcessingThread.Start(arrParams);
        }
        
        private FileImgWwN64 Make65x65HeightMap(FileImage image, String imgFileName)
        {
            Bitmap bm = HeightMapGenerator.GenerateHeightMapImage65x65(image.GetBitmap());
            //Byte[] imageData = ImageUtils.GetSavedImageData(bm, ref imgFileName);
            FileImgWwN64 file = new FileImgWwN64();
            file.LoadGrayImage(bm, Path.GetFileName(imgFileName), imgFileName);
            return file;
        }

        private void TsmiToPlateaus_Click(Object sender, EventArgs e)
        {
            FileMapWwCc1Pc map = this.m_LoadedFile as FileMapWwCc1Pc;
            if (map == null)
                return;
            Object[] arrParams =
            {//Arguments: func returning SupportedFileType, reload as new, reset auto-zoom, process type indication string.
                new Func<SupportedFileType>(()=> HeightMapGenerator.GeneratePlateauImage64x64(map, "_lvl")),
                false, false, "Generating plateaus"
            };
            this.m_ProcessingThread = new Thread(this.ExecuteThreaded);
            this.m_ProcessingThread.Start(arrParams);
        }

        private void TsmiSplitShadows_Click(Object sender, EventArgs e)
        {
            if (this.m_LoadedFile == null || this.m_LoadedFile.Frames == null || this.m_LoadedFile.Frames.Length == 0)
                return;
            // TODO maybe ask the indices with a simple save options dialog?
            Object[] arrParams =
            {//Arguments: func returning SupportedFileType, reload as new, reset auto-zoom, process type indication string.
                new Func<SupportedFileType>(()=> FileFramesWwShpTs.SplitShadows(this.m_LoadedFile, 4, 1)),
                true, false, "Splitting shadows"
            };
            this.m_ProcessingThread = new Thread(this.ExecuteThreaded);
            this.m_ProcessingThread.Start(arrParams);
        }

        private void TsmiCombineShadows_Click(Object sender, EventArgs e)
        {
            if (this.m_LoadedFile == null || this.m_LoadedFile.Frames == null || this.m_LoadedFile.Frames.Length == 0)
                return;
            // TODO maybe ask the indices with a simple save options dialog?
            Object[] arrParams =
            {//Arguments: func returning SupportedFileType, reload as new, reset auto-zoom, process type indication string.
                new Func<SupportedFileType>(()=> FileFramesWwShpTs.CombineShadows(this.m_LoadedFile, 1, 4)),
                true, false, "Combining shadows"
            };
            this.m_ProcessingThread = new Thread(this.ExecuteThreaded);
            this.m_ProcessingThread.Start(arrParams);
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
                Object[] arrParams =
                {//Arguments: func returning SupportedFileType, reload as new, reset auto-zoom, process type indication string.
                    new Func<SupportedFileType>(()=> this.PasteOnFrames(this.m_LoadedFile, pasteBox.Image, pasteBox.Coords, pasteBox.FrameRange, pasteBox.KeepIndices)),
                    false, false, "Pasting on frames"
                };
                this.m_ProcessingThread = new Thread(this.ExecuteThreaded);
                this.m_ProcessingThread.Start(arrParams);
            }
            catch (NotSupportedException ex)
            {
                MessageBox.Show(this, ex.Message, GetTitle(false), MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private SupportedFileType PasteOnFrames(SupportedFileType framesContainer, Bitmap image, Point pasteLocation, Int32[] framesRange, Boolean keepIndices)
        {
            FileFrames newfile = FileFrames.PasteImageOnFrames(framesContainer, image, pasteLocation, framesRange, keepIndices);
            image.Dispose();
            return newfile;
        }
        
        /// <summary>
        ///  Executes a threaded operation while locking the UI.
        ///  Arguments for the thread are: func returning SupportedFileType, reload as new, reset auto-zoom, and a string to indicate the process type being executed (eg. "splitting").
        /// </summary>
        /// <param name="parameters">Arguments for the thread are: func returning SupportedFileType, reload as new, reset auto-zoom, and a string to indicate the process type being executed (eg. "splitting").</param>
        private void ExecuteThreaded(Object parameters)
        {
            Object[] arrParams = parameters as Object[];
            if (arrParams == null || arrParams.Length != 4)
                return;
            if (!(arrParams[1] is Boolean) || !(arrParams[2] is Boolean))
                return;
            Func<SupportedFileType> func = arrParams[0] as Func<SupportedFileType>;
            Boolean asNewFile = (Boolean)arrParams[1];
            Boolean resetZoom = (Boolean)arrParams[2];
            String operationType = arrParams[3] as String;
            if (func == null)
                return;
            this.Invoke(new InvokeDelegateEnableControls(this.EnableControls), false, operationType);
            SupportedFileType newfile = null;
            try
            {
                // Processing code.
                newfile = func();
            }
            catch (ThreadAbortException)
            {
                // Ignore. Thread is aborted.
            }
            catch (Exception ex)
            {
                operationType = String.IsNullOrEmpty(operationType) ? String.Empty : operationType.Trim().ToLowerInvariant() + " ";
                String message = operationType + " failed:\n" + ex.Message;
                this.Invoke(new InvokeDelegateMessageBox(this.ShowMessageBox), message, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                this.Invoke(new InvokeDelegateEnableControls(this.EnableControls), true, null);
            }
            try
            {
                if (newfile != null)
                    this.Invoke(new InvokeDelegateReload(this.ReloadWithDispose), newfile, asNewFile, resetZoom);
                else
                    this.Invoke(new InvokeDelegateEnableControls(this.EnableControls), true, null);
            }
            catch (InvalidOperationException) { /* ignore */ }
        }

        private void EnableControls(Boolean enabled, String processingLabel)
        {
            if (!enabled)
                this.m_Loading = true;
            this.EnableToolstrips(enabled);
            if (!enabled)
            {
                // To prevent UI updates using loaded images from interfering with internal operations.
                // The UI gets reloaded afterwards anyway, so that should always restore the image.
                this.pzpImage.Image = null;
                // Disable controls
                this.numFrame.Enabled = false;
                this.cmbPalettes.Enabled = false;
                this.btnSavePalette.Enabled = false;
                // Create busy status label.
                if (this.m_BusyStatusLabel != null)
                {
                    try { this.m_BusyStatusLabel.Dispose(); }
                    catch { /*ignore*/ }
                }
                this.m_BusyStatusLabel = new Label();
                this.m_BusyStatusLabel.Text = (String.IsNullOrEmpty(processingLabel) ? "Processing" : processingLabel) + "...";
                this.m_BusyStatusLabel.TextAlign = ContentAlignment.MiddleCenter;
                this.m_BusyStatusLabel.Font = new Font(this.m_BusyStatusLabel.Font.FontFamily, 15F, FontStyle.Regular, GraphicsUnit.Pixel, 0);
                this.m_BusyStatusLabel.AutoSize = false;
                this.m_BusyStatusLabel.Size = new Size(300, 100);
                this.m_BusyStatusLabel.Anchor = AnchorStyles.None; // Always floating in the middle, even on resize.
                this.m_BusyStatusLabel.BorderStyle = BorderStyle.FixedSingle;
                Int32 x = (this.ClientRectangle.Width - 300) / 2;
                Int32 y = (this.ClientRectangle.Height - 100) / 2;
                this.m_BusyStatusLabel.Location = new Point(x, y);
                this.Controls.Add(this.m_BusyStatusLabel);
                this.m_BusyStatusLabel.Visible = true;
                this.m_BusyStatusLabel.BringToFront();
            }
            else
                this.ReloadUi(false);
            this.pzpImage.Enabled = enabled;
            if (enabled)
                this.m_Loading = false;
        }

        private void RemoveProcessingLabel()
        {
            if (this.m_BusyStatusLabel == null)
                return;
            this.Controls.Remove(this.m_BusyStatusLabel);
            try { this.m_BusyStatusLabel.Dispose(); }
            catch { /* ignore */ }
            this.m_BusyStatusLabel = null;
        }

        private void ReloadWithDispose(SupportedFileType newFile, Boolean asNew, Boolean resetZoom)
        {
            SupportedFileType oldFile = this.m_LoadedFile;
            this.m_LoadedFile = newFile;
            if (resetZoom)
                this.AutoSetZoom();
            EnableToolstrips(true);
            if (!this.pzpImage.Enabled)
                this.pzpImage.Enabled = true;
            this.ReloadUi(asNew);
            if (oldFile != null)
            {
                try { oldFile.Dispose(); }
                catch { /*ignore*/ }
            }
            this.m_Loading = false;
        }

        private void EnableToolstrips(Boolean enable)
        {
            this.tsmiOpen.Enabled = enable;
            this.tsmiSave.Enabled = enable;
            this.tsmiSaveFrames.Enabled = enable;
            if (!enable)
            {
                // Let the UI reload take care of re-enabling these.
                this.tsmiCopy.Enabled = false;
                this.tsmiImageToFrames.Enabled = false;
                this.tsmiFramesToSingleImage.Enabled = false;
                this.tsmiToHeightMap.Enabled = false;
                this.tsmiToPlateaus.Enabled = false;
                this.tsmiToHeightMapAdv.Enabled = false;
                this.tsmiTo65x65HeightMap.Enabled = false;
                this.tsmiCombineShadows.Enabled = false;
                this.tsmiSplitShadows.Enabled = false;
            }
            this.tsmiTestBed.Enabled = enable;
        }

        private DialogResult ShowMessageBox(String message, MessageBoxButtons buttons, MessageBoxIcon icon)
        {
            if (message == null)
                return DialogResult.Cancel;
            return MessageBox.Show(this, message, GetTitle(false), buttons, icon);
        }

        private void TsmiTestBed(Object sender, EventArgs e)
        {
#if DEBUG
            this.ExecuteTestCode();
#endif
        }
    }

}
