using System;

namespace Nyerguds.Util
{
    public interface IFileTypeBroadcaster
    {
        /// <summary>Very short code name for this type.</summary>
        String ShortTypeName { get; }
        /// <summary>Brief name and description of the overall file type, for the types dropdown in the open file dialog.</summary>
        String LongTypeName { get; }
        /// <summary>Possible file extensions for this file type.</summary>
        String[] FileExtensions { get; }
        /// <summary>Brief name and description of the specific type for each extension, for the types dropdown in the save file dialog.</summary>
        String[] DescriptionsForExtensions { get; }
        /// <summary>Supported types can always be loaded, but this indicates if save functionality to this type is also available.</summary>
        Boolean CanSave { get; }
    }
}
