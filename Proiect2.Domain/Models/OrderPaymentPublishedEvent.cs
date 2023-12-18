using CSharp.Choices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Proiect2.Domain.Models
{
    [AsChoice]
    public static partial class OrderPaymentPublishedEvent
    {
        public interface IOrderPaymentPublishedEvent { }

        public record OrderPaymentsPublishSucceededEvent : IOrderPaymentPublishedEvent
        {
            public string Csv { get; }
            public DateTime PublishedDate { get; }

            internal OrderPaymentsPublishSucceededEvent(string csv, DateTime publishedDate)
            {
                Csv = csv;
                PublishedDate = publishedDate;
            }
        }

        public record OrderPaymentsPublishFaildEvent : IOrderPaymentPublishedEvent
        {
            public string Reason { get; }

            internal OrderPaymentsPublishFaildEvent(string reason)
            {
                Reason = reason;
            }
        }
    }
}
