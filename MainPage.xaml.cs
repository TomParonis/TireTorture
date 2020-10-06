using System;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace TireTorture
{
    public partial class MainPage : ContentPage
    {
        Location startingPoint;
        Location endingPoint;
        SensorSpeed speed = SensorSpeed.Default;

        //0 = roll, 1 = pitch, 2 = yaw 
        double[] eulerAngles = new double[3];

        bool bMeasuringDistance;
        public MainPage()
        {
            InitializeComponent();
            bMeasuringDistance = false;
        }

        private async void DistanceButton_Clicked(object sender, EventArgs e)
        {
            if (bMeasuringDistance == false)
            {
                bMeasuringDistance = true;
                StartingLocation.Text = "";
                FinalLocation.Text = "";
                ElapsedDistance.Text = "";
                DistanceButton.Text = "Stop Measuring Distance";
                try
                {
                    var request = new GeolocationRequest(GeolocationAccuracy.Best);
                    startingPoint = await Geolocation.GetLocationAsync(request);
                }
                catch (FeatureNotSupportedException ex)
                {
                    StartingLocation.Text = "This feature is not supported on this device!";
                }
                catch (FeatureNotEnabledException ex)
                {
                    StartingLocation.Text = "This feature is not enabled on this device!";
                }
                catch (PermissionException ex)
                {//ex is null
                    StartingLocation.Text = "Permission Exception";
                }
                catch (Exception ex)
                {
                    StartingLocation.Text = "Exception!";
                }
                StartingLocation.Text = "Latitude: " + startingPoint.Latitude.ToString() + "  Longitude: " + startingPoint.Longitude.ToString();
            }
            else
            {
                bMeasuringDistance = false;
                DistanceButton.Text = "Start Measuring Distance";
                try
                {
                    var request = new GeolocationRequest(GeolocationAccuracy.Best);
                    endingPoint = await Geolocation.GetLocationAsync(request);
                }
                catch (FeatureNotSupportedException)
                {
                    FinalLocation.Text = "This feature is not supported on this device!";
                }
                catch (FeatureNotEnabledException)
                {
                    FinalLocation.Text = "This feature is not enabled on this device!";
                }
                catch (PermissionException)
                {
                    FinalLocation.Text = "This feature is not permitted on this device!";
                }
                catch (Exception)
                {
                    FinalLocation.Text = "Exception!";
                }
                FinalLocation.Text = "Latitude: " + endingPoint.Latitude.ToString() + "  Longitude: " + endingPoint.Longitude.ToString();
                double nDistanceCovered = Location.CalculateDistance(startingPoint, endingPoint, DistanceUnits.Kilometers);
                nDistanceCovered = nDistanceCovered * 1000;
                ElapsedDistance.Text = String.Format("{0:0.00}", nDistanceCovered) + " meters have been covered!";
            }
        }

        void ToEulerAngles(double W, double X, double Y, double Z)
        {//0 = roll, 1 = pitch, 2 = yaw 
            // roll (x-axis rotation)
            double sinr_cosp = 2 * (W * X + Y * Z);
            double cosr_cosp = 1 - 2 * (X * X + Y * Y);
            eulerAngles[0] = Math.Atan2(sinr_cosp, cosr_cosp);

            // pitch (y-axis rotation)
            double sinp = 2 * (W * Y - Z * X);
            if (Math.Abs(sinp) >= 1)
            {
                if (sinp >= 0)
                {
                    eulerAngles[1] = Math.PI / 2;
                }
                else
                {
                    eulerAngles[1] = 0 - Math.PI / 2; // use 90 degrees if out of range
                }
            }
            else
                eulerAngles[1] = Math.Asin(sinp);

            // yaw (z-axis rotation)
            double siny_cosp = 2 * (W * Z + X * Y);
            double cosy_cosp = 1 - 2 * (Y * Y + Z * Z);
            eulerAngles[2] = Math.Atan2(siny_cosp, cosy_cosp);


        }

        void OrientationSensor_ReadingChanged(object sender, OrientationSensorChangedEventArgs e)
        {
            var data = e.Reading;
            ToEulerAngles(data.Orientation.W, data.Orientation.X, data.Orientation.Y, data.Orientation.Z);
            EulerOutput.Text = eulerAngles[0].ToString("#.##") + "," + eulerAngles[1].ToString("#.##") + "," + eulerAngles[2].ToString("#.##");
        }

        public void OrientationSensorTest()
        {
            // Register for reading changes, be sure to unsubscribe when finished
            OrientationSensor.ReadingChanged += OrientationSensor_ReadingChanged;
        }


        private async void AngledWalkButton_Clicked(object sender, EventArgs e)
        {
            try
            {
                var startRequest = new GeolocationRequest(GeolocationAccuracy.Best);
                startingPoint = await Geolocation.GetLocationAsync(startRequest);
                OrientationSensorTest();
                OrientationSensor.Start(speed);
                while (OrientationSensor.IsMonitoring)
                {
                    var endRequest = new GeolocationRequest(GeolocationAccuracy.Best);
                    endingPoint = await Geolocation.GetLocationAsync(endRequest);
                    //double meters = Location.CalculateDistance(startingPoint, endingPoint, DistanceUnits.Kilometers);
                    //meters = meters * 1000;
                    //distanceElapsed.Text = String.Format("{0:0.00}", meters) + " meters have been covered!";
                }
            }
            catch (FeatureNotSupportedException fnsEx)
            {
                if (fnsEx != null)
                    EulerOutput.Text = fnsEx.Message;
                else
                    EulerOutput.Text = "This feature is not supported by your device!";
            }
            catch (Exception ex)
            {
                if (ex != null)
                    EulerOutput.Text = ex.Message;
                else
                    EulerOutput.Text = "Unknown Exception";
            }
        }
    }
}

