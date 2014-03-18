__author__ = 'imr3.com'
#-*- coding: UTF-8 -*-

import re,requests,json,cgi,urllib.parse
from datetime import datetime,timedelta
from string import Template

def fTime(timestamp,format):
    return datetime.fromtimestamp(float(timestamp)).strftime(format)

def fileRead(file):
    with open(file) as f:
        return f.read()

def dateadd(t,days):
    return (t + timedelta(days=days))

def qs(key,defval):
    en = cgi.os.environ["QUERY_STRING"]
    qs = urllib.parse.parse_qs(en)
    return qs[key][0] if key in qs and qs[key][0] is not None else defval

def getSinaAPIResult():
    req = requests.get("http://roll.sports.sina.com.cn/api/news_list.php?tag=2&cat_1=nbavideo&&k=&show_num=" + qs("num","500") + "&page=1&r=0.7270777993835509")
    result = req.content.decode("utf-8")

    list_content = []
    td = datetime.today().day

    matches = re.search(r"(?<=\"list\":)\[.*}\]",result)
    if matches:
        j = json.loads(matches.group(0))
        for item in j:
            t = datetime.fromtimestamp(float(item['time']))
            if(dateadd(t,7).day == td): break

            title = item["title"]
            if re.search(r'视频集锦|官方\d{1,2}佳球',title):
                list_content.append('%(br)s<li><span><a href="%(url)s" target="_blank" title="%(title)s">%(title)s</a></span><span>%(time)s</span></li>' %
                                  ({"br":("<br>" if "佳球" in title else ""),"url":item["url"],"title":title,"time":t.strftime("%m-%d %H:%M")}))
    return "\n".join(list_content)

if __name__ == '__main__':
    print('Content-type: text/html\n')

    content = fileRead('sina_nba_template.html')
    temp = Template(content)
    print(temp.substitute(list_content=getSinaAPIResult()))