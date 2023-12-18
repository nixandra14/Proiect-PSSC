using Proiect2.Domain.Models;
using LanguageExt;
using System.Collections.Generic;
using static Proiect2.Domain.Models.OrderPayments;


namespace Proiect2.Domain.Repositories
{
    public interface IPaymentsRepository
    {
        TryAsync<List<ValidatedCardNumber>> TryGetExistingPayments();

        TryAsync<Unit> TrySavePayments(PublishedOrderPayments payments);
    }
}