using System;
using System.Collections.Generic;

#nullable disable

namespace Test.DbSchema
{
    public partial class Order
    {
        public Order()
        {
            Orderdetail = new HashSet<Orderdetail>();
        }

        public int OrderId { get; set; }
        public string Ordernumber { get; set; }
        public int? Lcv { get; set; }

        public virtual ICollection<Orderdetail> Orderdetail { get; set; }
    }
}
