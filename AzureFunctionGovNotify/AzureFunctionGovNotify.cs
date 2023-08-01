using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
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
        public static async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Admin, "post", Route = null)] HttpRequestMessage req, ILogger log)
        {
            try
            {
                log.LogInformation("C# HTTP trigger function to send email via GovNotify");
                string jsonContent = req.Content != null ? await req.Content.ReadAsStringAsync() : string.Empty;
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
                            log.LogInformation("Email successfully sent via GovNotify");
                            return new OkResult();
                        }
                    }
                }
                return new BadRequestObjectResult("Invalid JSON content in the request body");
            }
            catch (Exception ex)
            {
                log.LogError("AzureFunctionGovNotify", ex);
                return new BadRequestObjectResult("Exception in AzureFunctionGovNotify");
            }
        }
    }
}
