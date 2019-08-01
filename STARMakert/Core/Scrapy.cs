using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Newtonsoft.Json;
using RestSharp;
using STARMakert.Classes;

namespace STARMakert.Core
{
    class Scrapy
    {
        private static string queryUrl = "http://query.sse.com.cn/";
        public static RootObject GetFiles()
        {
            RootObject rb;
            var client = new RestClient(queryUrl);
            var request = new RestRequest("commonSoaQuery.do?sqlId=GP_GPZCZ_SHXXPL&fileType=30%2C5%2C6&", Method.GET);
            request.AddHeader("Referer", "http://kcb.sse.com.cn/disclosure/");
            request.AddHeader("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/75.0.3770.142 Safari/537.36");
            var response = client.Execute(request);
            rb = JsonConvert.DeserializeObject<RootObject>(response.Content);
            return rb;

        }

        public static RootObject GetCompany()
        {
            var client = new RestClient(queryUrl);
            var request = new RestRequest("commonSoaQuery.do?sqlId=SH_XM_LB", Method.GET);
            request.AddHeader("Referer", "http://kcb.sse.com.cn/renewal/");
            request.AddHeader("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/75.0.3770.142 Safari/537.36");
            var response = client.Execute(request);
            RootObject rootObject = JsonConvert.DeserializeObject<RootObject>(response.Content);
            return rootObject;
        }
    }
}
