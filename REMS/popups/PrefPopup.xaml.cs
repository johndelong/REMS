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



namespace REMS.popups
{
    /// <summary>
    /// Interaction logic for preferences.xaml
    /// </summary>
    public partial class PrefPopup : Window, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        Dictionary<String,Boolean> componentChanges = new Dictionary<string,Boolean>();

        private string _motorXTravelDistance = Convert.ToString(Properties.Settings.Default.motorXTravelDistance);
        private string _motorYTravelDistance = Convert.ToString(Properties.Settings.Default.motorYTravelDistance);
        private string _motorZTravelDistance = Convert.ToString(Properties.Settings.Default.motorZTravelDistance);
        //private double _heatMapOpacity = Properties.Settings.Default.heatMapOpacity;
        private Boolean _hasChanged;

        public PrefPopup()
        {
            InitializeComponent();
            //heatMapOpacity.Value = _heatMapOpacity;
            TestSource = new ObservableCollection<ThresholdDetails>();
            this.DataContext = this;

            hasChanged = false;
            gridThresholds.ItemsSource = ExternalData.GetThresholds();
        }

        private ObservableCollection<ThresholdDetails> _testSource;
        public ObservableCollection<ThresholdDetails> TestSource
        {
            get
            {
                return _testSource;
            }
            set
            {
                _testSource = value;
                OnPropertyChanged("TestSource");
            }
        }

        private void click_add(object sender, RoutedEventArgs e)
        {
            
        }

        private void click_edit(object sender, RoutedEventArgs e)
        {
            ThresholdDetails lRow = new ThresholdDetails();
            
            TestSource.Add(lRow);
            //gridThresholdData.ItemsSource
        }

        private void btnOK_Click(object sender, RoutedEventArgs e)
        {
            saveUserPreferences();
            this.Close();
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void btnApply_Click(object sender, RoutedEventArgs e)
        {
            saveUserPreferences();
            hasChanged = false;
        }

        private void gridThresholdData_Selected(object sender, RoutedEventArgs e)
        {
            Threshold lData = (Threshold)gridThresholds.SelectedItem;
            TestSource.Clear();
            foreach (ThresholdDetails lRow in lData.data)
            {
                TestSource.Add(lRow);
            }
            //gridThresholdData.ItemsSource = lData.data;
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

        public Boolean hasChanged
        {
            get { return _hasChanged; }
            set { _hasChanged = value; OnPropertyChanged("hasChanged");} 
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
            Properties.Settings.Default.heatMapOpacity = Convert.ToInt32(heatMapOpacity.Value);
        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            TextBox tb = sender as TextBox;
            BindingExpression be = tb.GetBindingExpression(TextBox.TextProperty);
            string bindingPath = be.ParentBinding.Path.Path;
            componentChanges[bindingPath] = tb.Text != Convert.ToString(Properties.Settings.Default[bindingPath]);

            hasChanged = changesPresent();
        }

        private Boolean changesPresent()
        {
            Boolean lResult = false;
            foreach (Boolean lVal in componentChanges.Values)
            {
                if (lVal)
                {
                    lResult = true;
                    break;
                }
            }
            return lResult;
        }

        protected void OnPropertyChanged(string propertyName)
        {
            if (this.PropertyChanged != null)
            {
                this.PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
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

            hasChanged = changesPresent();
        }
    }
}
