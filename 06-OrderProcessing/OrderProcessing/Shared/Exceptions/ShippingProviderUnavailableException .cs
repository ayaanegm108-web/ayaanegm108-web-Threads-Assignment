using System;
using System.Collections.Generic;
using System.Text;

namespace OrderProcessing.Shared.Exceptions
{
    public class ShippingProviderUnavailableException : Exception
    {
        public ShippingProviderUnavailableException(string message) : base(message) { }
    }
}
