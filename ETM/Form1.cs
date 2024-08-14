using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Dynamic;
using System.Linq;
using System.Management;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ETM
{
    public partial class Form1 : Form
    {

        private bool resizeFlag = false;

        public Form1()
        {
            InitializeComponent();
            this.processView.SizeChanged += new EventHandler(ListView_SizeChange);
        }

        private void Form1_Load(object sender, EventArgs e)
        {

            ShowAllProcesses();
        }

        public ExpandoObject GetMoreInfo(int pid)
        {
            //create dynamic object
            dynamic process = new ExpandoObject();
            process.Description = "";
            process.Username = "SYSTEM";

            //get a list of processes from Win32_Process
            string query = String.Format("Select * From Win32_Process Where ProcessID = {0}",pid);
            ManagementObjectSearcher queryEngine = new ManagementObjectSearcher(query);
            ManagementObjectCollection processList = queryEngine.Get();

            foreach (ManagementObject processObject in processList)
            {
                string[] propertyList = new string[] {string.Empty, string.Empty};
                int responseValue = Convert.ToInt32(processObject.InvokeMethod("GetOwner", propertyList));
                if (responseValue == 0)
                {
                    process.Username = propertyList[0];
                }

                if (processObject["ExecutablePath"] != null)
                {
                    try
                    {
                        FileVersionInfo info = FileVersionInfo.GetVersionInfo(processObject["ExecutablePath"].ToString());
                    }
                    catch
                    {
                    }
                }
            }
            return process;
        }

        public string BytesToFormatValue(long originalValue)
        {
            List<string> suffixList = new List<string> {" B", " KB", " MB", " GB", " TB", " PB"};

            for (int x = 0; x < suffixList.Count; x++)
            {
                long tempValue = (originalValue / ((int)Math.Pow(1024, (x + 1))));
                if (tempValue == 0)
                {
                    return (originalValue / ((int)Math.Pow(1024, x))) + suffixList[x];
                }
            }
            return originalValue.ToString();
        }

        public void ShowAllProcesses()
        {
            Process[] runningProcesses = Process.GetProcesses();
            int processCount = 0;
            ImageList processIcons = new ImageList();

            foreach (Process process in runningProcesses)
            {
                string processStatus = (process.Responding == true ? "Responding" : "Not Responding" );

                //get process extra information
                dynamic processExtraInfo = GetMoreInfo(process.Id);

                //create data source to bind to listview
                string[] newProcessRow = {
                    process.Id.ToString(),
                    process.ProcessName,
                    processExtraInfo.Description,
                    processStatus,
                    processExtraInfo.Username,
                    BytesToFormatValue(process.PrivateMemorySize64),
                    
                };

                //get process icon, if any
                try
                {
                    processIcons.Images.Add(
                        process.Id.ToString(),
                        Icon.ExtractAssociatedIcon(process.MainModule.FileName).ToBitmap()
                    );
                }
                catch
                {
                }

                ListViewItem displayRow = new ListViewItem(newProcessRow)
                {
                    ImageIndex = processIcons.Images.IndexOfKey(process.Id.ToString())
                };

                processCount++;
                processView.Items.Add(displayRow);
                totalProcesses.Text = string.Format("{0} currently running.", processCount);
            }

            processView.LargeImageList = processIcons;
            processView.SmallImageList = processIcons;
        }

        private void ClearProcessList()
        {
            processView.Items.Clear();
            totalProcesses.Text = string.Format("{0} currently running.", 0);
        }

        private void ListView_SizeChange(object sender, EventArgs e)
        {
            //handles overlapping calls to SizeChange
            if (!resizeFlag)
            {
                resizeFlag = true;

                ListView proView = sender as ListView;
                if (proView != null)
                {
                    float totalTagSum = 0;
                    for (int x = 0; x < proView.Columns.Count; x++)
                        totalTagSum += Convert.ToInt32(proView.Columns[x].Tag);

                    //calculate width of each column
                    for (int x = 0; x < proView.Columns.Count; x++)
                    {
                        float colWidthPercent = (Convert.ToInt32(proView.Columns[x].Tag) / totalTagSum);
                        proView.Columns[x].Width = (int)(colWidthPercent * proView.ClientRectangle.Width);
                    }
                }
            }

            resizeFlag = false;
        }

        private void StartProcess_Click(object sender, EventArgs e)
        {
            //make sure that user input has correct format
            if (task2Run.Text.ToUpper().Contains(".EXE") == true)
            {
                try
                {
                    Process process = new Process();
                    process.StartInfo.FileName = task2Run.Text;
                    process.Start();
                    ClearProcessList();
                    MessageBox.Show("Process has been been initiated. Reloading list...");
                    ShowAllProcesses();
                }
                catch (Exception error)
                {
                    MessageBox.Show(string.Format("ERROR! '{0}' is not a valid process.", task2Run.Text));
                }
            }
            else
            {
                MessageBox.Show(string.Format("ERROR! '{0}' is not an executable process. Please make sure to enter a process name with the extension '.exe'.", task2Run.Text));
            }
        }

        private void KillProcess_Click(object sender, EventArgs e)
        {
            ListViewItem current = processView.SelectedItems[0];
            Process process = Process.GetProcessById(int.Parse(current.Text.ToString()));
            process.Kill();
            ClearProcessList();
            MessageBox.Show("Process has been killed successfully. Reloading list...");
            ShowAllProcesses();
        }
    }
}
