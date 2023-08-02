﻿using PrayerTimeEngine.Presentation.GraphicsView;
using PrayerTimeEngine.Presentation.ViewModel;

namespace PrayerTimeEngine
{
    public partial class MainPage : ContentPage
    {
        private MainPageViewModel _viewModel;

        public MainPage(MainPageViewModel viewModel)
        {
            InitializeComponent();
            BindingContext = this._viewModel = viewModel;
            viewModel.OnAfterLoadingPrayerTimes_EventTrigger += ViewModel_OnAfterLoadingPrayerTimes_EventTrigger;
        }

        private void ViewModel_OnAfterLoadingPrayerTimes_EventTrigger()
        {
            PrayerTimeGraphicView.DisplayPrayerTime = _viewModel.DisplayPrayerTime;
            PrayerTimeGraphicViewBase.Invalidate();
        }

        /// <summary>
        /// Triggers when the app is opened after being minimized
        /// </summary>
        private void app_Resumed()
        {
            _viewModel.OnActualAppearing();
        }

        /// <summary>
        /// Triggers when this page is navigated to from another page
        /// </summary>
        protected override void OnAppearing()
        {
            if (Application.Current is App app)
            {
                app.Resumed += app_Resumed;
            }

            _viewModel.OnActualAppearing();
        }

        protected override void OnDisappearing()
        {
            if (Application.Current is App app)
            {
                app.Resumed -= app_Resumed;
            }
        }
    }
}
