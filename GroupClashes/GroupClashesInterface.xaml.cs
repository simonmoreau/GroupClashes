using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Controls;
using Autodesk.Navisworks.Api;
using Autodesk.Navisworks.Api.Clash;
using WIN = System.Windows;

namespace GroupClashes;

/// <summary>
/// Interaction logic for GroupClashesInterface.xaml
/// </summary>
public partial class GroupClashesInterface : UserControl
{
    /// <summary>
    /// Gets the clash tests available in the current document.
    /// </summary>
    public ObservableCollection<CustomClashTest> ClashTests { get; set; }

    /// <summary>
    /// Gets the grouping modes available for the "Group by" selection.
    /// </summary>
    public ObservableCollection<GroupingMode> GroupByList { get; set; }

    /// <summary>
    /// Gets the grouping modes available for the "Then by" selection.
    /// </summary>
    public ObservableCollection<GroupingMode> GroupThenList { get; set; }

    /// <summary>
    /// Gets or sets the currently selected clash test.
    /// </summary>
    public ClashTest SelectedClashTest { get; set; }

    public GroupClashesInterface()
    {
        InitializeComponent();

        ClashTests = new ObservableCollection<CustomClashTest>();
        GroupByList = new ObservableCollection<GroupingMode>();
        GroupThenList = new ObservableCollection<GroupingMode>();

        RegisterChanges();

        DataContext = this;
    }

    private void Group_Button_Click(object sender, WIN.RoutedEventArgs e)
    {
        if (ClashTestListBox.SelectedItems.Count != 0)
        {
            UnRegisterChanges();

            foreach (object selectedItem in ClashTestListBox.SelectedItems)
            {
                CustomClashTest selectedClashTest = (CustomClashTest)selectedItem;
                ClashTest clashTest = selectedClashTest.ClashTest;

                if (clashTest.Children.Count != 0)
                {
                    if (comboBoxGroupBy.SelectedItem is null) comboBoxGroupBy.SelectedItem = GroupingMode.None;
                    if (comboBoxThenBy.SelectedItem is null) comboBoxThenBy.SelectedItem = GroupingMode.None;

                    if ((GroupingMode)comboBoxThenBy.SelectedItem != GroupingMode.None
                        || (GroupingMode)comboBoxGroupBy.SelectedItem != GroupingMode.None)
                    {
                        if ((GroupingMode)comboBoxThenBy.SelectedItem == GroupingMode.None
                            && (GroupingMode)comboBoxGroupBy.SelectedItem != GroupingMode.None)
                        {
                            GroupingMode mode = (GroupingMode)comboBoxGroupBy.SelectedItem;
                            GroupingFunctions.GroupClashes(clashTest, mode, GroupingMode.None, (bool)keepExistingGroupsCheckBox.IsChecked);
                        }
                        else if ((GroupingMode)comboBoxGroupBy.SelectedItem == GroupingMode.None
                            && (GroupingMode)comboBoxThenBy.SelectedItem != GroupingMode.None)
                        {
                            GroupingMode mode = (GroupingMode)comboBoxThenBy.SelectedItem;
                            GroupingFunctions.GroupClashes(clashTest, mode, GroupingMode.None, (bool)keepExistingGroupsCheckBox.IsChecked);
                        }
                        else
                        {
                            GroupingMode byMode = (GroupingMode)comboBoxGroupBy.SelectedItem;
                            GroupingMode thenByMode = (GroupingMode)comboBoxThenBy.SelectedItem;
                            GroupingFunctions.GroupClashes(clashTest, byMode, thenByMode, (bool)keepExistingGroupsCheckBox.IsChecked);
                        }
                    }
                }
            }

            RegisterChanges();
        }
    }

    private void Ungroup_Button_Click(object sender, WIN.RoutedEventArgs e)
    {
        if (ClashTestListBox.SelectedItems.Count != 0)
        {
            UnRegisterChanges();

            foreach (object selectedItem in ClashTestListBox.SelectedItems)
            {
                CustomClashTest selectedClashTest = (CustomClashTest)selectedItem;
                ClashTest clashTest = selectedClashTest.ClashTest;

                if (clashTest.Children.Count != 0)
                {
                    GroupingFunctions.UnGroupClashes(clashTest);
                }
            }

            RegisterChanges();
        }
    }

    private void RegisterChanges()
    {
        Application.MainDocument.Database.Changed += DocumentClashTests_Changed;

        DocumentClashTests dct = Application.MainDocument.GetClash().TestsData;
        dct.Changed += DocumentClashTests_Changed;

        GetClashTests();
        CheckPlugin();
        LoadComboBox();
    }

    private void UnRegisterChanges()
    {
        Application.MainDocument.Database.Changed -= DocumentClashTests_Changed;

        DocumentClashTests dct = Application.MainDocument.GetClash().TestsData;
        dct.Changed -= DocumentClashTests_Changed;
    }

    private void DocumentClashTests_Changed(object sender, EventArgs e)
    {
        GetClashTests();
        CheckPlugin();
        LoadComboBox();
    }

    private void GetClashTests()
    {
        DocumentClashTests dct = Application.MainDocument.GetClash().TestsData;
        ClashTests.Clear();

        foreach (SavedItem savedItem in dct.Tests)
        {
            if (savedItem.GetType() == typeof(ClashTest))
            {
                ClashTests.Add(new CustomClashTest(savedItem as ClashTest));
            }
        }
    }

    private void CheckPlugin()
    {
        if (Application.MainDocument is null
            || Application.MainDocument.IsClear
            || Application.MainDocument.GetClash() is null
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

        if (Application.MainDocument.Grids.ActiveSystem is null)
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

/// <summary>
/// Wraps a <see cref="ClashTest"/> and exposes its display and selection names.
/// </summary>
public class CustomClashTest
{
    private ClashTest _clashTest;

    /// <summary>
    /// Creates a new <see cref="CustomClashTest"/> wrapping the given clash test.
    /// </summary>
    /// <param name="test">The clash test to wrap.</param>
    public CustomClashTest(ClashTest test)
    {
        _clashTest = test;
    }

    /// <summary>
    /// Gets the display name of the wrapped clash test.
    /// </summary>
    public string DisplayName
    {
        get { return _clashTest.DisplayName; }
    }

    /// <summary>
    /// Gets the wrapped clash test.
    /// </summary>
    public ClashTest ClashTest
    {
        get { return _clashTest; }
    }

    /// <summary>
    /// Gets the name of the selection A of the wrapped clash test.
    /// </summary>
    public string SelectionAName
    {
        get { return GetSelectedItem(_clashTest.SelectionA); }
    }

    /// <summary>
    /// Gets the name of the selection B of the wrapped clash test.
    /// </summary>
    public string SelectionBName
    {
        get { return GetSelectedItem(_clashTest.SelectionB); }
    }

    private string GetSelectedItem(ClashSelection selection)
    {
        string result = "";
        if (selection.Selection.HasSelectionSources)
        {
            result = selection.Selection.SelectionSources.FirstOrDefault().ToString();
            if (result.Contains("lcop_selection_set_tree\\"))
            {
                result = result.Replace("lcop_selection_set_tree\\", "");
            }

            if (selection.Selection.SelectionSources.Count > 1)
            {
                result = result + " (and other selection sets)";
            }
        }
        else if (selection.Selection.GetSelectedItems().Count == 0)
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
