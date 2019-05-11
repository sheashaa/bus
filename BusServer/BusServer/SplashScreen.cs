using System.Windows.Forms;

namespace BusServer
{
    public partial class SplashScreen : Form
    {
        private long time = 0;
        private bool IsSaved;

        public SplashScreen()
        {
            InitializeComponent();
        }

        private void SplashScreenTimer_Tick(object sender, System.EventArgs e)
        {
            if (++time > 3)
            {
                if (IsSaved)
                {
                    Main main = new Main();
                    main.Show();
                    Owner = main;
                }
                else
                {
                    Login login = new Login();
                    login.Show();
                    Owner = login;
                }
                SplashScreenTimer.Enabled = false;
                Enabled = false;
                Hide();
            }
        }

        private void SplashScreen_Load(object sender, System.EventArgs e)
        {
            string dbPath = Application.StartupPath + "\\Data\\Data.mdf";
            string connectionString = "Data Source=(LocalDB)\\MSSQLLocalDB;AttachDbFilename=" + dbPath +";Integrated Security=True";

            Properties.Settings.Default.ConnectionString = connectionString;
            Properties.Settings.Default.Save();

            IsSaved = Properties.Settings.Default.KeptLogin;

            if (IsSaved && Properties.Settings.Default.KeptOperatorID != -1)
            {
                Properties.Settings.Default.OperatorID = Properties.Settings.Default.KeptOperatorID;
                Properties.Settings.Default.Save();
            }

            SplashScreenTimer.Enabled = true;
        }
    }
}
