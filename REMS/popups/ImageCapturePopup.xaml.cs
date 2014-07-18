using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace REMS.popups
{
    /// <summary>
    /// Interaction logic for ImageCapturePopup.xaml
    /// </summary>
    public partial class ImageCapturePopup : Window
    {
        //private TIS.Imaging.ICImagingControl icImagingControl1;
        private TIS.Imaging.ICImagingControl icImagingControl1 = new TIS.Imaging.ICImagingControl();
        private delegate void DeviceLostDelegate();
        private Action<System.Drawing.Bitmap> callback;
        private System.Drawing.Bitmap mImageBitmap = null;

        public ImageCapturePopup(Action<System.Drawing.Bitmap> aCallbackFunction)
        {
            InitializeComponent();
            Closed += ImageCapturePopup_Closed;
            callback = aCallbackFunction;

            camera_init();
        }

        private void camera_init()
        {
            windowsFromsHost1.Child = icImagingControl1;

            // Try to load the previously used device. 
            try
            {
                icImagingControl1.LoadDeviceStateFromFile("device.xml", true);
            }
            catch
            {
                // Either the xml file does not exist or the device
                // could not be loaded. In both cases we do nothing and proceed.
            }
            btnProperties.IsEnabled = icImagingControl1.DeviceValid;

            icImagingControl1.LiveDisplayDefault = false;
            icImagingControl1.LiveDisplayHeight = icImagingControl1.Height;
            icImagingControl1.LiveDisplayWidth = icImagingControl1.Width;

            if (icImagingControl1.DeviceValid)
            {
                StartLiveVideo();
                btnCapture.IsEnabled = true;
            }
            else
            {
                btnCapture.IsEnabled = false;
            }
        }

        private void ImageCapturePopup_Closed(object sender, EventArgs e)
        {
            if (icImagingControl1.DeviceValid)
            {
                icImagingControl1.LiveStop();
            }

            callback(mImageBitmap);
        }

        private void icImagingControl1_Load(object sender, EventArgs e)
        {
            if (icImagingControl1.DeviceValid)
            {
                StopLiveVideo();
            }
            else
            {
                icImagingControl1.Device = "";
            }
            icImagingControl1.ShowDeviceSettingsDialog();
            //cmdLive.Enabled = icImagingControl1.DeviceValid;
            btnProperties.IsEnabled = icImagingControl1.DeviceValid;
            if (icImagingControl1.DeviceValid)
            {
                // Save the currently used device into a file in order to be able to open it
                // automatically at the next program start.
                icImagingControl1.SaveDeviceStateToFile("device.xml");
            }

            if (icImagingControl1.DeviceValid && !icImagingControl1.LiveVideoRunning)
            {
                StartLiveVideo();
                btnCapture.IsEnabled = true;
            }
        }

        /// <summary>
        /// Start the live video and update the state of the start/stop button.
        /// </summary>
        private void StartLiveVideo()
        {
            icImagingControl1.LiveStart();
            //cmdLive.Text = "Stop Live";
        }

        /// <summary>
        /// Stop the live video and update the state of the start/stop button.
        /// </summary>
        private void StopLiveVideo()
        {
            icImagingControl1.LiveStop();
            //cmdLive.Text = "Start Live";
        }

        private void click_capture(object sender, EventArgs e)
        {
            //http://www.imagingcontrol.com/en_US/library/dotnet/saving-an-image-jpeg/

            if (icImagingControl1.DeviceValid)
            {
                icImagingControl1.MemorySnapImage();
                mImageBitmap = icImagingControl1.ImageActiveBuffer.Bitmap;
                Close();
            }
        }

        private void click_properties(object sender, EventArgs e)
        {
            if (icImagingControl1.DeviceValid)
            {
                icImagingControl1.ShowPropertyDialog();
            }
        }

        private void windowsFromsHost1_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (icImagingControl1.DeviceValid)
            {
                icImagingControl1.LiveDisplayHeight = icImagingControl1.Height;
                icImagingControl1.LiveDisplayWidth = icImagingControl1.Width;
            }
        }
    }
}
