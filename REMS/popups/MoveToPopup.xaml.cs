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
    /// Interaction logic for MoveToPopup.xaml
    /// </summary>
    public partial class MoveToPopup : Window
    {
        private Action<double, double, double> callback;
        private double mX, mY, mZ;

        public MoveToPopup(Action<double, double, double> aCallbackFunction)
        {
            InitializeComponent();
            callback = aCallbackFunction;
        }

        private void btnAccept_Click(object sender, RoutedEventArgs e)
        {
            callback(mX, mY, mZ);
            Close();
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void tb_LostFocus(object sender, RoutedEventArgs e)
        {
            TextBox lTextBox = sender as TextBox;

            double lValue = 0;
            try
            {
                lValue = Convert.ToDouble(lTextBox.Text);
            }
            catch(Exception){}

            if (lValue < 0)
                lValue = 0;

            lTextBox.Text = Convert.ToString(lValue);
        }

        public double XPos
        {
            get { return mX; }
            set { mX = value; }
        }

        public double YPos
        {
            get { return mY; }
            set { mY = value; }
        }

        public double ZPos
        {
            get { return mZ; }
            set { mZ = value; }
        }

    }
}
