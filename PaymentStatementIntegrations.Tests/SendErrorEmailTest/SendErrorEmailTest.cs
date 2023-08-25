using LogicAppUnit;
using LogicAppUnit.Helper;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using PaymentStatementIntegrations.Tests.Helpers;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;

namespace PaymentStatementIntegrations.Tests.SendErrorEmailTest
{
    /// <summary>
    /// Test cases for the <i>SendErrorEmail</i> workflow.
    /// </summary>
    [TestClass]
    public class SendErrorEmailTest : WorkflowTestBase
    {
        [TestInitialize]
        public void TestInitialize()
        {
            Initialize(Constants.LOGIC_APP_BASE_PATH, "SendErrorEmail");
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
        public void SendErrorEmail_Masks_Sensitive_Data()
        {
            // Override one of the settings in the local settings file
            var settingsToOverride = new Dictionary<string, string>();

            using (ITestRunner testRunner = CreateTestRunner(settingsToOverride))
            {
                // Mock the HTTP calls and customize responses
                testRunner.AddApiMocks = (request) =>
                {
                    HttpResponseMessage mockedResponse = new HttpResponseMessage();
                    if (request?.RequestUri != null)
                    {
                        if (request.RequestUri.AbsolutePath.Contains("/api/Email") && request.Method == HttpMethod.Post)
                        {
                            // Email function call
                            mockedResponse.RequestMessage = request;
                            mockedResponse.StatusCode = HttpStatusCode.OK;
                        }
                    }
                    return mockedResponse;
                };

                // Run the workflow
                var workflowResponse = testRunner.TriggerWorkflow(
                    GetWorkflowPayload(),
                    HttpMethod.Post);

                // Check workflow run status
                Assert.AreEqual(WorkflowRunStatus.Succeeded, testRunner.WorkflowRunStatus);

                // Check workflow response
                testRunner.ExceptionWrapper(() => Assert.AreEqual(HttpStatusCode.OK, workflowResponse.StatusCode));
                Assert.AreEqual(HttpStatusCode.OK, workflowResponse.StatusCode);

                // Check action result
                Assert.AreEqual(ActionStatus.Succeeded, testRunner.GetWorkflowActionStatus("Get_error_stack"));
                Assert.AreEqual(ActionStatus.Succeeded, testRunner.GetWorkflowActionStatus("Strip_sensitive_data"));
                Assert.AreEqual(ActionStatus.Succeeded, testRunner.GetWorkflowActionStatus("Send_email"));

                // Check masking
                var outputs = testRunner.GetWorkflowActionOutput("Strip_sensitive_data");
                Assert.IsTrue(outputs.ToString().Contains("client_secret=******&"));
                Assert.IsTrue(outputs.ToString().Contains("client_id=******"));
                Assert.IsTrue(outputs.ToString().Contains("Authorization******"));
                Assert.IsFalse(outputs.ToString().Contains("SENSITIVE"));
            }
        }

        private static StringContent GetWorkflowPayload()
        {
            var json = new
            {
                callingWorkflowName = "RleCrmInsert",
                workflowRunId = "ff421d65-5be6-4084-b748-af490100c9a5",
                progressText = "we got this far",
                apiKey = "apiKey",
                stack = new []
                {
                    new { status = "Failed", otherText = "otherTextFailure with client_secret=secret-SENSITIVE-data&Authorization:SENSITIVE-TOKEN}&client_id=12345SENSITIVE" },
                    new { status = "Succeeded", otherText = "Succeeded" }
                }
            };

            return UnitTestHelper.EncodeAsStringContent(json);
        }
    }
}