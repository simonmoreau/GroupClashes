using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Autodesk.Navisworks.Api.Plugins;

namespace GroupClashes
{
    [Plugin("GroupClashes.GroupClashesPane", "BM42",DisplayName = "Group Clashes",ToolTip = "Group clashes")]
    [DockPanePlugin(300, 380)]
    class GroupClashesPane : DockPanePlugin
    {
        public override Control CreateControlPane()
        {
            //create the control that will be used to display in the pane
            GroupClashesHostingControl control = new GroupClashesHostingControl();

            control.Dock = DockStyle.Fill;

            //create the control
            control.CreateControl();

            return control;
        }

        public override void DestroyControlPane(Control pane)
        {
            pane.Dispose();
        }
    }
}
