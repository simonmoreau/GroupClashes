using System;
using System.Collections.Generic;
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
    public ObservableCollection<DisplayedClashTest> ClashTests { get; set; }

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

        ClashTests = new ObservableCollection<DisplayedClashTest>();
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
                DisplayedClashTest selectedClashTest = (DisplayedClashTest)selectedItem;
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
                DisplayedClashTest selectedClashTest = (DisplayedClashTest)selectedItem;
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
        DocumentClashTests documentClashTests = Application.MainDocument.GetClash().TestsData;
        ClashTests.Clear();

        List<ClashTest> clashTests = GetClashTests(documentClashTests);

        foreach (ClashTest clashTest in clashTests)
        {
            if (clashTest != null)
            {
                ClashTests.Add(new DisplayedClashTest(clashTest));
            }
        }
    }

    private List<ClashTest?> GetClashTests(DocumentClashTests allTests)
    {
        List<SavedItem?> savedItems = new List<SavedItem?>();
#if VERSION2027
        savedItems = GetClashTestsRecursive(allTests.Value.TestsRoot).Cast<SavedItem?>().ToList();
#else
        savedItems = allTests.Tests.ToList();
#endif
        List<ClashTest?> clashTests = new List<ClashTest?>();
        foreach (SavedItem? savedItem in savedItems)
        {
            ClashTest? clashTest = (ClashTest?)savedItem;
            clashTests.Add(clashTest);
        }
        return clashTests;
    }

    private void CheckPlugin()
    {
#if VERSION2027
        if (Application.MainDocument is null
            || Application.MainDocument.IsClear
            || Application.MainDocument.GetClash() is null
            || Application.MainDocument.GetClash().TestsData.Value.TestsRoot.Children.Count == 0)
#else
        if (Application.MainDocument is null
            || Application.MainDocument.IsClear
            || Application.MainDocument.GetClash() is null
            || Application.MainDocument.GetClash().TestsData.Tests.Count == 0)
#endif
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

    private List<ClashTest> GetClashTestsRecursive(SavedItem root)
    {

        List<ClashTest> result = new List<ClashTest>();

        if (root is ClashTestFolder folder)
        {
            foreach (SavedItem child in folder.Children)
            {
                if (child is ClashTest clashTest)
                {
                    result.Add(clashTest);
                }
                else if (child is FolderItem subFolder)
                {
                    result.AddRange(GetClashTestsRecursive(subFolder));
                }
            }
        }
        else if (root is FolderItem folderItem)
        {
            foreach (SavedItem child in folderItem.Children)
            {
                if (child is ClashTest clashTest)
                {
                    result.Add(clashTest);
                }
                else if (child is FolderItem subFolder)
                {
                    result.AddRange(GetClashTestsRecursive(subFolder));
                }
            }
        }
        else if (root is ClashTest clashTest)
        {
            result.Add(clashTest);
        }

        return result;
    }
}
