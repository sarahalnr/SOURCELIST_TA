using System;

namespace sourcelist.Models.ViewModels
{
    public class SourceListDetailViewModel
    {
        public string SourceListNumber { get; set; }
        public string Requestor { get; set; }
        public string RequestorEmail { get; set; }
        public string BAUNumber { get; set; }
        public string PartDescription { get; set; }
        public string SupplierName { get; set; }
        public string VendorCode { get; set; }
        public string SupplierStatus { get; set; }
        public string SourceListStatus { get; set; }
        public string CMSFinalCRB { get; set; }
        public string ReasonSubmission { get; set; }
        public string ApproverName { get; set; }
        public string ApproverEmail { get; set; }
        public string ApproverStatus { get; set; }
        public DateTime? SubmittedDate { get; set; }
        public string AttachmentFileName { get; set; }
        public string AttachedEndorsement { get; set; }
        public string ValidityPeriod { get; set; }
        public string Remarks { get; set; }
    }
}