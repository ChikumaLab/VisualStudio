using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using uPLibrary.Networking.M2Mqtt;
using uPLibrary.Networking.M2Mqtt.Messages;

namespace MQTT_TRX
{
    public partial class Form1 : Form
    {
        private MqttClient _client;

        public Form1()
        {
            InitializeComponent();
        }
        public bool Connect(string host, int port, string username, string userpass)
        {
            try
            {
                _client = new MqttClient(host, port, false, null, null, MqttSslProtocols.None);
                if (_client == null)
                {
                    return false;
                }
                byte ret = _client.Connect(Guid.NewGuid().ToString(), username, userpass);
                //受信ハンドラ追加 
                //MqttReceived()は後述
                _client.MqttMsgPublishReceived += MqttReceived;
                return true;
            }
            catch
            {
                return false;
            }
        }
        public void Disconnect()
        {
            if (_client != null)
            {
                _client.Disconnect();
                _client = null;
            }
        }
        private void MqttReceived(object sender, MqttMsgPublishEventArgs e)
        {
            string topic = e.Topic;
            string message = Encoding.ASCII.GetString(e.Message);
            //メッセージを使った処理
            Console.WriteLine($"[topic]{topic} [msg]{message}");
        }
        public bool AddTopic(string topic)
        {
            if (_client == null)
            {
                return false;
            }
            _client.Subscribe(new string[] { topic }, new byte[] { MqttMsgBase.QOS_LEVEL_AT_MOST_ONCE });
            return true;
        }
        public bool RemoveTopic(string topic)
        {
            if (_client == null)
            {
                return false;
            }
            var ret = _client.Unsubscribe(new string[] { topic });
            return true;
        }
        public bool Publish(string topic, string message)
        {
            if (_client == null)
            {
                return false;
            }
            _client.Publish(topic, Encoding.UTF8.GetBytes(message));
            return true;
        }
        public bool CheckConnectStatus()
        {
            return _client.IsConnected;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            bool ret;
            ret = Connect("133.242.211.199", 1883, "", "");
            button1.Enabled = false;

            if (ret == true)
            {
                ret = AddTopic("/GPS");
                Publish("/GPS", "Test");
            }
            else
            {
                button1.Enabled = true;
            }
        }
    }
}
