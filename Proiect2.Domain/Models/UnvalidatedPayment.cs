using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Proiect2.Domain.Models
{
    public record UnvalidatedPayment(int OrderId,string CardNumber, decimal Total);

}
