using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace AISCast
{
    public partial class AddDialog : Form
    {
        public AddDialog()
        {
            InitializeComponent();
            this.CenterToParent();
        }

        public string DeviceName
        {
            get { return tbName.Text; }
        }

        public string DeviceId
        {
            get { return tbId.Text; }
        }

        private void btnOk_Click(object sender, EventArgs e)
        {
            Close();
        }
    }
}
