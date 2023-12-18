using Proiect2.Domain.Models;
using static LanguageExt.Prelude;
using LanguageExt;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using static Proiect2.Domain.Models.OrderPayments;
using System.Threading.Tasks;

namespace Proiect2.Domain
{
    public static class OrderPaymentsOperation
    {
        public static Task<IOrderPayments> ValidateOrderPayment(Func<OrderId, Option<OrderId>> checkOrderExists, UnvalidatedOrderPayments orderPayment) =>
            orderPayment.PaymentList
                      .Select(ValidateOrderPayment(checkOrderExists))
                      .Aggregate(CreateEmptyValidatedPaymentList().ToAsync(), ReduceValidPayments)
                      .MatchAsync(
                            Right: validatedPayments => new ValidatedOrderPayments(validatedPayments),
                            LeftAsync: errorMessage => Task.FromResult((IOrderPayments)new InvalidOrderPayments(orderPayment.PaymentList, errorMessage))
                      );

        private static Func<UnvalidatedPayment, EitherAsync<string, ValidatedOrderPayment>> ValidateOrderPayment(Func<OrderId, Option<OrderId>> checkOrderExists) =>
            unvalidatedOrderPayment => ValidateOrderPayment(checkOrderExists, unvalidatedOrderPayment);

        private static EitherAsync<string, ValidatedOrderPayment> ValidateOrderPayment(Func<OrderId, Option<OrderId>> checkOrderExists, UnvalidatedPayment unvalidatedPayment) =>

            from orderId in OrderId.TryParseOrderId(unvalidatedPayment.OrderId)
                                   .ToEitherAsync($"Invalid activity bill ({unvalidatedPayment.OrderId})")
            from cardNumber in CardNumber.TryParseCardNumber(unvalidatedPayment.CardNumber)
                               .ToEitherAsync($"Invalid activity bill ({unvalidatedPayment.OrderId})")
            from total in Total.TryParse(unvalidatedPayment.Total)
                           .ToEitherAsync($"Invalid activity bill ({unvalidatedPayment.OrderId})")
            from clientExists in checkOrderExists(orderId)
                                   .ToEitherAsync($"Student {orderId.Value} does not exist.")
            select new ValidatedOrderPayment(orderId, cardNumber, total);

        private static Either<string, List<ValidatedOrderPayment>> CreateEmptyValidatedPaymentList() =>
            Right(new List<ValidatedOrderPayment>());

        private static EitherAsync<string, List<ValidatedOrderPayment>> ReduceValidPayments(EitherAsync<string, List<ValidatedOrderPayment>> acc, EitherAsync<string, ValidatedOrderPayment> next) =>
            from list in acc
            from nextPayment in next
            select list.AppendValidPayment(nextPayment);

        private static List<ValidatedOrderPayment> AppendValidPayment(this List<ValidatedOrderPayment> list, ValidatedOrderPayment validPayment)
        {
            list.Add(validPayment);
            return list;
        }

        public static IOrderPayments ValidateCardNumbers(IOrderPayments orderPayment) => orderPayment.Match(
           whenUnvalidatedOrderPayments: unvalidated => unvalidated,
           whenInvalidOrderPayments: invalid => invalid,
           whenFailedOrderPayments: failed => failed,
           whenPublishedOrderPayments: published => published,
           whenVerifiedOrderPayments: verified => verified,
            whenValidatedOrderPayments: ValidateCardNumber
       
        );

        private static IOrderPayments ValidateCardNumber(ValidatedOrderPayments validOrderPayments) =>
            new VerifiedOrderPayments(validOrderPayments.PaymentList
                                                    .Select(ValidateOrderFinalPayment)
                                                    .ToList()
                                                    .AsReadOnly());

        private static ValidatedCardNumber ValidateOrderFinalPayment(ValidatedOrderPayment validPayment) =>
            new ValidatedCardNumber(  validPayment.orderId,
                                      validPayment.cardNumber,
                                      validPayment.total);

        public static IOrderPayments MergePayments(IOrderPayments orderPayment, IEnumerable<ValidatedCardNumber> existingPayments) => orderPayment.Match(
           whenUnvalidatedOrderPayments: unvalidated => unvalidated,
           whenInvalidOrderPayments: invalid => invalid,
           whenFailedOrderPayments: failed => failed,
           whenValidatedOrderPayments: validated => validated,
           whenPublishedOrderPayments: published => published,
           whenVerifiedOrderPayments: verified => MergePayments(verified.PaymentList, existingPayments));

        private static VerifiedOrderPayments MergePayments(IEnumerable<ValidatedCardNumber> newList, IEnumerable<ValidatedCardNumber> existingList)
        {
            var updatedAndNewPayments = newList.Select(payment => payment with { PaymentId = existingList.FirstOrDefault(g => g.orderId == payment.orderId)?.PaymentId ?? 0, IsUpdated = true });
            var oldPayments = existingList.Where(payment => !newList.Any(g => g.orderId == payment.orderId));
            var allPayments = updatedAndNewPayments.Union(oldPayments)
                                               .ToList()
                                               .AsReadOnly();
            return new VerifiedOrderPayments(allPayments);
        }

        public static IOrderPayments PublishOrderPayments(IOrderPayments orderPayment) => orderPayment.Match(
           whenUnvalidatedOrderPayments: unvalidated => unvalidated,
           whenInvalidOrderPayments: invalid => invalid,
           whenFailedOrderPayments: failed => failed,
           whenValidatedOrderPayments: validated => validated,
           whenPublishedOrderPayments: published => published,
           whenVerifiedOrderPayments: GenerateExport);

        private static IOrderPayments GenerateExport(VerifiedOrderPayments verifiedPayment) =>
            new PublishedOrderPayments(verifiedPayment.PaymentList,
                                    verifiedPayment.PaymentList.Aggregate(new StringBuilder(), CreateCsvLine).ToString(),
                                    DateTime.Now);

        private static StringBuilder CreateCsvLine(StringBuilder export, ValidatedCardNumber payment) =>
        //export.AppendLine($"{bill.OrderId.Value}, {bill.ExamGrade}, {bill.ActivityGrade}, {bill.FinalGrade}");
            export.AppendLine($"{payment.orderId.Value}, {payment.cardNumber}");
    }
}
