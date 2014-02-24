#-*- coding: UTF-8 -*-

import re
import requests
import json
from datetime import datetime

print('Content-type: text/html\n')

print('<!DOCTYPE html>')
print ('<html xmlns="http://www.w3.org/1999/xhtml">')
print('<head>')
print('<title>which videos i want to see on sina nba site - mr3</title>')
print('<meta http-equiv="content-type" content="text/html; charset=gb2312" />')
print('''
<style type="text/css">body{color:#009;font:14px/20px "宋体";}
a:link{color:#175f9c;text-decoration:none;}
a:hover{color:#f00; text-decoration:underline;}
li{line-height: 26px;width:500px;}
li:hover{background-color:#dcf0fd;}
</style>''')
print('</head>')
print('<body>')
print('<ul>')
req = requests.get("http://roll.sports.sina.com.cn/api/news_list.php?tag=2&cat_1=nbavideo&&k=&show_num=120&page=1&r=0.7270777993835509")
result = req.content.decode("utf-8")

matches = re.search(r"{.*}",result)
if matches:
	j = json.loads(matches.group(0))
	for item in j["list"]:
		title = item["title"] 
		if "视频集锦" in title or "10佳球" in title or "官方5佳球" in title 
			print('''
				%s<li>
				<span style="width:400px;display:inline-block;"><a href="%s" target="_blank">%s</a></span>
				<span style="color:#8e8e8e;display:inline-block;text-align:right;width:80px;">%s</span>
				</li>''' % ("<br>" if "10佳球" in title else "",item["url"],title,datetime.fromtimestamp(int(item['time'])).strftime("%m-%d %H:%M")))
print('</ul>')
print ('</body>')
