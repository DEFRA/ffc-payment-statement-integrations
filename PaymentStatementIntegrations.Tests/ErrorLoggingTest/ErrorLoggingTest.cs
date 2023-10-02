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

namespace PaymentStatementIntegrations.Tests.ErrorLoggingTest
{
    /// <summary>
    /// Test cases for the <i>ErrorLogging</i> workflow.
    /// </summary>
    [TestClass]
    public class ErrorLoggingTest : WorkflowTestBase
    {
        [TestInitialize]
        public void TestInitialize()
        {
            Initialize(Constants.LOGIC_APP_BASE_PATH, "ErrorLogging");
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
        public void LogError_Masks_Sensitive_Data()
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
                        if (request.RequestUri.AbsolutePath.Contains("/oauth2/token") && request.Method == HttpMethod.Post)
                        {
                            mockedResponse.RequestMessage = request;
                            mockedResponse.StatusCode = HttpStatusCode.OK;
                            mockedResponse.Content = ValidAuthToken();
                        }
                        else if (request.RequestUri.AbsolutePath.Contains("/api/data/v9.2/rpa_integrationinboundqueues") && request.Method == HttpMethod.Get)
                        {
                            mockedResponse.RequestMessage = request;
                            mockedResponse.StatusCode = HttpStatusCode.OK;
                            mockedResponse.Content = ValidCreateCrmLogRecord();
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
                Assert.AreEqual(ActionStatus.Succeeded, testRunner.GetWorkflowActionStatus("Get_CRM_Token"));
                Assert.AreEqual(ActionStatus.Succeeded, testRunner.GetWorkflowActionStatus("Parse_CRM_Token"));
                Assert.AreEqual(ActionStatus.Succeeded, testRunner.GetWorkflowActionStatus("Create_CRM_Error_Log"));

                // Check masking
                var outputs = testRunner.GetWorkflowActionOutput("Strip_sensitive_data");
                Assert.IsTrue(outputs.ToString().Contains("client_secret=******&"));
                Assert.IsTrue(outputs.ToString().Contains("client_id=******"));
                Assert.IsTrue(outputs.ToString().Contains("Authorization******"));
                Assert.IsFalse(outputs.ToString().Contains("SENSITIVE"));
            }
        }

        /// <summary>
        /// Tests that the correct response is processed.
        /// </summary>
        [TestMethod]
        public void LogError_Adds_StackTrace_And_Correct_Type()
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
                        if (request.RequestUri.AbsolutePath.Contains("/oauth2/token") && request.Method == HttpMethod.Post)
                        {
                            mockedResponse.RequestMessage = request;
                            mockedResponse.StatusCode = HttpStatusCode.OK;
                            mockedResponse.Content = ValidAuthToken();
                        }
                        else if (request.RequestUri.AbsolutePath.Contains("/api/data/v9.2/rpa_integrationinboundqueues") && request.Method == HttpMethod.Get)
                        {
                            mockedResponse.RequestMessage = request;
                            mockedResponse.StatusCode = HttpStatusCode.OK;
                            mockedResponse.Content = ValidCreateCrmLogRecord();
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
                Assert.AreEqual(ActionStatus.Succeeded, testRunner.GetWorkflowActionStatus("Get_CRM_Token"));
                Assert.AreEqual(ActionStatus.Succeeded, testRunner.GetWorkflowActionStatus("Parse_CRM_Token"));
                Assert.AreEqual(ActionStatus.Succeeded, testRunner.GetWorkflowActionStatus("Create_CRM_Error_Log"));

                // Check error message
                var crmRequest = testRunner.MockRequests.First(r => r.RequestUri.AbsolutePath.Contains("/api/data/v9.2/rpa_integrationinboundqueues"));
                Assert.AreEqual(HttpMethod.Post, crmRequest.Method);
                Assert.IsTrue(crmRequest.Content.Contains("Error text: we got this far \\\"Failure with client_secret"));
                Assert.IsTrue(crmRequest.Content.Contains("\"rpa_processingentity\":\"927350005"));
            }
        }

        private static StringContent GetWorkflowPayload()
        {
            var json = new
            {
                callingWorkflowName = "RleCrmInsert",
                workflowRunId = "ff421d65-5be6-4084-b748-af490100c9a5",
                progressText = "we got this far",
                stack = new []
                {
                    new { status = "Failed", 
                          error = new {
                            code = "ActionConditionFailed",
                            message = "Failure with client_secret=secret-SENSITIVE-data&Authorization:SENSITIVE-TOKEN}&client_id=12345SENSITIVE"
                          }
                    },
                    new { status = "Succeeded",
                          error  = new {
                            code = "",
                            message = ""
                        }
                    }
                }
            };

            return UnitTestHelper.EncodeAsStringContent(json);
        }

        private static StringContent ValidAuthToken()
        {
            var json = new
            {
                token_type = "Bearer",
                expires_in = "3599",
                ext_expires_in = "3599",
                expires_on = "1690535569",
                not_before = "1690531669",
                resource = "https://rpadevv9.crm4.dynamics.com",
                access_token = "eyJ0eXAiOiJKV1Qaaaaaaa"
            };

            return UnitTestHelper.EncodeAsStringContent(json);
        }

        private static StringContent ValidCreateCrmLogRecord()
        {
            // Return value not used, so blank is ok here
            var json = new
            {
            };

            return UnitTestHelper.EncodeAsStringContent(json);
        }

    }
}