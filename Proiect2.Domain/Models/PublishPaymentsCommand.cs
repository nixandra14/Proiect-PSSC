using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Proiect2.Domain.Models
{
    public record PublishPaymentsCommand
    {
        public PublishPaymentsCommand(IReadOnlyCollection<UnvalidatedPayment> inputPayment)
        {
            InputPayments = inputPayment;
        }

        public IReadOnlyCollection<UnvalidatedPayment> InputPayments { get; }
    }
}
