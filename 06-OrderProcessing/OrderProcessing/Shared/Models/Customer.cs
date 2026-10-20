using System;
using System.Collections.Generic;
using System.Text;

namespace OrderProcessing.Shared.Models
{
    public class Customer
    {
        
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string Email { get; set; } = "";
        public bool IsActive { get; set; }
        public LoyaltyTier LoyaltyTier { get; set; }
        
    }
}
