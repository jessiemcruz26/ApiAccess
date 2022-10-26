using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ApiAccess
{
    /// <summary>
    /// Customer model
    /// </summary>
    internal class Customer
    {
        public int StoreId { get; set; }
        public string CustomerId { get; set; }
        public string CELPostalCode { get; set; }
        public int TotalVisits { get; set; }
        public int SegmentCode { get; set; }
    }
}
