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
    /// Interaction logic for ProbeChangePopup.xaml
    /// </summary>
    public partial class ProbeChangePopup : Window
    {
        private Action<Boolean, int> callback;

        public ProbeChangePopup(Action<Boolean, int> aCallbackFunction)
        {
            InitializeComponent();
            callback = aCallbackFunction;
        }

        private void btnOK_Click(object sender, RoutedEventArgs e)
        {
            int lProbeNum = 0;
            if(rbHField.IsChecked == true)
            {
                if(rbHProbe1.IsChecked == true)
                    lProbeNum = 1;
                else if(rbHProbe2.IsChecked == true)
                    lProbeNum = 2;
                else if(rbHProbe3.IsChecked == true)
                    lProbeNum = 3;
            }

            callback((rbEField.IsChecked == true), lProbeNum);
            Close();
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
