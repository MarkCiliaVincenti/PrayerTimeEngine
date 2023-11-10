﻿using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;
using MethodTimer;
using MetroLog.Maui;
using Microsoft.Extensions.Logging;
using NodaTime;
using NodaTime.Extensions;
using PrayerTimeEngine.Core.Common.Enum;
using PrayerTimeEngine.Core.Data.Preferences;
using PrayerTimeEngine.Core.Domain;
using PrayerTimeEngine.Core.Domain.CalculationService.Interfaces;
using PrayerTimeEngine.Core.Domain.Calculators.Fazilet.Models;
using PrayerTimeEngine.Core.Domain.Calculators.Muwaqqit.Models;
using PrayerTimeEngine.Core.Domain.Calculators.Semerkand.Models;
using PrayerTimeEngine.Core.Domain.Configuration.Interfaces;
using PrayerTimeEngine.Core.Domain.Configuration.Models;
using PrayerTimeEngine.Core.Domain.Model;
using PrayerTimeEngine.Core.Domain.PlacesService.Interfaces;
using PrayerTimeEngine.Core.Domain.PlacesService.Models.Common;
using PrayerTimeEngine.Presentation.Service.Navigation;
using PropertyChanged;
using System.Globalization;
using System.Windows.Input;

namespace PrayerTimeEngine.Presentation.ViewModel
{
    [AddINotifyPropertyChangedInterface]
    public class MainPageViewModel(
            IPrayerTimeCalculationService prayerTimeCalculator,
            ILocationService placeService,
            IProfileService profileService,
            PreferenceService preferenceService,
            INavigationService navigationService,
            ILogger<MainPageViewModel> logger,
            TimeTypeAttributeService timeTypeAttributeService,
            ConcurrentDataLoader concurrentDataLoader
        ) : LogController
    {
        #region properties

        public PrayerTime DisplayPrayerTime
        {
            get
            {
                // only show data when no information is lacking
                if (Prayers == null || Prayers.AllPrayerTimes.Any(x => x.Start == null || x.End == null))
                {
                    return null;
                }

                Instant currentInstant = SystemClock.Instance.GetCurrentInstant();

                return Prayers.AllPrayerTimes.FirstOrDefault(x => x.Start.Value.ToInstant() <= currentInstant && currentInstant <= x.End.Value.ToInstant())
                    ?? Prayers.AllPrayerTimes.OrderBy(x => x.Start.Value.ToInstant()).FirstOrDefault(x => x.Start.Value.ToInstant() > currentInstant);
            }
        }

        public Profile CurrentProfile { get; set; }

        public PrayerTimesBundle Prayers { get; private set; }

        [OnChangedMethod(nameof(onSelectedPlaceChanged))]
        public BasicPlaceInfo SelectedPlace { get; set; }

        public ZonedDateTime? LastUpdated { get; private set; }

        public bool IsLoadingPrayerTimesOrSelectedPlace => IsLoadingPrayerTimes || IsLoadingSelectedPlace;
        public bool IsNotLoadingPrayerTimesOrSelectedPlace => !IsLoadingPrayerTimesOrSelectedPlace;

        public bool IsLoadingSelectedPlace { get; set; }
        public bool IsLoadingPrayerTimes { get; set; }

        public bool ShowFajrGhalas { get; set; }
        public bool ShowFajrRedness { get; set; }
        public bool ShowDuhaQuarter { get; set; }
        public bool ShowMithlayn { get; set; }
        public bool ShowKaraha { get; set; }
        public bool ShowIshtibaq { get; set; }
        public bool ShowMaghribSufficientTime { get; set; }
        public bool ShowOneThird { get; set; }
        public bool ShowTwoThird { get; set; }
        public bool ShowMidnight { get; set; }

        #endregion properties

        #region ICommand

        public ICommand PerformSearch => new Command<string>(async query => await PerformPlaceSearch(query));

        public async Task<List<BasicPlaceInfo>> PerformPlaceSearch(string searchText)
        {
            try
            {
                string languageCode = CultureInfo.CurrentCulture.TwoLetterISOLanguageName;
                return await placeService.SearchPlacesAsync(searchText, languageCode);
            }
            catch (Exception ex)
            {
                logger.LogDebug(ex, "Error during place search");
                doToast(ex.Message);
            }

            return new List<BasicPlaceInfo>();
        }

        public ICommand GoToSettingsPageCommand
            => new Command<EPrayerType>(
                async (prayerTime) =>
                {
                    if (!IsLoadingPrayerTimes)
                    {
                        await navigationService.NavigateTo<SettingsHandlerPageViewModel>(CurrentProfile, prayerTime);
                    }
                });

        public event Action OnAfterLoadingPrayerTimes_EventTrigger = delegate { };

        #endregion ICommand

        #region public methods

        public async void OnActualAppearing()
        {
            try
            {
                if (CurrentProfile == null)
                {
                    return;
                }

                showHideSpecificTimes();
                await loadPrayerTimes();
            }
            catch (Exception exception)
            {
                logger.LogDebug(exception, "Error during OnActualAppearing");
                doToast(exception.Message);
            }
        }

        [Time]
        public async Task OnPageLoaded()
        {
            try
            {
                double time1 = 0;
                double time2 = 0;

                time1 = await performanceLoad(time1).ConfigureAwait(false);

                await Task.Yield();
                await Task.Delay(1000);

                //time2 = await regularLoad(time2);

                doToast($"{time1:N0}ms/{time2:N0}ms to start!");
            }
            catch (Exception exception)
            {
                logger.LogDebug(exception, "Error during page load");
                doToast(exception.Message);
            }
        }

        private async Task<double> regularLoad(double time2)
        {
            CurrentProfile = (await concurrentDataLoader.LoadAllProfilesFromDbTask).First();
            await loadPrayerTimes();
            showHideSpecificTimes();

            time2 = (DateTime.Now - MauiProgram.StartDateTime).TotalMilliseconds;
            return time2;
        }

        private async Task<double> performanceLoad(double time1)
        {
            (Profile profile, PrayerTimesBundle prayerTimes) =
                await concurrentDataLoader.LoadAllProfilesFromJsonTask;

            if (profile != null && prayerTimes != null)
            {
                CurrentProfile = profile;
                Prayers = prayerTimes;
                showHideSpecificTimes();

                time1 = (DateTime.Now - MauiProgram.StartDateTime).TotalMilliseconds;
            }

            return time1;
        }

        #endregion public methods

        #region private methods

        private void createNewProfile()
        {
            // create new profile with SequenceNo = profiles.Select(x => x.SequenceNo).Max();
            // switch to this new profile
        }

        private void deleteCurrentProfile()
        {
            // don't allow if it is the only profile

            // delete current profile and recalculate SequenceNos
            // switch to profil before it or alternatively after it
        }

        private void switchProfile()
        {
            // set CurrentProfile to new value

            // reload prayer times for new profile
        }

        private int isLoadPrayerTimesRunningInterlockedInt = 0;  // 0 for false, 1 for true

        [Time]
        private async Task loadPrayerTimes()
        {
            if (CurrentProfile == null || Interlocked.CompareExchange(ref isLoadPrayerTimesRunningInterlockedInt, 1, 0) == 1)
            {
                return;
            }

            try
            {
                IsLoadingPrayerTimes = true;

                LocalDate today = DateTime.Now.ToLocalDateTime().Date;
                Prayers = await prayerTimeCalculator.ExecuteAsync(CurrentProfile, today);
                preferenceService.SaveCurrentData(CurrentProfile, Prayers);

                OnAfterLoadingPrayerTimes_EventTrigger.Invoke();
            }
            finally
            {
                IsLoadingPrayerTimes = false;
                Interlocked.Exchange(ref isLoadPrayerTimesRunningInterlockedInt, 0);  // Reset the flag to allow future runs
                LastUpdated = SystemClock.Instance.GetCurrentInstant().InZone(DateTimeZoneProviders.Tzdb[TimeZoneInfo.Local.Id]);
            }
        }

        private void showHideSpecificTimes()
        {
            ShowFajrGhalas = isCalculationShown(ETimeType.FajrGhalas);
            ShowFajrRedness = isCalculationShown(ETimeType.FajrKaraha);
            ShowDuhaQuarter = true;// isCalculationShown(ETimeType.DuhaQuarterOfDay);
            ShowMithlayn = isCalculationShown(ETimeType.AsrMithlayn);
            ShowKaraha = isCalculationShown(ETimeType.AsrKaraha);
            ShowMaghribSufficientTime = isCalculationShown(ETimeType.MaghribSufficientTime);
            ShowIshtibaq = isCalculationShown(ETimeType.MaghribIshtibaq);
            ShowOneThird = true;// isCalculationShown(ETimeType.IshaFirstThird);
            ShowTwoThird = true;// isCalculationShown(ETimeType.IshaSecondThird);
            ShowMidnight = true;// isCalculationShown(ETimeType.IshaMidnight);

            OnPropertyChanged();
        }

        private bool isCalculationShown(ETimeType timeData)
        {
            return profileService.GetTimeConfig(CurrentProfile, timeData)?.IsTimeShown == true;
        }

        private void onSelectedPlaceChanged()
        {
            Task.Run(async () =>
            {
                if (CurrentProfile == null || SelectedPlace == null)
                {
                    return;
                }

                try
                {
                    this.IsLoadingSelectedPlace = true;

                    CompletePlaceInfo completePlaceInfo = await placeService.GetTimezoneInfo(SelectedPlace);
                    var locationDataWithCalculationSource = new List<(ECalculationSource, BaseLocationData)>();
                    foreach (var calculationSource in Enum.GetValues<ECalculationSource>())
                    {
                        if (calculationSource == ECalculationSource.None)
                            continue;

                        BaseLocationData locationConfig =
                            await prayerTimeCalculator
                                .GetPrayerTimeCalculatorByCalculationSource(calculationSource)
                                .GetLocationInfo(completePlaceInfo);

                        locationDataWithCalculationSource.Add((calculationSource, locationConfig));
                    }

                    await profileService.UpdateLocationConfig(CurrentProfile, completePlaceInfo.DisplayText, locationDataWithCalculationSource);

                    List<ECalculationSource> missingLocationInfo =
                        Enum.GetValues<ECalculationSource>()
                            .Where(enumValue => !CurrentProfile.LocationConfigs.Select(x => x.CalculationSource).Contains(enumValue))
                            .ToList();

                    if (missingLocationInfo.Count != 0)
                    {
                        doToast($"Location information missing for {string.Join(", ", missingLocationInfo)}");
                    }
                }
                catch (Exception exception)
                {
                    logger.LogDebug(exception, "Error during place selection");
                    doToast(exception.Message);
                }
                finally
                {
                    this.IsLoadingSelectedPlace = false;
                }

                await loadPrayerTimes();
            });
        }

        public string GetLocationDataDisplayText()
        {
            if (this.CurrentProfile == null)
                return string.Empty;

            MuwaqqitLocationData muwaqqitLocationData = profileService.GetLocationConfig(this.CurrentProfile, ECalculationSource.Muwaqqit) as MuwaqqitLocationData;
            FaziletLocationData faziletLocationData = profileService.GetLocationConfig(this.CurrentProfile, ECalculationSource.Fazilet) as FaziletLocationData;
            SemerkandLocationData semerkandLocationData = profileService.GetLocationConfig(this.CurrentProfile, ECalculationSource.Semerkand) as SemerkandLocationData;

            return $"""
                    Muwaqqit:
                        - Coordinates:  
                        ({muwaqqitLocationData?.Latitude} / {muwaqqitLocationData?.Longitude})
                        - Timezone:     
                        '{muwaqqitLocationData?.TimezoneName}'
                    
                    Fazilet:
                        - Country 
                        '{faziletLocationData?.CountryName}'
                        - City 
                        '{faziletLocationData?.CityName}'
                    
                    Semerkand:
                        - Country 
                        '{semerkandLocationData?.CountryName}'
                        - City 
                        '{semerkandLocationData?.CityName}'
                    """;
        }

        public string GetPrayerTimeConfigDisplayText()
        {
            string outputText = string.Empty;

            foreach (KeyValuePair<EPrayerType, List<ETimeType>> item in timeTypeAttributeService.PrayerTypeToTimeTypes)
            {
                EPrayerType prayerType = item.Key;
                outputText += prayerType.ToString();

                foreach (ETimeType timeType in item.Value)
                {
                    if (!timeTypeAttributeService.ConfigurableTypes.Contains(timeType))
                        continue;

                    GenericSettingConfiguration config = profileService.GetTimeConfig(this.CurrentProfile, timeType);

                    outputText += Environment.NewLine;
                    outputText += $"- {timeType} mit {config.Source}";
                    if (config is MuwaqqitDegreeCalculationConfiguration degreeConfig)
                    {
                        outputText += $" ({degreeConfig.Degree}°)";
                    }
                }

                outputText += Environment.NewLine;
                outputText += Environment.NewLine;
            }

            return outputText;
        }

        // TODO REFACTOR
        private static void doToast(string text)
        {
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();

                ToastDuration duration = ToastDuration.Short;
                double fontSize = 14;

                var toast = Toast.Make(text, duration, fontSize);

                await toast.Show(cancellationTokenSource.Token);
            });
        }

        #endregion private methods
    }
}
