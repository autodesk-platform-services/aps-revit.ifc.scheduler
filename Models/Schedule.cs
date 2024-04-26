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
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using RevitToIfcScheduler.Utilities;

namespace RevitToIfcScheduler.Models
{
    public class ScheduleStarter
    {
        [JsonProperty("projectId")] public string ProjectId { get; set; }
        [JsonProperty("name")] public string Name { get; set; }
        [JsonProperty("cron")] public string Cron { get; set; }
        [JsonProperty("timeZoneId")] public string TimeZoneId { get; set; }
        [JsonProperty("ifcSettingsName")] public string IfcSettingsName { get; set; }
        [JsonProperty("lastFileCount")] public int? LastFileCount { get; set; }
        [JsonProperty("folderUrns")] public List<string> FolderUrns { get; set; }
        [JsonProperty("files")] public List<File> Files { get; set; }
    }
    
    public class Schedule
    {
        [Key]
        [JsonProperty("id")] public Guid Id { get; set; }
        [JsonProperty("hubId")] public string HubId { get; set; }
        [JsonProperty("region")] public string Region { get; set; }
        [JsonProperty("projectId")] public string ProjectId { get; set; }
        [JsonProperty("name")] public string Name { get; set; }
        [JsonProperty("cron")] public string Cron { get; set; }
        [JsonProperty("timeZoneId")] public string TimeZoneId { get; set; } = TimeZoneInfo.Local.Id;
        [JsonProperty("ifcSettingsName")] public string IfcSettingsName { get; set; }
        [JsonIgnore] public string SerializedFolderUrns { get; set; } = "[]";
        [JsonIgnore] public string SerializedFileUrns { get; set; } = "[]";
        [JsonProperty("lastStart")] public DateTime? LastStart { get; set; }
        [JsonProperty("lastFileCount")] public int? LastFileCount { get; set; } = 0;
        [JsonProperty("createdBy")] public string CreatedBy { get; set; }
        [JsonProperty("editedBy")] public string EditedBy { get; set; }

        [NotMapped]
        [JsonProperty("folderUrns")]
        public List<string> FolderUrns
        {
            get { return JsonConvert.DeserializeObject<List<string>>(SerializedFolderUrns); }
            set { SerializedFolderUrns = JsonConvert.SerializeObject(value); }
        }

        [NotMapped]
        [JsonProperty("files")]
        public List<File> Files
        {
            get { return JsonConvert.DeserializeObject<List<File>>(SerializedFileUrns); }
            set { SerializedFileUrns = JsonConvert.SerializeObject(value); }
        }

        public async static Task Run(Guid id)
        {
            ServiceProvider provider = (AppConfig.Services as ServiceCollection).BuildServiceProvider();
            Context.RevitIfcContext revitIfcContext = provider.GetService<Context.RevitIfcContext>();

            var schedule = await revitIfcContext.Schedules.FindAsync(id);

            var filesFromFolders = await Forge.GetAllChildRevitFiles(schedule.ProjectId, schedule.FolderUrns, new TwoLeggedTokenGetter());

            foreach (var file in schedule.Files)
            {
                filesFromFolders.Add(file);
            }
            

            await Converter.CreateConversionJobs(schedule.HubId, schedule.Region, schedule.ProjectId, filesFromFolders,
                schedule.IfcSettingsName, schedule.Name, schedule);
            
            schedule.LastStart = DateTime.Now;
            schedule.LastFileCount = filesFromFolders.Count;
            revitIfcContext.Update(schedule);
            await revitIfcContext.SaveChangesAsync();
        }
    }
}