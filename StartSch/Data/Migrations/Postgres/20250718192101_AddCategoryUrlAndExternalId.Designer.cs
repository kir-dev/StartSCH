﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using StartSch.Data.Migrations;

#nullable disable

namespace StartSch.Data.Migrations.Postgres
{
    [DbContext(typeof(PostgresDb))]
    [Migration("20250718192101_AddCategoryUrlAndExternalId")]
    partial class AddCategoryUrlAndExternalId
    {
        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "9.0.0")
                .HasAnnotation("Relational:MaxIdentifierLength", 63);

            NpgsqlModelBuilderExtensions.UseIdentityByDefaultColumns(modelBuilder);

            modelBuilder.Entity("Microsoft.AspNetCore.DataProtection.EntityFrameworkCore.DataProtectionKey", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<string>("FriendlyName")
                        .HasColumnType("text");

                    b.Property<string>("Xml")
                        .HasColumnType("text");

                    b.HasKey("Id");

                    b.ToTable("DataProtectionKeys");
                });

            modelBuilder.Entity("StartSch.Data.Category", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<int?>("ExternalIdInt")
                        .HasMaxLength(100)
                        .HasColumnType("integer");

                    b.Property<string>("ExternalUrl")
                        .HasMaxLength(1000)
                        .HasColumnType("character varying(1000)");

                    b.Property<string>("Name")
                        .HasMaxLength(100)
                        .HasColumnType("character varying(100)");

                    b.Property<int>("PageId")
                        .HasColumnType("integer");

                    b.HasKey("Id");

                    b.HasIndex("PageId", "ExternalIdInt")
                        .IsUnique();

                    b.HasIndex("PageId", "Name");

                    b.ToTable("Categories");
                });

            modelBuilder.Entity("StartSch.Data.CategoryInclude", b =>
                {
                    b.Property<int>("IncludedId")
                        .HasColumnType("integer");

                    b.Property<int>("IncluderId")
                        .HasColumnType("integer");

                    b.HasKey("IncludedId", "IncluderId");

                    b.HasIndex("IncluderId");

                    b.ToTable("CategoryIncludes");
                });

            modelBuilder.Entity("StartSch.Data.Event", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<DateTime>("Created")
                        .HasColumnType("timestamp with time zone");

                    b.Property<string>("DescriptionMarkdown")
                        .HasMaxLength(50000)
                        .HasColumnType("character varying(50000)");

                    b.Property<string>("Discriminator")
                        .IsRequired()
                        .HasMaxLength(13)
                        .HasColumnType("character varying(13)");

                    b.Property<DateTime?>("End")
                        .HasColumnType("timestamp with time zone");

                    b.Property<int?>("ParentId")
                        .HasColumnType("integer");

                    b.Property<DateTime?>("Start")
                        .HasColumnType("timestamp with time zone");

                    b.Property<string>("Title")
                        .IsRequired()
                        .HasMaxLength(300)
                        .HasColumnType("character varying(300)");

                    b.Property<DateTime>("Updated")
                        .HasColumnType("timestamp with time zone");

                    b.Property<string>("Url")
                        .HasMaxLength(1000)
                        .HasColumnType("character varying(1000)");

                    b.HasKey("Id");

                    b.HasIndex("ParentId");

                    b.ToTable("Events");

                    b.HasDiscriminator().HasValue("Event");

                    b.UseTphMappingStrategy();
                });

            modelBuilder.Entity("StartSch.Data.EventCategory", b =>
                {
                    b.Property<int>("CategoryId")
                        .HasColumnType("integer");

                    b.Property<int>("EventId")
                        .HasColumnType("integer");

                    b.HasKey("CategoryId", "EventId");

                    b.HasIndex("EventId");

                    b.ToTable("EventCategory");
                });

            modelBuilder.Entity("StartSch.Data.Interest", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<string>("Discriminator")
                        .IsRequired()
                        .HasMaxLength(34)
                        .HasColumnType("character varying(34)");

                    b.HasKey("Id");

                    b.ToTable("Interests");

                    b.HasDiscriminator().HasValue("Interest");

                    b.UseTphMappingStrategy();
                });

            modelBuilder.Entity("StartSch.Data.InterestSubscription", b =>
                {
                    b.Property<int>("InterestId")
                        .HasColumnType("integer");

                    b.Property<int>("UserId")
                        .HasColumnType("integer");

                    b.HasKey("InterestId", "UserId");

                    b.HasIndex("UserId", "InterestId");

                    b.ToTable("InterestSubscriptions");
                });

            modelBuilder.Entity("StartSch.Data.Notification", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<string>("Discriminator")
                        .IsRequired()
                        .HasMaxLength(34)
                        .HasColumnType("character varying(34)");

                    b.HasKey("Id");

                    b.ToTable("Notifications");

                    b.HasDiscriminator().HasValue("Notification");

                    b.UseTphMappingStrategy();
                });

            modelBuilder.Entity("StartSch.Data.NotificationRequest", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<DateTime>("Created")
                        .HasColumnType("timestamp with time zone");

                    b.Property<string>("Discriminator")
                        .IsRequired()
                        .HasMaxLength(21)
                        .HasColumnType("character varying(21)");

                    b.Property<int>("NotificationId")
                        .HasColumnType("integer");

                    b.Property<int>("UserId")
                        .HasColumnType("integer");

                    b.HasKey("Id");

                    b.HasIndex("NotificationId");

                    b.HasIndex("UserId");

                    b.ToTable("NotificationRequests");

                    b.HasDiscriminator().HasValue("NotificationRequest");

                    b.UseTphMappingStrategy();
                });

            modelBuilder.Entity("StartSch.Data.Page", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<string>("Name")
                        .HasMaxLength(100)
                        .HasColumnType("character varying(100)");

                    b.Property<int?>("PekId")
                        .HasColumnType("integer");

                    b.Property<string>("PekName")
                        .HasMaxLength(40)
                        .HasColumnType("character varying(40)");

                    b.Property<int?>("PincerId")
                        .HasColumnType("integer");

                    b.Property<string>("PincerName")
                        .HasMaxLength(40)
                        .HasColumnType("character varying(40)");

                    b.Property<string>("Url")
                        .HasMaxLength(100)
                        .HasColumnType("character varying(100)");

                    b.HasKey("Id");

                    b.HasIndex("PekId")
                        .IsUnique();

                    b.HasIndex("PekName")
                        .IsUnique();

                    b.HasIndex("PincerId")
                        .IsUnique();

                    b.HasIndex("PincerName")
                        .IsUnique();

                    b.HasIndex("Url")
                        .IsUnique();

                    b.ToTable("Pages");
                });

            modelBuilder.Entity("StartSch.Data.Post", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<string>("ContentMarkdown")
                        .HasMaxLength(50000)
                        .HasColumnType("character varying(50000)");

                    b.Property<DateTime>("Created")
                        .HasColumnType("timestamp with time zone");

                    b.Property<int?>("EventId")
                        .HasColumnType("integer");

                    b.Property<string>("ExcerptMarkdown")
                        .HasMaxLength(1000)
                        .HasColumnType("character varying(1000)");

                    b.Property<int?>("ExternalIdInt")
                        .HasColumnType("integer");

                    b.Property<string>("ExternalUrl")
                        .HasMaxLength(1000)
                        .HasColumnType("character varying(1000)");

                    b.Property<DateTime?>("Published")
                        .HasColumnType("timestamp with time zone");

                    b.Property<string>("Title")
                        .IsRequired()
                        .HasMaxLength(300)
                        .HasColumnType("character varying(300)");

                    b.Property<DateTime>("Updated")
                        .HasColumnType("timestamp with time zone");

                    b.HasKey("Id");

                    b.HasIndex("EventId");

                    b.HasIndex("ExternalUrl")
                        .IsUnique();

                    b.ToTable("Posts");
                });

            modelBuilder.Entity("StartSch.Data.PostCategory", b =>
                {
                    b.Property<int>("CategoryId")
                        .HasColumnType("integer");

                    b.Property<int>("PostId")
                        .HasColumnType("integer");

                    b.HasKey("CategoryId", "PostId");

                    b.HasIndex("PostId");

                    b.ToTable("PostCategory");
                });

            modelBuilder.Entity("StartSch.Data.PushSubscription", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<string>("Auth")
                        .IsRequired()
                        .HasMaxLength(50)
                        .HasColumnType("character varying(50)");

                    b.Property<string>("Endpoint")
                        .IsRequired()
                        .HasMaxLength(2000)
                        .HasColumnType("character varying(2000)");

                    b.Property<string>("P256DH")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("character varying(100)");

                    b.Property<int>("UserId")
                        .HasColumnType("integer");

                    b.HasKey("Id");

                    b.HasIndex("Endpoint")
                        .IsUnique();

                    b.HasIndex("UserId");

                    b.ToTable("PushSubscriptions");
                });

            modelBuilder.Entity("StartSch.Data.User", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<string>("AuthSchEmail")
                        .HasMaxLength(200)
                        .HasColumnType("character varying(200)");

                    b.Property<Guid?>("AuthSchId")
                        .HasColumnType("uuid");

                    b.Property<string>("StartSchEmail")
                        .HasMaxLength(200)
                        .HasColumnType("character varying(200)");

                    b.Property<bool>("StartSchEmailVerified")
                        .HasColumnType("boolean");

                    b.HasKey("Id");

                    b.HasIndex("AuthSchId")
                        .IsUnique();

                    b.ToTable("Users");
                });

            modelBuilder.Entity("StartSch.Data.PincerOpening", b =>
                {
                    b.HasBaseType("StartSch.Data.Event");

                    b.Property<DateTime?>("OrderingEnd")
                        .HasColumnType("timestamp with time zone");

                    b.Property<DateTime?>("OrderingStart")
                        .HasColumnType("timestamp with time zone");

                    b.Property<DateTime?>("OutOfStock")
                        .HasColumnType("timestamp with time zone");

                    b.Property<int>("PincerId")
                        .HasColumnType("integer");

                    b.HasIndex("PincerId")
                        .IsUnique();

                    b.HasDiscriminator().HasValue("PincerOpening");
                });

            modelBuilder.Entity("StartSch.Data.CategoryInterest", b =>
                {
                    b.HasBaseType("StartSch.Data.Interest");

                    b.Property<int>("CategoryId")
                        .HasColumnType("integer");

                    b.HasIndex("CategoryId");

                    b.HasDiscriminator().HasValue("CategoryInterest");
                });

            modelBuilder.Entity("StartSch.Data.EventInterest", b =>
                {
                    b.HasBaseType("StartSch.Data.Interest");

                    b.Property<int>("EventId")
                        .HasColumnType("integer");

                    b.HasIndex("EventId");

                    b.HasDiscriminator().HasValue("EventInterest");
                });

            modelBuilder.Entity("StartSch.Data.OrderingStartedNotification", b =>
                {
                    b.HasBaseType("StartSch.Data.Notification");

                    b.Property<int>("OpeningId")
                        .HasColumnType("integer");

                    b.HasIndex("OpeningId");

                    b.HasDiscriminator().HasValue("OrderingStartedNotification");
                });

            modelBuilder.Entity("StartSch.Data.PostNotification", b =>
                {
                    b.HasBaseType("StartSch.Data.Notification");

                    b.Property<int>("PostId")
                        .HasColumnType("integer");

                    b.HasIndex("PostId");

                    b.HasDiscriminator().HasValue("PostNotification");
                });

            modelBuilder.Entity("StartSch.Data.EmailRequest", b =>
                {
                    b.HasBaseType("StartSch.Data.NotificationRequest");

                    b.HasDiscriminator().HasValue("EmailRequest");
                });

            modelBuilder.Entity("StartSch.Data.PushRequest", b =>
                {
                    b.HasBaseType("StartSch.Data.NotificationRequest");

                    b.HasDiscriminator().HasValue("PushRequest");
                });

            modelBuilder.Entity("StartSch.Data.EmailWhenOrderingStartedInCategory", b =>
                {
                    b.HasBaseType("StartSch.Data.CategoryInterest");

                    b.HasDiscriminator().HasValue("EmailWhenOrderingStartedInCategory");
                });

            modelBuilder.Entity("StartSch.Data.EmailWhenPostPublishedInCategory", b =>
                {
                    b.HasBaseType("StartSch.Data.CategoryInterest");

                    b.HasDiscriminator().HasValue("EmailWhenPostPublishedInCategory");
                });

            modelBuilder.Entity("StartSch.Data.PushWhenOrderingStartedInCategory", b =>
                {
                    b.HasBaseType("StartSch.Data.CategoryInterest");

                    b.HasDiscriminator().HasValue("PushWhenOrderingStartedInCategory");
                });

            modelBuilder.Entity("StartSch.Data.PushWhenPostPublishedInCategory", b =>
                {
                    b.HasBaseType("StartSch.Data.CategoryInterest");

                    b.HasDiscriminator().HasValue("PushWhenPostPublishedInCategory");
                });

            modelBuilder.Entity("StartSch.Data.ShowEventsInCategory", b =>
                {
                    b.HasBaseType("StartSch.Data.CategoryInterest");

                    b.HasDiscriminator().HasValue("ShowEventsInCategory");
                });

            modelBuilder.Entity("StartSch.Data.ShowPostsInCategory", b =>
                {
                    b.HasBaseType("StartSch.Data.CategoryInterest");

                    b.HasDiscriminator().HasValue("ShowPostsInCategory");
                });

            modelBuilder.Entity("StartSch.Data.EmailWhenPostPublishedForEvent", b =>
                {
                    b.HasBaseType("StartSch.Data.EventInterest");

                    b.HasDiscriminator().HasValue("EmailWhenPostPublishedForEvent");
                });

            modelBuilder.Entity("StartSch.Data.PushWhenPostPublishedForEvent", b =>
                {
                    b.HasBaseType("StartSch.Data.EventInterest");

                    b.HasDiscriminator().HasValue("PushWhenPostPublishedForEvent");
                });

            modelBuilder.Entity("StartSch.Data.ShowPostsForEvent", b =>
                {
                    b.HasBaseType("StartSch.Data.EventInterest");

                    b.HasDiscriminator().HasValue("ShowPostsForEvent");
                });

            modelBuilder.Entity("StartSch.Data.Category", b =>
                {
                    b.HasOne("StartSch.Data.Page", "Page")
                        .WithMany("Categories")
                        .HasForeignKey("PageId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Page");
                });

            modelBuilder.Entity("StartSch.Data.CategoryInclude", b =>
                {
                    b.HasOne("StartSch.Data.Category", "Included")
                        .WithMany("IncluderCategoryIncludes")
                        .HasForeignKey("IncludedId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("StartSch.Data.Category", "Includer")
                        .WithMany("IncludedCategoryIncludes")
                        .HasForeignKey("IncluderId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Included");

                    b.Navigation("Includer");
                });

            modelBuilder.Entity("StartSch.Data.Event", b =>
                {
                    b.HasOne("StartSch.Data.Event", "Parent")
                        .WithMany("Children")
                        .HasForeignKey("ParentId");

                    b.Navigation("Parent");
                });

            modelBuilder.Entity("StartSch.Data.EventCategory", b =>
                {
                    b.HasOne("StartSch.Data.Category", null)
                        .WithMany()
                        .HasForeignKey("CategoryId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("StartSch.Data.Event", null)
                        .WithMany("EventCategories")
                        .HasForeignKey("EventId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("StartSch.Data.InterestSubscription", b =>
                {
                    b.HasOne("StartSch.Data.Interest", "Interest")
                        .WithMany("Subscriptions")
                        .HasForeignKey("InterestId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("StartSch.Data.User", "User")
                        .WithMany("InterestSubscriptions")
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Interest");

                    b.Navigation("User");
                });

            modelBuilder.Entity("StartSch.Data.NotificationRequest", b =>
                {
                    b.HasOne("StartSch.Data.Notification", "Notification")
                        .WithMany("Requests")
                        .HasForeignKey("NotificationId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("StartSch.Data.User", "User")
                        .WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Notification");

                    b.Navigation("User");
                });

            modelBuilder.Entity("StartSch.Data.Post", b =>
                {
                    b.HasOne("StartSch.Data.Event", "Event")
                        .WithMany("Posts")
                        .HasForeignKey("EventId");

                    b.Navigation("Event");
                });

            modelBuilder.Entity("StartSch.Data.PostCategory", b =>
                {
                    b.HasOne("StartSch.Data.Category", null)
                        .WithMany()
                        .HasForeignKey("CategoryId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("StartSch.Data.Post", null)
                        .WithMany("PostCategories")
                        .HasForeignKey("PostId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("StartSch.Data.PushSubscription", b =>
                {
                    b.HasOne("StartSch.Data.User", "User")
                        .WithMany("PushSubscriptions")
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("User");
                });

            modelBuilder.Entity("StartSch.Data.CategoryInterest", b =>
                {
                    b.HasOne("StartSch.Data.Category", "Category")
                        .WithMany("Interests")
                        .HasForeignKey("CategoryId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Category");
                });

            modelBuilder.Entity("StartSch.Data.EventInterest", b =>
                {
                    b.HasOne("StartSch.Data.Event", "Event")
                        .WithMany()
                        .HasForeignKey("EventId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Event");
                });

            modelBuilder.Entity("StartSch.Data.OrderingStartedNotification", b =>
                {
                    b.HasOne("StartSch.Data.PincerOpening", "Opening")
                        .WithMany()
                        .HasForeignKey("OpeningId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Opening");
                });

            modelBuilder.Entity("StartSch.Data.PostNotification", b =>
                {
                    b.HasOne("StartSch.Data.Post", "Post")
                        .WithMany()
                        .HasForeignKey("PostId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Post");
                });

            modelBuilder.Entity("StartSch.Data.Category", b =>
                {
                    b.Navigation("IncludedCategoryIncludes");

                    b.Navigation("IncluderCategoryIncludes");

                    b.Navigation("Interests");
                });

            modelBuilder.Entity("StartSch.Data.Event", b =>
                {
                    b.Navigation("Children");

                    b.Navigation("EventCategories");

                    b.Navigation("Posts");
                });

            modelBuilder.Entity("StartSch.Data.Interest", b =>
                {
                    b.Navigation("Subscriptions");
                });

            modelBuilder.Entity("StartSch.Data.Notification", b =>
                {
                    b.Navigation("Requests");
                });

            modelBuilder.Entity("StartSch.Data.Page", b =>
                {
                    b.Navigation("Categories");
                });

            modelBuilder.Entity("StartSch.Data.Post", b =>
                {
                    b.Navigation("PostCategories");
                });

            modelBuilder.Entity("StartSch.Data.User", b =>
                {
                    b.Navigation("InterestSubscriptions");

                    b.Navigation("PushSubscriptions");
                });
#pragma warning restore 612, 618
        }
    }
}
