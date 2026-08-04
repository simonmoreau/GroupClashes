using System;
using System.Diagnostics;
using Autodesk.Navisworks.Api.Plugins;

namespace GroupClashes;

/// <summary>
/// Ribbon plugin that toggles the visibility of the Group Clashes dock pane.
/// </summary>
[Plugin("GroupClashes", "BM42", DisplayName = "Group Clashes")]
[Strings("GroupClashes.name")]
[RibbonLayout("GroupClashes.xaml")]
[RibbonTab("ID_GroupClashesTab",
    DisplayName = "Group Clashes")]
[Command("ID_GroupClashesButton",
         Icon = "GroupClashesIcon_Small.ico", LargeIcon = "GroupClashesIcon_Large.ico",
         DisplayName = "Group Clashes")]
class RibbonHandler : CommandHandlerPlugin
{
    /// <inheritdoc />
    public override int ExecuteCommand(string commandId, params string[] parameters)
    {
        if (Autodesk.Navisworks.Api.Application.IsAutomated)
        {
            throw new InvalidOperationException("Invalid when running using Automation");
        }

        PluginRecord pluginRecord = Autodesk.Navisworks.Api.Application.Plugins.FindPlugin("GroupClashes.GroupClashesPane.BM42");

        if (pluginRecord is DockPanePluginRecord && pluginRecord.IsEnabled)
        {
            if (pluginRecord.LoadedPlugin is null)
            {
                pluginRecord.LoadPlugin();
            }

            DockPanePlugin dockPanePlugin = pluginRecord.LoadedPlugin as DockPanePlugin;
            if (dockPanePlugin is not null)
            {
                dockPanePlugin.Visible = !dockPanePlugin.Visible;
            }
        }

        return 0;
    }

    /// <inheritdoc />
    public override CommandState CanExecuteCommand(string commandId)
    {
        CommandState state = new CommandState();
        state.IsVisible = true;
        state.IsEnabled = true;
        state.IsChecked = true;

        return state;
    }

    /// <inheritdoc />
    public override bool CanExecuteRibbonTab(string name)
    {
        return true;
    }

    /// <inheritdoc />
    public override bool TryShowCommandHelp(string name)
    {
        string helpUrl = @"https://witty-river-01a861010.2.azurestaticapps.net/GroupClashes/GroupClashes.html";
        Process.Start(helpUrl);
        return true;
    }
}
