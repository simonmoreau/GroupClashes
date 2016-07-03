using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Navisworks.Api.Clash;
using Autodesk.Navisworks.Api;

namespace GroupClashes
{
    class GroupingFunctions
    {
        public static void GroupClashes(ClashTest selectedClashTest)
        {
            //Get existing clash result
            IEnumerable<ClashResult> clashResults = GetIndividualClashResults(selectedClashTest);

            //group all clashes
            //List<ClashResultGroup> clashResultGroups = GroupByLevel(clashResults.ToList());
            List<ClashResultGroup> clashResultGroups = GroupByGridIntersection(clashResults.ToList());

            //Remove groups with only one clash
            List<ClashResult> ungroupedClashResults = RemoveOneClashGroup(ref clashResultGroups);

            //Process these groups and clashes into the clash test
            ProcessClashGroup(clashResultGroups, ungroupedClashResults, selectedClashTest);
        }

        #region grouping functions
        private static List<ClashResultGroup> GroupByLevel(List<ClashResult> results)
        {
            //TODO Check if the grid system exist
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
            //TODO Check if the grid system exist
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

        private static List<ClashResultGroup> GroupByElementOfAGivenSelection(List<ClashResult> results)
        {
            Dictionary<GridIntersection, ClashResultGroup> groups = new Dictionary<GridIntersection, ClashResultGroup>();
            ClashResultGroup currentGroup;

            //foreach (ClashResult result in results)
            //{
            //    //Cannot add original result to new clash test, so I create a copy
            //    ClashResult copiedResult = (ClashResult)result.CreateCopy();
                
            //    GridIntersection closestIntersection = gridSystem.ClosestIntersection(copiedResult.Center);

            //    if (!groups.TryGetValue(closestIntersection, out currentGroup))
            //    {
            //        currentGroup = new ClashResultGroup();
            //        currentGroup.DisplayName = closestIntersection.DisplayName;
            //        groups.Add(closestIntersection, currentGroup);
            //    }
            //    currentGroup.Children.Add(copiedResult);
            //}

            return groups.Values.ToList();
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
        #endregion

    }
}
