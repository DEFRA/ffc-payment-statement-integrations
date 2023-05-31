using LogicAppUnit;
using LogicAppUnit.Helper;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;

namespace PaymentStatementIntegrations.Tests.CrmInsertTest
{
    /// <summary>
    /// Test cases for the <i>CrmInsert</i> workflow.
    /// </summary>
    [TestClass]
    public class CrmInsertTest : WorkflowTestBase
    {
        [TestInitialize]
        public void TestInitialize()
        {
            Initialize(Constants.LOGIC_APP_BASE_PATH, "CrmInsert");
        }

        [ClassCleanup]
        public static void CleanResources()
        {
            Close();
        }

        /// <summary>
        /// Tests that the correct response is returned when successful.
        /// </summary>
        [TestMethod]
        public void CrmInsertTest_When_Successful()
        {
            // Override one of the settings in the local settings file
            var settingsToOverride = new Dictionary<string, string>();

            using (ITestRunner testRunner = CreateTestRunner(settingsToOverride))
            {
                // Mock the HTTP calls and customize responses
                testRunner.AddApiMocks = (request) =>
                {
                    HttpResponseMessage mockedResponse = new HttpResponseMessage();
                    if (request?.RequestUri != null && request.RequestUri.AbsolutePath.Contains("/api/data/v9.1/") && request.Method == HttpMethod.Post)
                    {
                        mockedResponse.RequestMessage = request;
                        mockedResponse.StatusCode = HttpStatusCode.OK;
                        mockedResponse.Content = ContentHelper.CreatePlainStringContent("success");
                    }
                    return mockedResponse;
                };

                // Run the workflow
                var workflowResponse = testRunner.TriggerWorkflow(
                    GetServiceBusMessage(),
                    HttpMethod.Post);

                // Check workflow run status
                Assert.AreEqual(WorkflowRunStatus.Succeeded, testRunner.WorkflowRunStatus);

                // Check workflow response
                testRunner.ExceptionWrapper(() => Assert.AreEqual(HttpStatusCode.OK, workflowResponse.StatusCode));
                Assert.AreEqual("Success", workflowResponse.Content.ReadAsStringAsync().Result);

                // Check action result
                Assert.AreEqual(ActionStatus.Succeeded, testRunner.GetWorkflowActionStatus("Add_a_row_to_CRM"));

                // Check request to CRM via Dataserve connector
                var crmRequest = testRunner.MockRequests.First(r => r.RequestUri.AbsolutePath.Contains("/api/data/v9.1"));
                Assert.AreEqual(HttpMethod.Post, crmRequest.Method);
                Assert.IsTrue(crmRequest.Content.Contains("FFC_PaymentStatement_SFI_2022_1234567890_2022090615023001.pdf"));
            }
        }

        private static StringContent GetServiceBusMessage()
        {
            return ContentHelper.CreateJsonStringContent(
                "{" +
                    "\"body\": [{ " +
                        "\"contentData\": { " +
                            "\"sbi\": 12345678, " +
                            "\"frn\": 123456789, " +
                            "\"apiLink\": \"https://myStatementRetrievalApiEndpoint/statement-receiver/statement/v1/FFC_PaymentStatement_SFI_2022_1234567890_2022090615023001.pdf\", " +
                            "\"documentType\": \"Payment statement\", " + 
                            "\"scheme\": \"SFI\" " +
                        "}" +
                    "}]" +
                "}"
            );
        }
    }
}