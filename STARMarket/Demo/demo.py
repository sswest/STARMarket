#!/usr/bin/env python
# -*- coding:utf-8 -*-
# Created by 咸鱼 at 2019/7/29 15:03
import requests
import os
from Threadpool import Threadpool

headers = {
    "Referer":"http://kcb.sse.com.cn/disclosure/",
    "User-Agent":"Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/75.0.3770.142 Safari/537.36",
}

'''
API参数解析：
jsonCallBack    json回调函数 直接省略
isPagination    布尔值 功能未知  省略不影响返回结果?
sqlId           固定值GP_GPZCZ_SHXXPL
fileVersion     文件参数 功能位置
pageSize        一页显示数据量 不能留空（可以通过返回值来确认共有多少数据）
fileVersion
'''
url = "http://query.sse.com.cn/commonSoaQuery.do?jsonCallBack=&isPagination=&sqlId=GP_GPZCZ_SHXXPL&fileVersion=&pageHelp.pageSize={size}&fileType=30%2C5%2C6&_=1"

class Fileinfo:
    '''
    记录披露文件
    '''
    static_path = "http://static.sse.com.cn/stock"
    file_path = "信息披露数据库"


    def __init__(self,dic):
        '''
        解析json
        :param dic: 直接将单个json传递过来即可
        '''

        self.company = dic.get("companyFullName","不知名的小公司")
        self.filepath = dic.get("filePath",None)
        self.fileformat = self.filepath.split(".")[-1]
        self.filename = "".join([dic.get("fileTitle"),".",self.fileformat])
        self.filetype = dic.get("fileType")


    def download(self):
        '''
        下载披露文件
        :return:
        '''
        if not os.path.exists(self.file_path):
            os.mkdir(self.file_path)
        if not os.path.exists(os.path.join(self.file_path,self.company)):
            os.mkdir(os.path.join(self.file_path,self.company))
        if os.path.exists(os.path.join(os.path.dirname(os.path.abspath(__file__)),self.file_path,self.company,self.filename)):
            # 已下载的文件跳过
            return
        r = requests.get(url = self.static_path + self.filepath,stream = True)
        with open(os.path.join(os.path.dirname(os.path.abspath(__file__)),self.file_path,self.company,self.filename),"wb") as f:
            for chunk in r.iter_content(chunk_size = 512):
                f.write(chunk)
        print("【%s】 %s 下载成功" % (self.company,self.filename))

alllist = []
r= requests.get(url.format(size=1),headers=headers)
res = r.json()
total = res['pageHelp']['total']
r= requests.get(url.format(size=total),headers=headers)
res = r.json()
result = res.get("result",None)
for i in result:
    fi = Fileinfo(i)
    alllist.append(fi)

pool = Threadpool(10)
for fi in alllist:
    print(i)
    pool.apply(target=fi.download)
pool.join()
