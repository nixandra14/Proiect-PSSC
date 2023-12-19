using Exemple.Domain.Models;
using Exemple.Domain.Repositories;
using LanguageExt;
using Example.Data.Models;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using static Exemple.Domain.Models.ClientBills;
using static LanguageExt.Prelude;

namespace Example.Data.Repositories
{
    public class BillsRepository : IBillsRepository
    {
        private readonly BillsContext dbContext;

        public BillsRepository(BillsContext dbContext)
        {
            this.dbContext = dbContext;
        }

        public TryAsync<List<CalculatedBillNumber>> TryGetExistingBills() => async () => (await (
                          from g in dbContext.Bills
                          join s in dbContext.Clients on g.ClientId equals s.ClientId
                          select new { s.ClientEmail, g.BillAddress, g.BillId, g.BillNumber})
                          .AsNoTracking()
                          .ToListAsync())
                          .Select(result => new CalculatedBillNumber(
                                                    ClientEmail: new(result.ClientEmail),
                                                    BillAddress: new(result.BillAddress),
                                                    BillNumber: new(result.BillNumber))
                          { 
                            BillId = result.BillId
                          })
                          .ToList();

        public TryAsync<Unit> TrySaveBills(PublishedClientBills bills) => async () =>
        {
            var clients = (await dbContext.Clients.ToListAsync()).ToLookup(client=>client.ClientEmail);
            var newBills = bills.BillList
                                    .Where(g => g.IsUpdated && g.BillId == 0)
                                    .Select(g => new BillDto()
                                    {
                                        ClientId = clients[g.ClientEmail.Value].Single().ClientId,
                                        BillNumber = g.BillNumber.Value,
                                        BillAddress=g.BillAddress.Value
                                        
                                    });
            var updatedBills = bills.BillList.Where(g => g.IsUpdated && g.BillId > 0)
                                    .Select(g => new BillDto()
                                    {
                                        BillId = g.BillId,
                                        ClientId = clients[g.ClientEmail.Value].Single().ClientId,
                                        BillAddress=g.BillAddress.Value,
                                        BillNumber = g.BillNumber.Value
                                        
                                    });;

            dbContext.AddRange(newBills);
            foreach (var entity in updatedBills)
            {
                dbContext.Entry(entity).State = EntityState.Modified;
            }

            await dbContext.SaveChangesAsync();

            return unit;
        };
    }
}
