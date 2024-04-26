/////////////////////////////////////////////////////////////////////
// Copyright (c) Autodesk, Inc. All rights reserved
// Written by Developer Advocacy and Support
//
// Permission to use, copy, modify, and distribute this software in
// object code form for any purpose and without fee is hereby granted,
// provided that the above copyright notice appears in all copies and
// that both that copyright notice and the limited warranty and
// restricted rights notice below appear in all supporting
// documentation.
//
// AUTODESK PROVIDES THIS PROGRAM "AS IS" AND WITH ALL FAULTS.
// AUTODESK SPECIFICALLY DISCLAIMS ANY IMPLIED WARRANTY OF
// MERCHANTABILITY OR FITNESS FOR A PARTICULAR USE.  AUTODESK, INC.
// DOES NOT WARRANT THAT THE OPERATION OF THE PROGRAM WILL BE
// UNINTERRUPTED OR ERROR FREE.
/////////////////////////////////////////////////////////////////////

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Flurl.Http;
using Hangfire;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Serilog;
using Autodesk.SDKManager;
using Autodesk.DataManagement;
using Autodesk.Oss;
using Autodesk.ModelDerivative;
using Autodesk.ModelDerivative.Model;
using Autodesk.DataManagement.Model;
using RevitToIfcScheduler.Models;
using File = RevitToIfcScheduler.Models.File;

namespace RevitToIfcScheduler.Utilities
{

    public static class Forge
    {
        private static readonly SDKManager _sdkManager = SdkManagerBuilder.Create().Build();
        private static readonly string _localTempFolder = Path.Combine(Directory.GetCurrentDirectory(), "tmp");
        private static readonly string[] allowedFolderTypes = ["normal", "plan"];

        public static async Task<List<RevitToIfcScheduler.Models.Folder>> GetTopFolders(string hubId, string projectId, string token)
        {
            try
            {
                var dataManagementClient = new DataManagementClient(_sdkManager);
                var foldersData = await dataManagementClient.GetProjectTopFoldersAsync(hubId, projectId, accessToken: token);

                var folders = new List<RevitToIfcScheduler.Models.Folder>();
                foreach (var folder in foldersData.Data)
                {
                    string name = folder.Attributes.Name;
                    if (folder.Attributes.Hidden == false && IsValidTopFolder(name) && allowedFolderTypes.Contains(folder.Attributes.Extension.Data.FolderType))
                    {
                        folders.Add(new RevitToIfcScheduler.Models.Folder()
                        {
                            Id = folder.Id,
                            Name = folder.Attributes.Name,
                            WebView = folder.Links.WebView.Href
                        });
                    }
                }

                return folders;
            }
            catch (Exception exception)
            {
                throw;
            }
        }

        public static async Task<List<Base>> GetFolderContents(string projectId, string folderId, string token)
        {
            try
            {
                // Todo: replace codes with new SDK after contents data has the `links.webView` field.
                var children = new List<Base>();

                var url = $"{AppConfig.ForgeBaseUrl}/data/v1/projects/{projectId}/folders/{folderId}/contents";
                while (true)
                {
                    var data = await url
                        .WithOAuthBearerToken(token)
                        .GetJsonAsync<dynamic>();

                    foreach (dynamic item in data.data)
                    {
                        if (item.attributes.extension.type == "folders:autodesk.bim360:Folder")
                        {
                            children.Add(new RevitToIfcScheduler.Models.Folder()
                            {
                                Id = item.id,
                                Name = item.attributes.name,
                                WebView = item.links.webView.href
                            });
                        }
                    }

                    if (data.included != null)
                    {
                        foreach (dynamic item in data.included)
                        {
                            if (item.attributes.fileType == "rvt" || item.attributes.fileType == "ifc")
                            {
                                children.Add(new File()
                                {
                                    Id = item.id,
                                    Name = item.attributes.name,
                                    ItemId = item.relationships.item.data.id,
                                    FileType = item.attributes.fileType,
                                    FolderId = folderId,
                                    IsCompositeDesign = item.attributes.extension.data.isCompositeDesign ?? false,
                                    WebView = item.links.webView.href
                                });
                            }
                        }
                    }

                    if (data.links != null && data.links.next != null && data.links.next.href != null)
                    {
                        url = data.links.next.href;
                    }
                    else
                    {
                        break;
                    }
                }

                return children;
            }
            catch (Exception exception)
            {
                throw;
            }
        }

        public static async Task<List<File>> GetAllChildRevitFiles(string projectId, List<string> folderIds, TokenGetter tokenGetter)
        {
            var files = new HashSet<File>();
            var fetchedFolderIds = new List<string>();

            while (folderIds.Count > 0)
            {
                var folderUrn = folderIds.PopAt(0);
                if (!fetchedFolderIds.Contains(folderUrn))
                {
                    var token = await tokenGetter.GetToken();
                    var folderContents = await GetFolderContents(projectId, folderUrn, token);

                    foreach (var item in folderContents)
                    {
                        if (item is File file && file.FileType == "rvt")
                        {
                            files.Add(file);
                        }
                        else if (item is RevitToIfcScheduler.Models.Folder)
                        {
                            folderIds.Add(item.Id);
                        }
                    }
                }
                fetchedFolderIds.Add(folderUrn);
            }

            return files.ToList();
        }

        private static bool IsValidTopFolder(string name)
        {
            Guid guidResult;
            if (name.Contains("checklist_") || name.Contains("submittals-attachments") || name.Contains("Photos") ||
                name.Contains("ProjectTb") || name.Contains("dailylog_") || name.Contains("issue_")
                || name.Contains("issues_") || name.Contains("COST Root Folder") || Guid.TryParse(name, out guidResult))
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        public static async Task CheckOrCreateTransientBucket(string bucketKey)
        {
            try
            {
                var token = await new TwoLeggedTokenGetter().GetToken();
                var url = $"{AppConfig.ForgeBaseUrl}/oss/v2/buckets/{bucketKey}/details";

                var response = await url
                    .WithOAuthBearerToken(token)
                    .AllowHttpStatus("4xx")
                    .GetAsync();

                if (response.StatusCode == StatusCodes.Status200OK)
                {
                    Log.Information("Bucket Exists");
                }
                else if (response.StatusCode == StatusCodes.Status404NotFound)
                {
                    Log.Information("Bucket Does not Exist");
                    await CreateTransientBucket(bucketKey, token);
                }
                else
                {
                    Log.Warning("Bucket owned by another ClientID");
                }
            }
            catch (Exception exception)
            {
                Log.Error(exception.Message);
                Log.Error(exception.StackTrace);
                throw;
            }
        }

        public static async Task CreateTransientBucket(string bucketKey, string token, string region = "US")
        {
            try
            {
                var url = $"{AppConfig.ForgeBaseUrl}/oss/v2/buckets";
                var body = new
                {
                    bucketKey,
                    policyKey = "transient"
                };

                await url
                    .WithHeader("x-ads-region", region)
                    .WithOAuthBearerToken(token)
                    .PostJsonAsync(body);
            }
            catch (Exception exception)
            {
                Log.Error(exception.Message);
                Log.Error(exception.StackTrace);
                throw;
            }
        }

        public static async Task<string> GetItemStorageLocation(string projectId, string itemId, string token)
        {
            try
            {
                var dataManagementClient = new DataManagementClient(_sdkManager);
                var response = await dataManagementClient.GetItemTipAsync(projectId, itemId, accessToken: token);

                return response.Data.Relationships.Storage.Meta.Link.Href;
            }
            catch (Exception exception)
            {
                Log.Error(exception.Message);
                Log.Error(exception.StackTrace);
                throw;
            }
        }

        public static async Task<string> GetItemTipStorageId(string projectId, string itemId, string token)
        {
            try
            {
                var dataManagementClient = new DataManagementClient(_sdkManager);
                var response = await dataManagementClient.GetItemTipAsync(projectId, itemId, accessToken: token);

                return response.Data.Relationships.Storage.Data.Id;
            }
            catch (Exception exception)
            {
                Log.Error(exception.Message);
                Log.Error(exception.StackTrace);
                throw;
            }
        }

        public static async Task<string> MoveFileToOss(ConversionJob conversionJob, Context.RevitIfcContext revitIfcContext)
        {
            try
            {
                //Move file from WIPDM bucket (or other) into Transient bucket
                conversionJob.AddLog("Moving file to OSS");
                var objectName = conversionJob.Id.ToString() + (conversionJob.IsCompositeDesign ? ".zip" : ".rvt");

                var token = await new TwoLeggedTokenGetter().GetToken();
                // var sourceStorageLocation =
                //     await GetItemStorageLocation(conversionJob.ProjectId, conversionJob.ItemId, token);
                // var targetStorageLocation = $"{AppConfig.ForgeBaseUrl}/oss/v2/buckets/{AppConfig.BucketKey}/objects/{objectName}";

                //Return objectID
                // var objectId = await MoveFileFromDmToOss(sourceStorageLocation, targetStorageLocation, token, conversionJob, revitIfcContext);
                var objectId = await MoveFileFromDmToOss(conversionJob, revitIfcContext, token);

                conversionJob.AddLog($"Moved file to OSS: {objectId}");

                return objectId;
            }
            catch (Exception exception)
            {
                Log.Error(exception.Message);
                Log.Error(exception.StackTrace);

                conversionJob.AddLog($"FAILED to move file to OSS: {exception.Message}");
                revitIfcContext.ConversionJobs.Update(conversionJob);
                await revitIfcContext.SaveChangesAsync();
                throw;
            }
        }

        public static async Task<string> MoveFileFromDmToOss(ConversionJob conversionJob, Context.RevitIfcContext revitIfcContext, string token)
        {
            try
            {
                var objectName = conversionJob.Id.ToString() + (conversionJob.IsCompositeDesign ? ".zip" : ".rvt");
                var sourceStorageId = await GetItemTipStorageId(conversionJob.ProjectId, conversionJob.ItemId, token);
                var results = sourceStorageId.Replace("urn:adsk.objects:os.object:", string.Empty).Split(new char[] { '/' });
                if (results.Length < 2)
                    throw new InvalidOperationException("Failed to get storage info for the source RVT file.");

                var sourceStorageBucketKey = results[0];
                var sourceStorageObjectKey = results[1];

                var ossClient = new OssClient(_sdkManager);
                // var downloadInfo = await ossClient.SignedS3DownloadAsync(sourceStorageBucketKey, sourceStorageObjectKey, accessToken: token);
                // var uploadInfo = await ossClient.SignedS3UploadAsync(AppConfig.BucketKey, objectName, accessToken: token);

                // var downloadUrl = downloadInfo.Url;
                // long contentLength = downloadInfo.Size.Value;

                // var uploadStreamUrl = uploadInfo.Urls.First();
                // var uploadStreamKey = uploadInfo.UploadKey;

                // conversionJob.AddLog($"Moving file from DM to OSS. Content Length: {Math.Round((double)contentLength / 1_000_000.0)}MB");
                // revitIfcContext.ConversionJobs.Update(conversionJob);
                // await revitIfcContext.SaveChangesAsync();

                //If less than 20MB, upload normally
                // if (contentLength < 20_000_000)
                // {
                //     using Stream downloadStream = await downloadUrl
                //         .WithOAuthBearerToken(token)
                //         .GetStreamAsync(completionOption: HttpCompletionOption.ResponseHeadersRead);

                //     var content = new StreamContent(downloadStream);

                //     var response = await uploadStreamUrl
                //         .PutAsync(content);

                //     if ((HttpStatusCode)response.StatusCode != HttpStatusCode.OK)
                //         throw new InvalidOperationException("Failed to upload `" + objectName + "` to OSS.");

                //     var completeResponse = await ossClient.CompleteSignedS3UploadAsync(
                //         bucketKey: AppConfig.BucketKey,
                //         objectKey: objectName,
                //         contentType: "application/json",
                //         body: new Completes3uploadBody()
                //         {
                //             UploadKey = uploadStreamKey
                //         },
                //         accessToken: token
                //     );
                //     var apiResponse = new Autodesk.Forge.Core.ApiResponse<ObjectDetails>(completeResponse, await Autodesk.Oss.Client.LocalMarshalling.DeserializeAsync<ObjectDetails>(completeResponse.Content));

                //     if (apiResponse.HttpResponse.StatusCode != HttpStatusCode.OK)
                //         throw new InvalidOperationException("Failed to instruct uploading complete for `" + objectName + "`.");

                //     Log.Information("File fully uploaded to OSS");
                //     conversionJob.AddLog($"File fully uploaded to OSS");
                //     revitIfcContext.ConversionJobs.Update(conversionJob);
                //     await revitIfcContext.SaveChangesAsync();

                //     return apiResponse.Content.ObjectId;
                // }
                // else
                // {
                // }

                string filePath = Path.Combine(_localTempFolder, objectName);
                Directory.CreateDirectory(Path.GetDirectoryName(filePath));

                conversionJob.AddLog($"Moving file from DM to OSS.");
                revitIfcContext.ConversionJobs.Update(conversionJob);
                await revitIfcContext.SaveChangesAsync();

                await ossClient.Download(sourceStorageBucketKey, sourceStorageObjectKey, filePath, token, CancellationToken.None);
                var uploadResponse = await ossClient.Upload(AppConfig.BucketKey, objectName, filePath, token, CancellationToken.None);

                Log.Information("File fully uploaded to OSS");
                conversionJob.AddLog($"File fully uploaded to OSS");
                revitIfcContext.ConversionJobs.Update(conversionJob);
                await revitIfcContext.SaveChangesAsync();

                if (System.IO.File.Exists(filePath))
                    System.IO.File.Delete(filePath);

                return uploadResponse.ObjectId;
            }
            catch (Exception exception)
            {
                conversionJob.AddLog($"File Upload Failed: {exception.Message}");
                conversionJob.Status = ConversionJobStatus.Failed;
                revitIfcContext.ConversionJobs.Update(conversionJob);
                await revitIfcContext.SaveChangesAsync();

                Log.Error(exception.Message);
                Log.Error(exception.StackTrace);
                throw;
            }
        }

        public static async Task CreateIfcConversionJob(string hubId, string region, string projectId, string fileUrn, string itemId, string name, string folderId, string exportSettingName, string createdBy, bool isCompositeDesign, Guid? scheduleId)
        {
            try
            {
                ServiceProvider provider = (AppConfig.Services as ServiceCollection).BuildServiceProvider();
                Context.RevitIfcContext revitIfcContext = provider.GetService<Context.RevitIfcContext>();

                var schedule = await revitIfcContext.Schedules.FindAsync(scheduleId);
                var conversionJob = new ConversionJob()
                {
                    Id = Guid.NewGuid(),
                    HubId = hubId,
                    ProjectId = projectId,
                    FileUrn = fileUrn,
                    FileName = name,
                    FolderId = folderId,
                    JobSchedule = schedule,
                    IfcSettingsSetName = exportSettingName,
                    JobCreated = DateTime.UtcNow,
                    CreatedBy = createdBy,
                    ItemId = itemId,
                    Region = region,
                    IsCompositeDesign = isCompositeDesign,
                    Status = ConversionJobStatus.Created
                };

                if (string.IsNullOrWhiteSpace(conversionJob.FolderUrl))
                {
                    TokenGetter tokenGetter = new TwoLeggedTokenGetter();
                    var token = await tokenGetter.GetToken();
                    var folderData = await Forge.GetFileParentFolderData(conversionJob.ProjectId, conversionJob.ItemId, token);
                    conversionJob.FolderUrl = System.Web.HttpUtility.UrlDecode(folderData.Data.Links.WebView.Href);
                }

                //Look for identical past jobs -- if already completed, don't repeat.
                var pastSuccess = await revitIfcContext.ConversionJobs.Where(x =>
                        x.FileUrn == conversionJob.FileUrn &&
                        x.IfcSettingsSetName == conversionJob.IfcSettingsSetName &&
                        x.Status == ConversionJobStatus.Success)
                    .FirstOrDefaultAsync();

                conversionJob.AddLog("Created Job");
                revitIfcContext.ConversionJobs.Add(conversionJob);
                await revitIfcContext.SaveChangesAsync();

                if (pastSuccess != null)
                {
                    conversionJob.Status = ConversionJobStatus.Unchanged;
                    conversionJob.AddLog($"File has already been created on {pastSuccess.JobCreated}");
                    conversionJob.AddLog(pastSuccess.Id.ToString());
                    revitIfcContext.ConversionJobs.Update(conversionJob);
                    await revitIfcContext.SaveChangesAsync();
                }
                else
                {
                    BackgroundJob.Enqueue(() => BeginConversionJob(conversionJob.Id));
                }
            }
            catch (Exception exception)
            {
                Log.Error(exception.Message);
                Log.Error(exception.StackTrace);
                throw;
            }
        }

        public static async Task BeginConversionJob(Guid conversionJobId)
        {
            ServiceProvider provider = (AppConfig.Services as ServiceCollection).BuildServiceProvider();
            Context.RevitIfcContext revitIfcContext = provider.GetService<Context.RevitIfcContext>();

            try
            {

                var conversionJob = await revitIfcContext.ConversionJobs.FindAsync(conversionJobId);
                if (conversionJob.IsCompositeDesign && string.IsNullOrWhiteSpace(conversionJob.InputStorageLocation))
                {
                    conversionJob.InputStorageLocation = await MoveFileToOss(conversionJob, revitIfcContext);
                    revitIfcContext.ConversionJobs.Update(conversionJob);
                    await revitIfcContext.SaveChangesAsync();
                }

                TokenGetter tokenGetter = new TwoLeggedTokenGetter();
                var token = await tokenGetter.GetToken();

                var outputFormats = new List<JobPayloadFormat>()
                {
                    new JobIfcOutputFormat
                    {
                        Advanced = new JobIfcOutputFormatAdvanced
                        {
                            ExportSettingName = conversionJob.IfcSettingsSetName
                        }
                    }
                };

                var regionSpecifier = conversionJob.Region == "EU" ? Region.EMEA : Region.US;

                // specify Job details
                var jobPayload = new JobPayload()
                {
                    Input = new JobPayloadInput()
                    {
                        Urn = string.IsNullOrWhiteSpace(conversionJob.EncodedInputStorageLocation)
                                ? conversionJob.EncodedFileUrn
                                : conversionJob.EncodedInputStorageLocation,
                        CompressedUrn = conversionJob.IsCompositeDesign,
                        RootFilename = conversionJob.FileName
                    },
                    Output = new JobPayloadOutput()
                    {
                        Formats = outputFormats,
                        Destination = new JobPayloadOutputDestination() { Region = regionSpecifier } //!<<< New SDK will determine the API region by this field
                    },
                };

                var inputJson = JsonConvert.SerializeObject(jobPayload, Formatting.Indented);
                conversionJob.AddLog("Input JSON:");
                conversionJob.AddLog(inputJson);
                revitIfcContext.ConversionJobs.Update(conversionJob);
                await revitIfcContext.SaveChangesAsync();

                try
                {
                    var modelDerivativeClient = new ModelDerivativeClient(_sdkManager);
                    var jobResponse = await modelDerivativeClient.JobsApi.StartJobAsync(jobPayload: jobPayload, accessToken: token);

                    if (jobResponse.HttpResponse.IsSuccessStatusCode)
                    {
                        conversionJob.Status = jobResponse.HttpResponse.StatusCode == HttpStatusCode.Created
                                   ? ConversionJobStatus.Unchanged
                                   : ConversionJobStatus.Processing;

                        if (conversionJob.Status == ConversionJobStatus.Unchanged)
                        {
                            conversionJob.JobFinished = DateTime.UtcNow;
                            conversionJob.AddLog(($"This file has not changed since the last conversion to IFC. {conversionJob.Notes}").Trim());
                        }

                        BackgroundJob.Enqueue<HangfireJobs>(x => x.PollConversionJob(conversionJob.Id));

                        Log.Information($"Processing Conversion Job: {jobResponse.Content.ToString()} {conversionJob.Id}");
                    }
                }
                catch (ModelDerivativeApiException ex)
                {
                    if (ex.StatusCode == HttpStatusCode.NotAcceptable)
                    {
                        var resContent = ex.HttpResponseMessage.Content;

                        var errorDetail = await Autodesk.ModelDerivative.Client.LocalMarshalling.DeserializeAsync<ModelDerivativeError>(resContent);
                        if (errorDetail.Diagnostic == "This URN is from a shallow copy, not acceptable for any other modification.")
                        {
                            conversionJob.AddLog("This URN is from a shallow copy, not acceptable for any other modification.");
                            //Check settings to determine if the file should be moved to OSS, or if it should be marked as failed
                            if (AppConfig.IncludeShallowCopies)
                            {
                                conversionJob.AddLog("Creating Deep Copy of file in OSS");

                                //Move file to OSS
                                conversionJob.InputStorageLocation = await MoveFileToOss(conversionJob, revitIfcContext);
                                revitIfcContext.ConversionJobs.Update(conversionJob);
                                await revitIfcContext.SaveChangesAsync();

                                //Retry processing
                                BackgroundJob.Enqueue(() => BeginConversionJob(conversionJobId));
                            }
                            else
                            {
                                conversionJob.Status = ConversionJobStatus.ShallowCopy;
                            }
                        }
                        // else
                        // {
                        //     conversionJob.Status = ex.StatusCode == HttpStatusCode.Created
                        //         ? ConversionJobStatus.Unchanged
                        //         : ConversionJobStatus.Processing;

                        //     if (conversionJob.Status == ConversionJobStatus.Unchanged)
                        //     {
                        //         conversionJob.JobFinished = DateTime.UtcNow;
                        //         conversionJob.AddLog(($"This file has not changed since the last conversion to IFC. {conversionJob.Notes}").Trim());
                        //     }

                        //     BackgroundJob.Enqueue<HangfireJobs>(x => x.PollConversionJob(conversionJob.Id));

                        //     Log.Information($"Processing Conversion Job: {resContent} {conversionJob.Id}");
                        // }
                    }
                }

                revitIfcContext.ConversionJobs.Update(conversionJob);
                await revitIfcContext.SaveChangesAsync();
            }
            catch (Exception exception)
            {
                var conversionJob = await revitIfcContext.ConversionJobs.FindAsync(conversionJobId);
                conversionJob.AddLog($"Conversion Failed: {exception.Message}");
                conversionJob.Status = ConversionJobStatus.Failed;
                conversionJob.JobFinished = DateTime.UtcNow;
                revitIfcContext.ConversionJobs.Update(conversionJob);

                Log.Error(exception.Message);
                Log.Error(exception.StackTrace);
                throw;
            }
        }

        public static async Task<Manifest> GetModelDerivativeManifest(string urn, string token, string region)
        {
            var regionSpecifier = (region == "EU") ? Region.EMEA : Region.US;
            var modelDerivativeClient = new ModelDerivativeClient(_sdkManager);
            var response = await modelDerivativeClient.GetManifestAsync(urn, region: regionSpecifier, accessToken: token);

            return response;
        }

        /* Downloading File */
        public static async Task<Autodesk.DataManagement.Model.Folder> GetFileParentFolderData(string projectId, string itemId, string token)
        {
            try
            {
                var dataManagementClient = new DataManagementClient(_sdkManager);
                var response = await dataManagementClient.GetItemParentFolderAsync(projectId, itemId, accessToken: token);

                return response;
            }
            catch (Exception exception)
            {
                Log.Error("Could not get Parent Folder URN");
                throw new Exception("Could not get Parent Folder URN");
            }
        }

        public static async Task<string> GetFileParentFolderUrn(string projectId, string itemId, string token)
        {
            try
            {
                var response = await GetFileParentFolderData(projectId, itemId, token);

                return response.Data.Id;
            }
            catch (Exception exception)
            {
                Log.Error("Could not get Parent Folder URN");
                throw new Exception("Could not get Parent Folder URN");
            }
        }

        public static async Task<string> GetIfcDerivativeUrn(string fileUrn, string token, string region)
        {
            try
            {
                var data = await GetModelDerivativeManifest(fileUrn, token, region);
                var ifcDDerivative = data.Derivatives.Where(derivative => derivative.OutputType == "ifc").FirstOrDefault();
                if (ifcDDerivative != null)
                {
                    return ifcDDerivative.Children.First().Urn;
                }

                // foreach (var messageItem in ifcDDerivative.Messages)
                // {
                //     if (messageItem.code == "Revit-UnsupportedVersionOlder")
                //     {
                //         string exceptionMessage = "Revit Version Not Supported: ";
                //         foreach (var messageMessage in messageItem.Message)
                //         {
                //             exceptionMessage += messageMessage;
                //         }
                //         throw new Exception(exceptionMessage);
                //     }
                // }

                throw new Exception("IFC translation failed");
            }
            catch (Exception exception)
            {
                Log.Error($"Could not get Download URL: {exception.Message}");
                throw new Exception("Could not get Download Url");
            }
        }

        public static async Task<string> PassDownloadToStorageLocation(string derivativeUrn, string fileUrn, string storageLocation, ConversionJob conversionJob, string token)
        {
            try
            {
                var regionSpecifier = (conversionJob.Region == "EU") ? Region.EMEA : Region.US;
                var results = storageLocation.Replace("urn:adsk.objects:os.object:", string.Empty).Split(new char[] { '/' });
                if (results.Length < 2)
                    throw new InvalidOperationException("Failed to get storage info for the source RVT file.");

                var bucketKey = results[0];
                var objectName = results[1];

                var modelDerivativeClient = new ModelDerivativeClient(_sdkManager);
                var response = await modelDerivativeClient.DerivativesApi.GetDerivativeUrlAsync(derivativeUrn, fileUrn, region: regionSpecifier, accessToken: token);
                var cloudFrontPolicyName = "CloudFront-Policy";
                var cloudFrontKeyPairIdName = "CloudFront-Key-Pair-Id";
                var cloudFrontSignatureName = "CloudFront-Signature";

                var cloudFrontCookies = response.HttpResponse.Headers.GetValues("Set-Cookie");

                var cloudFrontPolicy = cloudFrontCookies.Where(value => value.Contains(cloudFrontPolicyName)).FirstOrDefault()?.Trim().Substring(cloudFrontPolicyName.Length + 1).Split(";").First();
                var cloudFrontKeyPairId = cloudFrontCookies.Where(value => value.Contains(cloudFrontKeyPairIdName)).FirstOrDefault()?.Trim().Substring(cloudFrontKeyPairIdName.Length + 1).Split(";").First();
                var cloudFrontSignature = cloudFrontCookies.Where(value => value.Contains(cloudFrontSignatureName)).FirstOrDefault()?.Trim().Substring(cloudFrontSignatureName.Length + 1).Split(";").First();

                var result = response.Content;
                var downloadUrl = result.Url.ToString();
                var queryString = "?Key-Pair-Id=" + cloudFrontKeyPairId + "&Signature=" + cloudFrontSignature + "&Policy=" + cloudFrontPolicy;
                downloadUrl += queryString;

                string filePath = Path.Combine(_localTempFolder, objectName);
                Directory.CreateDirectory(Path.GetDirectoryName(filePath));

                var downloadStream = await downloadUrl
                        .WithOAuthBearerToken(token)
                        .GetStreamAsync(completionOption: HttpCompletionOption.ResponseHeadersRead);

                using (FileStream outputFileStream = new FileStream(filePath, FileMode.Create))
                {
                    // downloadStream.Seek(0, SeekOrigin.Begin);
                    downloadStream.CopyTo(outputFileStream);
                }

                var ossClient = new OssClient(_sdkManager);
                var uploadResponse = await ossClient.Upload(bucketKey, objectName, filePath, token, CancellationToken.None);

                Log.Information("Successful Upload");

                if (System.IO.File.Exists(filePath))
                    System.IO.File.Delete(filePath);
                
                return uploadResponse.ObjectId;
            }
            catch (Exception exception)
            {
                Log.Error(exception.Message);
                Log.Error(exception.StackTrace);
                throw;
            }
        }

        /* Uploading Code */

        public static async Task<string> CreateStorageLocation(string projectId, string folderId, string derivativeUrn,
            string token)
        {
            var fileName = derivativeUrn.Split('/').Last();

            var dataManagementClient = new DataManagementClient(_sdkManager);
            var storagePayload = new StoragePayload()
            {
                Jsonapi = new ModifyFolderPayloadJsonapi()
                {
                    _Version = VersionNumber._10
                },
                Data = new StoragePayloadData()
                {
                    Type = Autodesk.DataManagement.Model.Type.Objects,
                    Attributes = new StoragePayloadDataAttributes()
                    {
                        Name = fileName
                    },
                    Relationships = new StoragePayloadDataRelationships()
                    {
                        Target = new ModifyFolderPayloadDataRelationshipsParent()
                        {
                            Data = new ModifyFolderPayloadDataRelationshipsParentData()
                            {
                                Type = Autodesk.DataManagement.Model.Type.Folders,
                                Id = folderId
                            }
                        }
                    }
                }
            };
            var response = await dataManagementClient.CreateStorageAsync(projectId, storagePayload: storagePayload, accessToken: token);

            return response.Data.Id;
        }

        public static async Task<string> GetExistingVersion(string projectId, string folderId, string fileName,
            string suffix, string token)
        {
            try
            {
                var name = fileName.Split('.').First();
                if (string.IsNullOrWhiteSpace(suffix))
                    name = name + ".ifc";
                else
                    name = name + suffix + ".ifc";

                var folderContents = await GetFolderContents(projectId, folderId, token);

                foreach (var file in folderContents)
                {
                    if (file is File && file.Name == name)
                    {
                        //Return existing URN
                        return (file as File).ItemId;
                    }
                }

                return null;

            }
            catch (Exception exception)
            {
                throw;
            }
        }

        public static async Task CreateFirstVersion(string projectId, string folderId, string objectId,
            string fileName, string suffix, string token)
        {
            try
            {
                var name = fileName.Split('.').First();
                if (string.IsNullOrWhiteSpace(suffix))
                    name = name + ".ifc";
                else
                    name = name + suffix + ".ifc";

                var dataManagementClient = new DataManagementClient(_sdkManager);
                var itemPayload = new ItemPayload()
                {
                    Jsonapi = new ModifyFolderPayloadJsonapi()
                    {
                        _Version = VersionNumber._10
                    },
                    Data = new ItemPayloadData()
                    {
                        Type = Autodesk.DataManagement.Model.Type.Items,
                        Attributes = new ItemPayloadDataAttributes()
                        {
                            DisplayName = name,
                            Extension = new ItemPayloadDataAttributesExtension()
                            {
                                Type = Autodesk.DataManagement.Model.Type.ItemsautodeskBim360File,
                                _Version = VersionNumber._10
                            }
                        },
                        Relationships = new ItemPayloadDataRelationships()
                        {
                            Tip = new FolderPayloadDataRelationshipsParent()
                            {
                                Data = new FolderPayloadDataRelationshipsParentData()
                                {
                                    Type = Autodesk.DataManagement.Model.Type.Versions,
                                    Id = "1"
                                }
                            },
                            Parent = new FolderPayloadDataRelationshipsParent()
                            {
                                Data = new FolderPayloadDataRelationshipsParentData()
                                {
                                    Type = Autodesk.DataManagement.Model.Type.Folders,
                                    Id = folderId
                                }
                            }
                        }
                    },
                    Included = new List<ItemPayloadIncluded>()
                    {
                        new ItemPayloadIncluded()
                        {
                            Type = Autodesk.DataManagement.Model.Type.Versions,
                            Id = "1",
                            Attributes = new ItemPayloadIncludedAttributes()
                            {
                                Name = name,
                                Extension = new ItemPayloadDataAttributesExtension()
                                {
                                    Type = Autodesk.DataManagement.Model.Type.VersionsautodeskBim360File,
                                    _Version = VersionNumber._10
                                }
                            },
                            Relationships = new ItemPayloadIncludedRelationships()
                            {
                                Storage = new FolderPayloadDataRelationshipsParent()
                                {
                                    Data = new FolderPayloadDataRelationshipsParentData()
                                    {
                                        Type = Autodesk.DataManagement.Model.Type.Objects,
                                        Id = objectId
                                    }
                                }
                            }
                        }
                    }
                };

                await dataManagementClient.CreateItemAsync(projectId, itemPayload: itemPayload, accessToken: token);

                Log.Information("Success");
            }
            catch (Exception exception)
            {
                Log.Error(exception.Message);
                throw;
            }
        }


        public static async Task CreateSubsequentVersion(string projectId, string itemId, string objectId,
            string fileName, string suffix, string token)
        {
            try
            {
                var name = fileName.Split('.').First();
                if (string.IsNullOrWhiteSpace(suffix))
                    name = name + ".ifc";
                else
                    name = name + suffix + ".ifc";


                var dataManagementClient = new DataManagementClient(_sdkManager);
                var versionPayload = new VersionPayload()
                {
                    Jsonapi = new ModifyFolderPayloadJsonapi()
                    {
                        _Version = VersionNumber._10
                    },
                    Data = new VersionPayloadData()
                    {
                        Type = Autodesk.DataManagement.Model.Type.Versions,
                        Attributes = new VersionPayloadDataAttributes()
                        {
                            Name = name,
                            Extension = new RelationshipRefsPayloadDataMetaExtension()
                            {
                                Type = Autodesk.DataManagement.Model.Type.VersionsautodeskBim360File,
                                _Version = VersionNumber._10
                            }
                        },
                        Relationships = new VersionPayloadDataRelationships()
                        {
                            Item = new FolderPayloadDataRelationshipsParent()
                            {
                                Data = new FolderPayloadDataRelationshipsParentData()
                                {
                                    Type = Autodesk.DataManagement.Model.Type.Items,
                                    Id = itemId
                                }
                            },
                            Storage = new FolderPayloadDataRelationshipsParent()
                            {
                                Data = new FolderPayloadDataRelationshipsParentData()
                                {
                                    Type = Autodesk.DataManagement.Model.Type.Objects,
                                    Id = objectId
                                }
                            }
                        }
                    }
                };
                await dataManagementClient.CreateVersionAsync(projectId, versionPayload: versionPayload, accessToken: token);

                Log.Information("Success");
            }
            catch (Exception exception)
            {
                Log.Error(exception.Message);
                throw;
            }

        }
    }

    static class ListExtension
    {
        public static T PopAt<T>(this List<T> list, int index)
        {
            T r = list[index];
            list.RemoveAt(index);
            return r;
        }
    }
}