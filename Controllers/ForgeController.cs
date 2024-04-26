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
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using RevitToIfcScheduler.Context;
using RevitToIfcScheduler.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Serilog;
using RevitToIfcScheduler.Utilities;

namespace RevitToIfcScheduler.Controllers
{
    public class ForgeController: ControllerBase
    {
        
        public ForgeController(Context.RevitIfcContext revitIfcContext)
        {
            RevitIfcContext = revitIfcContext;
        }
         
        private static readonly HttpClient client = new HttpClient();
        private Context.RevitIfcContext RevitIfcContext { get; set; }
        
        
        [HttpGet]
        [Route("api/forge/accounts")]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult> GetForgeAccounts()
        {
            try
            {
                if (!Authentication.IsAuthorized(HttpContext, RevitIfcContext, new List<AccountRole>(){AccountRole.AccountAdmin, AccountRole.ApplicationAdmin})) return Unauthorized();
                
                string token = await TokenManager.GetTwoLeggedToken();
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                var url = $"https://developer.api.autodesk.com/project/v1/hubs";

                var result = client.GetAsync(url).Result;
                if (result.IsSuccessStatusCode)
                {
                    var jsonResponse = result.Content.ReadAsStringAsync().Result;
                    dynamic data = JsonConvert.DeserializeObject<dynamic>(jsonResponse);
                    
                    List<dynamic> responseData = new List<dynamic>();
                    List<Account> accounts = RevitIfcContext.Accounts.ToList();
                    
                    foreach (dynamic hub in data.data)
                    {
                        var hubId = (string)hub.id;
                        var includedAccounts = accounts.Find(x => x.Id == hubId);
                        responseData.Add(new
                        {
                            id=hub.id,
                            name=hub.attributes.name,
                            region=hub.attributes.region,
                            enabled=!(includedAccounts == null)
                        }); 
                    }
                    
                    return Ok(responseData);
                }
                else
                {
                    throw new Exception(result.StatusCode.ToString());
                }
            }
            catch (Exception ex)
            {
                Log.Debug(ex, this.GetType().FullName);
                return BadRequest(ex);
            }
        }
        
        [HttpGet]
        [Route("api/forge/projects")]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult> GetForgeProjects()
        {
            try
            {
                if (!Authentication.IsAuthorized(HttpContext, RevitIfcContext, new List<AccountRole>(){AccountRole.AccountAdmin, AccountRole.ProjectAdmin, AccountRole.ApplicationAdmin})) return Unauthorized();
                
                var user = RevitToIfcScheduler.Models.User.FetchByContext(HttpContext, RevitIfcContext);
                var projects = new List<Project>();
                var client = new HttpClient();
                
                
                
                foreach (AccountPermissions singleAccount in user.Permissions)
                {
                    if (string.IsNullOrWhiteSpace(singleAccount.HubId)) continue;
                    
                    if (singleAccount.Role == AccountRole.AccountAdmin || user.HasPermission(AccountRole.ApplicationAdmin))
                    {
                        var twoLeggedToken = await TokenManager.GetTwoLeggedToken();
                        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", twoLeggedToken);
                    } else if (singleAccount.Role == AccountRole.ProjectAdmin)
                    {
                        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", user.Token);
                    }
                    else
                    {
                        continue;
                    }

                    var url = $"https://developer.api.autodesk.com/project/v1/hubs/{singleAccount.HubId}/projects?page[number]=0";
                    while (true)
                    {
                        var result = await client.GetAsync(url);

                        if (result.IsSuccessStatusCode)
                        {
                            var jsonResponse = result.Content.ReadAsStringAsync().Result;
                            dynamic data = JsonConvert.DeserializeObject<dynamic>(jsonResponse);

                            foreach (dynamic project in data.data)
                            {
                                projects.Add(new Project()
                                {
                                    HubId = singleAccount.HubId,
                                    Id = project.id,
                                    Name = project.attributes.name
                                });
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
                    }
                }
                
                
                return Ok(projects);
            }
            catch (Exception ex)
            {
                Log.Debug(ex, this.GetType().FullName);
                return BadRequest(ex);
            }
        }
        
        [HttpGet]
        [Route("api/forge/accounts/{hubId}/projects/{projectId}/topFolders")]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult> GetForgeProjectTopFolders(string hubId, string projectId)
        {
            try
            {
                if (!Authentication.IsAuthorized(HttpContext, RevitIfcContext, new List<AccountRole>(){AccountRole.AccountAdmin, AccountRole.ProjectAdmin, AccountRole.ApplicationAdmin}, projectId)) return Unauthorized();

                var token = await GetProjectToken(HttpContext, RevitIfcContext, projectId);
                var folders = await Forge.GetTopFolders(hubId, projectId, token);
                return Ok(folders);
            }
            catch (Exception ex)
            {
                Log.Debug(ex, this.GetType().FullName);
                return BadRequest(ex);
            }
        }

        [HttpGet]
        [Route("api/forge/projects/{projectId}/folders/{folderId}")]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult> GetForgeProjectFolderContents(string projectId, string folderId)
        {
            try
            {
                if (!Authentication.IsAuthorized(HttpContext, RevitIfcContext, new List<AccountRole>(){AccountRole.AccountAdmin, AccountRole.ProjectAdmin, AccountRole.ApplicationAdmin}, projectId)) return Unauthorized();

                var token = await GetProjectToken(HttpContext, RevitIfcContext, projectId);
                var contents = await Forge.GetFolderContents(projectId, folderId, token);
                var contentsToReturn = new List<Base>();

                //Filter out any IFC files
                foreach (var item in contents)
                {
                    if (item is File && (item as File).FileType == "ifc")
                    {
                        
                    }
                    else
                    {
                        contentsToReturn.Add(item);
                    }
                }
                
                
                return Ok(contentsToReturn);
            }
            catch (Exception ex)
            {
                Log.Debug(ex, this.GetType().FullName);
                return BadRequest(ex);
            }
        }

        private async Task<string> GetProjectToken(HttpContext httpContext, Context.RevitIfcContext revitIfcContext, string projectId)
        {
            var user = RevitToIfcScheduler.Models.User.FetchByContext(httpContext, revitIfcContext);
                
            foreach(var permissions in user.Permissions)
            {
                if (permissions.ProjectIds.Contains(projectId))
                {
                    if (permissions.Role == AccountRole.AccountAdmin || user.HasPermission(AccountRole.ApplicationAdmin))
                    {
                        var twoLeggedToken = await TokenManager.GetTwoLeggedToken();
                        return twoLeggedToken;
                    } 
                    if (permissions.Role == AccountRole.ProjectAdmin)
                    {
                        return user.Token;
                    }
                }
            }
            throw new Exception("User Not Authorized");
        }
    }
}