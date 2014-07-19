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
using REMS.popups;
using REMS.classes;
using System.ComponentModel;
using NationalInstruments.Controls;
using System.Collections.ObjectModel;
using REMS.data;

namespace REMS.popups
{
    /// <summary>
    /// Interaction logic for preferences.xaml
    /// </summary>
    public partial class PrefPopup : Window
    {
        ObservableCollection<ThresholdViewModel> _thresholds;

        Dictionary<String, Boolean> componentChanges = new Dictionary<string, Boolean>();

        private string _motorXTravelDistance = Convert.ToString(Properties.Settings.Default.motorXTravelDistance);
        private string _motorYTravelDistance = Convert.ToString(Properties.Settings.Default.motorYTravelDistance);
        private string _motorZTravelDistance = Convert.ToString(Properties.Settings.Default.motorZTravelDistance);
        private string _SAMinFrequency = Convert.ToString(Properties.Settings.Default.SAMinFrequency);
        private string _SAMaxFrequency = Convert.ToString(Properties.Settings.Default.SAMaxFrequency);
        private string _MotorCommPort = Convert.ToString(Properties.Settings.Default.MotorCommPort);
        private string _SAConnectionStrring = Properties.Settings.Default.SAConnectionString;

        public PrefPopup()
        {
            InitializeComponent();
            _thresholds = new ObservableCollection<ThresholdViewModel>(ExternalData.GetThresholds());
            gridThresholds.ItemsSource = _thresholds;

            btnRemoveLimit.IsEnabled = false;
            btnRemoveThreshold.IsEnabled = false;
            btnAddLimit.IsEnabled = false;
        }

        private void click_addThreshold(object sender, RoutedEventArgs e)
        {
            ThresholdViewModel lRow = new ThresholdViewModel();
            _thresholds.Add(lRow);
        }

        private void click_removeThreshold(object sender, RoutedEventArgs e)
        {
            _thresholds.RemoveAt(gridThresholds.SelectedIndex);
        }

        private void click_addLimit(object sender, RoutedEventArgs e)
        {
            ThresholdLimitViewModel lRow = new ThresholdLimitViewModel();
            _thresholds.ElementAt<ThresholdViewModel>(gridThresholds.SelectedIndex).Limits.Add(lRow);

        }

        private void click_removeLimit(object sender, RoutedEventArgs e)
        {
            _thresholds.ElementAt<ThresholdViewModel>(gridThresholds.SelectedIndex).Limits.RemoveAt(gridThresholdLimits.SelectedIndex);
        }

        private void btnOK_Click(object sender, RoutedEventArgs e)
        {
            savePreferences();
            this.Close();
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void btnApply_Click(object sender, RoutedEventArgs e)
        {
            savePreferences();
        }

        public string saConnectionString
        {
            get { return _SAConnectionStrring; }
            set
            {
                _SAConnectionStrring = value;
            }
        }

        public string motorXTravelDistance
        {
            get { return _motorXTravelDistance; }
            set
            {
                _motorXTravelDistance = validateNumericInput(value) ? value : _motorXTravelDistance;
            }
        }

        public string motorYTravelDistance
        {
            get { return _motorYTravelDistance; }
            set
            {
                _motorYTravelDistance = validateNumericInput(value) ? value : _motorYTravelDistance;
            }
        }

        public string motorZTravelDistance
        {
            get { return _motorZTravelDistance; }
            set
            {
                _motorZTravelDistance = validateNumericInput(value) ? value : _motorZTravelDistance;
            }
        }

        public string SAMinFrequency
        {
            get { return _SAMinFrequency; }
            set
            {
                _SAMinFrequency = validateNumericInput(value) ? value : _SAMinFrequency;
            }
        }

        public string SAMaxFrequency
        {
            get { return _SAMaxFrequency; }
            set
            {
                _SAMaxFrequency = validateNumericInput(value) ? value : _SAMaxFrequency;
            }
        }

        public string MotorCommPort
        {
            get { return _MotorCommPort; }
            set
            {
                _MotorCommPort = validateNumericInput(value) ? value : _MotorCommPort;
            }
        }

        private Boolean validateNumericInput(string input)
        {
            Boolean lResult = false;
            try
            {
                double lVal = Convert.ToDouble(input);
                if (lVal >= 0)
                    lResult = true;
            }
            catch
            {
                lResult = false;
            }

            return lResult;
        }

        private void saveUserPreferences()
        {
            Properties.Settings.Default.motorXTravelDistance = Convert.ToDouble(_motorXTravelDistance);
            Properties.Settings.Default.motorYTravelDistance = Convert.ToDouble(_motorYTravelDistance);
            Properties.Settings.Default.motorZTravelDistance = Convert.ToDouble(_motorZTravelDistance);
            Properties.Settings.Default.SAMinFrequency = Convert.ToDouble(_SAMinFrequency);
            Properties.Settings.Default.SAMaxFrequency = Convert.ToDouble(_SAMaxFrequency);
            Properties.Settings.Default.MotorCommPort = Convert.ToInt32(_MotorCommPort);
        }

        private void nsPercentageValidator(object sender, ValueChangedEventArgs<int> e)
        {
            NumericTextBoxInt32 numericStepper = sender as NumericTextBoxInt32;

            // Step size cannot be less than 1
            if (e.NewValue < 1)
                numericStepper.Value = 1;

            if (e.NewValue > 100)
                numericStepper.Value = 100;
        }

        private void nsValueChanged(object sender, ValueChangedEventArgs<int> e)
        {
            NumericTextBoxInt32 numericStepper = sender as NumericTextBoxInt32;
            nsPercentageValidator(sender, e);
            componentChanges[numericStepper.Name] = numericStepper.Value != (int)Properties.Settings.Default[numericStepper.Name];
        }

        private void savePreferences()
        {
            saveUserPreferences();

            ExternalData.SaveThresholds(_thresholds);
        }

        private void gridThresholdLimits_RowEditEnding(object sender, DataGridRowEditEndingEventArgs e)
        {
            if (this.gridThresholdLimits.SelectedItem != null)
            {
                (sender as DataGrid).RowEditEnding -= gridThresholdLimits_RowEditEnding;
                (sender as DataGrid).CommitEdit();
                (sender as DataGrid).Items.Refresh();
                (sender as DataGrid).RowEditEnding += gridThresholdLimits_RowEditEnding;
            }
            else return;

            //Sort threshold data
            foreach (ThresholdViewModel lThreshold in _thresholds)
            {
                Utilities.Sort<ThresholdLimitViewModel>(lThreshold.Limits);
            }

        }

        private void gridThresholdLimits_SelectedCellsChanged(object sender, SelectedCellsChangedEventArgs e)
        {
            if (gridThresholdLimits.SelectedItem != null)
            {
                btnRemoveLimit.IsEnabled = true;
            }
            else
            {
                btnRemoveLimit.IsEnabled = false;
            }
        }

        private void gridThresholds_SelectedCellsChanged(object sender, SelectedCellsChangedEventArgs e)
        {
            if (gridThresholds.SelectedItem != null)
            {
                btnRemoveThreshold.IsEnabled = true;
                btnAddLimit.IsEnabled = true;
            }
            else
            {
                btnRemoveThreshold.IsEnabled = false;
                btnAddLimit.IsEnabled = false;
            }
        }
    }
}
