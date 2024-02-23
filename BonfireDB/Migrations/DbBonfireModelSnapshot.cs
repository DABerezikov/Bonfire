﻿// <auto-generated />
using System;
using BonfireDB.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

#nullable disable

namespace BonfireDB.Migrations
{
    [DbContext(typeof(DbBonfire))]
    partial class DbBonfireModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder.HasAnnotation("ProductVersion", "8.0.1");

            modelBuilder.Entity("BonfireDB.Entities.Plant", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<int>("PlantCultureId")
                        .HasColumnType("INTEGER");

                    b.Property<int>("PlantSortId")
                        .HasColumnType("INTEGER");

                    b.HasKey("Id");

                    b.HasIndex("PlantCultureId");

                    b.HasIndex("PlantSortId");

                    b.ToTable("Plants");
                });

            modelBuilder.Entity("BonfireDB.Entities.PlantCulture", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<string>("Class")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.ToTable("PlantsCulture");
                });

            modelBuilder.Entity("BonfireDB.Entities.PlantSort", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<int?>("AgeOfSeedlings")
                        .HasColumnType("INTEGER");

                    b.Property<string>("Description")
                        .HasColumnType("TEXT");

                    b.Property<int?>("GrowingSeason")
                        .HasColumnType("INTEGER");

                    b.Property<int?>("LandingPattern")
                        .HasColumnType("INTEGER");

                    b.Property<int?>("MaxGerminationTime")
                        .HasColumnType("INTEGER");

                    b.Property<int?>("MinGerminationTime")
                        .HasColumnType("INTEGER");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<string>("PlantColor")
                        .HasColumnType("TEXT");

                    b.Property<int?>("PlantHeight")
                        .HasColumnType("INTEGER");

                    b.Property<int>("ProducerId")
                        .HasColumnType("INTEGER");

                    b.HasKey("Id");

                    b.HasIndex("ProducerId");

                    b.ToTable("PlantsSort");
                });

            modelBuilder.Entity("BonfireDB.Entities.Producer", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.ToTable("Producers");
                });

            modelBuilder.Entity("BonfireDB.Entities.Replanting", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<double>("PotVolume")
                        .HasColumnType("REAL");

                    b.Property<DateTime>("ReplantingDate")
                        .HasColumnType("TEXT");

                    b.Property<string>("ReplantingNote")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<int>("SeedlingInfoId")
                        .HasColumnType("INTEGER");

                    b.HasKey("Id");

                    b.HasIndex("SeedlingInfoId");

                    b.ToTable("Replants");
                });

            modelBuilder.Entity("BonfireDB.Entities.Seed", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<int>("PlantId")
                        .HasColumnType("INTEGER");

                    b.Property<int>("SeedsInfoId")
                        .HasColumnType("INTEGER");

                    b.HasKey("Id");

                    b.HasIndex("PlantId");

                    b.HasIndex("SeedsInfoId")
                        .IsUnique();

                    b.ToTable("Seeds");
                });

            modelBuilder.Entity("BonfireDB.Entities.Seedling", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<int>("PlantId")
                        .HasColumnType("INTEGER");

                    b.Property<double>("Quantity")
                        .HasColumnType("REAL");

                    b.Property<double>("Weight")
                        .HasColumnType("REAL");

                    b.HasKey("Id");

                    b.HasIndex("PlantId");

                    b.ToTable("Seedlings");
                });

            modelBuilder.Entity("BonfireDB.Entities.SeedlingInfo", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<string>("DeathNote")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<DateTime>("GerminationDate")
                        .HasColumnType("TEXT");

                    b.Property<DateTime>("LandingDate")
                        .HasColumnType("TEXT");

                    b.Property<string>("LunarPhase")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<int>("MotherPlantId")
                        .HasColumnType("INTEGER");

                    b.Property<string>("Note")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<string>("PlantPlace")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<string>("QuarantineCause")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<string>("QuarantineNote")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<DateTime>("QuarantineStartDate")
                        .HasColumnType("TEXT");

                    b.Property<DateTime>("QuarantineStopDate")
                        .HasColumnType("TEXT");

                    b.Property<DateTime>("QuenchingDate")
                        .HasColumnType("TEXT");

                    b.Property<int?>("SeedlingId")
                        .HasColumnType("INTEGER");

                    b.Property<int>("SeedlingNumber")
                        .HasColumnType("INTEGER");

                    b.Property<string>("SeedlingSource")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.HasIndex("SeedlingId");

                    b.ToTable("SeedlingInfos");
                });

            modelBuilder.Entity("BonfireDB.Entities.SeedsInfo", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<double>("AmountSeeds")
                        .HasColumnType("REAL");

                    b.Property<double?>("AmountSeedsWeight")
                        .HasColumnType("REAL");

                    b.Property<decimal>("CostPack")
                        .HasColumnType("TEXT");

                    b.Property<string>("DisposeComment")
                        .HasColumnType("TEXT");

                    b.Property<DateTime>("ExpirationDate")
                        .HasColumnType("TEXT");

                    b.Property<string>("Note")
                        .HasColumnType("TEXT");

                    b.Property<DateTime>("PurchaseDate")
                        .HasColumnType("TEXT");

                    b.Property<double>("QuantityPack")
                        .HasColumnType("REAL");

                    b.Property<string>("SeedSource")
                        .HasColumnType("TEXT");

                    b.Property<double>("WeightPack")
                        .HasColumnType("REAL");

                    b.HasKey("Id");

                    b.ToTable("SeedsInfo");
                });

            modelBuilder.Entity("BonfireDB.Entities.Treatment", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<string>("Product")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<int>("SeedlingInfoId")
                        .HasColumnType("INTEGER");

                    b.Property<DateTime>("TreatmentDate")
                        .HasColumnType("TEXT");

                    b.Property<string>("TreatmentMethod")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.HasIndex("SeedlingInfoId");

                    b.ToTable("Treatments");
                });

            modelBuilder.Entity("BonfireDB.Entities.Plant", b =>
                {
                    b.HasOne("BonfireDB.Entities.PlantCulture", "PlantCulture")
                        .WithMany("Plants")
                        .HasForeignKey("PlantCultureId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("BonfireDB.Entities.PlantSort", "PlantSort")
                        .WithMany("Plants")
                        .HasForeignKey("PlantSortId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("PlantCulture");

                    b.Navigation("PlantSort");
                });

            modelBuilder.Entity("BonfireDB.Entities.PlantSort", b =>
                {
                    b.HasOne("BonfireDB.Entities.Producer", "Producer")
                        .WithMany("PlantSorts")
                        .HasForeignKey("ProducerId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Producer");
                });

            modelBuilder.Entity("BonfireDB.Entities.Replanting", b =>
                {
                    b.HasOne("BonfireDB.Entities.SeedlingInfo", "SeedlingInfo")
                        .WithMany("Replants")
                        .HasForeignKey("SeedlingInfoId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("SeedlingInfo");
                });

            modelBuilder.Entity("BonfireDB.Entities.Seed", b =>
                {
                    b.HasOne("BonfireDB.Entities.Plant", "Plant")
                        .WithMany()
                        .HasForeignKey("PlantId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("BonfireDB.Entities.SeedsInfo", "SeedsInfo")
                        .WithOne("Seed")
                        .HasForeignKey("BonfireDB.Entities.Seed", "SeedsInfoId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Plant");

                    b.Navigation("SeedsInfo");
                });

            modelBuilder.Entity("BonfireDB.Entities.Seedling", b =>
                {
                    b.HasOne("BonfireDB.Entities.Plant", "Plant")
                        .WithMany()
                        .HasForeignKey("PlantId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Plant");
                });

            modelBuilder.Entity("BonfireDB.Entities.SeedlingInfo", b =>
                {
                    b.HasOne("BonfireDB.Entities.Seedling", null)
                        .WithMany("SeedlingInfos")
                        .HasForeignKey("SeedlingId");
                });

            modelBuilder.Entity("BonfireDB.Entities.Treatment", b =>
                {
                    b.HasOne("BonfireDB.Entities.SeedlingInfo", "SeedlingInfo")
                        .WithMany("Treatments")
                        .HasForeignKey("SeedlingInfoId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("SeedlingInfo");
                });

            modelBuilder.Entity("BonfireDB.Entities.PlantCulture", b =>
                {
                    b.Navigation("Plants");
                });

            modelBuilder.Entity("BonfireDB.Entities.PlantSort", b =>
                {
                    b.Navigation("Plants");
                });

            modelBuilder.Entity("BonfireDB.Entities.Producer", b =>
                {
                    b.Navigation("PlantSorts");
                });

            modelBuilder.Entity("BonfireDB.Entities.Seedling", b =>
                {
                    b.Navigation("SeedlingInfos");
                });

            modelBuilder.Entity("BonfireDB.Entities.SeedlingInfo", b =>
                {
                    b.Navigation("Replants");

                    b.Navigation("Treatments");
                });

            modelBuilder.Entity("BonfireDB.Entities.SeedsInfo", b =>
                {
                    b.Navigation("Seed")
                        .IsRequired();
                });
#pragma warning restore 612, 618
        }
    }
}
