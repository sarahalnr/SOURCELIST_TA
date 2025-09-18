using System;

namespace sourcelist.DTOs
{
    public class SourceListDTO
    {
        public string SourceListNumber { get; set; }
        public string Requestor { get; set; }
        public DateTime SubmittedDate { get; set; }
        public string BAUNumber { get; set; }
        public string PartDescription { get; set; }
        public string SupplierName { get; set; }
        public string VendorCode { get; set; }
        public string ReasonSubmission { get; set; }
        public string SourceListStatus { get; set; }
        public string ApproverStatus { get; set; }
    }
}