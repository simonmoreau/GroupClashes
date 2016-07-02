using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WIN = System.Windows;
using System.Windows.Controls;
using System.ComponentModel;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using Autodesk.Navisworks.Api.Clash;
using Autodesk.Navisworks.Api;

namespace GroupClashes
{
    /// <summary>
    /// Interaction logic for GroupClashesInterface.xaml
    /// </summary>
    public partial class GroupClashesInterface : UserControl
    {
        public ObservableCollection<ClashTest> ClashTests { get; set; }
        public ClashTest SelectedClashTest { get; set; }

        public GroupClashesInterface()
        {
            GetClashTests();
            InitializeComponent();
            this.DataContext = this;
            
        }

        private void Group_Button_Click(object sender, WIN.RoutedEventArgs e)
        {
            //this.DialogResult = true;
            //this.Close();
        }

        private void GetClashTests()
        {
            DocumentClashTests DCT = Application.MainDocument.GetClash().TestsData;
            //Register
            DCT.Changed += DocumentClashTests_Changed;
            ClashTests = new ObservableCollection<ClashTest>(DCT.Tests.Cast<ClashTest>());
        }

        void DocumentClashTests_Changed(object sender, SavedItemChangedEventArgs e)
        {
            DocumentClashTests DCT = Application.MainDocument.GetClash().TestsData;
            ClashTests.Clear();
            foreach (ClashTest test in DCT.Tests)
            {
                ClashTests.Add(test);
            }
        }

    }
}
