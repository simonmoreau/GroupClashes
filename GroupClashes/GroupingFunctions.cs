using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using Autodesk.Navisworks.Api;
using Autodesk.Navisworks.Api.Clash;

namespace GroupClashes;

/// <summary>
/// Contains all logic for grouping and ungrouping clash results.
/// </summary>
public static class GroupingFunctions
{
    /// <summary>
    /// Groups the clash results of the given clash test according to the specified grouping modes.
    /// </summary>
    /// <param name="selectedClashTest">The clash test to group.</param>
    /// <param name="groupingMode">The first-level grouping mode.</param>
    /// <param name="subgroupingMode">The optional second-level grouping mode.</param>
    /// <param name="keepExistingGroups">Whether to preserve the existing groups alongside the new ones.</param>
    public static void GroupClashes(ClashTest selectedClashTest, GroupingMode groupingMode, GroupingMode subgroupingMode, bool keepExistingGroups)
    {
        List<ClashResult> clashResults = GetIndividualClashResults(selectedClashTest, keepExistingGroups).ToList();
        List<ClashResultGroup> clashResultGroups = new List<ClashResultGroup>();

        CreateGroup(ref clashResultGroups, groupingMode, clashResults, string.Empty);

        if (subgroupingMode != GroupingMode.None)
        {
            CreateSubGroups(ref clashResultGroups, subgroupingMode);
        }

        List<ClashResult> ungroupedClashResults = RemoveOneClashGroup(ref clashResultGroups);

        if (keepExistingGroups)
        {
            clashResultGroups.AddRange(BackupExistingClashGroups(selectedClashTest));
        }

        ProcessClashGroup(clashResultGroups, ungroupedClashResults, selectedClashTest);
    }

    /// <summary>
    /// Flattens all groups of the given clash test back into individual clash results.
    /// </summary>
    /// <param name="selectedClashTest">The clash test to ungroup.</param>
    public static void UnGroupClashes(ClashTest selectedClashTest)
    {
        List<ClashResultGroup> groups = new List<ClashResultGroup>();
        List<ClashResult> results = GetIndividualClashResults(selectedClashTest, false).ToList();
        List<ClashResult> copiedResult = new List<ClashResult>();

        foreach (ClashResult result in results)
        {
            copiedResult.Add((ClashResult)result.CreateCopy());
        }

        ProcessClashGroup(groups, copiedResult, selectedClashTest);
    }

    private static void CreateGroup(ref List<ClashResultGroup> clashResultGroups, GroupingMode groupingMode, List<ClashResult> clashResults, string initialName)
    {
        switch (groupingMode)
        {
            case GroupingMode.None:
                return;
            case GroupingMode.Level:
                clashResultGroups = GroupByLevel(clashResults, initialName);
                break;
            case GroupingMode.GridIntersection:
                clashResultGroups = GroupByGridIntersection(clashResults, initialName);
                break;
            case GroupingMode.SelectionA:
            case GroupingMode.SelectionB:
                clashResultGroups = GroupByElementOfAGivenSelection(clashResults, groupingMode, initialName);
                break;
            case GroupingMode.ModelA:
            case GroupingMode.ModelB:
                clashResultGroups = GroupByElementOfAGivenModel(clashResults, groupingMode, initialName);
                break;
            case GroupingMode.ApprovedBy:
            case GroupingMode.AssignedTo:
            case GroupingMode.Status:
                clashResultGroups = GroupByProperties(clashResults, groupingMode, initialName);
                break;
        }
    }

    private static void CreateSubGroups(ref List<ClashResultGroup> clashResultGroups, GroupingMode mode)
    {
        List<ClashResultGroup> clashResultSubGroups = new List<ClashResultGroup>();

        foreach (ClashResultGroup group in clashResultGroups)
        {
            List<ClashResult> clashResults = new List<ClashResult>();

            foreach (SavedItem item in group.Children)
            {
                ClashResult clashResult = item as ClashResult;
                if (clashResult is not null)
                {
                    clashResults.Add(clashResult);
                }
            }

            List<ClashResultGroup> clashResultTempSubGroups = new List<ClashResultGroup>();
            CreateGroup(ref clashResultTempSubGroups, mode, clashResults, group.DisplayName + "_");
            clashResultSubGroups.AddRange(clashResultTempSubGroups);
        }

        clashResultGroups = clashResultSubGroups;
    }

    private static List<ClashResultGroup> GroupByLevel(List<ClashResult> results, string initialName)
    {
        GridSystem gridSystem = Application.MainDocument.Grids.ActiveSystem;
        Dictionary<GridLevel, ClashResultGroup> groups = new Dictionary<GridLevel, ClashResultGroup>();
        ClashResultGroup currentGroup;

        ClashResultGroup nullGridGroup = new ClashResultGroup();
        nullGridGroup.DisplayName = initialName + "No Level";

        foreach (ClashResult result in results)
        {
            ClashResult copiedResult = (ClashResult)result.CreateCopy();

            if (gridSystem.ClosestIntersection(copiedResult.Center) is not null)
            {
                GridLevel closestLevel = gridSystem.ClosestIntersection(copiedResult.Center).Level;

                if (!groups.TryGetValue(closestLevel, out currentGroup))
                {
                    currentGroup = new ClashResultGroup();
                    string displayName = closestLevel.DisplayName;
                    if (string.IsNullOrEmpty(displayName))
                    {
                        displayName = "Unnamed Level";
                    }
                    currentGroup.DisplayName = initialName + displayName;
                    groups.Add(closestLevel, currentGroup);
                }
                currentGroup.Children.Add(copiedResult);
            }
            else
            {
                nullGridGroup.Children.Add(copiedResult);
            }
        }

        IOrderedEnumerable<KeyValuePair<GridLevel, ClashResultGroup>> list = groups.OrderBy(key => key.Key.Elevation);
        groups = list.ToDictionary((keyItem) => keyItem.Key, (valueItem) => valueItem.Value);

        List<ClashResultGroup> groupsByLevel = groups.Values.ToList();
        if (nullGridGroup.Children.Count != 0)
        {
            groupsByLevel.Add(nullGridGroup);
        }

        return groupsByLevel;
    }

    private static List<ClashResultGroup> GroupByGridIntersection(List<ClashResult> results, string initialName)
    {
        GridSystem gridSystem = Application.MainDocument.Grids.ActiveSystem;
        Dictionary<GridIntersection, ClashResultGroup> groups = new Dictionary<GridIntersection, ClashResultGroup>();
        ClashResultGroup currentGroup;

        ClashResultGroup nullGridGroup = new ClashResultGroup();
        nullGridGroup.DisplayName = initialName + "No Grid intersection";

        foreach (ClashResult result in results)
        {
            ClashResult copiedResult = (ClashResult)result.CreateCopy();

            if (gridSystem.ClosestIntersection(copiedResult.Center) is not null)
            {
                GridIntersection closestGridIntersection = gridSystem.ClosestIntersection(copiedResult.Center);

                if (!groups.TryGetValue(closestGridIntersection, out currentGroup))
                {
                    currentGroup = new ClashResultGroup();
                    string displayName = closestGridIntersection.DisplayName;
                    if (string.IsNullOrEmpty(displayName))
                    {
                        displayName = "Unnamed Grid Intersection";
                    }
                    currentGroup.DisplayName = initialName + displayName;
                    groups.Add(closestGridIntersection, currentGroup);
                }
                currentGroup.Children.Add(copiedResult);
            }
            else
            {
                nullGridGroup.Children.Add(copiedResult);
            }
        }

        IOrderedEnumerable<KeyValuePair<GridIntersection, ClashResultGroup>> list = groups.OrderBy(key => key.Key.Position.X).OrderBy(key => key.Key.Level.Elevation);
        groups = list.ToDictionary((keyItem) => keyItem.Key, (valueItem) => valueItem.Value);

        List<ClashResultGroup> groupsByGridIntersection = groups.Values.ToList();
        if (nullGridGroup.Children.Count != 0)
        {
            groupsByGridIntersection.Add(nullGridGroup);
        }

        return groupsByGridIntersection;
    }

    private static List<ClashResultGroup> GroupByElementOfAGivenSelection(List<ClashResult> results, GroupingMode mode, string initialName)
    {
        Dictionary<ModelItem, ClashResultGroup> groups = new Dictionary<ModelItem, ClashResultGroup>();
        ClashResultGroup currentGroup;
        List<ClashResultGroup> emptyClashResultGroups = new List<ClashResultGroup>();

        foreach (ClashResult result in results)
        {
            ClashResult copiedResult = (ClashResult)result.CreateCopy();
            ModelItem modelItem = null;

            if (mode == GroupingMode.SelectionA)
            {
                if (copiedResult.CompositeItem1 is not null)
                {
                    modelItem = GetSignificantAncestorOrSelf(copiedResult.CompositeItem1);
                }
                else if (copiedResult.CompositeItem2 is not null)
                {
                    modelItem = GetSignificantAncestorOrSelf(copiedResult.CompositeItem2);
                }
            }
            else if (mode == GroupingMode.SelectionB)
            {
                if (copiedResult.CompositeItem2 is not null)
                {
                    modelItem = GetSignificantAncestorOrSelf(copiedResult.CompositeItem2);
                }
                else if (copiedResult.CompositeItem1 is not null)
                {
                    modelItem = GetSignificantAncestorOrSelf(copiedResult.CompositeItem1);
                }
            }

            string displayName = "Empty clash";
            if (modelItem is not null)
            {
                displayName = modelItem.DisplayName;
                if (!groups.TryGetValue(modelItem, out currentGroup))
                {
                    currentGroup = new ClashResultGroup();
                    if (string.IsNullOrEmpty(displayName))
                    {
                        displayName = modelItem.Parent.DisplayName;
                    }
                    if (string.IsNullOrEmpty(displayName))
                    {
                        displayName = "Unnamed Parent";
                    }
                    currentGroup.DisplayName = initialName + displayName;
                    groups.Add(modelItem, currentGroup);
                }

                currentGroup.Children.Add(copiedResult);
            }
            else
            {
                ClashResultGroup oneClashResultGroup = new ClashResultGroup();
                oneClashResultGroup.DisplayName = "Empty clash";
                oneClashResultGroup.Children.Add(copiedResult);
                emptyClashResultGroups.Add(oneClashResultGroup);
            }
        }

        List<ClashResultGroup> allGroups = groups.Values.ToList();
        allGroups.AddRange(emptyClashResultGroups);
        return allGroups;
    }

    private static List<ClashResultGroup> GroupByElementOfAGivenModel(List<ClashResult> results, GroupingMode mode, string initialName)
    {
        Dictionary<ModelItem, ClashResultGroup> groups = new Dictionary<ModelItem, ClashResultGroup>();
        ClashResultGroup currentGroup;
        List<ClashResultGroup> emptyClashResultGroups = new List<ClashResultGroup>();

        foreach (ClashResult result in results)
        {
            ClashResult copiedResult = (ClashResult)result.CreateCopy();
            ModelItem rootModel = null;

            if (mode == GroupingMode.ModelA)
            {
                if (copiedResult.CompositeItem1 is not null)
                {
                    rootModel = GetFileAncestor(copiedResult.CompositeItem1);
                }
                else if (copiedResult.CompositeItem2 is not null)
                {
                    rootModel = GetFileAncestor(copiedResult.CompositeItem2);
                }
            }
            else if (mode == GroupingMode.ModelB)
            {
                if (copiedResult.CompositeItem2 is not null)
                {
                    rootModel = GetFileAncestor(copiedResult.CompositeItem2);
                }
                else if (copiedResult.CompositeItem1 is not null)
                {
                    rootModel = GetFileAncestor(copiedResult.CompositeItem1);
                }
            }

            string displayName = "Empty clash";
            if (rootModel is not null)
            {
                displayName = rootModel.DisplayName;
                if (!groups.TryGetValue(rootModel, out currentGroup))
                {
                    currentGroup = new ClashResultGroup();
                    if (string.IsNullOrEmpty(displayName))
                    {
                        displayName = "Unnamed Model";
                    }
                    currentGroup.DisplayName = initialName + displayName;
                    groups.Add(rootModel, currentGroup);
                }

                currentGroup.Children.Add(copiedResult);
            }
            else
            {
                ClashResultGroup oneClashResultGroup = new ClashResultGroup();
                oneClashResultGroup.DisplayName = "Empty clash";
                oneClashResultGroup.Children.Add(copiedResult);
                emptyClashResultGroups.Add(oneClashResultGroup);
            }
        }

        List<ClashResultGroup> allGroups = groups.Values.ToList();
        allGroups.AddRange(emptyClashResultGroups);
        return allGroups;
    }

    private static List<ClashResultGroup> GroupByProperties(List<ClashResult> results, GroupingMode mode, string initialName)
    {
        Dictionary<string, ClashResultGroup> groups = new Dictionary<string, ClashResultGroup>();
        ClashResultGroup currentGroup;

        foreach (ClashResult result in results)
        {
            ClashResult copiedResult = (ClashResult)result.CreateCopy();
            string clashProperty = null;

            if (mode == GroupingMode.ApprovedBy)
            {
                clashProperty = copiedResult.ApprovedBy.ToString();
            }
            else if (mode == GroupingMode.AssignedTo)
            {
                clashProperty = copiedResult.AssignedTo.ToString();
            }
            else if (mode == GroupingMode.Status)
            {
                clashProperty = copiedResult.Status.ToString();
            }

            if (string.IsNullOrEmpty(clashProperty))
            {
                clashProperty = "Unspecified";
            }

            if (!groups.TryGetValue(clashProperty, out currentGroup))
            {
                currentGroup = new ClashResultGroup();
                currentGroup.DisplayName = initialName + clashProperty;
                groups.Add(clashProperty, currentGroup);
            }
            currentGroup.Children.Add(copiedResult);
        }

        return groups.Values.ToList();
    }

    private static void ProcessClashGroup(List<ClashResultGroup> clashGroups, List<ClashResult> ungroupedClashResults, ClashTest selectedClashTest)
    {
        using (Transaction tx = Application.MainDocument.BeginTransaction("Group clashes"))
        {
            ClashTest copiedClashTest = (ClashTest)selectedClashTest.CreateCopyWithoutChildren();
            GroupItem folder = selectedClashTest.Parent;
            ClashTest backupTest = (ClashTest)selectedClashTest.CreateCopy();
            DocumentClash documentClash = Application.MainDocument.GetClash();
            DocumentClashTests documentClashTestsData = documentClash.TestsData;

#if VERSION2027
                int indexOfClashTest = folder.Children.IndexOf(selectedClashTest);
                documentClashTestsData.TestsReplaceWithCopy(folder, indexOfClashTest, copiedClashTest);
#else
            SavedItemCollection allTests = documentClashTestsData.Tests;
            int indexOfClashTest = allTests.IndexOf(selectedClashTest);
            documentClashTestsData.TestsReplaceWithCopy(indexOfClashTest, copiedClashTest);
#endif

            int currentProgress = 0;
            int totalProgress = ungroupedClashResults.Count + clashGroups.Count;
            Progress progressBar = Application.BeginProgress("Copying Results", "Copying results from " + selectedClashTest.DisplayName + " to the Group Clashes pane...");

            foreach (ClashResultGroup clashResultGroup in clashGroups)
            {
                if (progressBar.IsCanceled)
                {
                    break;
                }

                documentClashTestsData.TestsAddCopy((GroupItem)folder.Children[indexOfClashTest], clashResultGroup);
                currentProgress++;
                progressBar.Update((double)currentProgress / totalProgress);
            }

            foreach (ClashResult clashResult in ungroupedClashResults)
            {
                if (progressBar.IsCanceled)
                {
                    break;
                }
                documentClash.TestsData.TestsAddCopy((GroupItem)folder.Children[indexOfClashTest], clashResult);
                currentProgress++;
                progressBar.Update((double)currentProgress / totalProgress);
            }
            if (progressBar.IsCanceled)
            {
                documentClashTestsData.TestsReplaceWithCopy(copiedClashTest, indexOfClashTest, backupTest);
            }
            tx.Commit();
            Application.EndProgress();
        }

    }

    private static List<ClashResult> RemoveOneClashGroup(ref List<ClashResultGroup> clashResultGroups)
    {
        List<ClashResult> ungroupedClashResults = new List<ClashResult>();
        List<ClashResultGroup> temporaryClashResultGroups = new List<ClashResultGroup>();
        temporaryClashResultGroups.AddRange(clashResultGroups);

        foreach (ClashResultGroup group in temporaryClashResultGroups)
        {
            if (group.Children.Count == 1)
            {
                ClashResult result = (ClashResult)group.Children.FirstOrDefault();
                ungroupedClashResults.Add(result);
                clashResultGroups.Remove(group);
            }
        }

        return ungroupedClashResults;
    }

    private static IEnumerable<ClashResult> GetIndividualClashResults(ClashTest clashTest, bool keepExistingGroup)
    {
        for (int i = 0; i < clashTest.Children.Count; i++)
        {
            if (clashTest.Children[i].IsGroup)
            {
                if (!keepExistingGroup)
                {
                    IEnumerable<ClashResult> groupResults = GetGroupResults((ClashResultGroup)clashTest.Children[i]);
                    foreach (ClashResult clashResult in groupResults)
                    {
                        yield return clashResult;
                    }
                }
            }
            else
            {
                yield return (ClashResult)clashTest.Children[i];
            }
        }
    }

    private static IEnumerable<ClashResultGroup> BackupExistingClashGroups(ClashTest clashTest)
    {
        for (int i = 0; i < clashTest.Children.Count; i++)
        {
            if (clashTest.Children[i].IsGroup)
            {
                yield return (ClashResultGroup)clashTest.Children[i].CreateCopy();
            }
        }
    }

    private static IEnumerable<ClashResult> GetGroupResults(ClashResultGroup clashResultGroup)
    {
        for (int i = 0; i < clashResultGroup.Children.Count; i++)
        {
            yield return (ClashResult)clashResultGroup.Children[i];
        }
    }

    private static ModelItem GetSignificantAncestorOrSelf(ModelItem item)
    {
        ModelItem originalItem = item;
        ModelItem currentComposite = null;

        while (item.Parent is not null)
        {
            item = item.Parent;
            if (item.IsComposite)
            {
                currentComposite = item;
            }
        }

        return currentComposite ?? originalItem;
    }

    private static ModelItem GetFileAncestor(ModelItem item)
    {
        ModelItem originalItem = item;
        ModelItem currentComposite = null;

        while (item.Parent is not null)
        {
            item = item.Parent;
            if (item.HasModel)
            {
                currentComposite = item;
                break;
            }
        }

        return currentComposite ?? originalItem;
    }
}

/// <summary>
/// Defines the available clash grouping modes.
/// </summary>
public enum GroupingMode
{
    /// <summary>
    /// No grouping is applied.
    /// </summary>
    [Description("<None>")]
    None,

    /// <summary>
    /// Groups clashes by the nearest grid level.
    /// </summary>
    [Description("Level")]
    Level,

    /// <summary>
    /// Groups clashes by the nearest grid intersection.
    /// </summary>
    [Description("Grid Intersection")]
    GridIntersection,

    /// <summary>
    /// Groups clashes by the element involved from selection A.
    /// </summary>
    [Description("Selection A")]
    SelectionA,

    /// <summary>
    /// Groups clashes by the element involved from selection B.
    /// </summary>
    [Description("Selection B")]
    SelectionB,

    /// <summary>
    /// Groups clashes by the model involved from selection A.
    /// </summary>
    [Description("Model A")]
    ModelA,

    /// <summary>
    /// Groups clashes by the model involved from selection B.
    /// </summary>
    [Description("Model B")]
    ModelB,

    /// <summary>
    /// Groups clashes by the assigned-to user.
    /// </summary>
    [Description("Assigned To")]
    AssignedTo,

    /// <summary>
    /// Groups clashes by the approved-by user.
    /// </summary>
    [Description("Approved By")]
    ApprovedBy,

    /// <summary>
    /// Groups clashes by their status.
    /// </summary>
    [Description("Status")]
    Status
}
