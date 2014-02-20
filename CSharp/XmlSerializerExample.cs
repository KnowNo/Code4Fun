using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml.Serialization;

namespace XmlSerializerExample
{
    class XmlSerializerExample
    {
        static void Main(string[] args)
        {
            #region 反序列化
//            //回复图文消息
//            var xmlContent = @"<xml>
//                                <ToUserName><![CDATA[toUser]]></ToUserName>
//                                <FromUserName><![CDATA[fromUser]]></FromUserName>
//                                <CreateTime>12345678</CreateTime>
//                                <MsgType><![CDATA[news]]></MsgType>
//                                <Content><![CDATA[]]></Content>
//                                <ArticleCount>2</ArticleCount>
//                                <Articles>
//                                <item>
//                                <Title><![CDATA[title1]]></Title>
//                                <Description><![CDATA[description1]]></Description>
//                                <PicUrl><![CDATA[picurl]]></PicUrl>
//                                <Url><![CDATA[url]]></Url>
//                                </item>
//                                <item>
//                                <Title><![CDATA[title]]></Title>
//                                <Description><![CDATA[description]]></Description>
//                                <PicUrl><![CDATA[picurl]]></PicUrl>
//                                <Url><![CDATA[url]]></Url>
//                                </item>
//                                </Articles>
//                                <FuncFlag>1</FuncFlag>
//                                </xml>";
//            var config = XmlDeserialize<WeiXinConfig>(xmlContent);

//            Console.WriteLine(config.ToUserName + "\n" + config.MsgType);
            #endregion

            #region 序列化

            var lstArticles = new List<WeixinArticle>
                {
                    new WeixinArticle
                        {
                            Title = "小举",
                            Description = "小举描述",
                            PicUrl = "smallju.jpg",
                            Url = "smallju.com"
                        },
                       new WeixinArticle
                        {
                            Title = "bigju",
                            Description = "大头举描述",
                            PicUrl = "bigju.jpg",
                            Url = "bigju.com"
                        }
                };

            var config = new WeiXinConfig
                {
                    ToUserName = "bigju",
                    FromUserName = "huai",
                    CreateTime = "123456789",
                    MsgType = "news",
                    ArticleCount = lstArticles.Count,
                    Articles = lstArticles,
                    FuncFlag = 1
                };
            Console.WriteLine(XmlSerialize<WeiXinConfig>(config));

            #endregion

            Console.Read();
        }

        /// <summary>
        /// XML反序列化
        /// </summary>
        /// <typeparam name="T">对象类型</typeparam>
        /// <param name="content">内容文本</param>
        /// <returns></returns>
        static T XmlDeserialize<T>(string content)
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
        static string XmlSerialize<T>(T t)
        {
            var serializer = new XmlSerializer(typeof(T));
            var stream = new MemoryStream();
            serializer.Serialize(stream, t);
            return Encoding.UTF8.GetString(stream.GetBuffer());

            //写入文件
            //using (StreamWriter writer = new StreamWriter(AppDomain.CurrentDomain.BaseDirectory + "weixin.xml"))
            //{
            //    serializer.Serialize(writer, t);
            //}

            //直接输出
            //serializer.Serialize(Console.Out, t);
        }
    }

    [Serializable]
    [XmlRoot(ElementName = "xml")]
    public class WeiXinConfig
    {
        /// <summary>
        /// 消息接收方微信号，一般为公众平台帐号微信号
        /// </summary>
        public string ToUserName { get; set; }

        /// <summary>
        /// 消息发送方微信号
        /// </summary>
        public string FromUserName { get; set; }

        /// <summary>
        /// 消息创建时间
        /// </summary>
        public string CreateTime { get; set; }

        /// <summary>
        /// 消息类型
        /// </summary>
        public string MsgType { get; set; }

        /// <summary>
        /// 消息内容
        /// </summary>
        public string Content { get; set; }

        /// <summary>
        /// 图文消息个数，限制为10条以内
        /// </summary>
        public int ArticleCount { get; set; }

        /// <summary>
        /// 微信文章
        /// </summary>
        [XmlArrayItem(ElementName = "item")]
        public List<WeixinArticle> Articles { get; set; }

        /// <summary>
        /// 星标消息分类
        /// </summary>
        public int FuncFlag { get; set; }
    }

    [Serializable]
    public class WeixinArticle
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
        /// 图片链接，支持JPG、PNG格式，较好的效果为大图640*320，小图80*80，限制图片链接的域名需要与开发者填写的基本资料中的Url一致
        /// </summary>
        public string PicUrl { get; set; }

        /// <summary>
        /// 点击图文消息跳转链接
        /// </summary>
        public string Url { get; set; }
    }
}
