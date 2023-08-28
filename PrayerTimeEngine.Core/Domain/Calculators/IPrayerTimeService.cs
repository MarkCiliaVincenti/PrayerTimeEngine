﻿using PrayerTimeEngine.Core.Common.Enum;
using PrayerTimeEngine.Core.Domain.CalculationService.Interfaces;
using PrayerTimeEngine.Core.Domain.Configuration.Models;
using PrayerTimeEngine.Core.Domain.Model;
using PrayerTimeEngine.Core.Domain.PlacesService.Models;

namespace PrayerTimeEngine.Core.Domain.Calculators
{
    public interface IPrayerTimeService
    {
        public Task<ILookup<ICalculationPrayerTimes, ETimeType>> GetPrayerTimesAsync(DateTime date, BaseLocationData locationData, List<GenericSettingConfiguration> configurations);
        public HashSet<ETimeType> GetUnsupportedTimeTypes();
        public Task<BaseLocationData> GetLocationInfo(LocationIQPlace place);
    }
}