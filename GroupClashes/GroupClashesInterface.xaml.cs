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

namespace GroupClashes
{
    /// <summary>
    /// Interaction logic for GroupClashesInterface.xaml
    /// </summary>
    public partial class GroupClashesInterface : UserControl
    {
        public GroupClashesInterface()
        {
            InitializeComponent();
        }

        private void Ok_Button_Click(object sender, RoutedEventArgs e)
        {
            //this.DialogResult = true;
            //this.Close();
        }

        private void Cancel_Button_Click(object sender, RoutedEventArgs e)
        {

            //this.DialogResult = false;
            //this.Close();
        }
    }
}
