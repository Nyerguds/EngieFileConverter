using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EngieFileConverter.Domain
{
    public class FileTypeSaveException: Exception
    {
        public FileTypeSaveException() { }
        public FileTypeSaveException(String message) : base(message) { }
        public FileTypeSaveException(String message, params Object[] args) : base(String.Format(message, args)) { }
        public FileTypeSaveException(String message, Exception innerException) : base(message, innerException) { }
        public FileTypeSaveException(String message, IEnumerable<Object> args, Exception innerException) : base(String.Format(message, args.ToArray()), innerException) { }

    }
}
