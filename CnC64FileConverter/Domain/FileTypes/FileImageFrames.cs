using Nyerguds.ImageManipulation;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace CnC64FileConverter.Domain.FileTypes
{
    public class FileImageFrames : FileImage
    {
        public override String ShortTypeName { get { return "ImageFRM"; } }
        /// <summary>Brief name and description of the overall file type, for the types dropdown in the open file dialog.</summary>
        public override String ShortTypeDescription { get { return "Frames data"; } }
        /// <summary>Possible file extensions for this file type.</summary>
        public override String[] FileExtensions { get { return new String[0]; } }
        /// <summary>Brief name and description of the specific types for all extensions, for the types dropdown in the save file dialog.</summary>
        public override String[] DescriptionsForExtensions { get { return null; } }

        public override Byte[] SaveToBytesAsThis(SupportedFileType fileToSave, Boolean dontCompress)
        {
            return null;
        }

        /// <summary>Enables frame controls on the UI.</summary>
        public override Boolean ContainsFrames { get { return true; } }
        /// <summary>Retrieves the sub-frames inside this file.</summary>
        public override SupportedFileType[] Frames { get { return FramesList.ToArray(); } }
        /// <summary>If the type supports frames, this determines whether an overview-frame is available as index '-1'. If not, index 0 is accessed directly.</summary>
        public override Boolean RenderCompositeFrame { get { return true; } }

        /// <summary>
        /// Avoid using this for adding frames: use AddFrame instead.
        /// </summary>
        public List<SupportedFileType> FramesList = new List<SupportedFileType>();

        /// <summary>
        /// Adds a frame to the list, setting its FrameParent property to this object.
        /// </summary>
        /// <param name="frame"></param>
        public void AddFrame(SupportedFileType frame)
        {
            frame.FrameParent = this;
            this.FramesList.Add(frame);
        }
    }
}
