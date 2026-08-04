using System.Linq;
using Autodesk.Navisworks.Api;
using Autodesk.Navisworks.Api.Clash;

namespace GroupClashes;

/// <summary>
/// Wraps a <see cref="ClashTest"/> and exposes its display and selection names.
/// </summary>
public class DisplayedClashTest
{
    private ClashTest _clashTest;

    /// <summary>
    /// Creates a new <see cref="DisplayedClashTest"/> wrapping the given clash test.
    /// </summary>
    /// <param name="test">The clash test to wrap.</param>
    public DisplayedClashTest(ClashTest test)
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
