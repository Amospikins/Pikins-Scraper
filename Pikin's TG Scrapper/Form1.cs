using TL;
using ComponentFactory.Krypton.Toolkit;
using System.Text;
using System.Data;

namespace Pikin_s_TG_Scrapper
{
    public partial class Form1 : KryptonForm
    {
        private readonly ManualResetEventSlim _codeReady = new();
        private WTelegram.Client? _client;
        private User? _user;
        string? Config(string what, string pphon)
        {

            switch (what)
            {
                case "api_id": return txtAppId.Text;
                case "api_hash": return txtAppHash.Text;
                case "phone_number": return pphon;
                case "session_pathname": return "sessions/" + pphon + ".session";
                case "verification_code":
                case "password":
                    BeginInvoke(new Action(() => CodeNeeded(what.Replace('_', ' '))));
                    _codeReady.Reset();
                    _codeReady.Wait();
                    return textBoxCode.Text;
                default: return null;
            };

        }

        private void CodeNeeded(string what)
        {

            labelCode.Text = what + ':';
            textBoxCode.Text = "";
            labelCode.Visible = textBoxCode.Visible = buttonSendCode.Visible = true;
            textBoxCode.Focus();
            listBoxCmd.Items.Add($"A {what} is required...");
        }
        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            cmbMinHour.SelectedIndex = 2;
            cmbTimeInterval.SelectedIndex = 0;
            cmbPref.SelectedIndex = 2;

            txtAppId.Text = Properties.Settings.Default.api_id;
            txtAppHash.Text = Properties.Settings.Default.api_hash;
            txtPhone.Text = Properties.Settings.Default.phone_number;
        }

        private async void button1_Click(object sender, EventArgs e)
        {
            try
            {

                progressBar1.Visible = true;
                listBoxCmd.Items.Clear();
                listBoxCmd.Items.Add($"Connecting & login {txtPhone.Text} into Telegram servers...");
                _client = new WTelegram.Client(what => Config(what, txtPhone.Text));
                _user = await _client.LoginUserIfNeeded();
                listBoxCmd.ForeColor = Color.LimeGreen;
                listBoxCmd.Items.Add($"We are now connected as {_user}");
                  
                progressBar1.Visible = false;
                button1.Enabled = false;
                button1.BackColor = Color.LimeGreen;
                button1.ForeColor = Color.White;
                button1.Text = "Connected";
                //panelActions.Visible = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                progressBar1.Visible = false;
            }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            Properties.Settings.Default.api_id = txtAppId.Text;
            Properties.Settings.Default.api_hash = txtAppHash.Text;
            Properties.Settings.Default.phone_number = txtPhone.Text;
            Properties.Settings.Default.Save();
        }

        private async void button2_Click(object sender, EventArgs e)
        {
            try
            {
                progressBar1.Visible = true;
                if (_user == null)
                {
                    MessageBox.Show("You must login first.");
                    return;
                }
                var chats = await _client.Messages_GetAllChats(null);
                listBoxCmd.Items.Clear();
                listBoxCmd.ForeColor= Color.Yellow;
                foreach (var chat in chats.chats.Values)
                    if (chat.IsActive)
                        listBoxCmd.Items.Add(chat);
                progressBar1.Visible = false;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                progressBar1.Visible = false;
            }
        }

        private void buttonSendCode_Click(object sender, EventArgs e)
        {
            labelCode.Visible = textBoxCode.Visible = buttonSendCode.Visible = false;
            _codeReady.Set();
        }

        private void button4_Click(object sender, EventArgs e)
        {
            try
            {
                if (dataGridView1.Rows.Count > 0)
                {
                    SaveFileDialog sfd = new();
                    sfd.Filter = "CSV (*.csv)|*.csv";
                    sfd.FileName = "Output.csv";
                    bool fileError = false;
                    if (sfd.ShowDialog() == DialogResult.OK)
                    {
                        if (File.Exists(sfd.FileName))
                        {
                            try
                            {
                                File.Delete(sfd.FileName);
                            }
                            catch (IOException ex)
                            {
                                fileError = true;
                                MessageBox.Show("It wasn't possible to write the data to the disk." + ex.Message);
                            }
                        }
                        if (!fileError)
                        {
                            try
                            {
                                int columnCount = dataGridView1.Columns.Count;
                                string columnNames = "";
                                string[] outputCsv = new string[dataGridView1.Rows.Count + 1];
                                for (int i = 0; i < columnCount; i++)
                                {
                                    columnNames += dataGridView1.Columns[i].HeaderText.ToString() + ",";
                                }
                                outputCsv[0] += columnNames;

                                for (int i = 1; i < dataGridView1.Rows.Count; i++)
                                {
                                    for (int j = 0; j < columnCount; j++)
                                    {
                                        outputCsv[i] += dataGridView1.Rows[i - 1].Cells[j].Value.ToString() + ",";
                                    }
                                }

                                File.WriteAllLines(sfd.FileName, outputCsv, Encoding.UTF8);
                                MessageBox.Show("Members Exported Successfully !!!", "Info");
                            }
                            catch (Exception ex)
                            {
                                MessageBox.Show("Error :" + ex.Message);
                            }
                        }
                    }
                }
                else
                {
                    MessageBox.Show("No Record To Export !!!", "Info");
                }
            }
            catch (Exception ex) { MessageBox.Show(ex.Message); }
        }

        private void button5_Click(object sender, EventArgs e)
        {

        }

        private async void button3_Click(object sender, EventArgs e)
        {
            progressBar1.Visible = true;

            try
            {
                if (listBoxCmd.SelectedItem is not ChatBase chat)
                {
                    MessageBox.Show("You must select a chat in the list first");
                  //  kryptonButton4.Enabled = true;
                  //  kryptonButton3.Enabled = true;
                  //  buttonGetChatsKrypton.Enabled = true;
                  //  kryptonButton2.Enabled = true;
                  //  progressBar1.Visible = false;
                    return;
                }
                var users = chat is Channel channel
                    ? (await _client!.Channels_GetAllParticipants(channel)).users
                    : (await _client.Messages_GetFullChat(chat.ID)).users;


                DataTable table = new();

                table.Columns.Add("Username", typeof(string));
                table.Columns.Add("user id", typeof(string));
                table.Columns.Add("access hash", typeof(string));
                table.Columns.Add("group", typeof(string));
                table.Columns.Add("group id", typeof(string));




                // var counti = 0;
                int minHours = cmbMinHour.SelectedIndex;
                int timeIntervals = cmbTimeInterval.SelectedIndex + 1;
                int prefs = cmbPref.SelectedIndex;

                 switch (prefs)
                {
                    case 0:
                        if (minHours == 0)
                        {
                            foreach (var user in users.Values)
                            {
                                if (!string.IsNullOrEmpty(user.username) && user.LastSeenAgo.TotalMinutes < timeIntervals)
                                {
                                    table.Rows.Add(user.username, user.id, user.access_hash, chat.Title, chat.ID);
                                }
                            }
                        }
                        else if (minHours == 1)
                        {
                            foreach (var user in users.Values)
                            {
                                if (!string.IsNullOrEmpty(user.username) && user.LastSeenAgo.TotalHours < timeIntervals)
                                {
                                    table.Rows.Add(user.username, user.id, user.access_hash, chat.Title, chat.ID);
                                }
                            }
                        }
                        else
                        {
                            foreach (var user in users.Values)
                            {
                                if (!string.IsNullOrEmpty(user.username))
                                {
                                table.Rows.Add(user.username, user.id, user.access_hash, chat.Title, chat.ID);
                                }
                            }
                        }
                        break;
                    case 1:
                        if (minHours == 0)
                        {
                            foreach (var user in users.Values)
                            {
                                if (string.IsNullOrEmpty(user.username) && user.LastSeenAgo.TotalMinutes < timeIntervals)
                                {
                                    table.Rows.Add(user.username, user.id, user.access_hash, chat.Title, chat.ID);
                                }
                            }
                        }
                        else if (minHours == 1)
                        {
                            foreach (var user in users.Values)
                            {
                                if (string.IsNullOrEmpty(user.username) && user.LastSeenAgo.TotalHours < timeIntervals)
                                {
                                    table.Rows.Add(user.username, user.id, user.access_hash, chat.Title, chat.ID);
                                }
                            }
                        }
                        else
                        {
                            foreach (var user in users.Values)
                            {
                                if (string.IsNullOrEmpty(user.username))
                                {
                                table.Rows.Add(user.username, user.id, user.access_hash, chat.Title, chat.ID);
                                }
                            }
                        }
                        break;
                    case 2:
                        if (minHours == 0)
                        {
                            foreach (var user in users.Values)
                            {
                                if (user.LastSeenAgo.TotalMinutes < timeIntervals)
                                {
                                    table.Rows.Add(user.username, user.id, user.access_hash, chat.Title, chat.ID);
                                }
                            }
                        }
                        else if (minHours == 1)
                        {
                            foreach (var user in users.Values)
                            {
                                if (user.LastSeenAgo.TotalHours < timeIntervals)
                                {
                                    table.Rows.Add(user.username, user.id, user.access_hash, chat.Title, chat.ID);
                                }
                            }
                        }
                        else
                        {
                            foreach (var user in users.Values)
                            {
                                table.Rows.Add(user.username, user.id, user.access_hash, chat.Title, chat.ID);
                            }
                        }
                        break;

                }

               


                dataGridView1.DataSource = table;
                progressBar1.Visible = false;
                label4.Text = dataGridView1.Rows.Count.ToString();
                label4.Visible = true;
            }
            catch (Exception ex)
            {
                progressBar1.Visible = false;
                MessageBox.Show(ex.Message);
            }
        }

        private void txtSearch_KeyPress(object sender, KeyPressEventArgs e)
        {
            (dataGridView1.DataSource as DataTable).DefaultView.RowFilter = string.Format("Username LIKE '{0}%'", txtSearch.Text);
        }

        private void dataGridView1_RowHeaderMouseDoubleClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            foreach (DataGridViewRow row in dataGridView1.SelectedRows)
            {
                dataGridView1.Rows.RemoveAt(row.Index);
            }
        }
    }
}