﻿using ScanbotSDK.Xamarin.Forms;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;
using Xamarin.Forms.PlatformConfiguration;
using Xamarin.Forms.PlatformConfiguration.iOSSpecific;
using Xamarin.Forms.Xaml;
using ZXing_Test_App.Common;

namespace ZXing_Test_App.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class ScanbotScannerPage : ContentPage
    {
        private bool isDetectionOn;
        private bool IsDetectionOn
        {
            get => isDetectionOn;
            set
            {
                isDetectionOn = value;
                //scanButton.Text = value ? "STOP SCANNING" : "START SCANNING";
                RefreshCamera();
            }
        }


        public ScanbotScannerPage()
        {
            InitializeComponent();
            SetupViews();
        }

        private void SetupViews()
        {
            IsDetectionOn = false;

            cameraView.OnBarcodeScanResult = (result) =>
            {
                string text = "";
                foreach (Barcode barcode in result.Barcodes)
                {
                    text += string.Format("{0} ({1})\n", barcode.Text, barcode.Format.ToString().ToUpper());
                }

                Device.BeginInvokeOnMainThread(() =>
                {
                    //resultsLabel.Text = text;
                });
            };
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();

            RefreshCamera();

            SetupIOSAppearance();

            //scanButton.Clicked += OnScanButtonPressed;
            //infoButton.Clicked += OnInfoButtonPressed;

            cameraView.Resume();

            if (ScanbotSDKConfiguration.LICENSE_KEY.Equals(""))
            {
                ShowTrialLicenseAlert();
            }
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();

            //scanButton.Clicked -= OnScanButtonPressed;
            //infoButton.Clicked -= OnInfoButtonPressed;

            cameraView.Pause();
        }

        private void OnScanButtonPressed(object sender, EventArgs e)
        {
            if (!SBSDK.IsLicenseValid)
            {
                ShowExpiredLicenseAlert();
                return;
            }

            IsDetectionOn = !IsDetectionOn;
        }

        private void RefreshCamera()
        {
            if (IsDetectionOn)
            {
                cameraView.StartDetection();
            }
            else
            {
                cameraView.StopDetection();
            }
        }

        private void OnInfoButtonPressed(object sender, EventArgs e)
        {
            DisplayAlert("Info", SBSDK.IsLicenseValid ? "Your SDK License is valid." : "Your SDK License has expired.", "Close");
        }

        private void ShowExpiredLicenseAlert()
        {
            DisplayAlert("Error", "Your SDK license has expired", "Close");
        }

        private void ShowTrialLicenseAlert()
        {
            DisplayAlert("Welcome", "You are using the Trial SDK License. The SDK will be active for one minute.", "Close");
        }

        private void SetupIOSAppearance()
        {
            if (Device.RuntimePlatform != Device.iOS) { return; }

            var safeInsets = On<iOS>().SafeAreaInsets();
            safeInsets.Bottom = 0;
            Padding = safeInsets;

            //resultsPreviewLayout.BackgroundColor = Color.White;
            //buttonsLayout.IsVisible = false;
        }
    }
}