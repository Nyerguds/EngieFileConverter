using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EngieFileConverter.Domain
{
    public class FileTypeSaveException: Exception
    {
        public FileTypeSaveException() { }
        public FileTypeSaveException(string message) : base(message) { }
        public FileTypeSaveException(string message, params object[] args) : base(String.Format(message, args)) { }
        public FileTypeSaveException(string message, Exception innerException) : base(message, innerException) { }
        public FileTypeSaveException(string message, IEnumerable<object> args, Exception innerException) : base(String.Format(message, args.ToArray()), innerException) { }

    }
}
