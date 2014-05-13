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
    /// Interaction logic for preferences.xaml
    /// </summary>
    public partial class NewThresholdPopup : Window
    {
        private Threshold mEditThreshold;

        public NewThresholdPopup()
        {
            InitializeComponent();

            /*
            List<AnalyzeSynopsisData> authors = new List<AnalyzeSynopsisData>();

            authors.Add(new AnalyzeSynopsisData()
            {
                threshold = "Test 1",
                state = "passed"
            });
             * */
        }

        public void editThreshold(Threshold aThreshold)
        {
            mEditThreshold = aThreshold;
            thresholdDefGrid.ItemsSource = mEditThreshold.data;
        }


    }
}
