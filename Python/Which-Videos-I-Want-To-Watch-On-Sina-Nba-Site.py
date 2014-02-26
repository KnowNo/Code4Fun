#-*- coding: UTF-8 -*-

import re
import requests
import json
from datetime import datetime

print('Content-type: text/html\n')

print('<!DOCTYPE html>')
print ('<html xmlns="http://www.w3.org/1999/xhtml">')
print('<head>')
print('<title>Which Videos I Want To See On Sina Nba Site - mr3</title>')
print('<meta http-equiv="content-type" content="text/html; charset=gb2312" />')
print('''<style type="text/css">
h2,ul,body{margin:0}
body{font:14px/20px "宋体";}
a:link{color:#175f9c;text-decoration:none;}
a:hover{color:#f00; text-decoration:underline;}
li{line-height: 26px;width:500px;}
li:hover{background-color:#dcf0fd;}
li span:first-child{ width:400px;display:inline-block; }
li span:last-child{ color:#8e8e8e;display:inline-block;text-align:right;width:80px; }
</style>''')
print('</head>')
print('<body>')
print('<h2>Which Videos I Want To See On Sina Nba Site</h2>')
print('<ul>')
req = requests.get("http://roll.sports.sina.com.cn/api/news_list.php?tag=2&cat_1=nbavideo&&k=&show_num=120&page=1&r=0.7270777993835509")
result = req.content.decode("utf-8")

matches = re.search(r"(?<=\"list\":)\[.*}\]",result)
if matches:
	j = json.loads(matches.group(0))
	for item in j:
		title = item["title"]
		if re.search(r'视频集锦|官方\d{1,2}佳球',title):
			print('''%s<li><span><a href="%s" target="_blank">%s</a></span><span>%s</span></li>''' % ("<br>" if "佳球" in title else "",item["url"],title,datetime.fromtimestamp(float(item['time'])).strftime("%m-%d %H:%M")))
print('</ul>')
print ('</body>')