using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using HandlebarsDotNet;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;
using Newtonsoft.Json;
using pdfservice.Models;
using pdfservice.Utils;
using Stubble.Core.Builders;
using WkHtmlToPdfDotNet;
using WkHtmlToPdfDotNet.Contracts;

namespace pdfservice.Controllers
{
    public class JSONResponse
    {
        public string type;
        public byte[] data;
    }
    [Route("api/[controller]")]
    public class PDFController : Controller
    {
        readonly IConverter _generatePdf;
        private readonly IPdfServiceWebUtility _pdfWebUtility;
        private readonly IConfiguration Configuration;
        protected ILogger _logger;

        public PDFController(IConfiguration configuration, ILoggerFactory loggerFactory, IConverter generatePdf, IPdfServiceWebUtility pdfWebUtility)
        {
            _generatePdf = generatePdf;
            _pdfWebUtility = pdfWebUtility;
            Configuration = configuration;
            _logger = loggerFactory.CreateLogger(typeof(PDFController));
        }

        [HttpPost]
        [Route("GetPDF_DFATest")]
        [Produces("text/html")]
        public IActionResult GetPDF_DFATest()
        {
            var template = "dfa_application_demo";
            var rawdata = new
            {
                applicationType = "SmallBusinessOwner",
                hasInsurance = false,
                dmgAddressLine1 = "address 1",
                dmgAddressLine2 = "address 2",
                dmgCity = "city ",
                dmgProvince = "dsgdfag",
                dmgDescription = "xdgdg",

                signatures = new SignatureGroupRequestModel[] {
                    new SignatureGroupRequestModel { PrintName = "test name1", SignDate = "test date1"},
                    new SignatureGroupRequestModel { PrintName = "test name2", SignDate = "test date2" },
                    }
            };


            string filename = $"Templates/{template}.mustache";
            if (System.IO.File.Exists(filename))
            {
                string format = System.IO.File.ReadAllText(filename);
                HandlebarsTemplate<object, object> handlebar = GetHandlebarsTemplate(format);

                handlebar = Handlebars.Compile(format);
                var html = handlebar(rawdata);

                return Content(html, "text/html", Encoding.UTF8);
            }

            return new NotFoundResult();
        }


        [HttpPost]
        [Route("GetPDF/{template}")]
        [Produces("application/pdf")]
        [ProducesResponseType(200, Type = typeof(FileContentResult))]
        public IActionResult GetPDF([FromBody] Dictionary<string, object> rawdata, string template)
        {
            // first do a mustache merge.
            string filename = $"Templates/{template}.mustache";


            // remove serialized "signature" entry
            Dictionary<string, object> parameters = new Dictionary<string, object>();
            parameters = rawdata
                .Where(x => x.Key != "signatures")
                .ToDictionary();

            // extract serialized "signature" entry - which is actually an array of SignatureGroup objects
            var sigparms = rawdata
                .Where(x => x.Key == "signatures")
                .Select(x => x.Value)
                .FirstOrDefault()
                .ToString();

            SignatureGroupViewModel[] signaturesViewModel = null;

            try
            {
                // deserialize "signature" entry correctly
                var signaturesRequest = JsonConvert.DeserializeObject<SignatureGroupRequestModel[]>(sigparms);
                // convert SignatureGroupRequestModel -> SignatureGroupViewModel
                signaturesViewModel = _pdfWebUtility.ConvertSignatureGroup(signaturesRequest);

            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }

            // add signatures back into Mustache data context
            parameters.Add("signatures", signaturesViewModel);

            if (System.IO.File.Exists(filename))
            {
                string html = null;

                try
                {
                    string format = System.IO.File.ReadAllText(filename);

                    // 2024-04-15 Register Handlebar helper (extra features)
                    HandlebarsTemplate<object, object> handlebar = GetHandlebarsTemplate(format);
                    html = handlebar(parameters);

                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex);
                }

                var doc = new HtmlToPdfDocument()
                {
                    GlobalSettings = {
                        PaperSize = PaperKind.Letter,
                        Orientation = Orientation.Portrait,
                        Margins = new MarginSettings(5.0,5.0,5.0,5.0)
                    },

                    Objects = {
                        new ObjectSettings()
                        {
                            HtmlContent = html
                        }
                    }
                };
                try
                {
                    var pdf = _generatePdf.Convert(doc);
                    return File(pdf, "application/pdf");
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "ERROR rendering PDF");
                    _logger.LogError(template);
                    _logger.LogError(html);
                }
            }

            return new NotFoundResult();
        }



        private static HandlebarsTemplate<object, object> GetHandlebarsTemplate(string format)
        {
            Handlebars.RegisterHelper("arraysig", (outputwriter, options, context, arguments) =>
            {
                try
                {
                    if (arguments.Length != 2)
                    {
                        throw new HandlebarsException("{{#arraysig}} helper must have exactly 2 arguments");
                    }

                    if (arguments[0] is IEnumerable<SignatureGroupViewModel>)
                    {
                        var sigList = arguments[0] as IEnumerable<SignatureGroupViewModel>;
                        var sigArray = sigList.ToArray<SignatureGroupViewModel>();
                        int.TryParse(arguments[1].ToString(), out int sigPosn);

                        if (sigPosn < sigArray.Length)
                        {
                            SignatureGroupViewModel result = sigArray[sigPosn];

                            options.Template(outputwriter, result);
                        }
                        else
                        {
                            options.Inverse(outputwriter, context);
                            //HandlebarsExtensions.WriteSafeString(outputwriter, "");
                        }
                    }
                    else
                    {
                        throw new HandlebarsException("{{arraysig}} 1st parameter must be an array of SignatureGroup");
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"{{arraysig unexpected exception: {ex.Message}");
                }

            });

            var handlebar = Handlebars.Compile(format);
            return handlebar;
        }


        [HttpPost]
        [Route("GetHash/{template}")]
        public IActionResult GetHash([FromBody] Dictionary<string, object> rawdata, string template)
        {
            // first do a mustache merge.
            var stubble = new StubbleBuilder().Build();
            string filename = $"Templates/{template}.mustache";

            if (System.IO.File.Exists(filename))
            {
                string format = System.IO.File.ReadAllText(filename);
                var html = stubble.Render(format, rawdata);

                // compute a hash of the template to render as PDF
                var hash = HashUtility.GetSHA256(Encoding.UTF8.GetBytes(html));
                return new JsonResult(new { hash = hash });
            }

            return new NotFoundResult();
        }

    }
}
