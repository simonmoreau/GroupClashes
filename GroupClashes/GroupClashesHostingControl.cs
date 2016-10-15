using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows;
using System.Windows.Forms.Integration;
using System.Windows.Media;
using System.Windows.Interop;

namespace GroupClashes
{


    public partial class GroupClashesHostingControl : UserControl
    {
        private ElementHost _ctrlHost;
        private GroupClashesInterface _wpfAddressCtrl;
        private Panel _hostPanel;

        public GroupClashesHostingControl()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            _ctrlHost = new ElementHost();
            this.Controls.Add(_ctrlHost);
            _ctrlHost.Dock = DockStyle.Fill;

            _wpfAddressCtrl = new GroupClashesInterface();
            _wpfAddressCtrl.InitializeComponent();
            _ctrlHost.Child = _wpfAddressCtrl;
            _ctrlHost.AutoSize = true;
        }

        protected override void OnVisibleChanged(EventArgs e)
        {
            base.OnVisibleChanged(e);

            _hostPanel = (Panel)Parent;
            _hostPanel.SizeChanged += (hostPanel_SizeChanged);
            ResizeControl();
        }

        private void hostPanel_SizeChanged(object sender, EventArgs e)
        {
            ResizeControl();
        }

        public void ResizeControl()
        {
            Width = _hostPanel.Width;
            Height = _hostPanel.Height;
        }

    }
}
