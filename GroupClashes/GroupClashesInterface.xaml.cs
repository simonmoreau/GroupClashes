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
        public ClashTest SelectedClashTest { get; set; }

        public GroupClashesInterface()
        {
            InitializeComponent();
            ClashTests = new ObservableCollection<CustomClashTest>();
            RegisterClashTestChanges();
            GetClashTests();
            this.DataContext = this;
        }

        private void Group_Button_Click(object sender, WIN.RoutedEventArgs e)
        {
            if (ClashTestListBox.SelectedItem != null)
            {
                CustomClashTest selectedClashTest = (CustomClashTest)ClashTestListBox.SelectedItem;
                ClashTest clashTest = selectedClashTest.ClashTest;

                if (comboBoxGroupBy.SelectedItem != null)
                {
                    if (comboBoxThenBy.SelectedItem == null)
                    {
                        GroupingMode mode = (GroupingMode)((EnumerationExtension.EnumerationMember)comboBoxGroupBy.SelectedItem).Value;
                        GroupingFunctions.GroupClashes(clashTest, mode, GroupingMode.None);
                    }
                    else
                    {
                        GroupingMode byMode = (GroupingMode)((EnumerationExtension.EnumerationMember)comboBoxGroupBy.SelectedItem).Value;
                        GroupingMode thenByMode = (GroupingMode)((EnumerationExtension.EnumerationMember)comboBoxThenBy.SelectedItem).Value;
                        GroupingFunctions.GroupClashes(clashTest, thenByMode, byMode);
                    }
                }
            }

        }

        private void RegisterClashTestChanges()
        {
            DocumentClashTests DCT = Application.MainDocument.GetClash().TestsData;
            //Register
            DCT.Changed += DocumentClashTests_Changed;
        }

        void DocumentClashTests_Changed(object sender, SavedItemChangedEventArgs e)
        {
            GetClashTests();
        }

        private void GetClashTests()
        {
            DocumentClashTests DCT = Application.MainDocument.GetClash().TestsData;
            ClashTests.Clear();
            foreach (ClashTest test in DCT.Tests)
            {
                ClashTests.Add(new CustomClashTest(test));
            }

            if (ClashTests.Count != 0)
            {
                Group_Button.IsEnabled = true;
            }
            else
            {
                Group_Button.IsEnabled = false;
            }
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

    public class EnumerationExtension : MarkupExtension
    {
        private Type _enumType;

        public EnumerationExtension(Type enumType)
        {
            if (enumType == null) throw new ArgumentNullException("enumType");

            EnumType = enumType;
        }

        public Type EnumType
        {
            get { return _enumType; }
            set
            {
                if (_enumType == value) return;
                var enumType = Nullable.GetUnderlyingType(value) ?? value;
                if (enumType.IsEnum == false) throw new ArgumentException("Type must be an Enum.");
                _enumType = value;
            }
        }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            var enumValues = Enum.GetValues(EnumType);

            return (
              from object enumValue in enumValues
              select new EnumerationMember
              {
                  Value = enumValue,
                  Description = GetDescription(enumValue)
              }).ToArray();
        }

        private string GetDescription(object enumValue)
        {
            var descriptionAttribute = EnumType
              .GetField(enumValue.ToString())
              .GetCustomAttributes(typeof(DescriptionAttribute), false)
              .FirstOrDefault() as DescriptionAttribute;


            return descriptionAttribute != null
              ? descriptionAttribute.Description
              : enumValue.ToString();
        }

        public class EnumerationMember
        {
            public string Description { get; set; }
            public object Value { get; set; }
        }
    }
}
