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

using System.Collections.Generic;
using System.Threading.Tasks;
using RevitToIfcScheduler.Models;

namespace RevitToIfcScheduler.Utilities
{
    public static class Converter
    {
        public static async Task ProcessConversionBatch(User user, Context.RevitIfcContext revitIfcContext, string projectId, ConversionBatch conversionBatch, Schedule schedule)
        {
            
            var finalFiles = new List<File>();

            if (user.HasPermission(AccountRole.AccountAdmin, projectId))
            {
                finalFiles = await APS.GetAllChildRevitFiles(projectId, conversionBatch.FolderUrns,
                    new TwoLeggedTokenGetter());
            } else if (user.HasPermission(AccountRole.ProjectAdmin, projectId))
            {
                finalFiles = await APS.GetAllChildRevitFiles(projectId, conversionBatch.FolderUrns,
                    new ThreeLeggedTokenGetter(user, revitIfcContext));
            }

            foreach (var file in conversionBatch.Files)
            {
                if (finalFiles.Find(x => x.ItemId == file.ItemId) == null)
                {
                    finalFiles.Add(file);
                }
            }
            
            var account = user.GetAccountFromProjectId(projectId);
            await CreateConversionJobs(account.HubId, account.Region, projectId, finalFiles, conversionBatch.ifcSettingsName, user.Email, schedule);
        }

        public static async Task CreateConversionJobs(string hubId, string region, string projectId,
            List<File> files, string exportSettingName, string createdBy, Schedule schedule)
        {
            foreach (File file in files)
            {
                if (schedule != null)
                {
                    await APS.CreateIfcConversionJob(hubId, region, projectId, file.Id, file.ItemId, file.Name, file.FolderId, exportSettingName, createdBy, file.IsCompositeDesign, schedule.Id);
                }
                else
                {
                    await APS.CreateIfcConversionJob(hubId, region, projectId, file.Id, file.ItemId, file.Name, file.FolderId, exportSettingName, createdBy, file.IsCompositeDesign, null);
                }
            }
        }
    }
}