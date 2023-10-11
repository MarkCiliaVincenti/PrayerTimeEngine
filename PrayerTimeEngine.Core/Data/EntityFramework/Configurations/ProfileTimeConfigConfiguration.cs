﻿using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PrayerTimeEngine.Core.Domain.Configuration.Models;
using System.Text.Json;

namespace PrayerTimeEngine.Core.Data.EntityFramework.Configurations
{
    public class ProfileTimeConfigConfiguration : IEntityTypeConfiguration<ProfileTimeConfig>
    {
        public void Configure(EntityTypeBuilder<ProfileTimeConfig> builder)
        {
            builder
                .HasOne(x => x.Profile)
                .WithMany(x => x.TimeConfigs)
                .HasForeignKey(x => x.ProfileID);

            builder
                .Property(x => x.CalculationConfiguration)
                .HasConversion(
                    x => JsonSerializer.Serialize(x, new JsonSerializerOptions()),
                    x => JsonSerializer.Deserialize<GenericSettingConfiguration>(x, new JsonSerializerOptions())
                );
        }
    }
}
