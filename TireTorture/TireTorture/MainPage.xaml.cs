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

        bool bMeasuringDistance, bAngledWalk, bRotation;
        public MainPage()
        {
            InitializeComponent();
            bMeasuringDistance = false;
            bAngledWalk = false;
            bRotation = false;
            try
            {
                OrientationSensorStart(true);
                OrientationSensor.Start(speed);
            }
            catch (FeatureNotSupportedException)
            {
                StartingLocation.Text = "The Orientation sensor is not supported on this device!";
            }
            catch (FeatureNotEnabledException)
            {
                StartingLocation.Text = "The Orientation sensor is not enabled on this device!";
            }
            catch (PermissionException)
            {//ex is null
                StartingLocation.Text = "Permission Exception";
            }
            catch (Exception)
            {
                StartingLocation.Text = "Unknown Startup Exception!";
            }
        }

        private async void StartMeasuringDistance()
        {
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
            if (startingPoint != null)
                StartingLocation.Text = "Latitude: " + startingPoint.Latitude.ToString() + "  Longitude: " + startingPoint.Longitude.ToString();
            else
                ElapsedDistance.Text = "Couldn't get Starting Point";
        }

        private async void StopMeasuringDistance()
        {
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
            if (bRotation == true)
            {
                double nRotations = nDistanceCovered / 10;
                ElapsedRotations.Text = String.Format("{0:0.00}", nRotations) + " rotations have occured!";
            }
        }


        private void DistanceButton_Clicked(object sender, EventArgs e)
        {
            if (bAngledWalk == false && bRotation == false)
            {
                if (bMeasuringDistance == true)
                {
                    StopMeasuringDistance();
                    DistanceButton.Text = "Start Measuring Distance";
                    bMeasuringDistance = false;
                }
                else
                {
                    DistanceButton.Text = "Stop Measuring Distance";
                    bMeasuringDistance = true;
                    ResetOutputVariables();
                    StartMeasuringDistance();
                }
            }
            else
            {
                StartingLocation.Text = "You can only run one function at a time!";
            }
        }

        public void ResetOutputVariables()
        {
            StartingLocation.Text = "";
            FinalLocation.Text = "";
            ElapsedDistance.Text = "";
            ElapsedRotations.Text = "";
        }

        private void RotationButton_Clicked(object sender, EventArgs e)
        {
            if (bAngledWalk == false && bMeasuringDistance == false)
            {
                if (bRotation == false)
                {
                    bRotation = true;
                    RotationButton.Text = "Stop Measuring Rotations";
                    ResetOutputVariables();
                    StartMeasuringDistance();
                }
                else
                {
                    StopMeasuringDistance();
                    RotationButton.Text = "Start Measuring Rotations";
                    bRotation = false;
                }
            }
            else
            {
                StartingLocation.Text = "You can only run one function at a time!";
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
            EulerAngles0.Text = eulerAngles[0].ToString("#.##");
            EulerAngles1.Text = eulerAngles[1].ToString("#.##");
            EulerAngles2.Text = eulerAngles[1].ToString("#.##");
            if (bAngledWalk == true && CheckOrientation() == false)
            {
                EulerAngles2.Text = "Whoops...";
                bAngledWalk = false;
                StartMeasuringAngledWalk(bAngledWalk);
            }
        }

        public void OrientationSensorStart(bool bStartOrientationSensor)
        {// Register for reading changes, be sure to unsubscribe when finished
            if (bStartOrientationSensor == true)
                OrientationSensor.ReadingChanged += OrientationSensor_ReadingChanged;
            else
                OrientationSensor.ReadingChanged -= OrientationSensor_ReadingChanged;
        }

        bool CheckOrientation()
        {
            if (eulerAngles[0] >= .4 && eulerAngles[0] <= .95)
                return true;
            else
                return false;
        }

        public void StartMeasuringAngledWalk(bool bAngledWalk)
        {
            if (bAngledWalk == true)
            {
                AngledWalkButton.Text = "Stop Angled Walk";
                ResetOutputVariables();
                StartMeasuringDistance();
            }
            else
            {
                AngledWalkButton.Text = "Start Angled Walk";
                StopMeasuringDistance();
            }
        }

        public void AngledWalkButton_Clicked(object sender, EventArgs e)
        {
            if (bRotation == false && bMeasuringDistance == false)
            {
                if (bAngledWalk == false)
                {
                    if (CheckOrientation() == true)
                    {
                        bAngledWalk = true;
                        EulerOutput.Text = "";
                        StartMeasuringAngledWalk(bAngledWalk);
                    }
                    else
                    {
                        EulerOutput.Text = "The Euler Angle needs to be between .4 and .95";
                    }
                }
                else
                {
                    bAngledWalk = false;
                    StartMeasuringAngledWalk(bAngledWalk);
                }
            }
            else
            {
                StartingLocation.Text = "You can only run one function at a time!";
            }
        }
    }
}

