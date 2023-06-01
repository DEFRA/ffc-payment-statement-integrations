using LogicAppUnit;
using LogicAppUnit.Helper;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;

namespace PaymentStatementIntegrations.Tests.CrmRetrievalTest
{
    /// <summary>
    /// Test cases for the <i>CrmRetrieval</i> workflow.
    /// </summary>
    [TestClass]
    public class CrmRetrievalTest : WorkflowTestBase
    {
        private const string _WebHookRequestApiKey = "valid-auth-webhook-apikey";

        [TestInitialize]
        public void TestInitialize()
        {
            Initialize(Constants.LOGIC_APP_BASE_PATH, "CrmRetrieval");
        }

        [ClassCleanup]
        public static void CleanResources()
        {
            Close();
        }

        /// <summary>
        /// Tests that the correct response is returned when an incorrect value for the 'X-API-Key header' is used with the webhook request.
        /// </summary>
        [TestMethod]
        public void CrmRetrievalTest_When_Invalid_Auth_Token_In_Request()
        {
            using (ITestRunner testRunner = CreateTestRunner())
            {
                // Run the workflow
                var workflowResponse = testRunner.TriggerWorkflow(
                    GetWebhookRequest(),
                    HttpMethod.Post,
                    new Dictionary<string, string> { { "x-api-key", "invalid-token" } });

                // Check workflow run status
                Assert.AreEqual(WorkflowRunStatus.Succeeded, testRunner.WorkflowRunStatus);

                // Check workflow response
                testRunner.ExceptionWrapper(() => Assert.AreEqual(HttpStatusCode.Unauthorized, workflowResponse.StatusCode));
                Assert.AreEqual("Invalid/No authorization header passed", workflowResponse.Content.ReadAsStringAsync().Result);
                Assert.AreEqual("text/plain; charset=utf-8", workflowResponse.Content.Headers.ContentType?.ToString());

                // Check action result
                Assert.AreEqual(ActionStatus.Succeeded, testRunner.GetWorkflowActionStatus("Unauthorized_Response"));
                Assert.AreEqual(ActionStatus.Skipped, testRunner.GetWorkflowActionStatus("Get_PDF_Contents"));
            }
        }

        /// <summary>
        /// Tests that the correct response is returned when the HTTP call to the Service One API to get the PDF content fails.
        /// </summary>
        [TestMethod]
        public void CrmRetrievalTest_When_Get_PDF_Service_Fails()
        {
            using (ITestRunner testRunner = CreateTestRunner())
            {
                // Mock the HTTP calls and customize responses
                testRunner.AddApiMocks = (request) =>
                {
                    HttpResponseMessage mockedResponse = new HttpResponseMessage();
                    if (request.RequestUri?.AbsolutePath == "/api/v1/statements/statement/54617" && request.Method == HttpMethod.Get)
                    {
                        mockedResponse.RequestMessage = request;
                        mockedResponse.StatusCode = HttpStatusCode.InternalServerError;
                        mockedResponse.Content = ContentHelper.CreatePlainStringContent("Internal server error detected in PDF Service");
                    }
                    return mockedResponse;
                };

                // Run the workflow
                var workflowResponse = testRunner.TriggerWorkflow(
                    GetWebhookRequest(),
                    HttpMethod.Post,
                    new Dictionary<string, string> { { "x-api-key", _WebHookRequestApiKey } });

                // Check workflow run status
                Assert.AreEqual(WorkflowRunStatus.Succeeded, testRunner.WorkflowRunStatus);

                // Check workflow response
                testRunner.ExceptionWrapper(() => Assert.AreEqual(HttpStatusCode.InternalServerError, workflowResponse.StatusCode));
                Assert.AreEqual("Unable to get PDF content: Internal server error detected in PDF Service", workflowResponse.Content.ReadAsStringAsync().Result);
                Assert.AreEqual("text/plain; charset=utf-8", workflowResponse.Content.Headers.ContentType?.ToString());

                // Check action result
                Assert.AreEqual(ActionStatus.Skipped, testRunner.GetWorkflowActionStatus("Unauthorized_Response"));
                Assert.AreEqual(ActionStatus.Failed, testRunner.GetWorkflowActionStatus("Get_PDF_Contents"));
                Assert.AreEqual(ActionStatus.Succeeded, testRunner.GetWorkflowActionStatus("Failed_PDF_Response"));
            }
        }

        /// <summary>
        /// Tests that the correct response is returned when the HTTP call to the Service Two API to update the customer details is successful.
        /// </summary>
        [TestMethod]
        public void CrmRetrievalTest_When_Successful()
        {
            // Override one of the settings in the local settings file
            var settingsToOverride = new Dictionary<string, string>();

            using (ITestRunner testRunner = CreateTestRunner(settingsToOverride))
            {
                // Mock the HTTP calls and customize responses
                testRunner.AddApiMocks = (request) =>
                {
                    HttpResponseMessage mockedResponse = new HttpResponseMessage();
                    if (request.RequestUri?.AbsolutePath == "/api/v1/statements/statement/54617" && request.Method == HttpMethod.Get)
                    {
                        mockedResponse.RequestMessage = request;
                        mockedResponse.StatusCode = HttpStatusCode.OK;
                        mockedResponse.Content = GetCustomerResponse();
                    }
                    else if (request.RequestUri?.AbsolutePath == "/api/v1/validate" && request.Method == HttpMethod.Post)
                    {
                        mockedResponse.RequestMessage = request;
                        mockedResponse.StatusCode = HttpStatusCode.OK;
                        mockedResponse.Content = ContentHelper.CreatePlainStringContent("success");
                    }
                    return mockedResponse;
                };

                // Run the workflow
                var workflowResponse = testRunner.TriggerWorkflow(
                    GetWebhookRequest(),
                    HttpMethod.Post,
                    new Dictionary<string, string> { { "x-api-key", _WebHookRequestApiKey } });

                // Check workflow run status
                Assert.AreEqual(WorkflowRunStatus.Succeeded, testRunner.WorkflowRunStatus);

                // Check workflow response
                testRunner.ExceptionWrapper(() => Assert.AreEqual(HttpStatusCode.OK, workflowResponse.StatusCode));
                //Assert.AreEqual("Webhook processed successfully", workflowResponse.Content.ReadAsStringAsync().Result);
                Assert.AreEqual("application/pdf", workflowResponse.Content.Headers.ContentType?.ToString());

                // Check action result
                Assert.AreEqual(ActionStatus.Skipped, testRunner.GetWorkflowActionStatus("Unauthorized_Response"));
                Assert.AreEqual(ActionStatus.Succeeded, testRunner.GetWorkflowActionStatus("Get_PDF_Contents"));
                Assert.AreEqual(ActionStatus.Skipped, testRunner.GetWorkflowActionStatus("Failed_PDF_Response"));
                Assert.AreEqual(ActionStatus.Succeeded, testRunner.GetWorkflowActionStatus("Return_PDF"));

                // Check request to Auth Server
                var authServerRequest = testRunner.MockRequests.First(r => r.RequestUri.AbsolutePath == "/api/v1/validate");
                Assert.AreEqual(HttpMethod.Post, authServerRequest.Method);
                Assert.AreEqual("ApiKey valid-auth-apikey", authServerRequest.Headers["x-api-key"].First());

                // Check request to PDF Server
                var pdfServerRequest = testRunner.MockRequests.First(r => r.RequestUri.AbsolutePath == "/api/v1/statements/statement/54617");
                Assert.AreEqual(HttpMethod.Get, pdfServerRequest.Method);
                Assert.AreEqual("ApiKey valid-auth-apikey", pdfServerRequest.Headers["x-api-key"].First());

                // Check tracked properties
                var trackedProps = testRunner.GetWorkflowActionTrackedProperties("Get_PDF_Contents");
                Assert.AreEqual("customer", trackedProps["recordType"]);
                Assert.AreEqual("54617", trackedProps["recordId"]);
                Assert.AreEqual("c2ddb2f2-7bff-4cce-b724-ac2400b12760", trackedProps["correlationId"]);
            }
        }

        private static StringContent GetWebhookRequestAsQueryString()
        {
            return ContentHelper.CreatePlainStringContent("?id=71fbcb8e-f974-449a-bb14-ac2400b150aa&correlationId=c2ddb2f2-7bff-4cce-b724-ac2400b12760&sourceSystem=CrmService&" +
                "timestamp=2022-08-27T08:45:00.1493711Z&type=CustomerUpdated&customerId=54617&resourceId=54617&resourceURI=https://external-service-one.testing.net/api/v1/customer/54617");
        }

        private static StringContent GetWebhookRequest()
        {
            return ContentHelper.CreateJsonStringContent(new
            {
                id = "71fbcb8e-f974-449a-bb14-ac2400b150aa",
                correlationId = "c2ddb2f2-7bff-4cce-b724-ac2400b12760",
                sourceSystem = "CrmService",
                timestamp = "2022-08-27T08:45:00.1493711Z",
                type = "CustomerUpdated",
                customerId = 54617,
                resourceId = "54617",
                resourceURI = "https://external-service-one.testing.net/api/v1/customer/54617"
            });
        }

        private static StringContent GetCustomerResponse()
        {
            return ContentHelper.CreateJsonStringContent(new
            {
                id = 54624,
                title = "Mr",
                firstName = "Peter",
                lastName = "Smith",
                dateOfBirth = "1970-04-25",
                languageCode = "en-GB",
                address = new
                {
                    line1 = "8 High Street",
                    line2 = (string?)null,
                    line3 = (string?)null,
                    town = "Luton",
                    county = "Bedfordshire",
                    postcode = "LT12 6TY",
                    countryCode = "UK",
                    countryName = "United Kingdom"
                }
            });
        }
    }
}