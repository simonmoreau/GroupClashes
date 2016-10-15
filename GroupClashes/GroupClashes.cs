using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Navisworks.Api.Clash;
using Autodesk.Navisworks.Api.Plugins;
using Autodesk.Navisworks.Api;
using Autodesk.Windows;

namespace GroupClashes
{
    // AddIn plugin to show/hide the Group Clashes Addin 
    [PluginAttribute("GroupClashes", "BM42", 
        ToolTip = "Groups clashes according to the items involved", 
        DisplayName = "Group Clashes")]
    [AddInPluginAttribute(AddInLocation.AddIn,
        Icon = "GroupClashesIcon_Small.ico",
        LargeIcon = "GroupClashesIcon_Large.ico", 
        LoadForCanExecute = true)]

    class GroupClashes : AddInPlugin
    {
        //private GroupClashesInterface groupClashesInterface;

        public override int Execute(params string[] parameters)
        {
            if (Autodesk.Navisworks.Api.Application.IsAutomated)
            {
                throw new InvalidOperationException("Invalid when running using Automation");
            }

            //Find the plugin
            PluginRecord pr = Application.Plugins.FindPlugin("GroupClashes.GroupClashesPane.BM42");

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

        protected override void OnLoaded()
        {
            //Debug.WriteLine("Loaded");
        }

        public override CommandState CanExecute()
        {
            return new CommandState(true);
        }

        
    }
}


