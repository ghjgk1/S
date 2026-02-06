using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using RailwayInformationDesk.Models;

namespace RailwayInformationDesk.Data;

public partial class RailwayInformationDeskContext : DbContext
{
    public RailwayInformationDeskContext()
    {
    }

    public RailwayInformationDeskContext(DbContextOptions<RailwayInformationDeskContext> options)
        : base(options)
    {
    }

    public virtual DbSet<ActualSchedule> ActualSchedules { get; set; }

    public virtual DbSet<Route> Routes { get; set; }

    public virtual DbSet<ScheduleTemplate> ScheduleTemplates { get; set; }

    public virtual DbSet<ScheduleTemplateStop> ScheduleTemplateStops { get; set; }

    public virtual DbSet<Station> Stations { get; set; }

    public virtual DbSet<TrainType> TrainTypes { get; set; }

    public virtual DbSet<TripInstance> TripInstances { get; set; }

    public virtual DbSet<User> Users { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseSqlServer("Server=localhost;Database=RailwayInformationDesk;Trusted_Connection=True;TrustServerCertificate=True;");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ActualSchedule>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__ActualSc__3214EC0714EB8683");

            entity.ToTable("ActualSchedule");

            entity.Property(e => e.ActualPlatform).HasMaxLength(20);

            entity.HasOne(d => d.Stop).WithMany(p => p.ActualSchedules)
                .HasForeignKey(d => d.StopId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ActualSchedule_Stop");

            entity.HasOne(d => d.TripInstance).WithMany(p => p.ActualSchedules)
                .HasForeignKey(d => d.TripInstanceId)
                .HasConstraintName("FK_ActualSchedule_TripInstance");
        });

        modelBuilder.Entity<Route>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Routes__3214EC07C4BC7C34");

            entity.Property(e => e.Name).HasMaxLength(200);

            entity.HasOne(d => d.ArrivalStation).WithMany(p => p.RouteArrivalStations)
                .HasForeignKey(d => d.ArrivalStationId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Routes_ArrivalStation");

            entity.HasOne(d => d.DepartureStation).WithMany(p => p.RouteDepartureStations)
                .HasForeignKey(d => d.DepartureStationId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Routes_DepartureStation");
        });

        modelBuilder.Entity<ScheduleTemplate>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Schedule__3214EC074D80B873");

            entity.Property(e => e.DaysOfWeek).HasMaxLength(7);
            entity.Property(e => e.IsActive).HasDefaultValue(true);

            entity.HasOne(d => d.Route).WithMany(p => p.ScheduleTemplates)
                .HasForeignKey(d => d.RouteId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ScheduleTemplates_Route");

            entity.HasOne(d => d.TrainType).WithMany(p => p.ScheduleTemplates)
                .HasForeignKey(d => d.TrainTypeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ScheduleTemplates_TrainType");
        });

        modelBuilder.Entity<ScheduleTemplateStop>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Schedule__3214EC078330C5C0");

            entity.HasIndex(e => new { e.TemplateId, e.StopOrder }, "UQ_TemplateStops_Template_StopOrder").IsUnique();

            entity.Property(e => e.Platform).HasMaxLength(20);

            entity.HasOne(d => d.Station).WithMany(p => p.ScheduleTemplateStops)
                .HasForeignKey(d => d.StationId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_TemplateStops_Station");

            entity.HasOne(d => d.Template).WithMany(p => p.ScheduleTemplateStops)
                .HasForeignKey(d => d.TemplateId)
                .HasConstraintName("FK_TemplateStops_Template");
        });

        modelBuilder.Entity<Station>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Stations__3214EC0798D4E516");

            entity.HasIndex(e => e.Name, "UQ__Stations__737584F684790B9C").IsUnique();

            entity.Property(e => e.City).HasMaxLength(100);
            entity.Property(e => e.Name).HasMaxLength(100);
            entity.Property(e => e.Region).HasMaxLength(100);
        });

        modelBuilder.Entity<TrainType>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__TrainTyp__3214EC07DB7CEB2D");

            entity.HasIndex(e => e.Name, "UQ__TrainTyp__737584F622384B0B").IsUnique();

            entity.Property(e => e.Name).HasMaxLength(100);
        });

        modelBuilder.Entity<TripInstance>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__TripInst__3214EC0769E5F99D");

            entity.HasIndex(e => new { e.TemplateId, e.TripDate }, "UQ_TripInstances_Template_Date").IsUnique();

            entity.HasOne(d => d.Template).WithMany(p => p.TripInstances)
                .HasForeignKey(d => d.TemplateId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_TripInstances_Template");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Users__3214EC075A950D5E");

            entity.HasIndex(e => e.Login, "UQ__Users__5E55825BA090AE71").IsUnique();

            entity.Property(e => e.FullName)
                .HasMaxLength(150)
                .HasDefaultValue("");
            entity.Property(e => e.Login).HasMaxLength(50);
            entity.Property(e => e.PasswordHash).HasMaxLength(100);
            entity.Property(e => e.Role).HasMaxLength(30);

            entity.HasOne(d => d.Station).WithMany(p => p.Users)
                .HasForeignKey(d => d.StationId)
                .HasConstraintName("FK_Users_Station");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
