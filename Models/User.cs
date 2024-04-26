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
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Serilog;
using Autodesk.SDKManager;
using Autodesk.Authentication;

namespace RevitToIfcScheduler.Models
{
    public class User
    {
        //Hash this
        [Key]
        [JsonIgnore]
        public Guid Id { get; set; }
        
        [JsonIgnore]
        public string HashedSessionKey { get; set; }
        
        [JsonProperty("autodeskId")]
        public string AutodeskId { get; set; }
        
        [JsonProperty("email")]
        public string Email { get; set; }
        
        [JsonProperty("name")]
        public string Name { get; set; }
        
        [JsonProperty("profilePicture")]
        public string ProfilePicture { get; set; }
        
        [JsonIgnore]
        public string SerializedPermissions { get; set; }

        [NotMapped]
        [JsonProperty("permissions")]
        public List<AccountPermissions> Permissions
        {
            get
            {
                if (SerializedPermissions == null) return new List<AccountPermissions>();
                return JsonConvert.DeserializeObject<List<AccountPermissions>>(SerializedPermissions);}
            set { SerializedPermissions = JsonConvert.SerializeObject(value); }
        }
        
        [JsonIgnore]
        public string EncryptedToken { get; set; }
        
        [JsonIgnore]
        public string EncryptedRefresh { get; set; }
        
        [JsonIgnore]
        public DateTime TokenExpiration { get; set; }


        [NotMapped]
        [JsonIgnore]
        public string Token
        {
            get { return AppConfig.DataProtector.Unprotect(EncryptedToken) ; }
            set { EncryptedToken = AppConfig.DataProtector.Protect(value); }
        }
        [NotMapped]
        [JsonIgnore]
        public string Refresh
        {
            get { return AppConfig.DataProtector.Unprotect(EncryptedRefresh) ; }
            set { EncryptedRefresh = AppConfig.DataProtector.Protect(value); }
        }

        private static readonly SDKManager _sdkManager = SdkManagerBuilder.Create().Build();

        public async Task FetchAutodeskDetails()
        {
            try
            {
                var authenticationClient = new AuthenticationClient(_sdkManager);
                var response = await authenticationClient.GetUserInfoAsync(Token);
                if (response != null)
                {
                    AutodeskId = response.Sub;
                    Email = response.Email;
                    Name = response.Name ?? response.GivenName + " " + response.FamilyName;
                    ProfilePicture = response.Picture;
                }
            }
            catch (Exception exception)
            {
                Log.Error(exception.Message);
            }
        }

        public async Task FetchPermissions(List<Account> accounts, string twoLeggedToken)
        {
            try
            {
                var permissions = new List<AccountPermissions>();
                var client = new HttpClient();
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", twoLeggedToken);

                if (AppConfig.AdminEmails.Contains(Email))
                {
                    
                    var accountPermission = new AccountPermissions()
                    {
                        HubId = "",
                        Region = "",
                        Role = AccountRole.ApplicationAdmin,
                        ProjectIds = new List<string>()
                    };
                    
                    permissions.Add(accountPermission);
                }
                
                foreach (var account in accounts)
                {
                    var url = account.Region == "EU"
                        ? $@"https://developer.api.autodesk.com/hq/v1/regions/eu/accounts/{account.AccountId}/users/{AutodeskId}"
                        : $@"https://developer.api.autodesk.com/hq/v1/accounts/{account.AccountId}/users/{AutodeskId}";

                    var result = await client.GetAsync(url);

                    if (result.IsSuccessStatusCode)
                    {
                        var jsonResponse = result.Content.ReadAsStringAsync().Result;
                        dynamic data = JsonConvert.DeserializeObject<dynamic>(jsonResponse);

                                              
                        
                        if (data.role == "account_admin" && data.status == "active" || AppConfig.AdminEmails.Contains(Email))
                        {
                            var accountPermission = new AccountPermissions()
                            {
                                HubId = account.HubId,
                                Region = account.Region,
                                Role = AccountRole.AccountAdmin,
                                ProjectIds = new List<string>()
                            };
                            
                            accountPermission.ProjectIds = await GetAuthorizedProjects(account.HubId, twoLeggedToken);
                            
                            permissions.Add(accountPermission);
                        }
                        else if (data.role == "project_admin" && data.status == "active")
                        {
                            var accountPermission = new AccountPermissions()
                            {
                                HubId = account.HubId,
                                Region = account.Region,
                                Role = AccountRole.ProjectAdmin,
                                ProjectIds = new List<string>()
                            };

                            //Fetch Projects
                            accountPermission.ProjectIds = await GetAuthorizedProjects(account.HubId, null);

                            permissions.Add(accountPermission);
                        }
                    }
                }

                Permissions = permissions;
            }
            catch (Exception exception)
            {
                Log.Error(exception.Message);
                throw exception;
            }
        }
        
        private async Task<List<string>> GetAuthorizedProjects(string hubId, string twoLeggedToken)
        {
            try
            {
                var projectIds = new List<string>();

                var client = new HttpClient();
                client.DefaultRequestHeaders.Authorization = 
                    new AuthenticationHeaderValue("Bearer", twoLeggedToken ?? Token);

                var url = $"https://developer.api.autodesk.com/project/v1/hubs/{hubId}/projects?page[number]=0";
                while (true)
                {
                    var result = await client.GetAsync(url);

                    if (result.IsSuccessStatusCode)
                    {
                        var jsonResponse = result.Content.ReadAsStringAsync().Result;
                        dynamic data = JsonConvert.DeserializeObject<dynamic>(jsonResponse);

                        foreach (dynamic project in data.data)
                        {
                            string projectId = project.id;
                            projectIds.Add(projectId);
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

                return projectIds;
            }
            catch (Exception exception)
            {
                Log.Error(exception.Message);
                throw exception;
            }
        }

        public static User FetchByContext(HttpContext httpContext, Context.RevitIfcContext revitIfcContext)
        {
            try
            {
                var hashedSessionKey = User.ComputeSha256Hash(httpContext.Request.Cookies[AppConfig.AppId]);
                var user = revitIfcContext.Users.First(u=>u.HashedSessionKey == hashedSessionKey);
                return user;
            }
            catch (Exception exception)
            {
                throw exception;
            }
        }

        public bool HasPermission(AccountRole accountRole, string projectId)
        {
            var account = GetAccountFromProjectId(projectId);

            return account != null &&  account.Role == accountRole;
        }

        public bool HasPermission(AccountRole accountRole)
        {
            foreach (var account in Permissions)
            {
                if (account.Role == accountRole)
                {
                    return true;
                }
            }

            return false;
        }

        public AccountPermissions GetAccountFromProjectId(string projectId)
        {
            foreach (var account in Permissions)
            {
                if (account.ProjectIds.Contains(projectId))
                {
                    return account;
                }
            }
            return null;
        }
        
        public static string ComputeSha256Hash(string rawData)  
        {  
            // Create a SHA256   
            using (SHA256 sha256Hash = SHA256.Create())  
            {  
                // ComputeHash - returns byte array  
                byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(rawData));  
  
                // Convert byte array to a string   
                StringBuilder builder = new StringBuilder();  
                for (int i = 0; i < bytes.Length; i++)  
                {  
                    builder.Append(bytes[i].ToString("x2"));  
                }  
                return builder.ToString();  
            }  
        } 
    }

    public class AccountPermissions
    {
        [JsonProperty("hubId")]
        public string HubId { get; set; }
        
        [JsonProperty("region")]
        public string Region { get; set; }
        
        [JsonConverter(typeof(StringEnumConverter))]
        [JsonProperty("role")]
        public AccountRole Role { get; set; }
        
        [JsonProperty("projectIds")]
        public List<string> ProjectIds { get; set; }
    }

    public enum AccountRole
    {
        AccountAdmin,
        ProjectAdmin,
        AccountUser,
        ApplicationAdmin
    }
}