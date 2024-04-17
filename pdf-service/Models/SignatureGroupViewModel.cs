using Microsoft.AspNetCore.Http;

namespace pdfservice.Models
{
    public class SignatureGroupViewModel
    {
        public string? PrintName { get; set; }
        public string? SignDate { get; set; }
        public IFormFile? SigImagePicker { get; set; }
        public string? SigImageDisplay { get; set; }
        public string? SigImageFormat { get; set; }

    }
}
