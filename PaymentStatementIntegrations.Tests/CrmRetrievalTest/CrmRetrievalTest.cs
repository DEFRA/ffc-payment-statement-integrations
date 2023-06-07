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
        private const string _ExamplePdfFile = "FFC_PaymentStatement_SFI_2022_1234567890_2023020911105608.pdf";

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
        /// Tests that the correct response is returned when the filename is missing from the request.
        /// </summary>
        [TestMethod]
        public void CrmRetrievalTest_When_Missing_Param_In_Request()
        {
            using (ITestRunner testRunner = CreateTestRunner())
            {
                // Mock the HTTP calls and customize responses
                testRunner.AddApiMocks = (request) =>
                {
                    HttpResponseMessage mockedResponse = new HttpResponseMessage();
                    if (request.RequestUri?.AbsolutePath == "/api/v1/statements/statement/" && request.Method == HttpMethod.Get)
                    {
                        mockedResponse.RequestMessage = request;
                        mockedResponse.StatusCode = HttpStatusCode.NotFound;
                        mockedResponse.Content = ContentHelper.CreatePlainStringContent("PDF not found");
                    }
                    return mockedResponse;
                };
                // Run the workflow
                var workflowResponse = testRunner.TriggerWorkflow(
                    GetParamsWithIdMissing(),
                    HttpMethod.Get);

                // Check workflow run status
                Assert.AreEqual(WorkflowRunStatus.Succeeded, testRunner.WorkflowRunStatus);

                // Check workflow response
                testRunner.ExceptionWrapper(() => Assert.AreEqual(HttpStatusCode.NotFound, workflowResponse.StatusCode));
                Assert.AreEqual("Unable to get PDF content: PDF not found", workflowResponse.Content.ReadAsStringAsync().Result);
                Assert.AreEqual("text/plain; charset=utf-8", workflowResponse.Content.Headers.ContentType?.ToString());

                // Check action result
                Assert.AreEqual(ActionStatus.Succeeded, testRunner.GetWorkflowActionStatus("Failed_Response"));
                Assert.AreEqual(ActionStatus.Skipped, testRunner.GetWorkflowActionStatus("Serve_PDF"));
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
                    if (request.RequestUri?.AbsolutePath == $"/api/v1/statements/statement/{_ExamplePdfFile}" && request.Method == HttpMethod.Get)
                    {
                        mockedResponse.RequestMessage = request;
                        mockedResponse.StatusCode = HttpStatusCode.InternalServerError;
                        mockedResponse.Content = ContentHelper.CreatePlainStringContent("Internal server error detected in PDF Service");
                    }
                    return mockedResponse;
                };

                // Run the workflow
                var workflowResponse = testRunner.TriggerWorkflow(
                    GetValidParams(),
                    HttpMethod.Get);

                // Check workflow run status
                Assert.AreEqual(WorkflowRunStatus.Succeeded, testRunner.WorkflowRunStatus);

                // Check workflow response
                testRunner.ExceptionWrapper(() => Assert.AreEqual(HttpStatusCode.InternalServerError, workflowResponse.StatusCode));
                Assert.AreEqual("Unable to get PDF content: Internal server error detected in PDF Service", workflowResponse.Content.ReadAsStringAsync().Result);
                Assert.AreEqual("text/plain; charset=utf-8", workflowResponse.Content.Headers.ContentType?.ToString());

                // Check action result
                Assert.AreEqual(ActionStatus.Skipped, testRunner.GetWorkflowActionStatus("Serve_PDF"));
                Assert.AreEqual(ActionStatus.Succeeded, testRunner.GetWorkflowActionStatus("Failed_Response"));
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
                    if (request.RequestUri?.AbsolutePath == $"/api/v1/statements/statement/{_ExamplePdfFile}" && request.Method == HttpMethod.Get)
                    {
                        mockedResponse.RequestMessage = request;
                        mockedResponse.StatusCode = HttpStatusCode.OK;
                        mockedResponse.Content = GetPdfResponse();
                    }
                    return mockedResponse;
                };

                // Run the workflow
                var workflowResponse = testRunner.TriggerWorkflow(
                    GetValidParams(),
                    HttpMethod.Get);

                // Check workflow run status
                Assert.AreEqual(WorkflowRunStatus.Succeeded, testRunner.WorkflowRunStatus);

                // Check workflow response
                testRunner.ExceptionWrapper(() => Assert.AreEqual(HttpStatusCode.OK, workflowResponse.StatusCode));
                Assert.AreEqual("Some dummy PDF content", workflowResponse.Content.ReadAsStringAsync().Result);
                Assert.AreEqual("application/pdf", workflowResponse.Content.Headers.ContentType?.ToString());

                // Check action result
                Assert.AreEqual(ActionStatus.Succeeded, testRunner.GetWorkflowActionStatus("Get_PDF_from_service"));
                Assert.AreEqual(ActionStatus.Skipped, testRunner.GetWorkflowActionStatus("Failed_Response"));
                Assert.AreEqual(ActionStatus.Succeeded, testRunner.GetWorkflowActionStatus("Serve_PDF"));

                // Check request to PDF Server
                var pdfServerRequest = testRunner.MockRequests.First(r => r.RequestUri.AbsolutePath == $"/api/v1/statements/statement/{_ExamplePdfFile}");
                Assert.AreEqual(HttpMethod.Get, pdfServerRequest.Method);
            }
        }

        private static Dictionary<string, string> GetValidParams()
        {
            var dict = new Dictionary<string, string>();
            dict.Add("id", _ExamplePdfFile);
            return dict;
        }

        private static Dictionary<string, string> GetParamsWithIdMissing()
        {
            var dict = new Dictionary<string, string>();
            dict.Add("id2", _ExamplePdfFile);
            return dict;
        }

        private static StringContent GetPdfResponse()
        {
            return new StringContent("Some dummy PDF content");
        }
    }
}