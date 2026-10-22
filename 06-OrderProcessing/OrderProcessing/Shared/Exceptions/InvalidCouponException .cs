using System;
using System.Collections.Generic;
using System.Text;

namespace OrderProcessing.Shared.Exceptions
{
    public class InvalidCouponException : Exception
    {
        public InvalidCouponException(string message) : base(message) { }
    }
}
