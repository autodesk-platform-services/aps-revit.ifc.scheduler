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
using RevitToIfcScheduler.Context;
using RevitToIfcScheduler.Models;
using Flurl;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Serilog;
using RevitToIfcScheduler.Utilities;

namespace RevitToIfcScheduler.Controllers
{
    public class OauthController : ControllerBase
    {

        public OauthController(Context.RevitIfcContext revitIfcContext)
        {
            RevitIfcContext = revitIfcContext;
        }

        private Context.RevitIfcContext RevitIfcContext { get; set; }


        [HttpGet]
        [Route("api/forge/oauth/callback")]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult> OauthCallback([FromQuery] string code, [FromQuery] string state)
        {
            try
            {
                if (code != null && state != null)
                {
                    var threeLeggedToken = await TokenManager.GetThreeLeggedTokenFromCode(code, HttpContext);

                    var sessionKey = Guid.NewGuid();

                    var user = new User()
                    {
                        HashedSessionKey = RevitToIfcScheduler.Models.User.ComputeSha256Hash(sessionKey.ToString()),
                        AutodeskId = "",
                        Token = threeLeggedToken.AccessToken,
                        Refresh = threeLeggedToken.RefreshToken,
                        TokenExpiration = DateTime.UtcNow.AddSeconds(threeLeggedToken.ExpiresIn.HasValue ? threeLeggedToken.ExpiresIn.Value - 300 : 0)
                    };

                    await user.FetchAutodeskDetails();

                    RevitIfcContext.Users.Add(user);
                    await RevitIfcContext.SaveChangesAsync();
                    HttpContext.Response.Cookies.Append(AppConfig.AppId, sessionKey.ToString());

                    var redirectString = Base64Encoder.Decode(state);
                    return Redirect(redirectString);
                }
                else
                {
                    return Unauthorized();
                }

            }
            catch (Exception ex)
            {
                Log.Debug(ex, this.GetType().FullName);
                return BadRequest(ex);
            }
        }
        [HttpGet]
        [Route("api/forge/oauth/url")]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public ActionResult GetOAuthURL([FromQuery] string encodedRedirectUrl)
        {
            try
            {
                var apsAuthUrl = TokenManager.GetAuthorizationURL(HttpContext, encodedRedirectUrl);

                return Ok(apsAuthUrl.ToString());
            }
            catch (Exception ex)
            {
                var msg = $"SessionAuthController.GetOAuthURL: {ex.Message}";
                Log.Debug(msg);
                return BadRequest(msg);
            }
        }

    }
}