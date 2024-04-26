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
using System.Linq;
using System.Runtime.Serialization;
using Autodesk.Authentication.Model;

namespace RevitToIfcScheduler.Utilities
{
    public static class Utils
    {

        public static Dictionary<Scopes, string> SetScope(this Dictionary<Scopes, string> scope, Scopes scopeEnum, string value)
        {
            if (scope == null)
                scope = new Dictionary<Scopes, string>();

            var scopeEnumString = Utils.GetEnumString(scopeEnum);
            scope.Add(scopeEnum, value);
            return scope;
        }

        internal static string GetEnumString<T>(T enumVal)
        {
            var enumType = typeof(T);
            var memInfo = enumType.GetMember(enumVal?.ToString());
            var attr = memInfo[0].GetCustomAttributes(false).OfType<EnumMemberAttribute>().FirstOrDefault();
            if (attr != null)
            {
                return attr.Value;
            }
            return null;
        }

    }
}