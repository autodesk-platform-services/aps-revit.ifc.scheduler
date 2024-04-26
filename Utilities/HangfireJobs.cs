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
using Hangfire;
using Newtonsoft.Json;
using Serilog;
using RevitToIfcScheduler.Models;

namespace RevitToIfcScheduler.Utilities
{
    public class HangfireJobs
    {
        private readonly Context.RevitIfcContext _revitIfcContext;
        public HangfireJobs(Context.RevitIfcContext revitIfcContext)
        {
            _revitIfcContext = revitIfcContext;
        }

        public async Task PollConversionJob(Guid conversionJobId)
        {
            try
            {

                var conversionJob = await _revitIfcContext.ConversionJobs.FindAsync(conversionJobId);

                var token = await new TwoLeggedTokenGetter().GetToken();
                var manifest = await APS.GetModelDerivativeManifest(conversionJob.EncodedStorageUrn, token, conversionJob.Region);

                if (manifest == null)
                {
                    conversionJob.AddLog("IFC Derivative not available");
                }
                
                switch (manifest.Status)
                {
                    case "pending":
                        conversionJob.AddLog($"Processing Model Derivative: {manifest.Progress}");
                        BackgroundJob.Schedule<HangfireJobs>(x => x.PollConversionJob(conversionJob.Id),TimeSpan.FromMinutes(1));
                        break;
                    case "inprogress":
                        conversionJob.AddLog($"Processing Model Derivative: {manifest.Progress}");
                        BackgroundJob.Schedule<HangfireJobs>(x => x.PollConversionJob(conversionJob.Id),TimeSpan.FromMinutes(1));
                        break;
                    case "processing":
                        conversionJob.AddLog($"Processing Model Derivative: {manifest.Progress}");
                        BackgroundJob.Schedule<HangfireJobs>(x => x.PollConversionJob(conversionJob.Id),TimeSpan.FromMinutes(1));
                        break;
                    case "success":
                        //Get Derivative URN
                        
                        foreach (var derivative in manifest.Derivatives)
                        {
                            if (derivative.OutputType == "ifc")
                            {
                                conversionJob.DerivativeUrn = derivative.Children[0].Urn;
                                conversionJob.AddLog($"Added Derivative URN from Manifest: {conversionJob.DerivativeUrn}");
                            }
                        }

                        if (string.IsNullOrWhiteSpace(conversionJob.DerivativeUrn))
                        {
                            conversionJob.AddLog("IFC derivative Not Generated");
                            conversionJob.Status = ConversionJobStatus.Failed;
                            _revitIfcContext.ConversionJobs.Update(conversionJob);
                            await _revitIfcContext.SaveChangesAsync();
                            return;
                        }

                        _revitIfcContext.ConversionJobs.Update(conversionJob);
                        await _revitIfcContext.SaveChangesAsync();
                        
                        //Create OnReceive
                        await ConversionJob.OnReceive(conversionJob);
                        break;
                    case "failed":
                        conversionJob.Status = ConversionJobStatus.Failed;
                        conversionJob.AddLog("Conversion Failed");
                        break;
                    case "timeout":
                        conversionJob.Status = ConversionJobStatus.TimeOut;
                        conversionJob.AddLog("Conversion Timed Out");
                        break;
                    default: 
                        conversionJob.AddLog(JsonConvert.SerializeObject(manifest));
                        break;
                }

                _revitIfcContext.ConversionJobs.Update(conversionJob);
                await _revitIfcContext.SaveChangesAsync();

            }
            catch (Exception exception)
            {
                Log.Error(exception.Message);
                Log.Error(exception.StackTrace);
                throw;
            }
        }
    }
}