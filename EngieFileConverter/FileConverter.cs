using EngieFileConverter.Domain;
using Nyerguds.ImageManipulation;
using Nyerguds.Util;
using Nyerguds.Util.UI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using EngieFileConverter.Domain.FileTypes;
using EngieFileConverter.UI;
using Nyerguds.GameData.Westwood;

namespace EngieFileConverter
{
    public static class FileConverter
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        public static Int32 Run(String[] args)
        {
#if DEBUG
            if (args.Length > 1)
                return ConvertImage(args);
#endif
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new FrmFileConverter(args));
            return 0;
        }

        [DllImport("Kernel32.dll")]
        public static extern Boolean AttachConsole(Int32 processId);

#if DEBUG
        public static Int32 ConvertImage(String[] args)
        {
            Boolean hasconsole = AttachConsole(-1);
            Boolean showErrors = true;
            Boolean showFeedback = false;
            Boolean noCompress = false;
            Boolean tilesets = false;

            Int32 i;
            Boolean readPaletteFile = false;
            String paletteFile = null;
            for(i = 0; i < args.Length; i++)
            {
                if (readPaletteFile)
                {
                    paletteFile = GeneralUtils.GetAbsolutePath(args[i]);
                    readPaletteFile = false;
                    continue;
                }
                if (!args[i].StartsWith("/"))
                    break;
                if (String.Equals(args[i], "/Q", StringComparison.InvariantCultureIgnoreCase))
                    showErrors = false;
                if (showErrors && String.Equals(args[i], "/V", StringComparison.InvariantCultureIgnoreCase))
                    showFeedback = true;
                if (String.Equals(args[i], "/T", StringComparison.InvariantCultureIgnoreCase))
                    tilesets = true;
                if (String.Equals(args[i], "/R", StringComparison.InvariantCultureIgnoreCase))
                    noCompress = true;
                if (String.Equals(args[i], "/P", StringComparison.InvariantCultureIgnoreCase) && paletteFile == null)
                    readPaletteFile = true;
            }
            if (!hasconsole)
            {
                showErrors = false;
                showFeedback = false;
            }

            if (args.Length - i < 2)
            {
                if (showErrors)
                    Console.WriteLine("Insufficient parameters!");
            }
            String input = args[i];
            String output = args[i+1];
            String inputFull = GeneralUtils.GetAbsolutePath(input);
            String outputFull = GeneralUtils.GetAbsolutePath(output);
            String feedback = String.Format("Converting file \"{0}\" to \"{1}\"... ", Path.GetFileName(inputFull), Path.GetFileName(outputFull));
            if (showFeedback)
                Console.Write(feedback);
            try
            {
                if (tilesets)
                    return ConvertTilesets(inputFull, outputFull, paletteFile, showErrors, showFeedback);
                if (!File.Exists(inputFull))
                {
                    if (showErrors)
                        Console.WriteLine("Error: file not found \"{0}\"", inputFull);
                    return 1;
                }
                // Special case. Processing ends here, except for error handling

                SupportedFileType[] possibleTypesInput = FileDialogGenerator.IdentifyByExtension<SupportedFileType>(SupportedFileType.AutoDetectTypes, inputFull);
                SupportedFileType[] possibleTypesOutput = FileDialogGenerator.IdentifyByExtension<SupportedFileType>(SupportedFileType.SupportedSaveTypes, outputFull);

                List<FileTypeLoadException> loadErrors;
                SupportedFileType inputImage = SupportedFileType.LoadFileAutodetect(inputFull, possibleTypesInput, out loadErrors, false);

                if (inputImage == null)
                {
                    if (showFeedback)
                    {
                        Console.WriteLine("Failed: could not load image.");
                        foreach (FileTypeLoadException error in loadErrors)
                            Console.WriteLine(error.Message);
                    }
                    return 1;
                }
                if (possibleTypesOutput.Length == 0)
                {
                    if (showFeedback)
                        Console.WriteLine("Failed: could not determine output type.");
                    return 1;
                }
                SupportedFileType outputType = possibleTypesOutput[0];
                if (possibleTypesOutput.Length > 1 && showFeedback)
                {
                    Console.WriteLine("Warning: multiple output types possible: " + String.Join(", ", possibleTypesOutput.Select(x => x.ShortTypeName).ToArray()));
                    Console.WriteLine("Selecting first one: " + outputType.ShortTypeName);
                }
                try
                {
                    // Check if image needs a palette.
                    Color[] cols = inputImage.GetColors();
                    if (inputImage.ColorsInPalette == 0 && (cols != null && cols.Length > 0) && paletteFile != null && File.Exists(paletteFile))
                    {
                        Color[] palette = ReadPalette(paletteFile, showErrors, showFeedback);
                        if (palette != null)
                        {
                            try
                            {
                                inputImage.SetColors(palette);
                            }
                            catch (Exception ex)
                            {
                                if (showFeedback)
                                    Console.WriteLine("Failed to set palette: {0}", ex.Message);
                            }
                        }
                    }
                    if (inputImage is FileTilesWwCc1N64Bpp4)
                    {
                        ((FileTilesWwCc1N64Bpp4)inputImage).ConvertToTiles(Path.GetDirectoryName(output), Path.GetFileNameWithoutExtension(output), outputType);
                    }
                    else
                        outputType.SaveAsThis(inputImage, outputFull, new SaveOption[0]);
                }
                catch (NotSupportedException ex)
                {
                    if (showFeedback)
                    {
                        Boolean hasMessage = !String.IsNullOrEmpty(ex.Message);
                        Console.WriteLine("Cannot save type " + inputImage.ShortTypeName + " as type " + outputType.ShortTypeName + (hasMessage ? ":" : "."));
                        if (hasMessage)
                            Console.WriteLine(ex.Message);
                    }
                    return 1;
                }
            }
            catch (Exception e)
            {
                if (showErrors)
                {
                    if (!showFeedback)
                        Console.Write(feedback);
                    Console.WriteLine("Error: " + e.Message);
                }
                return 1;
            }
            return 0;
        }

        public static Color[] ReadPalette(String paletteName, Boolean showErrors, Boolean showFeedback)
        {
            if (!File.Exists(paletteName))
            {
                if (showErrors)
                    Console.WriteLine("Palette file \"{0}\" not found!", paletteName);
                return null;
            }
            if (showFeedback)
                Console.WriteLine("Loading palette \"{0}\"", Path.GetFileName(paletteName));
            SupportedFileType palette = null;
            SupportedFileType[] possibleTypesPal = FileDialogGenerator.IdentifyByExtension<SupportedFileType>(SupportedFileType.AutoDetectTypes, paletteName);
            List<FileTypeLoadException> loadErrors;
            palette = SupportedFileType.LoadFileAutodetect(paletteName, possibleTypesPal, out loadErrors, false);
            if (palette == null && showErrors)
            {
                Console.WriteLine("Failed: could not load palette.");
                foreach (FileTypeLoadException error in loadErrors)
                    Console.WriteLine(error.Message);
            }
            Color[] colorPal = palette.GetColors();
            if (colorPal.Length == 0)
            {
                if (showErrors)
                    Console.WriteLine("Given file does not contain a colour palette!");
                colorPal = null;
            }
            return colorPal;

        }

        public static Int32 ConvertTilesets(String inputFull, String outputFull, String paletteFile, Boolean showErrors, Boolean showFeedback)
        {
            Boolean optimize = outputFull.EndsWith(".DA4", StringComparison.InvariantCultureIgnoreCase);
            if (!optimize && !outputFull.EndsWith(".DA8", StringComparison.InvariantCultureIgnoreCase))
            {
                if (showErrors)
                    Console.WriteLine("Cannot determine output type!");
                return 1;
            }
            String outputPath = Path.GetDirectoryName(outputFull);
            String outputFilename = Path.GetFileNameWithoutExtension(outputFull);

            Int32 filenameStart = inputFull.LastIndexOf(Path.DirectorySeparatorChar);
            String dirPart = inputFull.Substring(0, filenameStart + 1);
            String filePart = inputFull.Substring(filenameStart + 1);
            List<FileInfo> files = new DirectoryInfo(Path.GetDirectoryName(dirPart)).GetFiles(filePart).ToList();
            files = files.OrderBy(f=>f.Name).ToList();
            // Sort names, or just handle them by tile ID?
            Color[] palette;
            Boolean paletteNeeded = false;
            SupportedFileType[] readFiles = new SupportedFileType[0xFF];
            // 0: nothing needed. 1: Needs palette loaded. 2: Needs high colour to 8-bit conversion.
            Byte[] needsPal = new Byte[0xFF];
            for (Int32 id = 0; id < 0xFF; id++)
            {
                if (!MapConversion.TILEINFO.ContainsKey(id))
                {
                    needsPal[id] = 0;
                    continue;
                }
                TileInfo tile = MapConversion.TILEINFO[id];
                foreach (FileInfo file in files)
                {
                    if (!Path.GetFileNameWithoutExtension(file.Name).Equals(tile.TileName, StringComparison.InvariantCultureIgnoreCase) || readFiles[id] != null)
                        continue;
                    SupportedFileType[] possibleTypesTile = FileDialogGenerator.IdentifyByExtension<SupportedFileType>(SupportedFileType.AutoDetectTypes, file.Name);
                    List<FileTypeLoadException> loadErrors;
                    SupportedFileType tileFile = SupportedFileType.LoadFileAutodetect(file.FullName, possibleTypesTile, out loadErrors, false);
                    if (tileFile == null || tileFile.BitsPerPixel == 0 || tileFile.Width % 24 != 0 || tileFile.Height % 24 != 0)
                        continue;
                    readFiles[id] = tileFile;
                    if (tileFile.ColorsInPalette > 0 && tileFile.BitsPerPixel == 8)
                        needsPal[id] = 0;
                    else
                    {
                        paletteNeeded = true;
                        needsPal[id] = (Byte)(tileFile.GetColors().Length > 0 ? 1 : 2);
                    }
                }
            }
            // Ensure colours are all 8-bit
            if (paletteNeeded)
            {
                palette = ReadPalette(paletteFile, showErrors, showFeedback);
                if (palette == null)
                    return 1;
                for (Int32 i = 0; i < 0xFF; i++)
                {
                    SupportedFileType sf = readFiles[i];
                    if (sf == null || needsPal[i] == 0)
                        continue;
                    if (needsPal[i] == 2)
                    {
                        Bitmap img = ImageUtils.ConvertToPalette(sf.GetBitmap(), 8, palette);
                        String name = sf.LoadedFileName;
                        FileImage newFile = new FileImage();
                        newFile.LoadFile(img, name);
                        readFiles[i] = newFile;
                    }
                    else
                    {
                        try
                        {
                            sf.SetColors(palette);
                        }
                        catch (Exception ex)
                        {
                            if (showFeedback)
                                Console.WriteLine(sf.LoadedFileName +  ": Failed to set palette: {0}", ex.Message);
                            return 1;
                        }
                    }
                }
            }
            // Palette set.
            //FileTilesetPC readFiles = new SupportedFileType[0xFF];

            return 0;
        }
#endif
    }
}
