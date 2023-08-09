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

namespace PaymentStatementIntegrations.Tests.FfcCrmInsertTest
{
    /// <summary>
    /// Test cases for the <i>FfcCrmInsert</i> workflow.
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
                    if (request?.RequestUri != null)
                    {
                        if (request.RequestUri.AbsolutePath.Contains("/oauth2/token") && request.Method == HttpMethod.Post)
                        {
                            mockedResponse.RequestMessage = request;
                            mockedResponse.StatusCode = HttpStatusCode.OK;
                            mockedResponse.Content = ValidAuthToken();
                        }
                        else if (request.RequestUri.AbsolutePath.Contains("/api/data/v9.2/accounts") && request.Method == HttpMethod.Get)
                        {
                            mockedResponse.RequestMessage = request;
                            mockedResponse.StatusCode = HttpStatusCode.OK;
                            mockedResponse.Content = ValidOrgLookup();
                        }
                        else if (request.RequestUri.AbsolutePath.Contains("/api/data/v9.2/incidents") && request.Method == HttpMethod.Post)
                        {
                            mockedResponse.RequestMessage = request;
                            mockedResponse.StatusCode = HttpStatusCode.OK;
                            mockedResponse.Content = ValidCreateCase();
                        }
                        else if (request.RequestUri.AbsolutePath.Contains("/api/data/v9.2/rpa_customernotifications") && request.Method == HttpMethod.Post)
                        {
                            mockedResponse.RequestMessage = request;
                            mockedResponse.StatusCode = HttpStatusCode.OK;
                            mockedResponse.Content = ValidCreateNotification();
                        }
                        else if (request.RequestUri.AbsolutePath.Contains("/api/data/v9.2/rpa_activitymetadatas") && request.Method == HttpMethod.Post)
                        {
                            mockedResponse.RequestMessage = request;
                            mockedResponse.StatusCode = HttpStatusCode.OK;
                            mockedResponse.Content = ValidCreateMetadata();
                        }
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
                Assert.AreEqual(ActionStatus.Succeeded, testRunner.GetWorkflowActionStatus("Parse_Payload_JSON"));
                Assert.AreEqual(ActionStatus.Succeeded, testRunner.GetWorkflowActionStatus("If_JSON_is_valid"));
                Assert.AreEqual(ActionStatus.Succeeded, testRunner.GetWorkflowActionStatus("Get_CRM_Token"));
                Assert.AreEqual(ActionStatus.Succeeded, testRunner.GetWorkflowActionStatus("Parse_Token_Response"));
                Assert.AreEqual(ActionStatus.Succeeded, testRunner.GetWorkflowActionStatus("Extract_year"));
                Assert.AreEqual(ActionStatus.Succeeded, testRunner.GetWorkflowActionStatus("CRM_Lookup_Org"));
                Assert.AreEqual(ActionStatus.Succeeded, testRunner.GetWorkflowActionStatus("Parse_Organisation_Details"));
                Assert.AreEqual(ActionStatus.Succeeded, testRunner.GetWorkflowActionStatus("Extract_Org_Id"));
                Assert.AreEqual(ActionStatus.Succeeded, testRunner.GetWorkflowActionStatus("CRM_Create_Case"));
                Assert.AreEqual(ActionStatus.Succeeded, testRunner.GetWorkflowActionStatus("Extract_case_id"));
                Assert.AreEqual(ActionStatus.Succeeded, testRunner.GetWorkflowActionStatus("CRM_Create_Notification_Activity"));
                Assert.AreEqual(ActionStatus.Succeeded, testRunner.GetWorkflowActionStatus("Extract_activity_id"));
                Assert.AreEqual(ActionStatus.Succeeded, testRunner.GetWorkflowActionStatus("CRM_Create_Meta_Data"));
                Assert.AreEqual(ActionStatus.Skipped, testRunner.GetWorkflowActionStatus("Dead-letter_the_message"));
                Assert.AreEqual(ActionStatus.Succeeded, testRunner.GetWorkflowActionStatus("Complete_the_message"));

                // Check request to CRM for 'create metadata'
                var crmRequest = testRunner.MockRequests.First(r => r.RequestUri.AbsolutePath.Contains("/api/data/v9.2/rpa_activitymetadatas"));
                Assert.AreEqual(HttpMethod.Post, crmRequest.Method);
                Assert.IsTrue(crmRequest.Content.Contains("\"rpa_filename\":\"FFC_PaymentStatement_SFI_2022_1234567890_2022090615023001.pdf\""));

                // Check tracked properties
                var trackedProps = testRunner.GetWorkflowActionTrackedProperties("Initialize_Progress");
                Assert.AreEqual("FfcCrmInsert", trackedProps["WorkflowName"]);
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
                    if (request?.RequestUri != null)
                    {
                        if (request.RequestUri.AbsolutePath.Contains("/oauth2/token") && request.Method == HttpMethod.Post)
                        {
                            // Email auth token
                            mockedResponse.RequestMessage = request;
                            mockedResponse.StatusCode = HttpStatusCode.OK;
                            mockedResponse.Content = ValidAuthToken();
                        }
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
                Assert.AreEqual(ActionStatus.Failed, testRunner.GetWorkflowActionStatus("Parse_Payload_JSON"));
                Assert.AreEqual(ActionStatus.Skipped, testRunner.GetWorkflowActionStatus("If_JSON_is_valid"));
                Assert.AreEqual(ActionStatus.Skipped, testRunner.GetWorkflowActionStatus("Get_CRM_Token"));
                Assert.AreEqual(ActionStatus.Skipped, testRunner.GetWorkflowActionStatus("Parse_Token_Response"));
                Assert.AreEqual(ActionStatus.Skipped, testRunner.GetWorkflowActionStatus("Extract_year"));
                Assert.AreEqual(ActionStatus.Skipped, testRunner.GetWorkflowActionStatus("CRM_Lookup_Org"));
                Assert.AreEqual(ActionStatus.Skipped, testRunner.GetWorkflowActionStatus("Parse_Organisation_Details"));
                Assert.AreEqual(ActionStatus.Skipped, testRunner.GetWorkflowActionStatus("Extract_Org_Id"));
                Assert.AreEqual(ActionStatus.Skipped, testRunner.GetWorkflowActionStatus("CRM_Create_Case"));
                Assert.AreEqual(ActionStatus.Skipped, testRunner.GetWorkflowActionStatus("Extract_case_id"));
                Assert.AreEqual(ActionStatus.Skipped, testRunner.GetWorkflowActionStatus("CRM_Create_Notification_Activity"));
                Assert.AreEqual(ActionStatus.Skipped, testRunner.GetWorkflowActionStatus("Extract_activity_id"));
                Assert.AreEqual(ActionStatus.Skipped, testRunner.GetWorkflowActionStatus("CRM_Create_Meta_Data"));
                Assert.AreEqual(ActionStatus.Succeeded, testRunner.GetWorkflowActionStatus("Dead-letter_the_message"));
                Assert.AreEqual(ActionStatus.Succeeded, testRunner.GetWorkflowActionStatus("Send_error_email"));
                Assert.AreEqual(ActionStatus.Skipped, testRunner.GetWorkflowActionStatus("Complete_the_message"));

                // Check request to CRM via Dataserve connector never happened
                var crmRequest = testRunner.MockRequests.FirstOrDefault(r => r.RequestUri.AbsolutePath.Contains("/api/data/v9.1"));
                Assert.IsNull(crmRequest);

                // Check tracked properties
                var trackedProps = testRunner.GetWorkflowActionTrackedProperties("Initialize_Progress");
                Assert.AreEqual("FfcCrmInsert", trackedProps["WorkflowName"]);
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
                    if (request?.RequestUri != null)
                    {
                        if (request.RequestUri.AbsolutePath.Contains("/oauth2/token") && request.Method == HttpMethod.Post)
                        {
                            // Email auth token
                            mockedResponse.RequestMessage = request;
                            mockedResponse.StatusCode = HttpStatusCode.OK;
                            mockedResponse.Content = ValidAuthToken();
                        }
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
                Assert.AreEqual(ActionStatus.Succeeded, testRunner.GetWorkflowActionStatus("Parse_Payload_JSON"));
                Assert.AreEqual(ActionStatus.Failed, testRunner.GetWorkflowActionStatus("If_JSON_is_valid"));
                Assert.AreEqual(ActionStatus.Skipped, testRunner.GetWorkflowActionStatus("Get_CRM_Token"));
                Assert.AreEqual(ActionStatus.Skipped, testRunner.GetWorkflowActionStatus("Parse_Token_Response"));
                Assert.AreEqual(ActionStatus.Skipped, testRunner.GetWorkflowActionStatus("Extract_year"));
                Assert.AreEqual(ActionStatus.Skipped, testRunner.GetWorkflowActionStatus("CRM_Lookup_Org"));
                Assert.AreEqual(ActionStatus.Skipped, testRunner.GetWorkflowActionStatus("Parse_Organisation_Details"));
                Assert.AreEqual(ActionStatus.Skipped, testRunner.GetWorkflowActionStatus("Extract_Org_Id"));
                Assert.AreEqual(ActionStatus.Skipped, testRunner.GetWorkflowActionStatus("CRM_Create_Case"));
                Assert.AreEqual(ActionStatus.Skipped, testRunner.GetWorkflowActionStatus("Extract_case_id"));
                Assert.AreEqual(ActionStatus.Skipped, testRunner.GetWorkflowActionStatus("CRM_Create_Notification_Activity"));
                Assert.AreEqual(ActionStatus.Skipped, testRunner.GetWorkflowActionStatus("Extract_activity_id"));
                Assert.AreEqual(ActionStatus.Skipped, testRunner.GetWorkflowActionStatus("CRM_Create_Meta_Data"));
                Assert.AreEqual(ActionStatus.Succeeded, testRunner.GetWorkflowActionStatus("Dead-letter_the_message"));
                Assert.AreEqual(ActionStatus.Succeeded, testRunner.GetWorkflowActionStatus("Send_error_email"));
                Assert.AreEqual(ActionStatus.Skipped, testRunner.GetWorkflowActionStatus("Complete_the_message"));

                // Check request to CRM via Dataserve connector never happened
                var crmRequest = testRunner.MockRequests.FirstOrDefault(r => r.RequestUri.AbsolutePath.Contains("/api/data/v9.1"));
                Assert.IsNull(crmRequest);

                // Check tracked properties
                var trackedProps = testRunner.GetWorkflowActionTrackedProperties("Initialize_Progress");
                Assert.AreEqual("FfcCrmInsert", trackedProps["WorkflowName"]);
            }
        }

        /// <summary>
        /// Tests that the correct response is returned when successful.
        /// </summary>
        [TestMethod]
        public void CrmInsertTest_Fails_When_Failed_CRM_Calls()
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
                        else if (request.RequestUri.AbsolutePath.Contains("/api/data/v9.2/"))
                        {
                            // As this is a connector (and not an action HTTP call), the Unit Test Framework
                            // will not automatically remove the retry policy. Therefore we must use a code that is not 408, 429 or 5xx
                            // so BadRequest (400) is chosen to ensure the unit test completes without long retries.
                            mockedResponse.RequestMessage = request;
                            mockedResponse.StatusCode = HttpStatusCode.BadRequest;
                            mockedResponse.Content = ContentHelper.CreatePlainStringContent("bad request specific error");
                        }
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
                Assert.AreEqual(ActionStatus.Succeeded, testRunner.GetWorkflowActionStatus("Parse_Payload_JSON"));
                Assert.AreEqual(ActionStatus.Succeeded, testRunner.GetWorkflowActionStatus("If_JSON_is_valid"));
                Assert.AreEqual(ActionStatus.Succeeded, testRunner.GetWorkflowActionStatus("Get_CRM_Token"));
                Assert.AreEqual(ActionStatus.Succeeded, testRunner.GetWorkflowActionStatus("Parse_Token_Response"));
                Assert.AreEqual(ActionStatus.Succeeded, testRunner.GetWorkflowActionStatus("Extract_year"));
                Assert.AreEqual(ActionStatus.Failed, testRunner.GetWorkflowActionStatus("CRM_Lookup_Org"));
                Assert.AreEqual(ActionStatus.Skipped, testRunner.GetWorkflowActionStatus("Parse_Organisation_Details"));
                Assert.AreEqual(ActionStatus.Skipped, testRunner.GetWorkflowActionStatus("Extract_Org_Id"));
                Assert.AreEqual(ActionStatus.Skipped, testRunner.GetWorkflowActionStatus("CRM_Create_Case"));
                Assert.AreEqual(ActionStatus.Skipped, testRunner.GetWorkflowActionStatus("Extract_case_id"));
                Assert.AreEqual(ActionStatus.Skipped, testRunner.GetWorkflowActionStatus("CRM_Create_Notification_Activity"));
                Assert.AreEqual(ActionStatus.Skipped, testRunner.GetWorkflowActionStatus("Extract_activity_id"));
                Assert.AreEqual(ActionStatus.Skipped, testRunner.GetWorkflowActionStatus("CRM_Create_Meta_Data"));
                Assert.AreEqual(ActionStatus.Succeeded, testRunner.GetWorkflowActionStatus("Dead-letter_the_message"));
                Assert.AreEqual(ActionStatus.Succeeded, testRunner.GetWorkflowActionStatus("Send_error_email"));
                Assert.AreEqual(ActionStatus.Skipped, testRunner.GetWorkflowActionStatus("Complete_the_message"));

                // Check request to CRM
                var crmRequest = testRunner.MockRequests.First(r => r.RequestUri.AbsolutePath.Contains("/api/data/v9.2/accounts"));
                Assert.AreEqual(HttpMethod.Get, crmRequest.Method);
                Assert.AreEqual(string.Empty, crmRequest.Content);

                // Check tracked properties
                var trackedProps = testRunner.GetWorkflowActionTrackedProperties("Initialize_Progress");
                Assert.AreEqual("FfcCrmInsert", trackedProps["WorkflowName"]);
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

        private static StringContent ValidOrgLookup()
        {
            // Since a property beginning with '@' cannot be defined in an anonymous type,
            // this JSON is created using strings

            var jsonStr = "{ \"value\": [ { \"@odata.etag\": \"W162878835\", \"name\": \"1 Frog Hall\", \"accountid\": \"df1f5e8a-a175-e411-9411-00155deb6487\" } ] }";

            return new StringContent(jsonStr, Encoding.UTF8, "application/json");
        }

        private static StringContent ValidCreateCase()
        {
            var json = new
            {
                incidentid = "6e2fc685-1e2d-ee11-bdf4-000d3adf3558"
            };

            return UnitTestHelper.EncodeAsStringContent(json);
        }

        private static StringContent ValidCreateNotification()
        {
            var json = new
            {
                activityid = "70e5a58b-1e2d-ee11-bdf4-002248a28b4d"
            };

            return UnitTestHelper.EncodeAsStringContent(json);
        }

        private static StringContent ValidCreateMetadata()
        {
            // Return value not used, so blank is ok here
            var json = new
            {
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
    }
}