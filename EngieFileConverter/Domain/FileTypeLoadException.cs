using System;

namespace Nyerguds.Util
{
    /// <summary>File load exceptions. These are typically ignored in favour of checking the next type to try.</summary>
    [Serializable]
    public class FileTypeLoadException : Exception
    {
        /// <summary>USed to store the attempted load type in the Data dictionary to allow serialization.</summary>
        protected readonly string DataAttemptedLoadedType = "AttemptedLoadedType";

        /// <summary>File type that was attempted to be loaded and threw this exception.</summary>
        public string AttemptedLoadedType
        {
            get { return this.Data[this.DataAttemptedLoadedType] as string; }
            set { this.Data[this.DataAttemptedLoadedType] = value; }
        }

        public FileTypeLoadException() { }
        public FileTypeLoadException(string message) : base(message) { }
        public FileTypeLoadException(string message, Exception innerException) : base(message, innerException) { }
        public FileTypeLoadException(string message, string attemptedLoadedType)
            : base(message)
        {
            this.AttemptedLoadedType = attemptedLoadedType;
        }
        public FileTypeLoadException(string message, string attemptedLoadedType, Exception innerException)
            : base(message, innerException)
        {
            this.AttemptedLoadedType = attemptedLoadedType;
        }
    }

    /// <summary>A specific subclass for header parse failure. Can be used for distinguishing internally between different versions of a type.</summary>
    public class HeaderParseException : FileTypeLoadException
    {
        public HeaderParseException() { }
        public HeaderParseException(string message) : base(message) { }
        public HeaderParseException(string message, Exception innerException) : base(message, innerException) { }
        public HeaderParseException(string message, string attemptedLoadedType) : base(message, attemptedLoadedType) { }
        public HeaderParseException(string message, string attemptedLoadedType, Exception innerException) : base(message, attemptedLoadedType, innerException) { }
    }
}
