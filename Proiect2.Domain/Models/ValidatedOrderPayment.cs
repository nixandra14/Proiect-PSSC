using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Schema;

namespace Proiect2.Domain.Models
{
    public record ValidatedOrderPayment(OrderId orderId, CardNumber cardNumber, Total total);
}
