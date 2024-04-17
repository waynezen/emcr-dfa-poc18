using emcr_dfa_poc18.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Newtonsoft.Json;
using pdfservice.Models;
using pdfservice.Utils;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;



namespace emcr_dfa_poc18.Pages
{
    public enum TrueFalseEnum
    {
        False = 0,
        True = 1
    };

    public enum ApplicationTypeEnum
    {
        SmallBusinessOwner = 0,
        Indigenous = 1,
        Public = 2

    }

    public class IndexModel : PageModel
    {
        private readonly IPdfRequestService _pdfService;
        private readonly IPdfServiceWebUtility _pdfWebUtility;
        private readonly ILogger<IndexModel> _logger;

        public IndexModel(
            IPdfRequestService pdfService,
            IPdfServiceWebUtility pdfWebUtility,
            ILogger<IndexModel> logger)
        {
            _pdfService = pdfService;
            _pdfWebUtility = pdfWebUtility;
            _logger = logger;

            // create 2 signatures each for no insurance and application groups
            noinsuranceSignatures = new List<SignatureGroupViewModel>() { new SignatureGroupViewModel(), new SignatureGroupViewModel() };

            applicationSignatures = new List<SignatureGroupViewModel>() { new SignatureGroupViewModel(), new SignatureGroupViewModel() };
        }

        [BindProperty]
        [Display(Name = "Application Type")]
        public ApplicationTypeEnum applicationType { get; set; }
        [BindProperty]
        [Display(Name = "Do you have Insurance?")]
        public TrueFalseEnum hasInsurance { get; set; }

        [BindProperty]
        [Display(Name = "Address1")]
        public string? dmgAddressLine1 { get; set; }
        [BindProperty]
        [Display(Name = "Address2")]
        public string? dmgAddressLine2 { get; set; }
        [BindProperty]
        [Display(Name = "City")]
        public string? dmgCity { get; set; }
        [BindProperty]
        [Display(Name = "Province")]
        public string? dmgProvince { get; set; }
        [BindProperty]
        [Display(Name = "Description")]
        public string? dmgDescription { get; set; }

        [BindProperty]
        public List<SignatureGroupViewModel> noinsuranceSignatures { get; set; }

        [BindProperty]
        public List<SignatureGroupViewModel> applicationSignatures { get; set; }



        public async Task<IActionResult> OnPostGeneratePDFAsync()
        {

            // put together the parameters that we will pass to PDF Service
            Dictionary<string, object> parameters = new Dictionary<string, object>();

            var templateName = "dfa_application_demo";
            parameters.Add("applicationType", this.applicationType.ToString());
            parameters.Add("hasInsurance", this.hasInsurance.ToString());
            parameters.Add("dmgAddressLine1", this.dmgAddressLine1 ?? "");
            parameters.Add("dmgAddressLine2", this.dmgAddressLine2 ?? "");
            parameters.Add("dmgCity", this.dmgCity ?? "");
            parameters.Add("dmgProvince", this.dmgProvince ?? "");
            parameters.Add("dmgDescription", this.dmgDescription ?? "");


            var noinsSigGroup = _pdfWebUtility.ConvertSignatureGroup(this.noinsuranceSignatures);

            // must serialize signature data before sending over the wire
            var sigser = JsonConvert.SerializeObject(noinsSigGroup);
            parameters.Add("signatures", sigser);


            byte[] data = await _pdfService.GetPdf(parameters, templateName);
            if (data != null)
            {
                // write file to wwwroot - we may want to uniquely name the files and save them to a network device
                var pdfFileName = "dfae_acceptance.pdf";
                System.IO.File.WriteAllBytes($"wwwroot/pdf_forms/{pdfFileName}", data);

                return Redirect($"~/pdf_forms/{pdfFileName}");
            }

            return Page();
        }

        public void OnGet()
        {

        }


    }

    
}
