using LogicAppUnit;
using LogicAppUnit.Helper;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using PaymentStatementIntegrations.Tests.Helpers;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;

namespace PaymentStatementIntegrations.Tests.FfcCrmInsertTest
{
    /// <summary>
    /// Test cases for the <i>CrmInsert</i> workflow.
    /// </summary>
    [TestClass]
    public class FfcCrmInsertTest : WorkflowTestBase
    {
        [TestInitialize]
        public void TestInitialize()
        {
            Initialize(Constants.LOGIC_APP_BASE_PATH, "FfcCrmInsert");
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
                testRunner.ExceptionWrapper(() => Assert.AreEqual(HttpStatusCode.Accepted, workflowResponse.StatusCode));
                Assert.AreEqual(HttpStatusCode.Accepted, workflowResponse.StatusCode);

                // Check action result
                Assert.AreEqual(ActionStatus.Succeeded, testRunner.GetWorkflowActionStatus("Parse_JSON"));
                //Assert.AreEqual(ActionStatus.Succeeded, testRunner.GetWorkflowActionStatus("Add_a_row_to_CRM"));
                Assert.AreEqual(ActionStatus.Skipped, testRunner.GetWorkflowActionStatus("Dead-letter_the_message_failed_CRM"));
                Assert.AreEqual(ActionStatus.Skipped, testRunner.GetWorkflowActionStatus("Dead-letter_the_message_invalid_JSON"));
                Assert.AreEqual(ActionStatus.Succeeded, testRunner.GetWorkflowActionStatus("Complete_the_message"));

                // Check request to CRM via Dataserve connector
                //var crmRequest = testRunner.MockRequests.First(r => r.RequestUri.AbsolutePath.Contains("/api/data/v9.1"));
                //Assert.AreEqual(HttpMethod.Post, crmRequest.Method);
                //Assert.IsTrue(crmRequest.Content.Contains("FFC_PaymentStatement_SFI_2022_1234567890_2022090615023001.pdf"));

                // Check tracked properties
                var trackedProps = testRunner.GetWorkflowActionTrackedProperties("Parse_JSON");
                var expectedBody = "{\"sbi\":12345678,\"frn\":123456789,\"apiLink\":\"https://myStatementRetrievalApiEndpoint/statement-receiver/statement/v1/FFC_PaymentStatement_SFI_2022_1234567890_2022090615023001.pdf\",\"documentType\":\"Payment statement\",\"scheme\":\"SFI\"}";
                Assert.AreEqual(expectedBody, trackedProps["messageBody"]);
            }
        }

        /// <summary>
        /// Tests that the correct response is returned when invalid JSON.
        /// </summary>
        [TestMethod]
        public void CrmInsertTest_Fails_When_Invalid_Schema_JSON()
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
                    GetServiceBusMessageInvalidSchemaJson(),
                    HttpMethod.Post);

                // Check workflow run status
                Assert.AreEqual(WorkflowRunStatus.Succeeded, testRunner.WorkflowRunStatus);

                // Check workflow response
                testRunner.ExceptionWrapper(() => Assert.AreEqual(HttpStatusCode.Accepted, workflowResponse.StatusCode));
                Assert.AreEqual(HttpStatusCode.Accepted, workflowResponse.StatusCode);

                // Check action result
                Assert.AreEqual(ActionStatus.Failed, testRunner.GetWorkflowActionStatus("Parse_JSON"));
                //Assert.AreEqual(ActionStatus.Skipped, testRunner.GetWorkflowActionStatus("Add_a_row_to_CRM"));
                Assert.AreEqual(ActionStatus.Skipped, testRunner.GetWorkflowActionStatus("Dead-letter_the_message_failed_CRM"));
                Assert.AreEqual(ActionStatus.Succeeded, testRunner.GetWorkflowActionStatus("Dead-letter_the_message_invalid_JSON"));
                Assert.AreEqual(ActionStatus.Skipped, testRunner.GetWorkflowActionStatus("Complete_the_message"));

                // Check request to CRM via Dataserve connector never happened
                var crmRequest = testRunner.MockRequests.FirstOrDefault(r => r.RequestUri.AbsolutePath.Contains("/api/data/v9.1"));
                Assert.IsNull(crmRequest);

                // Check tracked properties
                var trackedProps = testRunner.GetWorkflowActionTrackedProperties("Parse_JSON");
                var expectedBody = "{\"sbi2\":12345678,\"frn2\":123456789,\"apiLink2\":\"https://myStatementRetrievalApiEndpoint/statement-receiver/statement/v1/FFC_PaymentStatement_SFI_2022_1234567890_2022090615023001.pdf\",\"documentType2\":\"Payment statement\",\"scheme2\":\"SFI\",\"sbi\":\"defgh\"}";
                Assert.AreEqual(expectedBody, trackedProps["messageBody"]);
            }
        }

        /// <summary>
        /// Tests that the correct response is returned when invalid JSON.
        /// </summary>
        [TestMethod]
        public void CrmInsertTest_Fails_When_Missing_Items_In_JSON()
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
                    GetServiceBusMessageValidButMissingItemsJson(),
                    HttpMethod.Post);

                // Check workflow run status
                Assert.AreEqual(WorkflowRunStatus.Succeeded, testRunner.WorkflowRunStatus);

                // Check workflow response
                testRunner.ExceptionWrapper(() => Assert.AreEqual(HttpStatusCode.Accepted, workflowResponse.StatusCode));
                Assert.AreEqual(HttpStatusCode.Accepted, workflowResponse.StatusCode);

                // Check action result
                Assert.AreEqual(ActionStatus.Succeeded, testRunner.GetWorkflowActionStatus("Parse_JSON"));
                //Assert.AreEqual(ActionStatus.Skipped, testRunner.GetWorkflowActionStatus("Add_a_row_to_CRM"));
                Assert.AreEqual(ActionStatus.Skipped, testRunner.GetWorkflowActionStatus("Dead-letter_the_message_failed_CRM"));
                Assert.AreEqual(ActionStatus.Succeeded, testRunner.GetWorkflowActionStatus("Dead-letter_the_message_invalid_JSON"));
                Assert.AreEqual(ActionStatus.Skipped, testRunner.GetWorkflowActionStatus("Complete_the_message"));

                // Check request to CRM via Dataserve connector never happened
                var crmRequest = testRunner.MockRequests.FirstOrDefault(r => r.RequestUri.AbsolutePath.Contains("/api/data/v9.1"));
                Assert.IsNull(crmRequest);

                // Check tracked properties
                var trackedProps = testRunner.GetWorkflowActionTrackedProperties("Parse_JSON");
                var expectedBody = "{\"sbi\":12345678,\"frn\":123456789}";
                Assert.AreEqual(expectedBody, trackedProps["messageBody"]);
            }
        }

        /// <summary>
        /// Tests that the correct response is returned when successful.
        /// </summary>
        [TestMethod]
        public void CrmInsertTest_Fails_When_Failed_CRM_Insert()
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
                        // As this is a connector (and not an action HTTP call), the Unit Test Framework
                        // will not automatically remove the retry policy. Therefore we must use a code that is not 408, 429 or 5xx
                        // so BadRequest (400) is chosen to ensure the unit test completes without long retries.
                        mockedResponse.RequestMessage = request;
                        mockedResponse.StatusCode = HttpStatusCode.BadRequest;
                        mockedResponse.Content = ContentHelper.CreatePlainStringContent("bad request specific error");
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
                testRunner.ExceptionWrapper(() => Assert.AreEqual(HttpStatusCode.Accepted, workflowResponse.StatusCode));
                Assert.AreEqual(HttpStatusCode.Accepted, workflowResponse.StatusCode);

                // Check action result
                Assert.AreEqual(ActionStatus.Succeeded, testRunner.GetWorkflowActionStatus("Parse_JSON"));
                //Assert.AreEqual(ActionStatus.Failed, testRunner.GetWorkflowActionStatus("Add_a_row_to_CRM"));
                Assert.AreEqual(ActionStatus.Skipped, testRunner.GetWorkflowActionStatus("Dead-letter_the_message_invalid_JSON"));
                //Assert.AreEqual(ActionStatus.Succeeded, testRunner.GetWorkflowActionStatus("Dead-letter_the_message_failed_CRM"));
                //Assert.AreEqual(ActionStatus.Skipped, testRunner.GetWorkflowActionStatus("Complete_the_message"));

                // Check request to CRM via Dataserve connector
                //var crmRequest = testRunner.MockRequests.First(r => r.RequestUri.AbsolutePath.Contains("/api/data/v9.1"));
                //Assert.AreEqual(HttpMethod.Post, crmRequest.Method);
                //Assert.IsTrue(crmRequest.Content.Contains("FFC_PaymentStatement_SFI_2022_1234567890_2022090615023001.pdf"));
                //var crmRequest = testRunner.MockRequests.First(r => r.RequestUri.AbsolutePath.Contains("/api/data/v9.1"));
                //Assert.AreEqual(HttpMethod.Post, crmRequest.Method);
                //Assert.IsTrue(crmRequest.Content.Contains("FFC_PaymentStatement_SFI_2022_1234567890_2022090615023001.pdf"));

                // Check tracked properties
                var trackedProps = testRunner.GetWorkflowActionTrackedProperties("Parse_JSON");
                var expectedBody = "{\"sbi\":12345678,\"frn\":123456789,\"apiLink\":\"https://myStatementRetrievalApiEndpoint/statement-receiver/statement/v1/FFC_PaymentStatement_SFI_2022_1234567890_2022090615023001.pdf\",\"documentType\":\"Payment statement\",\"scheme\":\"SFI\"}";
                Assert.AreEqual(expectedBody, trackedProps["messageBody"]);
                //var trackedPropsError = testRunner.GetWorkflowActionTrackedProperties("Compose_for_logging");
                //Assert.IsTrue(trackedPropsError["ErrorText"].Contains("BadRequest"));
                //var trackedPropsError = testRunner.GetWorkflowActionTrackedProperties("Compose_for_logging");
                //Assert.IsTrue(trackedPropsError["ErrorText"].Contains("BadRequest"));
            }
        }

        private static StringContent GetServiceBusMessage()
        {
            var json = new
            {
                // The JSON must match the data structure used by the Service Bus trigger, this includes 'contentData' to represent the message content
                contentData = new
                {
                    sbi = 12345678,
                    frn = 123456789,
                    apiLink = "https://myStatementRetrievalApiEndpoint/statement-receiver/statement/v1/FFC_PaymentStatement_SFI_2022_1234567890_2022090615023001.pdf",
                    documentType = "Payment statement",
                    scheme = "SFI",
                },
                contentType = "application/json",
                messageId = "ff421d65-5be6-4084-b748-af490100c9a5",
                label = "customer.54624",
                scheduledEnqueueTimeUtc = "1/1/0001 12:00:00 AM",
                sessionId = "54624",
                timeToLive = "06:00:00",
                deliveryCount = 1,
                enqueuedSequenceNumber = 6825,
                enqueuedTimeUtc = "2022-11-10T15:34:57.727Z",
                lockedUntilUtc = "9999-12-31T23:59:59.9999999Z",
                lockToken = "056bb9fa-9b8f-4d93-874b-7e78e71a588d",
                sequenceNumber = 980
            };

            return UnitTestHelper.EncodeAsStringContent(json);
        }

        private static StringContent GetServiceBusMessageInvalidSchemaJson()
        {
            var json = new
            {
                // The JSON must match the data structure used by the Service Bus trigger, this includes 'contentData' to represent the message content
                contentData = new
                {
                    sbi2 = 12345678,
                    frn2 = 123456789,
                    apiLink2 = "https://myStatementRetrievalApiEndpoint/statement-receiver/statement/v1/FFC_PaymentStatement_SFI_2022_1234567890_2022090615023001.pdf",
                    documentType2 = "Payment statement",
                    scheme2 = "SFI",
                    sbi = "defgh"
                },
                contentType = "application/json",
                messageId = "ff421d65-5be6-4084-b748-af490100c9a5",
                label = "customer.54624",
                scheduledEnqueueTimeUtc = "1/1/0001 12:00:00 AM",
                sessionId = "54624",
                timeToLive = "06:00:00",
                deliveryCount = 1,
                enqueuedSequenceNumber = 6825,
                enqueuedTimeUtc = "2022-11-10T15:34:57.727Z",
                lockedUntilUtc = "9999-12-31T23:59:59.9999999Z",
                lockToken = "056bb9fa-9b8f-4d93-874b-7e78e71a588d",
                sequenceNumber = 980
            };

            return UnitTestHelper.EncodeAsStringContent(json);
        }

        private static StringContent GetServiceBusMessageValidButMissingItemsJson()
        {
            var json = new
            {
                // The JSON must match the data structure used by the Service Bus trigger, this includes 'contentData' to represent the message content
                contentData = new
                {
                    sbi = 12345678,
                    frn = 123456789
                },
                contentType = "application/json",
                messageId = "ff421d65-5be6-4084-b748-af490100c9a5",
                label = "customer.54624",
                scheduledEnqueueTimeUtc = "1/1/0001 12:00:00 AM",
                sessionId = "54624",
                timeToLive = "06:00:00",
                deliveryCount = 1,
                enqueuedSequenceNumber = 6825,
                enqueuedTimeUtc = "2022-11-10T15:34:57.727Z",
                lockedUntilUtc = "9999-12-31T23:59:59.9999999Z",
                lockToken = "056bb9fa-9b8f-4d93-874b-7e78e71a588d",
                sequenceNumber = 980
            };

            return UnitTestHelper.EncodeAsStringContent(json);
        }
    }
}