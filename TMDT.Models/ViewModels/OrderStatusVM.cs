using BulkyBook.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TMDT.Models.ViewModels
{
    public class OrderStatusVM
    {
        public int CompletedOrders { get; set; }
        public int PendingOrders { get; set; }
        public int InProcessOrders { get; set; }
        public int CancelledOrders { get; set; }
        public List<OrderHeader> Orders { get; set; }
        public Dictionary<string, int> ProductCountByCategory { get; set; }
        public string ChartLabels { get; set; } // Precomputed labels for Chart.js
        public string ChartData { get; set; }
        public string PublisherChartLabels { get; set; } // Doanh thu theo nhà xuất bản
        public string PublisherChartData { get; set; }
    }
}
