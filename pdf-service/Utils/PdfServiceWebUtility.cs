using pdfservice.Models;
using System;
using System.Collections.Generic;
using System.IO;

namespace pdfservice.Utils
{
    public interface IPdfServiceWebUtility
    {
        SignatureGroupRequestModel[] ConvertSignatureGroup(List<SignatureGroupViewModel> signatureGroups);
        SignatureGroupViewModel[] ConvertSignatureGroup(SignatureGroupRequestModel[] reqSignatures);

    }

    public class PdfServiceWebUtility : IPdfServiceWebUtility
    {

        public SignatureGroupRequestModel[] ConvertSignatureGroup(List<SignatureGroupViewModel> viewSignatures)
        {
            SignatureGroupRequestModel[] result = new SignatureGroupRequestModel[viewSignatures.Count];


            int i = 0;
            foreach (var sigItem in viewSignatures)
            {
                // TODO: replace this with AutoMapper!!
                var sigResult = new SignatureGroupRequestModel() { PrintName = sigItem.PrintName, SignDate = sigItem.SignDate };

                if (sigItem?.SigImagePicker != null)
                {
                    sigResult.SigImageFormat = sigItem.SigImagePicker?.ContentType;

                    var memStream = new MemoryStream();
                    sigItem?.SigImagePicker?.CopyTo(memStream);
                    sigResult.SigImage = memStream.ToArray();
                }
                result[i] = sigResult;
                i++;
            }

            return (result);
        }

        public SignatureGroupViewModel[] ConvertSignatureGroup(SignatureGroupRequestModel[] requestSignatures)
        {
            SignatureGroupViewModel[] result = new SignatureGroupViewModel[requestSignatures.Length];

            int i = 0;
            foreach (var sigItem in requestSignatures)
            {
                // TODO: replace this with AutoMapper!!
                var sigResult = new SignatureGroupViewModel() { PrintName = sigItem.PrintName, SignDate = sigItem.SignDate };
                
                if (sigItem?.SigImage != null)
                {
                    sigResult.SigImageDisplay = Convert.ToBase64String(sigItem.SigImage);
                    sigResult.SigImageFormat = sigItem.SigImageFormat;
                }
                result[i] = sigResult;
                i++;
            }

            return (result);
        }
    }
}
