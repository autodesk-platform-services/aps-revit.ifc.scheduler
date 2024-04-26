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
using Microsoft.AspNetCore.Http;
using RevitToIfcScheduler.Models;

namespace RevitToIfcScheduler.Utilities
{
    public static class Authentication
    {
        public static bool IsAuthorized(HttpContext httpContext, Context.RevitIfcContext revitIfcContext)
        {
            try
            {
                var user = RevitToIfcScheduler.Models.User.FetchByContext(httpContext, revitIfcContext);
                return user != null;
            }
            catch 
            {
                return false;
            }
        }
        public static bool IsAuthorized(HttpContext httpContext, Context.RevitIfcContext revitIfcContext, List<AccountRole> accountRoles, string projectId)
        {
            try
            {
                var user = RevitToIfcScheduler.Models.User.FetchByContext(httpContext, revitIfcContext);
                foreach (var accountRole in accountRoles)
                {
                    if (accountRole == AccountRole.ApplicationAdmin && user.HasPermission(accountRole))
                    {
                        return true;
                    }

                    if (user.HasPermission(accountRole, projectId))
                    {
                        return true;
                    }
                }
                
                return false;
            }
            catch 
            {
                return false;
            }
        }
        public static bool IsAuthorized(HttpContext httpContext, Context.RevitIfcContext revitIfcContext, List<AccountRole> accountRoles)
        {
            try
            {
                var user = RevitToIfcScheduler.Models.User.FetchByContext(httpContext, revitIfcContext);
                foreach (var accountRole in accountRoles)
                {
                    if (user.HasPermission(accountRole))
                    {
                        return true;
                    }
                }
                
                return false;
            }
            catch 
            {
                return false;
            }
        }

        public static bool IsAuthorized(HttpContext httpsContext, string projectId)
        {
            return true;
        }
    }
}