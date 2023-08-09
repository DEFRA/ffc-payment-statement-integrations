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
                        if (request.RequestUri.AbsolutePath.Contains("/oauth2/token") && request.Method == HttpMethod.Post)
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
                // Since there is a condition on the trigger that the filename must end in '.ctrl', we must supply a Name property in the content
                var workflowResponse = testRunner.TriggerWorkflow(GetBlobControlFile(), HttpMethod.Post);

                // Check workflow run status
                Assert.AreEqual(WorkflowRunStatus.Succeeded, testRunner.WorkflowRunStatus);

                // Check workflow response
                testRunner.ExceptionWrapper(() => Assert.AreEqual(HttpStatusCode.Accepted, workflowResponse.StatusCode));
                Assert.AreEqual(HttpStatusCode.Accepted, workflowResponse.StatusCode);

                // Check action result
                Assert.AreEqual(ActionStatus.Succeeded, testRunner.GetWorkflowActionStatus("Get_trigger_CTL_contents"));
                Assert.AreEqual(ActionStatus.Succeeded, testRunner.GetWorkflowActionStatus("Decode_blob_CTL_contents"));
                Assert.AreEqual(ActionStatus.Succeeded, testRunner.GetWorkflowActionStatus("Parse_CTL_File"));
                Assert.AreEqual(ActionStatus.Succeeded, testRunner.GetWorkflowActionStatus("Get_CRM_Token"));
                Assert.AreEqual(ActionStatus.Succeeded, testRunner.GetWorkflowActionStatus("Parse_CRM_Token"));
                Assert.AreEqual(ActionStatus.Succeeded, testRunner.GetWorkflowActionStatus("Get_CRM_Org"));
                Assert.AreEqual(ActionStatus.Succeeded, testRunner.GetWorkflowActionStatus("Parse_Org_Response"));
                Assert.AreEqual(ActionStatus.Succeeded, testRunner.GetWorkflowActionStatus("Extract_OrganisationId"));
                Assert.AreEqual(ActionStatus.Succeeded, testRunner.GetWorkflowActionStatus("Set_SBI_from_CTL"));
                Assert.AreEqual(ActionStatus.Skipped, testRunner.GetWorkflowActionStatus("Set_SBI_from_Org"));
                Assert.AreEqual(ActionStatus.Succeeded, testRunner.GetWorkflowActionStatus("Get_CRM_Contact"));
                Assert.AreEqual(ActionStatus.Succeeded, testRunner.GetWorkflowActionStatus("Parse_Contact_Details"));
                Assert.AreEqual(ActionStatus.Succeeded, testRunner.GetWorkflowActionStatus("Extract_ContactId"));
                Assert.AreEqual(ActionStatus.Succeeded, testRunner.GetWorkflowActionStatus("Create_CRM_Case"));
                Assert.AreEqual(ActionStatus.Succeeded, testRunner.GetWorkflowActionStatus("Extract_NewCaseId"));
                Assert.AreEqual(ActionStatus.Succeeded, testRunner.GetWorkflowActionStatus("Convert_Date_Format"));
                Assert.AreEqual(ActionStatus.Succeeded, testRunner.GetWorkflowActionStatus("Create_Online_Submission_Activity"));
                Assert.AreEqual(ActionStatus.Succeeded, testRunner.GetWorkflowActionStatus("Extract_ActivityId"));
                Assert.AreEqual(ActionStatus.Succeeded, testRunner.GetWorkflowActionStatus("Get_Sharepoint_Token"));
                Assert.AreEqual(ActionStatus.Succeeded, testRunner.GetWorkflowActionStatus("Parse_Sharepoint_Token"));
                Assert.AreEqual(ActionStatus.Succeeded, testRunner.GetWorkflowActionStatus("Create_Folder"));

                Assert.AreEqual(ActionStatus.Succeeded, testRunner.GetWorkflowActionStatus("Get_filename"));
                Assert.AreEqual(ActionStatus.Succeeded, testRunner.GetWorkflowActionStatus("Read_blob_content"));
                Assert.AreEqual(ActionStatus.Succeeded, testRunner.GetWorkflowActionStatus("Copy_To_Sharepoint"));
                Assert.AreEqual(ActionStatus.Succeeded, testRunner.GetWorkflowActionStatus("Create_Meta_Data"));

                Assert.AreEqual(ActionStatus.Skipped, testRunner.GetWorkflowActionStatus("Get_error_stack"));
                Assert.AreEqual(ActionStatus.Skipped, testRunner.GetWorkflowActionStatus("Strip_sensitive_data"));
                Assert.AreEqual(ActionStatus.Skipped, testRunner.GetWorkflowActionStatus("Send_email"));

                // Check 'create folder' only ran once
                Assert.AreEqual(1, testRunner.GetWorkflowActionRepetitionCount("Create_Folder"));

                // Check that loop ran 3 times
                Assert.AreEqual(3, testRunner.GetWorkflowActionRepetitionCount("Get_filename"));
                Assert.AreEqual(3, testRunner.GetWorkflowActionRepetitionCount("Read_blob_content"));
                Assert.AreEqual(3, testRunner.GetWorkflowActionRepetitionCount("Copy_To_Sharepoint"));
                Assert.AreEqual(3, testRunner.GetWorkflowActionRepetitionCount("Create_Meta_Data"));


                // Check which SBI value was used - should be from CTL file
                var sbiStr = testRunner.GetWorkflowActionOutput("Set_SBI_from_CTL").ToString();
                Assert.IsFalse(sbiStr.Contains("\"value\": \"120068\""));
                Assert.IsTrue(sbiStr.Contains("\"value\": \"123456789\""));

                // Check request to CRM for 'create metadata'
                // The 'For-Each' loop runs in parallel so we can't guarantee the order or results here
                var crmMetadataRequests = testRunner.MockRequests.Where(r => r.RequestUri.AbsolutePath.Contains("/api/data/v9.2/rpa_activitymetadatas")).ToList();
                Assert.AreEqual(3, crmMetadataRequests.Count);
                Assert.IsTrue(crmMetadataRequests.All(x => x.Method == HttpMethod.Post));
                Assert.AreEqual(1, crmMetadataRequests.Count(x => x.Content.Contains("\"rpa_filename\":\"File1.txt\"")));
                Assert.AreEqual(1, crmMetadataRequests.Count(x => x.Content.Contains("\"rpa_filename\":\"File2.txt\"")));
                Assert.AreEqual(1, crmMetadataRequests.Count(x => x.Content.Contains("\"rpa_filename\":\"File3.txt\"")));

                // Check tracked properties
                var trackedProps = testRunner.GetWorkflowActionTrackedProperties("Init_FileList");
                Assert.AreEqual("RleCrmInsert", trackedProps["WorkflowName"]);
            }
        }

        /// <summary>
        /// Tests that the correct response is returned when successful, taking SBI from Organisation since missing in CTL.
        /// </summary>
        [TestMethod]
        public void CrmInsertTest_When_Successful_Taking_SBI_From_Org()
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
                // Since there is a condition on the trigger that the filename must end in '.ctrl', we must supply a Name property in the content
                var workflowResponse = testRunner.TriggerWorkflow(GetBlobControlFileMissingSbi(), HttpMethod.Post);

                // Check workflow run status
                Assert.AreEqual(WorkflowRunStatus.Succeeded, testRunner.WorkflowRunStatus);

                // Check workflow response
                testRunner.ExceptionWrapper(() => Assert.AreEqual(HttpStatusCode.Accepted, workflowResponse.StatusCode));
                Assert.AreEqual(HttpStatusCode.Accepted, workflowResponse.StatusCode);

                // Check action result
                Assert.AreEqual(ActionStatus.Succeeded, testRunner.GetWorkflowActionStatus("Get_trigger_CTL_contents"));
                Assert.AreEqual(ActionStatus.Succeeded, testRunner.GetWorkflowActionStatus("Decode_blob_CTL_contents"));
                Assert.AreEqual(ActionStatus.Succeeded, testRunner.GetWorkflowActionStatus("Parse_CTL_File"));
                Assert.AreEqual(ActionStatus.Succeeded, testRunner.GetWorkflowActionStatus("Get_CRM_Token"));
                Assert.AreEqual(ActionStatus.Succeeded, testRunner.GetWorkflowActionStatus("Parse_CRM_Token"));
                Assert.AreEqual(ActionStatus.Succeeded, testRunner.GetWorkflowActionStatus("Get_CRM_Org"));
                Assert.AreEqual(ActionStatus.Succeeded, testRunner.GetWorkflowActionStatus("Parse_Org_Response"));
                Assert.AreEqual(ActionStatus.Succeeded, testRunner.GetWorkflowActionStatus("Extract_OrganisationId"));
                Assert.AreEqual(ActionStatus.Skipped, testRunner.GetWorkflowActionStatus("Set_SBI_from_CTL"));
                Assert.AreEqual(ActionStatus.Succeeded, testRunner.GetWorkflowActionStatus("Set_SBI_from_Org"));
                Assert.AreEqual(ActionStatus.Succeeded, testRunner.GetWorkflowActionStatus("Get_CRM_Contact"));
                Assert.AreEqual(ActionStatus.Succeeded, testRunner.GetWorkflowActionStatus("Parse_Contact_Details"));
                Assert.AreEqual(ActionStatus.Succeeded, testRunner.GetWorkflowActionStatus("Extract_ContactId"));
                Assert.AreEqual(ActionStatus.Succeeded, testRunner.GetWorkflowActionStatus("Create_CRM_Case"));
                Assert.AreEqual(ActionStatus.Succeeded, testRunner.GetWorkflowActionStatus("Extract_NewCaseId"));
                Assert.AreEqual(ActionStatus.Succeeded, testRunner.GetWorkflowActionStatus("Convert_Date_Format"));
                Assert.AreEqual(ActionStatus.Succeeded, testRunner.GetWorkflowActionStatus("Create_Online_Submission_Activity"));
                Assert.AreEqual(ActionStatus.Succeeded, testRunner.GetWorkflowActionStatus("Extract_ActivityId"));
                Assert.AreEqual(ActionStatus.Succeeded, testRunner.GetWorkflowActionStatus("Get_Sharepoint_Token"));
                Assert.AreEqual(ActionStatus.Succeeded, testRunner.GetWorkflowActionStatus("Parse_Sharepoint_Token"));
                Assert.AreEqual(ActionStatus.Succeeded, testRunner.GetWorkflowActionStatus("Create_Folder"));

                Assert.AreEqual(ActionStatus.Succeeded, testRunner.GetWorkflowActionStatus("Get_filename"));
                Assert.AreEqual(ActionStatus.Succeeded, testRunner.GetWorkflowActionStatus("Read_blob_content"));
                Assert.AreEqual(ActionStatus.Succeeded, testRunner.GetWorkflowActionStatus("Copy_To_Sharepoint"));
                Assert.AreEqual(ActionStatus.Succeeded, testRunner.GetWorkflowActionStatus("Create_Meta_Data"));

                Assert.AreEqual(ActionStatus.Skipped, testRunner.GetWorkflowActionStatus("Get_error_stack"));
                Assert.AreEqual(ActionStatus.Skipped, testRunner.GetWorkflowActionStatus("Strip_sensitive_data"));
                Assert.AreEqual(ActionStatus.Skipped, testRunner.GetWorkflowActionStatus("Send_email"));

                // Check 'create folder' only ran once
                Assert.AreEqual(1, testRunner.GetWorkflowActionRepetitionCount("Create_Folder"));

                // Check that loop ran 3 times
                Assert.AreEqual(3, testRunner.GetWorkflowActionRepetitionCount("Get_filename"));
                Assert.AreEqual(3, testRunner.GetWorkflowActionRepetitionCount("Read_blob_content"));
                Assert.AreEqual(3, testRunner.GetWorkflowActionRepetitionCount("Copy_To_Sharepoint"));
                Assert.AreEqual(3, testRunner.GetWorkflowActionRepetitionCount("Create_Meta_Data"));


                // Check request to CRM for 'create metadata'
                // The 'For-Each' loop runs in parallel so we can't guarantee the order or results here
                var crmMetadataRequests = testRunner.MockRequests.Where(r => r.RequestUri.AbsolutePath.Contains("/api/data/v9.2/rpa_activitymetadatas")).ToList();
                Assert.AreEqual(3, crmMetadataRequests.Count);
                Assert.IsTrue(crmMetadataRequests.All(x => x.Method == HttpMethod.Post));
                Assert.AreEqual(1, crmMetadataRequests.Count(x => x.Content.Contains("\"rpa_filename\":\"File1.txt\"")));
                Assert.AreEqual(1, crmMetadataRequests.Count(x => x.Content.Contains("\"rpa_filename\":\"File2.txt\"")));
                Assert.AreEqual(1, crmMetadataRequests.Count(x => x.Content.Contains("\"rpa_filename\":\"File3.txt\"")));

                // Check which SBI value was used - should be from Organisation call (not CTL file)
                var sbiStr = testRunner.GetWorkflowActionOutput("Set_SBI_from_Org").ToString();
                Assert.IsTrue(sbiStr.Contains("\"value\": \"120068\""));
                Assert.IsFalse(sbiStr.Contains("\"value\": \"123456789\""));

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
                        if (request.RequestUri.AbsolutePath.Contains("/oauth2/token") && request.Method == HttpMethod.Post)
                        {
                            // Email auth token
                            mockedResponse.RequestMessage = request;
                            mockedResponse.StatusCode = HttpStatusCode.OK;
                            mockedResponse.Content = ValidAuthToken();
                        }
                        else if (request.RequestUri.AbsolutePath.Contains("/api/AzureFunctionGovNotify") && request.Method == HttpMethod.Post)
                        {
                            // Email function call
                            mockedResponse.RequestMessage = request;
                            mockedResponse.StatusCode = HttpStatusCode.OK;
                        }
                    }
                    return mockedResponse;
                };
                
                // Run the workflow
                // Since there is a condition on the trigger that the filename must end in '.ctrl', we must supply a Name property in the content
                var workflowResponse = testRunner.TriggerWorkflow(GetInvalidBlobControlFileMissingFrn(), HttpMethod.Post);

                // Check workflow run status
                Assert.AreEqual(WorkflowRunStatus.Succeeded, testRunner.WorkflowRunStatus);

                // Check workflow response
                testRunner.ExceptionWrapper(() => Assert.AreEqual(HttpStatusCode.Accepted, workflowResponse.StatusCode));
                Assert.AreEqual(HttpStatusCode.Accepted, workflowResponse.StatusCode);

                // Check action result
                Assert.AreEqual(ActionStatus.Succeeded, testRunner.GetWorkflowActionStatus("Get_trigger_CTL_contents"));
                Assert.AreEqual(ActionStatus.Succeeded, testRunner.GetWorkflowActionStatus("Decode_blob_CTL_contents"));
                Assert.AreEqual(ActionStatus.Failed, testRunner.GetWorkflowActionStatus("Parse_CTL_File"));
                Assert.AreEqual(ActionStatus.Skipped, testRunner.GetWorkflowActionStatus("Get_CRM_Token"));
                Assert.AreEqual(ActionStatus.Skipped, testRunner.GetWorkflowActionStatus("Is_File_Number_Match"));
                Assert.AreEqual(ActionStatus.Skipped, testRunner.GetWorkflowActionStatus("Append_file_mismatch_error"));
                Assert.AreEqual(ActionStatus.Succeeded, testRunner.GetWorkflowActionStatus("Get_error_stack"));
                Assert.AreEqual(ActionStatus.Succeeded, testRunner.GetWorkflowActionStatus("Strip_sensitive_data"));
                Assert.AreEqual(ActionStatus.Succeeded, testRunner.GetWorkflowActionStatus("Get_email_auth_token"));
                Assert.AreEqual(ActionStatus.Succeeded, testRunner.GetWorkflowActionStatus("Parse_email_auth_token"));
                Assert.AreEqual(ActionStatus.Succeeded, testRunner.GetWorkflowActionStatus("Send_email"));

                // Check error message
                var finalError = testRunner.GetWorkflowActionOutput("Compose_final_error").ToString();
                Assert.IsTrue(finalError.Contains("schema validation failed"));

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
                        if (request.RequestUri.AbsolutePath.Contains("/oauth2/token") && request.Method == HttpMethod.Post)
                        {
                            // Email auth token
                            mockedResponse.RequestMessage = request;
                            mockedResponse.StatusCode = HttpStatusCode.OK;
                            mockedResponse.Content = ValidAuthToken();
                        }
                        else if (request.RequestUri.AbsolutePath.Contains("/api/AzureFunctionGovNotify") && request.Method == HttpMethod.Post)
                        {
                            // Email function call
                            mockedResponse.RequestMessage = request;
                            mockedResponse.StatusCode = HttpStatusCode.OK;
                        }
                    }
                    return mockedResponse;
                };

                // Run the workflow
                // Since there is a condition on the trigger that the filename must end in '.ctrl', we must supply a Name property in the content
                var workflowResponse = testRunner.TriggerWorkflow(GetInvalidBlobControlFileMissingCrn(), HttpMethod.Post);

                // Check workflow run status
                Assert.AreEqual(WorkflowRunStatus.Succeeded, testRunner.WorkflowRunStatus);

                // Check workflow response
                testRunner.ExceptionWrapper(() => Assert.AreEqual(HttpStatusCode.Accepted, workflowResponse.StatusCode));
                Assert.AreEqual(HttpStatusCode.Accepted, workflowResponse.StatusCode);

                // Check action result
                Assert.AreEqual(ActionStatus.Succeeded, testRunner.GetWorkflowActionStatus("Get_trigger_CTL_contents"));
                Assert.AreEqual(ActionStatus.Succeeded, testRunner.GetWorkflowActionStatus("Decode_blob_CTL_contents"));
                Assert.AreEqual(ActionStatus.Failed, testRunner.GetWorkflowActionStatus("Parse_CTL_File"));
                Assert.AreEqual(ActionStatus.Skipped, testRunner.GetWorkflowActionStatus("Get_CRM_Token"));
                Assert.AreEqual(ActionStatus.Skipped, testRunner.GetWorkflowActionStatus("Is_File_Number_Match"));
                Assert.AreEqual(ActionStatus.Skipped, testRunner.GetWorkflowActionStatus("Append_file_mismatch_error"));
                Assert.AreEqual(ActionStatus.Succeeded, testRunner.GetWorkflowActionStatus("Get_error_stack"));
                Assert.AreEqual(ActionStatus.Succeeded, testRunner.GetWorkflowActionStatus("Strip_sensitive_data"));
                Assert.AreEqual(ActionStatus.Succeeded, testRunner.GetWorkflowActionStatus("Get_email_auth_token"));
                Assert.AreEqual(ActionStatus.Succeeded, testRunner.GetWorkflowActionStatus("Parse_email_auth_token"));
                Assert.AreEqual(ActionStatus.Succeeded, testRunner.GetWorkflowActionStatus("Send_email"));

                // Check error message
                var finalError = testRunner.GetWorkflowActionOutput("Compose_final_error").ToString();
                Assert.IsTrue(finalError.Contains("schema validation failed"));

                // Check tracked properties
                var trackedProps = testRunner.GetWorkflowActionTrackedProperties("Init_FileList");
                Assert.AreEqual("RleCrmInsert", trackedProps["WorkflowName"]);
            }
        }

        /// <summary>
        /// Tests that the correct response is returned when missing OUSR.
        /// </summary>
        [TestMethod]
        public void CrmInsertTest_Fails_When_Missing_Uosr_In_CTL_File()
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
                        else if (request.RequestUri.AbsolutePath.Contains("/api/AzureFunctionGovNotify") && request.Method == HttpMethod.Post)
                        {
                            // Email function call
                            mockedResponse.RequestMessage = request;
                            mockedResponse.StatusCode = HttpStatusCode.OK;
                        }
                    }
                    return mockedResponse;
                };

                // Run the workflow
                // Since there is a condition on the trigger that the filename must end in '.ctrl', we must supply a Name property in the content
                var workflowResponse = testRunner.TriggerWorkflow(GetInvalidBlobControlFileMissingUosr(), HttpMethod.Post);

                // Check workflow run status
                Assert.AreEqual(WorkflowRunStatus.Succeeded, testRunner.WorkflowRunStatus);

                // Check workflow response
                testRunner.ExceptionWrapper(() => Assert.AreEqual(HttpStatusCode.Accepted, workflowResponse.StatusCode));
                Assert.AreEqual(HttpStatusCode.Accepted, workflowResponse.StatusCode);

                // Check action result
                Assert.AreEqual(ActionStatus.Succeeded, testRunner.GetWorkflowActionStatus("Get_trigger_CTL_contents"));
                Assert.AreEqual(ActionStatus.Succeeded, testRunner.GetWorkflowActionStatus("Decode_blob_CTL_contents"));
                Assert.AreEqual(ActionStatus.Failed, testRunner.GetWorkflowActionStatus("Parse_CTL_File"));
                Assert.AreEqual(ActionStatus.Skipped, testRunner.GetWorkflowActionStatus("Get_CRM_Token"));
                Assert.AreEqual(ActionStatus.Skipped, testRunner.GetWorkflowActionStatus("Is_File_Number_Match"));
                Assert.AreEqual(ActionStatus.Skipped, testRunner.GetWorkflowActionStatus("Append_file_mismatch_error"));
                Assert.AreEqual(ActionStatus.Succeeded, testRunner.GetWorkflowActionStatus("Get_error_stack"));
                Assert.AreEqual(ActionStatus.Succeeded, testRunner.GetWorkflowActionStatus("Strip_sensitive_data"));
                Assert.AreEqual(ActionStatus.Succeeded, testRunner.GetWorkflowActionStatus("Get_email_auth_token"));
                Assert.AreEqual(ActionStatus.Succeeded, testRunner.GetWorkflowActionStatus("Parse_email_auth_token"));
                Assert.AreEqual(ActionStatus.Succeeded, testRunner.GetWorkflowActionStatus("Send_email"));

                // Check error message
                var finalError = testRunner.GetWorkflowActionOutput("Compose_final_error").ToString();
                Assert.IsTrue(finalError.Contains("schema validation failed"));

                // Check tracked properties
                var trackedProps = testRunner.GetWorkflowActionTrackedProperties("Init_FileList");
                Assert.AreEqual("RleCrmInsert", trackedProps["WorkflowName"]);
            }
        }

        /// <summary>
        /// Tests that the correct response is returned when missing Submission Date.
        /// </summary>
        [TestMethod]
        public void CrmInsertTest_Fails_When_Missing_SubmissionDate_In_CTL_File()
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
                        else if (request.RequestUri.AbsolutePath.Contains("/api/AzureFunctionGovNotify") && request.Method == HttpMethod.Post)
                        {
                            // Email function call
                            mockedResponse.RequestMessage = request;
                            mockedResponse.StatusCode = HttpStatusCode.OK;
                        }
                    }
                    return mockedResponse;
                };

                // Run the workflow
                // Since there is a condition on the trigger that the filename must end in '.ctrl', we must supply a Name property in the content
                var workflowResponse = testRunner.TriggerWorkflow(GetInvalidBlobControlFileMissingSubmissionDate(), HttpMethod.Post);

                // Check workflow run status
                Assert.AreEqual(WorkflowRunStatus.Succeeded, testRunner.WorkflowRunStatus);

                // Check workflow response
                testRunner.ExceptionWrapper(() => Assert.AreEqual(HttpStatusCode.Accepted, workflowResponse.StatusCode));
                Assert.AreEqual(HttpStatusCode.Accepted, workflowResponse.StatusCode);

                // Check action result
                Assert.AreEqual(ActionStatus.Succeeded, testRunner.GetWorkflowActionStatus("Get_trigger_CTL_contents"));
                Assert.AreEqual(ActionStatus.Succeeded, testRunner.GetWorkflowActionStatus("Decode_blob_CTL_contents"));
                Assert.AreEqual(ActionStatus.Failed, testRunner.GetWorkflowActionStatus("Parse_CTL_File"));
                Assert.AreEqual(ActionStatus.Skipped, testRunner.GetWorkflowActionStatus("Get_CRM_Token"));
                Assert.AreEqual(ActionStatus.Skipped, testRunner.GetWorkflowActionStatus("Is_File_Number_Match"));
                Assert.AreEqual(ActionStatus.Skipped, testRunner.GetWorkflowActionStatus("Append_file_mismatch_error"));
                Assert.AreEqual(ActionStatus.Succeeded, testRunner.GetWorkflowActionStatus("Get_error_stack"));
                Assert.AreEqual(ActionStatus.Succeeded, testRunner.GetWorkflowActionStatus("Strip_sensitive_data"));
                Assert.AreEqual(ActionStatus.Succeeded, testRunner.GetWorkflowActionStatus("Get_email_auth_token"));
                Assert.AreEqual(ActionStatus.Succeeded, testRunner.GetWorkflowActionStatus("Parse_email_auth_token"));
                Assert.AreEqual(ActionStatus.Succeeded, testRunner.GetWorkflowActionStatus("Send_email"));

                // Check error message
                var finalError = testRunner.GetWorkflowActionOutput("Compose_final_error").ToString();
                Assert.IsTrue(finalError.Contains("schema validation failed"));

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
                        if (request.RequestUri.AbsolutePath.Contains("/oauth2/token") && request.Method == HttpMethod.Post)
                        {
                            // Email auth token
                            mockedResponse.RequestMessage = request;
                            mockedResponse.StatusCode = HttpStatusCode.OK;
                            mockedResponse.Content = ValidAuthToken();
                        }
                        else if (request.RequestUri.AbsolutePath.Contains("/api/AzureFunctionGovNotify") && request.Method == HttpMethod.Post)
                        {
                            // Email function call
                            mockedResponse.RequestMessage = request;
                            mockedResponse.StatusCode = HttpStatusCode.OK;
                        }
                    }
                    return mockedResponse;
                };

                // Run the workflow
                // Since there is a condition on the trigger that the filename must end in '.ctrl', we must supply a Name property in the content
                var workflowResponse = testRunner.TriggerWorkflow(GetInvalidBlobControlFileWrongNumFiles(), HttpMethod.Post);

                // Check workflow run status
                Assert.AreEqual(WorkflowRunStatus.Succeeded, testRunner.WorkflowRunStatus);

                // Check workflow response
                testRunner.ExceptionWrapper(() => Assert.AreEqual(HttpStatusCode.Accepted, workflowResponse.StatusCode));
                Assert.AreEqual(HttpStatusCode.Accepted, workflowResponse.StatusCode);

                // Check action result
                Assert.AreEqual(ActionStatus.Succeeded, testRunner.GetWorkflowActionStatus("Get_trigger_CTL_contents"));
                Assert.AreEqual(ActionStatus.Succeeded, testRunner.GetWorkflowActionStatus("Decode_blob_CTL_contents"));
                Assert.AreEqual(ActionStatus.Succeeded, testRunner.GetWorkflowActionStatus("Parse_CTL_File"));
                Assert.AreEqual(ActionStatus.Failed, testRunner.GetWorkflowActionStatus("Is_File_Number_Match"));
                Assert.AreEqual(ActionStatus.Succeeded, testRunner.GetWorkflowActionStatus("Append_file_mismatch_error"));
                Assert.AreEqual(ActionStatus.Skipped, testRunner.GetWorkflowActionStatus("Get_CRM_Token"));
                Assert.AreEqual(ActionStatus.Succeeded, testRunner.GetWorkflowActionStatus("Get_error_stack"));
                Assert.AreEqual(ActionStatus.Succeeded, testRunner.GetWorkflowActionStatus("Strip_sensitive_data"));
                Assert.AreEqual(ActionStatus.Succeeded, testRunner.GetWorkflowActionStatus("Get_email_auth_token"));
                Assert.AreEqual(ActionStatus.Succeeded, testRunner.GetWorkflowActionStatus("Parse_email_auth_token"));
                Assert.AreEqual(ActionStatus.Succeeded, testRunner.GetWorkflowActionStatus("Send_email"));

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
                        if (request.RequestUri.AbsolutePath.Contains("/oauth2/token") && request.Method == HttpMethod.Post)
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
                // Since there is a condition on the trigger that the filename must end in '.ctrl', we must supply a Name property in the content
                var workflowResponse = testRunner.TriggerWorkflow(GetBlobControlFile(), HttpMethod.Post);

                // Check workflow run status
                Assert.AreEqual(WorkflowRunStatus.Succeeded, testRunner.WorkflowRunStatus);

                // Check workflow response
                testRunner.ExceptionWrapper(() => Assert.AreEqual(HttpStatusCode.Accepted, workflowResponse.StatusCode));
                Assert.AreEqual(HttpStatusCode.Accepted, workflowResponse.StatusCode);

                // Check action result
                Assert.AreEqual(ActionStatus.Succeeded, testRunner.GetWorkflowActionStatus("Get_trigger_CTL_contents"));
                Assert.AreEqual(ActionStatus.Succeeded, testRunner.GetWorkflowActionStatus("Decode_blob_CTL_contents"));
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

            var firstAuthCall = true;

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
                            // First call is CRM auth token - should fail
                            // Second call is email auth token - should be successful
                            mockedResponse.RequestMessage = request;
                            if (firstAuthCall)
                            {
                                mockedResponse.StatusCode = HttpStatusCode.BadRequest;
                                mockedResponse.Content = ContentHelper.CreatePlainStringContent("bad request specific error");
                                firstAuthCall = false;
                            }
                            else
                            {
                                mockedResponse.StatusCode = HttpStatusCode.OK;
                                mockedResponse.Content = ValidAuthToken();
                            }
                        }
                        else if (request.RequestUri.AbsolutePath.Contains("/api/AzureFunctionGovNotify") && request.Method == HttpMethod.Post)
                        {
                            // Email function call
                            mockedResponse.RequestMessage = request;
                            mockedResponse.StatusCode = HttpStatusCode.OK;
                        }
                    }
                    return mockedResponse;
                };

                // Run the workflow
                // Since there is a condition on the trigger that the filename must end in '.ctrl', we must supply a Name property in the content
                var workflowResponse = testRunner.TriggerWorkflow(GetBlobControlFile(), HttpMethod.Post);

                // Check workflow run status
                Assert.AreEqual(WorkflowRunStatus.Succeeded, testRunner.WorkflowRunStatus);

                // Check workflow response
                testRunner.ExceptionWrapper(() => Assert.AreEqual(HttpStatusCode.Accepted, workflowResponse.StatusCode));
                Assert.AreEqual(HttpStatusCode.Accepted, workflowResponse.StatusCode);

                // Check action result
                Assert.AreEqual(ActionStatus.Succeeded, testRunner.GetWorkflowActionStatus("Get_trigger_CTL_contents"));
                Assert.AreEqual(ActionStatus.Succeeded, testRunner.GetWorkflowActionStatus("Decode_blob_CTL_contents"));
                Assert.AreEqual(ActionStatus.Succeeded, testRunner.GetWorkflowActionStatus("Parse_CTL_File"));
                Assert.AreEqual(ActionStatus.Failed, testRunner.GetWorkflowActionStatus("Get_CRM_Token"));
                Assert.AreEqual(ActionStatus.Skipped, testRunner.GetWorkflowActionStatus("Parse_CRM_Token"));
                Assert.AreEqual(ActionStatus.Skipped, testRunner.GetWorkflowActionStatus("Get_CRM_Org"));
                Assert.AreEqual(ActionStatus.Succeeded, testRunner.GetWorkflowActionStatus("Get_error_stack"));
                Assert.AreEqual(ActionStatus.Succeeded, testRunner.GetWorkflowActionStatus("Strip_sensitive_data"));
                Assert.AreEqual(ActionStatus.Succeeded, testRunner.GetWorkflowActionStatus("Get_email_auth_token"));
                Assert.AreEqual(ActionStatus.Succeeded, testRunner.GetWorkflowActionStatus("Parse_email_auth_token"));
                Assert.AreEqual(ActionStatus.Succeeded, testRunner.GetWorkflowActionStatus("Send_email"));

                // Check masking
                var outputs = testRunner.GetWorkflowActionOutput("Strip_sensitive_data");
                Assert.IsTrue(outputs.ToString().Contains("client_secret=******&"));
                Assert.IsFalse(outputs.ToString().Contains("SENSITIVE"));

                // Check tracked properties
                var trackedProps = testRunner.GetWorkflowActionTrackedProperties("Init_FileList");
                Assert.AreEqual("RleCrmInsert", trackedProps["WorkflowName"]);
            }
        }

        private static StringContent GetBlobControlFile()
        {
            // Since a property beginning with '$' cannot be defined in an anonymous type,
            // this JSON is created using strings

            var jsonStr = "{ \"name\": \"myfilename.ctrl\", \"content\": { \"$content\": { \"sbi\": \"123456789\", \"frn\": \"1102077240\", \"crn\": \"11020219620000000\", \"uosr\": \"UOSR123456\", ";
            jsonStr += "\"submissionDate\": \"25/01/2023 07:55:40\", \"filesInSubmission\": 3, \"files\": [\"File1.txt\", \"File2.txt\", \"File3.txt\" ] } } }";

            return UnitTestHelper.EncodeAsStringContent(jsonStr, true, "content", "$content");
        }

        private static StringContent GetBlobControlFileMissingSbi()
        {
            // Since a property beginning with '$' cannot be defined in an anonymous type,
            // this JSON is created using strings

            var jsonStr = "{ \"name\": \"myfilename.ctrl\", \"content\": { \"$content\": { \"frn\": \"1102077240\", \"crn\": \"11020219620000000\", \"uosr\": \"UOSR123456\", ";
            jsonStr += "\"submissionDate\": \"25/01/2023 07:55:40\", \"filesInSubmission\": 3, \"files\": [\"File1.txt\", \"File2.txt\", \"File3.txt\" ] } } }";

            return UnitTestHelper.EncodeAsStringContent(jsonStr, true, "content", "$content");
        }

        private static StringContent GetInvalidBlobControlFileMissingFrn()
        {
            // Since a property beginning with '$' cannot be defined in an anonymous type,
            // this JSON is created using strings

            var jsonStr = "{ \"name\": \"myfilename.ctrl\", \"content\": { \"$content\": { \"sbi\": \"123456789\", \"crn\": \"11020219620000000\", \"uosr\": \"UOSR123456\", ";
            jsonStr += "\"submissionDate\": \"25/01/2023 07:55:40\", \"filesInSubmission\": 3, \"files\": [\"File1.txt\", \"File2.txt\", \"File3.txt\" ] } } }";

            return UnitTestHelper.EncodeAsStringContent(jsonStr, true, "content", "$content");
        }

        private static StringContent GetInvalidBlobControlFileMissingCrn()
        {
            // Since a property beginning with '$' cannot be defined in an anonymous type,
            // this JSON is created using strings

            var jsonStr = "{ \"name\": \"myfilename.ctrl\", \"content\": { \"$content\": { \"sbi\": \"123456789\", \"frn\": \"1102077240\", \"uosr\": \"UOSR123456\", ";
            jsonStr += "\"submissionDate\": \"25/01/2023 07:55:40\", \"filesInSubmission\": 3, \"files\": [\"File1.txt\", \"File2.txt\", \"File3.txt\" ] } } }";

            return UnitTestHelper.EncodeAsStringContent(jsonStr, true, "content", "$content");
        }

        private static StringContent GetInvalidBlobControlFileMissingUosr()
        {
            // Since a property beginning with '$' cannot be defined in an anonymous type,
            // this JSON is created using strings

            var jsonStr = "{ \"name\": \"myfilename.ctrl\", \"content\": { \"$content\": { \"sbi\": \"123456789\", \"frn\": \"1102077240\", \"crn\": \"11020219620000000\", ";
            jsonStr += "\"submissionDate\": \"25/01/2023 07:55:40\", \"filesInSubmission\": 3, \"files\": [\"File1.txt\", \"File2.txt\", \"File3.txt\" ] } } }";

            return UnitTestHelper.EncodeAsStringContent(jsonStr, true, "content", "$content");
        }

        private static StringContent GetInvalidBlobControlFileMissingSubmissionDate()
        {
            // Since a property beginning with '$' cannot be defined in an anonymous type,
            // this JSON is created using strings

            var jsonStr = "{ \"name\": \"myfilename.ctrl\", \"content\": { \"$content\": { \"sbi\": \"123456789\", \"frn\": \"1102077240\", \"crn\": \"11020219620000000\", \"uosr\": \"UOSR123456\", ";
            jsonStr += " \"filesInSubmission\": 3, \"files\": [\"File1.txt\", \"File2.txt\", \"File3.txt\" ] } } }";

            return UnitTestHelper.EncodeAsStringContent(jsonStr, true, "content", "$content");
        }

        private static StringContent GetInvalidBlobControlFileWrongNumFiles()
        {
            // Since a property beginning with '$' cannot be defined in an anonymous type,
            // this JSON is created using strings

            var jsonStr = "{ \"name\": \"myfilename.ctrl\", \"content\": { \"$content\": { \"sbi\": \"123456789\", \"frn\": \"1102077240\", \"crn\": \"11020219620000000\", \"uosr\": \"UOSR123456\", ";
            jsonStr += "\"submissionDate\": \"25/01/2023 07:55:40\", \"filesInSubmission\": 2, \"files\": [\"File1.txt\", \"File2.txt\", \"File3.txt\" ] } } }";

            return UnitTestHelper.EncodeAsStringContent(jsonStr, true, "content", "$content");
        }

        private static StringContent ValidOrgLookup()
        {
            var json = new
            {
                value = new[] {
                    new {
                        name = "1 Frog Hall",
                        accountid = "df1f5e8a-a175-e411-9411-00155deb6487",
                        rpa_sbinumber = "120068"
                    }
                }
            };

            return UnitTestHelper.EncodeAsStringContent(json);
        }

        private static StringContent ValidContactLookup()
        {
            var json = new
            {
                value = new[] {
                    new {
                        fullname = "contact-full-name",
                        contactid = "0a889fce-67d1-4844-ba26-8aaa26553dcb"
                    }
                }
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