 using CSharp.Choices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Proiect2.Domain.Models
{
    [AsChoice]
    public static partial class OrderPayments
    {
        public interface IOrderPayments { }

        public record UnvalidatedOrderPayments : IOrderPayments
        {
            public UnvalidatedOrderPayments(IReadOnlyCollection<UnvalidatedPayment> paymentList)
            {
                PaymentList = paymentList;
            }

            public IReadOnlyCollection<UnvalidatedPayment> PaymentList { get; }
        }

        public record InvalidOrderPayments : IOrderPayments
        {
            internal InvalidOrderPayments(IReadOnlyCollection<UnvalidatedPayment> paymentList, string reason)
            {
                PaymentList = paymentList;
                Reason = reason;
            }

            public IReadOnlyCollection<UnvalidatedPayment> PaymentList { get; }
            public string Reason { get; }
        }

        public record FailedOrderPayments : IOrderPayments
        {
            internal FailedOrderPayments(IReadOnlyCollection<UnvalidatedPayment> paymentList, Exception exception)
            {
                PaymentList = paymentList;
                Exception = exception;
            }

            public IReadOnlyCollection<UnvalidatedPayment> PaymentList { get; }
            public Exception Exception { get; }
        }

        public record ValidatedOrderPayments : IOrderPayments
        {
            internal ValidatedOrderPayments(IReadOnlyCollection<ValidatedOrderPayment> paymentList)
            {
                PaymentList = paymentList;
            }

            public IReadOnlyCollection<ValidatedOrderPayment> PaymentList { get; }
        }

        public record VerifiedOrderPayments : IOrderPayments
        {
            internal VerifiedOrderPayments(IReadOnlyCollection<ValidatedCardNumber> paymentList)
            {
                PaymentList = paymentList;
            }

            public IReadOnlyCollection<ValidatedCardNumber> PaymentList { get; }
        }

        public record PublishedOrderPayments : IOrderPayments
        {
            internal PublishedOrderPayments(IReadOnlyCollection<ValidatedCardNumber> paymentList, string csv, DateTime publishedDate)
            {
                PaymentList = paymentList;
                PublishedDate = publishedDate;
                Csv = csv;
            }

            public IReadOnlyCollection<ValidatedCardNumber> PaymentList { get; }
            public DateTime PublishedDate { get; }
            public string Csv { get; }
        }
    }
}
