using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Navisworks.Api.Plugins;

namespace GroupClashes
{
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
        public RibbonHandler()
        {
            m_toShowTab = false; // to show tab or not
            m_toEnableButton = false; // to enable button or not
        }

        public override int ExecuteCommand(string commandId, params string[] parameters)
        {
            if (Autodesk.Navisworks.Api.Application.IsAutomated)
            {
                throw new InvalidOperationException("Invalid when running using Automation");
            }

            //Find the plugin
            PluginRecord pr = Autodesk.Navisworks.Api.Application.Plugins.FindPlugin("GroupClashes.GroupClashesPane.BM42");

            if (pr != null && pr is DockPanePluginRecord && pr.IsEnabled)
            {
                //check if it needs loading
                if (pr.LoadedPlugin == null)
                {
                    pr.LoadPlugin();
                }

                DockPanePlugin dpp = pr.LoadedPlugin as DockPanePlugin;
                if (dpp != null)
                {
                    //switch the Visible flag
                    dpp.Visible = !dpp.Visible;
                }
            }

            //groupClashesInterface.ShowDialog();
            return 0;
        }

        public override CommandState CanExecuteCommand(String commandId)
        {
            CommandState state = new CommandState();
            state.IsVisible = true;
            state.IsEnabled = true;
            state.IsChecked = true;

            return state;
        }

        public override bool CanExecuteRibbonTab(string name)
        {
            return true;
        }

        public override bool TryShowCommandHelp(string name)
        {
            FileInfo dllFileInfo = new FileInfo(Assembly.GetExecutingAssembly().Location);
            string pathToHtmlFile = Path.Combine(dllFileInfo.Directory.FullName, @"Help\Help.html");
            System.Diagnostics.Process.Start(pathToHtmlFile);
            return true;
        }

        private bool m_toShowTab;
        private bool m_toEnableButton;
    }
}


