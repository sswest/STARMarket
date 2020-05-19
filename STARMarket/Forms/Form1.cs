using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.Eventing.Reader;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;
using Microsoft.Win32;
using STARMarket.Classes;
using STARMarket.Core;
using STARMarket.Forms;

namespace STARMarket
{
    public partial class Form1 : Form
    {

        public static string localBasePath;
        public static string fileLocatorPath;
        public static SortedDictionary<string,bool> filterDictionary = new SortedDictionary<string, bool>();
        private static Dictionary<string, string> statuDictionary = new Dictionary<string, string>();
        private static Dictionary<string,string> intermediaryTypes = new Dictionary<string, string>();
        private static Dictionary<string, string> localstatus = new Dictionary<string, string>();
        private static RootObject companyRoot;
        private static RootObject fileRoot;
        private static List<Result> resultTemp;
        private static List<Result> resultMatch = new List<Result>();
        private static List<Data> dataTemp = new List<Data>();
        private static List<Data> selectCompany = new List<Data>();
        private static int capacity = 10;
        private static Queue downloadQueue = new Queue(capacity);
        private static bool stopDownload = false;
        public static bool localData = false;

        public Form1()
        {
            InitializeComponent();
            Init();
        }
        // 其他初始化工作
        public void Init()
        {
            RegistryKey key = Registry.CurrentUser.OpenSubKey("Software",true);
            RegistryKey software = key.OpenSubKey("STARMarket", true);
            try
            {
                localBasePath = software.GetValue("localBasePath").ToString();
                fileLocatorPath = software.GetValue("fileLocatorPath").ToString();
            }
            catch (Exception)
            {
                //读取注册表错误时直接使用当前目录配置
                localBasePath = Directory.GetCurrentDirectory() + "\\信息披露";
                fileLocatorPath = Directory.GetCurrentDirectory() + "\\FileLocator Pro\\FileLocatorPro.exe";
            }
            statuDictionary.Add("全部","0");
            statuDictionary.Add("已受理","1");
            statuDictionary.Add("已问询","2");
            statuDictionary.Add("已过会", "3");
            statuDictionary.Add("提交注册","4");
            //在初始化数据过程中会处理注册结果
            statuDictionary.Add("注册生效","5-1");
            statuDictionary.Add("不予注册", "5-2");
            statuDictionary.Add("终止注册", "5-3");
            statuDictionary.Add("中止","7");
            statuDictionary.Add("终止","8");
            //statuDictionary.Add("未通过", "9");

            intermediaryTypes.Add("1", "保荐机构");
            intermediaryTypes.Add("2", "会计师事务所");
            intermediaryTypes.Add("3", "律师事务所");
            intermediaryTypes.Add("4", "评估机构");

            filterDictionary.Add("companyFullName",true);
            filterDictionary.Add("hangye", false);
            filterDictionary.Add("baojian", false);
            filterDictionary.Add("shenji", false);
            filterDictionary.Add("lvshi", false);
            filterDictionary.Add("pinggu", false);
            filterDictionary.Add("fileTitle", true);

            //获取本地目录的公司状态
            foreach (string statu in statuDictionary.Keys)
            {
                DirectoryInfo dir = new DirectoryInfo(localBasePath + "\\" + statu);
                if (dir.Exists)
                {
                    //遍历子文件名
                    foreach (var cmpdir in dir.GetDirectories())
                    {
                        try
                        {
                            localstatus.Add(cmpdir.Name, statu);
                        }
                        catch (Exception e)
                        {
                            MessageBox.Show(e.ToString());
                        }
                    }
                }
            }

        }
        //日期格式化
        private string DateFormat(string str)
        {
            if (str.Length >= 8)
            {
                return str.Substring(0, 4) + "-" + str.Substring(4, 2) + "-" + str.Substring(6, 2);
            }
            else
            {
                return "未获取";
            }
        }
        //异步下载文件
        private void AsyncDownLoad(Result result)
        {
            Result res = result as Result;
            res.mkdir();
            res.downlaod();
            float rate = (float)downloadQueue.Dequeue();
            if (!stopDownload)
            {
                //停止就不更新状态栏了！
                toolStripStatusLabel1.Text = "列表缓存进度" + rate.ToString("P");
            }
            
            
        }
        private void GetData(object sender, DoWorkEventArgs e)
        {
            try
            {
                //使用companyroot数据时 从pageHelp.data调用
                //使用fileroot数据时 从result调用
                companyRoot = Scrapy.GetCompany();
                fileRoot = Scrapy.GetFiles();
            }
            catch (Exception exception)
            {
                if (exception.Message=="0")
                {
                    DialogResult dialog = MessageBox.Show("在线获取数据失败\n是否加载本地文件数据?", "错误", MessageBoxButtons.OKCancel, MessageBoxIcon.Error);
                    if (dialog == DialogResult.OK)
                    {
                        e.Result = "加载本地数据";
                        return;
                    }
                }
                else
                {
                    MessageBox.Show(exception.ToString());
                }
                
            }
            //异步获取并初始化数据
            toolStripStatusLabel1.Text = "初始化数据";
            // 在这里校验一波公司名 避免搞事情
            foreach (var data in companyRoot.pageHelp.data)
            {
                if (data.stockAuditName != data.stockIssuer[0].s_issueCompanyFullName)
                {
                    data.stockAuditName = data.stockIssuer[0].s_issueCompanyFullName;
                }
                //处理特殊状态
                if (data.currStatus == "5")
                {
                    data.currStatus += "-" + data.registeResult;
                }
                var statu = statuDictionary.Where(q => q.Value == data.currStatus).First();
                if (localstatus.ContainsKey(data.stockAuditName))
                {
                    if (localstatus[data.stockAuditName]!=statu.Key)
                    {
                        //状态不一致 移动目录
                        //MessageBox.Show("公司" + data.stockAuditName+" 最新状态" +statu.Key +" 老状态"+localstatus[data.stockAuditName]);
                        DirectoryInfo olddir = new DirectoryInfo(Result.localBasePath+"\\"+localstatus[data.stockAuditName] + "\\" + data.stockAuditName);
                        DirectoryInfo dir2 = new DirectoryInfo(Result.localBasePath + "\\" + statu.Key);
                        if (!dir2.Exists) dir2.Create();
                        olddir.MoveTo(Result.localBasePath + "\\" + statu.Key+"\\"+ data.stockAuditName);
                        localstatus[data.stockAuditName] = statu.Key;
                        toolStripStatusLabel1.Text = data.stockAuditName + " 申报状态由 " +
                                                     localstatus[data.stockAuditName] + " 变为 " + statu.Key;

                    }
                }

            }
            foreach (Result res in fileRoot.result)
            {
                //让company和file互相引用
                Data companyData = (from c in companyRoot.pageHelp.data
                    where c.stockAuditName == res.companyFullName
                    select c).First();
                string currStatus = companyData.currStatus;
                var statu = statuDictionary.FirstOrDefault(q => q.Value == currStatus).Key;
                res.CompanyStatus = statu;
                res.CompanyData = companyData;
                //if (companyData.FileResults == null)
                //{
                //    //先要初始化列表
                //    companyData.FileResults = new List<Result>();
                //}
                companyData.FileResults.Add(res);
            }
            
            resultTemp = fileRoot.result.ToList();
            e.Result = "GetData";
        }
        static void CallBack(IAsyncResult ar)
        {
            //委托回调函数啦,什么也不干

        }
        private void DownLoadContrl(object sender, DoWorkEventArgs e)
        {
            //下载控制器
            List<Result> downResults = resultMatch.ToList();
            int i = 0;
            foreach (Result result in downResults)
            {
                i++;
                while (downloadQueue.Count == capacity)
                {
                    //队列达到最大时等待
                    Thread.Sleep(10);
                    if (stopDownload) break;
                }
                if (stopDownload) break;
                //加入队列
                if (!result.Exists)
                {
                    //文件不存在才下载
                    float rate = i / (float)downResults.Count;
                    downloadQueue.Enqueue(rate);
                    Action<Result> t = AsyncDownLoad;
                    IAsyncResult ar = t.BeginInvoke(result,CallBack,t);
                }
            }

            if (stopDownload)
            {
                toolStripStatusLabel1.Text = "列表缓存已停止";
            }
            while (downloadQueue.Count != 0)
            {
                //下载任务未完成 阻塞
                if (stopDownload) break;
                Thread.Sleep(1000);
            }
            e.Result = "DownLoadContrl";
        }
        private void Form1_Load(object sender, EventArgs e)
        {
            comboBox1.DropDownStyle = ComboBoxStyle.DropDownList;
            comboBox2.DropDownStyle = ComboBoxStyle.DropDownList;
            foreach (string s in statuDictionary.Keys)
            {
                comboBox2.Items.Add(s);
            }
        }
        private void Button1_Click(object sender, EventArgs e)
        {
            toolStripStatusLabel1.Text = "正在获取数据";
            button1.Enabled = false;
            using (BackgroundWorker bw = new BackgroundWorker())
            {
                bw.RunWorkerCompleted += new RunWorkerCompletedEventHandler(bw_RunWorkerCompleted);
                bw.DoWork += new DoWorkEventHandler(GetData);
                bw.RunWorkerAsync("Tank");
            }
        }
        private void Button2_Click(object sender, EventArgs e)
        {
            //下载列表文件
            if (button2.Text == "停止")
            {
                stopDownload = true;
                button2.Text = "缓存列表文件";
            }
            else
            {
                stopDownload = false;
                button2.Text = "停止";
                using (BackgroundWorker bw = new BackgroundWorker())
                {
                    bw.RunWorkerCompleted += new RunWorkerCompletedEventHandler(bw_RunWorkerCompleted);
                    bw.DoWork += new DoWorkEventHandler(DownLoadContrl);
                    bw.RunWorkerAsync("Tank");
                }
            }
            
            
        }
        void bw_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            //这时后台线程已经完成，并返回了主线程，所以可以直接使用UI控件了 
            string res = e.Result.ToString();
            if (res=="GetData")
            {
                toolStripStatusLabel1.Text = "初始化数据";
                button1.Enabled = true;
                comboBox2.SelectedIndex = 0;
                toolStripStatusLabel1.Text = "";
            } else if (res == "DownLoadContrl")
            {
                toolStripStatusLabel1.Text = "";
                button2.Text = "缓存列表文件";
            } else if (res == "加载本地数据")
            {
                加载本地数据ToolStripMenuItem_Click(new object(), new EventArgs());
            }
            else
            {
                toolStripStatusLabel1.Text = res;
            }
        }
        private void ComboBox2_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (companyRoot==null)
            {
                return;
            }
            comboBox1.Items.Clear();
            comboBox1.Items.Add("选择一家企业获取详细信息");
            dataTemp.Clear();
            foreach (Data data in companyRoot.pageHelp.data)
            {
                if (data.currStatus == statuDictionary[(string)comboBox2.SelectedItem] || (string)comboBox2.SelectedItem == "全部")
                {
                    comboBox1.Items.Add(data);
                    dataTemp.Add(data);
                }
            }

            label2.Text = "有" + Convert.ToString(dataTemp.Count) + "家企业：";
            comboBox1.SelectedIndex = 0;
        }
        private void ComboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            
            selectCompany.Clear();
            if (comboBox1.SelectedIndex == 0)
            {
                //未选中单个公司 即选中下拉框所有公司
                selectCompany = dataTemp.ToList();
                label_detals.Text = default;
                linkLabel1.Visible = false;
            }
            else
            {
                Data data = (Data)comboBox1.SelectedItem;
                selectCompany.Add(data);
                // 选择单个公司 调出属性栏
                if (!localData)
                {
                    label_detals.Text = "公司简称：" + data.stockIssuer[0].s_issueCompanyAbbrName + "\n";
                    label_detals.Text += "所属行业：" + data.stockIssuer[0].s_csrcCodeDesc + "\n";
                    var statu = statuDictionary.FirstOrDefault(q => q.Value == data.currStatus).Key;
                    label_detals.Text += "申报状态：" + statu + "\n";
                    string temp = "";
                    foreach (var itmd in data.intermediary)
                    {
                        if (intermediaryTypes.ContainsKey(itmd.i_intermediaryType))
                        {
                            temp += intermediaryTypes[itmd.i_intermediaryType] + "：" + itmd.i_intermediaryAbbrName + "\n";
                        }
                    }
                    label_detals.Text += temp;
                    label_detals.Text += "更新日期：" + DateFormat(data.updateDate) + "\n";
                    label_detals.Text += "受理日期：" + DateFormat(data.createTime);
                    linkLabel1.Visible = true;
                    linkLabel1.Links.Clear();
                    linkLabel1.Links.Add(0, 4, @"http://kcb.sse.com.cn/renewal/xmxq/index.shtml?auditId=" + data.stockAuditNum);
                }
            }

            //刷新列表框
            listBox1.Items.Clear();
            resultTemp.Clear();
            foreach (Data data in selectCompany)
            {
                foreach (Result result in data.FileResults)
                {
                    resultTemp.Add(result);
                }
            }
            //foreach (var result in fileRoot.result)
            //{
            //    if (selectCompany.Contains(result.companyFullName))
            //    {
            //        //公司被选中要显示出来
            //        resultTemp.Add(result);
            //    }
                
            //}
            //刷新过滤器
            Button3_Click(e, new EventArgs());
        }
        private void ListBox1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            //双击 判断是否存在 下载
            int index = listBox1.SelectedIndex;
            if (index==-1)
            {
                return;
            }

            Result targetResult = resultMatch[index];
            if (targetResult.Exists)
            {
                System.Diagnostics.Process.Start(targetResult.localPath);
            }
            else
            {
                toolStripStatusLabel1.Text = targetResult.fileTitle.Length > 50 ? "" : targetResult.fileTitle;
                toolStripStatusLabel1.Text += " 正在存储至本地";
                targetResult.mkdir();
                using (BackgroundWorker bw = new BackgroundWorker())
                {
                    bw.RunWorkerCompleted += new RunWorkerCompletedEventHandler(bw_RunWorkerCompleted);
                    bw.DoWork += new DoWorkEventHandler(targetResult.downlaod);
                    bw.RunWorkerAsync("Tank");
                }

            }
        }
        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            this.linkLabel1.Links[this.linkLabel1.Links.IndexOf(e.Link)].Visited = true;
            string targetUrl = e.Link.LinkData as string;
            if (!string.IsNullOrEmpty(targetUrl))
                System.Diagnostics.Process.Start(targetUrl);
        }
        private void 设置ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Form_config fm = new Form_config();
            fm.Show();
        }
        private void ListBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            //单体 显示状态栏
            int index = listBox1.SelectedIndex;
            if (index==-1)
            {
                return;   
            }
            Result targetResult = resultMatch[index];
            toolStripStatusLabel1.Text = targetResult.fileTitle.Length > 50 ? "":targetResult.fileTitle;
            if (targetResult.Exists)
            {
                toolStripStatusLabel1.Text += " 本地存储";
            }
            else
            {
                toolStripStatusLabel1.Text += " 网络位置";
            }

            if (comboBox1.SelectedIndex==0)
            {
                if (!localData)
                {
                    Data data = targetResult.CompanyData;
                    label_detals.Text = "公司简称：" + data.stockIssuer[0].s_issueCompanyAbbrName + "\n";
                    label_detals.Text += "所属行业：" + data.stockIssuer[0].s_csrcCodeDesc + "\n";
                    var statu = statuDictionary.FirstOrDefault(q => q.Value == data.currStatus).Key;
                    label_detals.Text += "申报状态：" + statu + "\n";
                    string temp = "";
                    foreach (var itmd in data.intermediary)
                    {
                        if (intermediaryTypes.ContainsKey(itmd.i_intermediaryType))
                        {
                            temp += intermediaryTypes[itmd.i_intermediaryType] + "：" + itmd.i_intermediaryAbbrName + "\n";
                        }
                    }
                    label_detals.Text += temp;
                    label_detals.Text += "更新日期：" + DateFormat(data.updateDate) + "\n";
                    label_detals.Text += "受理日期：" + DateFormat(data.createTime);
                    linkLabel1.Visible = true;
                    linkLabel1.Links.Clear();
                    linkLabel1.Links.Add(0, 4, @"http://kcb.sse.com.cn/renewal/xmxq/index.shtml?auditId=" + data.stockAuditNum);
                }

            }
        }
        private void ListBox1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                //调用双击功能啦
                ListBox1_MouseDoubleClick(sender,new MouseEventArgs(MouseButtons.Left,1,1,1,1));
            } else if (e.KeyCode == Keys.F2)
            {
                //打开目录定位文件
                int index = listBox1.SelectedIndex;
                if (index == -1)
                {
                    return;
                }
                
                Result targetResult = resultMatch[index];
                if (targetResult.Exists)
                {
                    System.Diagnostics.Process.Start("Explorer", "/select," + targetResult.localPath);

                }
            }
        }

        private void Button3_Click(object sender, EventArgs e)
        {
            //正则匹配
            if (resultTemp==null)
            {
                return;
            }
            string pattern = textBox1.Text;
            
            resultMatch = resultTemp.Where(r =>
            {
                string source_input = "";
                //放弃遍历字典了 无序难受 有时间自己实现有序字典
                //foreach (var dic in filterDictionary)
                //{
                //    MessageBox.Show(dic.Key);
                //}
                if (filterDictionary["companyFullName"])
                    source_input += r.companyFullName;
                if (filterDictionary["hangye"])
                    source_input += r.CompanyData.stockIssuer[0].s_csrcCodeDesc;
                if (filterDictionary["baojian"])
                    source_input += r.CompanyData.intermediary.Where(i => i.i_intermediaryType == "1").First().i_intermediaryName;
                if (filterDictionary["shenji"])
                    source_input += r.CompanyData.intermediary.Where(i => i.i_intermediaryType == "2").First().i_intermediaryName;
                if (filterDictionary["lvshi"])
                    source_input += r.CompanyData.intermediary.Where(i => i.i_intermediaryType == "3").First().i_intermediaryName;
                //if (filterDictionary["pinggu"])
                //{
                //    //有些项目没有评估机构 真难搞
                //    try { source_input += r.CompanyData.intermediary.Where(i => i.i_intermediaryType == "4").FirstOrDefault(); }
                //    catch (Exception) { }
                //}
                if (filterDictionary["fileTitle"])
                    source_input += r.fileTitle;
                return Regex.IsMatch(source_input, pattern);
            }).ToList();
            listBox1.Items.Clear();
            int index = 0;
            label1.Text = index.ToString() + "/" + Convert.ToString(fileRoot.pageHelp.total);
            foreach (var r in resultMatch)
            {
                index++;
                label1.Text = index.ToString() + "/" + Convert.ToString(fileRoot.pageHelp.total);
                listBox1.Items.Add(r.companyFullName + ":" + r.fileTitle);
            }
        }

        private void TextBox1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                //单击Button3
                Button3_Click(e,new EventArgs());
            }
        }

        private void 全文检索ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //打开FileLocator 
            string path = fileLocatorPath;
            FileInfo file = new FileInfo(path);
            if (!file.Exists)
            {
                设置ToolStripMenuItem_Click(sender,new EventArgs());
                return;
            }
            string args = "";
            if (comboBox1.SelectedIndex == -1 || comboBox1.SelectedIndex == 0)
            {
                if (comboBox2.SelectedIndex == -1 || comboBox2.SelectedIndex == 0)
                {
                    args += "-d \"" + Result.localBasePath + "\" ";
                }
                else
                {
                    args += "-d \"" + Result.localBasePath + "\\" + comboBox2.SelectedItem.ToString() + "\" ";

                }
            }
            else
            {
                Data data = dataTemp[comboBox1.SelectedIndex - 1];
                string company = comboBox1.SelectedItem.ToString();
                string statu = statuDictionary.FirstOrDefault(q => q.Value == data.currStatus).Key;
                args += "-d \"" + Result.localBasePath + "\\" + statu + "\\" + company + "\" ";
            }
            
            args += "-fex ";
            args += "-f \"" + textBox1.Text + "\" ";
            //args += "-c ";

            System.Diagnostics.Process.Start(path,args);

        }

        private void 加载本地数据ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (!localData)
            {
                //第二次就没必要提示了
                DialogResult dialog = MessageBox.Show("使用本地数据部分功能将无法使用\n建议只有在无法从互联网取得数据时才启用\n确定要加载本地缓存数据?\n", "提示", MessageBoxButtons.OKCancel, MessageBoxIcon.Asterisk,
                    MessageBoxDefaultButton.Button2);
                if (dialog == DialogResult.Cancel) return;
            }
            filterDictionary["hangye"] = false;
            filterDictionary["baojian"] = false;
            filterDictionary["shenji"] = false;
            filterDictionary["lvshi"] = false;
            DirectoryInfo directoryInfo = new DirectoryInfo(localBasePath);
            DirectoryInfo[] subinfos = directoryInfo.GetDirectories();
            List<Result> results = new List<Result>();
            PageHelp pageHelp = new PageHelp();
            List<Data> datas = new List<Data>();
            int total = 0;
            foreach (DirectoryInfo dir in subinfos)
            {
                if (statuDictionary.Keys.Contains(dir.Name))
                {
                    //本地文件存在状态列表 才加载
                    DirectoryInfo[] companydirInfos = dir.GetDirectories();
                    foreach (var companydir in companydirInfos)
                    {
                        Data data = new Data();
                        data.FileResults = new List<Result>();
                        data.currStatus = statuDictionary[dir.Name];
                        data.stockAuditName = companydir.Name;
                        foreach (FileInfo f in companydir.GetFiles())
                        {
                            //遍历文件名
                            Result result = new Result();
                            result.fileTitle = System.IO.Path.GetFileNameWithoutExtension(f.FullName);
                            result.filePath = f.FullName;
                            result.CompanyStatus = dir.Name;
                            result.companyFullName = companydir.Name;
                            results.Add(result);
                            data.FileResults.Add(result);
                            total++;
                            //MessageBox.Show(result.localPath);
                        }
                        datas.Add(data);
                    }
                }
            }
            pageHelp.data = datas;
            fileRoot = new RootObject();
            companyRoot = new RootObject();
            fileRoot.result = results;
            fileRoot.pageHelp = new PageHelp(){total = total.ToString()};
            companyRoot.pageHelp = pageHelp;
            resultTemp = fileRoot.result.ToList();
            localData = true;
            comboBox2.SelectedIndex = 0;
            button1.Enabled = false;
            button2.Enabled = false;
            label5.Text = "高级筛选支持正则表达式\n本地模式部分功能不可使用";
            toolStripStatusLabel1.Text = "本地数据已加载";
            this.Text = "科创板信息披露文件采集器(本地模式)";
        }

        private void 关于ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            AboutBox abx = new AboutBox();
            abx.Show();
        }
    }
}
