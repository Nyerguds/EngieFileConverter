using Nyerguds.Util;
using Nyerguds.Util.UI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace EngieFileConverter.Domain.FileTypes
{
    public static class FileTypesFactory
    {
        private static Type[] m_autoDetectTypes =
        {
            typeof(FileImagePng),
            typeof(FileImageBmp),
            typeof(FileImageGif),
            typeof(FileImageJpg),
            typeof(FileImagePcx),
            typeof(FileImage),
            typeof(FileIcon),
            typeof(FileFramesJazzFontC),
            typeof(FileFramesJazzFont),
            typeof(FileImgWwCps),
            typeof(FileImgWwCpsToon),
            typeof(FileImgWwCmp),
            typeof(FileFramesWwCpsAmi4),
            typeof(FileFramesWwWsa),
            typeof(FileFramesWwShpD2),
            typeof(FileFramesWwShpLol1),
            typeof(FileFramesWwShpCc),
            typeof(FileFramesWwShpTs),
            typeof(FileFramesWwShpBr),
            typeof(FileFramesWwFntV3),
            typeof(FileFramesWwFntV4),
            typeof(FileFramesWwBitFntUni),
            typeof(FileFramesFntD2k),
            typeof(FileImgWwLcw),
            typeof(FileImgWwN64),
            typeof(FileImgWwMhwanh), // Experimental
            typeof(FileMapWwCc1Pc),
            typeof(FileMapWwRa1Pc),
            typeof(FileMapWwCc1N64),
            typeof(FileMapWwCc1PcFromIni),
            typeof(FileMapWwCc1N64FromIni),
            typeof(FileTilesWwCc1N64Bpp4),
            typeof(FileTilesWwCc1N64Bpp8),
            typeof(FileTilesetWwCc1PC),
            typeof(FileTilesetWwRA1),
            typeof(FileFramesDogsDb),
            typeof(FileFramesAdvVga),
            typeof(FileImgKort),
            typeof(FileFramesKortBmp),
            typeof(FileFramesExec),
            typeof(FileImageExec),
            typeof(FileImgDynScr),
            typeof(FileImgDynScrV2),
            typeof(FileFramesDynBmp),
            typeof(FileImgDynBmpMtx),
            typeof(FilePaletteDyn),
            typeof(FileFramesMythosPal),
            typeof(FileFramesMythosVgs),
            typeof(FileFramesMythosVda),
            typeof(FileFramesTrMntPan),
            typeof(FileFramesTrMntPak),
            typeof(FileImgKotB),
            typeof(FileImgMythosLbv),
            typeof(FileFramesDfPic),
            typeof(FileFramesIgcSlb),
            typeof(FileFramesLadyGl),
            typeof(FileImgLadyTme),
            typeof(FileImgIgcGx2),
            typeof(FileImgIgcDmp),
            typeof(FileImgNova),
            typeof(FileImgStris),
            typeof(FileImgBif),
            typeof(FileImgJmx),
            typeof(FilePalette6Bit),
            typeof(FilePalette8Bit),
            typeof(FilePaletteWwCc1N64Pa8),
            //typeof(FileFramesInt33Cursor), // Not gonna autodetect this.
            typeof(FilePaletteWwCc1N64Pa4),
            typeof(FileTblWwPal),
            typeof(FilePaletteWwAmiga),
#if DEBUG
            typeof(FilePaletteWwPsx), // Experimental
#endif
#if false
            typeof(FileImgMythosRmm), // Do not enable; too much chance on false positive.
#endif
            typeof(FileImgHqQuest),
            typeof(FileFramesAdvIco), // Put at the bottom because file size divisible by 0x120 is the only thing identifying this.
        };

        private static readonly Type[] m_supportedOpenTypes =
        {
            typeof(FileImage),
            typeof(FileIcon),
            typeof(FileFramesInt33Cursor),
            typeof(FileImgWwCps),
            typeof(FileImgWwCpsToon),
            typeof(FileImgWwCmp),
            typeof(FileFramesWwShpD2),
            typeof(FileFramesWwShpLol1),
            typeof(FileFramesWwShpCc),
            typeof(FileFramesWwShpTs),
            typeof(FileFramesWwShpBr),
            typeof(FileFramesWwFntV3),
            typeof(FileFramesWwFntV4),
            typeof(FileFramesWwBitFntUni),
            typeof(FileFramesFntD2k),
            typeof(FileFramesWwWsa),
            typeof(FileFramesWwCpsAmi4),
            typeof(FileImgWwLcw),
            typeof(FileImgWwN64),
            typeof(FileImgWwMhwanh),
            typeof(FileMapWwCc1N64),
            typeof(FileMapWwCc1Pc),
            typeof(FileMapWwRa1Pc),
            typeof(FileTilesWwCc1N64Bpp4),
            typeof(FileTilesWwCc1N64Bpp8),
            typeof(FileTilesetWwCc1PC),
            typeof(FileTilesetWwRA1),
            typeof(FilePalette6Bit),
            typeof(FilePalette8Bit),
            typeof(FileImageExec),
            typeof(FileImageExecM),
            typeof(FilePaletteWwCc1N64Pa8),
            typeof(FilePaletteWwCc1N64Pa4),
            typeof(FileTblWwPal),
            typeof(FileFramesJazzFont),
            typeof(FileFramesJazzFontC),
            typeof(FileFramesDogsDb),
            typeof(FileFramesAdvVga),
            typeof(FileFramesAdvIco),
            typeof(FileFramesDynBmp),
            typeof(FileImgDynScr),
            typeof(FileImgDynScrV2),
            typeof(FilePaletteDyn),
            typeof(FileImgKort),
            typeof(FileFramesKortBmp),
            typeof(FileFramesTrMntPan),
            typeof(FileFramesTrMntPak),
            typeof(FileImgKotB),
            typeof(FileFramesMythosPal),
            typeof(FileFramesMythosVgs),
            typeof(FileFramesMythosVda),
            typeof(FilePaletteWwAmiga),
            typeof(FilePaletteWwPsx),
            typeof(FileFramesDfPic),
            typeof(FileFramesLadyGl),
            typeof(FileImgLadyTme),
            typeof(FileFramesIgcSlb),
            typeof(FileImgIgcGx2),
            typeof(FileImgIgcDmp),
            typeof(FileImgNova),
            typeof(FileImgStris),
            typeof(FileImgBif),
            typeof(FileImgJmx),
        };

        private static Type[] m_supportedSaveTypes =
        {
            //typeof(FileImgN64Standard),
            //typeof(FileImgN64Jap),
            //typeof(FileImgN64Gray),
            typeof(FileMapWwCc1N64),
            typeof(FileMapWwCc1Pc),
            typeof(FileImagePng),
            typeof(FileImageBmp),
            typeof(FileImageGif),
            typeof(FileImageJpg),
            typeof(FileIcon),
            typeof(FileFramesInt33Cursor),
            typeof(FilePalette6Bit),
            typeof(FilePalette8Bit),
            typeof(FileImageExec),
            typeof(FileFramesWwShpD2),
            typeof(FileFramesWwShpLol1),
            typeof(FileFramesWwShpCc),
            typeof(FileFramesWwShpTs),
            typeof(FileFramesWwShpBr),
            typeof(FileFramesWwFntV3),
            typeof(FileFramesWwFntV4),
            typeof(FileFramesWwBitFntUni),
            typeof(FileFramesFntD2k),
            typeof(FileFramesWwWsa),
            typeof(FileImgWwCps),
            typeof(FileImgWwCpsToon),
            typeof(FileImgWwCmp),
            typeof(FileFramesWwCpsAmi4),
            typeof(FileTilesetWwCc1PC),
            //typeof(FileTilesetWwRA1),
            typeof(FileFramesDogsDb),
            typeof(FileImgWwLcw),
            typeof(FileImgWwN64),
            typeof(FilePaletteWwCc1N64Pa4),
            typeof(FilePaletteWwCc1N64Pa8),
            typeof(FileTblWwPal),
            typeof(FileFramesJazzFont),
            typeof(FileFramesJazzFontC),
            typeof(FileFramesAdvVga),
            typeof(FileFramesAdvIco),
            typeof(FileImgDynScr),
            typeof(FileImgDynScrV2),
            typeof(FileFramesDynBmp),
            typeof(FileImgDynBmpMtx),
            typeof(FilePaletteDyn),
            typeof(FileImgKort),
            typeof(FileFramesKortBmp),
            typeof(FileFramesMythosVgs),
            typeof(FileFramesMythosVda),
            typeof(FileFramesMythosPal),
            typeof(FileFramesTrMntPan),
            typeof(FileFramesTrMntPak),
            typeof(FileImgKotB),
            typeof(FilePaletteWwAmiga),
            //typeof(FilePaletteWwPsx),
            typeof(FileFramesDfPic),
            typeof(FileImgIgcDmp),
            typeof(FileImgIgcGx2),
            typeof(FileImgNova),
            typeof(FileImgBif),
            typeof(FileImgJmx),
        };

#if DEBUG
        static FileTypesFactory()
        {
            CheckTypes(SupportedOpenTypes);
            CheckTypes(SupportedSaveTypes);
            CheckTypes(AutoDetectTypes);
        }

        private static void CheckTypes(Type[] types)
        {
            // internal check for development.
            Type sft = typeof(SupportedFileType);
            Int32 typesLength = types.Length;
            for (Int32 i = 0; i < typesLength; ++i)
            {
                Type t = types[i];
                if (!t.IsSubclassOf(sft))
                    throw new Exception("Entries in types list must all be SupportedFileType classes.");
            }
        }
#endif
        /// <summary>Lists all types that will appear in the Open File menu.</summary>
        public static Type[] SupportedOpenTypes { get { return ArrayUtils.CloneArray(m_supportedOpenTypes); } }
        /// <summary>Lists all types that can appear in the Save File menu.</summary>
        public static Type[] SupportedSaveTypes { get { return ArrayUtils.CloneArray(m_supportedSaveTypes); } }
        /// <summary>
        /// Lists all types that can be autodetected, in the order they will be detected. Note that as a first step,
        /// all types which contain the requested file's extension are filtered out and checked.
        /// </summary>
        public static Type[] AutoDetectTypes { get { return ArrayUtils.CloneArray(m_autoDetectTypes); } }

        /// <summary>
        /// Autodetects the file type from the given list, and if that fails, from the full autodetect list.
        /// </summary>
        /// <param name="path">File path to load.</param>
        /// <param name="preferredTypes">List of the most likely types it can be.</param>
        /// <param name="loadErrors">Returned list of occurred errors during autodetect.</param>
        /// <param name="onlyGivenTypes">True if only the possibleTypes list is processed to autodetect the type.</param>
        /// <returns>The detected type, or null if detection failed.</returns>
        public static SupportedFileType LoadFileAutodetect(String path, SupportedFileType[] preferredTypes, Boolean onlyGivenTypes, out List<FileTypeLoadException> loadErrors)
        {
            Byte[] fileData = File.ReadAllBytes(path);
            return LoadFileAutodetect(fileData, path, preferredTypes, onlyGivenTypes, out loadErrors);
        }

        /// <summary>
        /// Autodetects the file type from the given list, and if that fails, from the full autodetect list.
        /// </summary>
        /// <param name="fileData">File dat to load file from.</param>
        /// <param name="path">File path, used for extension filtering and file initialisation. Not for reading as bytes; fileData is used for that.</param>
        /// <param name="preferredTypes">List of the most likely types it can be.</param>
        /// <param name="loadErrors">Returned list of occurred errors during autodetect.</param>
        /// <param name="onlyGivenTypes">True if only the possibleTypes list is processed to autodetect the type.</param>
        /// <returns>The detected type, or null if detection failed.</returns>
        public static SupportedFileType LoadFileAutodetect(Byte[] fileData, String path, SupportedFileType[] preferredTypes, Boolean onlyGivenTypes, out List<FileTypeLoadException> loadErrors)
        {
            loadErrors = new List<FileTypeLoadException>();
            // See which extensions match, and try those first.
            if (preferredTypes == null)
                preferredTypes = FileDialogGenerator.IdentifyByExtension<SupportedFileType>(AutoDetectTypes, path);
            else if (onlyGivenTypes)
            {
                // Try extension-filtering first, then the rest.
                SupportedFileType[] preferredTypesExt = FileDialogGenerator.IdentifyByExtension(preferredTypes, path);
                Int32 extLength = preferredTypesExt.Length;
                for (Int32 i = 0; i < extLength; ++i)
                {
                    SupportedFileType typeObj = preferredTypesExt[i];
                    try
                    {
                        typeObj.LoadFile(fileData, path);
                        return typeObj;
                    }
                    catch (FileTypeLoadException e)
                    {
                        e.AttemptedLoadedType = typeObj.ShortTypeName;
                        loadErrors.Add(e);
                    }
                    preferredTypes = preferredTypes.Where(tp => preferredTypesExt.All(tpe => tpe.GetType() != tp.GetType())).ToArray();
                }
            }
            Int32 prefTypesLength = preferredTypes.Length;
            for (Int32 i = 0; i < prefTypesLength; ++i)
            {
                SupportedFileType typeObj = preferredTypes[i];
                try
                {
                    typeObj = (SupportedFileType)Activator.CreateInstance(typeObj.GetType());
                    typeObj.LoadFile(fileData, path);
                    return typeObj;
                }
                catch (FileTypeLoadException e)
                {
                    e.AttemptedLoadedType = typeObj.ShortTypeName;
                    loadErrors.Add(e);
                }
            }
            if (onlyGivenTypes)
                return null;
            Int32 autoTypesLength = AutoDetectTypes.Length;
            for (Int32 i = 0; i < autoTypesLength; ++i)
            {
                Type type = AutoDetectTypes[i];
                // Skip entries on the already-tried list.
                Boolean isPreferredType = false;
                for (Int32 j = 0; j < prefTypesLength; ++j)
                {
                    if (preferredTypes[j].GetType() != type)
                        continue;
                    isPreferredType = true;
                    break;
                }
                if (isPreferredType)
                    continue;
                SupportedFileType objInstance = null;
                try
                {
                    objInstance = (SupportedFileType)Activator.CreateInstance(type);
                }
                catch
                {
                    /* Ignore; programmer error. */
                }
                if (objInstance == null)
                    continue;
                try
                {
                    objInstance.LoadFile(fileData, path);
                    return objInstance;
                }
                catch (FileTypeLoadException e)
                {
                    // objInstance should not be disposed here since it never succeeded in initializing,
                    // and should not contain any loaded images at that point.
                    e.AttemptedLoadedType = objInstance.ShortTypeName;
                    loadErrors.Add(e);
                    // Removes any stored images.
                    objInstance.Dispose();
                }
            }
            return null;
        }

    }
}
