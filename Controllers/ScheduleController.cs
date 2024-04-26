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
using System.Linq;
using System.Threading.Tasks;
using RevitToIfcScheduler.Context;
using RevitToIfcScheduler.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Serilog;
using RevitToIfcScheduler.Utilities;
using Hangfire;

namespace RevitToIfcScheduler.Controllers
{
    public class ScheduleController: ControllerBase
    {
        public ScheduleController(Context.RevitIfcContext revitIfcContext)
        {
            RevitIfcContext = revitIfcContext;
        }
         
        private Context.RevitIfcContext RevitIfcContext { get; set; }
        
        
        [HttpGet]
        [Route("api/projects/{projectId}/schedules")]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult> GetSchedules(string projectId)
        {
            try
            {
                if (!Authentication.IsAuthorized(HttpContext, RevitIfcContext, new List<AccountRole>(){AccountRole.AccountAdmin, AccountRole.ProjectAdmin, AccountRole.ApplicationAdmin}, projectId)) return Unauthorized();
                var items = await RevitIfcContext.Schedules
                    .Where(x=>x.ProjectId == projectId)
                    .ToArrayAsync();
                
                return Ok(items);
            }
            catch (Exception ex)
            {
                Log.Debug(ex, GetType().FullName);
                return BadRequest(ex);
            }
        }
        
        
        [HttpPost]
        [Route("api/projects/{projectId}/schedules")]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult> PostSchedule([FromBody] ScheduleStarter newItem, string projectId)
        {
            try
            {
                if (!Authentication.IsAuthorized(HttpContext, RevitIfcContext, new List<AccountRole>(){AccountRole.AccountAdmin, AccountRole.ProjectAdmin, AccountRole.ApplicationAdmin}, projectId)) return Unauthorized();
                var user = RevitToIfcScheduler.Models.User.FetchByContext(HttpContext, RevitIfcContext);
                
                var account = user.GetAccountFromProjectId(projectId);
                var schedule = new Schedule()
                {
                    HubId = account.HubId,
                    Region = account.Region,
                    ProjectId = newItem.ProjectId,
                    Name = newItem.Name,
                    Cron = newItem.Cron,
                    TimeZoneId = newItem.TimeZoneId,
                    IfcSettingsName = newItem.IfcSettingsName,
                    LastFileCount = newItem.LastFileCount,
                    CreatedBy = user.Email,
                    FolderUrns = newItem.FolderUrns,
                    Files = newItem.Files
                };
                
                await RevitIfcContext.Schedules.AddAsync(schedule);
                
                var existingIfcSettingsSet = RevitIfcContext.IfcSettingsSets
                    .Where(x => x.Name == schedule.IfcSettingsName)
                    .ToList();
                if (existingIfcSettingsSet.Count == 0)
                {
                    RevitIfcContext.IfcSettingsSets.Add(new IfcSettingsSet()
                    {
                        Name = schedule.IfcSettingsName
                    });
                }
                
                await RevitIfcContext.SaveChangesAsync();
                
                RecurringJob.AddOrUpdate(schedule.Id.ToString(), () => Schedule.Run(schedule.Id), 
                    schedule.Cron, TimeZoneInfo.FindSystemTimeZoneById(schedule.TimeZoneId));

                return Created($"/api/projects/{projectId}/schedules/{schedule.Id}", schedule);
            }
            catch (Exception ex)
            {
                Log.Debug(ex, GetType().FullName);
                return BadRequest(ex);
            }
        }
        
        
        [HttpPatch]
        [Route("api/projects/{projectId}/schedules/{id}")]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult> PatchSchedule([FromBody] ScheduleStarter updateItem, string projectId, Guid id )
        {
            try
            {
                if (!Authentication.IsAuthorized(HttpContext, RevitIfcContext, new List<AccountRole>(){AccountRole.AccountAdmin, AccountRole.ProjectAdmin, AccountRole.ApplicationAdmin}, projectId)) return Unauthorized();
                var user = RevitToIfcScheduler.Models.User.FetchByContext(HttpContext, RevitIfcContext);
                var item = await RevitIfcContext.Schedules.FindAsync(id);
                if (item == null) return NotFound(id);

                item.EditedBy = user.Email;
                if (updateItem.Name != null)
                {
                    item.Name = updateItem.Name;
                }

                if (updateItem.IfcSettingsName != null)
                {
                    item.IfcSettingsName = updateItem.IfcSettingsName;
                    var existingIfcSettingsSet = RevitIfcContext.IfcSettingsSets
                        .Where(x => x.Name == updateItem.IfcSettingsName)
                        .ToList();
                    if (existingIfcSettingsSet.Count == 0)
                    {
                        RevitIfcContext.IfcSettingsSets.Add(new IfcSettingsSet()
                        {
                            Name = updateItem.IfcSettingsName
                        });
                    }
                }

                if (updateItem.Cron != null)
                {
                    item.Cron = updateItem.Cron;
                }

                if (updateItem.TimeZoneId != null)
                {
                    item.TimeZoneId = updateItem.TimeZoneId;
                }

                if (updateItem.Files != null)
                {
                    item.Files = updateItem.Files;
                }

                if (updateItem.FolderUrns != null)
                {
                    item.FolderUrns = updateItem.FolderUrns;
                }

                if (updateItem.LastFileCount != null)
                {
                    item.LastFileCount = updateItem.LastFileCount;
                }
                
                RevitIfcContext.Schedules.Update(item);
                await RevitIfcContext.SaveChangesAsync();
                
                RecurringJob.AddOrUpdate(item.Id.ToString(), () => Schedule.Run(item.Id), item.Cron, 
                    TimeZoneInfo.FindSystemTimeZoneById(item.TimeZoneId));
                return Ok(item);
            }
            catch (Exception ex)
            {
                Log.Debug(ex, GetType().FullName);
                return BadRequest(ex);
            }
        }
        
        
        [HttpDelete]
        [Route("api/projects/{projectId}/schedules/{id}")]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult> DeleteSchedule(string projectId, Guid id)
        {
            try
            {
                if (!Authentication.IsAuthorized(HttpContext, RevitIfcContext, new List<AccountRole>(){AccountRole.AccountAdmin, AccountRole.ProjectAdmin, AccountRole.ApplicationAdmin}, projectId)) return Unauthorized();
                var item = await RevitIfcContext.Schedules.FindAsync(id);
                if (item == null) return NotFound(id);
                
                //Set connected job schedules to null, and add a note
                var scheduleConversions = (await RevitIfcContext.ConversionJobs
                        .Where(x => x.JobSchedule == item)
                        .ToListAsync())
                    .Select(x =>
                    {
                        x.JobSchedule = null;
                        x.AddLog($"Run using a deleted schedule named {item.Name}");
                        return x;
                    });
                
                RevitIfcContext.ConversionJobs.UpdateRange(scheduleConversions);
                RevitIfcContext.Schedules.Remove(item);

                await RevitIfcContext.SaveChangesAsync();
                RecurringJob.RemoveIfExists(item.Id.ToString());
                return NoContent();
            }
            catch (Exception ex)
            {
                Log.Debug(ex, GetType().FullName);
                return BadRequest(ex);
            }
        }
    }
}