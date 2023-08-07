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

namespace PaymentStatementIntegrations.Tests.RleCrmInsertTest
{
    /// <summary>
    /// Test cases for the <i>RleCrmInsert</i> workflow.
    /// </summary>
    [TestClass]
    public class RleCrmInsertTest : WorkflowTestBase
    {
        [TestInitialize]
        public void TestInitialize()
        {
            Initialize(Constants.LOGIC_APP_BASE_PATH, "RleCrmInsert");
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
                        if (request.RequestUri.AbsolutePath.Contains("/Read_CTL_blob_contents") && request.Method == HttpMethod.Post)
                        {
                            mockedResponse.RequestMessage = request;
                            mockedResponse.StatusCode = HttpStatusCode.OK;
                            mockedResponse.Content = GetBlobControlFile();
                        }
                        else if (request.RequestUri.AbsolutePath.Contains("/oauth2/token") && request.Method == HttpMethod.Post)
                        {
                            // CRM token
                            mockedResponse.RequestMessage = request;
                            mockedResponse.StatusCode = HttpStatusCode.OK;
                            mockedResponse.Content = ValidAuthToken();
                        }
                        else if (request.RequestUri.AbsolutePath.Contains("/tokens/OAuth/2") && request.Method == HttpMethod.Post)
                        {
                            // Sharepoint token
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
                        else if (request.RequestUri.AbsolutePath.Contains("/api/data/v9.2/contacts") && request.Method == HttpMethod.Get)
                        {
                            mockedResponse.RequestMessage = request;
                            mockedResponse.StatusCode = HttpStatusCode.OK;
                            mockedResponse.Content = ValidContactLookup();
                        }
                        else if (request.RequestUri.AbsolutePath.Contains("/api/data/v9.2/incidents") && request.Method == HttpMethod.Post)
                        {
                            mockedResponse.RequestMessage = request;
                            mockedResponse.StatusCode = HttpStatusCode.OK;
                            mockedResponse.Content = ValidCreateCase();
                        }
                        else if (request.RequestUri.AbsolutePath.Contains("/api/data/v9.2/rpa_onlinesubmissions") && request.Method == HttpMethod.Post)
                        {
                            mockedResponse.RequestMessage = request;
                            mockedResponse.StatusCode = HttpStatusCode.OK;
                            mockedResponse.Content = ValidCreateOnlineSubmission();
                        }
                        else if (request.RequestUri.AbsolutePath.Contains("/api/data/v9.2/rpa_activitymetadatas") && request.Method == HttpMethod.Post)
                        {
                            mockedResponse.RequestMessage = request;
                            mockedResponse.StatusCode = HttpStatusCode.OK;
                            mockedResponse.Content = ValidCreateMetadata();
                        }
                        else if (request.RequestUri.AbsolutePath.Contains("/_api/web/folders") && request.Method == HttpMethod.Post)
                        {
                            // Sharepoint - create folder
                            mockedResponse.RequestMessage = request;
                            mockedResponse.StatusCode = HttpStatusCode.OK;
                            mockedResponse.Content = new StringContent(string.Empty);
                        }
                        else if (request.RequestUri.AbsolutePath.Contains("/Read_blob_content") && request.Method == HttpMethod.Post)
                        {
                            mockedResponse.RequestMessage = request;
                            mockedResponse.StatusCode = HttpStatusCode.OK;
                            mockedResponse.Content = GetBlobFile();
                        }
                        else if (request.RequestUri.AbsolutePath.Contains("/_api/web/GetFolderByServerRelativeUrl") && request.RequestUri.AbsolutePath.Contains("/Files/add") && request.Method == HttpMethod.Post)
                        {
                            // Sharepoint - upload file
                            mockedResponse.RequestMessage = request;
                            mockedResponse.StatusCode = HttpStatusCode.OK;
                            mockedResponse.Content = GetSharepointUploadResponse();
                        }
                    }
                    return mockedResponse;
                };

                // Run the workflow
                var workflowResponse = testRunner.TriggerWorkflow(HttpMethod.Post);

                // Check workflow run status
                Assert.AreEqual(WorkflowRunStatus.Succeeded, testRunner.WorkflowRunStatus);

                // Check workflow response
                testRunner.ExceptionWrapper(() => Assert.AreEqual(HttpStatusCode.Accepted, workflowResponse.StatusCode));
                Assert.AreEqual(HttpStatusCode.Accepted, workflowResponse.StatusCode);

                // Check action result
                Assert.AreEqual(ActionStatus.Succeeded, testRunner.GetWorkflowActionStatus("Read_CTL_blob_contents"));
                Assert.AreEqual(ActionStatus.Succeeded, testRunner.GetWorkflowActionStatus("Parse_CTL_File"));
                Assert.AreEqual(ActionStatus.Succeeded, testRunner.GetWorkflowActionStatus("Get_CRM_Token"));
                Assert.AreEqual(ActionStatus.Succeeded, testRunner.GetWorkflowActionStatus("Parse_CRM_Token"));
                Assert.AreEqual(ActionStatus.Succeeded, testRunner.GetWorkflowActionStatus("Get_CRM_Org"));
                Assert.AreEqual(ActionStatus.Succeeded, testRunner.GetWorkflowActionStatus("Parse_Org_Response"));
                Assert.AreEqual(ActionStatus.Succeeded, testRunner.GetWorkflowActionStatus("Set_OrganisationId"));
                Assert.AreEqual(ActionStatus.Succeeded, testRunner.GetWorkflowActionStatus("Get_CRM_Contact"));
                Assert.AreEqual(ActionStatus.Succeeded, testRunner.GetWorkflowActionStatus("Parse_Contact_Details"));
                Assert.AreEqual(ActionStatus.Succeeded, testRunner.GetWorkflowActionStatus("Extract_ContactId"));
                Assert.AreEqual(ActionStatus.Succeeded, testRunner.GetWorkflowActionStatus("Create_CRM_Case"));
                Assert.AreEqual(ActionStatus.Succeeded, testRunner.GetWorkflowActionStatus("Set_NewCaseId"));
                Assert.AreEqual(ActionStatus.Succeeded, testRunner.GetWorkflowActionStatus("Create_Online_Submission_Activity"));
                Assert.AreEqual(ActionStatus.Succeeded, testRunner.GetWorkflowActionStatus("Set_ActivityId"));
                Assert.AreEqual(ActionStatus.Succeeded, testRunner.GetWorkflowActionStatus("Get_Sharepoint_Token"));
                Assert.AreEqual(ActionStatus.Succeeded, testRunner.GetWorkflowActionStatus("Parse_Sharepoint_Token"));
                Assert.AreEqual(ActionStatus.Succeeded, testRunner.GetWorkflowActionStatus("Create_Folder"));

                Assert.AreEqual(ActionStatus.Succeeded, testRunner.GetWorkflowActionStatus("Get_filename"));
                Assert.AreEqual(ActionStatus.Succeeded, testRunner.GetWorkflowActionStatus("Read_blob_content"));
                Assert.AreEqual(ActionStatus.Succeeded, testRunner.GetWorkflowActionStatus("Copy_To_Sharepoint"));
                Assert.AreEqual(ActionStatus.Succeeded, testRunner.GetWorkflowActionStatus("Create_Meta_Data"));

                // Check 'create folder' only ran once
                Assert.AreEqual(1, testRunner.GetWorkflowActionRepetitionCount("Create_Folder"));

                // Check that loop ran 3 times
                Assert.AreEqual(3, testRunner.GetWorkflowActionRepetitionCount("Get_filename"));
                Assert.AreEqual(3, testRunner.GetWorkflowActionRepetitionCount("Read_blob_content"));
                Assert.AreEqual(3, testRunner.GetWorkflowActionRepetitionCount("Copy_To_Sharepoint"));
                Assert.AreEqual(3, testRunner.GetWorkflowActionRepetitionCount("Create_Meta_Data"));


                // Check request to CRM for 'create metadata'
                var crmRequest = testRunner.MockRequests.First(r => r.RequestUri.AbsolutePath.Contains("/api/data/v9.2/rpa_activitymetadatas"));
                Assert.AreEqual(HttpMethod.Post, crmRequest.Method);
                Assert.IsTrue(crmRequest.Content.Contains("\"rpa_filename\":\"File1.txt\""));

                // Check tracked properties
                var trackedProps = testRunner.GetWorkflowActionTrackedProperties("Init_FileList");
                Assert.AreEqual("RleCrmInsert", trackedProps["WorkflowName"]);
            }
        }

        /// <summary>
        /// Tests that the correct response is returned when missing FRN.
        /// </summary>
        [TestMethod]
        public void CrmInsertTest_Fails_When_Missing_Frn_In_CTL_File()
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
                        if (request.RequestUri.AbsolutePath.Contains("/Read_CTL_blob_contents") && request.Method == HttpMethod.Post)
                        {
                            mockedResponse.RequestMessage = request;
                            mockedResponse.StatusCode = HttpStatusCode.OK;
                            mockedResponse.Content = GetInvalidBlobControlFileMissingFrn();
                        }
                    }
                    return mockedResponse;
                };

                // Run the workflow
                var workflowResponse = testRunner.TriggerWorkflow(HttpMethod.Post);

                // Check workflow run status
                Assert.AreEqual(WorkflowRunStatus.Failed, testRunner.WorkflowRunStatus);

                // Check workflow response
                testRunner.ExceptionWrapper(() => Assert.AreEqual(HttpStatusCode.Accepted, workflowResponse.StatusCode));
                Assert.AreEqual(HttpStatusCode.Accepted, workflowResponse.StatusCode);

                // Check action result
                Assert.AreEqual(ActionStatus.Succeeded, testRunner.GetWorkflowActionStatus("Read_CTL_blob_contents"));
                Assert.AreEqual(ActionStatus.Succeeded, testRunner.GetWorkflowActionStatus("Parse_CTL_File"));
                Assert.AreEqual(ActionStatus.Skipped, testRunner.GetWorkflowActionStatus("Get_CRM_Token"));
                Assert.AreEqual(ActionStatus.Failed, testRunner.GetWorkflowActionStatus("All_Values_Are_Present"));
                Assert.AreEqual(ActionStatus.Succeeded, testRunner.GetWorkflowActionStatus("Append_missing_value_error"));
                Assert.AreEqual(ActionStatus.Skipped, testRunner.GetWorkflowActionStatus("Append_file_mismatch_error"));
                Assert.AreEqual(ActionStatus.Succeeded, testRunner.GetWorkflowActionStatus("Get_error_stack"));
                Assert.AreEqual(ActionStatus.Succeeded, testRunner.GetWorkflowActionStatus("Strip_sensitive_data"));

                // Check error message
                var finalError = testRunner.GetWorkflowActionOutput("Compose_final_error").ToString();
                Assert.IsTrue(finalError.Contains("missing values"));

                // Check tracked properties
                var trackedProps = testRunner.GetWorkflowActionTrackedProperties("Init_FileList");
                Assert.AreEqual("RleCrmInsert", trackedProps["WorkflowName"]);
            }
        }

        /// <summary>
        /// Tests that the correct response is returned when missing CRN.
        /// </summary>
        [TestMethod]
        public void CrmInsertTest_Fails_When_Missing_Crn_In_CTL_File()
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
                        if (request.RequestUri.AbsolutePath.Contains("/Read_CTL_blob_contents") && request.Method == HttpMethod.Post)
                        {
                            mockedResponse.RequestMessage = request;
                            mockedResponse.StatusCode = HttpStatusCode.OK;
                            mockedResponse.Content = GetInvalidBlobControlFileMissingCrn();
                        }
                    }
                    return mockedResponse;
                };

                // Run the workflow
                var workflowResponse = testRunner.TriggerWorkflow(HttpMethod.Post);

                // Check workflow run status
                Assert.AreEqual(WorkflowRunStatus.Failed, testRunner.WorkflowRunStatus);

                // Check workflow response
                testRunner.ExceptionWrapper(() => Assert.AreEqual(HttpStatusCode.Accepted, workflowResponse.StatusCode));
                Assert.AreEqual(HttpStatusCode.Accepted, workflowResponse.StatusCode);

                // Check action result
                Assert.AreEqual(ActionStatus.Succeeded, testRunner.GetWorkflowActionStatus("Read_CTL_blob_contents"));
                Assert.AreEqual(ActionStatus.Succeeded, testRunner.GetWorkflowActionStatus("Parse_CTL_File"));
                Assert.AreEqual(ActionStatus.Skipped, testRunner.GetWorkflowActionStatus("Get_CRM_Token"));
                Assert.AreEqual(ActionStatus.Failed, testRunner.GetWorkflowActionStatus("All_Values_Are_Present"));
                Assert.AreEqual(ActionStatus.Succeeded, testRunner.GetWorkflowActionStatus("Append_missing_value_error"));
                Assert.AreEqual(ActionStatus.Skipped, testRunner.GetWorkflowActionStatus("Append_file_mismatch_error"));
                Assert.AreEqual(ActionStatus.Succeeded, testRunner.GetWorkflowActionStatus("Get_error_stack"));
                Assert.AreEqual(ActionStatus.Succeeded, testRunner.GetWorkflowActionStatus("Strip_sensitive_data"));

                // Check error message
                var finalError = testRunner.GetWorkflowActionOutput("Compose_final_error").ToString();
                Assert.IsTrue(finalError.Contains("missing values"));

                // Check tracked properties
                var trackedProps = testRunner.GetWorkflowActionTrackedProperties("Init_FileList");
                Assert.AreEqual("RleCrmInsert", trackedProps["WorkflowName"]);
            }
        }

        /// <summary>
        /// Tests that the correct response is returned when file number mismatch.
        /// </summary>
        [TestMethod]
        public void CrmInsertTest_Fails_When_File_Number_Mismatch()
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
                        if (request.RequestUri.AbsolutePath.Contains("/Read_CTL_blob_contents") && request.Method == HttpMethod.Post)
                        {
                            mockedResponse.RequestMessage = request;
                            mockedResponse.StatusCode = HttpStatusCode.OK;
                            mockedResponse.Content = GetInvalidBlobControlFileWrongNumFiles();
                        }
                    }
                    return mockedResponse;
                };

                // Run the workflow
                var workflowResponse = testRunner.TriggerWorkflow(HttpMethod.Post);

                // Check workflow run status
                Assert.AreEqual(WorkflowRunStatus.Failed, testRunner.WorkflowRunStatus);

                // Check workflow response
                testRunner.ExceptionWrapper(() => Assert.AreEqual(HttpStatusCode.Accepted, workflowResponse.StatusCode));
                Assert.AreEqual(HttpStatusCode.Accepted, workflowResponse.StatusCode);

                // Check action result
                Assert.AreEqual(ActionStatus.Succeeded, testRunner.GetWorkflowActionStatus("Read_CTL_blob_contents"));
                Assert.AreEqual(ActionStatus.Succeeded, testRunner.GetWorkflowActionStatus("Parse_CTL_File"));
                Assert.AreEqual(ActionStatus.Skipped, testRunner.GetWorkflowActionStatus("Get_CRM_Token"));
                Assert.AreEqual(ActionStatus.Failed, testRunner.GetWorkflowActionStatus("All_Values_Are_Present"));
                Assert.AreEqual(ActionStatus.Skipped, testRunner.GetWorkflowActionStatus("Append_missing_value_error"));
                Assert.AreEqual(ActionStatus.Succeeded, testRunner.GetWorkflowActionStatus("Append_file_mismatch_error"));
                Assert.AreEqual(ActionStatus.Succeeded, testRunner.GetWorkflowActionStatus("Get_error_stack"));
                Assert.AreEqual(ActionStatus.Succeeded, testRunner.GetWorkflowActionStatus("Strip_sensitive_data"));

                // Check error message
                var finalError = testRunner.GetWorkflowActionOutput("Compose_final_error").ToString();
                Assert.IsTrue(finalError.Contains("mismatch of file numbers"));

                // Check tracked properties
                var trackedProps = testRunner.GetWorkflowActionTrackedProperties("Init_FileList");
                Assert.AreEqual("RleCrmInsert", trackedProps["WorkflowName"]);
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
                        if (request.RequestUri.AbsolutePath.Contains("/Read_CTL_blob_contents") && request.Method == HttpMethod.Post)
                        {
                            mockedResponse.RequestMessage = request;
                            mockedResponse.StatusCode = HttpStatusCode.OK;
                            mockedResponse.Content = GetBlobControlFile();
                        }
                        else if (request.RequestUri.AbsolutePath.Contains("/oauth2/token") && request.Method == HttpMethod.Post)
                        {
                            // CRM token
                            mockedResponse.RequestMessage = request;
                            mockedResponse.StatusCode = HttpStatusCode.OK;
                            mockedResponse.Content = ValidAuthToken();
                        }
                        else if (request.RequestUri.AbsolutePath.Contains("/api/data/v9.2/accounts") && request.Method == HttpMethod.Get)
                        {
                            mockedResponse.RequestMessage = request;
                            mockedResponse.StatusCode = HttpStatusCode.BadRequest;
                            mockedResponse.Content = ContentHelper.CreatePlainStringContent("bad request specific error");
                        }
                    }
                    return mockedResponse;
                };

                // Run the workflow
                var workflowResponse = testRunner.TriggerWorkflow(HttpMethod.Post);

                // Check workflow run status
                Assert.AreEqual(WorkflowRunStatus.Failed, testRunner.WorkflowRunStatus);

                // Check workflow response
                testRunner.ExceptionWrapper(() => Assert.AreEqual(HttpStatusCode.Accepted, workflowResponse.StatusCode));
                Assert.AreEqual(HttpStatusCode.Accepted, workflowResponse.StatusCode);

                // Check action result
                Assert.AreEqual(ActionStatus.Succeeded, testRunner.GetWorkflowActionStatus("Read_CTL_blob_contents"));
                Assert.AreEqual(ActionStatus.Succeeded, testRunner.GetWorkflowActionStatus("Parse_CTL_File"));
                Assert.AreEqual(ActionStatus.Succeeded, testRunner.GetWorkflowActionStatus("Get_CRM_Token"));
                Assert.AreEqual(ActionStatus.Succeeded, testRunner.GetWorkflowActionStatus("Parse_CRM_Token"));
                Assert.AreEqual(ActionStatus.Failed, testRunner.GetWorkflowActionStatus("Get_CRM_Org"));
                Assert.AreEqual(ActionStatus.Succeeded, testRunner.GetWorkflowActionStatus("Get_error_stack"));
                Assert.AreEqual(ActionStatus.Succeeded, testRunner.GetWorkflowActionStatus("Strip_sensitive_data"));

                // Check request to CRM
                var crmRequest = testRunner.MockRequests.First(r => r.RequestUri.AbsolutePath.Contains("/api/data/v9.2/accounts"));
                Assert.AreEqual(HttpMethod.Get, crmRequest.Method);
                Assert.AreEqual(string.Empty, crmRequest.Content);

                // Check tracked properties
                var trackedProps = testRunner.GetWorkflowActionTrackedProperties("Init_FileList");
                Assert.AreEqual("RleCrmInsert", trackedProps["WorkflowName"]);
            }
        }

        /// <summary>
        /// Tests that the correct response is returned when successful.
        /// </summary>
        [TestMethod]
        public void CrmInsertTest_Fails_When_Bad_Auth_Call_And_Masks_Secret()
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
                        if (request.RequestUri.AbsolutePath.Contains("/Read_CTL_blob_contents") && request.Method == HttpMethod.Post)
                        {
                            mockedResponse.RequestMessage = request;
                            mockedResponse.StatusCode = HttpStatusCode.OK;
                            mockedResponse.Content = GetBlobControlFile();
                        }
                        else if (request.RequestUri.AbsolutePath.Contains("/oauth2/token") && request.Method == HttpMethod.Post)
                        {
                            mockedResponse.RequestMessage = request;
                            mockedResponse.StatusCode = HttpStatusCode.BadRequest;
                            mockedResponse.Content = ContentHelper.CreatePlainStringContent("bad request specific error");
                        }
                    }
                    return mockedResponse;
                };

                // Run the workflow
                var workflowResponse = testRunner.TriggerWorkflow(HttpMethod.Post);

                // Check workflow run status
                Assert.AreEqual(WorkflowRunStatus.Failed, testRunner.WorkflowRunStatus);

                // Check workflow response
                testRunner.ExceptionWrapper(() => Assert.AreEqual(HttpStatusCode.Accepted, workflowResponse.StatusCode));
                Assert.AreEqual(HttpStatusCode.Accepted, workflowResponse.StatusCode);

                // Check action result
                Assert.AreEqual(ActionStatus.Succeeded, testRunner.GetWorkflowActionStatus("Read_CTL_blob_contents"));
                Assert.AreEqual(ActionStatus.Succeeded, testRunner.GetWorkflowActionStatus("Parse_CTL_File"));
                Assert.AreEqual(ActionStatus.Failed, testRunner.GetWorkflowActionStatus("Get_CRM_Token"));
                Assert.AreEqual(ActionStatus.Skipped, testRunner.GetWorkflowActionStatus("Parse_CRM_Token"));
                Assert.AreEqual(ActionStatus.Skipped, testRunner.GetWorkflowActionStatus("Get_CRM_Org"));
                Assert.AreEqual(ActionStatus.Succeeded, testRunner.GetWorkflowActionStatus("Get_error_stack"));
                Assert.AreEqual(ActionStatus.Succeeded, testRunner.GetWorkflowActionStatus("Strip_sensitive_data"));

                // Check masking
                var outputs = testRunner.GetWorkflowActionOutput("Strip_sensitive_data");
                Assert.IsTrue(outputs.ToString().Contains("client_secret=******&"));


                // Check tracked properties
                var trackedProps = testRunner.GetWorkflowActionTrackedProperties("Init_FileList");
                Assert.AreEqual("RleCrmInsert", trackedProps["WorkflowName"]);
            }
        }

        private static StringContent GetBlobControlFile()
        {
            var json = new
            {
                content = new
                {
                    sbi = "123456789",
                    frn = "1102077240",
                    crn = "11020219620000000",
                    uosr = "UOSR123456",
                    submissionDateTime = "25/01/2023 07:55:40",
                    filesInSubmission = 3,
                    files = new[] {
                        new {
                        fileName = "File1.txt",
                            guid = "GUID-1"
                        },
                        new {
                        fileName = "File2.txt",
                            guid = "GUID-2"
                        },
                        new {
                        fileName = "File3.txt",
                            guid = "GUID-3"
                        }
                    }
                }
            };

            return UnitTestHelper.EncodeAsStringContent(json);
        }

        private static StringContent GetInvalidBlobControlFileMissingFrn()
        {
            var json = new
            {
                content = new
                {
                    sbi = "123456789",
                    crn = "11020219620000000",
                    uosr = "UOSR123456",
                    submissionDateTime = "25/01/2023 07:55:40",
                    filesInSubmission = 3,
                    files = new[] {
                        new {
                        fileName = "File1.txt",
                            guid = "GUID-1"
                        },
                        new {
                        fileName = "File2.txt",
                            guid = "GUID-2"
                        },
                        new {
                        fileName = "File3.txt",
                            guid = "GUID-3"
                        }
                    }
                }
            };

            return UnitTestHelper.EncodeAsStringContent(json);
        }

        private static StringContent GetInvalidBlobControlFileMissingCrn()
        {
            var json = new
            {
                content = new
                {
                    sbi = "123456789",
                    frn = "1102077240",
                    uosr = "UOSR123456",
                    submissionDateTime = "25/01/2023 07:55:40",
                    filesInSubmission = 3,
                    files = new[] {
                        new {
                        fileName = "File1.txt",
                            guid = "GUID-1"
                        },
                        new {
                        fileName = "File2.txt",
                            guid = "GUID-2"
                        },
                        new {
                        fileName = "File3.txt",
                            guid = "GUID-3"
                        }
                    }
                }
            };

            return UnitTestHelper.EncodeAsStringContent(json);
        }

        private static StringContent GetInvalidBlobControlFileWrongNumFiles()
        {
            var json = new
            {
                content = new
                {
                    sbi = "123456789",
                    frn = "1102077240",
                    crn = "11020219620000000",
                    uosr = "UOSR123456",
                    submissionDateTime = "25/01/2023 07:55:40",
                    filesInSubmission = 2,
                    files = new[] {
                        new {
                        fileName = "File1.txt",
                            guid = "GUID-1"
                        },
                        new {
                        fileName = "File2.txt",
                            guid = "GUID-2"
                        },
                        new {
                        fileName = "File3.txt",
                            guid = "GUID-3"
                        }
                    }
                }
            };

            return UnitTestHelper.EncodeAsStringContent(json);
        }

        private static StringContent GetBlobFile()
        {
            var json = new
            {
                content = new
                {
                    tempData = "123456789"
                }
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

        private static StringContent ValidContactLookup()
        {
            var json = new
            {
                fullname = "contact-full-name",
                contactid = "0a889fce-67d1-4844-ba26-8aaa26553dcb"
            };

            return UnitTestHelper.EncodeAsStringContent(json);
        }

        private static StringContent ValidCreateCase()
        {
            var json = new
            {
                incidentid = "6e2fc685-1e2d-ee11-bdf4-000d3adf3558"
            };

            return UnitTestHelper.EncodeAsStringContent(json);
        }

        private static StringContent ValidCreateOnlineSubmission()
        {
            var json = new
            {
                activityid = "70e5a58b-1e2d-ee11-bdf4-002248a28b4d"
            };

            return UnitTestHelper.EncodeAsStringContent(json);
        }

        private static StringContent GetSharepointUploadResponse()
        {
            var json = new
            {
                ServerRelativeUrl = "/uploadedpath"
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