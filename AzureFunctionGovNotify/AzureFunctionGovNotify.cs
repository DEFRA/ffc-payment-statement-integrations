using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Notify.Client;
using PaymentStatementIntegrations.Email;

namespace AzureFunctionGovNotify
{
    public class AzureFunctionGovNotify
    {
        [FunctionName("AzureFunctionGovNotify")]
        public static async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req, ILogger log)
        {
            try
            {
                log.LogInformation("AzureFunctionGovNotify: send email via GovNotify");
                log.LogInformation("AzureFunctionGovNotify: calling IP = " + GetCallingIp(req, log));
                string jsonContent = req.Body != null ? await new StreamReader(req.Body).ReadToEndAsync() : string.Empty;
                log.LogInformation("AzureFunctionGovNotify: json = " + jsonContent);
                if (!string.IsNullOrEmpty(jsonContent))
                {
                    // Deserialise json data
                    var emailData = JsonConvert.DeserializeObject<EmailModel>(jsonContent);

                    if (emailData != null)
                    {
                        if (!string.IsNullOrEmpty(emailData.ApiKey) &&
                            !string.IsNullOrEmpty(emailData.ToAddress) &&
                            !string.IsNullOrEmpty(emailData.TemplateId))
                        {
                            var client = new NotificationClient(emailData.ApiKey);

                            client.SendEmail(
                                emailAddress: emailData.ToAddress,
                                templateId: emailData.TemplateId,
                                personalisation: emailData.Personalisations
                            );
                            log.LogInformation("AzureFunctionGovNotify: Email successfully sent via GovNotify");
                            return new OkResult();
                        }
                    }
                }
                return new BadRequestObjectResult("AzureFunctionGovNotify: Invalid JSON content received by AzureFunctionGovNotify in the request body");
            }
            catch (Exception ex)
            {
                log.LogError("AzureFunctionGovNotify " + ex.Message);
                return new BadRequestObjectResult("AzureFunctionGovNotify: Exception " + ex.Message);
            }
        }

        private static string GetCallingIp(HttpRequest request, ILogger log)
        {
            log.LogInformation("AzureFunctionGovNotify x-for1 " + request.Headers["X-Forwarded-For"]);
            log.LogInformation("AzureFunctionGovNotify x-for2 " + request.Headers["X-Forwarded-For"].FirstOrDefault());
            return (request.Headers["X-Forwarded-For"].FirstOrDefault() ?? string.Empty).Split(new char[] { ':' }).FirstOrDefault();
        }
    }
}
