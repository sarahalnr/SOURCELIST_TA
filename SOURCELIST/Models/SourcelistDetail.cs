using System.ComponentModel.DataAnnotations;

namespace sourcelist.Models
{
    public class SourcelistDetail
    {
        [Key]
        public string SourceListNumber { get; set; }
        public string ReasonSubmission { get; set; }
        public string CMSFinalCRB { get; set; }
        public string AttachmentFileName { get; set; }
        public string AttachedEndorsement { get; set; }
        public string Remarks { get; set; }
        public string ValidityPeriod { get; set; }
    }
}
