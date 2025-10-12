using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace sourcelist.Models.ViewModels
{
    public class SourceListCreateViewModel
    {
        // Properti dari Form
        public string? Requestor { get; set; }

        [Required(ErrorMessage = "BAU No. / Part Number wajib diisi.")]
        public string BAUNumber { get; set; }

        [Required(ErrorMessage = "Part Description wajib diisi.")]
        public string PartDescription { get; set; }

        public string? SupplierName { get; set; }

        [Required(ErrorMessage = "Vendor Code wajib diisi.")]
        public string VendorCode { get; set; }

        [Required(ErrorMessage = "Supplier Status wajib diisi.")]
        public string SupplierStatus { get; set; }

        [Required(ErrorMessage = "Source List Status wajib diisi.")]
        public string SourceListStatus { get; set; }

        public IFormFile? AssessmentAttachmentFile { get; set; }

        [Required(ErrorMessage = "CMS# of Final CRB wajib diisi.")]
        public string CMSFinalCRB { get; set; }

        [Required(ErrorMessage = "Reason of Submission wajib diisi.")]
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