using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WIN = System.Windows;
using System.Windows.Controls;
using System.ComponentModel;
using System.Windows.Markup;
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
        public ObservableCollection<CustomClashTest> ClashTests { get; set; }
        public ObservableCollection<GroupingMode> GroupByList { get; set; }
        public ObservableCollection<GroupingMode> GroupThenList { get; set; }
        public ClashTest SelectedClashTest { get; set; }

        public GroupClashesInterface()
        {
            InitializeComponent();

            ClashTests = new ObservableCollection<CustomClashTest>();
            GroupByList = new ObservableCollection<GroupingMode>();
            GroupThenList = new ObservableCollection<GroupingMode>();

            RegisterChanges();
            
            GetClashTests();
            CheckPlugin();
            LoadComboBox();
            this.DataContext = this;
        }

        private void Group_Button_Click(object sender, WIN.RoutedEventArgs e)
        {
            if (ClashTestListBox.SelectedItem != null)
            {
                CustomClashTest selectedClashTest = (CustomClashTest)ClashTestListBox.SelectedItem;
                ClashTest clashTest = selectedClashTest.ClashTest;
                
                if (clashTest.Children.Count != 0)
                {
                    if (comboBoxGroupBy.SelectedItem != null
    || (GroupingMode)comboBoxGroupBy.SelectedItem == GroupingMode.None)
                    {
                        //Unsubscribe temporarly
                        UnRegisterChanges();

                        if (comboBoxThenBy.SelectedItem == null
                            || (GroupingMode)comboBoxThenBy.SelectedItem == GroupingMode.None)
                        {
                            GroupingMode mode = (GroupingMode)comboBoxGroupBy.SelectedItem;
                            GroupingFunctions.GroupClashes(clashTest, mode, GroupingMode.None);
                        }
                        else
                        {
                            GroupingMode byMode = (GroupingMode)comboBoxGroupBy.SelectedItem;
                            GroupingMode thenByMode = (GroupingMode)comboBoxThenBy.SelectedItem;
                            GroupingFunctions.GroupClashes(clashTest, thenByMode, byMode);
                        }

                        //Resubscribe
                        RegisterChanges();
                    }
                }
            }

        }

        private void Ungroup_Button_Click(object sender, WIN.RoutedEventArgs e)
        {
            if (ClashTestListBox.SelectedItem != null)
            {
                CustomClashTest selectedClashTest = (CustomClashTest)ClashTestListBox.SelectedItem;
                ClashTest clashTest = selectedClashTest.ClashTest;

                if (clashTest.Children.Count != 0)
                {
                    GroupingFunctions.UnGroupClashes(clashTest);
                }
            }
        }

        private void RegisterChanges()
        {
            //When the document change
            Application.MainDocument.Database.Changed += DocumentClashTests_Changed;

            //When a clash test change
            DocumentClashTests DCT = Application.MainDocument.GetClash().TestsData;
            //Register
            DCT.Changed += DocumentClashTests_Changed;
        }

        private void UnRegisterChanges()
        {
            //When the document change
            Application.MainDocument.Database.Changed -= DocumentClashTests_Changed;

            //When a clash test change
            DocumentClashTests DCT = Application.MainDocument.GetClash().TestsData;
            //Register
            DCT.Changed -= DocumentClashTests_Changed;
        }

        void DocumentClashTests_Changed(object sender, EventArgs e)
        {
            GetClashTests();
            CheckPlugin();
            LoadComboBox();
        }

        private void GetClashTests()
        {
            DocumentClashTests DCT = Application.MainDocument.GetClash().TestsData;
            ClashTests.Clear();
            foreach (ClashTest test in DCT.Tests)
            {
                ClashTests.Add(new CustomClashTest(test));
            }
        }

        private void CheckPlugin()
        {
            //Inactive if there is no document open or there are no clash tests
            if (Application.MainDocument == null
                || Application.MainDocument.IsClear
                || Application.MainDocument.GetClash() == null
                || Application.MainDocument.GetClash().TestsData.Tests.Count == 0)
            {
                Group_Button.IsEnabled = false;
                comboBoxGroupBy.IsEnabled = false;
                comboBoxThenBy.IsEnabled = false;
                Ungroup_Button.IsEnabled = false;
            }
            else
            {
                Group_Button.IsEnabled = true;
                comboBoxGroupBy.IsEnabled = true;
                comboBoxThenBy.IsEnabled = true;
                Ungroup_Button.IsEnabled = true;
            }
        }

        private void LoadComboBox()
        {
            GroupByList.Clear();
            GroupThenList.Clear();

            foreach (GroupingMode mode in Enum.GetValues(typeof(GroupingMode)).Cast<GroupingMode>())
            {
                GroupThenList.Add(mode);
                GroupByList.Add(mode);
            }

            if (Application.MainDocument.Grids.ActiveSystem == null)
            {
                GroupByList.Remove(GroupingMode.GridIntersection);
                GroupByList.Remove(GroupingMode.Level);
                GroupThenList.Remove(GroupingMode.GridIntersection);
                GroupThenList.Remove(GroupingMode.Level);
            }

            comboBoxGroupBy.SelectedIndex = 0;
            comboBoxThenBy.SelectedIndex = 0;
        }
    }

    public class CustomClashTest
    {
        public CustomClashTest(ClashTest test)
        {
            _clashTest = test;
        }

        public string DisplayName { get { return _clashTest.DisplayName; } }

        private ClashTest _clashTest;
        public ClashTest ClashTest { get { return _clashTest; } }

        public string SelectionAName
        {
            get { return GetSelectedItem(_clashTest.SelectionA); }
        }

        public string SelectionBName
        {
            get { return GetSelectedItem(_clashTest.SelectionB); }
        }

        private string GetSelectedItem(ClashSelection selection)
        {
            string result = "";
            if (selection.Selection.GetSelectedItems().Count == 0)
            {
                result = "No item have been selected.";
            }
            else if (selection.Selection.GetSelectedItems().Count == 1)
            {
                result = selection.Selection.GetSelectedItems().FirstOrDefault().DisplayName;
            }
            else
            {
                result = selection.Selection.GetSelectedItems().FirstOrDefault().DisplayName;
                foreach (ModelItem item in selection.Selection.GetSelectedItems().Skip(1))
                {
                    result = result + "; " + item.DisplayName;
                }
            }

            return result;
        }

    }
}
