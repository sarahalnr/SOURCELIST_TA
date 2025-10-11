using System.ComponentModel.DataAnnotations;

namespace sourcelist.Models
{
    public class Supplier
    {
        [Key]
        public int ID_Supplier { get; set; }
        public string NamaSupplier { get; set; }
        public string KodeVendor { get; set; }
        public string EmailSupplier { get; set; }
        public string Status { get; set; }
    }
}
