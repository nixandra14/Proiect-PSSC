using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProiectPSSC.Domain.Models
{
    public record CalculatedOrderTotalPayment(ClientEmail clientEmail, CalculatedProductPrice product, ProductPrice totalPrice)
    {
        public int OrderId { get; set; } 
        public bool IsUpdated { get; set; }

    }
}
