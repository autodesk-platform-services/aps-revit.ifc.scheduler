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
using System.Threading.Tasks;
using RevitToIfcScheduler.Models;

namespace RevitToIfcScheduler.Utilities
{
    public class ThreeLeggedTokenGetter: TokenGetter
    {
        private User User;
        private Context.RevitIfcContext _revitIfcContext;
        
        public ThreeLeggedTokenGetter(User user, Context.RevitIfcContext revitIfcContext)
        {
            User = user;
            _revitIfcContext = revitIfcContext;
        }
        
        
        public override async Task<string> GetToken()
        {
            if (User.TokenExpiration <= DateTime.UtcNow)
            {
                await TokenManager.RefreshThreeLeggedToken(User, _revitIfcContext);
            }

            return User.Token;
        }
    }
}