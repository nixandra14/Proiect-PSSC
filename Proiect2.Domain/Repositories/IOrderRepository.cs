using LanguageExt;
using Proiect2.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Proiect2.Domain.Models.OrderId;

namespace Proiect2.Domain.Repositories
{
    public interface IOrderRepository
    {
        TryAsync<List<OrderId>> TryGetExistingOrders(IEnumerable<int> ordersToCheck);

    }
}
