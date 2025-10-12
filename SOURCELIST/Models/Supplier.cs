using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace sourcelist.Models
{
    [Table("M_SUPPLIER")]
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
