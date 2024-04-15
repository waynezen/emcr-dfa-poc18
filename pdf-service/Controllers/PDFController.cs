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
        private readonly IConfiguration Configuration;
        protected ILogger _logger;

        public PDFController(IConfiguration configuration, ILoggerFactory loggerFactory, IConverter generatePdf)
        {
            _generatePdf = generatePdf;
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

                signatures = new[] {
                    new { PrintName = "test name1", SignDate = "test date1"},
                    new { PrintName = "test name2", SignDate = "test date2" },
                    }
            };

            var foo = JsonConvert.SerializeObject(rawdata);
            var bar = JsonConvert.DeserializeObject(foo);



            string filename = $"Templates/{template}.mustache";
            if (System.IO.File.Exists(filename))
            {
                string format = System.IO.File.ReadAllText(filename);

                var handlebar = Handlebars.Compile(format);

                var html = handlebar(bar);

                //var html = stubble.Render(format, rawdata);

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

            // deserialize "signature" entry correctly
            SignatureGroup[] signatures = JsonConvert.DeserializeObject<SignatureGroup[]>(sigparms);

            // add signatures back into Mustache data context
            parameters.Add("signatures", signatures);

            if (System.IO.File.Exists(filename))
            {
                string html = null;

                try
                {
                    string format = System.IO.File.ReadAllText(filename);
                    var handlebar = Handlebars.Compile(format);
                    html = handlebar(parameters);

                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex);
                }


                //var html = stubble.Render(format, rawdata);

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
