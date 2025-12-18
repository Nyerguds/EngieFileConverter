using System;

namespace Nyerguds.Util
{
    /// <summary>File load exceptions. These are typically ignored in favour of checking the next type to try.</summary>
    [Serializable]
    public class FileTypeLoadException : Exception
    {
        /// <summary>USed to store the attempted load type in the Data dictionary to allow serialization.</summary>
        protected readonly String DataAttemptedLoadedType = "AttemptedLoadedType";

        /// <summary>File type that was attempted to be loaded and threw this exception.</summary>
        public String AttemptedLoadedType
        {
            get { return this.Data[this.DataAttemptedLoadedType] as String; }
            set { this.Data[this.DataAttemptedLoadedType] = value; }
        }

        public FileTypeLoadException() { }
        public FileTypeLoadException(String message) : base(message) { }
        public FileTypeLoadException(String message, Exception innerException) : base(message, innerException) { }
        public FileTypeLoadException(String message, String attemptedLoadedType)
            : base(message)
        {
            this.AttemptedLoadedType = attemptedLoadedType;
        }
        public FileTypeLoadException(String message, String attemptedLoadedType, Exception innerException)
            : base(message, innerException)
        {
            this.AttemptedLoadedType = attemptedLoadedType;
        }
    }

    /// <summary>A specific subclass for header parse failure. Can be used for distinguishing internally between different versions of a type.</summary>
    public class HeaderParseException : FileTypeLoadException
    {
        public HeaderParseException() { }
        public HeaderParseException(String message) : base(message) { }
        public HeaderParseException(String message, Exception innerException) : base(message, innerException) { }
        public HeaderParseException(String message, String attemptedLoadedType) : base(message, attemptedLoadedType) { }
        public HeaderParseException(String message, String attemptedLoadedType, Exception innerException) : base(message, attemptedLoadedType, innerException) { }
    }
}
