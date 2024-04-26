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
using System.Threading.Tasks;
using RevitToIfcScheduler.Context;
using RevitToIfcScheduler.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Serilog;
using RevitToIfcScheduler.Utilities;

namespace RevitToIfcScheduler.Controllers
{
    public class IfcSettingsController: ControllerBase
    {
        public IfcSettingsController(Context.RevitIfcContext revitIfcContext)
        {
            RevitIfcContext = revitIfcContext;
        }
         
        private Context.RevitIfcContext RevitIfcContext { get; set; }
        
        
        [HttpGet]
        [Route("api/ifcSettings")]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult> GetIfcSettings()
        {
            try
            {
                if (!Authentication.IsAuthorized(HttpContext, RevitIfcContext)) return Unauthorized();
                var items = await RevitIfcContext.IfcSettingsSets.ToArrayAsync();
                
                return Ok(items);
            }
            catch (Exception ex)
            {
                Log.Debug(ex, this.GetType().FullName);
                return BadRequest(ex);
            }
        }
        
        
        [HttpPost]
        [Route("api/ifcSettings")]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult> PostIfcSettings([FromBody] IfcSettingsSet newItem)
        {
            try
            {
                if (!Authentication.IsAuthorized(HttpContext, RevitIfcContext, new List<AccountRole>(){AccountRole.AccountAdmin, AccountRole.ApplicationAdmin})) return Unauthorized();

                var existingSets = await RevitIfcContext.IfcSettingsSets.ToListAsync();
                
                var item = new IfcSettingsSet(){
                    Name = newItem.Name,
                    IsDefault = existingSets.Count == 0
                };

                await RevitIfcContext.IfcSettingsSets.AddAsync(item);
                await RevitIfcContext.SaveChangesAsync();

                return Created($"/api/ifcSettings/{item.Id}", item);
            }
            catch (Exception ex)
            {
                Log.Debug(ex, this.GetType().FullName);
                return BadRequest(ex);
            }
        }
        
        
        [HttpPatch]
        [Route("api/ifcSettings/{id}")]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult> PatchIfcSettings([FromBody] IfcSettingsSet updateItem, Guid id )
        {
            try
            {
                if (!Authentication.IsAuthorized(HttpContext, RevitIfcContext, new List<AccountRole>(){AccountRole.AccountAdmin, AccountRole.ApplicationAdmin})) return Unauthorized();
                var item = await RevitIfcContext.IfcSettingsSets.FindAsync(id);
                if (item == null) return NotFound(id);

                if (updateItem.Name != null)
                {
                    item.Name = updateItem.Name;
                }

                if (updateItem.IsDefault)
                {
                    item.IsDefault = true;
                    var allIfcSettings = await RevitIfcContext.IfcSettingsSets.ToListAsync();
                    foreach (var settings in allIfcSettings)
                    {
                        if (settings.Id != item.Id && settings.IsDefault)
                        {
                            settings.IsDefault = false;
                            RevitIfcContext.IfcSettingsSets.Update(settings);
                        }
                    }
                }
                
                RevitIfcContext.IfcSettingsSets.Update(item);
                await RevitIfcContext.SaveChangesAsync();
                return Ok(item);
            }
            catch (Exception ex)
            {
                Log.Debug(ex, this.GetType().FullName);
                return BadRequest(ex);
            }
        }
        
        
        [HttpDelete]
        [Route("api/ifcSettings/{id}")]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult> DeleteIfcSettings(Guid id)
        {
            try
            {
                if (!Authentication.IsAuthorized(HttpContext, RevitIfcContext, new List<AccountRole>(){AccountRole.AccountAdmin})) return Unauthorized();
                var item = await RevitIfcContext.IfcSettingsSets.FindAsync(id);
                if (item == null) return NotFound(id);
                RevitIfcContext.IfcSettingsSets.Remove(item);

                await RevitIfcContext.SaveChangesAsync();
                return NoContent();
            }
            catch (Exception ex)
            {
                Log.Debug(ex, this.GetType().FullName);
                return BadRequest(ex);
            }
        }
    }
}