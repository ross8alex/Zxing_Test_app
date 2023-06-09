﻿using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using IO.Scanbot.Sdk.Barcode.Entity;
using IO.Scanbot.Sdk.Barcode;
using IO.Scanbot.Sdk.Camera;
using IO.Scanbot.Sdk.Contourdetector;
using IO.Scanbot.Sdk.UI.Camera;
using IO.Scanbot.Sdk.UI.View.Barcode;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xamarin.Forms.Platform.Android;
using Xamarin.Forms;
using ZXing_Test_App.Droid;
using AndroidX.Core.Content;
using AndroidX.Core.App;
using Android;
using Android.Content.PM;
using ScanbotSDK.Xamarin.Forms.Android;
using ZXing_Test_App.Views;

[assembly: ExportRenderer(typeof(BarcodeCameraView), typeof(AndroidBarcodeCameraRenderer))]
namespace ZXing_Test_App.Droid
{
    /*
      By extending 'ViewRenderer' we specify that we want our custom renderer to target 'BarcodeCameraView' and
      override it with our native view, which is a 'FrameLayout' in this case (see layout/barcode_camera_view.xml)
   */
    class AndroidBarcodeCameraRenderer : ViewRenderer<BarcodeCameraView, FrameLayout>, ICameraOpenCallback
    {
        protected BarcodeCameraView.BarcodeScannerResultHandler HandleScanResult;
        protected DocumentAutoSnappingController autoSnappingController;
        protected BarcodeDetectorFrameHandler barcodeDetectorFrameHandler;
        protected FrameLayout cameraLayout;
        protected ScanbotCameraView cameraView;
        protected FinderOverlayView finderOverlayView;

        private readonly int REQUEST_PERMISSION_CODE = 200;

        public AndroidBarcodeCameraRenderer(Context context) : base(context)
        {
            SetupViews(context);
        }

        private void SetupViews(Context context)
        {

            // We instantiate our views from the layout XML
            cameraLayout = (FrameLayout)LayoutInflater
                .FromContext(context)
                .Inflate(Resource.Layout.barcode_camera_view, null, false);

            // Here we retrieve the Camera View...
            cameraView = cameraLayout.FindViewById<ScanbotCameraView>(Resource.Id.barcode_camera);

            // ...and here we retrieve and configure the Finder Overlay View
            finderOverlayView = cameraLayout.FindViewById<FinderOverlayView>(Resource.Id.barcode_finder_overlay);
            finderOverlayView.SetFinderMinPadding(80);
            finderOverlayView.RequiredAspectRatios = new List<FinderAspectRatio>
            {
                new FinderAspectRatio(3, 1)
            };
        }

        private void StartDetection()
        {
            cameraView.OnResume();
            barcodeDetectorFrameHandler.Enabled = true;
            finderOverlayView.Visibility = ViewStates.Visible;
            CheckPermissions();
        }

        private void StopDetection()
        {
            barcodeDetectorFrameHandler.Enabled = false;
            finderOverlayView.Visibility = ViewStates.Invisible;
        }

        /*
            This is probably the most important method that belongs to a ViewRenderer.
            You must override this in order to actually implement the renderer.
            OnElementChanged is called whenever the View or one of its properties have changed;
            this includes the initialization as well, therefore we initialize our native control here.
         */
        protected override void OnElementChanged(ElementChangedEventArgs<BarcodeCameraView> e)
        {

            // The SetNativeControl method should be used to instantiate the native control,
            // and this method will also assign the control reference to the Control property
            SetNativeControl(cameraLayout);

            base.OnElementChanged(e);

            if (Control != null)
            {
                // The Element object is the instance of BarcodeCameraView as defined in the Forms
                // core project. We've defined some delegates there, and we'll bind to them here so that
                // these native calls will be executed whenever those methods will be called.
                Element.OnResumeHandler = (sender, e) =>
                {
                    cameraView.OnResume();
                };

                Element.OnPauseHandler = (sender, e) =>
                {
                    cameraView.OnPause();
                };

                Element.StartDetectionHandler = (sender, e) =>
                {
                    StartDetection();
                };

                Element.StopDetectionHandler = (sender, e) =>
                {
                    StopDetection();
                };

                // Similarly, we have defined a delegate in our BarcodeCameraView implementation,
                // so that we can trigger it whenever the Scanner will return a valid result.
                HandleScanResult = Element.OnBarcodeScanResult;

                // In this example we demonstrate how to lock the orientation of the UI (Activity)
                // as well as the orientation of the taken picture to portrait.
                cameraView.LockToPortrait(true);

                // Here we create the BarcodeDetectorFrameHandler which will take care of detecting
                // barcodes in your video frames
                var detector = new IO.Scanbot.Sdk.ScanbotSDK(Context.GetActivity()).CreateBarcodeDetector();
                barcodeDetectorFrameHandler = BarcodeDetectorFrameHandler.Attach(cameraView, detector);

                detector.ModifyConfig(new Function1Impl<BarcodeScannerConfigBuilder>((response) => {
                    response.SetSaveCameraPreviewFrame(false);
                }));

                if (barcodeDetectorFrameHandler is BarcodeDetectorFrameHandler handler)
                {
                    handler.SetDetectionInterval(0);
                    handler.AddResultHandler(new BarcodeDetectorResultHandler((result) => HandleFrameHandlerResult(result)));

                    // Uncomment to enable auto-snapping (eg. single barcode scan)
                    // var barcodeAutoSnappingController = BarcodeAutoSnappingController.Attach(cameraView, handler);
                    // barcodeAutoSnappingController.SetSensitivity(1f);
                }

                cameraView.SetCameraOpenCallback(this);
            }
        }

        void ICameraOpenCallback.OnCameraOpened()
        {
            cameraView.PostDelayed(() =>
            {
                // Disable auto-focus sound:
                cameraView.SetAutoFocusSound(false);

                // Uncomment to disable shutter sound (supported since Android 4.2+):
                // Please note that some devices may not allow disabling the camera shutter sound. 
                // If the shutter sound state cannot be set to the desired value, this method will be ignored.
                // cameraView.SetShutterSound(false);

                // Enable ContinuousFocus mode:
                cameraView.ContinuousFocus();
            }, 500);
        }

        private bool HandleSuccess(BarcodeScanningResult result)
        {
            if (result == null) { return false; }

            ScanbotSDK.Xamarin.Forms.BarcodeScanningResult outResult = new ScanbotSDK.Xamarin.Forms.BarcodeScanningResult
            {
                Barcodes = result.BarcodeItems.ToFormsBarcodeList(),
                Image = result.PreviewFrame.ToImageSource()
            };

            HandleScanResult?.Invoke(outResult);
            return true;
        }

        private bool HandleFrameHandlerResult(FrameHandlerResult result)
        {
            if (result is FrameHandlerResult.Success success)
            {
                if (success.Value is BarcodeScanningResult barcodeResult)
                {
                    HandleSuccess(barcodeResult);
                }
            }
            else
            {
                cameraView.Post(() => Toast.MakeText(Context.GetActivity(), "License has expired!", ToastLength.Long).Show());
                cameraView.RemoveFrameHandler(barcodeDetectorFrameHandler);
            }

            return false;
        }

        private void CheckPermissions()
        {
            if (Context == null || Context.GetActivity() == null)
            {
                return;
            }

            var activity = Context.GetActivity();

            if (ContextCompat.CheckSelfPermission(activity, Manifest.Permission.Camera) != Permission.Granted)
            {
                ActivityCompat.RequestPermissions(activity, new string[] { Manifest.Permission.Camera }, REQUEST_PERMISSION_CODE);
            }
        }
    }

    // Here we define a custom BarcodeDetectorResultHandler. Whenever a result is ready, the frame handler
    // will call the Handle method on this object. To make this more flexible, we allow to
    // specify a delegate through the constructor.
    class BarcodeDetectorResultHandler : BarcodeDetectorFrameHandler.BarcodeDetectorResultHandler
    {
        public delegate bool HandleResultFunction(FrameHandlerResult result);
        public readonly HandleResultFunction handleResultFunc;

        public BarcodeDetectorResultHandler(HandleResultFunction handleResultFunc)
        {
            this.handleResultFunc = handleResultFunc;
        }

        public override bool Handle(FrameHandlerResult result)
        {
            handleResultFunc(result);
            return false;
        }
    }
}