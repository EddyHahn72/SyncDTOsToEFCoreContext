using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Test.DbSchema;
using HenryHarrow.EntityFrameworkCore;

namespace TesterCore
{
    class Program
    {
        static void Main(string[] args)
        {
            new Program().TestAsync().Wait();
        }


        public async Task<IEnumerable<Test.DbSchema.Order>> TestAsync()
        {
            var context = new HenryHarrowContext();
            

            var res = await context.Order.Include("Orderdetail").ToListAsync();

            AutoMapper.Mapper mapper = new AutoMapper.Mapper(new AutoMapper.MapperConfiguration((c) =>
            {
                c.CreateMap<Order, Order>();
                c.CreateMap<Orderdetail, Orderdetail>();
            }));

            var copy = new List<Order>();

            mapper.Map(res, copy);
            copy[0].Ordernumber = "TaDa";
            copy[0].Orderdetail.First().Description = "Updated";
            copy[0].Orderdetail.Add(new Orderdetail { Description = "This is new 1" });
            copy[0].Orderdetail.Remove(copy[0].Orderdetail.ElementAt(1));
            copy[0].Orderdetail.Add(new Orderdetail { Description = "This is new 2" });

            Console.WriteLine();
            Console.WriteLine("Before state synchronized; just loaded from DB");
            Console.WriteLine("[0] state: {0}", context.Entry(res[0]).State);
            Console.WriteLine("[0][0] state: {0}", context.Entry(res[0].Orderdetail.ToList()[0]).State);
            Console.WriteLine("[0][1] state: {0}", context.Entry(res[0].Orderdetail.ToList()[1]).State);
            Console.WriteLine("[0][2] state: {0}", context.Entry(res[0].Orderdetail.ToList()[2]).State);


            context.SyncDtoToEf(copy, res);

            Console.WriteLine();
            Console.WriteLine("After state synchronized");
            Console.WriteLine("[0] state: {0}", context.Entry(res[0]).State);
            var details = res[0].Orderdetail.ToList();
            for (int index = 0; index < details.Count; index++)
            {
                Console.WriteLine("[0][{2}] state: {0}  {1}", context.Entry(details[index]).State, details[index].Description, index);
            }

            foreach (var ti in context.ChangeTracker.Entries())
            {

            }

            context.SaveChanges();

            return res;
        }
    }
}
