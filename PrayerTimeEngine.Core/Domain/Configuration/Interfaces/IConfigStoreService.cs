﻿using PrayerTimeEngine.Core.Domain.Configuration.Models;

namespace PrayerTimeEngine.Core.Domain.Configuration.Interfaces
{
    public interface IConfigStoreService
    {
        public Task<List<Profile>> GetProfiles();
        public Task SaveProfile(Profile profile);
    }
}
