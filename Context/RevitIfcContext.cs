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

using RevitToIfcScheduler.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace RevitToIfcScheduler.Context
{
    public class RevitIfcContextFactory : IDesignTimeDbContextFactory<RevitIfcContext>
    {
        public RevitIfcContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<RevitIfcContext>();
            optionsBuilder.UseSqlServer(
                "Server=(localdb)\\MSSQLLocalDB;Database=RevitIFCScheduler;Trusted_Connection=True;",
                b => b.MigrationsAssembly("RevitToIfcScheduler"));

            return new RevitIfcContext(optionsBuilder.Options);
        }
    }

    public class RevitIfcContext: DbContext
    {
        public RevitIfcContext(DbContextOptions<RevitIfcContext> options)
            : base(options)
        {
            // Database.SetCommandTimeout(300);
        }

        // Non-generic overload lets derived classes (e.g. PostgreSQLRevitIfcContext)
        // chain their own DbContextOptions<TDerived> up through this base constructor.
        protected RevitIfcContext(DbContextOptions options)
            : base(options)
        {
        }
        
        public DbSet<User> Users { get; set; }
        public DbSet<IfcSettingsSet> IfcSettingsSets { get; set; }
        public DbSet<Schedule> Schedules { get; set; }
        public DbSet<ConversionJob> ConversionJobs { get; set; }
        public DbSet<Account> Accounts { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Account>(entity =>
            {
                entity.Property(e => e.Id).HasMaxLength(450);
            });

            modelBuilder.Entity<User>(entity =>
            {
                entity.Property(e => e.HashedSessionKey).HasMaxLength(64);
                entity.HasIndex(e => e.HashedSessionKey)
                    .IsUnique()
                    .HasDatabaseName("IX_Users_HashedSessionKey");
            });
        }
    }
}