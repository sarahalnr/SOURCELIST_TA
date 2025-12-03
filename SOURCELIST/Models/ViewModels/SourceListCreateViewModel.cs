using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace sourcelist.Models.ViewModels
{
    public class SourceListCreateViewModel
    {
        // Properti dari Form
        public string? Requestor { get; set; }

        public string BAUNumber { get; set; }

        public string PartDescription { get; set; }

        public string? SupplierName { get; set; }

        public string VendorCode { get; set; }
        public string SupplierStatus { get; set; }

        public string SourceListStatus { get; set; }

        public IFormFile? AssessmentAttachmentFile { get; set; }

        public string CMSFinalCRB { get; set; }

        public string ReasonSubmission { get; set; }

        public IFormFile? AttachedEndorsementFile { get; set; }

        // Properti yang diisi OTOMATIS 
        public string? RequestorEmail { get; set; }
        public string? ApproverName { get; set; }
        public string? ApproverEmail { get; set; }

        // PROPERTI ID
        public int RequestorId { get; set; }
        public int ApproverId { get; set; }
        public int? SupplierId { get; set; }
    }
}