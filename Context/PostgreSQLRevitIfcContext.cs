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
    // Subclass of RevitIfcContext used for two purposes:
    // 1. Design-time: `dotnet ef migrations add` uses PostgreSQLRevitIfcContextFactory
    //    to generate the PostgreSQL migration set under Migrations/PostgreSQL/.
    // 2. Runtime (PostgreSQL only): registered in DI so that EF Core's migration
    //    discovery finds migrations tagged [DbContext(typeof(PostgreSQLRevitIfcContext))]
    //    rather than the SQL Server ones tagged [DbContext(typeof(RevitIfcContext))].
    public class PostgreSQLRevitIfcContext : RevitIfcContext
    {
        // Constructor for design-time factory (DbContextOptions<RevitIfcContext> base type).
        public PostgreSQLRevitIfcContext(DbContextOptions<RevitIfcContext> options)
            : base(options)
        {
        }

        // Constructor for runtime DI registration via AddDbContext<PostgreSQLRevitIfcContext>.
        public PostgreSQLRevitIfcContext(DbContextOptions<PostgreSQLRevitIfcContext> options)
            : base(options)
        {
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
