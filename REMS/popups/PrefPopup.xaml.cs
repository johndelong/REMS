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

namespace REMS.popups
{
    /// <summary>
    /// Interaction logic for preferences.xaml
    /// </summary>
    public partial class PrefPopup : Window
    {
        public PrefPopup()
        {
            InitializeComponent();
            gridThresholds.ItemsSource = ExternalData.GetThresholds();
        }

        private void click_add(object sender, RoutedEventArgs e)
        {
            NewThresholdPopup popup = new NewThresholdPopup();
            popup.ShowDialog();
        }

        private void click_edit(object sender, RoutedEventArgs e)
        {
            NewThresholdPopup popup = new NewThresholdPopup();
            popup.editThreshold((Threshold)gridThresholds.SelectedItem);
            popup.ShowDialog();
        }

        private void gridThresholdData_Selected(object sender, RoutedEventArgs e)
        {
            Threshold lData = (Threshold)gridThresholds.SelectedItem;
            gridThresholdData.ItemsSource = lData.data;
        }
    }
}
