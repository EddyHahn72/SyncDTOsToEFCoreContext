using System;
using System.Collections.Generic;

#nullable disable

namespace Test.DbSchema
{
    public partial class Orderdetail
    {
        public int OrderdetailId { get; set; }
        public int OrderId { get; set; }
        public string Description { get; set; }
        public int? Lcv { get; set; }

        public virtual Order Order { get; set; }
    }
}
