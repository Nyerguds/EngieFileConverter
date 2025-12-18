using Nyerguds.ImageManipulation;
using Nyerguds.Util;
using Nyerguds.Util.UI;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using EngieFileConverter.Domain.FileTypes;
using EngieFileConverter.Domain.HeightMap;
using System.Text;
using System.Text.RegularExpressions;
using Nyerguds.Util.UI.SaveOptions;
using EngieFileConverter.Domain;

namespace EngieFileConverter.UI
{
    public partial class FrmFileConverter : Form
    {
        private const String PROG_NAME = "Engie File Converter";
        private const String PROG_AUTHOR = "Created by Nyerguds";
        private const Int32 PALETTE_DIM = 226;
        // TODO make configurable?
        private readonly String m_PalettePath = Path.GetDirectoryName(Application.ExecutablePath);

        private String[] m_StartupParamPath;
        private List<PaletteDropDownInfo> m_DefaultPalettes;
        private List<PaletteDropDownInfo> m_ReadPalettes;
        private SupportedFileType m_LoadedFile;
        private String m_LastOpenedFolder;
        private Thread m_ProcessingThread;
        private Label m_BusyStatusLabel;
        private Boolean m_Loading;
        private Control m_FocusedControl;

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
            PalettePanel.InitPaletteControl(8, this.palColorPalette, new Color[256], PALETTE_DIM);
            this.palColorPalette.Visible = false;
            this.m_DefaultPalettes = this.LoadDefaultPalettes();
            this.m_ReadPalettes = this.LoadExtraPalettes();
            this.RefreshPalettes(false, false);
#if DEBUG
            this.tsmiTestBed.Visible = true;
#endif
        }

        public static String GetTitle()
        {
            return GetTitle(false);
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
            {
                List<String> files = new List<String>();
                files.Add(args[0]);
                for (Int32 i = 1; i < args.Length; ++i)
                {
                    String pth = args[i];
                    if (File.Exists(pth))
                        files.Add(pth);
                }
                this.m_StartupParamPath = files.ToArray();
            }
        }

        public List<PaletteDropDownInfo> LoadDefaultPalettes()
        {
            List<PaletteDropDownInfo> palettes = new List<PaletteDropDownInfo>();
            palettes.Add(new PaletteDropDownInfo("Black/White", 1, new Color[] {Color.Black, Color.White}, null, -1, false, false));
            palettes.Add(new PaletteDropDownInfo("White/Black", 1, new Color[] {Color.White, Color.Black}, null, -1, false, false));
            palettes.Add(new PaletteDropDownInfo("Black/Red", 1, new Color[] {Color.Black, Color.Red}, null, -1, false, false));

            palettes.Add(new PaletteDropDownInfo("CGA pal 0, dark", 2, PaletteUtils.GetCgaPalette(0, true, false, false, 2), null, -1, false, false));
            palettes.Add(new PaletteDropDownInfo("CGA pal 0, bright", 2, PaletteUtils.GetCgaPalette(0, true, false, true, 2), null, -1, false, false));
            palettes.Add(new PaletteDropDownInfo("CGA pal 1, dark", 2, PaletteUtils.GetCgaPalette(0, true, true, false, 2), null, -1, false, false));
            palettes.Add(new PaletteDropDownInfo("CGA pal 1, bright", 2, PaletteUtils.GetCgaPalette(0, true, true, true, 2), null, -1, false, false));
            palettes.Add(new PaletteDropDownInfo("CGA pal 2, dark", 2, PaletteUtils.GetCgaPalette(0, false, true, false, 2), null, -1, false, false));
            palettes.Add(new PaletteDropDownInfo("CGA pal 2, bright", 2, PaletteUtils.GetCgaPalette(0, false, true, true, 2), null, -1, false, false));

            palettes.Add(new PaletteDropDownInfo("Grayscale B->W", 4, PaletteUtils.GenerateGrayPalette(4, null, false), null, -1, false, false));
            //palettes.Add(new PaletteDropDownInfo("Heights Blue->Red", 4, PaletteUtils.GenerateRainbowPalette(4, false, false, true, 0, 240), null, -1, false, false));
            palettes.Add(new PaletteDropDownInfo("Grayscale W->B", 4, PaletteUtils.GenerateGrayPalette(4, null, true), null, -1, false, false));
            palettes.Add(new PaletteDropDownInfo("Rainbow", 4, PaletteUtils.GenerateRainbowPalette(4, -1, null, false), null, -1, false, false));
            palettes.Add(new PaletteDropDownInfo("EGA Palette", 4, PaletteUtils.GetEgaPalette(4), null, -1, false, false));
            //palettes.Add(new PaletteDropDownInfo("Windows palette", 4, PaletteUtils.GenerateDefWindowsPalette(4, false, false), null, -1, false, false));
            palettes.Add(new PaletteDropDownInfo("Grayscale B->W", 8, PaletteUtils.GenerateGrayPalette(8, null, false), null, -1, false, false));
            palettes.Add(new PaletteDropDownInfo("Heights Blue->Red", 8, PaletteUtils.GenerateRainbowPalette(8, -1, null, true, 0, 240, true), null, -1, false, false));
            //palettes.Add(new PaletteDropDownInfo("Grayscale W->B", 8, PaletteUtils.GenerateGrayPalette(8, false, true), null, -1, false, false));
            palettes.Add(new PaletteDropDownInfo("Rainbow", 8, PaletteUtils.GenerateRainbowPalette(8, -1, null, false), null, -1, false, false));
            //palettes.Add(new PaletteDropDownInfo("Windows palette", 8, PaletteUtils.GenerateDefWindowsPalette(8, false, false), null, -1, false, false));
            return palettes;
        }

        private void TsmiCopyClick(Object sender, EventArgs e)
        {
            this.pzpImage.CopyToClipboard();
        }

        private void FrmDragEnter(Object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
                e.Effect = DragDropEffects.Copy;
        }

        private void FrmDragDrop(Object sender, DragEventArgs e)
        {
            String[] files = (String[]) e.Data.GetData(DataFormats.FileDrop);
            if (files.Length == 0)
                return;
            List<String> filesList = new List<String>();
            String basePath = null;
            String firstFoundFolder = null;
            Int32 foldersFound = 0;
            for (Int32 i = 0; i < files.Length; ++i)
            {
                String path = files[i];
                try
                {
                    if ((File.GetAttributes(path) & FileAttributes.Directory) != 0)
                    {
                        if (firstFoundFolder == null)
                            firstFoundFolder = Path.GetFullPath(path);
                        filesList.AddRange(Directory.GetFiles(path));
                        foldersFound++;
                    }
                    else
                    {
                        filesList.Add(path);
                        basePath = Path.GetDirectoryName(path);
                    }
                }
                catch
                {
                    continue; 
                }
            }
            if (filesList.Count == 0)
                return;
            if (basePath == null && firstFoundFolder != null)
                basePath = foldersFound == 1 ? firstFoundFolder : Path.GetDirectoryName(firstFoundFolder);
            SupportedFileType[] preferredTypes = FileDialogGenerator.IdentifyByExtension<SupportedFileType>(FileTypesFactory.AutoDetectTypes, filesList[0]);
            this.m_LastOpenedFolder = basePath;
            this.LoadFile(filesList.ToArray(), null, preferredTypes);
        }

        private void LoadFile(String[] paths, SupportedFileType selectedType, SupportedFileType[] preferredTypes)
        {
            this.ExecuteThreaded(()=> this.LoadFileProc(paths, selectedType, preferredTypes), true, true, true, "Loading");
        }

        private SupportedFileType LoadFileProc(String[] paths, SupportedFileType selectedType, SupportedFileType[] preferredTypes)
        {
            if (paths == null || paths.Length == 0)
                return null;
            String path = paths[0];
            if (paths.Length > 1)
                return this.LoadMultiple(paths, selectedType, preferredTypes);
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
                        loadedFile = FileTypesFactory.LoadFileAutodetect(fileData, path, preferredTypes, error != null, out loadErrors);
                        if (loadedFile != null)
                            error = null;
                        else
                        {
                            if (error != null)
                                loadErrors.Insert(0, error);
                            String[] errors = loadErrors.Select(er => er.AttemptedLoadedType + ": " + er.Message).ToArray();
                            String filename = path == null ? String.Empty : (" of \"" + Path.GetFileName(path) + "\"");
                            String title = "File type of " + filename + " could not be identified. Errors returned by all attempts:";
                            this.Invoke(new Action(() => this.ShowScrollingMessageBox("Could not load file.", title, errors, false)));
                            return null;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                if (ex is ArgumentException) // No stack trace for this.
                    error = new FileTypeLoadException(ex.Message);
                else
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
                DialogResult dr = (DialogResult)this.Invoke(
                    new Func<DialogResult>(() => this.ShowMessageBox(loadQuestionFormat, MessageBoxButtons.YesNo, MessageBoxIcon.Question)));
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
            {
                String message = "File loading failed: " + error.Message;
                if (error.InnerException != null)
                    message += '\n' + error.InnerException.StackTrace;
                this.Invoke(new Action(() => this.ShowMessageBox(message, MessageBoxButtons.OK, MessageBoxIcon.Warning)));
            }
            if (loadedFile == null && isEmptyFile)
                this.Invoke(new Action(() => this.ShowMessageBox("File loading failed: The file is empty.", MessageBoxButtons.OK, MessageBoxIcon.Warning)));
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
            DialogResult dr = (DialogResult)this.Invoke(
                new Func<DialogResult>(() => this.ShowMessageBox(message.ToString(), MessageBoxButtons.YesNo, MessageBoxIcon.Warning)));
            if (dr == DialogResult.Yes)
            {
                if (currentType != null)
                    currentType.Dispose();
                return fr;
            }
            fr.Dispose();
            return currentType;
        }


        /// <summary>
        /// Load multiple frames as frames file.
        /// </summary>
        /// <param name="paths">path that was opened.</param>
        /// <param name="selectedType">Specific type that was selected in the Open File menu. Null for "all types"</param>
        /// <param name="preferredTypes">Preferred types based on extension.</param>
        /// <returns>A generic SupportedType object filled with the frames, or the original 'currentType' object if the detect failed or was aborted.</returns>
        private SupportedFileType LoadMultiple(String[] paths, SupportedFileType selectedType, SupportedFileType[] preferredTypes)
        {
            String[] paths2 = new String[paths.Length];
            Array.Copy(paths, paths2, paths.Length);
            Array.Sort(paths2);
            FileFrames fr = new FileFrames(true);
            SupportedFileType[] loadedFiles = new SupportedFileType[paths.Length];
            for (Int32 i = 0; i < paths2.Length; ++i)
            {
                String path = paths2[i];
                if (File.Exists(path))
                {
                    try
                    {
                        Byte[] fileData = File.ReadAllBytes(path);
                        if (fileData.Length == 0)
                        {
                            FileImageFrame frame = new FileImageFrame();
                            frame.LoadFileFrame(fr, selectedType, null, path, -1);
                            frame.SetBitsPerColor(selectedType == null ? 32 : selectedType.BitsPerPixel);
                            frame.SetFileClass(selectedType == null ? FileClass.Image : selectedType.FileClass);
                            frame.SetNeedsPalette(selectedType != null && selectedType.NeedsPalette);
                            frame.SetExtraInfo("Empty file.");
                            fr.AddFrame(frame);
                        }
                        else
                        {
                            List<FileTypeLoadException> loadErrors;
                            loadedFiles[i] = FileTypesFactory.LoadFileAutodetect(fileData, path, preferredTypes, selectedType != null, out loadErrors);
                            fr.AddFrame(loadedFiles[i]);
                        }
                    }
                    catch
                    {
                        //Ignore
                    }
                }
            }
            return fr;
        }


        private void AutoSetZoom()
        {
            this.pzpImage.AutoSetZoom(GetListToAutoSetZoom(this.m_LoadedFile));
        }

        private static Bitmap[] GetListToAutoSetZoom(SupportedFileType file)
        {
            if (file == null)
                return null;
            List<Bitmap> framesToCheck = new List<Bitmap>();
            framesToCheck.Add(file.GetBitmap());
            SupportedFileType[] frames = file.Frames;
            Int32 nrOfFrames;
            if (frames != null && (nrOfFrames = frames.Length) > 0)
            {
                for (Int32 i = 0; i < nrOfFrames; ++i)
                {
                    Bitmap img;
                    if (frames[i] != null && (img = frames[i].GetBitmap()) != null)
                        framesToCheck.Add(img);
                }
            }
            return framesToCheck.ToArray();
        }

        private void ReloadUi(Boolean fromNewFile)
        {
            ReloadUi(fromNewFile, fromNewFile);
        }

        private void ReloadUi(Boolean resetPalettes, Boolean resetIndex)
        {
            Boolean hasFrames = this.m_LoadedFile != null && this.m_LoadedFile.Frames != null && this.m_LoadedFile.Frames.Length > 0;
            Int32 bpp = this.m_LoadedFile == null ? -1 : Math.Abs(this.m_LoadedFile.BitsPerPixel);
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
                if (resetIndex)
                    this.numFrame.Value = -1;
                frames = this.m_LoadedFile.Frames.Length;
                Int32 last = frames - 1;
                this.numFrame.Maximum = last;
                this.lblNrOfFrames.Visible = true;
                this.lblNrOfFrames.Text = "/ " + last;
                if (last >= 0 && !this.m_LoadedFile.IsFramesContainer)
                    this.numFrame.Minimum = 0;
            }
            SupportedFileType shownFile = this.GetShownFile();
            Boolean hasFile = shownFile != null;
            Boolean hasShownImage = hasFile && shownFile.GetBitmap() != null;
            Boolean hasPal = this.GetColorStatus() != ColorStatus.None;
            Int32 shownBpp = shownFile != null ? Math.Abs(shownFile.BitsPerPixel) : -1;
            Boolean canExportFrames = this.m_LoadedFile != null && (this.m_LoadedFile.FileClass & (FileClass.Image | FileClass.FrameSet)) != 0;

            // General
            this.tsmiSave.Enabled = hasFile;
            this.tsmiSaveRaw.Enabled = hasShownImage;
            this.tsmiSaveSingleFrame.Enabled = canExportFrames && numFrame.Value >= 0;
            this.tsmiSaveFrames.Enabled = canExportFrames;
            this.tsmiFramesToSingleImage.Enabled = canExportFrames;
            this.tsmiCopy.Enabled = hasShownImage;

            // General frame tools
            this.tsmiImageToFrames.Enabled = hasShownImage;
            this.tsmiFramesToSingleImage.Enabled = canExportFrames && frames > 0;
            // General animations "paste on frames" option.
            this.tsmiPasteOnFrames.Enabled = (hasFrames && frames > 0) || (!hasFrames && hasShownImage);

            // Extract colors
            this.tsmiExtractPal.Enabled = hasPal;
            this.tsmiExtract4BitPal.Enabled = hasPal && shownBpp == 8;
            this.tsmiImageToPalette4Bit.Enabled = hasShownImage;
            this.tsmiImageToPalette8Bit.Enabled = hasShownImage;
            this.tsmiMatchToPalette.Enabled = hasFile && (this.m_LoadedFile.FileClass & (FileClass.Image | FileClass.FrameSet)) != 0;
            int globalBpp = !hasFile ? -1 : this.m_LoadedFile.GetGlobalBpp();
            this.tsmiRemovePalette.Enabled = globalBpp != -1 && globalBpp <= 8;
            this.tsmiSetToDifferenPalette.Enabled = globalBpp != -1 && globalBpp <= 8;
            this.tsmiChangeTo24BitRgb.Enabled = hasFile && (this.m_LoadedFile.FileClass & (FileClass.Image | FileClass.FrameSet)) != 0 && this.m_LoadedFile.BitsPerPixel != 24;
            this.tsmiChangeTo32BitArgb.Enabled = hasFile && (this.m_LoadedFile.FileClass & (FileClass.Image | FileClass.FrameSet)) != 0 && this.m_LoadedFile.BitsPerPixel != 32;

            // C&C64 toolsets
            this.tsmiToHeightMap.Enabled = shownFile is FileMapWwCc1Pc;
            this.tsmiToPlateaus.Enabled = shownFile is FileMapWwCc1Pc;
            this.tsmiToHeightMapAdv.Enabled = shownFile is FileMapWwCc1Pc;
            this.tsmiTo65x65HeightMap.Enabled = hasShownImage && shownFile.Width == 64 && shownFile.Height == 64 && shownFile.FileClass != FileClass.CcMap;
            // Tiberian Sun shadow tools
            this.tsmiCombineShadows.Enabled = hasFrames && bpp == 8 && frames > 0 && frames % 2 == 0;
            this.tsmiSplitShadows.Enabled = hasFrames && bpp == 8 && frames > 0;

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
                PalettePanel.InitPaletteControl(8, this.palColorPalette, new Color[256], PALETTE_DIM);
                this.palColorPalette.Visible = false;
            }
            else
            {
                this.lblValFilename.Text = GeneralUtils.DoubleAmpersands(shownFile.LoadedFileName);
                this.lblValType.Text = GeneralUtils.DoubleAmpersands(shownFile.LongTypeName);
                this.toolTip1.SetToolTip(this.lblValType, this.lblValType.Text);
                this.lblValSize.Text = shownFile.Width + "×" + shownFile.Height;
                this.lblValColorFormat.Text = shownBpp < 0 ? String.Empty : (shownBpp == 0 ? "N/A" : (shownBpp + " BPP" + (shownBpp < 8 ? " (paletted)" : String.Empty)));
                Color[] palette = shownFile.GetColors();
                Int32 actualColors = palette == null ? 0 : palette.Length;
                Boolean needsPalette = shownFile.NeedsPalette;
                this.lblValColorsInPal.Text = actualColors + (needsPalette ? " (0 in file)" : String.Empty);
                this.lblValInfo.Text = GeneralUtils.DoubleAmpersands(shownFile.ExtraInfo);
                this.cmbPalettes.Enabled = needsPalette;
                Bitmap image = shownFile.GetBitmap();
                this.pzpImage.Image = image;
                this.RefreshPalettes(resetPalettes, resetPalettes);
                if (needsPalette) // && resetPalettes)
                    this.CmbPalettesSelectedIndexChanged(null, null);
                else
                    this.RefreshColorControls();
            }
            this.RemoveProcessingLabel();
            this.LoadFocus();
            this.AllowDrop = true;
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
            if (loadedFile.NeedsPalette)
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
            FileInfo[] files = new DirectoryInfo(m_PalettePath).GetFiles("*.pal").OrderBy(x => x.Name).ToArray();
            Int32 filesLength = files.Length;
            for (Int32 i = 0; i < filesLength; ++i)
                palettes.AddRange(PaletteDropDownInfo.LoadSubPalettesInfoFromPalette(files[i], false, false, true));
            return palettes;
        }

        private void FrmFileConverterShown(Object sender, EventArgs e)
        {
            if (this.m_StartupParamPath != null)
                this.LoadFile(this.m_StartupParamPath, null, null);
            else
                this.ReloadUi(true);
        }

        private void TsmiSaveClick(Object sender, EventArgs e)
        {
            this.Save(false, false);
        }

        private void tsmiSaveSingleFrameClick(Object sender, EventArgs e)
        {
            this.Save(false, true);
        }

        private void TsmiSaveFramesClick(Object sender, EventArgs e)
        {
            this.Save(true, false);
        }

        private void TsmiSaveRawClick(Object sender, EventArgs e)
        {
            this.SaveFocus(this);
            Bitmap image;
            SupportedFileType shown = this.GetShownFile();
            if (shown == null || (image = shown.GetBitmap()) == null)
                return;
            String imagePath = shown.LoadedFile;
            String filename;
            using (SaveFileDialog sfd = new SaveFileDialog())
            {
                sfd.Filter = "All files (*.*)|*.*";
                sfd.InitialDirectory = Path.GetDirectoryName(imagePath);
                sfd.FileName = Path.GetFileNameWithoutExtension(imagePath) + ".dat";
                this.AllowDrop = false;
                DialogResult res = sfd.ShowDialog(this);
                if (res != DialogResult.OK)
                {
                    this.AllowDrop = true;
                    return;
                }
                filename = sfd.FileName;
            }
            this.ExecuteThreaded(() => this.SaveRaw(image, filename), false, false, false, "Saving");
        }

        private SupportedFileType SaveRaw(Bitmap image, String fileName)
        {
            Int32 stride;
            Byte[] rawData = ImageUtils.GetImageData(image, out stride, image.PixelFormat, true);
            File.WriteAllBytes(fileName, rawData);
            return null;
        }

        private void Save(Boolean frames, Boolean saveSingle)
        {
            this.SaveFocus(this);
            if (this.m_LoadedFile == null)
                return;
            SupportedFileType selectedItem;
            Boolean hasFrames = this.m_LoadedFile.Frames != null && this.m_LoadedFile.Frames.Length > 0;
            Boolean saveSingleFrame = !frames && saveSingle && hasFrames && this.numFrame.Value != -1;
            SupportedFileType loadedFile = saveSingleFrame ? this.m_LoadedFile.Frames[(Int32) this.numFrame.Value] : this.m_LoadedFile;
            Boolean hasEmptyFrames = frames && hasFrames && loadedFile.Frames.Any(f => f == null || f.GetBitmap() == null);
            Type selectType = frames ? typeof (FileImagePng) : loadedFile.GetType();
            Type[] saveTypes = FileTypesFactory.SupportedSaveTypes;
            Int32 nrOfSaveTypes = saveTypes.Length;
            FileClass loadedFileType = loadedFile.FileClass;
            FileClass frameFileType = FileClass.None;
            if (hasFrames && !saveSingleFrame)
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
                if (hasFrames && !saveSingleFrame)
                    message += "\nTry exporting as frames instead.";
                MessageBox.Show(this, message, GetTitle(), MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            Type saveableType = selectType;
            if (!filteredTypes.Contains(saveableType))
            {
                while (saveableType != null && saveableType != typeof(SupportedFileType))
                {
                    saveableType = saveableType.BaseType;
                    if (saveableType == null || !filteredTypes.Contains(saveableType))
                        continue;
                    selectType = saveableType;
                    break;
                }
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
            String title = "Save " + (frames? "as Frames" : "As");
            String filename = FileDialogGenerator.ShowSaveFileFialog(this, title, selectType, filteredTypes.ToArray(), typeof(FileImagePng), false, true, loadedFile.LoadedFile, out selectedItem);
            if (filename == null || selectedItem == null)
                return;
            List<Option> saveOptions = new List<Option>();
            Option[] saveOptionsChosen = null;
            try
            {
                // For export to frames only: collect the options for all frames.
                HashSet<String> saveOptsUnique = new HashSet<String>();
                if (frames && hasFrames)
                {
                    SupportedFileType[] internalFrames = loadedFile.Frames;
                    Int32 nrOfFrames = internalFrames.Length;
                    for (Int32 i = 0; i < nrOfFrames; ++i)
                    {
                        Option[] optsInt = selectedItem.GetSaveOptions(internalFrames[i], filename);
                        for (Int32 j = 0; j < optsInt.Length; ++j)
                        {
                            Option optInt = optsInt[j];
                            if (saveOptsUnique.Contains(optInt.Code))
                                continue;
                            saveOptsUnique.Add(optInt.Code);
                            saveOptions.Add(optInt);
                        }
                    }
                }
                else
                {
                    Option[] optsFile = selectedItem.GetSaveOptions(loadedFile, filename);
                    if (optsFile != null)
                    {
                        for (Int32 j = 0; j < optsFile.Length; ++j)
                        {
                            Option optFile = optsFile[j];
                            if (saveOptsUnique.Contains(optFile.Code))
                                continue;
                            saveOptsUnique.Add(optFile.Code);
                            saveOptions.Add(optFile);
                        }
                    }
                }
                if (frames && hasFrames)
                {
                    // Check if this is a loaded files range; in that case, prefer using the real filenames.
                    FileFrames framesFile = loadedFile as FileFrames;
                    Boolean fromfileRangeToMultiple = framesFile != null && framesFile.FromFileRange;
                    String filenameEx = Path.GetFileNameWithoutExtension(filename) + "-00000" + Path.GetExtension(filename);
                    saveOptions.Add(new Option("FRAMES_NEWNAMES", OptionInputType.Boolean, "Override internal names with new given name (names will be generated as \"" + filenameEx + "\"). Otherwise the current internal frame names are kept.", fromfileRangeToMultiple? "0" : "1"));
                    if (hasEmptyFrames)
                        saveOptions.Add(new Option("FRAMES_NULLFRAMES", OptionInputType.Boolean, "Save empty frames as 0-byte files", "1"));
                }

                if (saveOptions.Count > 0)
                {
                    SaveOptionInfo soi = new SaveOptionInfo();
                    soi.Name = GeneralUtils.DoubleAmpersands("Extra save options for " + selectedItem.LongTypeName);
                    soi.Properties = saveOptions.ToArray();
                    using (FrmOptions opts = new FrmOptions(GetTitle(), soi))
                    {
                        opts.Height = opts.OptimalHeight;
                        if (opts.ShowDialog(this) != DialogResult.OK)
                            return;
                        saveOptionsChosen = opts.GetSaveOptions();
                    }
                }
            }
            catch (FileTypeSaveException ex)
            {
                String message = "Cannot save " + (frames ? "frame of " : String.Empty) + "type " + loadedFile.ShortTypeName
                                 + " as type " + selectedItem.ShortTypeName + (String.IsNullOrEmpty(ex.Message) ? "." : ":\n" + ex.Message);
                MessageBox.Show(this, message, GetTitle(), MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            catch (ArgumentException ex)
            {
                String msg = GeneralUtils.RecoverArgExceptionMessage(ex, false);
                String message = "Cannot save " + (frames ? "frame of " : String.Empty) + "type " + loadedFile.ShortTypeName
                                 + " as type " + selectedItem.ShortTypeName + (String.IsNullOrEmpty(msg) ? "." : ":\n" + msg);
                MessageBox.Show(this, message, GetTitle(), MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            catch (NotImplementedException)
            {
                String message = "Sorry, saving is not available for type " + selectedItem.ShortTypeName + ".";
                MessageBox.Show(this, message, GetTitle(), MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            this.ExecuteThreaded(()=> this.SaveFile(frames, loadedFile, selectedItem, filename, saveOptionsChosen),false, false, false, "Saving");
        }

        private SupportedFileType SaveFile(Boolean frames, SupportedFileType loadedFile, SupportedFileType selectedItem, String filename, Option[] saveOptions)
        {
            try
            {
                if (!frames)
                    selectedItem.SaveAsThis(loadedFile, filename, saveOptions);
                else
                {
                    if (loadedFile.Frames == null)
                        return null;
                    //String path = Path.Combine(Path.GetDirectoryName(filename), Path.GetFileNameWithoutExtension(filename));
                    String path = Path.GetDirectoryName(filename);
                    String fileName = Path.GetFileNameWithoutExtension(filename) + "-";
                    String extension = Path.GetExtension(filename);
                    Boolean newNames = saveOptions != null && GeneralUtils.IsTrueValue(Option.GetSaveOptionValue(saveOptions, "FRAMES_NEWNAMES"));
                    Boolean nullFrames = saveOptions != null && GeneralUtils.IsTrueValue(Option.GetSaveOptionValue(saveOptions, "FRAMES_NULLFRAMES"));
                    if (saveOptions == null)
                        saveOptions = new Option[0];
                    for (Int32 i = 0; i < loadedFile.Frames.Length; ++i)
                    {
                        SupportedFileType frame = loadedFile.Frames[i];
                        String framePath = Path.Combine(path, (newNames ? (fileName + i.ToString("D5")) : Path.GetFileNameWithoutExtension(frame.LoadedFileName)) + extension);
                        if (frame.GetBitmap() != null)
                            selectedItem.SaveAsThis(frame, framePath, saveOptions);
                        else if (nullFrames) // Allow empty frames as empty files.
                            File.WriteAllBytes(framePath, new Byte[0]);
                    }
                }
            }
            catch (FileTypeSaveException ex)
            {
                String message = "Error saving " + (frames ? "frame of " : String.Empty) + "type " + loadedFile.ShortTypeName
                                 + " as type " + selectedItem.ShortTypeName + (String.IsNullOrEmpty(ex.Message) ? "." : ":\n" + ex.Message);
#if DEBUG
                message += "\n" + ex.StackTrace;
#endif
                this.Invoke(new Action(() => this.ShowMessageBox(message, MessageBoxButtons.OK, MessageBoxIcon.Warning)));
            }
            catch (ArgumentException ex)
            {
                String msg = GeneralUtils.RecoverArgExceptionMessage(ex, false);
                String message = "Error saving " + (frames ? "frame of " : String.Empty) + "type " + loadedFile.ShortTypeName
                                 + " as type " + selectedItem.ShortTypeName + (String.IsNullOrEmpty(msg) ? "." : ":\n" + msg);
#if DEBUG
                message += "\n" + ex.StackTrace;
#endif
                this.Invoke(new Action(() => this.ShowMessageBox(message, MessageBoxButtons.OK, MessageBoxIcon.Warning)));
            }
            catch (NotImplementedException)
            {
                String message = "Sorry, saving is not available for type " + selectedItem.ShortTypeName + ".";
                this.Invoke(new Action(() => this.ShowMessageBox(message, MessageBoxButtons.OK, MessageBoxIcon.Warning)));
            }
            catch (NotSupportedException)
            {
                String message = "Sorry, saving is not available for type " + selectedItem.ShortTypeName + ".";
                this.Invoke(new Action(() => this.ShowMessageBox(message, MessageBoxButtons.OK, MessageBoxIcon.Warning)));
            }
            return null;
        }

        private void TsmiExitClick(Object sender, EventArgs e)
        {
            this.Close();
        }

        private void SaveFocus(Control ctrl)
        {
            if (this.m_Loading || this.m_BusyStatusLabel != null || !ctrl.ContainsFocus)
                return;
            this.m_FocusedControl = ctrl;
            foreach (Control control in ctrl.Controls)
            {
                if (!control.ContainsFocus)
                    continue;
                this.SaveFocus(control);
                break;
            }
        }

        private void LoadFocus()
        {
            if (this.m_FocusedControl != null && this.m_FocusedControl.Enabled && !this.m_FocusedControl.ContainsFocus)
                this.m_FocusedControl.Focus();
            this.m_FocusedControl = null;
        }

        protected void FrmFileConverterFormClosing(Object sender, FormClosingEventArgs e)
        {
            if (!this.m_Loading || this.m_ProcessingThread == null || !this.m_ProcessingThread.IsAlive)
                return;
            DialogResult result = this.ShowMessageBox("Operations are in progress! Are you sure you want to quit?", MessageBoxButtons.YesNo, MessageBoxIcon.Exclamation);
            if (result == DialogResult.Yes)
            {
                this.m_ProcessingThread.Abort();
            }
            else
            {
                // abort the closing of the form.
                e.Cancel = true;
            }
        }

        private void TsmiOpenClick(Object sender, EventArgs e)
        {
            this.SaveFocus(this);
            SupportedFileType selectedItem;
            String[] filenames = FileDialogGenerator.ShowOpenFileFialog(this, null, false, FileTypesFactory.SupportedOpenTypes, FileTypesFactory.AutoDetectTypes, this.m_LastOpenedFolder, "images", null, true, out selectedItem);
            if (filenames == null ||filenames.Length == 0)
                return;
            String filename = filenames[0];
            this.m_LastOpenedFolder = Path.GetDirectoryName(filename);
            SupportedFileType[] preferredTypes = null;
            if (selectedItem == null)
                preferredTypes = FileDialogGenerator.IdentifyByExtension<SupportedFileType>(FileTypesFactory.AutoDetectTypes, filename);
            else
            {
                SupportedFileType curr = selectedItem;
                Type currType = curr.GetType();
                Type[] subTypes = FileTypesFactory.AutoDetectTypes.Where(x => currType.IsAssignableFrom(x)).ToArray();
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
            this.LoadFile(filenames, selectedItem, preferredTypes);
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
            // 1-bit and 2-bit palettes can not currently be saved.
            this.btnSavePalette.Enabled = fileLoaded && cs != ColorStatus.None && Math.Abs(loadedFile.BitsPerPixel) >= 4;
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
                bpp = Math.Abs(loadedFile.BitsPerPixel);
                // Fix for palettes larger than the color depth would normally allow (can happen on png)
                while (1 << bpp < pal.Length)
                    bpp *= 2;
                bpp = Math.Min(8, bpp);
            }
            else
            {
                bpp = 0;
            }
            Boolean showPal = bpp > 0 && bpp <= 8;
            this.palColorPalette.Visible = showPal;
            if (showPal)
                PalettePanel.InitPaletteControl(bpp, this.palColorPalette, pal, PALETTE_DIM);
            this.LoadFocus();
        }

        private void NumFrameValueChanged(Object sender, EventArgs e)
        {
            if (this.m_LoadedFile != null && this.m_LoadedFile.Frames != null && this.m_LoadedFile.Frames.Length > 0)
            {
                this.SaveFocus(this);
                this.ReloadUi(false);
            }
        }

        private void CmbPalettesSelectedIndexChanged(Object sender, EventArgs e)
        {
            this.SaveFocus(this);
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

        private void BtnResetPaletteClick(Object sender, EventArgs e)
        {
            this.SaveFocus(this);
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
                        DialogResult dr = MessageBox.Show("This will remove all changes you have made to the palette since it was loaded!\n\nAre you sure you want to continue?", GetTitle(), MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                        if (dr != DialogResult.Yes)
                            return;
                    }
                    Color zeroCol = (currentPal.Colors != null && currentPal.Colors.Length > 1) ? currentPal.Colors[0] : Color.Black;
                    currentPal.Revert(this.GetCurrentTypeTransparencyMask());
                    Color[] colors = currentPal.Colors;
                    this.GetShownFile().SetColors(colors);

                    // If CGA color 0 changed: change all CGA palettes.
                    if (this.GetShownFile().BitsPerPixel == -2 && colors.Length > 0 && zeroCol.ToArgb() != colors[0].ToArgb())
                    {
                        PaletteDropDownInfo[] itemsToChange = this.GetPalettes(2, false, this.GetCurrentTypeTransparencyMask()).Where(p => p.Name.StartsWith("CGA ")).ToArray();
                        for (Int32 i = 0; i < itemsToChange.Length; ++i)
                        {
                            PaletteDropDownInfo cgaPal = itemsToChange[i];
                            if (cgaPal.Colors.Length > 0)
                                cgaPal.Colors[0] = cgaPal.ColorBackup != null && cgaPal.ColorBackup.Length > 1 ? cgaPal.ColorBackup[0] : Color.Black;
                        }
                    }


                    break;
                default:
                    return;
            }
            SupportedFileType shownFile = this.GetShownFile();
            this.pzpImage.Image = shownFile.GetBitmap();
            this.pzpImage.RefreshImage();
            this.RefreshColorControls();
        }

        private void BtnSavePaletteClick(Object sender, EventArgs e)
        {
            this.SaveFocus(this);
            ColorStatus cs = this.GetColorStatus();
            if (cs == ColorStatus.None)
                return;
            SupportedFileType loadedFile = this.GetShownFile();
            Int32 bpp = Math.Abs(loadedFile.BitsPerPixel);
            if (bpp == 1)
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
                    currentPal = new PaletteDropDownInfo(null, bpp, loadedFile.GetColors(), null, -1, false, false);
                    break;
                default:
                    return;
            }
            PaletteDropDownInfo palInfo;
            using (FrmManagePalettes palSave = new FrmManagePalettes(currentPal.BitsPerPixel, this.m_PalettePath))
            {
                palSave.Icon = this.Icon;
                palSave.Title = GetTitle();
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
                palInfo = palSave.PaletteToSave;
            }
            // If null, it was a simple immediate overwrite, without the management box ever popping up, so
            // just consider the current entry "saved".
            if (palInfo == null)
                currentPal.ClearRevert();
            else
            {
                // Get source position, reload all, then loop through to check which one to reselect.
                this.RefreshPalettes(true, true);
                String source = palInfo.SourceFile;
                Int32 index = palInfo.Entry;
                foreach (PaletteDropDownInfo pdd in this.cmbPalettes.Items)
                {
                    if (pdd.SourceFile != source || pdd.Entry != index)
                        continue;
                    this.cmbPalettes.SelectedItem = pdd;
                    break;
                }
            }
            this.LoadFocus();
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
            Int32 bpp = shown == null ? 0 : Math.Abs(shown.BitsPerPixel);
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

        private void PalColorViewerColorLabelMouseDoubleClick(Object sender, PaletteClickEventArgs e)
        {
            if (e.Button != MouseButtons.Left)
                return;
            PalettePanel palPanel = sender as PalettePanel;
            if (palPanel == null)
                return;
            this.EditColor(palPanel, e.Index, e.Color);
        }

        private void SetPaletteColor(PalettePanel palpanel, Int32 colindex, Color color, SupportedFileType loadedFile)
        {
            ColorStatus cs = this.GetColorStatus();
            if (colindex >= (1 << Math.Abs(loadedFile.BitsPerPixel)))
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
                    PaletteDropDownInfo[] itemsToChange;
                    // If CGA color 0: change all CGA palettes.
                    if (loadedFile.BitsPerPixel == -2 && colindex == 0)
                    {
                        itemsToChange = this.GetPalettes(2, false, this.GetCurrentTypeTransparencyMask()).ToArray();
                    }
                    else
                    {
                        itemsToChange = new PaletteDropDownInfo[] { this.cmbPalettes.SelectedItem as PaletteDropDownInfo };
                    }
                    for (Int32 i = 0; i < itemsToChange.Length; ++i)
                    {
                        PaletteDropDownInfo currentPal = itemsToChange[i];
                        if (currentPal != null && currentPal.Colors.Length > colindex)
                            currentPal.Colors[colindex] = color;
                    }
                }
                SupportedFileType shownFile = this.GetShownFile();
                this.pzpImage.Image = shownFile.GetBitmap();
            }
            this.pzpImage.RefreshImage();
            this.RefreshColorControls();
        }


        private void PalColorViewerColorLabelMouseClick(Object sender, PaletteClickEventArgs e)
        {
            if (e.Button != MouseButtons.Right)
                return;
            ContextMenu cm = new ContextMenu();
            if (this.palColorPalette.Palette.Length <= e.Index)
                return;
            this.SaveFocus(this);
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
            Int32 colIndex = (Int32)cm.Tag;
            Color color = this.palColorPalette.Palette[colIndex];
            this.EditColor(this.palColorPalette, colIndex, color);
        }

        private void EditColor(PalettePanel palPanel, Int32 colindex, Color color)
        {
            SupportedFileType shownFile = this.GetShownFile();
            if (shownFile == null)
                return;
            this.SaveFocus(this);
            Color newCol;
            if (shownFile.BitsPerPixel == -2)
            {
                // CGA Mode.
                using (FrmPalette palFrm = new FrmPalette(4, PaletteUtils.GetEgaPalette(), true, ColorSelMode.Single))
                {
                    Int32 selectedCol = PaletteUtils.FindEgaColor(color);
                    palFrm.SelectedIndices = selectedCol == 0 ? null : new Int32[] {selectedCol};
                    palFrm.Text = "Full CGA palette";
                    if (palFrm.ShowDialog(this) != DialogResult.OK)
                        return;
                    newCol = palFrm.GetSelectedColors()[0];
                }
            }
            else
            {
                using (ColorDialog cdl = new ColorDialog())
                {
                    cdl.Color = color;
                    cdl.FullOpen = true;
                    cdl.CustomColors = this.pzpImage.CustomColors;
                    this.AllowDrop = false;
                    DialogResult res = cdl.ShowDialog(this);
                    this.pzpImage.CustomColors = cdl.CustomColors;
                    if (res != DialogResult.OK)
                    {
                        this.AllowDrop = true;
                        return;
                    }
                    newCol = cdl.Color;
                }
            }
            newCol = Color.FromArgb(color.A, newCol);
            this.SetPaletteColor(palPanel, colindex, newCol, shownFile);
            this.AllowDrop = true;
            this.LoadFocus();
        }

        private void SetColorTransparent(Object sender, EventArgs e)
        {
            this.SetPalColorAlpha(sender, 0);
        }

        private void SetColorOpaque(Object sender, EventArgs e)
        {
            this.SetPalColorAlpha(sender, 255);
        }

        private void SetColorAlpha(Object sender, EventArgs e)
        {
            MenuItem cm = sender as MenuItem;
            if (cm == null)
                return;
            if (!(cm.Tag is Int32))
                return;
            Int32 index = (Int32)cm.Tag;
            if (this.palColorPalette.Palette.Length <= index)
                return;
            Color col = this.palColorPalette.Palette[index];
            using (FrmSetAlpha alphaForm = new FrmSetAlpha(col.A))
            {
                this.AllowDrop = false;
                if (alphaForm.ShowDialog(this) != DialogResult.OK)
                {
                    this.AllowDrop = true;
                    return;
                }
                col = Color.FromArgb(alphaForm.Alpha, col);
            }
            SupportedFileType loadedFile = this.GetShownFile();
            this.SetPaletteColor(this.palColorPalette, index, col, loadedFile);
            this.AllowDrop = true;

        }

        private void SetPalColorAlpha(Object sender, Int32 alpha)
        {
            MenuItem cm = sender as MenuItem;
            if (cm == null)
                return;
            if (!(cm.Tag is Int32))
                return;
            Int32 index = (Int32)cm.Tag;
            if (this.palColorPalette.Palette.Length <= index)
                return;
            Color col = this.palColorPalette.Palette[index];
            col = Color.FromArgb(alpha, col);
            SupportedFileType loadedFile = this.GetShownFile();
            this.SetPaletteColor(this.palColorPalette, index, col, loadedFile);
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
            this.SaveFocus(this);
            Bitmap image = shownFile.GetBitmap();
            List<PaletteDropDownInfo> allPalettes = new List<PaletteDropDownInfo>();
            allPalettes.AddRange(this.m_DefaultPalettes);
            allPalettes.AddRange(this.m_ReadPalettes);
            String imagePath = shownFile.LoadedFile;
            if (String.IsNullOrEmpty(imagePath))
                imagePath = shownFile.LoadedFileName;
            Int32 frameWidth;
            Int32 frameHeight;
            Int32 maxFrames;
            Color? trimColor;
            Int32? trimIndex;
            Int32 matchBpp;
            Color[] matchPalette;
            using (FrmFramesCutter frameCutter = new FrmFramesCutter(image, this.pzpImage.CustomColors, allPalettes.ToArray()))
            {
                frameCutter.CustomColors = this.pzpImage.CustomColors;
                this.AllowDrop = false;
                DialogResult dr = frameCutter.ShowDialog(this);
                this.pzpImage.CustomColors = frameCutter.CustomColors;
                if (dr != DialogResult.OK)
                {
                    this.AllowDrop = true;
                    return;
                }
                frameWidth = frameCutter.FrameWidth;
                frameHeight = frameCutter.FrameHeight;
                maxFrames = frameCutter.Frames;
                trimColor = frameCutter.TrimColor;
                trimIndex = frameCutter.TrimIndex;
                matchBpp = frameCutter.MatchBpp;
                matchPalette = frameCutter.MatchPalette;
            }
            this.ExecuteThreaded(() => FileFrames.CutImageIntoFrames(image, imagePath, frameWidth, frameHeight, maxFrames, trimColor, trimIndex, matchBpp, matchPalette, false, shownFile.NeedsPalette),
                false, true, true, "Splitting into frames");
        }

        private void TsmiFramesToSingleImageClick(Object sender, EventArgs e)
        {
            if (this.m_LoadedFile == null)
                return;
            SupportedFileType[] frames = this.m_LoadedFile.Frames;
            Int32 nrOfframes;
            if (frames == null || (nrOfframes = frames.Length) == 0)
                return;
            this.SaveFocus(this);
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
            Option[] so = new Option[5];
            so[0] = new Option("FRW", OptionInputType.Number, "Frame width", maxWidth + ",", maxWidth.ToString());
            so[1] = new Option("FRH", OptionInputType.Number, "Frame height", maxHeight + ",", maxHeight.ToString());
            so[2] = new Option("FRC", OptionInputType.Boolean, "Center in frame", "0");
            so[3] = new Option("FPL", OptionInputType.Number, "Frames per line", "1," + nrOfframes, ((Int32)Math.Sqrt(nrOfframes)).ToString());
            if (highestBpp <= 8)
                so[4] = new Option("BGI", OptionInputType.Palette, "Background color around frames", highestBpp + "|" + paletteStr, "0");
            else
                so[4] = new Option("BGC", OptionInputType.Color, "Background color around frames", hasAlpha ? "A" : hasSimpleTrans ? "T" : String.Empty, "#00000000");
            SaveOptionInfo soi = new SaveOptionInfo();
            soi.Name = "Frames to single image";
            soi.Properties = so;
            
            try
            {
                using (FrmOptions opts = new FrmOptions(GetTitle(), soi))
                {
                    opts.Height = opts.OptimalHeight;
                    if (opts.ShowDialog(this) != DialogResult.OK)
                        return;
                    so = opts.GetSaveOptions();
                }
            }
            catch (ArgumentException ex)
            {
                MessageBox.Show(this, "Error initializing conversion options: " + GeneralUtils.RecoverArgExceptionMessage(ex, true), GetTitle(), MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            
            Int32 frameWidth;
            Int32.TryParse(Option.GetSaveOptionValue(so, "FRW"), out frameWidth);
            Int32 frameHeight;
            Int32.TryParse(Option.GetSaveOptionValue(so, "FRH"), out frameHeight);
            Int32 framesPerLine;
            Int32.TryParse(Option.GetSaveOptionValue(so, "FPL"), out framesPerLine);
            Boolean centerFrames = GeneralUtils.IsTrueValue(Option.GetSaveOptionValue(so, "FRC"));
            Byte fillPalIndex = 0;
            Color fillColor = Color.Empty;
            if (highestBpp <= 8)
                Byte.TryParse(Option.GetSaveOptionValue(so, "BGI"), out fillPalIndex);
            else
                fillColor = ColorUtils.ColorFromHexString(Option.GetSaveOptionValue(so, "BGC"));
            this.ExecuteThreaded(() => this.FramesToSingleImage(frameImages, frameWidth, frameHeight, centerFrames, framesPerLine, fillPalIndex, fillColor), false, true, true, "Combining frames");
        }

        private SupportedFileType FramesToSingleImage(Bitmap[] images, Int32 framesWidth, Int32 framesHeight, Boolean centerFrames, Int32 framesPerLine, Byte backFillPalIndex, Color backFillColor)
        {
            Bitmap bm = ImageUtils.BuildImageFromFrames(images, framesWidth, framesHeight, centerFrames, framesPerLine, backFillPalIndex, backFillColor);
            FileImagePng returnImg = new FileImagePng();
            returnImg.LoadFile(bm, this.m_LoadedFile.LoadedFile);
            return returnImg;
        }

        private void TsmiToHeightMapAdvClick(Object sender, EventArgs e)
        {
            this.GenerateHeightMap(true);
        }

        private void TsmiToHeightMapClick(Object sender, EventArgs e)
        {
            this.GenerateHeightMap(false);
        }

        private void GenerateHeightMap(Boolean selectHeightMap)
        {
            FileMapWwCc1Pc map = this.m_LoadedFile as FileMapWwCc1Pc;
            if (map == null)
                return;
            this.SaveFocus(this);
            String loadedPath = this.m_LoadedFile.LoadedFile;
            String baseFileName = Path.Combine(Path.GetDirectoryName(loadedPath), Path.GetFileNameWithoutExtension(loadedPath));
            String pngFileName = baseFileName + ".png";
            Bitmap plateauImage = null;
            if (selectHeightMap)
            {
                SupportedFileType selectedType;
                String filename = FileDialogGenerator.ShowOpenFileFialog(this, "Select height levels image", new Type[] { typeof(FileImage) }, null, pngFileName, "images", null, true, out selectedType);
                if (filename == null)
                    return;
                this.m_LastOpenedFolder = Path.GetDirectoryName(filename);
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
                    MessageBox.Show(this, "Could not load file as " + selectedType.LongTypeName + ":\n\n" + e.Message, GetTitle(), MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                if (plateauImage.Width != 64 || plateauImage.Height != 64)
                {
                    MessageBox.Show(this, "Height levels image needs to be 64×64.", GetTitle(), MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
            }
            this.ExecuteThreaded(() => HeightMapGenerator.GenerateHeightMapImage64x64(map, plateauImage, null), true, true, true, "Generating height map");
        }

        private void TsmiTo65X65HeightMapClick(Object sender, EventArgs e)
        {
            SupportedFileType image = this.GetShownFile();
            if (image == null || image.Width != 64 || image.Height != 64 || image.FileClass == FileClass.CcMap)
                return;
            this.SaveFocus(this);
            String baseFileName = Path.Combine(Path.GetDirectoryName(image.LoadedFile), Path.GetFileNameWithoutExtension(image.LoadedFile));
            String imgFileName = baseFileName + ".img";
            this.ExecuteThreaded(() => this.Make65x65HeightMap(image, imgFileName), true, false, false, "Creating height map");
        }

        private FileImgWwN64 Make65x65HeightMap(SupportedFileType image, String imgFileName)
        {
            Bitmap bm = HeightMapGenerator.GenerateHeightMapImage65x65(image.GetBitmap());
            //Byte[] imageData = ImageUtils.GetSavedImageData(bm, ref imgFileName);
            FileImgWwN64 file = new FileImgWwN64();
            file.LoadGrayImage(bm, Path.GetFileName(imgFileName), imgFileName);
            return file;
        }

        private void TsmiToPlateausClick(Object sender, EventArgs e)
        {
            FileMapWwCc1Pc map = this.m_LoadedFile as FileMapWwCc1Pc;
            if (map == null)
                return;
            this.SaveFocus(this);
            this.ExecuteThreaded(() => HeightMapGenerator.GeneratePlateauImage64x64(map, "_lvl"), false, false, false, "Generating plateaus");
        }

        private void TsmiCombineShadowsClick(Object sender, EventArgs e)
        {
            if (this.m_LoadedFile == null || this.m_LoadedFile.Frames == null || this.m_LoadedFile.Frames.Length == 0)
                return;
            this.SaveFocus(this);
            Option[] so = new Option[1];
            so[0] = new Option("IND", OptionInputType.Number, "Output shadow index", "0,255", "4");
            SaveOptionInfo soi = new SaveOptionInfo();
            soi.Name = "Shadow combining options:";
            soi.Properties = so;
            try
            {
                using (FrmOptions opts = new FrmOptions(GetTitle(), soi))
                {
                    opts.Height = opts.OptimalHeight;
                    if (opts.ShowDialog(this) != DialogResult.OK)
                        return;
                }
            }
            catch (ArgumentException ex)
            {
                MessageBox.Show(this, "Error initializing conversion options: " + GeneralUtils.RecoverArgExceptionMessage(ex, true), GetTitle(), MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            Int32 ind;
            Int32.TryParse(Option.GetSaveOptionValue(so, "IND"), out ind);
            this.ExecuteThreaded(() => FileFramesWwShpTs.CombineShadows(this.m_LoadedFile, 1, (Byte) ind), false, true, false, "Combining shadows");
        }

        private void TsmiSplitShadowsClick(Object sender, EventArgs e)
        {
            if (this.m_LoadedFile == null || this.m_LoadedFile.Frames == null || this.m_LoadedFile.Frames.Length == 0)
                return;
            this.SaveFocus(this);
            Option[] so = new Option[1];
            so[0] = new Option("IND", OptionInputType.Number, "Input shadow index", "0,255", "4");
            SaveOptionInfo soi = new SaveOptionInfo();
            soi.Name = "Shadow splitting options:";
            soi.Properties = so;
            try
            {
                using (FrmOptions opts = new FrmOptions(GetTitle(), soi))
                {
                    opts.Height = opts.OptimalHeight;
                    if (opts.ShowDialog(this) != DialogResult.OK)
                        return;
                }
            }
            catch (ArgumentException ex)
            {
                MessageBox.Show(this, "Error initializing conversion options: " + GeneralUtils.RecoverArgExceptionMessage(ex, true), GetTitle(), MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            Int32 ind;
            Int32.TryParse(Option.GetSaveOptionValue(so, "IND"), out ind);
            this.ExecuteThreaded(() => FileFramesWwShpTs.SplitShadows(this.m_LoadedFile, (Byte) ind, 1), false, true, false, "Splitting shadows");
        }

        private void TsmiApplyTransparencyMaskClick(Object sender, EventArgs e)
        {
            if (this.m_LoadedFile == null || this.m_LoadedFile.Frames == null || this.m_LoadedFile.Frames.Length == 0)
                return;
            this.SaveFocus(this);



            Option[] so = new Option[1];
            so[0] = new Option("IND", OptionInputType.Number, "Input shadow index", "0,255", "4");
            SaveOptionInfo soi = new SaveOptionInfo();
            soi.Name = "Shadow splitting options:";
            soi.Properties = so;
            try
            {
                using (FrmOptions opts = new FrmOptions(GetTitle(), soi))
                {
                    opts.Height = opts.OptimalHeight;
                    if (opts.ShowDialog(this) != DialogResult.OK)
                        return;
                }
            }
            catch (ArgumentException ex)
            {
                return;
            }
            Int32 ind;
            Int32.TryParse(Option.GetSaveOptionValue(so, "IND"), out ind);
            //this.ExecuteThreaded(() => FileFrames.ApplyTransparencyMask(this.m_LoadedFile, (Byte)ind, 1), false, true, false, "Splitting shadows");
        }

        private void TsmiSplitTransparencyMaskClick(Object sender, EventArgs e)
        {

        }

        private void TsmiPasteOnFramesClick(Object sender, EventArgs e)
        {
            if (this.m_LoadedFile == null)
                return;
            Boolean singleImage = (this.m_LoadedFile.Frames == null || this.m_LoadedFile.Frames.Length == 0) && this.m_LoadedFile.GetBitmap() != null;
            if (!singleImage && this.m_LoadedFile.Frames.Length == 0)
                return;
            this.SaveFocus(this);
            try
            {
                SupportedFileType[] frames = singleImage ? new SupportedFileType[] { this.m_LoadedFile } : this.m_LoadedFile.Frames;
                Int32 nrOfFrames = frames.Length;
                Int32 maxWidth = frames.Max(fr => fr == null ? 0 : fr.Width);
                Int32 maxHeight = frames.Max(fr => fr == null ? 0 : fr.Height);
                Bitmap image;
                Point pastePoint;
                Int32[] frameRange;
                Boolean keepIndices;
                // Pastebox deliberately does not dispose its Image, so it can be passed on to the function.
                using (FrmPasteOnFrames pasteBox = new FrmPasteOnFrames(nrOfFrames, maxWidth, maxHeight, Math.Abs(this.m_LoadedFile.BitsPerPixel), this.m_LastOpenedFolder))
                {
                    DialogResult dr = pasteBox.ShowDialog(this);
                    this.m_LastOpenedFolder = pasteBox.LastSelectedFolder;
                    image = pasteBox.Image;
                    if (dr != DialogResult.OK)
                    {
                        if (image != null)
                            image.Dispose();
                        return;
                    }
                    pastePoint = pasteBox.Coords;
                    frameRange = pasteBox.FrameRange;
                    keepIndices = pasteBox.KeepIndices;
                }
                this.ExecuteThreaded(() => this.PasteOnFrames(this.m_LoadedFile, image, pastePoint, frameRange, keepIndices, true), false, false, false, "Pasting on frames");
            }
            catch (ArgumentException ex)
            {
                MessageBox.Show(this, GeneralUtils.RecoverArgExceptionMessage(ex, true), GetTitle(), MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private SupportedFileType PasteOnFrames(SupportedFileType framesContainer, Bitmap image, Point pasteLocation, Int32[] framesRange, Boolean keepIndices, Boolean disposeImage)
        {
            SupportedFileType newfile = FileFrames.PasteImageOnFrames(framesContainer, image, pasteLocation, framesRange, keepIndices);
            if (disposeImage)
                image.Dispose();
            return newfile;
        }

        private void TsmiExtractPalClick(Object sender, EventArgs e)
        {
            SupportedFileType shownImage = this.GetShownFile();
            if (shownImage == null)
                return;
            this.SaveFocus(this);
            Int32 bpp = Math.Abs(shownImage.BitsPerPixel);
            Color[] pal = shownImage.GetColors();
            Int32 nrOfColors = pal.Count();
            if (nrOfColors == 0 || (bpp != 1 && bpp != 2 && bpp != 4 && bpp != 8))
                return;
            ColorStatus cs = this.GetColorStatus();
            Int32 fullPal = 1 << bpp;
            Int32 height = (Int32) Math.Sqrt(fullPal);
            Int32 width = fullPal / height;
            Byte[] image = new Byte[fullPal];
            for (Int32 i = 0; i < fullPal; ++i)
                image[i] = (Byte)i;
            if (bpp == 2)
                bpp = 4;
            PixelFormat pf = ImageUtils.GetIndexedPixelFormat(bpp);
            image = ImageUtils.ConvertFrom8Bit(image, width, height, bpp, true);
            Int32 stride = ImageUtils.GetMinimumStride(width, bpp);
            Bitmap bm = ImageUtils.BuildImage(image, width, height, stride, pf, pal, Color.Black);
            FileImagePng palImage = new FileImagePng();
            String path = Path.GetDirectoryName(shownImage.LoadedFile);
            String name;
            PaletteDropDownInfo pddi = this.cmbPalettes.SelectedItem as PaletteDropDownInfo;
            if (cs == ColorStatus.External && pddi != null)
            {
                name = pddi.SourceFile;
                if (name == null)
                    name = Regex.Replace(pddi.Name, "[" + Regex.Escape(new String(Path.GetInvalidFileNameChars())) + "]", String.Empty);
                else if (name.EndsWith(".pal", StringComparison.InvariantCultureIgnoreCase))
                    name = name.Substring(0, name.Length - 4);
            }
            else
                name = Path.GetFileNameWithoutExtension(shownImage.LoadedFile);
            palImage.LoadFile(bm, Path.Combine(path, name + ".png"));
            this.ReloadWithDispose(palImage, true, true, true);
        }

        private void TsmiImageToPalette4BitClick(Object sender, EventArgs e)
        {
            this.ImageToPalette(true);
        }

        private void TsmiImageToPalette8BitClick(Object sender, EventArgs e)
        {
            this.ImageToPalette(false);
        }

        private void ImageToPalette(Boolean fourBit)
        {
            SupportedFileType shownImage = this.GetShownFile();
            if (shownImage == null || shownImage.GetBitmap() == null)
                return;
            this.SaveFocus(this);
            try
            {
                String maxCol = (fourBit ? 16 : 256).ToString(NumberFormatInfo.InvariantInfo);
                Option[] so = new Option[3];
                so[0] = new Option("CRX", OptionInputType.Number, "X", "0," + (shownImage.Width - 1), "0");
                so[1] = new Option("CRY", OptionInputType.Number, "Y", "0," + (shownImage.Height - 1), "0");
                so[2] = new Option("CRN", OptionInputType.Number, "Limit amount of colors to", "0," + maxCol, maxCol);
                SaveOptionInfo soi = new SaveOptionInfo();
                soi.Name = "This will take a (wrapping) line of pixels and convert them to a color palette.\nCoordinates of start pixel:";
                soi.Properties = so;
                try
                {
                    using (FrmOptions opts = new FrmOptions(GetTitle(), soi))
                    {
                        opts.Height = opts.OptimalHeight;
                        if (opts.ShowDialog(this) != DialogResult.OK)
                            return;
                    }
                }
                catch (ArgumentException ex)
                {
                    MessageBox.Show(this, "Error initializing conversion options: " + GeneralUtils.RecoverArgExceptionMessage(ex, true), GetTitle(), MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                Int32 coordX;
                Int32.TryParse(Option.GetSaveOptionValue(so, "CRX"), out coordX);
                Int32 coordY;
                Int32.TryParse(Option.GetSaveOptionValue(so, "CRY"), out coordY);
                Int32 limit;
                Int32.TryParse(Option.GetSaveOptionValue(so, "CRN"), out limit);
                this.ExecuteThreaded(() => this.ConvertToPalette(shownImage, coordX, coordY, fourBit, limit), true, true, true, "Converting to palette");
            }
            catch (ArgumentException ex)
            {
                MessageBox.Show(this, GeneralUtils.RecoverArgExceptionMessage(ex, true), GetTitle(), MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private SupportedFileType ConvertToPalette(SupportedFileType file, Int32 x, Int32 y, Boolean fourBit, Int32 limit)
        {
            if (file == null || (file.GetBitmap()) == null)
                return null;
            this.SaveFocus(this);
            Int32 palWidth = fourBit ? 4 : 16;
            Int32 palHeight = (limit + palWidth -1) / palWidth;
            Int32 palSize = palWidth * palHeight;
            Int32 palStride = fourBit ? palWidth / 2 : palWidth;
            String path = Path.GetDirectoryName(file.LoadedFile);
            String name = Path.GetFileNameWithoutExtension(file.LoadedFile);
            Byte[] imageData = ImageUtils.GetImageData(file.GetBitmap(), PixelFormat.Format24bppRgb);
            Int32 startPoint = (y * file.Width + x) * 3;
            Int32 palLen = Math.Min(3 * limit, imageData.Length - startPoint);
            Int32 palEnd = startPoint + palLen;
            Byte[] paletteData = new Byte[palLen];
            Int32 palPtr = 0;
            for (Int32 i = startPoint; i < palEnd; i += 3)
            {
                paletteData[palPtr++] = imageData[i + 2];
                paletteData[palPtr++] = imageData[i + 1];
                paletteData[palPtr++] = imageData[i];
            }
            Color[] col = ColorUtils.ReadEightBitPaletteFile(paletteData, false);
            Byte[] newImageData = Enumerable.Range(0, limit).Select(b => (Byte)b).ToArray();
            Byte[] fullImageData = new Byte[palSize];
            Array.Copy(newImageData, fullImageData, limit);
            if (fourBit)
                fullImageData = ImageUtils.ConvertFrom8Bit(fullImageData, palWidth, palWidth, 4, true);
            PixelFormat pf = fourBit ? PixelFormat.Format4bppIndexed : PixelFormat.Format8bppIndexed;
            Bitmap bm = ImageUtils.BuildImage(fullImageData, palWidth, palHeight, palStride, pf, col, Color.Black);
            ColorPalette adjustedPal = ImageUtils.GetPalette(col, limit);
            bm.Palette = adjustedPal;
            FileImagePng palImage = new FileImagePng();
            palImage.LoadFile(bm, Path.Combine(path, name + ".png"));
            return palImage;
        }

        private void TsmiChangeTo24BitRgbClick(Object sender, EventArgs e)
        {
            SupportedFileType fileToEdit = this.m_LoadedFile;
            if (fileToEdit == null || (fileToEdit.FileClass & FileClass.Image | FileClass.FrameSet) == 0)
                return;
            this.ExecuteThreaded(() => this.ChangeToRgb(fileToEdit, 24), true, true, true, "Changing to 24bpp RGB");
        }

        private void TsmiChangeTo32BitArgbClick(Object sender, EventArgs e)
        {
            SupportedFileType fileToEdit = this.m_LoadedFile;
            if (fileToEdit == null || (fileToEdit.FileClass & FileClass.Image | FileClass.FrameSet) == 0)
                return;
            this.ExecuteThreaded(()=> this.ChangeToRgb(fileToEdit, 32), true, true, true, "Changing to 32bpp ARGB");
        }

        private SupportedFileType ChangeToRgb(SupportedFileType fileToEdit, Int32 bpp)
        {
            PixelFormat pf = bpp == 24 ? PixelFormat.Format24bppRgb : PixelFormat.Format32bppArgb;
            if (!fileToEdit.IsFramesContainer)
            {
                Bitmap image = fileToEdit.GetBitmap();
                Int32 stride;
                Byte[] resBytes = ImageUtils.GetImageData(image, out stride, pf);
                Bitmap result = ImageUtils.BuildImage(resBytes, image.Width, image.Height, stride, pf, null, null);
                FileImagePng newFile = new FileImagePng();
                newFile.LoadFile(result, fileToEdit.LoadedFile);
                return newFile;
            }
            else
            {
                Int32 frames = fileToEdit.Frames.Length;
                FileFrames newFile = new FileFrames(fileToEdit);
                newFile.SetFileNames(fileToEdit.LoadedFile);
                newFile.SetCommonPalette(true);
                newFile.SetBitsPerPixel(bpp);
                for (Int32 i = 0; i < frames; ++i)
                {
                    FileImageFrame newFrame = new FileImageFrame();
                    newFile.AddFrame(newFrame);
                    SupportedFileType frame = fileToEdit.Frames[i];
                    if (frame == null)
                        continue;
                    newFrame.SetFileNames(frame.LoadedFile);
                    Bitmap image = frame.GetBitmap();
                    if (image == null)
                        continue;
                    Int32 stride;
                    Byte[] resBytes = ImageUtils.GetImageData(image, out stride, pf);
                    Bitmap result = ImageUtils.BuildImage(resBytes, image.Width, image.Height, stride, pf, null, null);
                    newFrame.LoadFile(result, frame.LoadedFile);
                }
                return newFile;
            }
        }

        private void TsmiMatchToPaletteClick(Object sender, EventArgs e)
        {
            SupportedFileType fileToEdit = this.m_LoadedFile;
            if (fileToEdit == null || (fileToEdit.FileClass & FileClass.Image | FileClass.FrameSet) == 0)
                return;
            List<PaletteDropDownInfo> allPalettes = new List<PaletteDropDownInfo>();
            allPalettes.AddRange(this.m_DefaultPalettes);
            allPalettes.AddRange(this.m_ReadPalettes);
            Color[] matchPalette;
            Int32 matchBpp;
            using (FrmFramesToPal toPal = new FrmFramesToPal(fileToEdit, allPalettes.ToArray(), false))
            {
                DialogResult dr = toPal.ShowDialog(this);
                this.pzpImage.CustomColors = toPal.CustomColors;
                if (dr != DialogResult.OK)
                    return;
                matchBpp = toPal.MatchBpp;
                matchPalette = toPal.MatchPalette;
            }
            this.ExecuteThreaded(() => this.MatchToPalette(fileToEdit, matchBpp, matchPalette), true, true, true, "Matching to palette");
        }

        private void TsmiRemovePaletteClick(Object sender, EventArgs e)
        {
            SupportedFileType fileToEdit = this.m_LoadedFile;
            if (fileToEdit == null || (fileToEdit.FileClass & (FileClass.Image | FileClass.FrameSet)) == 0)
                return;
            SupportedFileType editedFile = RemovePalette(fileToEdit);
            this.ReloadWithDispose(editedFile, true, false, false);
        }

        private void TsmiSetToDifferenPaletteClick(Object sender, EventArgs e)
        {
            SupportedFileType fileToEdit = this.m_LoadedFile;
            if (fileToEdit == null || (fileToEdit.FileClass & (FileClass.Image | FileClass.FrameSet)) == 0)
                return;
            Int32 bpp = fileToEdit.GetGlobalBpp();
            if (bpp == -1 || bpp > 8)
            {
                MessageBox.Show(this, "This function only supports indexed types.", GetTitle(), MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            List<PaletteDropDownInfo> allPalettes = new List<PaletteDropDownInfo>();
            allPalettes.AddRange(this.m_DefaultPalettes);
            allPalettes.AddRange(this.m_ReadPalettes);
            Color[] matchPalette;
            using (FrmFramesToPal setPal = new FrmFramesToPal(fileToEdit, allPalettes.ToArray(), true))
            {
                DialogResult dr = setPal.ShowDialog(this);
                this.pzpImage.CustomColors = setPal.CustomColors;
                if (dr != DialogResult.OK)
                    return;
                matchPalette = setPal.MatchPalette;
            }
            this.ExecuteThreaded(()=> this.SetToPalette(fileToEdit, matchPalette), true, false, false, "Setting different palette");
        }

        private SupportedFileType RemovePalette(SupportedFileType fileToEdit)
        {
            Int32 bpp = fileToEdit.GetGlobalBpp();
            if (bpp <= 0 || bpp > 8)
                return null;
            if (bpp > 1 && bpp < 4)
                bpp = 4;
            FileFrames newFile = fileToEdit as FileFrames;
            // only use case for not making a new one is FileFrames + FileImageFrame combo, since it can be 100 adjusted.
            Boolean keepFile = newFile != null && newFile.FramesList.All(fr => fr == null || fr is FileImageFrame);
            Boolean hasFrames = fileToEdit.IsFramesContainer && fileToEdit.Frames != null;
            Int32 frames = hasFrames ? fileToEdit.Frames.Length : 1;
            if (hasFrames)
            {
                if (!keepFile)
                {
                    newFile = new FileFrames(fileToEdit);
                    newFile.SetFileNames(fileToEdit.LoadedFile);
                }
                newFile.SetCommonPalette(true);
                newFile.SetBitsPerPixel(bpp);
                newFile.SetNeedsPalette(true);
            }
            for (Int32 i = 0; i < frames; ++i)
            {
                SupportedFileType frame = hasFrames ? fileToEdit.Frames[i] : fileToEdit;
                if (frame == null)
                    continue;
                FileImageFrame newFrame = frame as FileImageFrame;
                Boolean keepFrame = newFrame != null && keepFile;
                if (!keepFrame)
                {
                    newFrame = new FileImageFrame();
                    newFrame.SetFileNames(frame.LoadedFile);
                }
                newFrame.SetNeedsPalette(true);
                if (keepFile)
                    continue;
                Bitmap image = frame.GetBitmap();
                if (image == null)
                {
                    if (!hasFrames)
                        return null;
                    continue;
                }
                newFrame.LoadFile(ImageUtils.CloneImage(frame.GetBitmap()), frame.LoadedFile);
                if (!hasFrames)
                    return newFrame;
                newFile.AddFrame(newFrame);
            }
            return newFile;
        }

        private SupportedFileType MatchToPalette(SupportedFileType fileToEdit, Int32 matchBpp, Color[] matchPalette)
        {
            if (!fileToEdit.IsFramesContainer)
            {
                Bitmap image = fileToEdit.GetBitmap();
                Bitmap[] result = ImageUtils.ImageToFrames(image, image.Width, image.Height, null, null, matchBpp, matchPalette, 0, 0);
                if (result == null || result.Length == 0)
                    return null;
                FileImagePng newFile = new FileImagePng();
                newFile.LoadFile(result[0], fileToEdit.LoadedFile);
                return newFile;
            }
            else
            {
                Int32 frames = fileToEdit.Frames.Length;
                FileFrames newFile = new FileFrames(fileToEdit);
                newFile.SetFileNames(fileToEdit.LoadedFile);
                newFile.SetCommonPalette(true);
                newFile.SetBitsPerPixel(matchBpp);
                newFile.SetPalette(matchPalette);
                for (Int32 i = 0; i < frames; ++i)
                {
                    FileImageFrame newFrame = new FileImageFrame();
                    newFile.AddFrame(newFrame);
                    SupportedFileType frame = fileToEdit.Frames[i];
                    if (frame == null)
                        continue;
                    newFrame.SetFileNames(frame.LoadedFile);
                    Bitmap image = frame.GetBitmap();
                    if (image == null)
                        continue;
                    Bitmap[] result = ImageUtils.ImageToFrames(image, image.Width, image.Height, null, null, matchBpp, matchPalette, 0, 0);
                    if (result == null || result.Length == 0)
                        return null;
                    newFrame.LoadFile(result[0], frame.LoadedFile);
                }
                return newFile;
            }
        }

        private SupportedFileType SetToPalette(SupportedFileType fileToEdit, Color[] newPalette)
        {
            Int32 bpp = fileToEdit.GetGlobalBpp();
            if (bpp <= 0 || bpp > 8)
                return null;
            if (bpp > 1 && bpp < 4)
                bpp = 4;
            FileFrames framesFile = fileToEdit as FileFrames;
            if (framesFile != null)
            {
                framesFile.SetCommonPalette(true);
                framesFile.SetBitsPerPixel(bpp);
                framesFile.SetColors(newPalette);
                return framesFile;
            }
            Boolean hasFrames = fileToEdit.IsFramesContainer && fileToEdit.Frames != null;
            
            Int32 frames = hasFrames ? fileToEdit.Frames.Length : 1;
            FileFrames newFile = null;
            if (hasFrames)
            {
                newFile = new FileFrames(fileToEdit);
                newFile.SetFileNames(fileToEdit.LoadedFile);
                newFile.SetCommonPalette(true);
                newFile.SetBitsPerPixel(bpp);
                newFile.SetPalette(newPalette);
            }
            for (Int32 i = 0; i < frames; ++i)
            {
                FileImageFrame newFrame = new FileImageFrame();

                SupportedFileType frame = hasFrames ? fileToEdit.Frames[i] : fileToEdit;
                if (frame == null)
                    continue;
                newFrame.SetFileNames(frame.LoadedFile);
                Bitmap image = frame.GetBitmap();
                if (image == null)
                {
                    if (!hasFrames)
                        return null;
                    continue;
                }
                newFrame.LoadFile(ImageUtils.CloneImage(frame.GetBitmap()), frame.LoadedFile);
                if (!hasFrames)
                {
                    newFrame.SetColors(newPalette);
                    return newFrame;
                }
                newFile.AddFrame(newFrame);
            }
            newFile.SetColors(newPalette);
            return newFile;
        }

        private void TsmiExtract4BitPalClick(Object sender, EventArgs e)
        {
            SupportedFileType shownFile = this.GetShownFile();
            if (shownFile == null || shownFile.BitsPerPixel != 8 || this.GetColorStatus() == ColorStatus.None)
                return;
            this.SaveFocus(this);
            Option[] so = new Option[1];
            so[0] = new Option("start", OptionInputType.Number, "Start index", "0," + 240, "0");
            SaveOptionInfo soi = new SaveOptionInfo();
            soi.Name = "16-color palette from 256-color palette.\n\nSelect start index of 16-color range. Press Cancel to select manually.";
            soi.Properties = so;
            Int32[] selectedIndices = null;
            try
            {
                using (FrmOptions opts = new FrmOptions(GetTitle(), soi))
                {
                    opts.Height = opts.OptimalHeight;
                    if (opts.ShowDialog(this) == DialogResult.OK)
                    {
                        Int32 startIndex;
                        Int32.TryParse(Option.GetSaveOptionValue(so, "start"), out startIndex);
                        selectedIndices = Enumerable.Range(startIndex, Math.Min(16, 256 - startIndex)).ToArray();
                    }
                }
            }
            catch (ArgumentException ex)
            {
                MessageBox.Show(this, "Error initializing conversion options: " + GeneralUtils.RecoverArgExceptionMessage(ex, true), GetTitle(), MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            Color[] col;
            using (FrmPalette palFrm = new FrmPalette(8, shownFile.GetColors(), false, ColorSelMode.Multi))
            {
                palFrm.SelectedIndices = selectedIndices;
                palFrm.Text = "Select 16 colors";
                if (palFrm.ShowDialog(this) != DialogResult.OK)
                    return;
                col = palFrm.GetSelectedColors();
            }
            Byte[] newImageData = Enumerable.Range(0, 16).Select(b => (Byte) b).ToArray();
            newImageData = ImageUtils.ConvertFrom8Bit(newImageData, 4, 4, 4, true);
            PixelFormat pf = PixelFormat.Format4bppIndexed;
            Bitmap bm = ImageUtils.BuildImage(newImageData, 4, 4, 2, pf, col, Color.Black);
            FileImagePng palImage = new FileImagePng();
            String path = Path.GetDirectoryName(shownFile.LoadedFile);
            String name = Path.GetFileNameWithoutExtension(shownFile.LoadedFile);
            palImage.LoadFile(bm, Path.Combine(path, name + ".png"));
            this.ReloadWithDispose(palImage, true, true, true);
        }

        private void TsmiManagePalettes4BitClick(Object sender, EventArgs e)
        {
            this.ManagePalettes(true);
        }

        private void TsmiManagePalettes8BitClick(Object sender, EventArgs e)
        {
            this.ManagePalettes(false);
        }

        private void ManagePalettes(Boolean fourBit)
        {
            this.SaveFocus(this);
            using (FrmManagePalettes palSave = new FrmManagePalettes(fourBit ? 4 : 8, this.m_PalettePath))
            {
                palSave.Icon = this.Icon;
                palSave.Title = GetTitle();
                palSave.PaletteToSave = null;
                palSave.StartPosition = FormStartPosition.CenterParent;
                if (palSave.ShowDialog(this) != DialogResult.OK)
                    return;
            }
            this.RefreshPalettes(true, true);
            this.RefreshColorControls();
        }

        /// <summary>
        /// Executes a threaded operation while locking the UI. 
        /// </summary>
        /// <param name="function">A func returning SupportedFileType</param>
        /// <param name="resetPalettes">True to reset palettes dropdown when loading the file resulting from the operation</param>
        /// <param name="resetIndex">True to reset frames index when loading the file resulting from the operation</param>
        /// <param name="resetZoom">True to reset auto-zoom when loading the file resulting from the operation</param>
        /// <param name="operationType">String to indicate the process type being executed (eg. "Saving")</param>
        private void ExecuteThreaded(Func<SupportedFileType> function, Boolean resetPalettes, Boolean resetIndex, Boolean resetZoom, String operationType)
        {
            if (this.m_ProcessingThread != null && this.m_ProcessingThread.IsAlive)
                return;
            //Arguments: func returning SupportedFileType, reset palettes, reset index, reset auto-zoom, process type indication string.
            Object[] arrParams = {function, resetPalettes, resetIndex, resetZoom, operationType};
            this.m_ProcessingThread = new Thread(this.ExecuteThreadedActual);
            this.m_ProcessingThread.Start(arrParams);
        }

        /// <summary>
        /// Executes a threaded operation while locking the UI.
        /// "parameters" must be an array of Object containing 4 items:
        /// a func returning SupportedFileType,
        /// boolean 'reset palettes dropdown',
        /// boolean 'reset frames index',
        /// boolean 'reset auto-zoom',
        /// and a string to indicate the process type being executed (eg. "Saving").
        /// </summary>
        /// <param name="parameters">
        ///     Array of Object, containing 5 items: func returning SupportedFileType, boolean 'reset palettes dropdown', boolean 'reset frames index',
        ///     boolean 'reset auto-zoom', string to indicate the process type being executed (eg. "Saving").
        /// </param>
        private void ExecuteThreadedActual(Object parameters)
        {
            Object[] arrParams = parameters as Object[];
            Func<SupportedFileType> func;
            if (arrParams == null || arrParams.Length < 4 || (func = arrParams[0] as Func<SupportedFileType>) == null || !(arrParams[1] is Boolean) || !(arrParams[2] is Boolean) || !(arrParams[3] is Boolean))
            {
                try { this.Invoke(new Action(() => this.EnableControls(true, null))); }
                catch (InvalidOperationException) { /* ignore */ }
                return;
            }
            Boolean resetPalettes = (Boolean)arrParams[1];
            Boolean resetIndex = (Boolean)arrParams[2];
            Boolean resetZoom = (Boolean)arrParams[3];
            String operationType = arrParams[4] as String;
            this.Invoke(new Action(() => this.EnableControls(false, operationType)));
            operationType = String.IsNullOrEmpty(operationType) ? "Operation" : operationType.Trim();
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
            catch (ArgumentException argex)
            {
                String message = operationType + " failed:\n" + GeneralUtils.RecoverArgExceptionMessage(argex, true);
                this.Invoke(new Action(() => this.ShowMessageBox(message, MessageBoxButtons.OK, MessageBoxIcon.Warning)));
                this.Invoke(new Action(() => this.EnableControls(true, null)));
            }
            catch (Exception ex)
            {
                String message = operationType + " failed:\n" + ex.Message + "\n" + ex.StackTrace;
                this.Invoke(new Action(() => this.ShowMessageBox(message, MessageBoxButtons.OK, MessageBoxIcon.Warning)));
                this.Invoke(new Action(() => this.EnableControls(true, null)));
            }
            try
            {
                if (newfile != null)
                    this.Invoke(new Action(() => this.ReloadWithDispose(newfile, resetPalettes, resetIndex, resetZoom)));
                else
                    this.Invoke(new Action(() => this.EnableControls(true, null)));
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
                this.AllowDrop = false;
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
                this.ReloadUi(false, false);
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

        private void ReloadWithDispose(SupportedFileType newFile, Boolean resetPalettes, Boolean resetIndex, Boolean resetZoom)
        {
            SupportedFileType oldFile = this.m_LoadedFile;
            this.m_LoadedFile = newFile;
            if (resetZoom)
                this.AutoSetZoom();
            this.EnableToolstrips(true);
            if (!this.pzpImage.Enabled)
                this.pzpImage.Enabled = true;
            this.ReloadUi(resetPalettes, resetIndex);
            // Don't dispose if the object is the same.
            if (oldFile != null && oldFile != newFile)
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
            this.tsmiSaveRaw.Enabled = enable;
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
                this.tsmiExtractPal.Enabled = false;
                this.tsmiExtract4BitPal.Enabled = false;
                this.tsmiImageToPalette4Bit.Enabled = false;
                this.tsmiImageToPalette8Bit.Enabled = false;
            }
#if DEBUG
            this.tsmiTestBed.Enabled = enable;
#endif
        }

        private DialogResult ShowMessageBox(String message, MessageBoxButtons buttons, MessageBoxIcon icon)
        {
            if (message == null)
                return DialogResult.Cancel;
            this.AllowDrop = false;
            DialogResult result = MessageBox.Show(this, message, GetTitle(), buttons, icon);
            this.AllowDrop = true;
            return result;
        }

        private DialogResult ShowScrollingMessageBox(string title, string titleMessage, string[] message, bool showCancel)
        {
            return ScrollingMessageBox.ShowAsDialog(this, title, titleMessage, message, showCancel);
        }

        private DialogResult ShowScrollingMessageBox(string title, string titleMessage, string message, bool showCancel)
        {
            return ScrollingMessageBox.ShowAsDialog(this, title, titleMessage, message, showCancel);
        }

        private void TsmiTestBedClick(Object sender, EventArgs e)
        {
#if DEBUG
            this.SaveFocus(this);
            this.ExecuteTestCode();
#endif
        }

    }

}
