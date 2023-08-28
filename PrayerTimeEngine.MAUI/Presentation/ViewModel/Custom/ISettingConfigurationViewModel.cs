﻿using PrayerTimeEngine.Core.Domain.Configuration.Models;

namespace PrayerTimeEngine.Presentation.ViewModel.Custom
{
    public interface ISettingConfigurationViewModel
    {
        public GenericSettingConfiguration BuildSetting(int minuteAdjustment, bool isTimeShown);
        public IView GetUI();
        public void AssignSettingValues(GenericSettingConfiguration configuration);
    }
}
