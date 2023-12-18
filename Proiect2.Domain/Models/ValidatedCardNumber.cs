using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Proiect2.Domain.Models
{
    public record ValidatedCardNumber(OrderId orderId, CardNumber cardNumber, Total total)
    {
        public int PaymentId { get; set; }
    public bool IsUpdated { get; set; }
}
}
