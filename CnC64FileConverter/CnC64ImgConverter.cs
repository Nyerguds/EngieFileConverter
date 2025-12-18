using CnC64FileConverter.Domain;
using CnC64FileConverter.Domain.ImageFile;
using CnC64FileConverter.Domain.Utils;
using CnC64FileConverter.UI;
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

namespace CnC64FileConverter
{
    public static class CnC64ImgConverter
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        public static Int32 Run(String[] args)
        {
            //if (AttachConsole(-1))
            //    Console.ReadKey();
            if (args.Length > 1)
                return ConvertImage(args);
            
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new FrmCnC64FileConverter(args));
            return 0;
        }

        [DllImport("Kernel32.dll")]
        public static extern Boolean AttachConsole(int processId);


        private static Int32 ConvertImage(String[] args)
        {            
            Boolean hasconsole = AttachConsole(-1);
            Boolean showErrors = true;
            Boolean showFeedback = false;
            //Boolean forHeightMap = false;
            
            Int32 i;
            for(i = 0; i < args.Length; i++)
            {
                if (!args[i].StartsWith("/"))
                    break;
                if (String.Equals(args[i], "/Q", StringComparison.InvariantCultureIgnoreCase))
                    showErrors = false;
                if (showErrors && String.Equals(args[i], "/V", StringComparison.InvariantCultureIgnoreCase))
                    showFeedback = true;
                //if (showErrors && String.Equals(args[i], "/H", StringComparison.InvariantCultureIgnoreCase))
                //    forHeightMap = true;
                
            }
            if (args.Length - i < 2)
            {
                if (hasconsole && showErrors)
                    Console.WriteLine("Insufficient parameters!");
            }
            String input = args[i];
            String output = args[i+1];
            String inputFull = GeneralUtils.GetAbsolutePath(input, null);
            String outputFull = GeneralUtils.GetAbsolutePath(output, null);
            String feedback = String.Format("Converting file \"{0}\" to \"{1}\"... ", Path.GetFileName(inputFull), Path.GetFileName(outputFull));
            if (hasconsole && showFeedback)
                Console.Write(feedback);
            if (!File.Exists(inputFull))
            {
                if (hasconsole && showErrors)
                    Console.WriteLine(String.Format("Error: File not found \"{0}\"", inputFull));
                return 1;
            }
            try
            {
                N64FileType[] possibleTypesInput = FileDialogGenerator.IdentifyByExtension<N64FileType>(N64FileType.AutoDetectTypes, inputFull);
                N64FileType[] possibleTypesOutput = FileDialogGenerator.IdentifyByExtension<N64FileType>(N64FileType.SupportedSaveTypes, outputFull);

                String ext = Path.GetExtension(inputFull).ToUpperInvariant().Trim('.');
                List<FileTypeLoadException> loadErrors;
                N64FileType inputImage = N64FileType.LoadImageAutodetect(inputFull,possibleTypesInput, out loadErrors);
                if (inputImage == null)
                {
                    if (hasconsole && showFeedback)
                    {
                        Console.WriteLine("Failed: could not load image.");
                        foreach(FileTypeLoadException error in loadErrors)
                            Console.WriteLine(error.Message);
                    }
                }
                if (inputImage != null)
                {
                    if (possibleTypesOutput.Length == 0)
                    {
                        if (hasconsole && showFeedback)
                            Console.WriteLine("Failed: could not determine output type.");
                    }
                    else
                    {
                        N64FileType outputType = possibleTypesOutput[0];
                        if (possibleTypesOutput.Length > 1 && hasconsole && showFeedback)
                        {
                            Console.WriteLine("Warning: multiple output types possible: " + String.Join(", ", possibleTypesOutput.Select(x => x.ShortTypeName).ToArray()));
                            Console.WriteLine("Selecting first one: " + outputType.ShortTypeName);
                        }
                        try
                        {
                            outputType.SaveAsThis(inputImage, outputFull);
                        }
                        catch (NotSupportedException)
                        {
                            if (hasconsole && showFeedback)
                                Console.WriteLine("Cannot save type " + inputImage.ShortTypeName + " as type " + outputType.ShortTypeName + ".");
                            return 1;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                if (hasconsole && showErrors)
                {
                    if (!showFeedback)
                        Console.Write(feedback);
                    Console.WriteLine("Error: " + e.Message);
                }
                
                return 1;
            }
            return 0;
        }

    }
}
