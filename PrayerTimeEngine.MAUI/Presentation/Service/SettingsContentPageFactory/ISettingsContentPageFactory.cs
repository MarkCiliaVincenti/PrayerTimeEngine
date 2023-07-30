﻿using PrayerTimeEngine.Code.Presentation.View;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PrayerTimeEngine.Presentation.Service.SettingsContentPageFactory
{
    public interface ISettingsContentPageFactory
    {
        SettingsContentPage Create();
    }
}
