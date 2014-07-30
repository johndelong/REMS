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
using REMS.classes;

namespace REMS.popups
{
    /// <summary>
    /// Interaction logic for ProbeChangePopup.xaml
    /// </summary>
    public partial class ProbeChangePopup : Window
    {
        private Action<String, int, int> callback;

        public ProbeChangePopup(Action<String, int, int> aCallbackFunction)
        {
            InitializeComponent();
            callback = aCallbackFunction;
        }

        private void btnOK_Click(object sender, RoutedEventArgs e)
        {
            int lProbeNum = 0;
            int lProbeOffset = 0;
            if(rbHField.IsChecked == true)
            {
                if (rbHProbe1.IsChecked == true)
                {
                    lProbeNum = 1;
                    lProbeOffset = 140;
                }
                else if (rbHProbe2.IsChecked == true)
                {
                    lProbeNum = 2;
                    lProbeOffset = 130;
                }
                else if (rbHProbe3.IsChecked == true)
                {
                    lProbeNum = 3;
                    lProbeOffset = 120;
                }
            }
            else if (rbEField.IsChecked == true)
            {
                if (rbEProbe1.IsChecked == true)
                {
                    lProbeNum = 1;
                }
                else if (rbEProbe2.IsChecked == true)
                {
                    lProbeNum = 2;
                }
            }

            string lScanMode;

            if (rbEField.IsChecked == true)
                lScanMode = Constants.EField;
            else
                lScanMode = Constants.HField;

            callback(lScanMode, lProbeNum, lProbeOffset);
            Close();
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
