using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using BusServer.Utility;

namespace BusServer
{
    public partial class Login : Form
    {
        public Login()
        {
            InitializeComponent();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            bool remember = checkBox1.Checked;

            string username = textBox1.Text;
            string password = textBox2.Text;
            int id;

            string query = "select Id from Operator where Username = '" + username +"' and Password = '" + password + "'";
            string connectionString = Properties.Settings.Default.ConnectionString;
            
            DataTable dataTable = DBUtilities.ExecuteQuery(query, connectionString);

            if (dataTable != null)
            {
                if (dataTable.Rows.Count == 1)
                {
                    id = int.Parse(dataTable.Rows[0][0].ToString());
                }
                else if (dataTable.Rows.Count <= 0)
                {
                    MessageBox.Show("Database Error:\n Invalid credential", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                else
                {
                    MessageBox.Show("Database Error:\n Multiple accounts with the same username", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
            }
            else
            {
                MessageBox.Show("Unknown Error", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (remember)
            {
                Properties.Settings.Default.KeptLogin = true;
                Properties.Settings.Default.KeptOperatorID = id;
            }

            Properties.Settings.Default.OperatorID = id;
            Properties.Settings.Default.Save();

            Main main = new Main();
            main.Show();
            Owner = main;
            Hide();
            Enabled = false;
        }
    }
}
