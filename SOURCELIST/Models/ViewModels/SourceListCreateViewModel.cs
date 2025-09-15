namespace sourcelist.Models.ViewModels
{
    public class SourceListCreateViewModel
    {
        public string Requestor { get; set; }
        public string BAUNumber { get; set; }
        public string PartDescription { get; set; }
        public string SupplierName { get; set; }
        public string VendorCode { get; set; }
        public string SupplierStatus { get; set; }
        public string SourceListStatus { get; set; }
        public IFormFile? AttachmentFile { get; set; }
        public string CMSFinalCRB { get; set; }
        public string ReasonSubmission { get; set; }

        //public string RequestorEmail { get; set; } // tambahan

        public string ApproverName { get; set; }
        public string ApproverEmail { get; set; }
    }
}
