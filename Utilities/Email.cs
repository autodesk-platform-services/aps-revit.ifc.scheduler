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
using System.Threading.Tasks;
using SendGrid;
using SendGrid.Helpers.Mail;
using RevitToIfcScheduler.Models;

namespace RevitToIfcScheduler.Utilities
{
    public class Email
    {
        public static async Task SendConfirmation(ConversionJob conversionJob)
        {
            if (AppConfig.SendGridApiKey != null && AppConfig.FromEmail != null && AppConfig.ToEmail != null)
            {
                //TODO: Get Webview URL
                
                foreach (var toEmail in AppConfig.ToEmail.Split(','))
                {
                    var sendGridClient = new SendGridClient(AppConfig.SendGridApiKey);
                    var from = new EmailAddress(AppConfig.FromEmail);
                    var to = new EmailAddress(toEmail.Trim());
                    var subject = "Revit to IFC Conversion Completed";
                    var plainContent = $"Revit File Converted to IFC: {conversionJob.FileName}";
                    var htmlContent = $"<h1>Revit File Converted to IFC:</h1> " +
                                      $"<p>{conversionJob.FileName}</p>" +
                                      //$"<a href='https://docs.b360.autodesk.com/projects/{conversionJob.ProjectId.Substring(2)}/folders/{conversionJob.FolderId}/detail'>Open Project Folder in BIM 360</a>";
                                      $"<a href='{conversionJob.FolderUrl}'>Open Project Folder in ACC/BIM360</a>";

                    var mailMessage = MailHelper.CreateSingleEmailToMultipleRecipients(from, new List<EmailAddress>() {to},
                        subject, plainContent, htmlContent);

                    await sendGridClient.SendEmailAsync(mailMessage);
                }
            }
        }
    }
}