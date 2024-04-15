using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace emcr_dfa_poc18.Services
{
    public interface IPdfRequestService
    {
        Task<byte[]> GetPdf(Dictionary<string, object> parameters, string template);
        Task<string> GetPdfHash(Dictionary<string, string> parameters, string template);
    }
}
