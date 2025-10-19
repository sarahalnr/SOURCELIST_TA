using System.Collections.Generic;

namespace sourcelist.Models.ViewModels
{
    public class ChartDataViewModel
    {
        public List<string> Labels { get; set; } = new List<string>();

        // Data untuk Grafik 1 
        public List<int> ApproveData { get; set; } = new List<int>(); 
        public List<int> RejectData { get; set; } = new List<int>();  

        public List<int> RejectedData { get; set; } = new List<int>(); 

        // Data untuk Grafik 2 
        public List<int> TotalData { get; set; } = new List<int>();
    }
}