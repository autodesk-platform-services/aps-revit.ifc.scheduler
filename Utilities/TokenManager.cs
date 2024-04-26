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
using Flurl;
using Microsoft.AspNetCore.Http;
using Autodesk.SDKManager;
using Autodesk.Authentication;
using Autodesk.Authentication.Model;
using RevitToIfcScheduler.Models;

namespace RevitToIfcScheduler.Utilities
{
    public static class TokenManager
    {
        private static string TwoLeggedToken { get; set; }
        private static DateTime Expiration { get; set; }
        private static readonly SDKManager _sdkManager = SdkManagerBuilder.Create().Build();

        public static async Task<string> GetTwoLeggedToken()
        {
            if (TwoLeggedToken != null && Expiration > DateTime.UtcNow)
            {
                return TwoLeggedToken;
            }
            else
            {
                try
                {
                    var authenticationClient = new AuthenticationClient(_sdkManager);
                    var twoLeggedAuth = await authenticationClient.GetTwoLeggedTokenAsync(
                        AppConfig.ClientId,
                        AppConfig.ClientSecret,
                        ScopeStringToArray(AppConfig.TwoLegScope)
                    );

                    TwoLeggedToken = twoLeggedAuth.AccessToken;
                    Expiration = DateTime.UtcNow.AddSeconds(twoLeggedAuth.ExpiresIn.HasValue ? twoLeggedAuth.ExpiresIn.Value : 0);

                    return TwoLeggedToken;
                }
                catch (AuthenticationApiException ex)
                {
                    throw new Exception("Request failed! (with HTTP response " + ex.HttpResponseMessage.StatusCode + ")");
                }
            }
        }

        public static string GetAuthorizationURL(HttpContext httpContext, string state)
        {
            var redirectUrl = GetRedirectUrl(httpContext);
            var authenticationClient = new AuthenticationClient(_sdkManager);

            var strResponseType = Utils.GetEnumString(ResponseType.Code);
            var scopes = ScopeStringToArray(AppConfig.ThreeLegScope);
            var strScopes = String.Join(" ", scopes.Select(x => Utils.GetEnumString(x)));

            string apsAuthUrl = authenticationClient.tokenApi.Authorize(
                AppConfig.ClientId,
                strResponseType,
                redirectUrl,
                state,
                null,
                strScopes
            );

            return apsAuthUrl;
        }

        public static async Task<ThreeLeggedToken> GetThreeLeggedTokenFromCode(string code, HttpContext httpContext)
        {
            try
            {
                var redirectUrl = GetRedirectUrl(httpContext);
                var authenticationClient = new AuthenticationClient(_sdkManager);
                var threeLeggedToken = await authenticationClient.GetThreeLeggedTokenAsync(
                    AppConfig.ClientId,
                    AppConfig.ClientSecret,
                    code,
                    redirectUrl
                );

                return threeLeggedToken;
            }
            catch (Exception exception)
            {
                throw exception;
            }
        }

        public static async Task RefreshThreeLeggedToken(User user, Context.RevitIfcContext revitIfcContext)
        {
            try
            {
                var authenticationClient = new AuthenticationClient(_sdkManager);
                var threeLeggedToken = await authenticationClient.GetRefreshTokenAsync(
                    AppConfig.ClientId,
                    AppConfig.ClientSecret,
                    user.Refresh,
                    ScopeStringToArray(AppConfig.ThreeLegScope)
                );

                user.Token = threeLeggedToken.AccessToken;
                user.Refresh = threeLeggedToken._RefreshToken;
                user.TokenExpiration = DateTime.UtcNow.AddSeconds(threeLeggedToken.ExpiresIn.HasValue ? threeLeggedToken.ExpiresIn.Value : 0);

                revitIfcContext.Users.Update(user);
                await revitIfcContext.SaveChangesAsync();
            }
            catch (Exception exception)
            {
                throw exception;
            }
        }

        public static string GetRedirectUrl(HttpContext httpContext)
        {
            return ((httpContext.Request.IsHttps ? "https://" : "http://") + httpContext.Request.Host.ToUriComponent())
                .AppendPathSegments("api", "aps", "oauth", "callback");
        }

        public static List<Scopes> ScopeStringToArray(string scopeString)
        {
            var scopeStrings = scopeString.Split(' ');
            var scopes = new List<Scopes>();

            if (scopeStrings.Contains("data:read")) scopes.Add(Scopes.DataRead);
            if (scopeStrings.Contains("data:write")) scopes.Add(Scopes.DataWrite);
            if (scopeStrings.Contains("data:create")) scopes.Add(Scopes.DataCreate);
            if (scopeStrings.Contains("data:search")) scopes.Add(Scopes.DataSearch);

            if (scopeStrings.Contains("account:read")) scopes.Add(Scopes.AccountRead);
            if (scopeStrings.Contains("account:write")) scopes.Add(Scopes.AccountWrite);

            if (scopeStrings.Contains("bucket:read")) scopes.Add(Scopes.BucketRead);
            if (scopeStrings.Contains("bucket:create")) scopes.Add(Scopes.BucketCreate);
            if (scopeStrings.Contains("bucket:update")) scopes.Add(Scopes.BucketUpdate);
            if (scopeStrings.Contains("bucket:delete")) scopes.Add(Scopes.BucketDelete);

            if (scopeStrings.Contains("user:profileRead")) scopes.Add(Scopes.UserProfileRead);

            return scopes;
        }
    }
}