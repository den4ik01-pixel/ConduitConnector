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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ConduitConnector
{
    /// <summary>
    /// Interaction logic for SettingsControl.xaml
    /// </summary>
    public partial class SettingsControl : UserControl
    {
        public double Angle;
        public bool WasConnectBtnClicked;

        public SettingsControl()
        {
            InitializeComponent();
        }

        #region UiType1 Slider

        private void degreesSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            Angle = degreesSlider.Value;
        }

        private void Connect1Btn_Click(object sender, RoutedEventArgs e)
        {
            WasConnectBtnClicked = true;
            (this.Parent as Window).Close();
        }

        #endregion

        #region UiType2 Buttons

        private void Connect2Btn_Click(object sender, RoutedEventArgs e)
        {
            Button senderButton = sender as Button;

            if (senderButton.Content != null)
            {
                Angle = double.Parse(senderButton.Content.ToString().Replace("°", ""));
                WasConnectBtnClicked = true;
                (this.Parent as Window).Close();
            }
            else
            {
                if (!string.IsNullOrEmpty(AngleTb.Text))
                {
                    if (!double.TryParse(AngleTb.Text, out Angle))
                    {
                        MessageBox.Show("Enter the valid angle to connect conduits");
                        return;
                    }

                    WasConnectBtnClicked = true;
                    (this.Parent as Window).Close();
                }
                else
                    MessageBox.Show("Enter or choose the angle to connect conduits");
            }
        }

        #endregion

        #region UiType3 RadioButtons

        private void Connect3Btn_Click(object sender, RoutedEventArgs e)
        {
            WasConnectBtnClicked = true;
            (this.Parent as Window).Close();
        }

        private void RadioButton_Checked(object sender, RoutedEventArgs e)
        {
            RadioButton senderButton = sender as RadioButton;

            if (senderButton.Content != null)
            {
                Angle = double.Parse(senderButton.Content.ToString().Replace("°", ""));
                WasConnectBtnClicked = true;
            }
        }

        #endregion
    }
}
