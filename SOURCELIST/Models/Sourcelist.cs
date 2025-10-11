using System.ComponentModel.DataAnnotations;

namespace sourcelist.Models
{
    public class Sourcelist
    {
        [Key]
        public string SourceListNumber { get; set; }
        public int ID_Requestor { get; set; }
        public int ID_Approver { get; set; }
        public int ID_Supplier { get; set; }
        public string BAUNumber { get; set; }
        public string PartDescription { get; set; }
        public string SupplierStatus { get; set; }
        public string SourceListStatus { get; set; }
        public DateTime SubmittedDate { get; set; }
        public string ApprovalStatus { get; set; }
        public DateTime? ApproveDate { get; set; }
    }
}
