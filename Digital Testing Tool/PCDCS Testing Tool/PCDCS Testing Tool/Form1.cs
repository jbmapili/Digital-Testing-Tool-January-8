using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
//using WindowsFormsControlLibrary1;

using OpcRcw.Da;
using Microsoft.VisualBasic.FileIO;
using System.IO;
using System.Threading;
using System.Text.RegularExpressions;
using TracerX;

namespace PCDCS_Testing_Tool
{
    public partial class Form1 : Form
    {
        DxpSimpleAPI.DxpSimpleClass opc = new DxpSimpleAPI.DxpSimpleClass();
        List<string[]> list = new List<string[]>();
        List<string> listreg = new List<string>() { };
        List<string> registers = new List<string>() { };
        List<string> tags = new List<string>() { };
        List<TextBox> tag_no = new List<TextBox>();
        List<TextBox> reg_no = new List<TextBox>();
        List<TextBox> valueReg = new List<TextBox>();
        string[] sItemIDArray = new string[5];
        int a = -1;
        bool saved = true;
        public Form1()
        {
            InitializeComponent();
            backgroundWorker1.WorkerReportsProgress = true;
        }
        private void Form1_Load(object sender, EventArgs e)
        {
            progressBar1.Visible = false;
            maintable.Visible = false;

        }

        private void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
            for (int i = 0; i < list.Count; i++)
            {
                Thread.Sleep(100);
                backgroundWorker1.ReportProgress(i);
            }

        }

        private void backgroundWorker1_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            int i = e.ProgressPercentage;
            TextBox txtNo = new TextBox();
            txtNo.Text = i.ToString();
            txtNo.ReadOnly = true;
            maintable.Controls.Add(txtNo, 0, i);

            TextBox txtTagNo = new TextBox();
            txtTagNo.Text = list[i][1];
            txtTagNo.Tag = i.ToString();
            txtTagNo.ReadOnly = true;
            maintable.Controls.Add(txtTagNo, 1, i);
            tag_no.Add(txtTagNo);

            TextBox txtRegNo = new TextBox();
            txtRegNo.Text = list[i][6];
            txtRegNo.Tag = i.ToString();
            txtRegNo.ReadOnly = true;
            maintable.Controls.Add(txtRegNo, 2, i);
            reg_no.Add(txtRegNo);

            TextBox txtStatus = new TextBox();
            maintable.Controls.Add(txtStatus, 3, i);
            valueReg.Add(txtStatus);

            Button btnOn = new Button();
            btnOn.Text = "On";
            btnOn.Tag = i.ToString();
            btnOn.Click += btnOn_Click;
            maintable.Controls.Add(btnOn, 4, i);

            Button btnOff = new Button();
            btnOff.Text = "Off";
            btnOff.Tag = i.ToString();
            btnOff.Click += btnOff_Click;
            maintable.Controls.Add(btnOff, 5, i);

            progressBar1.Value = e.ProgressPercentage;

            if (e.ProgressPercentage + 1 == list.Count)
            {
                maintable.Visible = true;
                button1.Enabled = true;
                progressBar1.Visible = false;
            }
            if (e.ProgressPercentage == 0)
            {
                maintable.Visible = false;
                button1.Enabled = false;
                progressBar1.Visible = true;
            }
        }
        private void btnListRefresh_Click(object sender, EventArgs e)
        {
            cmbServerList.Items.Clear();
            string[] ServerNameArray;
            opc.EnumServerList(txtNode.Text, out ServerNameArray);

            for (int a = 0; a < ServerNameArray.Count<string>(); a++)
            {
                cmbServerList.Items.Add(ServerNameArray[a]);
            }
            cmbServerList.SelectedIndex = 0;
        }

        private void btnConnect_Click(object sender, EventArgs e)
        {
            if (opc.Connect(txtNode.Text, cmbServerList.Text))
            {
                btnListRefresh.Enabled = false;
                btnDisconnect.Enabled = true;
                btnConnect.Enabled = false;

            }
        }

        object[] oValue = new object[5];
               
        private object WriteValCopy(string sText, int n)
        {
            if (oValue[n] is Array)
            {
                string[] sBufPut = sText.Split(',');
                Array ary = (Array)oValue[n];
                object[] oAry = new object[ary.Length];
                for (int j = 0; j < ary.Length; j++)
                {
                    if (j >= sBufPut.Length)
                        break;
                    try
                    {
                        if (oValue[n] is UInt16)
                        {
                            oAry[j] = UInt16.Parse(sBufPut[j]);
                        }
                        else if (oValue[n] is UInt32)
                        {
                            oAry[j] = UInt32.Parse(sBufPut[j]);
                        }
                        else
                        {
                            oAry[j] = (object)sBufPut[j];
                        }
                    }
                    catch
                    {
                        MessageBox.Show("Error");
                    }
                }
                return oAry;
            }
            else
            {
                return sText;
            }
        }

        private void FileReadBtn_Click(object sender, EventArgs e)
        {

            if (openFileDialog1.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                using (TextFieldParser parser = new TextFieldParser(openFileDialog1.FileName, Encoding.GetEncoding(932)))
                {
                    parser.Delimiters = new string[] { "," };
                    bool st = false;

                    string checkPointString = "";
                    if (Properties.Settings.Default.EnglishMode == true)
                    {
                        checkPointString = "[Dictionary for Alarm]";
                    }
                    else
                    {
                        checkPointString = "[アラーム用辞書]";
                    }

                    while (true)
                    {
                        string[] parts = parser.ReadFields();
                        if (parts == null)
                        {
                            break;
                        }
                        if (st)
                        {
                            if (parts[0] != "[END]")
                            {
                                list.Add(parts);
                            }
                            else
                            {
                                st = false;
                            }
                        }

                        if (parts[0] == checkPointString)
                        {
                            st = true;
                        }
                    }


                    maintable.Controls.Clear();

                    if (list.Count > 0)
                    {
                        registers.Clear();
                        progressBar1.Maximum = list.Count;
                        tags.Clear();
                        tag_no.Clear();
                        reg_no.Clear();
                        valueReg.Clear();
                        maintable.RowCount = list.Count;
                        maintable.Height = 29 * list.Count;
                        backgroundWorker1.RunWorkerAsync();
                        for (int i = 0; i < list.Count; i++)
                        {
                            registers.Add(list[i][6]);
                            tags.Add(list[i][1]);
                        }
                    }
                    else
                    {
                        Label message = new Label();
                        maintable.Visible = false;
                        message.Text = "There are no lists inside the file.";
                        message.Location = new Point(0, 30);
                        message.Width = 200;                        
                        panel1.Controls.Add(message);
                    }
                }
            }
        }

        void btnOff_Click(object sender, EventArgs e)
        {
            OpcOnOff(0, Convert.ToInt32((sender as Button).Tag.ToString()), (sender as Button).Text);
        }

        void btnOn_Click(object sender, EventArgs e)
        {
            OpcOnOff(1, Convert.ToInt32((sender as Button).Tag.ToString()), (sender as Button).Text);
        }

        private void btnDisconnect_Click(object sender, EventArgs e)
        {
            if (opc.Disconnect())
            {
                btnConnect.Enabled = true;
                btnListRefresh.Enabled = true;
                btnDisconnect.Enabled = false;
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            search();
        }

        private void search()
        {
            if (a > -1)
            {
                reg_no[a].BackColor = SystemColors.Control;
                tag_no[a].BackColor = SystemColors.Control;
            }
            if (registers.Contains(txtReg.Text))
            {
                a = registers.IndexOf(txtReg.Text);
                reg_no[a].BackColor = Color.Red;
                panel1.VerticalScroll.Value = a * 29;
            }
            else if (tags.Contains(txtReg.Text))
            {
                a = tags.IndexOf(txtReg.Text);
                tag_no[a].BackColor = Color.Red;
                panel1.VerticalScroll.Value = a * 29;
            }
        }

        private void OpcOnOff(int value, int tag, string sender)
        {
            try
            {
                string[] target = new string[] { reg_no[tag].Text };
                object[] val = new object[] { value };
                int[] nErrorArray;

                data1.ColumnCount = 6;
                data1.Columns[0].Name = "Date Time";
                data1.Columns[1].Name = "Tag No.";
                data1.Columns[2].Name = "Register No";
                data1.Columns[3].Name = "Status";
                data1.Columns[4].Name = "Success/Error";
                data1.Columns[5].Name = "Sender";

                if (opc.Write(target, val, out nErrorArray))
                {
                    valueReg[tag].Text = value.ToString();
                    string[] row = new string[] { DateTime.Now.ToString(), tag_no[tag].Text, reg_no[tag].Text, valueReg[tag].Text, "Write Success", "btn" + sender };
                    data1.Rows.Add(row);
                }
                else
                {
                    valueReg[tag].Text = "Write Error";
                    string[] row = new string[] { DateTime.Now.ToString(), tag_no[tag].Text, reg_no[tag].Text, value.ToString(), "Write Error", "btn" + sender };
                    data1.Rows.Add(row);
                }
                short[] wQualityArray;
                OpcRcw.Da.FILETIME[] fTimeArray;

                if (opc.Read(target, out val, out wQualityArray, out fTimeArray, out nErrorArray) == true)
                {
                    valueReg[tag].Text = val[0].ToString();
                    string[] row = new string[] { DateTime.Now.ToString(), tag_no[tag].Text, reg_no[tag].Text, valueReg[tag].Text, "Read Success", "btn" + sender };
                    data1.Rows.Add(row);
                }
                else
                {

                    string[] row = new string[] { DateTime.Now.ToString(), tag_no[tag].Text, reg_no[tag].Text, valueReg[tag].Text, "Read Error", "btn" + sender };
                    data1.Rows.Add(row);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        private void txtReg_TextChanged(object sender, EventArgs e)
        {
            foreach (TextBox tag in tag_no)
            {
                tag.BackColor = SystemColors.Control;
            }
            foreach (TextBox regs in reg_no)
            {
                regs.BackColor = SystemColors.Control;
            }
            if (txtReg.Text != "")
            {
                listreg.Clear();
                Regex reg = new Regex(txtReg.Text.ToUpper() + "+?");
                foreach (TextBox tag in tag_no)
                {
                    Match m= reg.Match(tag.Text);
                    if (m.Success)
                    {
                        tag.BackColor = Color.Red;
                        listreg.Add(tag.Tag.ToString());
                    }
                }
                foreach (TextBox regs in reg_no)
                {
                    Match m = reg.Match(regs.Text);
                    if (m.Success)
                    {
                        regs.BackColor = Color.Red;
                        listreg.Add(regs.Tag.ToString());
                    }
                }
                if (listreg.Count > 0) 
                { 
                    panel1.VerticalScroll.Value = Convert.ToInt32(listreg[0]) * 29;
                }
                else
                {
                    MessageBox.Show("No result found!");
                }
            }
        }


        private void txtReg_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                search();
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            SaveFileDialog sf = new SaveFileDialog();
            sf.Filter = "Csv File|*.csv|Text File|*.txt";
            if (sf.ShowDialog() == DialogResult.OK)
            {
                using (StreamWriter sw = new StreamWriter(sf.FileName))
                {
                    for (int r = 0; r < data1.RowCount - 1; r++)
                    {
                        sw.WriteLine("{0},{1},{2},{3},{4},{5}", data1.Rows[r].Cells[0].Value,
                                                            data1.Rows[r].Cells[1].Value,
                                                            data1.Rows[r].Cells[2].Value,
                                                            data1.Rows[r].Cells[3].Value,
                                                            data1.Rows[r].Cells[4].Value,
                                                            data1.Rows[r].Cells[5].Value);
                    }
                    sw.Close();
                    saved = true;
                }
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            if (list.Count > 0)
            {
                string error = "No error";
                for (int a = 0; a < list.Count; a++)
                {
                    string[] target = new string[] { reg_no[a].Text };
                    object[] val;
                    int[] nErrorArray;
                    short[] wQualityArray;
                    OpcRcw.Da.FILETIME[] fTimeArray;

                    data1.ColumnCount = 6;
                    data1.Columns[0].Name = "Date Time";
                    data1.Columns[1].Name = "Tag No.";
                    data1.Columns[2].Name = "Register No";
                    data1.Columns[3].Name = "Status";
                    data1.Columns[4].Name = "Success/Error";
                    data1.Columns[5].Name = "Sender";

                    string[] row;

                    bool rr = opc.Read(target, out val, out wQualityArray, out fTimeArray, out nErrorArray);
                    if (nErrorArray[0] == 0 && rr)
                    {
                        valueReg[a].Text = val[0].ToString();
                        //row = new string[] { DateTime.Now.ToString(), tag_no[a].Text, reg_no[a].Text, valueReg[a].Text, "Read Success", "btn" + (sender as Button).Text };
                    }
                    else
                    {
                        valueReg[a].Text = "Read Error";
                        row = new string[] { DateTime.Now.ToString(), tag_no[a].Text, reg_no[a].Text, valueReg[a].Text, "Read Error", "btn" + (sender as Button).Text };
                        data1.Rows.Add(row);
                        error = "There are errors";
                    }
                    
                }
                string[] rows = new string[] { DateTime.Now.ToString(),"-" , "-", "Done reading values", error, "btn" + (sender as Button).Text };
                data1.Rows.Add(rows);
                saved = false;
                }
            
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (!saved)
            {
                string msg = MessageBox.Show("Do you want to save the log to file before closing?", "WARNING",
                                    MessageBoxButtons.YesNoCancel,
                                    MessageBoxIcon.Warning).ToString();
                if (msg == "Yes")
                {
                    SaveFileDialog sf = new SaveFileDialog();
                    sf.Filter = "Csv File|*.csv|Text File|*.txt";
                    if (sf.ShowDialog() == DialogResult.OK)
                    {
                        using (StreamWriter sw = new StreamWriter(sf.FileName))
                        {
                            for (int r = 0; r < data1.RowCount - 1; r++)
                            {
                                sw.WriteLine("{0},{1},{2},{3},{4},{5}", data1.Rows[r].Cells[0].Value,
                                                                    data1.Rows[r].Cells[1].Value,
                                                                    data1.Rows[r].Cells[2].Value,
                                                                    data1.Rows[r].Cells[3].Value,
                                                                    data1.Rows[r].Cells[4].Value,
                                                                    data1.Rows[r].Cells[5].Value);
                            }
                            sw.Close();
                            saved = true;
                        }
                    }
                    else
                    {
                        e.Cancel = true;
                    }
                }
                if (msg == "Cancel")
                {
                    e.Cancel = true;
                }
            }
        }

        private void data1_RowsAdded(object sender, DataGridViewRowsAddedEventArgs e)
        {
            saved = false;
            data1.FirstDisplayedScrollingRowIndex = data1.RowCount - 1;
        }


  
    }
}
