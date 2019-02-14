using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace information_Client
{
    public partial class Progressbar_Form : Form
    {
        public Progressbar_Form()
        {
            InitializeComponent();
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            if (this.progressBar1.Value < this.progressBar1.Maximum)
            {
                this.progressBar1.Value = 1000;
            }
            else if (this.progressBar1.Value == this.progressBar1.Maximum)
            {
                this.Close();
            }
        }

        private void Progressbar_Form_Load(object sender, EventArgs e)
        {
            timer1.Start();
        }
    }
}
