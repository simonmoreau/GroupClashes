using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Navisworks.Api.Clash;
using Autodesk.Navisworks.Api;
using System.ComponentModel;

namespace GroupClashes
{
    class GroupingFunctions
    {
        public static void GroupClashes(ClashTest selectedClashTest, GroupingMode groupingMode, GroupingMode renamingMode)
        {
            //Get existing clash result
            IEnumerable<ClashResult> clashResults = GetIndividualClashResults(selectedClashTest);
            List<ClashResultGroup> clashResultGroups = new List<ClashResultGroup>();

            //group all clashes
            switch (groupingMode)
            {
                case GroupingMode.None:
                    return;
                case GroupingMode.Level:
                    clashResultGroups = GroupByLevel(clashResults.ToList());
                    break;
                case GroupingMode.GridIntersection:
                    clashResultGroups = GroupByGridIntersection(clashResults.ToList());
                    break;
                case GroupingMode.SelectionA:
                case GroupingMode.SelectionB:
                    clashResultGroups = GroupByElementOfAGivenSelection(clashResults.ToList(),groupingMode);
                    break;
                case GroupingMode.ApprovedBy:
                case GroupingMode.AssignedTo:
                case GroupingMode.Status:
                    clashResultGroups = GroupByProperties(clashResults.ToList(), groupingMode);
                    break;
            }

            //Optionnaly, rename clash groups
            if (renamingMode != GroupingMode.None)
            {
                RemaneGroupBySortingMode(ref clashResultGroups, renamingMode);
            }

            //Remove groups with only one clash
            List<ClashResult> ungroupedClashResults = RemoveOneClashGroup(ref clashResultGroups);

            //Process these groups and clashes into the clash test
            ProcessClashGroup(clashResultGroups, ungroupedClashResults, selectedClashTest);
        }

        public static void UnGroupClashes(ClashTest selectedClashTest)
        {
            List<ClashResultGroup> groups = new List<ClashResultGroup>();
            List<ClashResult> results = GetIndividualClashResults(selectedClashTest).ToList();
            List<ClashResult> copiedResult = new List<ClashResult>();

            foreach (ClashResult result in results)
            {
                copiedResult.Add((ClashResult)result.CreateCopy());
            }

            //Process this empty group list and clashes into the clash test
            ProcessClashGroup(groups, copiedResult, selectedClashTest);

        }

        #region grouping functions
        private static List<ClashResultGroup> GroupByLevel(List<ClashResult> results)
        {
            //I already check if it exists
            GridSystem gridSystem = Application.MainDocument.Grids.ActiveSystem;
            Dictionary<GridLevel, ClashResultGroup> groups = new Dictionary<GridLevel, ClashResultGroup>();
            ClashResultGroup currentGroup;

            foreach (ClashResult result in results)
            {
                //Cannot add original result to new clash test, so I create a copy
                ClashResult copiedResult = (ClashResult)result.CreateCopy();
                GridLevel closestLevel = gridSystem.ClosestIntersection(copiedResult.Center).Level;

                if (!groups.TryGetValue(closestLevel, out currentGroup))
                {
                    currentGroup = new ClashResultGroup();
                    currentGroup.DisplayName = closestLevel.DisplayName;
                    groups.Add(closestLevel, currentGroup);
                }
                currentGroup.Children.Add(copiedResult);
            }

            IOrderedEnumerable<KeyValuePair<GridLevel,ClashResultGroup>> list = groups.OrderBy(key => key.Key.Elevation);
            groups = list.ToDictionary((keyItem) => keyItem.Key, (valueItem) => valueItem.Value);
            return groups.Values.ToList();
        }

        private static List<ClashResultGroup> GroupByGridIntersection(List<ClashResult> results)
        {
            //I already check if it exists
            GridSystem gridSystem = Application.MainDocument.Grids.ActiveSystem;
            Dictionary<GridIntersection, ClashResultGroup> groups = new Dictionary<GridIntersection, ClashResultGroup>();
            ClashResultGroup currentGroup;

            foreach (ClashResult result in results)
            {
                //Cannot add original result to new clash test, so I create a copy
                ClashResult copiedResult = (ClashResult)result.CreateCopy();
                GridIntersection closestIntersection = gridSystem.ClosestIntersection(copiedResult.Center);

                if (!groups.TryGetValue(closestIntersection, out currentGroup))
                {
                    currentGroup = new ClashResultGroup();
                    currentGroup.DisplayName = closestIntersection.DisplayName;
                    groups.Add(closestIntersection, currentGroup);
                }
                currentGroup.Children.Add(copiedResult);
            }

            IOrderedEnumerable<KeyValuePair<GridIntersection, ClashResultGroup>> list = groups.OrderBy(key => key.Key.Position.X).OrderBy(key => key.Key.Level.Elevation);
            groups = list.ToDictionary((keyItem) => keyItem.Key, (valueItem) => valueItem.Value);
            return groups.Values.ToList();
        }

        private static List<ClashResultGroup> GroupByElementOfAGivenSelection(List<ClashResult> results,GroupingMode mode)
        {
            Dictionary<ModelItem, ClashResultGroup> groups = new Dictionary<ModelItem, ClashResultGroup>();
            ClashResultGroup currentGroup;

            foreach (ClashResult result in results)
            {
                //Cannot add original result to new clash test, so I create a copy
                ClashResult copiedResult = (ClashResult)result.CreateCopy();
                ModelItem modelItem = null;

                if (mode == GroupingMode.SelectionA)
                {
                    modelItem = GetSignificantAncestorOrSelf(copiedResult.CompositeItem1);
                }
                else if (mode == GroupingMode.SelectionB)
                {
                    modelItem = GetSignificantAncestorOrSelf(copiedResult.CompositeItem2);
                }

                if (!groups.TryGetValue(modelItem, out currentGroup))
                {
                    currentGroup = new ClashResultGroup();
                    currentGroup.DisplayName = modelItem.DisplayName;
                    groups.Add(modelItem, currentGroup);
                }
                currentGroup.Children.Add(copiedResult);
            }

            return groups.Values.ToList();
        }

        private static List<ClashResultGroup> GroupByProperties(List<ClashResult> results, GroupingMode mode)
        {
            Dictionary<string, ClashResultGroup> groups = new Dictionary<string, ClashResultGroup>();
            ClashResultGroup currentGroup;

            foreach (ClashResult result in results)
            {
                //Cannot add original result to new clash test, so I create a copy
                ClashResult copiedResult = (ClashResult)result.CreateCopy();
                string clashProperty = null;

                if (mode == GroupingMode.ApprovedBy)
                {
                    clashProperty = copiedResult.ApprovedBy;
                }
                else if (mode == GroupingMode.AssignedTo)
                {
                    clashProperty = copiedResult.AssignedTo;
                }
                else if (mode == GroupingMode.Status)
                {
                    clashProperty = copiedResult.Status.ToString();
                }

                if (string.IsNullOrEmpty(clashProperty)) { clashProperty = "Unset"; }

                if (!groups.TryGetValue(clashProperty, out currentGroup))
                {
                    currentGroup = new ClashResultGroup();
                    currentGroup.DisplayName = clashProperty;
                    groups.Add(clashProperty, currentGroup);
                }
                currentGroup.Children.Add(copiedResult);
            }

            return groups.Values.ToList();
        }

        private static void RemaneGroupBySortingMode(ref List<ClashResultGroup> clashResultGroups, GroupingMode mode)
        {
            GridSystem gridSystem = Application.MainDocument.Grids.ActiveSystem;

            foreach (ClashResultGroup clashResultGroup in clashResultGroups)
            {
                switch (mode)
                {
                    case GroupingMode.None:
                    case GroupingMode.SelectionA:
                    case GroupingMode.SelectionB:
                        return;
                    case GroupingMode.Level:
                        GridLevel closestLevel = gridSystem.ClosestIntersection(clashResultGroup.Center).Level;
                        clashResultGroup.DisplayName = closestLevel.DisplayName + "_" + clashResultGroup.DisplayName;
                        break;
                    case GroupingMode.GridIntersection:
                        GridIntersection closestIntersection = gridSystem.ClosestIntersection(clashResultGroup.Center);
                        clashResultGroup.DisplayName = closestIntersection.DisplayName + "_" + clashResultGroup.DisplayName;
                        break;
                    case GroupingMode.ApprovedBy:
                        string approvedByValue = "N/A";
                        if (!String.IsNullOrEmpty(clashResultGroup.ApprovedBy))
                        {
                            approvedByValue = clashResultGroup.ApprovedBy;
                        }
                        clashResultGroup.DisplayName = approvedByValue + "_" + clashResultGroup.DisplayName;
                        break;
                    case GroupingMode.AssignedTo:
                        string assignedToValue = "N/A";
                        if (!String.IsNullOrEmpty(clashResultGroup.AssignedTo))
                        {
                            assignedToValue = clashResultGroup.ApprovedBy;
                        }
                        clashResultGroup.DisplayName = assignedToValue + "_" + clashResultGroup.DisplayName;
                        break;
                    case GroupingMode.Status:
                        clashResultGroup.DisplayName = clashResultGroup.Status.ToString() + "_" + clashResultGroup.DisplayName;
                        break;
                }
            }
        }

        

        #endregion


        #region helpers
        private static void ProcessClashGroup(List<ClashResultGroup> clashGroups, List<ClashResult> ungroupedClashResults, ClashTest selectedClashTest)
        {
            using (Transaction tx = Application.MainDocument.BeginTransaction("Group clashes"))
            {
                ClashTest copiedClashTest = (ClashTest)selectedClashTest.CreateCopyWithoutChildren();
                //When we replace theTest with our new test, theTest will be disposed. If the operation is cancelled, we need a non-disposed copy of theTest with children to sub back in.
                ClashTest BackupTest = (ClashTest)selectedClashTest.CreateCopy();
                DocumentClash documentClash = Application.MainDocument.GetClash();
                int indexOfClashTest = documentClash.TestsData.Tests.IndexOf(selectedClashTest);
                documentClash.TestsData.TestsReplaceWithCopy(indexOfClashTest, copiedClashTest);

                int CurrentProgress = 0;
                int TotalProgress = ungroupedClashResults.Count + clashGroups.Count;
                Progress ProgressBar = Application.BeginProgress("Copying Results", "Copying results from " + selectedClashTest.DisplayName + " to the Group Clashes pane...");
                foreach (ClashResultGroup clashResultGroup in clashGroups)
                {
                    if (ProgressBar.IsCanceled) break;
                    documentClash.TestsData.TestsAddCopy((GroupItem)documentClash.TestsData.Tests[indexOfClashTest], clashResultGroup);
                    CurrentProgress++;
                    ProgressBar.Update((double)CurrentProgress / TotalProgress);
                }
                foreach (ClashResult clashResult in ungroupedClashResults)
                {
                    if (ProgressBar.IsCanceled) break;
                    documentClash.TestsData.TestsAddCopy((GroupItem)documentClash.TestsData.Tests[indexOfClashTest], clashResult);
                    CurrentProgress++;
                    ProgressBar.Update((double)CurrentProgress / TotalProgress);
                }
                if (ProgressBar.IsCanceled) documentClash.TestsData.TestsReplaceWithCopy(indexOfClashTest, BackupTest);
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
                    result.DisplayName = group.DisplayName;
                    ungroupedClashResults.Add(result);
                    clashResultGroups.Remove(group);
                }
            }

            return ungroupedClashResults;
        }

        private static IEnumerable<ClashResult> GetIndividualClashResults(ClashTest clashTest)
        {
            for (var i = 0; i < clashTest.Children.Count; i++)
            {
                if (clashTest.Children[i].IsGroup)
                {
                    IEnumerable<ClashResult> GroupResults = GetGroupResults((ClashResultGroup)clashTest.Children[i]);
                    foreach (ClashResult clashResult in GroupResults)
                    {
                        yield return clashResult;
                    }
                }
                else yield return (ClashResult)clashTest.Children[i];
            }
        }

        private static IEnumerable<ClashResult> GetGroupResults(ClashResultGroup clashResultGroup)
        {
            for (var i = 0; i < clashResultGroup.Children.Count; i++)
            {
                yield return (ClashResult)clashResultGroup.Children[i];
            }
        }

        private static ModelItem GetSignificantAncestorOrSelf(ModelItem Item)
        {
            ModelItem OriginalItem = Item;
            ModelItem CurrentComposite = null;

            //Get last composite item.
            while (Item.Parent != null)
            {
                Item = Item.Parent;
                if (Item.IsComposite) CurrentComposite = Item;
            }
            return CurrentComposite ?? OriginalItem;
        }
        #endregion

    }

    public enum GroupingMode
    {
        [Description("<None>")]
        None,
        [Description("Level")]
        Level,
        [Description("Grid Intersection")]
        GridIntersection,
        [Description("Selection A")]
        SelectionA,
        [Description("Selection B")]
        SelectionB,
        [Description("Assigned To")]
        AssignedTo,
        [Description("Approved By")]
        ApprovedBy,
        [Description("Status")]
        Status
    }
}
