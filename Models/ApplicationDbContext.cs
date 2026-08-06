using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity;

namespace Tounaent_Fixtures.Models;

public partial class ApplicationDbContext : DbContext
{
    // EF Core does not support .NET Framework past EF Core 3.1 (EOL Dec 2022), so this is
    // ported to EF6 (System.Data.Entity), which Microsoft still actively supports on
    // .NET Framework. Constructor now passes the connection string name/string to the EF6
    // base constructor instead of taking DbContextOptions<T>.
    public ApplicationDbContext()
        : base("name=DefaultConnection")
    {
    }

    public ApplicationDbContext(string connectionString)
        : base(connectionString)
    {
    }

    public virtual DbSet<Gender> Gender { get; set; } = null!;

    public virtual DbSet<Registration> Registrations { get; set; } = null!;

    public virtual DbSet<TblTournament> TblTournament { get; set; } = null!;

    public virtual DbSet<TblTournamentUserReg> TblTournamentUserRegs { get; set; } = null!;

    public virtual DbSet<TblCategory> TblCategory { get; set; } = null!;

    public virtual DbSet<TblWeightCategory> TblWeightCategory { get; set; } = null!;

    public virtual DbSet<TblDistrict> TblDistricts { get; set; } = null!;

    public virtual DbSet<TblDistLocalClub> TblDistLocalClubs { get; set; } = null!;

    protected override void OnModelCreating(DbModelBuilder modelBuilder)
    {
        // TblDistLocalClub was HasNoKey() in EF Core, but ClubId is ValueGeneratedOnAdd()
        // (i.e. a real identity column) - EF6 doesn't support keyless entities the same way
        // EF Core does, so this now declares ClubId as the actual primary key. This is more
        // correct than the original EF Core scaffold, not just a workaround.
        modelBuilder.Entity<TblDistLocalClub>().HasKey(e => e.ClubId);
        modelBuilder.Entity<TblDistLocalClub>().ToTable("Tbl_Dist_LocalClub");
        modelBuilder.Entity<TblDistLocalClub>().Property(e => e.AddedBy)
            .HasMaxLength(50).HasColumnName("Added_by");
        modelBuilder.Entity<TblDistLocalClub>().Property(e => e.AddedDt)
            .HasColumnName("Added_dt");
        modelBuilder.Entity<TblDistLocalClub>().Property(e => e.ClubId)
            .HasColumnName("Club_id").HasDatabaseGeneratedOption(DatabaseGeneratedOption.Identity);
        modelBuilder.Entity<TblDistLocalClub>().Property(e => e.DistictId).HasColumnName("Distict_id");
        modelBuilder.Entity<TblDistLocalClub>().Property(e => e.LocalClubName)
            .HasMaxLength(100).HasColumnName("Local_Club_Name");
        modelBuilder.Entity<TblDistLocalClub>().Property(e => e.ModifyBy)
            .HasMaxLength(50).HasColumnName("Modify_by");
        modelBuilder.Entity<TblDistLocalClub>().Property(e => e.ModifyDt)
            .HasColumnName("Modify_dt");
        modelBuilder.Entity<TblDistLocalClub>().Property(e => e.StateId).HasColumnName("State_id");

        modelBuilder.Entity<TblWeightCategory>().HasKey(e => e.WeightCatId);
        modelBuilder.Entity<TblWeightCategory>().Property(e => e.WeightCatId).HasColumnName("Weight_Cat_id");
        modelBuilder.Entity<TblWeightCategory>().Property(e => e.WeightCatName).HasColumnName("Weight_Cat_Name");
        modelBuilder.Entity<TblWeightCategory>().Property(e => e.CatId).HasColumnName("Cat_id");
        modelBuilder.Entity<TblWeightCategory>().Property(e => e.ModifyBy).HasColumnName("Modify_by");
        modelBuilder.Entity<TblWeightCategory>().Property(e => e.AddedBy).HasColumnName("Added_by");
        modelBuilder.Entity<TblWeightCategory>().Property(e => e.ModifyDt).HasColumnName("Modify_dt");
        modelBuilder.Entity<TblWeightCategory>().Property(e => e.AddedDt).HasColumnName("Added_dt");

        modelBuilder.Entity<TblCategory>().HasKey(e => e.CatId);
        modelBuilder.Entity<TblCategory>().ToTable("Tbl_Category");
        modelBuilder.Entity<TblCategory>().Property(e => e.CatId).HasColumnName("Cat_id");
        modelBuilder.Entity<TblCategory>().Property(e => e.CategoryName).HasColumnName("Category_Name");
        modelBuilder.Entity<TblCategory>().Property(e => e.GenId).HasColumnName("Gen_id");
        modelBuilder.Entity<TblCategory>().Property(e => e.IsActive).HasColumnName("IsActive");
        modelBuilder.Entity<TblCategory>().Property(e => e.AddedDt).HasColumnName("Added_dt");
        modelBuilder.Entity<TblCategory>().Property(e => e.AddedBy).HasColumnName("Added_by");
        modelBuilder.Entity<TblCategory>().Property(e => e.ModifyDt).HasColumnName("Modify_dt");
        modelBuilder.Entity<TblCategory>().Property(e => e.ModifyBy).HasColumnName("Modify_by");

        modelBuilder.Entity<TblTournamentUserReg>().HasKey(e => e.TrUserId);
        modelBuilder.Entity<TblTournamentUserReg>().ToTable("Tbl_Tournament_User_Reg");
        modelBuilder.Entity<TblTournamentUserReg>().Property(e => e.TrUserId).HasColumnName("Tr_User_id");
        modelBuilder.Entity<TblTournamentUserReg>().Property(e => e.AddedBy)
            .HasMaxLength(50).HasColumnName("Added_by");
        modelBuilder.Entity<TblTournamentUserReg>().Property(e => e.AddedDt).HasColumnName("Added_dt");
        modelBuilder.Entity<TblTournamentUserReg>().Property(e => e.AdharNumb).HasMaxLength(500);
        modelBuilder.Entity<TblTournamentUserReg>().Property(e => e.CatId).HasColumnName("Cat_id");
        modelBuilder.Entity<TblTournamentUserReg>().Property(e => e.CategoryName)
            .HasMaxLength(50).HasColumnName("Category_Name");
        modelBuilder.Entity<TblTournamentUserReg>().Property(e => e.ClubName).HasMaxLength(500);
        modelBuilder.Entity<TblTournamentUserReg>().Property(e => e.District).HasMaxLength(50);
        modelBuilder.Entity<TblTournamentUserReg>().Property(e => e.DistrictId).HasColumnName("District_id");
        modelBuilder.Entity<TblTournamentUserReg>().Property(e => e.Dob).HasColumnName("DOB");
        modelBuilder.Entity<TblTournamentUserReg>().Property(e => e.Email).HasMaxLength(150);
        modelBuilder.Entity<TblTournamentUserReg>().Property(e => e.FatherName)
            .HasMaxLength(150).HasColumnName("Father_Name");
        modelBuilder.Entity<TblTournamentUserReg>().Property(e => e.Gender).HasMaxLength(10);
        modelBuilder.Entity<TblTournamentUserReg>().Property(e => e.MobileNo).HasMaxLength(150);
        modelBuilder.Entity<TblTournamentUserReg>().Property(e => e.ModifyBy)
            .HasMaxLength(50).HasColumnName("Modify_by");
        modelBuilder.Entity<TblTournamentUserReg>().Property(e => e.ModifyDt).HasColumnName("Modify_dt");
        modelBuilder.Entity<TblTournamentUserReg>().Property(e => e.Name).HasMaxLength(150);
        modelBuilder.Entity<TblTournamentUserReg>().Property(e => e.TrId).HasColumnName("Tr_id");
        modelBuilder.Entity<TblTournamentUserReg>().Property(e => e.UserId).HasColumnName("User_id");
        modelBuilder.Entity<TblTournamentUserReg>().Property(e => e.WeighCatName)
            .HasMaxLength(50).HasColumnName("Weigh_Cat_Name");
        modelBuilder.Entity<TblTournamentUserReg>().Property(e => e.Weight).HasColumnName("weight");
        modelBuilder.Entity<TblTournamentUserReg>().Property(e => e.WeightCatId).HasColumnName("Weight_Cat_id");

        modelBuilder.Entity<TblTournament>().HasKey(e => e.TournamentId);
        modelBuilder.Entity<TblTournament>().ToTable("Tbl_Tournament");
        modelBuilder.Entity<TblTournament>().Property(e => e.TournamentId).HasColumnName("Tournament_Id");
        modelBuilder.Entity<TblTournament>().Property(e => e.AddedBy)
            .HasMaxLength(255).HasColumnName("Added_by");
        modelBuilder.Entity<TblTournament>().Property(e => e.AddedDt).HasColumnName("Added_dt");
        modelBuilder.Entity<TblTournament>().Property(e => e.DistictId).HasColumnName("Distict_id");
        modelBuilder.Entity<TblTournament>().Property(e => e.DistictName)
            .HasMaxLength(100).IsUnicode(false).HasColumnName("Distict_Name");
        modelBuilder.Entity<TblTournament>().Property(e => e.FromDt).HasColumnName("From_dt");
        modelBuilder.Entity<TblTournament>().Property(e => e.ModifyBy)
            .HasMaxLength(255).HasColumnName("Modify_by");
        modelBuilder.Entity<TblTournament>().Property(e => e.ModifyDt).HasColumnName("Modify_dt");
        modelBuilder.Entity<TblTournament>().Property(e => e.OrganizedBy).HasMaxLength(255);
        modelBuilder.Entity<TblTournament>().Property(e => e.ToDt).HasColumnName("To_dt");
        modelBuilder.Entity<TblTournament>().Property(e => e.TournamentName).HasMaxLength(255);
        modelBuilder.Entity<TblTournament>().Property(e => e.Venue).HasMaxLength(255);
        modelBuilder.Entity<TblTournament>().Property(e => e.MatchType).HasMaxLength(1);

        modelBuilder.Entity<Gender>().HasKey(e => e.GenderId);
        modelBuilder.Entity<Gender>().ToTable("Gender");
        modelBuilder.Entity<Gender>().Property(e => e.AddedBy)
            .HasMaxLength(50).HasColumnName("Added_by");
        modelBuilder.Entity<Gender>().Property(e => e.AddedDt).HasColumnName("Added_dt");
        modelBuilder.Entity<Gender>().Property(e => e.GenderName).HasMaxLength(50);
        modelBuilder.Entity<Gender>().Property(e => e.ModifyBy)
            .HasMaxLength(50).HasColumnName("Modify_by");
        modelBuilder.Entity<Gender>().Property(e => e.ModifyDt).HasColumnName("Modify_dt");

        modelBuilder.Entity<Registration>().HasKey(e => e.RegistrationId);
        modelBuilder.Entity<Registration>().ToTable("Registration");
        modelBuilder.Entity<Registration>().Property(e => e.Aadhaar).HasMaxLength(20);
        modelBuilder.Entity<Registration>().Property(e => e.Address).HasMaxLength(250);
        modelBuilder.Entity<Registration>().Property(e => e.CreatedDate);
        modelBuilder.Entity<Registration>().Property(e => e.Dob).HasColumnName("DOB");
        modelBuilder.Entity<Registration>().Property(e => e.Email).HasMaxLength(100);
        modelBuilder.Entity<Registration>().Property(e => e.Height).HasMaxLength(20);
        modelBuilder.Entity<Registration>().Property(e => e.Name).HasMaxLength(100);
        modelBuilder.Entity<Registration>().Property(e => e.Phone).HasMaxLength(15);
        modelBuilder.Entity<Registration>().Property(e => e.PinCode).HasMaxLength(10);
        modelBuilder.Entity<Registration>().Property(e => e.Weight).HasMaxLength(20);
        modelBuilder.Entity<Registration>().HasRequired(d => d.Gender)
            .WithMany(p => p.Registrations)
            .HasForeignKey(d => d.GenderId)
            .WillCascadeOnDelete(false);

        // Same treatment as TblDistLocalClub above: DistictId is ValueGeneratedOnAdd(), so it's
        // a real identity/PK column, not a genuinely keyless entity.
        modelBuilder.Entity<TblDistrict>().HasKey(e => e.DistictId);
        modelBuilder.Entity<TblDistrict>().ToTable("Tbl_District");
        modelBuilder.Entity<TblDistrict>().Property(e => e.AddedBy)
            .HasMaxLength(50).HasColumnName("Added_by");
        modelBuilder.Entity<TblDistrict>().Property(e => e.AddedDt).HasColumnName("Added_dt");
        modelBuilder.Entity<TblDistrict>().Property(e => e.DistictId)
            .HasColumnName("Distict_id").HasDatabaseGeneratedOption(DatabaseGeneratedOption.Identity);
        modelBuilder.Entity<TblDistrict>().Property(e => e.DistictName)
            .HasMaxLength(50).HasColumnName("Distict_Name");
        modelBuilder.Entity<TblDistrict>().Property(e => e.ModifyBy)
            .HasMaxLength(50).HasColumnName("Modify_by");
        modelBuilder.Entity<TblDistrict>().Property(e => e.ModifyDt).HasColumnName("Modify_dt");
        modelBuilder.Entity<TblDistrict>().Property(e => e.StateId).HasColumnName("State_id");

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(DbModelBuilder modelBuilder);
}
