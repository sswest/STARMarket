using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Win32;
using STARMarket.Classes;

namespace STARMarket.Forms
{
    public partial class Form_config : Form
    {
        public Form_config()
        {
            InitializeComponent();
        }

        private void Form_config_Load(object sender, EventArgs e)
        {
            if (Form1.localData)
            {
                checkBox2.Enabled = false;
                checkBox3.Enabled = false;
                checkBox4.Enabled = false;
                checkBox5.Enabled = false;
            }
            textBox1.Text = Form1.localBasePath;
            textBox2.Text = Form1.fileLocatorPath;
            foreach (CheckBox control in this.groupBox2.Controls)
            {
                control.Checked = Form1.filterDictionary[control.Tag.ToString()];
            }
        }
        //读取注册表

        private void Button3_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog dialog = new FolderBrowserDialog();
            dialog.Description = "请选择缓存目录";
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                textBox1.Text = dialog.SelectedPath;
            }
        }

        private void Button4_Click(object sender, EventArgs e)
        {
            OpenFileDialog fileDialog = new OpenFileDialog();
            fileDialog.InitialDirectory = textBox2.Text;
            fileDialog.Title = "请选择检索程序";
            fileDialog.Filter = "所有文件(*.exe)|*.exe";
            if (fileDialog.ShowDialog() == DialogResult.OK)
            {
                textBox2.Text = fileDialog.FileName;
            }
        }

        private void Button1_Click(object sender, EventArgs e)
        {
            //遍历高级筛选
            foreach (CheckBox control in this.groupBox2.Controls)
            {
                Form1.filterDictionary[control.Tag.ToString()] = control.Checked;

            }
            //写入注册表
            try
            {
                RegistryKey key = Registry.CurrentUser.OpenSubKey("Software",true);
                RegistryKey software = key.OpenSubKey("STARMarket", true);
                if (software == null)
                {
                    software = key.CreateSubKey("STARMarket");
                }
                //software = key.OpenSubKey("STARMarket", true);
                Form1.localBasePath = textBox1.Text;
                Form1.fileLocatorPath = textBox2.Text;
                Result.localBasePath = textBox1.Text;
                software.SetValue("localBasePath", textBox1.Text);
                software.SetValue("fileLocatorPath", textBox2.Text);
                this.Close();
            }
            catch (Exception exception)
            {
                MessageBox.Show(exception.ToString());
            }

        }

        private void Button2_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}
