using Microsoft.AspNetCore.Http;
using System.Collections.Generic;

namespace pdfservice.Models
{
    public class SignatureGroupRequestModel
    {
        public string PrintName { get; set; }
        public string SignDate { get; set; }
        public byte[] SigImage { get; set; }
        public string SigImageFormat { get; set; }

    }
}
