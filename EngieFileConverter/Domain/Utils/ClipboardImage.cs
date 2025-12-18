using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Nyerguds.ImageManipulation;

namespace Nyerguds.Util
{
    public class ClipboardImage
    {
        /// <summary>
        /// Retrieves an image from the given clipboard data object, in the order PNG, DIB, Bitmap, Image object.
        /// </summary>
        /// <param name="retrievedData">The clipboard data.</param>
        /// <returns>The extracted image, or null if no supported image type was found.</returns>
        public static Bitmap GetClipboardImage(DataObject retrievedData)
        {
            Bitmap clipboardimage = null;
            String[] formats = retrievedData.GetFormats();
            if (formats.Length == 0)
                return null;
            // Order: try PNG, move on to try 32-bit ARGB DIB, then technically-RGB DIB abused as ARGB, and finally the normal Bitmap and Image types.
            Boolean built = false;
            if (formats.Contains("PNG"))
            {
                Byte[] pngData = TryGetStreamDataFromClipboard(retrievedData, "PNG");
                if (pngData != null)
                {
                    clipboardimage = ImageUtils.LoadBitmap(pngData);
                    // LoadBitmap clones the object.
                    if (clipboardimage != null) built = true;
                }
            }
            if (clipboardimage == null && formats.Contains("Format17"))
            {
                Byte[] dibdata = TryGetStreamDataFromClipboard(retrievedData, "Format17");
                clipboardimage = DibHandler.ImageFromDib5(dibdata, 0, 0, true);
                // ImageFromClipboardDib5 builds the image in local memory.
                if (clipboardimage != null) built = true;
            }
            if (clipboardimage == null && formats.Contains(DataFormats.Dib))
            {
                Byte[] dibdata = TryGetStreamDataFromClipboard(retrievedData, DataFormats.Dib);
                clipboardimage = DibHandler.ImageFromDib(dibdata, 0);
                // ImageFromClipboardDib builds the image in local memory.
                if (clipboardimage != null) built = true;
            }
            if (clipboardimage == null && formats.Contains(DataFormats.Bitmap))
                clipboardimage = retrievedData.GetData(DataFormats.Bitmap) as Bitmap;
            if (clipboardimage == null && formats.Contains(typeof(Bitmap).FullName))
                clipboardimage = retrievedData.GetData(typeof(Bitmap)) as Bitmap;
            if (clipboardimage == null && formats.Contains(typeof (Image).FullName))
            {
                Image clipImage = retrievedData.GetData(typeof (Image)) as Image;
                clipboardimage = clipImage as Bitmap;
                if (clipboardimage == null && clipImage != null)
                {
                    clipboardimage = new Bitmap(clipImage);
                    built = true;
                }
            }
            // If the image wasn't specifically built using BuildImage already, clone it to separate it from any backing sources.
            if (clipboardimage == null || built)
                return clipboardimage;
            Bitmap returnImage = ImageUtils.CloneImage(clipboardimage);
            try { clipboardimage.Dispose(); }
            catch { /* Ignore */ }
            return returnImage;
        }

        /// <summary>
        /// Copies the given image to the clipboard as PNG, DIB and standard Bitmap format.
        /// </summary>
        /// <param name="image">Image to put on the clipboard.</param>
        /// <param name="imageNoTr">Optional specifically nontransparent version of the image to put on the clipboard.</param>
        /// <param name="data">Clipboard data object to put the image into. Might already contain other stuff. Leave null to create a new one.</param>
        public static void SetClipboardImage(Bitmap image, Bitmap imageNoTr, DataObject data)
        {
            if (data == null)
                data = new DataObject();
            if (imageNoTr == null)
                imageNoTr = image;
            using (MemoryStream pngMemStream = new MemoryStream())
            using (MemoryStream dib5MemStream = new MemoryStream())
            using (MemoryStream dibMemStream = new MemoryStream())
            {
                // As standard bitmap, without transparency support
                data.SetData(DataFormats.Bitmap, true, imageNoTr);
                // As PNG. Gimp will prefer this over the other two.
                Byte[] pngData = ImageUtils.GetPngImageData(image, 0, false);
                pngMemStream.Write(pngData, 0, pngData.Length);
                data.SetData("PNG", false, pngMemStream);
                // As DIBv5. This supports transparency when using BITFIELDS.
                Byte[] dib5Data = DibHandler.ConvertToDib5(image);
                dib5MemStream.Write(dib5Data, 0, dib5Data.Length);
                data.SetData("Format17", false, dib5MemStream);
                // As DIB. This is (wrongly) accepted as ARGB by many applications.
                Byte[] dibData = DibHandler.ConvertToDib(image);
                dibMemStream.Write(dibData, 0, dibData.Length);
                data.SetData(DataFormats.Dib, false, dibMemStream);
                // The 'copy=true' argument means the MemoryStreams can be safely disposed after the operation.
                Clipboard.SetDataObject(data, true);
            }
        }

        public static Byte[] TryGetStreamDataFromClipboard(DataObject retrievedData, String identifier)
        {
            if (!retrievedData.GetDataPresent(identifier))
                return null;
            // Get the dib header
            Object data = retrievedData.GetData(identifier);
            if (!(data is MemoryStream))
                return null;
            MemoryStream ms = retrievedData.GetData(identifier) as MemoryStream;
            if (ms == null)
                return null;
            return ms.ToArray();
        }       

    }
}