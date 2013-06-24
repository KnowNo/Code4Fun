using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Text;
using System.Web;
using System.Web.Security;
using System.Xml;
using System.Xml.Serialization;

/// <summary>
/// wechat 的摘要说明
/// </summary>
public class wechat : IHttpHandler
{
    private const string Token = "Your Token";

    public void ProcessRequest(HttpContext context)
    {
        context.Response.ContentType = "text/plain";

        if (context.Request.RequestType.Equals("GET"))
        {
            var echoStr = context.Request.QueryString["echoStr"];
            if (CheckSignature() && echoStr.Length > 0)
            {
                WriteLog(echoStr, "echoStr");

                context.Response.Write(echoStr);
                context.Response.End();
            }
        }

        if (context.Request.RequestType.Equals("POST"))
        {
            var stream = context.Request.InputStream;
            var streambyte = new byte[stream.Length];
            stream.Read(streambyte, 0, (int)stream.Length);
            var strPostMsg = Encoding.UTF8.GetString(streambyte);

            WriteLog(strPostMsg, "PostMsg");

            if (strPostMsg.Length > 0)
            {
                var config = XmlDeserialize<WeChatMessageModel>(strPostMsg);
                var responseMsg = new WeChatMessageModel
                    {
                        ToUserName = config.FromUserName,
                        FromUserName = config.ToUserName,
                        CreateTime = ConvertDateTimeInt(DateTime.Now),
                        FuncFlag = 0
                    };

                switch (config.MsgType)
                {
                    case WeChatMessageModel.EumMsgType.Text:
                        responseMsg.MsgType = WeChatMessageModel.EumMsgType.Text;
                        if (config.Content.Substring(0, 3).Trim().ToUpper() == "TXL")
                        {
                            responseMsg.Content = GetTxlMemberInfo(config.Content.Substring(3).Trim());
                        }
                        else
                        {
                            responseMsg.Content = GetNodeInnerText("ResponseMsg", "/ResponseMsg/" + WeChatMessageModel.EumMsgType.Text);
                        }
                        break;
                    case WeChatMessageModel.EumMsgType.Event:
                        responseMsg.MsgType = WeChatMessageModel.EumMsgType.Text;
                        switch (config.Event)
                        {
                            case WeChatMessageModel.EumEventType.Subscribe:
                                responseMsg.Content = GetNodeInnerText("ResponseMsg", "/ResponseMsg/" + WeChatMessageModel.EumEventType.Subscribe);
                                break;
                            case WeChatMessageModel.EumEventType.Unsubscribe:
                                responseMsg.Content = GetNodeInnerText("ResponseMsg", "/ResponseMsg/" + WeChatMessageModel.EumEventType.Unsubscribe);
                                break;
                        }
                        break;
                }
                var responseValue = GetResponseXml(responseMsg);

                WriteLog(responseValue, "ResponseMsg");

                context.Response.Write(responseValue);
                context.Response.End();
            }
        }
    }

    ///<summary>
    /// 
    /// 验证微信签名
    /// 
    /// * 将token、timestamp、nonce三个参数进行字典序排序
    /// * 将三个参数字符串拼接成一个字符串进行sha1加密
    /// * 开发者获得加密后的字符串可与signature对比，标识该请求来源于微信。
    /// 
    /// </summary>
    /// <returns></returns>
    private static bool CheckSignature()
    {
        var signature = HttpContext.Current.Request.QueryString["signature"];
        var timestamp = HttpContext.Current.Request.QueryString["timestamp"];
        var nonce = HttpContext.Current.Request.QueryString["nonce"];

        var sbLog = new StringBuilder();
        sbLog.AppendLine("TIME:" + DateTime.Now);
        sbLog.AppendLine("signature:" + signature);
        sbLog.AppendLine("timestamp:" + timestamp);
        sbLog.AppendLine("nonce:" + nonce);
        sbLog.AppendLine("=====================================");
        WriteLog(sbLog.ToString(), "CheckSignature");

        string[] arrTmp = { Token, timestamp, nonce };
        Array.Sort(arrTmp); //字典排序
        var tmpStr = string.Join("", arrTmp);
        tmpStr = FormsAuthentication.HashPasswordForStoringInConfigFile(tmpStr, "SHA1");
        return tmpStr != null && tmpStr.ToLower() == signature;
    }

    /// <summary>
    /// 写入日志
    /// </summary>
    /// <param name="value">要写入的字符串</param>
    /// <param name="directoryName">目录名</param>
    /// <param name="fileName">文件名</param>
    private static void WriteLog(string value, string directoryName = "", string fileName = "")
    {
        directoryName = HttpContext.Current.Server.MapPath(string.Format("log\\{0}\\", directoryName));
        if (!Directory.Exists(directoryName))
        {
            Directory.CreateDirectory(directoryName);
        }

        var filePath = string.Format("{0}{1}{2}.xml", directoryName, fileName, DateTime.Now.ToString("yyyyMMdd"));

        //写入文件
        using (var writer = new StreamWriter(filePath, true, Encoding.UTF8))
        {
            writer.WriteLine(value);
            writer.Flush();
            writer.Close();
        }
    }

    /// <summary>
    /// 读取文本
    /// </summary>
    /// <param name="fileName">文件名称</param>
    /// <returns></returns>
    private static string ReadText(string fileName)
    {
        var strRead = string.Empty;
        fileName = HttpContext.Current.Server.MapPath(string.Format("message\\{0}.txt", fileName));
        if (File.Exists(fileName))
        {
            using (var reader = File.OpenText(fileName))
            {
                strRead = reader.ReadToEnd();
                reader.Close();
            }
        }
        else
        {   //写入文件
            using (var writer = new StreamWriter(fileName))
            {
                writer.Close();
            }
        }
        return strRead;
    }

    /// <summary>
    /// 构造回复文本消息xml结构
    /// </summary>
    /// <param name="config">消息实例</param>
    /// <returns></returns>
    private static string GetResponseXml(WeChatMessageModel config)
    {
        var text = new StringBuilder();
        text.AppendLine("<xml>");

        text.AppendFormat("<{0}><![CDATA[{1}]]></{0}>", "ToUserName", config.ToUserName);
        text.AppendFormat("<{0}><![CDATA[{1}]]></{0}>", "FromUserName", config.FromUserName);
        text.AppendFormat("<{0}>{1}</{0}>", "CreateTime", config.CreateTime);
        text.AppendFormat("<{0}><![CDATA[{1}]]></{0}>", "MsgType", config.MsgType);

        switch (config.MsgType)
        {
            case WeChatMessageModel.EumMsgType.Text:
                text.AppendFormat("<{0}><![CDATA[{1}]]></{0}>", "Content", config.Content);
                break;
            case WeChatMessageModel.EumMsgType.Music:
                text.AppendLine("<Music>");
                text.AppendFormat("<{0}><![CDATA[{1}]]></{0}>", "Music", config.Music.Title);
                text.AppendFormat("<{0}><![CDATA[{1}]]></{0}>", "Description", config.Music.Description);
                text.AppendFormat("<{0}><![CDATA[{1}]]></{0}>", "MusicUrl", config.Music.MusicUrl);
                text.AppendFormat("<{0}><![CDATA[{1}]]></{0}>", "HQMusicUrl", config.Music.HQMusicUrl);
                text.AppendLine("</Music>");
                break;
            case WeChatMessageModel.EumMsgType.News:
                text.AppendFormat("<{0}>{1}</{0}>", "ArticleCount", config.Articles.Count);
                text.AppendLine("<Articles>");
                foreach (var item in config.Articles)
                {
                    text.AppendLine("<item>");
                    text.AppendFormat("<{0}><![CDATA[{1}]]></{0}>", "Title", item.Title);
                    text.AppendFormat("<{0}><![CDATA[{1}]]></{0}>", "Description", item.Description);
                    text.AppendFormat("<{0}><![CDATA[{1}]]></{0}>", "PicUrl", item.PicUrl);
                    text.AppendFormat("<{0}><![CDATA[{1}]]></{0}>", "Url", item.Url);
                    text.AppendLine("</item>");
                }
                text.AppendLine("</Articles>");
                break;
        }
        text.AppendFormat("<{0}>{1}</{0}>", "FuncFlag", config.FuncFlag);
        text.AppendLine("</xml>");
        return text.ToString();
    }

    /// <summary>
    /// 构造回复文本消息xml结构
    /// </summary>
    /// <param name="dic">消息集合字典</param>
    /// <returns></returns>
    private static string GetEventResponseText(Dictionary<string, string> dic)
    {
        var responseText = new StringBuilder();
        responseText.AppendLine("<xml>");
        foreach (KeyValuePair<string, string> keyValue in dic)
        {
            responseText.AppendFormat("<{0}><![CDATA[{1}]]></{0}>", keyValue.Key, keyValue.Value);
        }
        responseText.AppendLine("</xml>");
        return responseText.ToString();
    }

    #region XML操作相关
    /// <summary>
    /// XML反序列化
    /// </summary>
    /// <typeparam name="T">对象类型</typeparam>
    /// <param name="content">内容文本</param>
    /// <returns></returns>
    private static T XmlDeserialize<T>(string content)
    {
        var serializer = new XmlSerializer(typeof(T));
        using (var reader = new StringReader(content))
        {
            return (T)serializer.Deserialize(reader);
        }
    }

    /// <summary>
    /// XML序列化
    /// </summary>
    /// <typeparam name="T">对象类型</typeparam>
    /// <param name="t">对象实例</param>
    private static string XmlSerialize<T>(T t)
    {
        var serializer = new XmlSerializer(typeof(T));
        //serializer.Serialize(HttpContext.Current.Response.Output, t);
        var stream = new MemoryStream();
        serializer.Serialize(stream, t);
        var result = Encoding.UTF8.GetString(stream.GetBuffer());
        stream.Close();
        return result;
    }

    /// <summary>
    /// 获取通讯录成员信息
    /// </summary>
    /// <param name="xmlName">xml名称</param>
    /// <param name="memberName">节点名称</param>
    /// <returns></returns>
    public static string GetTxlMemberInfo(string xmlName, string memberName)
    {
        var node = GetNode(xmlName, string.Format("/TXL/Member[Name='{0}']", memberName));

        if (node == null) { return "查不到此记录,请检测输入信息是否有误！"; }

        var childNodes = node.ChildNodes;

        var strOut = new StringBuilder();
        strOut.AppendLine("查询结果\n");
        strOut.AppendLine(string.Format("姓名：{0}", childNodes[0].InnerText));
        strOut.AppendLine(string.Format("手机：{0}", childNodes[1].InnerText));
        strOut.AppendLine(string.Format("ＱＱ：{0}", childNodes[2].InnerText));
        strOut.AppendLine(string.Format("现居：{0}", childNodes[3].InnerText));
        strOut.AppendLine(string.Format("籍贯：{0}", childNodes[4].InnerText));
        strOut.AppendLine(string.Format("备注：{0}", childNodes[5].InnerText));
        strOut.AppendLine(GetNodeInnerText("ResponseMsg", string.Format("/ResponseMsg/{0}", xmlName)));
        return strOut.ToString();
    }

    /// <summary>
    /// 获取XML节点集合
    /// </summary>
    /// <param name="xmlPath">节点名称</param>
    /// <param name="xpath">XPath 表达式</param>
    /// <returns></returns>
    private static XmlNodeList GetNodeList(string xmlPath, string xpath)
    {
        var xmlDoc = new XmlDocument();
        xmlPath = HttpContext.Current.Server.MapPath(string.Format("XmlData\\{0}.xml", xmlPath));
        XmlNodeList nodeList = null;
        if (File.Exists(xmlPath))
        {
            xmlDoc.Load(xmlPath);
            var xmlDocEle = xmlDoc.DocumentElement;
            if (xmlDocEle != null) { nodeList = xmlDocEle.SelectNodes(xpath); }
        }
        return nodeList;
    }

    /// <summary>
    /// 获取XML节点
    /// </summary>
    /// <param name="xmlPath">节点名称</param>
    /// <param name="xpath">XPath 表达式</param>
    /// <returns></returns>
    private static XmlNode GetNode(string xmlPath, string xpath)
    {
        var xmlDoc = new XmlDocument();
        xmlPath = HttpContext.Current.Server.MapPath(string.Format("XmlData\\{0}.xml", xmlPath));
        XmlNode node = null;
        if (File.Exists(xmlPath))
        {
            xmlDoc.Load(xmlPath);
            var xmlDocEle = xmlDoc.DocumentElement;
            if (xmlDocEle != null) { node = xmlDocEle.SelectSingleNode(xpath); }
        }
        return node;
    }

    /// <summary>
    /// 获取XML节点
    /// </summary>
    /// <param name="xmlPath"></param>
    /// <param name="xpath"></param>
    /// <returns></returns>
    private static string GetNodeInnerText(string xmlPath, string xpath)
    {
        var node = GetNode(xmlPath, xpath);
        return node == null ? "查不到此记录，请检测输入信息是否有误！" : node.InnerText;
    }

    #endregion

    #region 时间转换相关
    /// <summary>
    /// datetime转换为unix time
    /// </summary>
    /// <param name="time">时间</param>
    /// <returns></returns>
    private static int ConvertDateTimeInt(DateTime time)
    {
        var startTime = TimeZone.CurrentTimeZone.ToLocalTime(new DateTime(1970, 1, 1));
        return (int)(time - startTime).TotalSeconds;
    }

    /// <summary>
    /// unix时间转换为datetime
    /// </summary>
    /// <param name="timeStamp"></param>
    /// <returns></returns>
    private static DateTime UnixTimeToTime(string timeStamp)
    {
        var dtStart = TimeZone.CurrentTimeZone.ToLocalTime(new DateTime(1970, 1, 1));
        var lTime = long.Parse(timeStamp + "0000000");
        var toNow = new TimeSpan(lTime);
        return dtStart.Add(toNow);
    }

    #endregion

    public bool IsReusable
    {
        get { return false; }
    }

}

[Serializable]
[XmlRoot(ElementName = "xml")]
public class WeChatMessageModel
{
    /// <summary>
    /// 音乐消息实例
    /// </summary>
    public WeChatMusicModel Music { get; set; }

    /// <summary>
    /// 多条图文消息信息，默认第一个item为大图
    /// </summary>
    [XmlArrayItem(ElementName = "item")]
    public List<WeChatNewsModel> Articles { get; set; }

    /// <summary>
    /// 接收方微信号
    /// </summary>
    public string ToUserName { get; set; }

    /// <summary>
    /// 发送方微信号
    /// <remarks>若为普通用户，则是一个OpenID / 开发者微信号</remarks>
    /// </summary>
    public string FromUserName { get; set; }

    /// <summary>
    /// 消息创建时间 （整型） 
    /// </summary>
    public int CreateTime { get; set; }

    /// <summary>
    /// 消息类型
    /// </summary>
    public EumMsgType MsgType { get; set; }

    /// <summary>
    /// 消息id，64位整型
    /// </summary>
    public long MsgId { get; set; }

    /// <summary>
    /// 位0x0001被标志时，星标刚收到的消息。
    /// </summary>
    public int FuncFlag { get; set; }

    #region 文本消息

    /// <summary>
    /// 回复的消息内容,长度不超过2048字节
    /// </summary>
    [StringLength(2048)]
    public string Content { get; set; }

    #endregion

    #region 图片消息

    /// <summary>
    /// 图片链接
    /// </summary>
    public string PicUrl { get; set; }

    #endregion

    #region 地理位置消息

    /// <summary>
    /// 地理位置纬度
    /// </summary>
    [XmlElement(ElementName = "Location_X")]
    public string LocationX { get; set; }

    /// <summary>
    /// 地理位置经度
    /// </summary>
    [XmlElement(ElementName = "Location_Y")]
    public string LocationY { get; set; }

    /// <summary>
    /// 地图缩放大小
    /// </summary>
    public int Scale { get; set; }

    /// <summary>
    /// 地理位置信息
    /// </summary>
    [XmlElement(ElementName = "Label")]
    public string LocationInfo { get; set; }

    #endregion

    #region 链接消息

    /// <summary>
    /// 消息标题
    /// </summary>
    public string Title { get; set; }

    /// <summary>
    /// 消息描述
    /// </summary>
    public string Description { get; set; }

    /// <summary>
    /// 消息链接
    /// </summary>
    public string Url { get; set; }

    #endregion

    #region 图文消息

    /// <summary>
    /// 图文消息个数，限制为10条以内
    /// </summary>
    [StringLength(10)]
    public int ArticleCount { get; set; }

    #endregion

    #region 事件推送

    /// <summary>
    /// 事件类型，subscribe(订阅)、unsubscribe(取消订阅)、CLICK(自定义菜单点击事件)
    /// </summary>
    public EumEventType Event { get; set; }

    /// <summary>
    /// 事件KEY值，与自定义菜单接口中KEY值对应
    /// </summary>
    public string EventKey { get; set; }

    #endregion

    /// <summary>
    /// 消息类型枚举
    /// </summary>
    public enum EumMsgType
    {
        /// <summary>
        /// 文本消息
        /// </summary>
        [XmlEnum(Name = "text")]
        Text,

        /// <summary>
        /// 图片消息
        /// </summary>
        [XmlEnum(Name = "image")]
        Image,

        /// <summary>
        /// 音乐消息
        /// </summary>
        [XmlEnum(Name = "music")]
        Music,

        /// <summary>
        /// 图文消息
        /// </summary>
        [XmlEnum(Name = "news")]
        News,

        /// <summary>
        /// 地理位置消息
        /// </summary>
        [XmlEnum(Name = "location")]
        Location,

        /// <summary>
        /// 事件消息
        /// </summary>
        [XmlEnum(Name = "event")]
        Event
    }

    /// <summary>
    /// 事件类型枚举
    /// </summary>
    public enum EumEventType
    {
        /// <summary>
        /// 订阅
        /// </summary>
        [XmlEnum(Name = "subscribe")]
        Subscribe,

        /// <summary>
        /// 取消订阅
        /// </summary>
        [XmlEnum(Name = "unsubscribe")]
        Unsubscribe,

        /// <summary>
        /// 自定义菜单点击事件
        /// </summary>
        [XmlEnum(Name = "CLICK")]
        Click
    }
}

/// <summary>
/// 音乐消息实体
/// </summary>
[Serializable]
public class WeChatMusicModel
{
    /// <summary>
    /// 消息标题
    /// </summary>
    public string Title { get; set; }

    /// <summary>
    /// 消息描述
    /// </summary>
    public string Description { get; set; }

    /// <summary>
    /// 音乐链接
    /// </summary>
    public string MusicUrl { get; set; }

    /// <summary>
    /// 高质量音乐链接，WIFI环境优先使用该链接播放音乐
    /// </summary>
    public string HQMusicUrl { get; set; }
}

/// <summary>
/// 图文消息
/// </summary>
[Serializable]
public class WeChatNewsModel
{
    /// <summary>
    /// 图文消息标题
    /// </summary>
    public string Title { get; set; }

    /// <summary>
    /// 图文消息描述
    /// </summary>
    public string Description { get; set; }

    /// <summary>
    /// 图片链接，支持JPG、PNG格式，较好的效果为大图640*320，小图80*80。
    /// </summary>
    public string PicUrl { get; set; }

    /// <summary>
    /// 点击图文消息跳转链接
    /// </summary>
    public string Url { get; set; }
}