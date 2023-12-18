using Proiect2.Domain.Models;
using static Proiect2.Domain.Models.OrderPaymentPublishedEvent;
using static Proiect2.Domain.OrderPaymentsOperation;
using System;
using static Proiect2.Domain.Models.OrderPayments;
using LanguageExt;
using System.Threading.Tasks;
using System.Collections.Generic;
using Proiect2.Domain.Repositories;
using System.Linq;
using static LanguageExt.Prelude;
using Microsoft.Extensions.Logging;

namespace Proiect2.Domain
{
    public class PublishPaymentWorkflow
    {
        private readonly IOrderRepository ordersRepository;
        private readonly IPaymentsRepository paymentsRepository;
        private readonly ILogger<PublishPaymentWorkflow> logger;

        public PublishPaymentWorkflow(IOrderRepository ordersRepository, IPaymentsRepository paymentsRepository, ILogger<PublishPaymentWorkflow> logger)
        {
            this.ordersRepository = ordersRepository;
            this.paymentsRepository = paymentsRepository;
            this.logger = logger;
        }

        public async Task<IOrderPaymentPublishedEvent> ExecuteAsync(PublishPaymentsCommand command)
        {
            UnvalidatedOrderPayments unvalidatedPayments = new UnvalidatedOrderPayments(command.InputPayments);

            var result = from orders in ordersRepository.TryGetExistingOrders(unvalidatedPayments.PaymentList.Select(payment => payment.OrderId))
                                          .ToEither(ex => new FailedOrderPayments(unvalidatedPayments.PaymentList, ex) as IOrderPayments)
                         from existingPayments in paymentsRepository.TryGetExistingPayments()
                                          .ToEither(ex => new FailedOrderPayments(unvalidatedPayments.PaymentList, ex) as IOrderPayments)
                         let checkOrderExists = (Func<OrderId, Option<OrderId>>)(order => CheckOrderExists(orders, order))
                         from publishedPayments in ExecuteWorkflowAsync(unvalidatedPayments, existingPayments, checkOrderExists).ToAsync()
                         from _ in paymentsRepository.TrySavePayments(publishedPayments)
                                          .ToEither(ex => new FailedOrderPayments(unvalidatedPayments.PaymentList, ex) as IOrderPayments)
                         select publishedPayments;

            return await result.Match(
                    Left: orderPayments => GenerateFailedEvent(orderPayments) as IOrderPaymentPublishedEvent,
                    Right: publishedPayments => new OrderPaymentsPublishSucceededEvent(publishedPayments.Csv, publishedPayments.PublishedDate)
                );
        }

        private async Task<Either<IOrderPayments, PublishedOrderPayments>> ExecuteWorkflowAsync(UnvalidatedOrderPayments unvalidatedPayments,
                                                                                          IEnumerable<ValidatedCardNumber> existingPayments,
                                                                                          Func<OrderId, Option<OrderId>> CheckOrderExists)
        {

            IOrderPayments payments = await ValidateOrderPayment(CheckOrderExists, unvalidatedPayments);
            payments = ValidateCardNumbers(payments);
            payments = MergePayments(payments, existingPayments);
            payments = PublishOrderPayments(payments);



            return payments.Match<Either<IOrderPayments, PublishedOrderPayments>>(
                  whenUnvalidatedOrderPayments: unvalidated => Left(unvalidated as IOrderPayments),
             whenInvalidOrderPayments: invalidPayments => Left(invalidPayments as IOrderPayments),
           whenFailedOrderPayments: failedPayments => Left(failedPayments as IOrderPayments),
             whenValidatedOrderPayments: validatedPayments => Left(validatedPayments as IOrderPayments),
             whenPublishedOrderPayments: publishedPayments => Right(publishedPayments),
           whenVerifiedOrderPayments: calculatedPayments => Left(calculatedPayments as IOrderPayments)
            );
        }


        private Option<OrderId> CheckOrderExists(IEnumerable<OrderId> orders, OrderId OrderId)
        {
            if (orders.Any(c => c == OrderId))
            {
                return Some(OrderId);
            }
            else
            {
                return None;
            }
        }

        private OrderPaymentsPublishFaildEvent GenerateFailedEvent(IOrderPayments orderPayments) =>
            orderPayments.Match<OrderPaymentsPublishFaildEvent>(

           whenUnvalidatedOrderPayments: unvalidated => new($"Invalid state {nameof(UnvalidatedOrderPayments)}"),
            whenInvalidOrderPayments: invalidPayments => new(invalidPayments.Reason),
            whenFailedOrderPayments: failedPayments =>
           {
               logger.LogError(failedPayments.Exception, failedPayments.Exception.Message);
               return new(failedPayments.Exception.Message);
           },
            whenValidatedOrderPayments: validatedPayments => new($"Invalid state {nameof(ValidatedOrderPayments)}"),
          whenVerifiedOrderPayments: calculatedPayments => new($"Invalid state {nameof (VerifiedOrderPayments)}"),
           whenPublishedOrderPayments: publishedPayments => new($"Invalid state {nameof(PublishedOrderPayments)}")

           );
    }
}
