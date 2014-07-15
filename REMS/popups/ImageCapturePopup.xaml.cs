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

            windowsFromsHost1.Child = icImagingControl1;
        }

        private void ImageCapturePopup_Closed(object sender, EventArgs e)
        {
            callback(mImageBitmap);
        }

        private void DeviceLost()
        {
            icImagingControl1.Device = "";
            MessageBox.Show("Device Lost!");
        }

        private void icImagingControl1_Load(object sender, EventArgs e)
        {
            icImagingControl1.ShowDeviceSettingsDialog();
            if (icImagingControl1.DeviceValid)
            {
                icImagingControl1.LiveStart();
            }
            else
            {
                //MessageBox.Show("No Device!");
                //Close();
            }
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
            /*SaveFileDialog dlg = new SaveFileDialog();
            dlg.AddExtension = true;
            dlg.DefaultExt = "jpg";
            dlg.Filter = "JPEG Images (*.jpg)|*.jpg||";
            dlg.OverwritePrompt = true;*/

            //if (dlg.ShowDialog() == DialogResult.OK)
            //{
            //icImagingControl1.ImageActiveBuffer.SaveAsJpeg(mFileName, 100);
            
            //}
        }

        private void click_properties(object sender, EventArgs e)
        {
            if (icImagingControl1.DeviceValid)
            {
                icImagingControl1.ShowPropertyDialog();
            }
        }
    }
}
