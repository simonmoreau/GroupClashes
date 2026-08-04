using System.Windows.Forms;
using Autodesk.Navisworks.Api.Plugins;

namespace GroupClashes;

/// <summary>
/// Dock pane plugin that hosts the Group Clashes interface.
/// </summary>
[Plugin("GroupClashes.GroupClashesPane", "BM42", DisplayName = "Group Clashes", ToolTip = "Group clashes")]
[DockPanePlugin(300, 380)]
class GroupClashesPane : DockPanePlugin
{
    /// <inheritdoc />
    public override Control CreateControlPane()
    {
        GroupClashesHostingControl control = new GroupClashesHostingControl();

        control.Dock = DockStyle.Fill;

        control.CreateControl();

        return control;
    }

    /// <inheritdoc />
    public override void DestroyControlPane(Control pane)
    {
        pane.Dispose();
    }
}
