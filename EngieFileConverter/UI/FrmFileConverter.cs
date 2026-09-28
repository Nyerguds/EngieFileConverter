using EngieFileConverter.Domain;
using EngieFileConverter.Domain.FileTypes;
using EngieFileConverter.Domain.HeightMap;
using Nyerguds.ImageManipulation;
using Nyerguds.Util;
using Nyerguds.Util.UI;
using Nyerguds.Util.UI.SaveOptions;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace EngieFileConverter.UI
{
    public partial class FrmFileConverter : Form, IHasStatusLabel
    {
        private const string PROG_NAME = "Engie File Converter";
        private const string PROG_AUTHOR = "Created by Nyerguds";
        private const int PALETTE_DIM = 226;
        // TODO make configurable?
        private readonly string m_PalettePath = Path.GetDirectoryName(Application.ExecutablePath);

        private string[] m_StartupParamPath;
        private List<PaletteDropDownInfo> m_DefaultPalettes;
        private List<PaletteDropDownInfo> m_ReadPalettes;
        private SupportedFileType m_LoadedFile;
        private string m_LastOpenedFolder;
        private SimpleMultiThreading smt;
        private Label m_BusyStatusLabel;
        private bool m_Loading;
        private Control m_FocusedControl;

        public Label StatusLabel
        {
            get { return m_BusyStatusLabel; }
            set { m_BusyStatusLabel = value; }
        }

        private SupportedFileType GetShownFile()
        {
            if (m_LoadedFile == null)
                return null;
            int shownFrame = GetShownFrame();
            return shownFrame == -1 ? m_LoadedFile : m_LoadedFile.Frames[shownFrame];
        }

        private int GetShownFrame()
        {
            int num = (int)numFrame.Value;
            if (m_LoadedFile == null || m_LoadedFile.Frames == null || num < 0 || num >= m_LoadedFile.Frames.Length)
                return -1;
            return num;
        }

        public FrmFileConverter()
        {
            InitializeComponent();
            Text = GetTitle(true);
            PalettePanel.InitPaletteControl(8, palColorPalette, new Color[256], PALETTE_DIM);
            palColorPalette.Visible = false;
            m_DefaultPalettes = LoadDefaultPalettes();
            m_ReadPalettes = LoadExtraPalettes();
            RefreshPalettes(false, false);
            smt = new SimpleMultiThreading(this, BorderStyle.Fixed3D);
#if DEBUG
            tsmiTestBed.Visible = true;
#endif
        }

        public static string GetTitle()
        {
            return GetTitle(false);
        }

        public static string GetTitle(bool withAuthor)
        {
            string title = PROG_NAME + " " + GeneralUtils.ProgramVersion();
            if (withAuthor)
                title += " - " + PROG_AUTHOR;
            return title;
        }

        public FrmFileConverter(string[] args)
            : this()
        {
            if (args.Length > 0 && File.Exists(args[0]))
            {
                List<string> files = new List<string>();
                files.Add(args[0]);
                for (int i = 1; i < args.Length; ++i)
                {
                    string pth = args[i];
                    if (File.Exists(pth))
                        files.Add(pth);
                }
                m_StartupParamPath = files.ToArray();
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

        private void TsmiCopyClick(object sender, EventArgs e)
        {
            pzpImage.CopyToClipboard();
        }

        private void FrmDragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
                e.Effect = DragDropEffects.Copy;
        }

        private void FrmDragDrop(object sender, DragEventArgs e)
        {
            string[] files = (string[]) e.Data.GetData(DataFormats.FileDrop);
            if (files.Length == 0)
                return;
            List<string> filesList = new List<string>();
            string basePath = null;
            string firstFoundFolder = null;
            int foldersFound = 0;
            for (int i = 0; i < files.Length; ++i)
            {
                string path = files[i];
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
            m_LastOpenedFolder = basePath;
            LoadFile(filesList.ToArray(), null, preferredTypes);
        }

        private void LoadFile(string[] paths, SupportedFileType selectedType, SupportedFileType[] preferredTypes)
        {
            ExecuteThreaded(()=> LoadFileProc(paths, selectedType, preferredTypes), true, true, true, "Loading");
        }

        private SupportedFileType LoadFileProc(string[] paths, SupportedFileType selectedType, SupportedFileType[] preferredTypes)
        {
            if (paths == null || paths.Length == 0)
                return null;
            string path = paths[0];
            if (paths.Length > 1)
                return LoadMultiple(paths, selectedType, preferredTypes);
            SupportedFileType loadedFile = null;
            byte[] fileData = null;
            FileTypeLoadException error = null;
            bool isEmptyFile = false;
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
                            string[] errors = loadErrors.Select(er => er.AttemptedLoadedType + ": " + er.Message).ToArray();
                            string filename = path == null ? String.Empty : (" of \"" + Path.GetFileName(path) + "\"");
                            string title = "File type of " + filename + " could not be identified. Errors returned by all attempts:";
                            Invoke(new Action(() => ShowScrollingMessageBox("Could not load file.", title, errors, false)));
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
            List<string> filesChain = null;
            if (!isEmptyFile && error == null && loadedFile.IsFramesContainer && (filesChain = loadedFile.GetFilesToLoadMissingData(path)) != null && filesChain.Count > 0)
            {
                const string loadQuestion = "The file \"{0}\" seems to be missing a starting point. Would you like to load it from \"{1}\"{2}?";
                const string loadQuestionChain = " (chained through {0})";
                string firstPath = filesChain.First();
                string[] chain = filesChain.Skip(1).Select(pth => "\"" + Path.GetFileName(pth) + "\"").ToArray();
                string chainQuestion = chain.Length == 0 ? String.Empty : String.Format(loadQuestionChain, String.Join(", ", chain));
                string loadQuestionFormat = String.Format(loadQuestion, Path.GetFileName(path), Path.GetFileName(firstPath), chainQuestion);
                DialogResult dr = (DialogResult)Invoke(
                    new Func<DialogResult>(() => ShowMessageBox(loadQuestionFormat, MessageBoxButtons.YesNo, MessageBoxIcon.Question)));
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
                SupportedFileType frames = CheckForFrames(path, detectSource);
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
                string message = "File loading failed: " + error.Message;
                if (error.InnerException != null)
                    message += '\n' + error.InnerException.StackTrace;
                Invoke(new Action(() => ShowMessageBox(message, MessageBoxButtons.OK, MessageBoxIcon.Warning)));
            }
            if (loadedFile == null && isEmptyFile)
                Invoke(new Action(() => ShowMessageBox("File loading failed: The file is empty.", MessageBoxButtons.OK, MessageBoxIcon.Warning)));
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
        private SupportedFileType CheckForFrames(string path, SupportedFileType currentType)
        {
            string minName;
            string maxName;
            bool hasEmptyFrames;
            SupportedFileType fr = FileFrames.CheckForFrames(path, currentType, out minName, out maxName, out hasEmptyFrames);
            if (fr == null)
                return currentType;
            StringBuilder message = new StringBuilder("The file appears to be part of a range (").Append(minName).Append(" - ").Append(maxName).Append(").");
            if (hasEmptyFrames)
                message.Append("\nSome of these frames are empty files. Not every save format supports empty frames.");
            message.Append("\n\nDo you wish to load the frames from all files?");
            DialogResult dr = (DialogResult)Invoke(
                new Func<DialogResult>(() => ShowMessageBox(message.ToString(), MessageBoxButtons.YesNo, MessageBoxIcon.Warning)));
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
        private SupportedFileType LoadMultiple(string[] paths, SupportedFileType selectedType, SupportedFileType[] preferredTypes)
        {
            string[] paths2 = new string[paths.Length];
            Array.Copy(paths, paths2, paths.Length);
            Array.Sort(paths2);
            FileFrames fr = new FileFrames(true);
            SupportedFileType[] loadedFiles = new SupportedFileType[paths.Length];
            for (int i = 0; i < paths2.Length; ++i)
            {
                string path = paths2[i];
                if (File.Exists(path))
                {
                    try
                    {
                        byte[] fileData = File.ReadAllBytes(path);
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
            pzpImage.AutoSetZoom(GetListToAutoSetZoom(m_LoadedFile));
        }

        private static Bitmap[] GetListToAutoSetZoom(SupportedFileType file)
        {
            if (file == null)
                return null;
            List<Bitmap> framesToCheck = new List<Bitmap>();
            framesToCheck.Add(file.GetBitmap());
            SupportedFileType[] frames = file.Frames;
            int nrOfFrames;
            if (frames != null && (nrOfFrames = frames.Length) > 0)
            {
                for (int i = 0; i < nrOfFrames; ++i)
                {
                    Bitmap img;
                    if (frames[i] != null && (img = frames[i].GetBitmap()) != null)
                        framesToCheck.Add(img);
                }
            }
            return framesToCheck.ToArray();
        }

        private void ReloadUi(bool fromNewFile)
        {
            ReloadUi(fromNewFile, fromNewFile);
        }

        private void ReloadUi(bool resetPalettes, bool resetIndex)
        {
            bool hasFrames = m_LoadedFile != null && m_LoadedFile.Frames != null && m_LoadedFile.Frames.Length > 0;
            int bpp = m_LoadedFile == null ? -1 : Math.Abs(m_LoadedFile.BitsPerPixel);
            lblFrame.Enabled = hasFrames;
            numFrame.Enabled = hasFrames;
            numFrame.Minimum = -1;
            int frames = 0;
            if (!hasFrames)
            {
                numFrame.Value = -1;
                numFrame.Maximum = -1;
                lblNrOfFrames.Visible = false;
            }
            else
            {
                if (resetIndex)
                    numFrame.Value = -1;
                frames = m_LoadedFile.Frames.Length;
                int last = frames - 1;
                numFrame.Maximum = last;
                lblNrOfFrames.Visible = true;
                lblNrOfFrames.Text = "/ " + last;
                if (last >= 0 && !m_LoadedFile.IsFramesContainer)
                    numFrame.Minimum = 0;
            }
            SupportedFileType shownFile = GetShownFile();
            bool hasFile = shownFile != null;
            bool hasShownImage = hasFile && shownFile.GetBitmap() != null;
            bool hasPal = GetColorStatus() != ColorStatus.None;
            int shownBpp = shownFile != null ? Math.Abs(shownFile.BitsPerPixel) : -1;
            bool canExportFrames = m_LoadedFile != null && (m_LoadedFile.FileClass & (FileClass.Image | FileClass.FrameSet)) != 0;

            // General
            tsmiSave.Enabled = hasFile;
            tsmiSaveRaw.Enabled = hasShownImage;
            tsmiSaveSingleFrame.Enabled = canExportFrames && numFrame.Value >= 0;
            tsmiSaveFrames.Enabled = canExportFrames;
            tsmiFramesToSingleImage.Enabled = canExportFrames;
            tsmiCopy.Enabled = hasShownImage;

            // General frame tools
            tsmiImageToFrames.Enabled = hasShownImage;
            tsmiFramesToSingleImage.Enabled = canExportFrames && frames > 0;
            // General animations "paste on frames" option.
            tsmiPasteOnFrames.Enabled = (hasFrames && frames > 0) || (!hasFrames && hasShownImage);

            // Extract colors
            tsmiExtractPal.Enabled = hasPal;
            tsmiExtract4BitPal.Enabled = hasPal && shownBpp == 8;
            tsmiImageToPalette4Bit.Enabled = hasShownImage;
            tsmiImageToPalette8Bit.Enabled = hasShownImage;
            tsmiMatchToPalette.Enabled = hasFile && (m_LoadedFile.FileClass & (FileClass.Image | FileClass.FrameSet)) != 0;
            int globalBpp = !hasFile ? -1 : m_LoadedFile.GetGlobalBpp();
            tsmiRemovePalette.Enabled = globalBpp != -1 && globalBpp <= 8;
            tsmiSetToDifferenPalette.Enabled = globalBpp != -1 && globalBpp <= 8;
            tsmiChangeTo24BitRgb.Enabled = hasFile && (m_LoadedFile.FileClass & (FileClass.Image | FileClass.FrameSet)) != 0 && m_LoadedFile.BitsPerPixel != 24;
            tsmiChangeTo32BitArgb.Enabled = hasFile && (m_LoadedFile.FileClass & (FileClass.Image | FileClass.FrameSet)) != 0 && m_LoadedFile.BitsPerPixel != 32;

            // C&C64 toolsets
            tsmiToHeightMap.Enabled = shownFile is FileMapWwCc1Pc;
            tsmiToPlateaus.Enabled = shownFile is FileMapWwCc1Pc;
            tsmiToHeightMapAdv.Enabled = shownFile is FileMapWwCc1Pc;
            tsmiTo65x65HeightMap.Enabled = hasShownImage && shownFile.Width == 64 && shownFile.Height == 64 && shownFile.FileClass != FileClass.CcMap;
            // Tiberian Sun shadow tools
            tsmiCombineShadows.Enabled = hasFrames && bpp == 8 && frames > 0 && frames % 2 == 0;
            tsmiSplitShadows.Enabled = hasFrames && bpp == 8 && frames > 0;

            if (!hasFile)
            {
                string emptystr = "---";
                lblValFilename.Text = emptystr;
                lblValType.Text = emptystr;
                toolTip1.SetToolTip(lblValType, null);
                lblValSize.Text = emptystr;
                lblValColorFormat.Text = emptystr;
                lblValColorsInPal.Text = emptystr;
                lblValInfo.Text = String.Empty;
                cmbPalettes.Enabled = false;
                cmbPalettes.SelectedIndex = 0;
                btnResetPalette.Enabled = false;
                btnSavePalette.Enabled = false;
                pzpImage.Image = null;
                PalettePanel.InitPaletteControl(8, palColorPalette, new Color[256], PALETTE_DIM);
                palColorPalette.Visible = false;
            }
            else
            {
                lblValFilename.Text = GeneralUtils.DoubleAmpersands(shownFile.LoadedFileName);
                lblValType.Text = GeneralUtils.DoubleAmpersands(shownFile.LongTypeName);
                toolTip1.SetToolTip(lblValType, lblValType.Text);
                lblValSize.Text = String.Format("{0}×{1}", shownFile.Width, shownFile.Height);
                lblValColorFormat.Text = shownBpp < 0 ? String.Empty : (shownBpp == 0 ? "N/A" : (shownBpp + " BPP" + (shownBpp < 8 ? " (paletted)" : String.Empty)));
                Color[] palette = shownFile.GetColors();
                int actualColors = palette == null ? 0 : palette.Length;
                bool needsPalette = shownFile.NeedsPalette;
                lblValColorsInPal.Text = actualColors + (needsPalette ? " (0 in file)" : String.Empty);
                lblValInfo.Text = GeneralUtils.DoubleAmpersands(shownFile.ExtraInfo);
                cmbPalettes.Enabled = needsPalette;
                Bitmap image = shownFile.GetBitmap();
                pzpImage.Image = image;
                RefreshPalettes(resetPalettes, resetPalettes);
                if (needsPalette) // && resetPalettes)
                    CmbPalettesSelectedIndexChanged(null, null);
                else
                    RefreshColorControls();
            }
            SimpleMultiThreading.RemoveBusyLabel(this);
            LoadFocus();
            AllowDrop = true;
        }

        private ColorStatus GetColorStatus()
        {
            SupportedFileType loadedFile = GetShownFile();
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

        public List<PaletteDropDownInfo> GetPalettes(int bpp, bool reloadFiles, bool[] typeTransModifier)
        {
            List<PaletteDropDownInfo> allPalettes = m_DefaultPalettes.Where(p => p.BitsPerPixel == bpp).ToList();
            if (reloadFiles)
                m_ReadPalettes = LoadExtraPalettes();
            allPalettes.AddRange(m_ReadPalettes.Where(p => p.BitsPerPixel == bpp));
            foreach (PaletteDropDownInfo info in allPalettes)
                info.Colors = PaletteUtils.ApplyPalTransparencyMask(info.Colors, typeTransModifier);
            return allPalettes;
        }

        public List<PaletteDropDownInfo> LoadExtraPalettes()
        {
            List<PaletteDropDownInfo> palettes = new List<PaletteDropDownInfo>();
            FileInfo[] files = new DirectoryInfo(m_PalettePath).GetFiles("*.pal").OrderBy(x => x.Name).ToArray();
            int filesLength = files.Length;
            for (int i = 0; i < filesLength; ++i)
                palettes.AddRange(PaletteDropDownInfo.LoadSubPalettesInfoFromPalette(files[i], false, false, true));
            return palettes;
        }

        private void FrmFileConverterShown(object sender, EventArgs e)
        {
            if (m_StartupParamPath != null)
                LoadFile(m_StartupParamPath, null, null);
            else
                ReloadUi(true);
        }

        private void TsmiSaveClick(object sender, EventArgs e)
        {
            Save(false, false);
        }

        private void tsmiSaveSingleFrameClick(object sender, EventArgs e)
        {
            Save(false, true);
        }

        private void TsmiSaveFramesClick(object sender, EventArgs e)
        {
            Save(true, false);
        }

        private void TsmiSaveRawClick(object sender, EventArgs e)
        {
            SaveFocus(this);
            Bitmap image;
            SupportedFileType shown = GetShownFile();
            if (shown == null || (image = shown.GetBitmap()) == null)
                return;
            string imagePath = shown.LoadedFile;
            string filename;
            using (SaveFileDialog sfd = new SaveFileDialog())
            {
                sfd.Filter = "All files (*.*)|*.*";
                sfd.InitialDirectory = Path.GetDirectoryName(imagePath);
                sfd.FileName = Path.GetFileNameWithoutExtension(imagePath) + ".dat";
                AllowDrop = false;
                DialogResult res = sfd.ShowDialog(this);
                if (res != DialogResult.OK)
                {
                    AllowDrop = true;
                    return;
                }
                filename = sfd.FileName;
            }
            ExecuteThreaded(() => SaveRaw(image, filename), false, false, false, "Saving");
        }

        private SupportedFileType SaveRaw(Bitmap image, string fileName)
        {
            int stride;
            byte[] rawData = ImageUtils.GetImageData(image, out stride, image.PixelFormat, true);
            File.WriteAllBytes(fileName, rawData);
            return null;
        }

        private void Save(bool frames, bool saveSingle)
        {
            SaveFocus(this);
            if (m_LoadedFile == null)
                return;
            SupportedFileType selectedItem;
            bool hasFrames = m_LoadedFile.Frames != null && m_LoadedFile.Frames.Length > 0;
            bool saveSingleFrame = !frames && saveSingle && hasFrames && numFrame.Value != -1;
            SupportedFileType loadedFile = saveSingleFrame ? m_LoadedFile.Frames[(int) numFrame.Value] : m_LoadedFile;
            bool hasEmptyFrames = frames && hasFrames && loadedFile.Frames.Any(f => f == null || f.GetBitmap() == null);
            Type selectType = frames ? typeof (FileImagePng) : loadedFile.GetType();
            Type[] saveTypes = FileTypesFactory.SupportedSaveTypes;
            int nrOfSaveTypes = saveTypes.Length;
            FileClass loadedFileType = loadedFile.FileClass;
            FileClass frameFileType = FileClass.None;
            if (hasFrames && !saveSingleFrame)
            {
                SupportedFileType first = m_LoadedFile.Frames.FirstOrDefault(x => x != null && x.GetBitmap() != null);
                if (first != null)
                    frameFileType = first.FileClass;
            }
            List<Type> filteredTypes = new List<Type>();
            for (int i = 0; i < nrOfSaveTypes; ++i)
            {
                Type saveType = saveTypes[i];
                SupportedFileType tmpsft = (SupportedFileType) Activator.CreateInstance(saveType);
                FileClass diff = tmpsft.InputFileClass & (frames ? frameFileType : loadedFileType);
                if ((diff & ~FileClass.FrameSet) != 0 || (!frames && (tmpsft.FrameInputFileClass & frameFileType) != 0))
                    filteredTypes.Add(saveType);
            }
            if (filteredTypes.Count == 0)
            {
                string message = "No types found for saving this data.";
                if (hasFrames && !saveSingleFrame)
                    message += "\nTry exporting as frames instead.";
                ShowMessageBox(message, MessageBoxButtons.OK, MessageBoxIcon.Warning);
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
            string title = "Save " + (frames? "as Frames" : "As");
            string filename = FileDialogGenerator.ShowSaveFileFialog(this, title, selectType, filteredTypes.ToArray(), typeof(FileImagePng), false, true, loadedFile.LoadedFile, out selectedItem);
            if (filename == null || selectedItem == null)
                return;
            List<Option> saveOptions = new List<Option>();
            Option[] saveOptionsChosen = null;
            try
            {
                // For export to frames only: collect the options for all frames.
                HashSet<string> saveOptsUnique = new HashSet<string>();
                if (frames && hasFrames)
                {
                    SupportedFileType[] internalFrames = loadedFile.Frames;
                    int nrOfFrames = internalFrames.Length;
                    for (int i = 0; i < nrOfFrames; ++i)
                    {
                        Option[] optsInt = selectedItem.GetSaveOptions(internalFrames[i], filename);
                        for (int j = 0; j < optsInt.Length; ++j)
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
                        for (int j = 0; j < optsFile.Length; ++j)
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
                    bool fromfileRangeToMultiple = framesFile != null && framesFile.FromFileRange;
                    string filenameEx = Path.GetFileNameWithoutExtension(filename) + "-00000" + Path.GetExtension(filename);
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
                string message = "Cannot save " + (frames ? "frame of " : String.Empty) + "type " + loadedFile.ShortTypeName
                                 + " as type " + selectedItem.ShortTypeName + (String.IsNullOrEmpty(ex.Message) ? "." : ":\n" + ex.Message);
                ShowMessageBox(message, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            catch (ArgumentException ex)
            {
                string msg = GeneralUtils.RecoverArgExceptionMessage(ex, false);
                string message = "Cannot save " + (frames ? "frame of " : String.Empty) + "type " + loadedFile.ShortTypeName
                                 + " as type " + selectedItem.ShortTypeName + (String.IsNullOrEmpty(msg) ? "." : ":\n" + msg);
                ShowMessageBox(message, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            catch (NotImplementedException)
            {
                string message = "Sorry, saving is not available for type " + selectedItem.ShortTypeName + ".";
                ShowMessageBox(message, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            ExecuteThreaded(
                () => SaveFile(frames, loadedFile, selectedItem, filename, saveOptionsChosen, m_LoadedFile),
                false, false, false, "Saving");
        }

        private SupportedFileType SaveFile(bool frames, SupportedFileType loadedFile, SupportedFileType selectedItem, string filename, Option[] saveOptions,
            SupportedFileType reloadFile)
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
                    string path = Path.GetDirectoryName(filename);
                    string fileName = Path.GetFileNameWithoutExtension(filename) + "-";
                    string extension = Path.GetExtension(filename);
                    bool newNames = saveOptions != null && GeneralUtils.IsTrueValue(Option.GetSaveOptionValue(saveOptions, "FRAMES_NEWNAMES"));
                    bool nullFrames = saveOptions != null && GeneralUtils.IsTrueValue(Option.GetSaveOptionValue(saveOptions, "FRAMES_NULLFRAMES"));
                    if (saveOptions == null)
                        saveOptions = new Option[0];
                    for (int i = 0; i < loadedFile.Frames.Length; ++i)
                    {
                        SupportedFileType frame = loadedFile.Frames[i];
                        string framePath = Path.Combine(path, (newNames ? (fileName + i.ToString("D5")) : Path.GetFileNameWithoutExtension(frame.LoadedFileName)) + extension);
                        if (frame.GetBitmap() != null)
                            selectedItem.SaveAsThis(frame, framePath, saveOptions);
                        else if (nullFrames) // Allow empty frames as empty files.
                            File.WriteAllBytes(framePath, new byte[0]);
                    }
                }
            }
            catch (FileTypeSaveException ex)
            {
                string message = "Error saving " + (frames ? "frame of " : String.Empty) + "type " + loadedFile.ShortTypeName
                                 + " as type " + selectedItem.ShortTypeName + (String.IsNullOrEmpty(ex.Message) ? "." : ":\n" + ex.Message);
#if DEBUG
                message += "\n" + ex.StackTrace;
#endif
                Invoke(new Action(() => ShowMessageBox(message, MessageBoxButtons.OK, MessageBoxIcon.Warning)));
            }
            catch (ArgumentException ex)
            {
                string msg = GeneralUtils.RecoverArgExceptionMessage(ex, false);
                string message = "Error saving " + (frames ? "frame of " : String.Empty) + "type " + loadedFile.ShortTypeName
                                 + " as type " + selectedItem.ShortTypeName + (String.IsNullOrEmpty(msg) ? "." : ":\n" + msg);
#if DEBUG
                message += "\n" + ex.StackTrace;
#endif
                Invoke(new Action(() => ShowMessageBox(message, MessageBoxButtons.OK, MessageBoxIcon.Warning)));
            }
            catch (NotImplementedException)
            {
                string message = "Sorry, saving is not available for type " + selectedItem.ShortTypeName + ".";
                Invoke(new Action(() => ShowMessageBox(message, MessageBoxButtons.OK, MessageBoxIcon.Warning)));
            }
            catch (NotSupportedException)
            {
                string message = "Sorry, saving is not available for type " + selectedItem.ShortTypeName + ".";
                Invoke(new Action(() => ShowMessageBox(message, MessageBoxButtons.OK, MessageBoxIcon.Warning)));
            }
            // Reload currently loaded file, not whatever was passed to this function.
            return reloadFile;
        }

        private void TsmiExitClick(object sender, EventArgs e)
        {
            Close();
        }

        private void SaveFocus(Control ctrl)
        {
            if (m_Loading || m_BusyStatusLabel != null || !ctrl.ContainsFocus)
                return;
            m_FocusedControl = ctrl;
            foreach (Control control in ctrl.Controls)
            {
                if (!control.ContainsFocus)
                    continue;
                SaveFocus(control);
                break;
            }
        }

        private void LoadFocus()
        {
            if (m_FocusedControl != null && m_FocusedControl.Enabled && !m_FocusedControl.ContainsFocus)
                m_FocusedControl.Focus();
            m_FocusedControl = null;
        }

        protected void FrmFileConverterFormClosing(object sender, FormClosingEventArgs e)
        {
            if (!m_Loading || smt == null || !smt.IsExecuting)
                return;
            DialogResult result = ShowMessageBox("Operations are in progress! Are you sure you want to quit?", MessageBoxButtons.YesNo, MessageBoxIcon.Exclamation);
            if (result == DialogResult.Yes)
            {
                smt.AbortThreadedOperation(5000);
            }
            else
            {
                // abort the closing of the form.
                e.Cancel = true;
            }
        }

        private void TsmiOpenClick(object sender, EventArgs e)
        {
            SaveFocus(this);
            SupportedFileType selectedItem;
            string[] filenames = FileDialogGenerator.ShowOpenFileFialog(this, null, false, FileTypesFactory.SupportedOpenTypes, FileTypesFactory.AutoDetectTypes, m_LastOpenedFolder, "images", null, true, out selectedItem);
            if (filenames == null ||filenames.Length == 0)
                return;
            string filename = filenames[0];
            m_LastOpenedFolder = Path.GetDirectoryName(filename);
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
            LoadFile(filenames, selectedItem, preferredTypes);
        }

        private bool[] GetCurrentTypeTransparencyMask()
        {
            return m_LoadedFile == null ? null : m_LoadedFile.TransparencyMask;
        }

        private void RefreshColorControls()
        {
            SupportedFileType loadedFile = GetShownFile();
            bool fileLoaded = loadedFile != null;
            ColorStatus cs = GetColorStatus();
            // 1-bit and 2-bit palettes can not currently be saved.
            btnSavePalette.Enabled = fileLoaded && cs != ColorStatus.None && Math.Abs(loadedFile.BitsPerPixel) >= 4;
            cmbPalettes.Enabled = cs == ColorStatus.External;
            // Ignore this if the palette is handled by the dropdown
            bool resetEnabled;
            Color[] pal;
            switch (cs)
            {
                case ColorStatus.Internal:
                    resetEnabled = loadedFile.ColorsChanged();
                    pal = loadedFile.GetColors();
                    break;
                case ColorStatus.External:
                    PaletteDropDownInfo currentPal = cmbPalettes.SelectedItem as PaletteDropDownInfo;
                    resetEnabled = currentPal != null && currentPal.IsChanged(GetCurrentTypeTransparencyMask());
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
            btnResetPalette.Enabled = resetEnabled;
            int bpp;
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
            bool showPal = bpp > 0 && bpp <= 8;
            palColorPalette.Visible = showPal;
            if (showPal)
                PalettePanel.InitPaletteControl(bpp, palColorPalette, pal, PALETTE_DIM);
            LoadFocus();
        }

        private void NumFrameValueChanged(object sender, EventArgs e)
        {
            if (m_LoadedFile != null && m_LoadedFile.Frames != null && m_LoadedFile.Frames.Length > 0)
            {
                SaveFocus(this);
                ReloadUi(false);
            }
        }

        private void CmbPalettesSelectedIndexChanged(object sender, EventArgs e)
        {
            SaveFocus(this);
            if (GetColorStatus() != ColorStatus.External)
                return;
            PaletteDropDownInfo currentPal = cmbPalettes.SelectedItem as PaletteDropDownInfo;
            Color[] targetPal;
            SupportedFileType loadedFile = GetShownFile();
            if (currentPal == null)
            {
                if (!btnSavePalette.Enabled)
                    btnSavePalette.Enabled = true;
                targetPal = PaletteUtils.GenerateGrayPalette(8, null, false);
            }
            else
            {
                targetPal = currentPal.Colors;
                int bpp = currentPal.BitsPerPixel;
                if (btnSavePalette.Enabled && bpp == 1)
                    btnSavePalette.Enabled = false;
                else if (!btnSavePalette.Enabled && bpp != 1)
                    btnSavePalette.Enabled = true;
                btnResetPalette.Enabled = currentPal.IsChanged(GetCurrentTypeTransparencyMask());
            }
            if (loadedFile == null)
                pzpImage.Image = null;
            else
            {
                loadedFile.SetColors(targetPal);
                pzpImage.Image = loadedFile.GetBitmap();
            }
            pzpImage.RefreshImage();
            RefreshColorControls();
        }

        private void BtnResetPaletteClick(object sender, EventArgs e)
        {
            SaveFocus(this);
            ColorStatus cs = GetColorStatus();
            if (cs == ColorStatus.None)
                return;
            switch (cs)
            {
                case ColorStatus.Internal:
                    GetShownFile().ResetColors();
                    break;
                case ColorStatus.External:
                    PaletteDropDownInfo currentPal = cmbPalettes.SelectedItem as PaletteDropDownInfo;
                    if (currentPal == null)
                        return;
                    if (currentPal.SourceFile != null && currentPal.Entry >= 0)
                    {
                        string message = "This will remove all changes you have made to the palette since it was loaded!\n\nAre you sure you want to continue?";
                        DialogResult dr = ShowMessageBox(message, MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                        if (dr != DialogResult.Yes)
                            return;
                    }
                    Color zeroCol = (currentPal.Colors != null && currentPal.Colors.Length > 1) ? currentPal.Colors[0] : Color.Black;
                    currentPal.Revert(GetCurrentTypeTransparencyMask());
                    Color[] colors = currentPal.Colors;
                    GetShownFile().SetColors(colors);

                    // If CGA color 0 changed: change all CGA palettes.
                    if (GetShownFile().BitsPerPixel == -2 && colors.Length > 0 && zeroCol.ToArgb() != colors[0].ToArgb())
                    {
                        PaletteDropDownInfo[] itemsToChange = GetPalettes(2, false, GetCurrentTypeTransparencyMask()).Where(p => p.Name.StartsWith("CGA ")).ToArray();
                        for (int i = 0; i < itemsToChange.Length; ++i)
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
            SupportedFileType shownFile = GetShownFile();
            pzpImage.Image = shownFile.GetBitmap();
            pzpImage.RefreshImage();
            RefreshColorControls();
        }

        private void BtnSavePaletteClick(object sender, EventArgs e)
        {
            SaveFocus(this);
            ColorStatus cs = GetColorStatus();
            if (cs == ColorStatus.None)
                return;
            SupportedFileType loadedFile = GetShownFile();
            int bpp = Math.Abs(loadedFile.BitsPerPixel);
            if (bpp == 1)
                return;
            PaletteDropDownInfo currentPal;
            switch (cs)
            {
                case ColorStatus.External:
                    currentPal = cmbPalettes.SelectedItem as PaletteDropDownInfo;
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
            using (FrmManagePalettes palSave = new FrmManagePalettes(currentPal.BitsPerPixel, m_PalettePath))
            {
                palSave.Icon = Icon;
                palSave.Title = GetTitle();
                palSave.PaletteToSave = currentPal;
                palSave.SuggestedSaveName = m_LoadedFile.LoadedFile ?? m_LoadedFile.LoadedFileName;
                palSave.StartPosition = FormStartPosition.CenterParent;
                DialogResult dr = palSave.ShowDialog(this);
                if (dr != DialogResult.OK || cs == ColorStatus.Internal)
                {
                    RefreshPalettes(true, true);
                    RefreshColorControls();
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
                RefreshPalettes(true, true);
                string source = palInfo.SourceFile;
                int index = palInfo.Entry;
                foreach (PaletteDropDownInfo pdd in cmbPalettes.Items)
                {
                    if (pdd.SourceFile != source || pdd.Entry != index)
                        continue;
                    cmbPalettes.SelectedItem = pdd;
                    break;
                }
            }
            LoadFocus();
        }

        private void RefreshPalettes(bool forced, bool reloadFiles)
        {
            int oldBpp = -1;
            PaletteDropDownInfo currentPal = cmbPalettes.SelectedItem as PaletteDropDownInfo;
            if (currentPal != null)
                oldBpp = currentPal.BitsPerPixel;
            SupportedFileType shown = GetShownFile();
            if (GetColorStatus() == ColorStatus.Internal)
            {
                // Shows text on the disabled control.
                cmbPalettes.DataSource = null;
                cmbPalettes.Items.Clear();
                cmbPalettes.Items.Add(GetColorStatus() == ColorStatus.Internal ? "Inbuilt palette" : "None");
                cmbPalettes.SelectedIndex = 0;
                return;
            }
            int bpp = shown == null ? 0 : Math.Abs(shown.BitsPerPixel);
            // Don't reload if it was the same :)
            if (oldBpp != -1 && oldBpp == bpp && !forced)
                return;
            int index = -1;
            List<PaletteDropDownInfo> bppPalettes = GetPalettes(bpp, reloadFiles, GetCurrentTypeTransparencyMask());
            if (forced && oldBpp != -1 && oldBpp == bpp && currentPal != null)
                index = bppPalettes.FindIndex(x => x.Name == currentPal.Name);
            if (bppPalettes.Count == 0)
                bppPalettes.Add(new PaletteDropDownInfo("None", -1, PaletteUtils.GenerateGrayPalette(8, null, false), null, -1, false, false));
            cmbPalettes.DataSource = bppPalettes;
            if (index >= 0)
                cmbPalettes.SelectedIndex = index;
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            // override of menu shortcuts to allow copying and pasting text in the preview text field and numeric up/down controls.
            bool isCtrlC = keyData == (Keys.Control | Keys.C);
            bool isCtrlV = keyData == (Keys.Control | Keys.V);
            bool isCtrlX = keyData == (Keys.Control | Keys.X);
            bool isCtrlA = keyData == (Keys.Control | Keys.A);
            bool isCtrlZ = keyData == (Keys.Control | Keys.Z);
            if (!isCtrlC && !isCtrlV && !isCtrlX && !isCtrlA && !isCtrlZ)
                return base.ProcessCmdKey(ref msg, keyData);
            TextBox tb = ActiveControl as TextBox;
            EnhNumericUpDown num = ActiveControl as EnhNumericUpDown;
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

        private void PalColorViewerColorLabelMouseDoubleClick(object sender, PaletteClickEventArgs e)
        {
            if (e.Button != MouseButtons.Left)
                return;
            PalettePanel palPanel = sender as PalettePanel;
            if (palPanel == null)
                return;
            EditColor(palPanel, e.Index, e.Color);
        }

        private void SetPaletteColor(PalettePanel palpanel, int colindex, Color color, SupportedFileType loadedFile)
        {
            ColorStatus cs = GetColorStatus();
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
                        itemsToChange = GetPalettes(2, false, GetCurrentTypeTransparencyMask()).ToArray();
                    }
                    else
                    {
                        itemsToChange = new PaletteDropDownInfo[] { cmbPalettes.SelectedItem as PaletteDropDownInfo };
                    }
                    for (int i = 0; i < itemsToChange.Length; ++i)
                    {
                        PaletteDropDownInfo currentPal = itemsToChange[i];
                        if (currentPal != null && currentPal.Colors.Length > colindex)
                            currentPal.Colors[colindex] = color;
                    }
                }
                SupportedFileType shownFile = GetShownFile();
                pzpImage.Image = shownFile.GetBitmap();
            }
            pzpImage.RefreshImage();
            RefreshColorControls();
        }


        private void PalColorViewerColorLabelMouseClick(object sender, PaletteClickEventArgs e)
        {
            if (e.Button != MouseButtons.Right)
                return;
            ContextMenu cm = new ContextMenu();
            if (palColorPalette.Palette.Length <= e.Index)
                return;
            SaveFocus(this);
            MenuItem miEd = new MenuItem("Edit...", EditColor);
            miEd.Tag = e.Index;
            cm.MenuItems.Add(miEd);
            MenuItem miTr = new MenuItem("Set transparent", SetColorTransparent);
            miTr.Tag = e.Index;
            cm.MenuItems.Add(miTr);
            MenuItem miOp = new MenuItem("Set opaque", SetColorOpaque);
            miOp.Tag = e.Index;
            cm.MenuItems.Add(miOp);
            MenuItem miAl = new MenuItem("Set alpha...", SetColorAlpha);
            miAl.Tag = e.Index;
            cm.MenuItems.Add(miAl);
            cm.Show((Control)sender, e.Location);
        }

        private void EditColor(object sender, EventArgs e)
        {
            MenuItem cm = sender as MenuItem;
            if (cm == null)
                return;
            if (!(cm.Tag is int))
                return;
            int colIndex = (int)cm.Tag;
            Color color = palColorPalette.Palette[colIndex];
            EditColor(palColorPalette, colIndex, color);
        }

        private void EditColor(PalettePanel palPanel, int colindex, Color color)
        {
            SupportedFileType shownFile = GetShownFile();
            if (shownFile == null)
                return;
            SaveFocus(this);
            Color newCol;
            if (shownFile.BitsPerPixel == -2)
            {
                // CGA Mode.
                using (FrmPalette palFrm = new FrmPalette(4, PaletteUtils.GetEgaPalette(), true, ColorSelMode.Single))
                {
                    int selectedCol = PaletteUtils.FindEgaColor(color);
                    palFrm.SelectedIndices = selectedCol == 0 ? null : new int[] {selectedCol};
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
                    cdl.CustomColors = pzpImage.CustomColors;
                    AllowDrop = false;
                    DialogResult res = cdl.ShowDialog(this);
                    pzpImage.CustomColors = cdl.CustomColors;
                    if (res != DialogResult.OK)
                    {
                        AllowDrop = true;
                        return;
                    }
                    newCol = cdl.Color;
                }
            }
            newCol = Color.FromArgb(color.A, newCol);
            SetPaletteColor(palPanel, colindex, newCol, shownFile);
            AllowDrop = true;
            LoadFocus();
        }

        private void SetColorTransparent(object sender, EventArgs e)
        {
            SetPalColorAlpha(sender, 0);
        }

        private void SetColorOpaque(object sender, EventArgs e)
        {
            SetPalColorAlpha(sender, 255);
        }

        private void SetColorAlpha(object sender, EventArgs e)
        {
            MenuItem cm = sender as MenuItem;
            if (cm == null)
                return;
            if (!(cm.Tag is int))
                return;
            int index = (int)cm.Tag;
            if (palColorPalette.Palette.Length <= index)
                return;
            Color col = palColorPalette.Palette[index];
            using (FrmSetAlpha alphaForm = new FrmSetAlpha(col.A))
            {
                AllowDrop = false;
                if (alphaForm.ShowDialog(this) != DialogResult.OK)
                {
                    AllowDrop = true;
                    return;
                }
                col = Color.FromArgb(alphaForm.Alpha, col);
            }
            SupportedFileType loadedFile = GetShownFile();
            SetPaletteColor(palColorPalette, index, col, loadedFile);
            AllowDrop = true;

        }

        private void SetPalColorAlpha(object sender, int alpha)
        {
            MenuItem cm = sender as MenuItem;
            if (cm == null)
                return;
            if (!(cm.Tag is int))
                return;
            int index = (int)cm.Tag;
            if (palColorPalette.Palette.Length <= index)
                return;
            Color col = palColorPalette.Palette[index];
            col = Color.FromArgb(alpha, col);
            SupportedFileType loadedFile = GetShownFile();
            SetPaletteColor(palColorPalette, index, col, loadedFile);
        }

        private enum ColorStatus
        {
            None,
            Internal,
            External
        }

        private void TsmiImageToFramesClick(object sender, EventArgs e)
        {
            SupportedFileType shownFile = GetShownFile();
            if (shownFile == null)
                return;
            SaveFocus(this);
            Bitmap image = shownFile.GetBitmap();
            List<PaletteDropDownInfo> allPalettes = new List<PaletteDropDownInfo>();
            allPalettes.AddRange(m_DefaultPalettes);
            allPalettes.AddRange(m_ReadPalettes);
            string imagePath = shownFile.LoadedFile;
            if (String.IsNullOrEmpty(imagePath))
                imagePath = shownFile.LoadedFileName;
            int frameWidth;
            int frameHeight;
            int maxFrames;
            Color? trimColor;
            int? trimIndex;
            int matchBpp;
            Color[] matchPalette;
            using (FrmFramesCutter frameCutter = new FrmFramesCutter(image, pzpImage.CustomColors, allPalettes.ToArray()))
            {
                frameCutter.CustomColors = pzpImage.CustomColors;
                AllowDrop = false;
                DialogResult dr = frameCutter.ShowDialog(this);
                pzpImage.CustomColors = frameCutter.CustomColors;
                if (dr != DialogResult.OK)
                {
                    AllowDrop = true;
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
            ExecuteThreaded(() => FileFrames.CutImageIntoFrames(image, imagePath, frameWidth, frameHeight, maxFrames, trimColor, trimIndex, matchBpp, matchPalette, false, shownFile.NeedsPalette),
                false, true, true, "Splitting into frames");
        }

        private void TsmiFramesToSingleImageClick(object sender, EventArgs e)
        {
            if (m_LoadedFile == null)
                return;
            SupportedFileType[] frames = m_LoadedFile.Frames;
            int nrOfframes;
            if (frames == null || (nrOfframes = frames.Length) == 0)
                return;
            SaveFocus(this);
            Bitmap[] frameImages = new Bitmap[nrOfframes];
            PixelFormat highestPf = PixelFormat.Undefined;
            int highestBpp = 0;
            Color[] palette = null;
            int maxWidth = 0;
            int maxHeight = 0;
            for (int i = 0; i < nrOfframes; ++i)
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
                int curBpp = Image.GetPixelFormatSize(curPf);
                if (curBpp <= highestBpp)
                    continue;
                highestPf = curPf;
                highestBpp = curBpp;
                if (highestBpp <= 8)
                    palette = img.Palette.Entries;
            }
            if (highestBpp == 0)
                return;
            bool hasAlpha = true;
            bool hasSimpleTrans = false;
            string paletteStr = null;
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
            so[3] = new Option("FPL", OptionInputType.Number, "Frames per line", "1," + nrOfframes, ((int)Math.Sqrt(nrOfframes)).ToString());
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
                string message = "Error initializing conversion options: " + GeneralUtils.RecoverArgExceptionMessage(ex, true);
                ShowMessageBox(message, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            int frameWidth;
            Int32.TryParse(Option.GetSaveOptionValue(so, "FRW"), out frameWidth);
            int frameHeight;
            Int32.TryParse(Option.GetSaveOptionValue(so, "FRH"), out frameHeight);
            int framesPerLine;
            Int32.TryParse(Option.GetSaveOptionValue(so, "FPL"), out framesPerLine);
            bool centerFrames = GeneralUtils.IsTrueValue(Option.GetSaveOptionValue(so, "FRC"));
            byte fillPalIndex = 0;
            Color fillColor = Color.Empty;
            if (highestBpp <= 8)
                byte.TryParse(Option.GetSaveOptionValue(so, "BGI"), out fillPalIndex);
            else
                fillColor = ColorUtils.ColorFromHexString(Option.GetSaveOptionValue(so, "BGC"));
            ExecuteThreaded(() => FramesToSingleImage(frameImages, frameWidth, frameHeight, centerFrames, framesPerLine, fillPalIndex, fillColor), false, true, true, "Combining frames");
        }

        private SupportedFileType FramesToSingleImage(Bitmap[] images, int framesWidth, int framesHeight, bool centerFrames, int framesPerLine, byte backFillPalIndex, Color backFillColor)
        {
            Bitmap bm = ImageUtils.BuildImageFromFrames(images, framesWidth, framesHeight, centerFrames, framesPerLine, backFillPalIndex, backFillColor);
            FileImagePng returnImg = new FileImagePng();
            returnImg.LoadFile(bm, m_LoadedFile.LoadedFile);
            return returnImg;
        }

        private void TsmiToHeightMapAdvClick(object sender, EventArgs e)
        {
            GenerateHeightMap(true);
        }

        private void TsmiToHeightMapClick(object sender, EventArgs e)
        {
            GenerateHeightMap(false);
        }

        private void GenerateHeightMap(bool selectHeightMap)
        {
            FileMapWwCc1Pc map = m_LoadedFile as FileMapWwCc1Pc;
            if (map == null)
                return;
            SaveFocus(this);
            string loadedPath = m_LoadedFile.LoadedFile;
            string baseFileName = Path.Combine(Path.GetDirectoryName(loadedPath), Path.GetFileNameWithoutExtension(loadedPath));
            string pngFileName = baseFileName + ".png";
            Bitmap plateauImage = null;
            if (selectHeightMap)
            {
                SupportedFileType selectedType;
                string filename = FileDialogGenerator.ShowOpenFileFialog(this, "Select height levels image", new Type[] { typeof(FileImage) }, null, pngFileName, "images", null, true, out selectedType);
                if (filename == null)
                    return;
                m_LastOpenedFolder = Path.GetDirectoryName(filename);
                if (selectedType == null)
                    selectedType = new FileImage();
                try
                {
                    byte[] fileData = File.ReadAllBytes(filename);
                    selectedType.LoadFile(fileData, filename);
                    plateauImage = selectedType.GetBitmap();
                }
                catch (Exception e)
                {
                    string message = "Could not load file as " + selectedType.LongTypeName + ":\n\n" + e.Message;
                    ShowMessageBox(message, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                if (plateauImage.Width != 64 || plateauImage.Height != 64)
                {
                    string message = "Height levels image needs to be 64×64.";
                    ShowMessageBox(message, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
            }
            ExecuteThreaded(() => HeightMapGenerator.GenerateHeightMapImage64x64(map, plateauImage, null), true, true, true, "Generating height map");
        }

        private void TsmiTo65X65HeightMapClick(object sender, EventArgs e)
        {
            SupportedFileType image = GetShownFile();
            if (image == null || image.Width != 64 || image.Height != 64 || image.FileClass == FileClass.CcMap)
                return;
            SaveFocus(this);
            string baseFileName = Path.Combine(Path.GetDirectoryName(image.LoadedFile), Path.GetFileNameWithoutExtension(image.LoadedFile));
            string imgFileName = baseFileName + ".img";
            ExecuteThreaded(() => Make65x65HeightMap(image, imgFileName), true, false, false, "Creating height map");
        }

        private FileImgWwN64 Make65x65HeightMap(SupportedFileType image, string imgFileName)
        {
            Bitmap bm = HeightMapGenerator.GenerateHeightMapImage65x65(image.GetBitmap());
            //Byte[] imageData = ImageUtils.GetSavedImageData(bm, ref imgFileName);
            FileImgWwN64 file = new FileImgWwN64();
            file.LoadGrayImage(bm, Path.GetFileName(imgFileName), imgFileName);
            return file;
        }

        private void TsmiToPlateausClick(object sender, EventArgs e)
        {
            FileMapWwCc1Pc map = m_LoadedFile as FileMapWwCc1Pc;
            if (map == null)
                return;
            SaveFocus(this);
            ExecuteThreaded(() => HeightMapGenerator.GeneratePlateauImage64x64(map, "_lvl"), false, false, false, "Generating plateaus");
        }

        private void TsmiCombineShadowsClick(object sender, EventArgs e)
        {
            if (m_LoadedFile == null || m_LoadedFile.Frames == null || m_LoadedFile.Frames.Length == 0)
                return;
            SaveFocus(this);
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
                string message = "Error initializing conversion options: " + GeneralUtils.RecoverArgExceptionMessage(ex, true);
                ShowMessageBox(message, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            int ind;
            Int32.TryParse(Option.GetSaveOptionValue(so, "IND"), out ind);
            ExecuteThreaded(() => FileFramesWwShpTs.CombineShadows(m_LoadedFile, 1, (byte) ind), false, true, false, "Combining shadows");
        }

        private void TsmiSplitShadowsClick(object sender, EventArgs e)
        {
            if (m_LoadedFile == null || m_LoadedFile.Frames == null || m_LoadedFile.Frames.Length == 0)
                return;
            SaveFocus(this);
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
                string message = "Error initializing conversion options: " + GeneralUtils.RecoverArgExceptionMessage(ex, true);
                ShowMessageBox(message, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            int ind;
            Int32.TryParse(Option.GetSaveOptionValue(so, "IND"), out ind);
            ExecuteThreaded(() => FileFramesWwShpTs.SplitShadows(m_LoadedFile, (byte) ind, 1), false, true, false, "Splitting shadows");
        }

        private void TsmiApplyTransparencyMaskClick(object sender, EventArgs e)
        {
            if (m_LoadedFile == null || m_LoadedFile.Frames == null || m_LoadedFile.Frames.Length == 0)
                return;
            SaveFocus(this);



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
            int ind;
            Int32.TryParse(Option.GetSaveOptionValue(so, "IND"), out ind);
            //this.ExecuteThreaded(() => FileFrames.ApplyTransparencyMask(this.m_LoadedFile, (Byte)ind, 1), false, true, false, "Splitting shadows");
        }

        private void TsmiSplitTransparencyMaskClick(object sender, EventArgs e)
        {

        }

        private void TsmiPasteOnFramesClick(object sender, EventArgs e)
        {
            if (m_LoadedFile == null)
                return;
            bool singleImage = (m_LoadedFile.Frames == null || m_LoadedFile.Frames.Length == 0) && m_LoadedFile.GetBitmap() != null;
            if (!singleImage && m_LoadedFile.Frames.Length == 0)
                return;
            SaveFocus(this);
            try
            {
                SupportedFileType[] frames = singleImage ? new SupportedFileType[] { m_LoadedFile } : m_LoadedFile.Frames;
                int nrOfFrames = frames.Length;
                int maxWidth = frames.Max(fr => fr == null ? 0 : fr.Width);
                int maxHeight = frames.Max(fr => fr == null ? 0 : fr.Height);
                Bitmap image;
                Point pastePoint;
                int[] frameRange;
                bool keepIndices;
                int shownFrame = GetShownFrame();
                // Pastebox deliberately does not dispose its Image, so it can be passed on to the function.
                using (FrmPasteOnFrames pasteBox = new FrmPasteOnFrames(
                    nrOfFrames, maxWidth, maxHeight, Math.Abs(m_LoadedFile.BitsPerPixel), m_LastOpenedFolder, shownFrame))
                {
                    DialogResult dr = pasteBox.ShowDialog(this);
                    m_LastOpenedFolder = pasteBox.LastSelectedFolder;
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
                ExecuteThreaded(() => PasteOnFrames(m_LoadedFile, image, pastePoint, frameRange, keepIndices, true), false, false, false, "Pasting on frames");
            }
            catch (ArgumentException ex)
            {
                string message = GeneralUtils.RecoverArgExceptionMessage(ex, true);
                ShowMessageBox(message, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private SupportedFileType PasteOnFrames(SupportedFileType framesContainer, Bitmap image, Point pasteLocation, int[] framesRange, bool keepIndices, bool disposeImage)
        {
            SupportedFileType newfile = FileFrames.PasteImageOnFrames(framesContainer, image, pasteLocation, framesRange, keepIndices);
            if (disposeImage)
                image.Dispose();
            return newfile;
        }

        private void TsmiExtractPalClick(object sender, EventArgs e)
        {
            SupportedFileType shownImage = GetShownFile();
            if (shownImage == null)
                return;
            SaveFocus(this);
            int bpp = Math.Abs(shownImage.BitsPerPixel);
            Color[] pal = shownImage.GetColors();
            int nrOfColors = pal.Count();
            if (nrOfColors == 0 || (bpp != 1 && bpp != 2 && bpp != 4 && bpp != 8))
                return;
            ColorStatus cs = GetColorStatus();
            int fullPal = 1 << bpp;
            int height = (int) Math.Sqrt(fullPal);
            int width = fullPal / height;
            byte[] image = new byte[fullPal];
            for (int i = 0; i < fullPal; ++i)
                image[i] = (byte)i;
            if (bpp == 2)
                bpp = 4;
            PixelFormat pf = ImageUtils.GetIndexedPixelFormat(bpp);
            image = ImageUtils.ConvertFrom8Bit(image, width, height, bpp, true);
            int stride = ImageUtils.GetMinimumStride(width, bpp);
            Bitmap bm = ImageUtils.BuildImage(image, width, height, stride, pf, pal, Color.Black);
            FileImagePng palImage = new FileImagePng();
            string path = Path.GetDirectoryName(shownImage.LoadedFile);
            string name;
            PaletteDropDownInfo pddi = cmbPalettes.SelectedItem as PaletteDropDownInfo;
            if (cs == ColorStatus.External && pddi != null)
            {
                name = pddi.SourceFile;
                if (name == null)
                    name = Regex.Replace(pddi.Name, "[" + Regex.Escape(new string(Path.GetInvalidFileNameChars())) + "]", String.Empty);
                else if (name.EndsWith(".pal", StringComparison.InvariantCultureIgnoreCase))
                    name = name.Substring(0, name.Length - 4);
            }
            else
                name = Path.GetFileNameWithoutExtension(shownImage.LoadedFile);
            palImage.LoadFile(bm, Path.Combine(path, name + ".png"));
            ReloadWithDispose(palImage, true, true, true);
        }

        private void TsmiImageToPalette4BitClick(object sender, EventArgs e)
        {
            ImageToPalette(true);
        }

        private void TsmiImageToPalette8BitClick(object sender, EventArgs e)
        {
            ImageToPalette(false);
        }

        private void ImageToPalette(bool fourBit)
        {
            SupportedFileType shownImage = GetShownFile();
            if (shownImage == null || shownImage.GetBitmap() == null)
                return;
            SaveFocus(this);
            try
            {
                string maxCol = (fourBit ? 16 : 256).ToString(NumberFormatInfo.InvariantInfo);
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
                    string message = "Error initializing conversion options: " + GeneralUtils.RecoverArgExceptionMessage(ex, true);
                    ShowMessageBox(message, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                int coordX;
                Int32.TryParse(Option.GetSaveOptionValue(so, "CRX"), out coordX);
                int coordY;
                Int32.TryParse(Option.GetSaveOptionValue(so, "CRY"), out coordY);
                int limit;
                Int32.TryParse(Option.GetSaveOptionValue(so, "CRN"), out limit);
                ExecuteThreaded(() => ConvertToPalette(shownImage, coordX, coordY, fourBit, limit), true, true, true, "Converting to palette");
            }
            catch (ArgumentException ex)
            {
                string message = GeneralUtils.RecoverArgExceptionMessage(ex, true);
                ShowMessageBox(message, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private SupportedFileType ConvertToPalette(SupportedFileType file, int x, int y, bool fourBit, int limit)
        {
            if (file == null || (file.GetBitmap()) == null)
                return null;
            SaveFocus(this);
            int palWidth = fourBit ? 4 : 16;
            int palHeight = (limit + palWidth -1) / palWidth;
            int palSize = palWidth * palHeight;
            int palStride = fourBit ? palWidth / 2 : palWidth;
            string path = Path.GetDirectoryName(file.LoadedFile);
            string name = Path.GetFileNameWithoutExtension(file.LoadedFile);
            byte[] imageData = ImageUtils.GetImageData(file.GetBitmap(), PixelFormat.Format24bppRgb);
            int startPoint = (y * file.Width + x) * 3;
            int palLen = Math.Min(3 * limit, imageData.Length - startPoint);
            int palEnd = startPoint + palLen;
            byte[] paletteData = new byte[palLen];
            int palPtr = 0;
            for (int i = startPoint; i < palEnd; i += 3)
            {
                paletteData[palPtr++] = imageData[i + 2];
                paletteData[palPtr++] = imageData[i + 1];
                paletteData[palPtr++] = imageData[i];
            }
            Color[] col = ColorUtils.ReadEightBitPaletteFile(paletteData, false);
            byte[] newImageData = Enumerable.Range(0, limit).Select(b => (byte)b).ToArray();
            byte[] fullImageData = new byte[palSize];
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

        private void TsmiChangeTo24BitRgbClick(object sender, EventArgs e)
        {
            SupportedFileType fileToEdit = m_LoadedFile;
            if (fileToEdit == null || (fileToEdit.FileClass & FileClass.Image | FileClass.FrameSet) == 0)
                return;
            ExecuteThreaded(() => ChangeToRgb(fileToEdit, 24), true, true, true, "Changing to 24bpp RGB");
        }

        private void TsmiChangeTo32BitArgbClick(object sender, EventArgs e)
        {
            SupportedFileType fileToEdit = m_LoadedFile;
            if (fileToEdit == null || (fileToEdit.FileClass & FileClass.Image | FileClass.FrameSet) == 0)
                return;
            ExecuteThreaded(()=> ChangeToRgb(fileToEdit, 32), true, true, true, "Changing to 32bpp ARGB");
        }

        private SupportedFileType ChangeToRgb(SupportedFileType fileToEdit, int bpp)
        {
            PixelFormat pf = bpp == 24 ? PixelFormat.Format24bppRgb : PixelFormat.Format32bppArgb;
            if (!fileToEdit.IsFramesContainer)
            {
                Bitmap image = fileToEdit.GetBitmap();
                int stride;
                byte[] resBytes = ImageUtils.GetImageData(image, out stride, pf);
                Bitmap result = ImageUtils.BuildImage(resBytes, image.Width, image.Height, stride, pf, null, null);
                FileImagePng newFile = new FileImagePng();
                newFile.LoadFile(result, fileToEdit.LoadedFile);
                return newFile;
            }
            else
            {
                int frames = fileToEdit.Frames.Length;
                FileFrames newFile = new FileFrames(fileToEdit);
                newFile.SetFileNames(fileToEdit.LoadedFile);
                newFile.SetCommonPalette(true);
                newFile.SetBitsPerPixel(bpp);
                for (int i = 0; i < frames; ++i)
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
                    int stride;
                    byte[] resBytes = ImageUtils.GetImageData(image, out stride, pf);
                    Bitmap result = ImageUtils.BuildImage(resBytes, image.Width, image.Height, stride, pf, null, null);
                    newFrame.LoadFile(result, frame.LoadedFile);
                }
                return newFile;
            }
        }

        private void TsmiMatchToPaletteClick(object sender, EventArgs e)
        {
            SupportedFileType fileToEdit = m_LoadedFile;
            if (fileToEdit == null || (fileToEdit.FileClass & FileClass.Image | FileClass.FrameSet) == 0)
                return;
            List<PaletteDropDownInfo> allPalettes = new List<PaletteDropDownInfo>();
            allPalettes.AddRange(m_DefaultPalettes);
            allPalettes.AddRange(m_ReadPalettes);
            Color[] matchPalette;
            int matchBpp;
            using (FrmFramesToPal toPal = new FrmFramesToPal(fileToEdit, allPalettes.ToArray(), false))
            {
                DialogResult dr = toPal.ShowDialog(this);
                pzpImage.CustomColors = toPal.CustomColors;
                if (dr != DialogResult.OK)
                    return;
                matchBpp = toPal.MatchBpp;
                matchPalette = toPal.MatchPalette;
            }
            ExecuteThreaded(() => MatchToPalette(fileToEdit, matchBpp, matchPalette), true, true, true, "Matching to palette");
        }

        private void TsmiRemovePaletteClick(object sender, EventArgs e)
        {
            SupportedFileType fileToEdit = m_LoadedFile;
            if (fileToEdit == null || (fileToEdit.FileClass & (FileClass.Image | FileClass.FrameSet)) == 0)
                return;
            SupportedFileType editedFile = RemovePalette(fileToEdit);
            ReloadWithDispose(editedFile, true, false, false);
        }

        private void TsmiSetToDifferenPaletteClick(object sender, EventArgs e)
        {
            SupportedFileType fileToEdit = m_LoadedFile;
            if (fileToEdit == null || (fileToEdit.FileClass & (FileClass.Image | FileClass.FrameSet)) == 0)
                return;
            int bpp = fileToEdit.GetGlobalBpp();
            if (bpp == -1 || bpp > 8)
            {
                ShowMessageBox("This function only supports indexed types.", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            List<PaletteDropDownInfo> allPalettes = new List<PaletteDropDownInfo>();
            allPalettes.AddRange(m_DefaultPalettes);
            allPalettes.AddRange(m_ReadPalettes);
            Color[] matchPalette;
            using (FrmFramesToPal setPal = new FrmFramesToPal(fileToEdit, allPalettes.ToArray(), true))
            {
                DialogResult dr = setPal.ShowDialog(this);
                pzpImage.CustomColors = setPal.CustomColors;
                if (dr != DialogResult.OK)
                    return;
                matchPalette = setPal.MatchPalette;
            }
            ExecuteThreaded(()=> SetToPalette(fileToEdit, matchPalette), true, false, false, "Setting different palette");
        }

        private SupportedFileType RemovePalette(SupportedFileType fileToEdit)
        {
            int bpp = fileToEdit.GetGlobalBpp();
            if (bpp <= 0 || bpp > 8)
                return null;
            if (bpp > 1 && bpp < 4)
                bpp = 4;
            FileFrames newFile = fileToEdit as FileFrames;
            // only use case for not making a new one is FileFrames + FileImageFrame combo, since it can be 100 adjusted.
            bool keepFile = newFile != null && newFile.FramesList.All(fr => fr == null || fr is FileImageFrame);
            bool hasFrames = fileToEdit.IsFramesContainer && fileToEdit.Frames != null;
            int frames = hasFrames ? fileToEdit.Frames.Length : 1;
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
            for (int i = 0; i < frames; ++i)
            {
                SupportedFileType frame = hasFrames ? fileToEdit.Frames[i] : fileToEdit;
                if (frame == null)
                    continue;
                FileImageFrame newFrame = frame as FileImageFrame;
                bool keepFrame = newFrame != null && keepFile;
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

        private SupportedFileType MatchToPalette(SupportedFileType fileToEdit, int matchBpp, Color[] matchPalette)
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
                int frames = fileToEdit.Frames.Length;
                FileFrames newFile = new FileFrames(fileToEdit);
                newFile.SetFileNames(fileToEdit.LoadedFile);
                newFile.SetCommonPalette(true);
                newFile.SetBitsPerPixel(matchBpp);
                newFile.SetPalette(matchPalette);
                for (int i = 0; i < frames; ++i)
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
            int bpp = fileToEdit.GetGlobalBpp();
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
            bool hasFrames = fileToEdit.IsFramesContainer && fileToEdit.Frames != null;

            int frames = hasFrames ? fileToEdit.Frames.Length : 1;
            FileFrames newFile = null;
            if (hasFrames)
            {
                newFile = new FileFrames(fileToEdit);
                newFile.SetFileNames(fileToEdit.LoadedFile);
                newFile.SetCommonPalette(true);
                newFile.SetBitsPerPixel(bpp);
                newFile.SetPalette(newPalette);
            }
            for (int i = 0; i < frames; ++i)
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

        private void TsmiExtract4BitPalClick(object sender, EventArgs e)
        {
            SupportedFileType shownFile = GetShownFile();
            if (shownFile == null || shownFile.BitsPerPixel != 8 || GetColorStatus() == ColorStatus.None)
                return;
            SaveFocus(this);
            Option[] so = new Option[1];
            so[0] = new Option("start", OptionInputType.Number, "Start index", "0," + 240, "0");
            SaveOptionInfo soi = new SaveOptionInfo();
            soi.Name = "16-color palette from 256-color palette.\n\nSelect start index of 16-color range. Press Cancel to select manually.";
            soi.Properties = so;
            int[] selectedIndices = null;
            try
            {
                using (FrmOptions opts = new FrmOptions(GetTitle(), soi))
                {
                    opts.Height = opts.OptimalHeight;
                    if (opts.ShowDialog(this) == DialogResult.OK)
                    {
                        int startIndex;
                        Int32.TryParse(Option.GetSaveOptionValue(so, "start"), out startIndex);
                        selectedIndices = Enumerable.Range(startIndex, Math.Min(16, 256 - startIndex)).ToArray();
                    }
                }
            }
            catch (ArgumentException ex)
            {
                string message = "Error initializing conversion options: " + GeneralUtils.RecoverArgExceptionMessage(ex, true);
                ShowMessageBox(message, MessageBoxButtons.OK, MessageBoxIcon.Warning);
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
            byte[] newImageData = Enumerable.Range(0, 16).Select(b => (byte) b).ToArray();
            newImageData = ImageUtils.ConvertFrom8Bit(newImageData, 4, 4, 4, true);
            PixelFormat pf = PixelFormat.Format4bppIndexed;
            Bitmap bm = ImageUtils.BuildImage(newImageData, 4, 4, 2, pf, col, Color.Black);
            FileImagePng palImage = new FileImagePng();
            string path = Path.GetDirectoryName(shownFile.LoadedFile);
            string name = Path.GetFileNameWithoutExtension(shownFile.LoadedFile);
            palImage.LoadFile(bm, Path.Combine(path, name + ".png"));
            ReloadWithDispose(palImage, true, true, true);
        }

        private void TsmiManagePalettes4BitClick(object sender, EventArgs e)
        {
            ManagePalettes(true);
        }

        private void TsmiManagePalettes8BitClick(object sender, EventArgs e)
        {
            ManagePalettes(false);
        }

        private void ManagePalettes(bool fourBit)
        {
            SaveFocus(this);
            using (FrmManagePalettes palSave = new FrmManagePalettes(fourBit ? 4 : 8, m_PalettePath))
            {
                palSave.Icon = Icon;
                palSave.Title = GetTitle();
                palSave.PaletteToSave = null;
                palSave.StartPosition = FormStartPosition.CenterParent;
                if (palSave.ShowDialog(this) != DialogResult.OK)
                    return;
            }
            RefreshPalettes(true, true);
            RefreshColorControls();
        }

        /// <summary>
        /// Executes a threaded operation while locking the UI. 
        /// </summary>
        /// <param name="function">A function returning SupportedFileType</param>
        /// <param name="resetPalettes">True to reset palettes dropdown when loading the file resulting from the operation</param>
        /// <param name="resetIndex">True to reset frames index when loading the file resulting from the operation</param>
        /// <param name="resetZoom">True to reset auto-zoom when loading the file resulting from the operation</param>
        /// <param name="operationType">String to indicate the process type being executed (eg. "Saving")</param>
        private void ExecuteThreaded(Func<SupportedFileType> function, bool resetPalettes, bool resetIndex, bool resetZoom, string operationType)
        {

            smt.ExecuteThreaded(
                function,
                (newfile) => ReloadWithDispose(newfile, resetPalettes, resetIndex, resetZoom), true,
                EnableControls, operationType);
        }

        private void EnableControls(bool enabled, string processingLabel, SimpleMultiThreading currentMultiThreader)
        {
            if (!enabled)
                m_Loading = true;
            EnableToolstrips(enabled);
            if (!enabled)
            {
                AllowDrop = false;
                // To prevent UI updates using loaded images from interfering with internal operations.
                // The UI gets reloaded afterwards anyway, so that should always restore the image.
                pzpImage.Image = null;
                // Disable controls
                numFrame.Enabled = false;
                cmbPalettes.Enabled = false;
                btnSavePalette.Enabled = false;
                // Create busy status label.
                currentMultiThreader.CreateBusyLabel(this, processingLabel);
            }
            else
                ReloadUi(false, false);
            pzpImage.Enabled = enabled;
            if (enabled)
                m_Loading = false;
        }

        private void ReloadWithDispose(SupportedFileType newFile, bool resetPalettes, bool resetIndex, bool resetZoom)
        {
            SupportedFileType oldFile = m_LoadedFile;
            m_LoadedFile = newFile;
            if (resetZoom)
                AutoSetZoom();
            EnableToolstrips(true);
            if (!pzpImage.Enabled)
                pzpImage.Enabled = true;
            ReloadUi(resetPalettes, resetIndex);
            // Don't dispose if the object is the same.
            if (oldFile != null && oldFile != newFile)
            {
                try { oldFile.Dispose(); }
                catch { /*ignore*/ }
            }
            m_Loading = false;
        }

        private void EnableToolstrips(bool enable)
        {
            tsmiOpen.Enabled = enable;
            tsmiSave.Enabled = enable;
            tsmiSaveRaw.Enabled = enable;
            tsmiSaveFrames.Enabled = enable;
            if (!enable)
            {
                // Let the UI reload take care of re-enabling these.
                tsmiCopy.Enabled = false;
                tsmiImageToFrames.Enabled = false;
                tsmiFramesToSingleImage.Enabled = false;
                tsmiToHeightMap.Enabled = false;
                tsmiToPlateaus.Enabled = false;
                tsmiToHeightMapAdv.Enabled = false;
                tsmiTo65x65HeightMap.Enabled = false;
                tsmiCombineShadows.Enabled = false;
                tsmiSplitShadows.Enabled = false;
                tsmiExtractPal.Enabled = false;
                tsmiExtract4BitPal.Enabled = false;
                tsmiImageToPalette4Bit.Enabled = false;
                tsmiImageToPalette8Bit.Enabled = false;
            }
#if DEBUG
            tsmiTestBed.Enabled = enable;
#endif
        }

        private DialogResult ShowMessageBox(string message, MessageBoxButtons buttons, MessageBoxIcon icon)
        {
            if (message == null)
                return DialogResult.Cancel;
            AllowDrop = false;
            DialogResult result = MessageBox.Show(this, message, GetTitle(), buttons, icon);
            AllowDrop = true;
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

        private void TsmiTestBedClick(object sender, EventArgs e)
        {
#if DEBUG
            SaveFocus(this);
            ExecuteTestCode();
#endif
        }

    }

}
