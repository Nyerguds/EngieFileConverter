namespace EngieFileConverter.UI
{
    partial class FrmFileConverter
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.lblFilename = new System.Windows.Forms.Label();
            this.lblSize = new System.Windows.Forms.Label();
            this.lblColorformat = new System.Windows.Forms.Label();
            this.lblValFilename = new System.Windows.Forms.Label();
            this.lblColorsInPal = new System.Windows.Forms.Label();
            this.lblValSize = new System.Windows.Forms.Label();
            this.lblValColorFormat = new System.Windows.Forms.Label();
            this.lblValColorsInPal = new System.Windows.Forms.Label();
            this.lblType = new System.Windows.Forms.Label();
            this.lblValType = new System.Windows.Forms.Label();
            this.menuStrip1 = new System.Windows.Forms.MenuStrip();
            this.fileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.tsmiOpen = new System.Windows.Forms.ToolStripMenuItem();
            this.tsmiSave = new System.Windows.Forms.ToolStripMenuItem();
            this.tsmiSaveFrames = new System.Windows.Forms.ToolStripMenuItem();
            this.tsmiExit = new System.Windows.Forms.ToolStripMenuItem();
            this.tsmiEdit = new System.Windows.Forms.ToolStripMenuItem();
            this.tsmiCopy = new System.Windows.Forms.ToolStripMenuItem();
            this.tsmiImageToFrames = new System.Windows.Forms.ToolStripMenuItem();
            this.tsmiFramesToSingleImage = new System.Windows.Forms.ToolStripMenuItem();
            this.tsmiHeightMapTools = new System.Windows.Forms.ToolStripMenuItem();
            this.tsmiToHeightMap = new System.Windows.Forms.ToolStripMenuItem();
            this.tsmiToPlateaus = new System.Windows.Forms.ToolStripMenuItem();
            this.tsmiToHeightMapAdv = new System.Windows.Forms.ToolStripMenuItem();
            this.tsmiTo65x65HeightMap = new System.Windows.Forms.ToolStripMenuItem();
            this.tsmiShadowSplit = new System.Windows.Forms.ToolStripMenuItem();
            this.tsmiCombineShadows = new System.Windows.Forms.ToolStripMenuItem();
            this.tsmiSplitShadows = new System.Windows.Forms.ToolStripMenuItem();
            this.tsmiAnimation = new System.Windows.Forms.ToolStripMenuItem();
            this.tsmiPasteOnFrames = new System.Windows.Forms.ToolStripMenuItem();
            this.tsmiTestBed = new System.Windows.Forms.ToolStripMenuItem();
            this.lblFrame = new System.Windows.Forms.Label();
            this.lblNrOfFrames = new System.Windows.Forms.Label();
            this.btnResetPalette = new System.Windows.Forms.Button();
            this.btnSavePalette = new System.Windows.Forms.Button();
            this.toolTip1 = new System.Windows.Forms.ToolTip(this.components);
            this.lblInfo = new System.Windows.Forms.Label();
            this.lblValInfo = new System.Windows.Forms.Label();
            this.cmbPalettes = new Nyerguds.Util.UI.ComboBoxSmartWidth();
            this.numFrame = new Nyerguds.Util.UI.EnhNumericUpDown();
            this.palColorViewer = new Nyerguds.Util.UI.PalettePanel();
            this.pzpImage = new Nyerguds.Util.UI.PixelZoomPanel();
            this.menuStrip1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numFrame)).BeginInit();
            this.SuspendLayout();
            // 
            // lblFilename
            // 
            this.lblFilename.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.lblFilename.Location = new System.Drawing.Point(664, 24);
            this.lblFilename.Name = "lblFilename";
            this.lblFilename.Size = new System.Drawing.Size(94, 23);
            this.lblFilename.TabIndex = 100;
            this.lblFilename.Text = "Filename:";
            this.lblFilename.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblSize
            // 
            this.lblSize.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.lblSize.Location = new System.Drawing.Point(664, 100);
            this.lblSize.Name = "lblSize";
            this.lblSize.Size = new System.Drawing.Size(94, 23);
            this.lblSize.TabIndex = 102;
            this.lblSize.Text = "Image size:";
            this.lblSize.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblColorformat
            // 
            this.lblColorformat.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.lblColorformat.Location = new System.Drawing.Point(664, 123);
            this.lblColorformat.Name = "lblColorformat";
            this.lblColorformat.Size = new System.Drawing.Size(94, 23);
            this.lblColorformat.TabIndex = 108;
            this.lblColorformat.Text = "Color format:";
            this.lblColorformat.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblValFilename
            // 
            this.lblValFilename.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.lblValFilename.Location = new System.Drawing.Point(664, 47);
            this.lblValFilename.Name = "lblValFilename";
            this.lblValFilename.Size = new System.Drawing.Size(240, 23);
            this.lblValFilename.TabIndex = 101;
            this.lblValFilename.Text = "---";
            this.lblValFilename.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblColorsInPal
            // 
            this.lblColorsInPal.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.lblColorsInPal.Location = new System.Drawing.Point(664, 146);
            this.lblColorsInPal.Name = "lblColorsInPal";
            this.lblColorsInPal.Size = new System.Drawing.Size(94, 23);
            this.lblColorsInPal.TabIndex = 112;
            this.lblColorsInPal.Text = "Colors in palette:";
            this.lblColorsInPal.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblValSize
            // 
            this.lblValSize.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.lblValSize.Location = new System.Drawing.Point(747, 100);
            this.lblValSize.Name = "lblValSize";
            this.lblValSize.Size = new System.Drawing.Size(157, 23);
            this.lblValSize.TabIndex = 105;
            this.lblValSize.Text = "---";
            this.lblValSize.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblValColorFormat
            // 
            this.lblValColorFormat.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.lblValColorFormat.Location = new System.Drawing.Point(747, 123);
            this.lblValColorFormat.Name = "lblValColorFormat";
            this.lblValColorFormat.Size = new System.Drawing.Size(157, 23);
            this.lblValColorFormat.TabIndex = 109;
            this.lblValColorFormat.Text = "---";
            this.lblValColorFormat.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblValColorsInPal
            // 
            this.lblValColorsInPal.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.lblValColorsInPal.Location = new System.Drawing.Point(747, 146);
            this.lblValColorsInPal.Name = "lblValColorsInPal";
            this.lblValColorsInPal.Size = new System.Drawing.Size(157, 23);
            this.lblValColorsInPal.TabIndex = 113;
            this.lblValColorsInPal.Text = "---";
            this.lblValColorsInPal.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblType
            // 
            this.lblType.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.lblType.Location = new System.Drawing.Point(664, 77);
            this.lblType.Name = "lblType";
            this.lblType.Size = new System.Drawing.Size(94, 23);
            this.lblType.TabIndex = 102;
            this.lblType.Text = "File type:";
            this.lblType.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblValType
            // 
            this.lblValType.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.lblValType.Location = new System.Drawing.Point(747, 73);
            this.lblValType.Name = "lblValType";
            this.lblValType.Size = new System.Drawing.Size(157, 30);
            this.lblValType.TabIndex = 120;
            this.lblValType.Text = "---";
            this.lblValType.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // menuStrip1
            // 
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.fileToolStripMenuItem,
            this.tsmiEdit});
            this.menuStrip1.Location = new System.Drawing.Point(0, 0);
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.Size = new System.Drawing.Size(904, 24);
            this.menuStrip1.TabIndex = 121;
            this.menuStrip1.Text = "menuStrip1";
            // 
            // fileToolStripMenuItem
            // 
            this.fileToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.tsmiOpen,
            this.tsmiSave,
            this.tsmiSaveFrames,
            this.tsmiExit});
            this.fileToolStripMenuItem.Name = "fileToolStripMenuItem";
            this.fileToolStripMenuItem.Size = new System.Drawing.Size(37, 20);
            this.fileToolStripMenuItem.Text = "File";
            // 
            // tsmiOpen
            // 
            this.tsmiOpen.Name = "tsmiOpen";
            this.tsmiOpen.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.O)));
            this.tsmiOpen.Size = new System.Drawing.Size(200, 22);
            this.tsmiOpen.Text = "&Open file";
            this.tsmiOpen.Click += new System.EventHandler(this.TsmiOpen_Click);
            // 
            // tsmiSave
            // 
            this.tsmiSave.Name = "tsmiSave";
            this.tsmiSave.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.S)));
            this.tsmiSave.Size = new System.Drawing.Size(200, 22);
            this.tsmiSave.Text = "&Save file...";
            this.tsmiSave.Click += new System.EventHandler(this.TsmiSave_Click);
            // 
            // tsmiSaveFrames
            // 
            this.tsmiSaveFrames.Name = "tsmiSaveFrames";
            this.tsmiSaveFrames.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.F)));
            this.tsmiSaveFrames.Size = new System.Drawing.Size(200, 22);
            this.tsmiSaveFrames.Text = "Save as &frames...";
            this.tsmiSaveFrames.Click += new System.EventHandler(this.TsmiSaveFrames_Click);
            // 
            // tsmiExit
            // 
            this.tsmiExit.Name = "tsmiExit";
            this.tsmiExit.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Alt | System.Windows.Forms.Keys.F4)));
            this.tsmiExit.Size = new System.Drawing.Size(200, 22);
            this.tsmiExit.Text = "Exit";
            this.tsmiExit.Click += new System.EventHandler(this.TsmiExit_Click);
            // 
            // tsmiEdit
            // 
            this.tsmiEdit.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.tsmiCopy,
            this.tsmiImageToFrames,
            this.tsmiFramesToSingleImage,
            this.tsmiHeightMapTools,
            this.tsmiShadowSplit,
            this.tsmiAnimation,
            this.tsmiTestBed});
            this.tsmiEdit.Name = "tsmiEdit";
            this.tsmiEdit.Size = new System.Drawing.Size(39, 20);
            this.tsmiEdit.Text = "Edit";
            // 
            // tsmiCopy
            // 
            this.tsmiCopy.Name = "tsmiCopy";
            this.tsmiCopy.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.C)));
            this.tsmiCopy.Size = new System.Drawing.Size(246, 22);
            this.tsmiCopy.Text = "Copy";
            this.tsmiCopy.Click += new System.EventHandler(this.tsmiCopy_Click);
            // 
            // tsmiImageToFrames
            // 
            this.tsmiImageToFrames.Name = "tsmiImageToFrames";
            this.tsmiImageToFrames.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.I)));
            this.tsmiImageToFrames.Size = new System.Drawing.Size(246, 22);
            this.tsmiImageToFrames.Text = "&Image to frames...";
            this.tsmiImageToFrames.Click += new System.EventHandler(this.TsmiImageToFramesClick);
            // 
            // tsmiFramesToSingleImage
            // 
            this.tsmiFramesToSingleImage.Name = "tsmiFramesToSingleImage";
            this.tsmiFramesToSingleImage.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.R)));
            this.tsmiFramesToSingleImage.Size = new System.Drawing.Size(246, 22);
            this.tsmiFramesToSingleImage.Text = "F&rames to single image...";
            this.tsmiFramesToSingleImage.Click += new System.EventHandler(this.TsmiFramesToSingleImageClick);
            // 
            // tsmiHeightMapTools
            // 
            this.tsmiHeightMapTools.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.tsmiToHeightMap,
            this.tsmiToPlateaus,
            this.tsmiToHeightMapAdv,
            this.tsmiTo65x65HeightMap});
            this.tsmiHeightMapTools.Name = "tsmiHeightMapTools";
            this.tsmiHeightMapTools.Size = new System.Drawing.Size(246, 22);
            this.tsmiHeightMapTools.Text = "N64 height map generation";
            // 
            // tsmiToHeightMap
            // 
            this.tsmiToHeightMap.Name = "tsmiToHeightMap";
            this.tsmiToHeightMap.Size = new System.Drawing.Size(285, 22);
            this.tsmiToHeightMap.Text = "Map to &basic height map image";
            this.tsmiToHeightMap.Visible = false;
            this.tsmiToHeightMap.Click += new System.EventHandler(this.TsmiToHeightMap_Click);
            // 
            // tsmiToPlateaus
            // 
            this.tsmiToPlateaus.Name = "tsmiToPlateaus";
            this.tsmiToPlateaus.Size = new System.Drawing.Size(285, 22);
            this.tsmiToPlateaus.Text = "Map to basic &levels image";
            this.tsmiToPlateaus.Click += new System.EventHandler(this.TsmiToPlateaus_Click);
            // 
            // tsmiToHeightMapAdv
            // 
            this.tsmiToHeightMapAdv.Name = "tsmiToHeightMapAdv";
            this.tsmiToHeightMapAdv.Size = new System.Drawing.Size(285, 22);
            this.tsmiToHeightMapAdv.Text = "Map to &height map using levels";
            this.tsmiToHeightMapAdv.Click += new System.EventHandler(this.TsmiToHeightMapAdv_Click);
            // 
            // tsmiTo65x65HeightMap
            // 
            this.tsmiTo65x65HeightMap.Name = "tsmiTo65x65HeightMap";
            this.tsmiTo65x65HeightMap.Size = new System.Drawing.Size(285, 22);
            this.tsmiTo65x65HeightMap.Text = "64×64 image to 65×65 height map &image";
            this.tsmiTo65x65HeightMap.Click += new System.EventHandler(this.TsmiTo65x65HeightMap_Click);
            // 
            // tsmiShadowSplit
            // 
            this.tsmiShadowSplit.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.tsmiCombineShadows,
            this.tsmiSplitShadows});
            this.tsmiShadowSplit.Name = "tsmiShadowSplit";
            this.tsmiShadowSplit.Size = new System.Drawing.Size(246, 22);
            this.tsmiShadowSplit.Text = "TS shadow splitting";
            // 
            // tsmiCombineShadows
            // 
            this.tsmiCombineShadows.Name = "tsmiCombineShadows";
            this.tsmiCombineShadows.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.M)));
            this.tsmiCombineShadows.Size = new System.Drawing.Size(217, 22);
            this.tsmiCombineShadows.Text = "Co&mbine shadows";
            this.tsmiCombineShadows.Click += new System.EventHandler(this.TsmiCombineShadows_Click);
            // 
            // tsmiSplitShadows
            // 
            this.tsmiSplitShadows.Name = "tsmiSplitShadows";
            this.tsmiSplitShadows.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.T)));
            this.tsmiSplitShadows.Size = new System.Drawing.Size(217, 22);
            this.tsmiSplitShadows.Text = "Spli&t shadows";
            this.tsmiSplitShadows.Click += new System.EventHandler(this.TsmiSplitShadows_Click);
            // 
            // tsmiAnimation
            // 
            this.tsmiAnimation.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.tsmiPasteOnFrames});
            this.tsmiAnimation.Name = "tsmiAnimation";
            this.tsmiAnimation.Size = new System.Drawing.Size(246, 22);
            this.tsmiAnimation.Text = "Animation";
            // 
            // tsmiPasteOnFrames
            // 
            this.tsmiPasteOnFrames.Name = "tsmiPasteOnFrames";
            this.tsmiPasteOnFrames.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.P)));
            this.tsmiPasteOnFrames.Size = new System.Drawing.Size(244, 22);
            this.tsmiPasteOnFrames.Text = "&Paste image on frames...";
            this.tsmiPasteOnFrames.Click += new System.EventHandler(this.TsmiPasteOnFrames_Click);
            // 
            // tsmiTestBed
            // 
            this.tsmiTestBed.Name = "tsmiTestBed";
            this.tsmiTestBed.Size = new System.Drawing.Size(246, 22);
            this.tsmiTestBed.Text = "Test bed";
            this.tsmiTestBed.Visible = false;
            this.tsmiTestBed.Click += new System.EventHandler(this.TsmiTestBed);
            // 
            // lblFrame
            // 
            this.lblFrame.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.lblFrame.Location = new System.Drawing.Point(12, 527);
            this.lblFrame.Name = "lblFrame";
            this.lblFrame.Size = new System.Drawing.Size(40, 20);
            this.lblFrame.TabIndex = 124;
            this.lblFrame.Text = "Frame:";
            this.lblFrame.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // lblNrOfFrames
            // 
            this.lblNrOfFrames.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.lblNrOfFrames.Location = new System.Drawing.Point(129, 527);
            this.lblNrOfFrames.Margin = new System.Windows.Forms.Padding(0, 0, 3, 0);
            this.lblNrOfFrames.Name = "lblNrOfFrames";
            this.lblNrOfFrames.Size = new System.Drawing.Size(40, 20);
            this.lblNrOfFrames.TabIndex = 125;
            this.lblNrOfFrames.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // btnResetPalette
            // 
            this.btnResetPalette.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnResetPalette.Enabled = false;
            this.btnResetPalette.Location = new System.Drawing.Point(788, 522);
            this.btnResetPalette.Name = "btnResetPalette";
            this.btnResetPalette.Size = new System.Drawing.Size(49, 23);
            this.btnResetPalette.TabIndex = 313;
            this.btnResetPalette.Text = "Revert";
            this.btnResetPalette.UseVisualStyleBackColor = true;
            this.btnResetPalette.Click += new System.EventHandler(this.BtnResetPalette_Click);
            // 
            // btnSavePalette
            // 
            this.btnSavePalette.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnSavePalette.Enabled = false;
            this.btnSavePalette.Location = new System.Drawing.Point(843, 522);
            this.btnSavePalette.Name = "btnSavePalette";
            this.btnSavePalette.Size = new System.Drawing.Size(49, 23);
            this.btnSavePalette.TabIndex = 314;
            this.btnSavePalette.Text = "Save...";
            this.btnSavePalette.UseVisualStyleBackColor = true;
            this.btnSavePalette.Click += new System.EventHandler(this.BtnSavePalette_Click);
            // 
            // lblInfo
            // 
            this.lblInfo.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.lblInfo.Location = new System.Drawing.Point(664, 169);
            this.lblInfo.Name = "lblInfo";
            this.lblInfo.Size = new System.Drawing.Size(94, 23);
            this.lblInfo.TabIndex = 112;
            this.lblInfo.Text = "Additional info:";
            this.lblInfo.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblValInfo
            // 
            this.lblValInfo.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.lblValInfo.Location = new System.Drawing.Point(667, 192);
            this.lblValInfo.Name = "lblValInfo";
            this.lblValInfo.Size = new System.Drawing.Size(237, 92);
            this.lblValInfo.TabIndex = 315;
            // 
            // cmbPalettes
            // 
            this.cmbPalettes.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.cmbPalettes.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbPalettes.Enabled = false;
            this.cmbPalettes.FormattingEnabled = true;
            this.cmbPalettes.Location = new System.Drawing.Point(667, 524);
            this.cmbPalettes.Name = "cmbPalettes";
            this.cmbPalettes.Size = new System.Drawing.Size(115, 21);
            this.cmbPalettes.TabIndex = 126;
            this.cmbPalettes.SelectedIndexChanged += new System.EventHandler(this.CmbPalettes_SelectedIndexChanged);
            // 
            // numFrame
            // 
            this.numFrame.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.numFrame.EnteredValue = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.numFrame.Location = new System.Drawing.Point(58, 529);
            this.numFrame.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            -2147483648});
            this.numFrame.Name = "numFrame";
            this.numFrame.SelectedText = "";
            this.numFrame.SelectionLength = 0;
            this.numFrame.SelectionStart = 0;
            this.numFrame.Size = new System.Drawing.Size(68, 20);
            this.numFrame.TabIndex = 123;
            this.numFrame.Value = new decimal(new int[] {
            1,
            0,
            0,
            -2147483648});
            this.numFrame.ValueChanged += new System.EventHandler(this.numFrame_ValueChanged);
            // 
            // palColorViewer
            // 
            this.palColorViewer.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.palColorViewer.AutoSize = true;
            this.palColorViewer.ColorSelectMode = Nyerguds.Util.UI.ColorSelMode.None;
            this.palColorViewer.LabelSize = new System.Drawing.Size(12, 12);
            this.palColorViewer.Location = new System.Drawing.Point(667, 287);
            this.palColorViewer.Name = "palColorViewer";
            this.palColorViewer.PadBetween = new System.Drawing.Point(2, 2);
            this.palColorViewer.Palette = null;
            this.palColorViewer.Remap = null;
            this.palColorViewer.SelectedIndices = new int[0];
            this.palColorViewer.ShowColorToolTipsAlpha = true;
            this.palColorViewer.Size = new System.Drawing.Size(226, 226);
            this.palColorViewer.TabIndex = 122;
            this.palColorViewer.TransItemBackColor = System.Drawing.Color.Empty;
            this.palColorViewer.ColorLabelMouseDoubleClick += new Nyerguds.Util.UI.PaletteClickEventHandler(this.PalColorViewer_ColorLabelMouseDoubleClick);
            this.palColorViewer.ColorLabelMouseClick += new Nyerguds.Util.UI.PaletteClickEventHandler(this.PalColorViewer_ColorLabelMouseClick);
            // 
            // pzpImage
            // 
            this.pzpImage.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.pzpImage.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.pzpImage.CustomColors = null;
            this.pzpImage.Image = null;
            this.pzpImage.ImageVisible = false;
            this.pzpImage.Location = new System.Drawing.Point(12, 30);
            this.pzpImage.Margin = new System.Windows.Forms.Padding(0);
            this.pzpImage.Name = "pzpImage";
            this.pzpImage.Size = new System.Drawing.Size(646, 522);
            this.pzpImage.TabIndex = 316;
            this.pzpImage.ZoomFactor = 1;
            this.pzpImage.ZoomFactorMinimum = -10;
            // 
            // FrmFileConverter
            // 
            this.AllowDrop = true;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(904, 555);
            this.Controls.Add(this.lblValInfo);
            this.Controls.Add(this.btnResetPalette);
            this.Controls.Add(this.btnSavePalette);
            this.Controls.Add(this.cmbPalettes);
            this.Controls.Add(this.lblNrOfFrames);
            this.Controls.Add(this.lblFrame);
            this.Controls.Add(this.numFrame);
            this.Controls.Add(this.palColorViewer);
            this.Controls.Add(this.lblValType);
            this.Controls.Add(this.lblValColorsInPal);
            this.Controls.Add(this.lblValColorFormat);
            this.Controls.Add(this.lblValSize);
            this.Controls.Add(this.lblInfo);
            this.Controls.Add(this.lblColorsInPal);
            this.Controls.Add(this.lblValFilename);
            this.Controls.Add(this.lblColorformat);
            this.Controls.Add(this.lblType);
            this.Controls.Add(this.lblSize);
            this.Controls.Add(this.lblFilename);
            this.Controls.Add(this.menuStrip1);
            this.Controls.Add(this.pzpImage);
            this.Icon = global::EngieFileConverter.Properties.Resources.EngieIcon;
            this.KeyPreview = true;
            this.MainMenuStrip = this.menuStrip1;
            this.MinimumSize = new System.Drawing.Size(700, 593);
            this.Name = "FrmFileConverter";
            this.Text = "Engie File Converter - Created by Nyerguds";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.FrmFileConverter_FormClosing);
            this.Shown += new System.EventHandler(this.FrmFileConverter_Shown);
            this.DragDrop += new System.Windows.Forms.DragEventHandler(this.Frm_DragDrop);
            this.DragEnter += new System.Windows.Forms.DragEventHandler(this.Frm_DragEnter);
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numFrame)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label lblFilename;
        private System.Windows.Forms.Label lblSize;
        private System.Windows.Forms.Label lblColorformat;
        private System.Windows.Forms.Label lblValFilename;
        private System.Windows.Forms.Label lblColorsInPal;
        private System.Windows.Forms.Label lblValSize;
        private System.Windows.Forms.Label lblValColorFormat;
        private System.Windows.Forms.Label lblValColorsInPal;
        private System.Windows.Forms.Label lblType;
        private System.Windows.Forms.Label lblValType;
        private System.Windows.Forms.MenuStrip menuStrip1;
        private System.Windows.Forms.ToolStripMenuItem fileToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem tsmiOpen;
        private System.Windows.Forms.ToolStripMenuItem tsmiSave;
        private System.Windows.Forms.ToolStripMenuItem tsmiExit;
        private Nyerguds.Util.UI.PalettePanel palColorViewer;
        private System.Windows.Forms.ToolStripMenuItem tsmiEdit;
        private System.Windows.Forms.ToolStripMenuItem tsmiHeightMapTools;
        private System.Windows.Forms.ToolStripMenuItem tsmiToHeightMap;
        private System.Windows.Forms.ToolStripMenuItem tsmiToPlateaus;
        private System.Windows.Forms.ToolStripMenuItem tsmiToHeightMapAdv;
        private System.Windows.Forms.ToolStripMenuItem tsmiTo65x65HeightMap;
        private Nyerguds.Util.UI.EnhNumericUpDown numFrame;
        private System.Windows.Forms.Label lblFrame;
        private System.Windows.Forms.Label lblNrOfFrames;
        private Nyerguds.Util.UI.ComboBoxSmartWidth cmbPalettes;
        private System.Windows.Forms.Button btnResetPalette;
        private System.Windows.Forms.Button btnSavePalette;
        private System.Windows.Forms.ToolStripMenuItem tsmiCopy;
        private System.Windows.Forms.ToolTip toolTip1;
        private System.Windows.Forms.Label lblInfo;
        private System.Windows.Forms.Label lblValInfo;
        private System.Windows.Forms.ToolStripMenuItem tsmiShadowSplit;
        private System.Windows.Forms.ToolStripMenuItem tsmiCombineShadows;
        private System.Windows.Forms.ToolStripMenuItem tsmiSplitShadows;
        private System.Windows.Forms.ToolStripMenuItem tsmiAnimation;
        private System.Windows.Forms.ToolStripMenuItem tsmiPasteOnFrames;
        private System.Windows.Forms.ToolStripMenuItem tsmiTestBed;
        private System.Windows.Forms.ToolStripMenuItem tsmiSaveFrames;
        private Nyerguds.Util.UI.PixelZoomPanel pzpImage;
        private System.Windows.Forms.ToolStripMenuItem tsmiImageToFrames;
        private System.Windows.Forms.ToolStripMenuItem tsmiFramesToSingleImage;
    }
}

