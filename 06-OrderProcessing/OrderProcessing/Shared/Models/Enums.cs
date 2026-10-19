using System;
using System.Collections.Generic;
using System.Text;

namespace OrderProcessing.Shared.Models
{
    enum LoyaltyTier { Bronze, Silver, Gold }
    enum OrderStatus { Pending, Validated, Rejected, PaymentFailed, Completed, Failed }
    enum PaymentStatus { Captured, Declined, Refunded }
}
