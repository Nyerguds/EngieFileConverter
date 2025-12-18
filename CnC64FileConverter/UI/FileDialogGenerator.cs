using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
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
        /// <param name="owner">Owner window for the dialog</param>
        /// <param name="typesList">List of types to show.</param>
        /// <param name="specificTypesList">More specific list of types that have grouped items from the first list as specific items.</param>
        /// <param name="currentPath">Path to open. Can contain a filename, but only the path is used.</param>
        /// <param name="generaltypedesc">General description of the type, to be used in "All supported ???". Defaults to "files" if left blank.</param>
        /// <param name="generaltypeExt">Specific extension to always be supported. Can be left blank. for none</param>
        /// <param name="selectedItem">Returns a (blank) object of the chosen type, or null if "all files" or "all supported types" was selected. Can be used for loading in the file's data.</param>
        /// <returns>The chosen filename, or null if the user cancelled.</returns>
        public static String ShowOpenFileFialog<T>(IWin32Window owner, Type[] typesList, Type[] specificTypesList, String currentPath, String generaltypedesc, String generaltypeExt, out T selectedItem) where T : FileTypeBroadcaster
        {
            selectedItem = default(T);
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Multiselect = false;
            FileDialogItem<T>[] items = typesList.Select(x => new FileDialogItem<T>(x)).ToArray();
            T[] correspondingObjects;
            ofd.Filter = GetFileFilterForOpen<T>(items, generaltypedesc, generaltypeExt, out correspondingObjects);
            ofd.InitialDirectory = String.IsNullOrEmpty(currentPath) ? Path.GetFullPath(".") : Path.GetDirectoryName(currentPath);
            //ofd.FilterIndex
            DialogResult res = ofd.ShowDialog(owner);
            if (res != System.Windows.Forms.DialogResult.OK)
                return null;
            selectedItem = correspondingObjects[ofd.FilterIndex - 1];
            if (specificTypesList != null)
            {
                Int32 filterIndex;
                T specificType = FindMoreSpecificItem<T>(specificTypesList, ofd.FileName, selectedItem == null ? null : selectedItem.GetType(), out filterIndex);
                if (specificType != null && !specificType.Equals(default(T)))
                {
                    FileDialogItem<T>[] specificItems = specificTypesList.Select(x => new FileDialogItem<T>(x)).ToArray();
                    selectedItem = specificItems[filterIndex].ItemObject;
                }
            }
            return ofd.FileName;
        }

        /// <summary>
        /// generates a file save dialog with automatically generated types list. Returns the chosen filename, or null if user cancelled.
        /// The output parameter "selectedItem" will contain a (blank) object of the chosen type that can be used to determine how to save the data.
        /// </summary>
        /// <typeparam name="T">The basic type of which subtypes populate the typesList. Needs to inherit from FileTypeBroadcaster.</typeparam>
        /// <param name="owner">Owner window for the dialog</param>
        /// <param name="selectType">Type to select in the dropdown as default.</param>
        /// <param name="typesList">List of class types that inherit from T.</param>
        /// <param name="currentPath">Path and filename to set as default in the save dialog.</param>
        /// <param name="generaltypedesc">General description of the type, to be used in "All supported ???". Defaults to "files" if left blank.</param>
        /// <param name="generaltypeExt">Specific extension to always be supported. Can be left blank. for none</param>
        /// <param name="selectedItem">Returns a (blank) object of the chosen type, or null if "all files" or "all supported types" was selected. Can be used for loading in the file's data.</param>
        /// <returns>The chosen filename, or null if the user cancelled.</returns>
        public static String ShowSaveFileFialog<T>(IWin32Window owner, Type selectType, Type[] typesList, Boolean skipOtherExtensions, String currentPath, out T selectedItem) where T : FileTypeBroadcaster
        {
            selectedItem = default(T);
            SaveFileDialog sfd = new SaveFileDialog();
            FileDialogItem<T>[] items = typesList.Select(x => new FileDialogItem<T>(x)).ToArray();
            Int32 filterIndex;
            Boolean typeFound = false;
            for (filterIndex = 0; filterIndex < items.Length; filterIndex++)
            {
                if (selectType == items[filterIndex].ItemType)
                {
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
            sfd.Filter = GetFileFilterForSave<T>(items, skipOtherExtensions, out correspondingObjects);
            sfd.FilterIndex = filterIndex;
            //sfd.Filter = "Westwood font file (*.fnt)|*.fnt";
            sfd.InitialDirectory = String.IsNullOrEmpty(currentPath) ? Path.GetFullPath(".") : Path.GetDirectoryName(currentPath);
            if (!String.IsNullOrEmpty(currentPath))
            {
                String fn = Path.GetFileName(currentPath);
                String ext = Path.GetExtension(fn).TrimStart('.');
                T selectedType = correspondingObjects[filterIndex-1];
                if (selectedType != null && !selectedType.Equals(default(T))
                    && selectedType.FileExtensions.Length > 0 && !ext.Equals(selectedType.FileExtensions[0]))
                {
                    fn = Path.GetFileNameWithoutExtension(currentPath) + "." + selectedType.FileExtensions[0];
                }
                sfd.FileName = fn;
            }
            DialogResult res = sfd.ShowDialog(owner);
            if (res != System.Windows.Forms.DialogResult.OK)
                return null;
            selectedItem = correspondingObjects[sfd.FilterIndex - 1];
            return sfd.FileName;
        }


        private static T FindMoreSpecificItem<T>(Type[] moreSpecificTypesList, String currentPath, Type currentType, out Int32 indexInList) where T : FileTypeBroadcaster
        {
            FileDialogItem<T>[] items = moreSpecificTypesList.Select(x => new FileDialogItem<T>(x)).ToArray();
            String ext = Path.GetExtension(currentPath).TrimStart('.');
            T[] specificTypes = IdentifyByExtension<T>(moreSpecificTypesList, currentPath);
            T specificType = default(T);
            Boolean typeFound = false;
            indexInList = 0;
            if (specificTypes.Length > 0)
            {
                foreach (T obj in specificTypes)
                {
                    if ((currentType == null || currentType.IsAssignableFrom(obj.GetType())) && obj.FileExtensions.Contains(ext))
                    {
                        specificType = obj;
                        break;
                    }
                }
                if (specificType != null && !specificType.Equals(default(T)))
                {
                    for (indexInList = 0; indexInList < items.Length; indexInList++)
                    {
                        if (specificType.GetType() == items[indexInList].ItemType)
                        {
                            typeFound = true;
                            break;
                        }
                    }
                }
            }
            if (!typeFound)
                indexInList = 0;
            return specificType;
        }

        private static String GetFileFilterForSave<T>(FileDialogItem<T>[] fileDialogItems, Boolean skipOtherExtensions, out T[] correspondingObjects) where T : FileTypeBroadcaster
        {
            List<String> types = new List<String>();
            List<T> objects = new List<T>();
            foreach (FileDialogItem<T> itemType in fileDialogItems)
            {
                String[] extensions = itemType.Extensions;
                String[] filters = itemType.Filters;
                String[] descriptions = itemType.DescriptionsForExtensions;
                for (Int32 i = 0; i < extensions.Length; i++)
                {
                    String descr = skipOtherExtensions ? itemType.Description : descriptions[i];
                    types.Add(String.Format("{0} ({1})|{1}", descr, filters[i]));
                    T obj = itemType.ItemObject;
                    objects.Add(obj);
                    if (skipOtherExtensions)
                        break;
                }
            }
            correspondingObjects = objects.ToArray();
            return String.Join("|", types.ToArray());
        }

        private static String GetFileFilterForOpen<T>(FileDialogItem<T>[] fileDialogItems, String generaltypedesc, String generaltypeExt, out T[] correspondingObjects) where T : FileTypeBroadcaster
        {
            List<String> types = new List<String>();
            List<T> objects = new List<T>();
            HashSet<String> allTypes = new HashSet<String>();
            types.Add(String.Empty); // to be replaced later
            objects.Add(default(T));
            foreach (FileDialogItem<T> itemType in fileDialogItems)
            {
                HashSet<String> curTypes = new HashSet<String>();
                foreach (String filter in itemType.Filters)
                {
                    curTypes.Add(filter);
                    allTypes.Add(filter);
                }
                types.Add(String.Format("{0} ({1})|{1}", itemType.Description, String.Join(";", curTypes.ToArray())));
                objects.Add(itemType.ItemObject);
            }
            if (String.IsNullOrEmpty(generaltypedesc))
                generaltypedesc = "files";
            if (!String.IsNullOrEmpty(generaltypeExt))
                allTypes.Add("*." + generaltypeExt);
            String allTypesStr = String.Join(";", allTypes.ToArray());
            types[0] = "All supported " + generaltypedesc + " (" + allTypesStr + ")|" + allTypesStr;
            types.Add("All files (*.*)|*.*");
            objects.Add(default(T));
            correspondingObjects = objects.ToArray();
            return String.Join("|", types.ToArray());
        }

        public static T[] IdentifyByExtension<T>(Type[] typesList, String receivedPath) where T : FileTypeBroadcaster
        {
            List<T> possibleMatches = new List<T>();
            String filename = receivedPath;
            String ext = Path.GetExtension(receivedPath).TrimStart('.');
            FileDialogItem<T>[] items = typesList.Select(x => new FileDialogItem<T>(x)).ToArray();
            // prefer those on which it is the primary type
            // Try only the single-extension types
            foreach (FileDialogItem<T> item in items)
                if (item.Extensions.Length == 1 && item.Extensions[0].Equals(ext, StringComparison.InvariantCultureIgnoreCase))
                    possibleMatches.Add(item.ItemObject);
            // Try primary extension of each joint type
            foreach (FileDialogItem<T> item in items)
                if (item.Extensions.Length > 1 && item.Extensions[0].Equals(ext, StringComparison.InvariantCultureIgnoreCase))
                    possibleMatches.Add(item.ItemObject);
            // final fallback: sub-types of joint type
            foreach (FileDialogItem<T> item in items)
                if (item.Extensions.Length > 1 && item.Extensions.Skip(1).Contains(ext, StringComparer.InvariantCultureIgnoreCase))
                    possibleMatches.Add(item.ItemObject);
            return possibleMatches.ToArray();
        }

    }

    public class FileDialogItem<T> where T : FileTypeBroadcaster
    {
        public String[] Extensions { get; private set; }
        public String[] DescriptionsForExtensions { get; private set; }
        public String[] Filters { get { return Extensions.Select(x => "*." + x).ToArray(); } }
        public String Description { get; private set; }
        public String FullDescription
        {
            get { return String.Format("{0} (*.{1})", this.Description, this.Extensions); }
        }

        /// <summary>Returns a newly created instance of this type.</summary>
        public T ItemObject { get { return (T)Activator.CreateInstance(ItemType); } }
        public Type ItemType { get; private set; }

        public FileDialogItem(Type itemtype)
        {
            if (!itemtype.IsSubclassOf(typeof(T)))
                throw new ArgumentException("Entries in autoDetectTypes list must all be " + typeof(T).Name + " classes!", "itemtype");
            ItemType = itemtype;
            T item = ItemObject;
            if (item.FileExtensions.Length != item.DescriptionsForExtensions.Length)
                throw new ArgumentException("Entry " + ItemObject.GetType().Name + " does not have equal amount of extensions and descriptions!", "itemtype");
            this.Description = item.ShortTypeDescription;
            this.Extensions = item.FileExtensions;
            this.DescriptionsForExtensions = item.DescriptionsForExtensions;
        }

        public override String ToString()
        {
            return ItemObject.ShortTypeDescription;
        }
    }
}
