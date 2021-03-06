﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Net;
using Newtonsoft.Json;
using System.IO;
using Newtonsoft.Json.Linq;
using System.Threading;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;

namespace Bilibili_Live_Helper
{
    public partial class ToolsPage :MetroFramework.Forms.MetroForm
    {
        public ToolsPage()
        {
            InitializeComponent();
            //初始化控件
            //控件双缓存，有效优化ListView闪烁
            listView1.GetType().GetProperty("DoubleBuffered", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(this.listView1, true, null);
            listView2.GetType().GetProperty("DoubleBuffered", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(this.listView2, true, null);
            metroTabControl1.ItemSize = new Size((metroTabControl1.Size.Width-20)/4,35);
            metroTabControl2.Visible = true;
        }
        private void ToolsPage_Load(object sender, EventArgs e)
        {
            //初始化主页         
            //先加载一次背包
            LoadBagList();
            //加载用户信息
            LoadUserInfo();
            //加载用户勋章
            LoadMedalList();
            //无限刷新背包
            this.timer1.Start();
        }

        private void button1_Click(object sender, EventArgs e)
        {
        }



        private void button1_Click_1(object sender, EventArgs e)
        {
        }

        private void LoadUserInfo()
        {
            try
            {
                string userInfoUrl = "https://api.live.bilibili.com/User/getUserInfo";
                JObject jo = JObject.Parse(BiliHelper.BiliGetRequest(userInfoUrl, BiliHelper.Data.BiliCookie));
                this.Text = (string)jo["data"]["uname"];
                this.lb_BiliCoin.Text = (string)jo["data"]["billCoin"];
                this.lb_Gold.Text = (string)jo["data"]["gold"];
                this.lb_Silver.Text = (string)jo["data"]["silver"];
                this.pic_Logo.ImageLocation = (string)jo["data"]["face"];
                //保存Cookie，方便下次登录
                string savaPath = System.IO.Directory.GetCurrentDirectory() + @"\账号库\";
                //先判断有没有这个文件夹，没有就创建。
                if (!Directory.Exists(savaPath))
                {
                    Directory.CreateDirectory(savaPath);
                }
                BinaryFormatter bf = new BinaryFormatter();
                using (FileStream fs = new FileStream(savaPath + (string)jo["data"]["uname"] + ".dat", FileMode.Create))
                {
                    //将Cookie序列化，并且保存到指定的流
                    bf.Serialize(fs, BiliHelper.Data.BiliCookie);
                }
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
            }

        }
        private void LoadBagList()
        {
            try
            {
                this.listView1.Items.Clear();
                string bagHost = "https://api.live.bilibili.com/xlive/web-room/v1/gift/bag_list";
                JObject bagJson = JObject.Parse(BiliHelper.BiliGetRequest(bagHost, BiliHelper.Data.BiliCookie));
                int bagCount = bagJson["data"]["list"].Count();//背包礼物种类数量
                //加载背包
                for (int i = 0; i < bagCount; i++)
                {
                    ListViewItem li = new ListViewItem(bagJson["data"]["list"][i]["bag_id"].ToString());
                    li.SubItems.Add(bagJson["data"]["list"][i]["gift_id"].ToString());
                    li.SubItems.Add(bagJson["data"]["list"][i]["gift_name"].ToString());
                    li.SubItems.Add(bagJson["data"]["list"][i]["gift_num"].ToString());
                    li.SubItems.Add(bagJson["data"]["list"][i]["corner_mark"].ToString());
                    this.listView1.Items.Add(li);
                }
                //背包列表自适应大小
                for (int i = 0; i < this.listView1.Columns.Count; i++)
                {
                    this.listView1.Columns[i].Width = this.listView1.ClientSize.Width / this.listView1.Columns.Count;
                }
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
            }
        }

        private void LoadMedalList()
        {
            try
            {
                this.listView2.Items.Clear();
                string medalHost = "https://api.live.bilibili.com/i/api/medal?page=1&pageSize=100";
                JObject medalJson = JObject.Parse(BiliHelper.BiliGetRequest(medalHost, BiliHelper.Data.BiliCookie));
                int medalCount = medalJson["data"]["fansMedalList"].Count();
                for (int i = 0; i < medalCount; i++)
                {
                    string medalName = medalJson["data"]["fansMedalList"][i]["medal_name"].ToString();
                    string medalLevel = medalJson["data"]["fansMedalList"][i]["level"].ToString();
                    string medalColor = medalJson["data"]["fansMedalList"][i]["medal_color"].ToString();
                    string medalCurrentIntimacy = medalJson["data"]["fansMedalList"][i]["intimacy"].ToString();
                    string medalTotalIntimacy = medalJson["data"]["fansMedalList"][i]["next_intimacy"].ToString();
                    string medalTodayFeed = medalJson["data"]["fansMedalList"][i]["todayFeed"].ToString();
                    string medalTodayFeedLimit = medalJson["data"]["fansMedalList"][i]["dayLimit"].ToString();
                    string medalUpName = medalJson["data"]["fansMedalList"][i]["target_name"].ToString();
                    ListViewItem li = new ListViewItem(medalLevel);
                    li.UseItemStyleForSubItems = false;
                    li.SubItems.Add(medalName);
                    li.SubItems.Add(medalCurrentIntimacy + "/" + medalTotalIntimacy);
                    li.SubItems.Add(medalTodayFeed + "/" + medalTodayFeedLimit);
                    li.SubItems.Add(medalUpName);
                    li.BackColor = Color.FromArgb(int.Parse(medalColor));
                    li.SubItems[1].BackColor = Color.FromArgb(int.Parse(medalColor));
                    this.listView2.Items.Add(li);
                }
                //自适应单元格宽度
              for (int i = 0; i < this.listView2.Columns.Count; i++)
              {
                    this.listView2.Columns[i].Width = -2;
              }
                
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
            }
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            LoadBagList();
        }
        private void button2_Click(object sender, EventArgs ee)
        {
            //一键满所有勋章,一切以辣条为基准
            int seedCount = 0;//记录送出辣条数
            try
            {
                string medalHost = "https://api.live.bilibili.com/i/api/medal?page=1&pageSize=100";
                JObject medalJson = JObject.Parse(BiliHelper.BiliGetRequest(medalHost, BiliHelper.Data.BiliCookie));
                int medalCount = medalJson["data"]["fansMedalList"].Count();
                //从第一个勋章开始送
                for (int i = 0; i < medalCount; i++)
                {
                    string roomId = medalJson["data"]["fansMedalList"][i]["roomid"].ToString();
                    string medalTodayFeed = medalJson["data"]["fansMedalList"][i]["todayFeed"].ToString();
                    string medalTodayFeedLimit = medalJson["data"]["fansMedalList"][i]["dayLimit"].ToString();
                    int needCount = int.Parse(medalTodayFeedLimit) - int.Parse(medalTodayFeed);//还需要辣条数
                    //获取背包
                    string bagHost = "https://api.live.bilibili.com/xlive/web-room/v1/gift/bag_list";
                    JObject bagJson = JObject.Parse(BiliHelper.BiliGetRequest(bagHost, BiliHelper.Data.BiliCookie));
                    int bagCount = bagJson["data"]["list"].Count();//背包礼物种类数量
                    //从背包第一个礼物开始送
                    for (int j = 0; j < bagCount; j++)
                    {
                        //只送辣条
                        if (bagJson["data"]["list"][j]["gift_name"].ToString() == "辣条")
                        {
                            //这个背包的礼物数量
                            int currentNum = int.Parse(bagJson["data"]["list"][j]["gift_num"].ToString());
                            string bagId = bagJson["data"]["list"][j]["bag_id"].ToString();
                            if (currentNum > needCount)
                            {
                                //如果当前这个背包的辣条数大于需要的，就送需要的辣条数量
                                BiliHelper.BiliSendGift(roomId, "1", needCount.ToString(), bagId);
                                seedCount += needCount;

                                //输出日志
                                this.tb_Log.AppendText("[" + DateTime.Now.Date.ToString() + "]" + "成功将" + medalJson["data"]["fansMedalList"][i]["medal_name"] + "勋章亲密度送满" + Environment.NewLine);
                                break;//跳出，进行下一个勋章
                            }
                            else
                            {
                                //如果当前这个背包的辣条数小于需要的，就送全部辣条
                                BiliHelper.BiliSendGift(roomId, "1", currentNum.ToString(), bagId);
                                seedCount += currentNum;//如果是自己的勋章，也会加。
                                needCount -= currentNum;//刷新还需要的辣条数（如果不更新，会导致一个房间送过多辣条）
                            }

                        }
                    }
                }
                MessageBox.Show("全部赠送完毕，累积送出" + seedCount);
                //刷新勋章
                LoadMedalList();
            }
            catch (Exception e)
            {
                MessageBox.Show("1.勋章里面有自己勋章的时候，可能会出现+或者\n2.对方未开通直播间可能出现\n【给泽酱看的："+e.Message+"】");
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
          
        }

        private void metroButton2_Click(object sender, EventArgs e)
        {
            this.metroTabControl1.Visible = false;
            this.metroTabControl2.Visible = true;
        }

        private void metroButton1_Click(object sender, EventArgs e)
        {
            this.metroTabControl1.Visible = true;
            this.metroTabControl2.Visible = false;
        }
    }
}
