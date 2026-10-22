using System;
using System.Collections.Generic;
using System.Text;

namespace OrderProcessing.Shared.Exceptions
{
    public class DataFileCorruptedException : Exception
    {
        public string FileName { get; }

        public DataFileCorruptedException(string fileName, Exception? inner = null)
            : base($"Data file '{fileName}' is corrupted or contains invalid JSON.", inner)
        {
            FileName = fileName;
        }
    }
}
