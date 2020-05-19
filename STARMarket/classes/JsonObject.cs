using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using RestSharp;
using RestSharp.Extensions;

namespace STARMarket.Classes
{
    public class ActionErrors
    {
    }

    public class ActionMessages
    {
    }

    public class ErrorMessages
    {
    }

    public class Errors
    {
    }

    public class FieldErrors
    {
    }

    public class I_person
    {
        public string i_p_personName { get; set; }
        public string i_p_jobType { get; set; }
        public string i_p_personId { get; set; }
        public string i_p_jobTitle { get; set; }
    }
    public class Intermediary
    {
        public string auditId { get; set; }
        public string i_intermediaryType { get; set; }
        public string i_intermediaryId { get; set; }
        public List<I_person> i_person { get; set; }
        public string i_intermediaryAbbrName { get; set; }
        public string i_intermediaryName { get; set; }
    }

    public class StockIssuer
    {
        public string auditId { get; set; }
        public string s_stockIssueId { get; set; }
        public string s_issueCompanyFullName { get; set; }
        public string s_csrcCode { get; set; }
        public string s_areaNameDesc { get; set; }
        public string s_companyCode { get; set; }
        public string s_personName { get; set; }
        public string s_personId { get; set; }
        public string s_jobTitle { get; set; }
        public string s_issueCompanyAbbrName { get; set; }
        public string s_csrcCodeDesc { get; set; }
        public string s_province { get; set; }
    }

    public class Data
    {
        public Data()
        {
            this.FileResults = new List<Result>();
        }
        public string fileUpdateTime { get; set; }
        public string filePath { get; set; }
        public string publishDate { get; set; }
        public string fileLastVersion { get; set; }
        public string stockAuditNum { get; set; }
        public string auditItemId { get; set; }
        public string filename { get; set; }
        public string companyFullName { get; set; }
        public string fileSize { get; set; }
        public string StockType { get; set; }
        public string fileTitle { get; set; }
        public string auditStatus { get; set; }
        public string fileVersion { get; set; }
        public string fileType { get; set; }
        public string MarketType { get; set; }
        public string fileId { get; set; }
        public string updateDate { get; set; }
        public string planIssueCapital { get; set; }
        public string wenHao { get; set; }
        public string stockAuditName { get; set; }
        public string currStatus { get; set; }
        public string registeResult { get; set; }
        public List<Intermediary> intermediary { get; set; }
        public string collectType { get; set; }
        public List<StockIssuer> stockIssuer { get; set; }
        public string createTime { get; set; }
        public string auditApplyDate { get; set; }
        public string issueAmount { get; set; }
        public string commitiResult { get; set; }
        public string issueMarketType { get; set; }
        // FileResults 用于绑定文件
        public List<Result> FileResults { get; set; }

        public override string ToString()
        {
            return this.stockAuditName;
        }
    }

    public class PageHelp
    {
        public string beginPage { get; set; }
        public string cacheSize { get; set; }
        public List<Data> data { get; set; }
        public string endDate { get; set; }
        public string endPage { get; set; }
        public string objectResult { get; set; }
        public string pageCount { get; set; }
        public string pageNo { get; set; }
        public string pageSize { get; set; }
        public string searchDate { get; set; }
        public string sort { get; set; }
        public string startDate { get; set; }
        public string total { get; set; }
    }

    public class Result
    {
        public static string localBasePath = Form1.localBasePath;
        public string fileUpdateTime { get; set; }
        public string filePath { get; set; }
        public string publishDate { get; set; }
        public string fileLastVersion { get; set; }
        public string stockAuditNum { get; set; }
        public string auditItemId { get; set; }
        public string filename { get; set; }
        public string companyFullName { get; set; }
        public string fileSize { get; set; }
        public string StockType { get; set; }
        public string fileTitle { get; set; }
        public string auditStatus { get; set; }
        public string fileVersion { get; set; }
        public string fileType { get; set; }
        public string MarketType { get; set; }
        public string fileId { get; set; }
        public string CompanyStatus { get; set; }
        public Data CompanyData { get; set; }

        public string localPath
        {
            get
            {
                //返回本地保存路径
                string[] temp = filePath.Split('.');
                string format = temp[temp.Length - 1];
                string path = localBasePath + "\\" + CompanyStatus + "\\" + companyFullName + "\\" + fileTitle + "." + format;
                return path;
            }

        }

        public void downlaod()
        {
            //下载文件
            var client = new RestClient("http://static.sse.com.cn/stock");
            var request = new RestRequest(filePath,Method.GET);
            client.DownloadData(request).SaveAs(localPath);

        }

        public void downlaod(object sender, DoWorkEventArgs e)
        {
            //下载文件
            this.downlaod();
            e.Result = fileTitle + " 缓存成功";

        }

        public void mkdir()
        {
            //创建目录
            DirectoryInfo dir = new DirectoryInfo(localBasePath);
            if (!dir.Exists)
            {
                //创建子目录
                dir.Create();
                dir = new DirectoryInfo(localBasePath + "\\"+CompanyStatus);
                dir.Create();
                dir = new DirectoryInfo(localBasePath + "\\" + CompanyStatus + "\\" + companyFullName);
                dir.Create();
            }
            else
            {
                dir = new DirectoryInfo(localBasePath + "\\" + CompanyStatus);
                if (!dir.Exists)
                {
                    dir.Create();
                    dir = new DirectoryInfo(localBasePath + "\\" + CompanyStatus + "\\" + companyFullName);
                    dir.Create();
                }
                else
                {
                    dir = new DirectoryInfo(localBasePath + "\\" + CompanyStatus + "\\" + companyFullName);
                    if (!dir.Exists) dir.Create();
                }
            }
        }

        public bool Exists
        {
            //返回文件是否已经下载
            get
            {
                FileInfo file = new FileInfo(localPath);
                return file.Exists;
            }
        }
    }

    public class RootObject
    {
        public List<ActionErrors> actionErrors { get; set; }
        public List<ActionMessages> actionMessages { get; set; }
        public List<ErrorMessages> errorMessages { get; set; }
        public Errors errors { get; set; }
        public FieldErrors fieldErrors { get; set; }
        public string isPagination { get; set; }
        public string jsonCallBack { get; set; }
        public string locale { get; set; }
        public PageHelp pageHelp { get; set; }
        public List<Result> result { get; set; }
        public string texts { get; set; }
        public string type { get; set; }
        public string validateCode { get; set; }
    }
}
