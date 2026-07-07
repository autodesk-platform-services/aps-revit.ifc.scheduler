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

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace RevitToIfcScheduler.Context
{
    // Design-time-only subclass used by `dotnet ef migrations add` to generate
    // the PostgreSQL migration set under Migrations/PostgreSQL/. This class is
    // NOT registered in DI; the runtime always uses RevitIfcContext with the
    // provider chosen by DatabaseProviderConfiguration in Startup.cs.
    public class PostgreSQLRevitIfcContext : RevitIfcContext
    {
        public PostgreSQLRevitIfcContext(DbContextOptions<RevitIfcContext> options)
            : base(options)
        {
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseNpgsql(
                    "Host=localhost;Database=RevitIFCScheduler;Username=postgres;Password=postgres",
                    b => b.MigrationsAssembly("RevitToIfcScheduler"));
            }
        }
    }

    public class PostgreSQLRevitIfcContextFactory : IDesignTimeDbContextFactory<PostgreSQLRevitIfcContext>
    {
        public PostgreSQLRevitIfcContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<RevitIfcContext>();
            optionsBuilder.UseNpgsql(
                "Host=localhost;Database=RevitIFCScheduler;Username=postgres;Password=postgres",
                b => b.MigrationsAssembly("RevitToIfcScheduler"));

            return new PostgreSQLRevitIfcContext(optionsBuilder.Options);
        }
    }
}
