using System;

namespace Nyerguds.Util
{
    public interface IFileTypeBroadcaster
    {
        /// <summary>Very short code name for this type.</summary>
        string ShortTypeName { get; }
        /// <summary>Brief name and description of the overall file type, for the types dropdown in the open file dialog.</summary>
        string LongTypeName { get; }
        /// <summary>Possible file extensions for this file type.</summary>
        string[] FileExtensions { get; }
        /// <summary>Brief name and description of the specific type for each extension, for the types dropdown in the save file dialog.</summary>
        string[] DescriptionsForExtensions { get; }
        /// <summary>Supported types can always be loaded, but this indicates if save functionality to this type is also available.</summary>
        bool CanSave { get; }
    }
}
