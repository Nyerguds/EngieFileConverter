using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace Nyerguds.Util.UI
{
    /// <summary>
    /// Static class for the main static functions. the type can be derived from the out param, so this makes it unnecessary to put it in the function call.
    /// </summary>
    public static class FileDialogGenerator
    {

        /// <summary>
        /// generates a file open dialog with automatically generated types list. Returns the chosen filename, or null if user cancelled.
        /// The output parameter "selectedItem" will contain a (blank) object of the chosen type, or null if "all files" or "all supported types" was selected.
        /// </summary>
        /// <typeparam name="T">The basic type of which subtypes populate the typesList. Needs to inherit from FileTypeBroadcaster.</typeparam>
        /// <param name="owner">Owner window for the dialog.</param>
        /// <param name="title">Title of the dialog.</param>
        /// <param name="typesList">List of types to show.</param>
        /// <param name="currentPath">Path to open.</param>
        /// <param name="generaltypedesc">General description of the type, to be used in "All supported ???". Defaults to "files" if left blank.</param>
        /// <param name="generaltypeExt">Specific extension to always be supported. Can be left blank for none.</param>
        /// <param name="selectedItem">Returns a (blank) object of the chosen type, or null if "all files" or "all supported types" was selected. Can be used for loading in the file's data.</param>
        /// <returns>The chosen filename, or null if the user cancelled.</returns>
        public static String ShowOpenFileFialog<T>(IWin32Window owner, String title, Type[] typesList, String currentPath, String generaltypedesc, String generaltypeExt, out T selectedItem) where T : IFileTypeBroadcaster
        {
            selectedItem = default(T);
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Multiselect = false;
            FileDialogItem<T>[] items = typesList.Select(x => new FileDialogItem<T>(x)).ToArray();
            T[] correspondingObjects;
            if (title != null)
                ofd.Title = title;
            ofd.Filter = GetFileFilterForOpen(items, generaltypedesc, generaltypeExt, out correspondingObjects);
            ofd.InitialDirectory = Path.GetFullPath(String.IsNullOrEmpty(currentPath) ? "." : currentPath);
            //ofd.FilterIndex
            DialogResult res = ofd.ShowDialog(owner);
            if (res != DialogResult.OK)
                return null;
            selectedItem = correspondingObjects[ofd.FilterIndex - 1];
            return ofd.FileName;
        }

        /// <summary>
        /// generates a file save dialog with automatically generated types list. Returns the chosen filename, or null if user cancelled.
        /// The output parameter "selectedItem" will contain a (blank) object of the chosen type that can be used to determine how to save the data.
        /// </summary>
        /// <typeparam name="T">The basic type of which subtypes populate the typesList. Needs to inherit from FileTypeBroadcaster.</typeparam>
        /// <param name="owner">Owner window for the dialog.</param>
        /// <param name="selectType">Type to select in the dropdown as default.</param>
        /// <param name="typesList">List of class types that inherit from T.</param>
        /// <param name="skipOtherExtensions">True to only use the first extension for each item in the list.</param>
        /// <param name="joinExtensions">True to combine all extensions into one filter.</param>
        /// <param name="currentPath">Path and filename to set as default in the save dialog.</param>
        /// <param name="selectedItem">Returns a (blank) object of the chosen type, or null if "all files" or "all supported types" was selected. Can be used for loading in the file's data.</param>
        /// <returns>The chosen filename, or null if the user cancelled.</returns>
        public static String ShowSaveFileFialog<T>(IWin32Window owner, Type selectType, Type[] typesList, Boolean skipOtherExtensions, Boolean joinExtensions, String currentPath, out T selectedItem) where T : IFileTypeBroadcaster
        {
            selectedItem = default(T);
            SaveFileDialog sfd = new SaveFileDialog();
            FileDialogItem<T>[] items = typesList.Select(x => new FileDialogItem<T>(x)).ToArray();
            Int32 filterIndex = 0;
            Boolean typeFound = false;
            if (selectType != null)
            {
                Int32 nrOfItems = items.Length;
                for (filterIndex = 0; filterIndex < nrOfItems; ++filterIndex)
                {
                    if (selectType != items[filterIndex].ItemType)
                        continue;
                    typeFound = true;
                    break;
                }
            }
            if (typeFound)
                filterIndex++;
            else
            {
                T specificType = FindMoreSpecificItem<T>(typesList, currentPath, selectType, out filterIndex);
                if (specificType != null && !specificType.Equals(default(T)))
                {
                    typeFound = true;
                    filterIndex++;
                }
                // detect by loaded file extension
            }
            if (!typeFound)
                filterIndex = 1;
            T[] correspondingObjects;
            sfd.Filter = GetFileFilterForSave(items, skipOtherExtensions, joinExtensions, out correspondingObjects);
            sfd.FilterIndex = filterIndex;
            //sfd.Filter = "Westwood font file (*.fnt)|*.fnt";
            sfd.InitialDirectory = Path.GetFullPath(String.IsNullOrEmpty(currentPath) ? "." : Path.GetDirectoryName(currentPath));
            if (!String.IsNullOrEmpty(currentPath))
            {
                String fn = Path.GetFileName(currentPath);
                String ext = Path.GetExtension(fn).TrimStart('.');
                T selectedType = correspondingObjects[filterIndex-1];
                if (selectedType != null && !selectedType.Equals(default(T)) && selectedType.FileExtensions.Length > 0)
                {
                    // makes sure the extension's case matches the one in the filter, so the dialog doesn't add an additional one.
                    Int32 extIndex = Array.FindIndex(selectedType.FileExtensions, x => x.Equals(ext, StringComparison.OrdinalIgnoreCase));
                    ext = selectedType.FileExtensions[extIndex == -1 ? 0 : extIndex];
                    fn = Path.GetFileNameWithoutExtension(currentPath) + "." + ext;
                }
                sfd.FileName = fn;
            }
            DialogResult res = sfd.ShowDialog(owner);
            if (res != DialogResult.OK)
                return null;
            selectedItem = correspondingObjects[sfd.FilterIndex - 1];
            return sfd.FileName;
        }


        private static T FindMoreSpecificItem<T>(Type[] moreSpecificTypesList, String currentPath, Type currentType, out Int32 indexInList) where T : IFileTypeBroadcaster
        {
            indexInList = 0;
            if (currentPath == null)
                return default(T);;
            FileDialogItem<T>[] items = moreSpecificTypesList.Select(x => new FileDialogItem<T>(x)).ToArray();
            String ext = Path.GetExtension(currentPath).TrimStart('.');
            T[] specificTypes = IdentifyByExtension<T>(moreSpecificTypesList, currentPath);
            Int32 nrOfSpecificTypes = specificTypes.Length;
            T specificType = default(T);
            Boolean typeFound = false;
            if (nrOfSpecificTypes > 0)
            {
                for (Int32 i = 0; i < nrOfSpecificTypes; i++)
                {
                    T obj = specificTypes[i];
                    if ((currentType != null && !currentType.IsInstanceOfType(obj)) || !obj.FileExtensions.Contains(ext, StringComparer.InvariantCultureIgnoreCase))
                        continue;
                    specificType = obj;
                    break;
                }
                if (specificType != null && !specificType.Equals(default(T)))
                {
                    for (indexInList = 0; indexInList < items.Length; ++indexInList)
                    {
                        if (specificType.GetType() != items[indexInList].ItemType)
                            continue;
                        typeFound = true;
                        break;
                    }
                }
            }
            if (!typeFound)
                indexInList = 0;
            return specificType;
        }

        private static String GetFileFilterForSave<T>(FileDialogItem<T>[] fileDialogItems, Boolean skipOtherExtensions, Boolean joinExtensions, out T[] correspondingObjects) where T : IFileTypeBroadcaster
        {
            List<String> types = new List<String>();
            List<T> objects = new List<T>();
            Int32 itemsLen = fileDialogItems.Length;
            for (Int32 i = 0; i < itemsLen; ++i)
            {
                FileDialogItem<T> itemType = fileDialogItems[i];
                String[] extensions = itemType.Extensions;
                String[] filters = itemType.Filters;
                String[] descriptions = itemType.DescriptionsForExtensions;
                if (!skipOtherExtensions && joinExtensions)
                {
                    List<String> curTypes = new List<String>();
                    foreach (String filter in itemType.Filters.Distinct())
                        curTypes.Add(filter);
                    types.Add(String.Format("{0} ({1})|{1}", itemType.Description, String.Join(";", curTypes.ToArray())));
                    objects.Add(itemType.ItemObject);
                    continue;
                }
                Int32 extLength = extensions.Length;
                for (Int32 j = 0; j < extLength; ++j)
                {
                    String descr = skipOtherExtensions ? itemType.Description : descriptions[j];
                    types.Add(String.Format("{0} ({1})|{1}", descr, filters[j]));
                    T obj = itemType.ItemObject;
                    objects.Add(obj);
                    if (skipOtherExtensions)
                        break;
                }
            }
            correspondingObjects = objects.ToArray();
            return String.Join("|", types.ToArray());
        }

        private static String GetFileFilterForOpen<T>(FileDialogItem<T>[] fileDialogItems, String generaltypedesc, String generaltypeExt, out T[] correspondingObjects) where T : IFileTypeBroadcaster
        {
            Boolean singleItem = fileDialogItems.Length == 1;
            List<String> types = new List<String>();
            List<T> objects = new List<T>();
            HashSet<String> allTypes = singleItem ? null : new HashSet<String>();
            if (!singleItem)
            {
                types.Add(String.Empty); // to be replaced later
                objects.Add(default(T));
            }
            Int32 fdItemsLength = fileDialogItems.Length;
            for (Int32 i = 0; i < fdItemsLength; ++i)
            {
                FileDialogItem<T> itemType = fileDialogItems[i];
                List<String> curTypes = new List<String>();
                foreach (String filter in itemType.Filters.Distinct())
                {
                    curTypes.Add(filter);
                    if (!singleItem)
                        allTypes.Add(filter);
                }
                types.Add(String.Format("{0} ({1})|{1}", itemType.Description, String.Join(";", curTypes.ToArray())));
                objects.Add(itemType.ItemObject);
            }
            if (String.IsNullOrEmpty(generaltypedesc))
                generaltypedesc = "files";
            if (!singleItem)
            {
                if (!String.IsNullOrEmpty(generaltypeExt))
                    allTypes.Add("*." + generaltypeExt);
                String allTypesStr = String.Join(";", allTypes.ToArray());
                types[0] = "All supported " + generaltypedesc + " (" + allTypesStr + ")|" + allTypesStr;
            }
            types.Add("All files (*.*)|*.*");
            objects.Add(default(T));
            correspondingObjects = objects.ToArray();
            return String.Join("|", types.ToArray());
        }

        public static T[] IdentifyByExtension<T>(T[] typesList, String receivedPath) where T : IFileTypeBroadcaster
        {
            FileDialogItem<T>[] items = typesList.Select(x => new FileDialogItem<T>(x)).ToArray();
            return IdentifyByExtension(items, receivedPath);
        }

        public static T[] IdentifyByExtension<T>(Type[] typesList, String receivedPath) where T : IFileTypeBroadcaster
        {
            FileDialogItem<T>[] items = typesList.Select(x => new FileDialogItem<T>(x)).ToArray();
            return IdentifyByExtension(items, receivedPath);
        }

        public static T[] IdentifyByExtension<T>(FileDialogItem<T>[] items, String receivedPath) where T : IFileTypeBroadcaster
        {
            Int32 nrOfItems = items.Length;
            List<T> possibleMatches = new List<T>();
            String ext = Path.GetExtension(receivedPath).TrimStart('.');
            // prefer those on which it is the primary type
            // Try only the single-extension types
            for (Int32 i = 0; i < nrOfItems; ++i)
            {
                FileDialogItem<T> item = items[i];
                if (item.Extensions.Length == 1 && item.Extensions[0].Equals(ext, StringComparison.InvariantCultureIgnoreCase))
                    possibleMatches.Add(item.ItemObject);
            }
            // Try primary extension of each joint type
            for (Int32 i = 0; i < nrOfItems; ++i)
            {
                FileDialogItem<T> item = items[i];
                if (item.Extensions.Length > 1 && item.Extensions[0].Equals(ext, StringComparison.InvariantCultureIgnoreCase))
                    possibleMatches.Add(item.ItemObject);
            }
            // final fallback: sub-types of joint type
            for (Int32 i = 0; i < nrOfItems; ++i)
            {
                FileDialogItem<T> item = items[i];
                if (item.Extensions.Length > 1 && item.Extensions.Skip(1).Contains(ext, StringComparer.InvariantCultureIgnoreCase))
                    possibleMatches.Add(item.ItemObject);
            }
            return possibleMatches.ToArray();
        }

        public static T[] GetItemsList<T>(Type[] typesList) where T : IFileTypeBroadcaster
        {
            Int32 typesListLength = typesList.Length;
            T[] items = new T[typesListLength];
            for (Int32 i = 0; i < typesListLength; i++)
                items[i] = (T)Activator.CreateInstance(typesList[i]);
            return items;
        }

    }

    public class FileDialogItem<T> where T : IFileTypeBroadcaster
    {
        public String[] Extensions { get; private set; }
        public String[] DescriptionsForExtensions { get; private set; }
        public String[] Filters { get { return this.Extensions.Select(x => "*." + x).ToArray(); } }
        public String Description { get; private set; }
        public String FullDescription
        {
            get { return String.Format("{0} (*.{1})", this.Description, this.Extensions); }
        }

        /// <summary>Returns a newly created instance of this type.</summary>
        public T ItemObject { get { return itemObjectSet ? itemObject: (T)Activator.CreateInstance(this.ItemType); } }

        private T itemObject;
        private Boolean itemObjectSet;

        public Type ItemType { get; private set; }

        public FileDialogItem(Type itemtype)
        {
            if (!itemtype.IsSubclassOf(typeof(T)))
                throw new ArgumentException("Entries in autoDetectTypes list must all be " + typeof(T).Name + " classes!", "itemtype");
            this.ItemType = itemtype;
            T item = this.ItemObject;
            if (item.FileExtensions.Length != item.DescriptionsForExtensions.Length)
                throw new ArgumentException("Entry " + this.ItemObject.GetType().Name + " does not have equal amount of extensions and descriptions!", "itemtype");
            this.Description = item.ShortTypeDescription;
            this.Extensions = item.FileExtensions;
            this.DescriptionsForExtensions = item.DescriptionsForExtensions;
        }
        
        public FileDialogItem(T item)
        {
            if (item == null)
                throw new ArgumentNullException("item");
            this.ItemType = item.GetType();
            this.itemObject = item;
            this.itemObjectSet = true;
            if (item.FileExtensions.Length != item.DescriptionsForExtensions.Length)
                throw new ArgumentException("Entry " + this.ItemObject.GetType().Name + " does not have equal amount of extensions and descriptions!", "itemtype");
            this.Description = item.ShortTypeDescription;
            this.Extensions = item.FileExtensions;
            this.DescriptionsForExtensions = item.DescriptionsForExtensions;
        }

        public override String ToString()
        {
            return this.ItemObject.ShortTypeDescription;
        }
    }
}
