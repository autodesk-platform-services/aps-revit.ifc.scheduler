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
using System.IO;
using System.Reflection;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.DependencyInjection;

namespace RevitToIfcScheduler.Models
{
    public static class AppConfig
    {
        public static string ClientId { get; set; }
        public static string ClientSecret { get; set; }
        public static string LogPath { get; set; } = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        public static List<string> AdminEmails { get; set; }
        public static string FilePath { get; set; }
        public static string AppId { get; set; }
        public static string SendGridApiKey { get; set; }
        public static string FromEmail { get; set; }
        public static string ToEmail { get; set; }
        public static string TwoLegScope { get; set; }
        public static string ThreeLegScope { get; set; }
        public static IDataProtector DataProtector { get; set; }
        public static string SqlDB { get; set; }
        public static IServiceCollection Services { get; set; }
        public static string BucketKey { get; set; }
        public static bool IncludeShallowCopies { get; set; }
        public static string ApsBaseUrl { get; set; }
    }
}