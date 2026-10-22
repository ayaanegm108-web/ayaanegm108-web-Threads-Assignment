using System;
using System.Collections.Generic;
using System.Text;

namespace OrderProcessing.Shared.Exceptions
{
    public class DatabaseWriteException : Exception
    {
        public DatabaseWriteException(string message) : base(message) { }
    }
}
