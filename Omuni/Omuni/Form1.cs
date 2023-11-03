using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO.Ports;
using System.IO;


namespace Omuni
{
    public partial class Form1 : Form
    {
#if false
        struct CDM_FORM
        {
            byte CMD_MODE;
            short Length;
            byte CMD_ID;
            byte src;
            byte dst;
            byte cmd;
            byte[] data;
            byte checkSum;
        }
#endif

        enum sensorCommandData
        {
            Get_BOAR_ADDRESS = 0x00,
            Get_Temp,
            Get_Humidity,
            Get_pressure,
            Get_TempAll,
            Set_Light,
            Get_Light,
            Set_IR,
            Get_IR,
            Set_Servo_Duty,
            Set_Servo_Freq,
            Start_Servo,
            Stop_Servo,
            Get_Servo,
            Get_distance1,
            Get_distance2,
            Get_distance3,
            Get_distance4,
            Get_distanceAll,
            Set_Motor_Board,
            Get_Luminance,
        }

        private int checkDistance(int data)
        {
            int ret = 0;

            if(data > 300)
            {
                ret = 10;
            }
            else if(data > 270)
            {
                ret = 11;
            }
            else if(data> 250)
            {
                ret = 12;
            }
            else if(data > 230)
            {
                ret = 13;
            }
            else if(data > 210)
            {
                ret = 14;
            }
            else if(data > 200)
            {
                ret = 15;
            }
            else if (data > 185)
            {
                ret = 16;
            }
            else if (data > 175)
            {
                ret = 17;
            }
            else if (data > 160)
            {
                ret = 18;
            }
            else if (data > 150)
            {
                ret = 19;
            }
            else if (data > 130)
            {
                ret = 20;
            }
            else
            {
                ret = -1;
            }
            return ret;
        }
        public byte[] uartRxData = new byte[1024];
        public static Byte[] sendData = new Byte[64];
        public int logCnt;
        public int graphCnt;
        public int uartRxLength;
        public System.Net.IPEndPoint localEP;
        private System.Net.Sockets.UdpClient udpClient = null;
        public string jpegName;
        public BinaryWriter jpegData;
        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            string[] port = SerialPort.GetPortNames();
            radioButton_udpMode.Checked = true;
            port_comboBox.Items.Clear();
            graphCnt = 0;
            foreach (string portData in port)
            {
                var tmp = portData.IndexOf("COM");
                if (tmp >= 0)
                {
                    port_comboBox.Items.Add(portData);

                }
            }
            if(port_comboBox.Items.Count > 0)
            {
                port_comboBox.SelectedIndex = 0;
            }
            logCnt = 0;
            ListView_list();
        }

        private void port_button_Click(object sender, EventArgs e)
        {
            if(port_button.Text == "接続")
            {
                if (radioButton_udpMode.Checked)
                {
                    groupBox_udp.Enabled = false;
                    localEP = new System.Net.IPEndPoint(
                        System.Net.IPAddress.Any, int.Parse(textBox_ipPort.Text));
                    udpClient = new System.Net.Sockets.UdpClient(localEP);
                    //非同期的なデータ受信を開始する
                    udpClient.BeginReceive(ReceiveCallback, udpClient);
                }
                else
                {
                    serialPort1.PortName = port_comboBox.Text;
                    serialPort1.Open();

                }
                groupBox_controler.Enabled = true;
                port_button.Text = "切断";
            }
            else
            {
                if (radioButton_udpMode.Checked)
                {
                    udpClient.Close();
                }
                else
                {
                    serialPort1.Close();
                }

                port_button.Text = "接続";

            }
        }
        private void ReceiveCallback(IAsyncResult ar)
        {
            System.Net.Sockets.UdpClient udp =
                (System.Net.Sockets.UdpClient)ar.AsyncState;

            //非同期受信を終了する
            System.Net.IPEndPoint remoteEP = null;
            byte[] rcvBytes;
            try
            {
                rcvBytes = udp.EndReceive(ar, ref remoteEP);
            }
            catch (System.Net.Sockets.SocketException ex)
            {
                Console.WriteLine("受信エラー({0}/{1})",
                    ex.Message, ex.ErrorCode);
                return;
            }
            catch (ObjectDisposedException ex)
            {
                //すでに閉じている時は終了
                Console.WriteLine("Socketは閉じられています。");
                return;
            }

            this.Invoke((Action)(() =>
            {
                int dummy = 0;
                string dataCMD ="";
                string dataID = "";
                string ViewData = "-";
                double dataBuf2 = 0.0;
                int dataLen = ((rcvBytes[0] << 8) | (rcvBytes[1]));
                switch (rcvBytes[2])
                {
                    case 0x30:
                        dataID = "Command Request";
                        break;
                    case 0x31:
                        dataID = "Command Confirm";
                        break;
                    case 0x33:
                        dataID = "Command Indication";
                        break;
                    case 0x34:
                        dataID = "Remote Request";
                        break;
                    case 0x35:
                        dataID = "Remote Confirm";
                        break;
                    case 0x36:
                        dataID = "Remote Indication";
                        break;
                    default:
                        dataID = "UNKNOWN ID";
                        break;
                }
                dataCMD = rcvBytes[5].ToString("X02");    // CMD
                if (rcvBytes[3] == 0x82)
                {
                    switch (rcvBytes[5])
                    {
                        case (byte)sensorCommandData.Get_TempAll:
                            dummy = ((rcvBytes[6] << 24) | (rcvBytes[7] << 16) | (rcvBytes[8] << 8) | rcvBytes[9]);
                            dataBuf2 = (double)dummy / 100.0;
                            textBox_temp.Text = dataBuf2.ToString("F");
                            chart1.Series["温度"].Points.AddXY(graphCnt,dataBuf2);
                            dummy = ((rcvBytes[10] << 24) | (rcvBytes[11] << 16) | (rcvBytes[12] << 8) | rcvBytes[13]);
                            dataBuf2 = (double)dummy / 1024.0;
                            chart1.Series["湿度"].Points.AddXY(graphCnt,dataBuf2);
                            textBox_hum.Text = dataBuf2.ToString("F");
                            dummy = ((rcvBytes[14] << 24) | (rcvBytes[15] << 16) | (rcvBytes[16] << 8) | rcvBytes[17]);
                            dataBuf2 = (double)dummy / 100.0;
                            chart1.Series["気圧"].Points.AddXY(graphCnt,dataBuf2/100.0);
                            textBox_press.Text = dataBuf2.ToString("F");
                            graphCnt++;
                            break;
                        case (byte)sensorCommandData.Get_distanceAll:
                            dummy = (rcvBytes[6] << 8) | (rcvBytes[7]);
                            textBox_distance1.Text = dummy.ToString();
                            dummy = checkDistance(dummy);
                            if(dummy < 0)
                            {
                                label_sen1.Text = "障害物無し";
                                label_sen1.ForeColor = System.Drawing.Color.Black;
                            }
                            else
                            {
                                label_sen1.Text = dummy.ToString() + " cm以内";
                                label_sen1.ForeColor = System.Drawing.Color.Red;
                            }
                            dummy = (rcvBytes[8] << 8) | (rcvBytes[9]);
                            textBox_distance2.Text = dummy.ToString();
                            dummy = checkDistance(dummy);
                            if (dummy < 0)
                            {
                                label_sen2.Text = "障害物無し";
                                label_sen2.ForeColor = System.Drawing.Color.Black;
                            }
                            else
                            {
                                label_sen2.Text = dummy.ToString() + " cm以内";
                                label_sen2.ForeColor = System.Drawing.Color.Red;
                            }
                            dummy = (rcvBytes[10] << 8) | (rcvBytes[11]);
                            textBox_distance3.Text = dummy.ToString();
                            dummy = checkDistance(dummy);
                            if (dummy < 0)
                            {
                                label_sen3.Text = "障害物無し";
                                label_sen3.ForeColor = System.Drawing.Color.Black;
                            }
                            else
                            {
                                label_sen3.Text = dummy.ToString() + " cm以内";
                                label_sen3.ForeColor = System.Drawing.Color.Red;
                            }
                            dataCMD = "Sensor Get distance All.conf";
                            ViewData = rcvBytes[6].ToString("X02") + " " + rcvBytes[7].ToString("X02") + " ";
                            ViewData += rcvBytes[8].ToString("X02") + " " + rcvBytes[9].ToString("X02") + " ";
                            ViewData += rcvBytes[10].ToString("X02") + " " + rcvBytes[11].ToString("X02");
                            break;

                        case (byte)sensorCommandData.Get_Luminance:
                            dummy = (rcvBytes[6] << 8) | (rcvBytes[7]);
                            textBox_Luminance.Text = dummy.ToString();
                            if (dummy > 1000)
                            {
                                dummy = 1000;
                            }
                            progressBar1.Value = dummy;
                            dataCMD = "Sensor Get Luminance.conf";
                            ViewData = rcvBytes[6].ToString("X02") + " " + rcvBytes[7].ToString("X02");
                            break;
                        default:
                            break;
                    }

                }
                else if(rcvBytes[3] == 0x81)
                {
                    switch (rcvBytes[5])
                    {
                        case (byte)0x01:
                            if(rcvBytes[7] == 1)
                            {
                                DateTime dt = DateTime.Now;
                                jpegName = dt.Year.ToString() + dt.Month.ToString() + dt.Day.ToString()+ "_";
                                jpegName = jpegName + dt.Hour.ToString() + dt.Minute.ToString() + dt.Second.ToString() + ".jpg";
                                jpegData = new BinaryWriter(new FileStream(jpegName, FileMode.Create));
                                jpegData.Write(rcvBytes, 8, rcvBytes.Length - 8);
                            }
                            else if(rcvBytes[6] == rcvBytes[7])
                            {
                                jpegData.Write(rcvBytes, 8, rcvBytes.Length - 8);
                                jpegData.Close();
                                pictureBox1.Image = System.Drawing.Image.FromFile(jpegName);
                            }
                            else
                            {
                                jpegData.Write(rcvBytes, 8, rcvBytes.Length - 8);
                            }
                            break;
                    }
                }



                string[] itemList = {
                logCnt.ToString(),
                dataLen.ToString(),
                dataID,
                rcvBytes[3].ToString("X02"),    // src
                rcvBytes[4].ToString("X02"),    // dst
                dataCMD,    // CMD
                ViewData,
                "-"
                };

                ListViewItem lvi = new ListViewItem(itemList);
                lvi.BackColor = System.Drawing.Color.LightBlue;
                listView_Log.Items.Add(lvi);
                logCnt++;

            }));

            //データを文字列に変換する
            string rcvMsg = System.Text.Encoding.UTF8.GetString(rcvBytes);

            //受信したデータと送信者の情報をRichTextBoxに表示する
            //再びデータ受信を開始する
            udp.BeginReceive(ReceiveCallback, udp);
        }

        private void serialPort1_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            int dataLength;
            var dataDummy = new byte[3];

            System.Threading.Thread.Sleep(200);
            serialPort1.Read(dataDummy, 0, 1);
            if (dataDummy[0] != 0x7E)
            {
                serialPort1.DiscardInBuffer();
                return;
            }
            serialPort1.Read(dataDummy, 0, 2);
            dataLength = (dataDummy[0] << 8) + (dataDummy[1]) + 1;
            if(serialPort1.BytesToRead < dataLength)
            {
                serialPort1.DiscardInBuffer();
                return;
            }
            uartRxLength = dataLength;
            serialPort1.Read(uartRxData, 0, dataLength);
            this.Invoke((Action)(() =>
            {
                int dummy = 0;
                byte dataCMD = 0;
                string dataID ="";
                int dataBuf = 0;
                double dataBuf2 = 0.0;
                ulong dataBuf3 = 0;
                for (int i = 0; i < uartRxLength; i++)
                {
                    dummy = dummy + uartRxData[i];
                }


                switch (uartRxData[0])
                {
                    case 0x30:
                        dataID = "Command Request";
                        break;
                    case 0x31:
                        dataID = "Command Confirm";
                        break;
                    case 0x33:
                        dataID = "Command Indication";
                        break;
                    case 0x34:
                        dataID = "Remote Request";
                        break;
                    case 0x35:
                        dataID = "Remote Confirm";
                        break;
                    case 0x36:
                        dataID = "Remote Indication";
                        break;
                    default:
                        dataID = "UNKNOWN ID";
                        break;
                }



                string[] itemList = {
                logCnt.ToString(),
                uartRxLength.ToString(),
                dataID,
                uartRxData[1].ToString("X02"),
                uartRxData[2].ToString("X02"),
                uartRxData[3].ToString("X02"),
                "-",
                "-"
                };

                ListViewItem lvi = new ListViewItem(itemList);
                lvi.BackColor = System.Drawing.Color.LightBlue;
                listView_Log.Items.Add(lvi);
                logCnt++;
            }));
        }

        private void button_mtLU_Click(object sender, EventArgs e)
        {
            ushort dty = (ushort)(3200.0 * trackBar_Speed.Value * 0.01);
            motor_cnt(checkBox_debug.Checked, 0x05, dty, (ushort)trackBar_Time.Value);

        }

        private void button_mtRD_Click(object sender, EventArgs e)
        {
            ushort dty = (ushort)(3200.0 * trackBar_Speed.Value * 0.01);
            motor_cnt(checkBox_debug.Checked, 0x04, dty, (ushort)trackBar_Time.Value);

        }

        private void button_mtD_Click(object sender, EventArgs e)
        {
            ushort dty = (ushort)(3200.0 * trackBar_Speed.Value * 0.01);
            motor_cnt(checkBox_debug.Checked, 0x03, dty, (ushort)trackBar_Time.Value);

        }

        private void button_mtU_Click(object sender, EventArgs e)
        {
            ushort dty = (ushort)(3200.0 * trackBar_Speed.Value * 0.01);
            motor_cnt(checkBox_debug.Checked, 0x02, dty, (ushort)trackBar_Time.Value);

        }

        private void button_mtLD_Click(object sender, EventArgs e)
        {
            ushort dty = (ushort)(3200.0 * trackBar_Speed.Value * 0.01);
            motor_cnt(checkBox_debug.Checked, 0x00, dty, (ushort)trackBar_Time.Value);
        }
        private void button_mtRU_Click(object sender, EventArgs e)
        {
            ushort dty = (ushort)(3200.0 * trackBar_Speed.Value * 0.01);
            motor_cnt(checkBox_debug.Checked, 0x01, dty, (ushort)trackBar_Time.Value);
        }

        private void button_mtLL_Click(object sender, EventArgs e)
        {
            ushort dty = (ushort)(3200.0 * trackBar_Speed.Value * 0.01);
            CMD_motorRot(checkBox_debug.Checked, 0, dty, (ushort) trackBar_Time.Value);
        }

        private void button_mtRR_Click(object sender, EventArgs e)
        {
            ushort dty = (ushort)(3200.0 * trackBar_Speed.Value * 0.01);
            CMD_motorRot(checkBox_debug.Checked, 1, dty, (ushort)trackBar_Time.Value);

        }
        private void ListView_list()
        {
            listView_Log.FullRowSelect = true;
            listView_Log.GridLines = true;
            listView_Log.Sorting = SortOrder.None;
            listView_Log.View = View.Details;

            ColumnHeader[] colHeaderRegValue = new ColumnHeader[8];
            for(int i = 0;i < 8;i++)
            {
                colHeaderRegValue[i] = new ColumnHeader();
            }
            colHeaderRegValue[0].Text = "No.";
            colHeaderRegValue[1].Text = "Length";
            colHeaderRegValue[2].Text = "ID";
            colHeaderRegValue[3].Text = "SRC";
            colHeaderRegValue[4].Text = "DST";
            colHeaderRegValue[5].Text = "CMD";
            colHeaderRegValue[6].Text = "Data_All";
            colHeaderRegValue[7].Text = "Checksum";
            listView_Log.Columns.AddRange(colHeaderRegValue);
        }
        private void CMD_motorRot(bool mode,byte rot, ushort duty, ushort time)
        {
            int dummy;
            ushort length = 0;
            ushort buf = 0;

            string strData,strId,strSrc,strDst,strCmd;
            string strCs;

            strCmd = "Set Motor rot";

            if (radioButton_serialMode.Checked)
            {
                sendData[length++] = 0x7E;
            }
            length = (ushort)(length + 2);
            if (mode)
            {
                sendData[length++] = 0x30;
                strSrc = "-";
                strDst = "-";
                strId = "Command Request";
                dummy = sendData[length - 1];
            }
            else
            {
                strId = "Remote Command Request";
                sendData[length++] = 0x33;
                dummy = sendData[length - 1];
                sendData[length++] = 0x00;
                strSrc = sendData[length - 1].ToString("X2");
                dummy += sendData[length - 1];
                sendData[length++] = 0x03;
                strDst = sendData[length - 1].ToString("X2");
                dummy += sendData[length - 1];
            }
            sendData[length++] = (byte) 0x01;
            strData = sendData[length - 1].ToString("X2");
            dummy += sendData[length - 1];
            sendData[length++] = rot;
            strData = strData +" "+ sendData[length - 1].ToString("X2");
            dummy += sendData[length - 1];
            buf = duty;
            sendData[length++] = (byte)((buf & 0xFF00) >> 8);
            strData = strData + " " + sendData[length - 1].ToString("X2");
            dummy += sendData[length - 1];
            sendData[length++] = (byte)(buf & 0x00FF);
            strData = strData + " " + sendData[length - 1].ToString("X2");
            dummy += sendData[length - 1];
            buf = time;
            sendData[length++] = (byte)((buf & 0xFF00) >> 8);
            strData = strData + " " + sendData[length - 1].ToString("X2");
            dummy += sendData[length - 1];
            sendData[length++] = (byte)(buf & 0x00FF);
            strData = strData + " " + sendData[length - 1].ToString("X2");
            dummy += sendData[length - 1];
            sendData[length++] = (byte)(0xff - (dummy));
            strCs = sendData[length - 1].ToString("X2");

            string[] itemList = {
                logCnt.ToString(),
                length.ToString(),
                strId,
                strSrc,
                strDst,
                strCmd,
                strData,
                strCs
                };

            ListViewItem lvi = new ListViewItem(itemList);
            lvi.BackColor = System.Drawing.Color.LightYellow;
            listView_Log.Items.Add(lvi);
            logCnt++;
            if (radioButton_serialMode.Checked)
            {
                buf = (ushort)(length - 3);
                sendData[1] = (byte)((0xFF00 & buf) >> 8);
                sendData[2] = (byte)((0x00FF & buf));
                serialPort1.Write(sendData, 0, length);
            }
            else
            {
                buf = (ushort)(length - 2);
                sendData[0] = (byte)((0xFF00 & buf) >> 8);
                sendData[1] = (byte)((0x00FF & buf));
                udpClient.BeginSend(sendData, length - 1,
                    textBox_dstIp.Text, int.Parse(textBox_dstPort.Text),
                    SendCallback, udpClient);
            }

        }
        private void motor_cnt(bool mode,  byte direction, ushort duty, ushort time)
        {
            int dummy;
            ushort length = 0;
            ushort buf = 0;
            string strData, strId, strSrc, strDst, strCmd;
            string strCs;

            strCmd = "Set_Motor_direction";

            if(radioButton_serialMode.Checked)
            {
                sendData[length++] = 0x7E;
            }
            length = (ushort)(length + 2);
            if (mode)
            {
                sendData[length++] = 0x30;
                strSrc = "-";
                strDst = "-";
                strId = "Command Request";
                dummy = sendData[length - 1];
            }
            else
            {
                sendData[length++] = 0x33;
                strId = "Remote Command Request";
                dummy = sendData[length - 1];
                sendData[length++] = 0x00;
                strSrc = sendData[length - 1].ToString("X2");
                dummy += sendData[length - 1];
                sendData[length++] = 0x03;
                strDst = sendData[length - 1].ToString("X2");
                dummy += sendData[length - 1];
            }
            sendData[length++] = 0x02;
            strData = sendData[length - 1].ToString("X2");
            dummy += sendData[length - 1];
            sendData[length++] = direction;
            strData = strData + " " + sendData[length - 1].ToString("X2");
            dummy += sendData[length - 1];
            buf = duty;
            sendData[length++] = (byte)((buf & 0xFF00) >> 8);
            strData = strData + " " + sendData[length - 1].ToString("X2");
            dummy += sendData[length - 1];
            sendData[length++] = (byte)(buf & 0x00FF);
            strData = strData + " " + sendData[length - 1].ToString("X2");
            dummy += sendData[length - 1];
            buf = time;
            sendData[length++] = (byte)((buf & 0xFF00) >> 8);
            strData = strData + " " + sendData[length - 1].ToString("X2");
            dummy += sendData[length - 1];
            sendData[length++] = (byte)(buf & 0x00FF);
            strData = strData + " " + sendData[length - 1].ToString("X2");
            dummy += sendData[length - 1];
            sendData[length++] = (byte)(0xff - (dummy));
            strCs = sendData[length - 1].ToString("X2");

            string[] itemList = {
                logCnt.ToString(),
                length.ToString(),
                strId,
                strSrc,
                strDst,
                strCmd,
                strData,
                strCs
                };
            ListViewItem lvi = new ListViewItem(itemList);
            lvi.BackColor = System.Drawing.Color.LightYellow;
            listView_Log.Items.Add(lvi);
            logCnt++;

            if (radioButton_serialMode.Checked)
            {
                buf = (ushort)(length - 3);
                sendData[1] = (byte)((0xFF00 & buf) >> 8);
                sendData[2] = (byte)((0x00FF & buf));
                serialPort1.Write(sendData, 0, length);
            }
            else
            {
                buf = (ushort)(length - 2);
                sendData[0] = (byte)((0xFF00 & buf) >> 8);
                sendData[1] = (byte)((0x00FF & buf));
                udpClient.BeginSend(sendData, length - 1,
                    textBox_dstIp.Text, int.Parse(textBox_dstPort.Text),
                    SendCallback, udpClient);
            }

        }
        private void SendCallback(IAsyncResult ar)
        {

        }
        private void radioButton_udpMode_CheckedChanged(object sender, EventArgs e)
        {
            if(radioButton_udpMode.Checked)
            {
                groupBox_serial.Enabled = false;
                groupBox_udp.Enabled = true;
            }
            else
            {
                groupBox_serial.Enabled = true;
                groupBox_udp.Enabled = false;
            }
        }

        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            if (udpClient != null)
            {
                udpClient.Close();
            }
        }

        private void trackBar_Speed_Scroll(object sender, EventArgs e)
        {
            double buf;

            buf = ((double)trackBar_Speed.Value / (double)trackBar_Speed.Maximum) * 100.0;
            label_speed.Text = buf.ToString()+"%"; 
        }

        private void trackBar_Time_Scroll(object sender, EventArgs e)
        {
            double buf;

            buf = trackBar_Time.Value / 10.0;
            label_sec.Text = buf.ToString() + "秒";
        }

        private void button1_Click(object sender, EventArgs e)
        {
            int dummy;
            ushort length = 0;
            ushort buf = 0;
            string strData, strId, strSrc, strDst, strCmd;
            string strCs;

            strCmd = "Set Servo Duty";

            if (radioButton_serialMode.Checked)
            {
                sendData[length++] = 0x7E;
            }
            length = (ushort)(length + 2);
            if (checkBox_debug.Checked)
            {
                sendData[length++] = 0x30;
                strSrc = "-";
                strDst = "-";
                strId = "Command Request";
                dummy = sendData[length - 1];
            }
            else
            {
                sendData[length++] = 0x33;
                strId = "Remote Command Request";
                dummy = sendData[length - 1];
                sendData[length++] = 0x00;
                strSrc = sendData[length - 1].ToString("X2");
                dummy += sendData[length - 1];
                sendData[length++] = 0x82;
                strDst = sendData[length - 1].ToString("X2");
                dummy += sendData[length - 1];
            }
            sendData[length++] = (byte)sensorCommandData.Set_Servo_Duty;
            strData = sendData[length - 1].ToString("X2");
            dummy += sendData[length - 1];

            buf = (ushort)(trackBar_Servo.Value + 0x01F4);

            sendData[length++] = (byte)((buf & 0xFF00) >> 8);
            strData = strData + " " + sendData[length - 1].ToString("X2");
            dummy += sendData[length - 1];
            sendData[length++] = (byte)(buf & 0x00FF);
            strData = strData + " " + sendData[length - 1].ToString("X2");
            dummy += sendData[length - 1];
            sendData[length++] = (byte)(0xff - (dummy));
            strCs = sendData[length - 1].ToString("X2");

            string[] itemList = {
                logCnt.ToString(),
                length.ToString(),
                strId,
                strSrc,
                strDst,
                strCmd,
                strData,
                strCs
                };
            ListViewItem lvi = new ListViewItem(itemList);
            lvi.BackColor = System.Drawing.Color.LightYellow;
            listView_Log.Items.Add(lvi);
            logCnt++;

            if (radioButton_serialMode.Checked)
            {
                buf = (ushort)(length - 3);
                sendData[1] = (byte)((0xFF00 & buf) >> 8);
                sendData[2] = (byte)((0x00FF & buf));
                serialPort1.Write(sendData, 0, length);
            }
            else
            {
                buf = (ushort)(length - 2);
                sendData[0] = (byte)((0xFF00 & buf) >> 8);
                sendData[1] = (byte)((0x00FF & buf));
                udpClient.BeginSend(sendData, length - 1,
                    textBox_dstIp.Text, int.Parse(textBox_dstPort.Text),
                    SendCallback, udpClient);
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            int dummy;
            ushort length = 0;
            ushort buf = 0;
            string strData, strId, strSrc, strDst, strCmd;
            string strCs;

            strCmd = "Get Luminance.req";

            if (radioButton_serialMode.Checked)
            {
                sendData[length++] = 0x7E;
            }
            length = (ushort)(length + 2);
            if (checkBox_debug.Checked)
            {
                sendData[length++] = 0x30;
                strSrc = "-";
                strDst = "-";
                strId = "Command Request";
                dummy = sendData[length - 1];
            }
            else
            {
                sendData[length++] = 0x33;
                strId = "Remote Command Request";
                dummy = sendData[length - 1];
                sendData[length++] = 0x00;
                strSrc = sendData[length - 1].ToString("X2");
                dummy += sendData[length - 1];
                sendData[length++] = 0x82;
                strDst = sendData[length - 1].ToString("X2");
                dummy += sendData[length - 1];
            }
            sendData[length++] = (byte)sensorCommandData.Get_Luminance;
            strData = sendData[length - 1].ToString("X2");
            dummy += sendData[length - 1];

            sendData[length++] = (byte)(0xff - (dummy));
            strCs = sendData[length - 1].ToString("X2");

            string[] itemList = {
                logCnt.ToString(),
                length.ToString(),
                strId,
                strSrc,
                strDst,
                strCmd,
                strData,
                strCs
                };
            ListViewItem lvi = new ListViewItem(itemList);
            lvi.BackColor = System.Drawing.Color.LightYellow;
            listView_Log.Items.Add(lvi);
            logCnt++;

            if (radioButton_serialMode.Checked)
            {
                buf = (ushort)(length - 3);
                sendData[1] = (byte)((0xFF00 & buf) >> 8);
                sendData[2] = (byte)((0x00FF & buf));
                serialPort1.Write(sendData, 0, length);
            }
            else
            {
                buf = (ushort)(length - 2);
                sendData[0] = (byte)((0xFF00 & buf) >> 8);
                sendData[1] = (byte)((0x00FF & buf));
                udpClient.BeginSend(sendData, length - 1,
                    textBox_dstIp.Text, int.Parse(textBox_dstPort.Text),
                    SendCallback, udpClient);
            }

        }

        private void button6_Click(object sender, EventArgs e)
        {
            int dummy;
            ushort length = 0;
            ushort buf = 0;
            string strData, strId, strSrc, strDst, strCmd;
            string strCs;

            strCmd = "Get Distance_All.req";

            if (radioButton_serialMode.Checked)
            {
                sendData[length++] = 0x7E;
            }
            length = (ushort)(length + 2);
            if (checkBox_debug.Checked)
            {
                sendData[length++] = 0x30;
                strSrc = "-";
                strDst = "-";
                strId = "Command Request";
                dummy = sendData[length - 1];
            }
            else
            {
                sendData[length++] = 0x33;
                strId = "Remote Command Request";
                dummy = sendData[length - 1];
                sendData[length++] = 0x00;
                strSrc = sendData[length - 1].ToString("X2");
                dummy += sendData[length - 1];
                sendData[length++] = 0x82;
                strDst = sendData[length - 1].ToString("X2");
                dummy += sendData[length - 1];
            }
            sendData[length++] = (byte)sensorCommandData.Get_distanceAll;
            strData = sendData[length - 1].ToString("X2");
            dummy += sendData[length - 1];

            sendData[length++] = (byte)(0xff - (dummy));
            strCs = sendData[length - 1].ToString("X2");

            string[] itemList = {
                logCnt.ToString(),
                length.ToString(),
                strId,
                strSrc,
                strDst,
                strCmd,
                strData,
                strCs
                };
            ListViewItem lvi = new ListViewItem(itemList);
            lvi.BackColor = System.Drawing.Color.LightYellow;
            listView_Log.Items.Add(lvi);
            logCnt++;

            if (radioButton_serialMode.Checked)
            {
                buf = (ushort)(length - 3);
                sendData[1] = (byte)((0xFF00 & buf) >> 8);
                sendData[2] = (byte)((0x00FF & buf));
                serialPort1.Write(sendData, 0, length);
            }
            else
            {
                buf = (ushort)(length - 2);
                sendData[0] = (byte)((0xFF00 & buf) >> 8);
                sendData[1] = (byte)((0x00FF & buf));
                udpClient.BeginSend(sendData, length - 1,
                    textBox_dstIp.Text, int.Parse(textBox_dstPort.Text),
                    SendCallback, udpClient);
            }

        }

        private void button8_Click(object sender, EventArgs e)
        {
            int dummy;
            ushort length = 0;
            ushort buf = 0;
            string strData, strId, strSrc, strDst, strCmd;
            string strCs;

            strCmd = "Get TempAll.req";

            if (radioButton_serialMode.Checked)
            {
                sendData[length++] = 0x7E;
            }
            length = (ushort)(length + 2);
            if (checkBox_debug.Checked)
            {
                sendData[length++] = 0x30;
                strSrc = "-";
                strDst = "-";
                strId = "Command Request";
                dummy = sendData[length - 1];
            }
            else
            {
                sendData[length++] = 0x33;
                strId = "Remote Command Request";
                dummy = sendData[length - 1];
                sendData[length++] = 0x00;
                strSrc = sendData[length - 1].ToString("X2");
                dummy += sendData[length - 1];
                sendData[length++] = 0x82;
                strDst = sendData[length - 1].ToString("X2");
                dummy += sendData[length - 1];
            }
            sendData[length++] = (byte)sensorCommandData.Get_TempAll;
            strData = sendData[length - 1].ToString("X2");
            dummy += sendData[length - 1];

            sendData[length++] = (byte)(0xff - (dummy));
            strCs = sendData[length - 1].ToString("X2");

            string[] itemList = {
                logCnt.ToString(),
                length.ToString(),
                strId,
                strSrc,
                strDst,
                strCmd,
                strData,
                strCs
                };
            ListViewItem lvi = new ListViewItem(itemList);
            lvi.BackColor = System.Drawing.Color.LightYellow;
            listView_Log.Items.Add(lvi);
            logCnt++;

            if (radioButton_serialMode.Checked)
            {
                buf = (ushort)(length - 3);
                sendData[1] = (byte)((0xFF00 & buf) >> 8);
                sendData[2] = (byte)((0x00FF & buf));
                serialPort1.Write(sendData, 0, length);
            }
            else
            {
                buf = (ushort)(length - 2);
                sendData[0] = (byte)((0xFF00 & buf) >> 8);
                sendData[1] = (byte)((0x00FF & buf));
                udpClient.BeginSend(sendData, length - 1,
                    textBox_dstIp.Text, int.Parse(textBox_dstPort.Text),
                    SendCallback, udpClient);
            }
        }

        private void button4_Click(object sender, EventArgs e)
        {
            int dummy;
            ushort length = 0;
            ushort buf = 0;
            string strData, strId, strSrc, strDst, strCmd;
            string strCs;

            strCmd = "Get Camera.req";

            if (radioButton_serialMode.Checked)
            {
                sendData[length++] = 0x7E;
            }
            length = (ushort)(length + 2);
            if (checkBox_debug.Checked)
            {
                sendData[length++] = 0x30;
                strSrc = "-";
                strDst = "-";
                strId = "Command Request";
                dummy = sendData[length - 1];
            }
            else
            {
                sendData[length++] = 0x33;
                strId = "Remote Command Request";
                dummy = sendData[length - 1];
                sendData[length++] = 0x00;
                strSrc = sendData[length - 1].ToString("X2");
                dummy += sendData[length - 1];
                sendData[length++] = 0x81;
                strDst = sendData[length - 1].ToString("X2");
                dummy += sendData[length - 1];
            }
            sendData[length++] = (byte)0x01;
            strData = sendData[length - 1].ToString("X2");
            dummy += sendData[length - 1];

            sendData[length++] = (byte)(0xff - (dummy));
            strCs = sendData[length - 1].ToString("X2");

            string[] itemList = {
                logCnt.ToString(),
                length.ToString(),
                strId,
                strSrc,
                strDst,
                strCmd,
                strData,
                strCs
                };
            ListViewItem lvi = new ListViewItem(itemList);
            lvi.BackColor = System.Drawing.Color.LightYellow;
            listView_Log.Items.Add(lvi);
            logCnt++;

            if (radioButton_serialMode.Checked)
            {
                buf = (ushort)(length - 3);
                sendData[1] = (byte)((0xFF00 & buf) >> 8);
                sendData[2] = (byte)((0x00FF & buf));
                serialPort1.Write(sendData, 0, length);
            }
            else
            {
                buf = (ushort)(length - 2);
                sendData[0] = (byte)((0xFF00 & buf) >> 8);
                sendData[1] = (byte)((0x00FF & buf));
                udpClient.BeginSend(sendData, length - 1,
                    textBox_dstIp.Text, int.Parse(textBox_dstPort.Text),
                    SendCallback, udpClient);
            }

        }
        private void trackBar_Servo_Scroll(object sender, EventArgs e)
        {
            int dummy;
            ushort length = 0;
            ushort buf = 0;
            string strData, strId, strSrc, strDst, strCmd;
            string strCs;

            strCmd = "Set Servo Duty";

            if (radioButton_serialMode.Checked)
            {
                sendData[length++] = 0x7E;
            }
            length = (ushort)(length + 2);
            if (checkBox_debug.Checked)
            {
                sendData[length++] = 0x30;
                strSrc = "-";
                strDst = "-";
                strId = "Command Request";
                dummy = sendData[length - 1];
            }
            else
            {
                sendData[length++] = 0x33;
                strId = "Remote Command Request";
                dummy = sendData[length - 1];
                sendData[length++] = 0x00;
                strSrc = sendData[length - 1].ToString("X2");
                dummy += sendData[length - 1];
                sendData[length++] = 0x82;
                strDst = sendData[length - 1].ToString("X2");
                dummy += sendData[length - 1];
            }
            sendData[length++] = (byte)sensorCommandData.Set_Servo_Duty;
            strData = sendData[length - 1].ToString("X2");
            dummy += sendData[length - 1];

            buf = (ushort)(trackBar_Servo.Value + 0x01F4);

            sendData[length++] = (byte)((buf & 0xFF00) >> 8);
            strData = strData + " " + sendData[length - 1].ToString("X2");
            dummy += sendData[length - 1];
            sendData[length++] = (byte)(buf & 0x00FF);
            strData = strData + " " + sendData[length - 1].ToString("X2");
            dummy += sendData[length - 1];
            sendData[length++] = (byte)(0xff - (dummy));
            strCs = sendData[length - 1].ToString("X2");

            string[] itemList = {
                logCnt.ToString(),
                length.ToString(),
                strId,
                strSrc,
                strDst,
                strCmd,
                strData,
                strCs
                };
            ListViewItem lvi = new ListViewItem(itemList);
            lvi.BackColor = System.Drawing.Color.LightYellow;
            listView_Log.Items.Add(lvi);
            logCnt++;

            if (radioButton_serialMode.Checked)
            {
                buf = (ushort)(length - 3);
                sendData[1] = (byte)((0xFF00 & buf) >> 8);
                sendData[2] = (byte)((0x00FF & buf));
                serialPort1.Write(sendData, 0, length);
            }
            else
            {
                buf = (ushort)(length - 2);
                sendData[0] = (byte)((0xFF00 & buf) >> 8);
                sendData[1] = (byte)((0x00FF & buf));
                udpClient.BeginSend(sendData, length - 1,
                    textBox_dstIp.Text, int.Parse(textBox_dstPort.Text),
                    SendCallback, udpClient);
            }

        }

        private void button2_Click(object sender, EventArgs e)
        {
            int dummy;
            ushort length = 0;
            ushort buf = 0;
            string strData, strId, strSrc, strDst, strCmd;
            string strCs;

            strCmd = "Set Servo Duty";

            if (radioButton_serialMode.Checked)
            {
                sendData[length++] = 0x7E;
            }
            length = (ushort)(length + 2);
            if (checkBox_debug.Checked)
            {
                sendData[length++] = 0x30;
                strSrc = "-";
                strDst = "-";
                strId = "Command Request";
                dummy = sendData[length - 1];
            }
            else
            {
                sendData[length++] = 0x33;
                strId = "Remote Command Request";
                dummy = sendData[length - 1];
                sendData[length++] = 0x00;
                strSrc = sendData[length - 1].ToString("X2");
                dummy += sendData[length - 1];
                sendData[length++] = 0x82;
                strDst = sendData[length - 1].ToString("X2");
                dummy += sendData[length - 1];
            }
            sendData[length++] = (byte)sensorCommandData.Set_Light;
            strData = sendData[length - 1].ToString("X2");
            dummy += sendData[length - 1];

            buf = (ushort)(trackBar2.Value );

            sendData[length++] = (byte)((buf & 0xFF00) >> 8);
            strData = strData + " " + sendData[length - 1].ToString("X2");
            dummy += sendData[length - 1];
            sendData[length++] = (byte)(buf & 0x00FF);
            strData = strData + " " + sendData[length - 1].ToString("X2");
            dummy += sendData[length - 1];
            sendData[length++] = (byte)(0xff - (dummy));
            strCs = sendData[length - 1].ToString("X2");

            string[] itemList = {
                logCnt.ToString(),
                length.ToString(),
                strId,
                strSrc,
                strDst,
                strCmd,
                strData,
                strCs
                };
            ListViewItem lvi = new ListViewItem(itemList);
            lvi.BackColor = System.Drawing.Color.LightYellow;
            listView_Log.Items.Add(lvi);
            logCnt++;

            if (radioButton_serialMode.Checked)
            {
                buf = (ushort)(length - 3);
                sendData[1] = (byte)((0xFF00 & buf) >> 8);
                sendData[2] = (byte)((0x00FF & buf));
                serialPort1.Write(sendData, 0, length);
            }
            else
            {
                buf = (ushort)(length - 2);
                sendData[0] = (byte)((0xFF00 & buf) >> 8);
                sendData[1] = (byte)((0x00FF & buf));
                udpClient.BeginSend(sendData, length - 1,
                    textBox_dstIp.Text, int.Parse(textBox_dstPort.Text),
                    SendCallback, udpClient);
            }

        }

        private void trackBar2_Scroll(object sender, EventArgs e)
        {
            int dummy;
            ushort length = 0;
            ushort buf = 0;
            string strData, strId, strSrc, strDst, strCmd;
            string strCs;

            strCmd = "Set Servo Duty";

            if (radioButton_serialMode.Checked)
            {
                sendData[length++] = 0x7E;
            }
            length = (ushort)(length + 2);
            if (checkBox_debug.Checked)
            {
                sendData[length++] = 0x30;
                strSrc = "-";
                strDst = "-";
                strId = "Command Request";
                dummy = sendData[length - 1];
            }
            else
            {
                sendData[length++] = 0x33;
                strId = "Remote Command Request";
                dummy = sendData[length - 1];
                sendData[length++] = 0x00;
                strSrc = sendData[length - 1].ToString("X2");
                dummy += sendData[length - 1];
                sendData[length++] = 0x82;
                strDst = sendData[length - 1].ToString("X2");
                dummy += sendData[length - 1];
            }
            sendData[length++] = (byte)sensorCommandData.Set_Light;
            strData = sendData[length - 1].ToString("X2");
            dummy += sendData[length - 1];

            buf = (ushort)(trackBar2.Value);

            sendData[length++] = (byte)((buf & 0xFF00) >> 8);
            strData = strData + " " + sendData[length - 1].ToString("X2");
            dummy += sendData[length - 1];
            sendData[length++] = (byte)(buf & 0x00FF);
            strData = strData + " " + sendData[length - 1].ToString("X2");
            dummy += sendData[length - 1];
            sendData[length++] = (byte)(0xff - (dummy));
            strCs = sendData[length - 1].ToString("X2");

            string[] itemList = {
                logCnt.ToString(),
                length.ToString(),
                strId,
                strSrc,
                strDst,
                strCmd,
                strData,
                strCs
                };
            ListViewItem lvi = new ListViewItem(itemList);
            lvi.BackColor = System.Drawing.Color.LightYellow;
            listView_Log.Items.Add(lvi);
            logCnt++;

            if (radioButton_serialMode.Checked)
            {
                buf = (ushort)(length - 3);
                sendData[1] = (byte)((0xFF00 & buf) >> 8);
                sendData[2] = (byte)((0x00FF & buf));
                serialPort1.Write(sendData, 0, length);
            }
            else
            {
                buf = (ushort)(length - 2);
                sendData[0] = (byte)((0xFF00 & buf) >> 8);
                sendData[1] = (byte)((0x00FF & buf));
                udpClient.BeginSend(sendData, length - 1,
                    textBox_dstIp.Text, int.Parse(textBox_dstPort.Text),
                    SendCallback, udpClient);
            }

        }

        private void button5_Click(object sender, EventArgs e)
        {
            int dummy;
            ushort length = 0;
            ushort buf = 0;
            string strData, strId, strSrc, strDst, strCmd;
            string strCs;

            strCmd = "Addr";

            if (radioButton_serialMode.Checked)
            {
                sendData[length++] = 0x7E;
            }
            length = (ushort)(length + 2);
            if (checkBox_debug.Checked)
            {
                sendData[length++] = 0x30;
                strSrc = "-";
                strDst = "-";
                strId = "Command Request";
                dummy = sendData[length - 1];
            }
            else
            {
                sendData[length++] = 0x33;
                strId = "Remote Command Request";
                dummy = sendData[length - 1];
                sendData[length++] = 0x00;
                strSrc = sendData[length - 1].ToString("X2");
                dummy += sendData[length - 1];
                sendData[length++] = 0x82;
                strDst = sendData[length - 1].ToString("X2");
                dummy += sendData[length - 1];
            }
            sendData[length++] = 0x00;
            strData = sendData[length - 1].ToString("X2");
            dummy += sendData[length - 1];

            sendData[length++] = (byte)(0xff - (dummy));
            strCs = sendData[length - 1].ToString("X2");

            string[] itemList = {
                logCnt.ToString(),
                length.ToString(),
                strId,
                strSrc,
                strDst,
                strCmd,
                strData,
                strCs
                };
            ListViewItem lvi = new ListViewItem(itemList);
            lvi.BackColor = System.Drawing.Color.LightYellow;
            listView_Log.Items.Add(lvi);
            logCnt++;

            if (radioButton_serialMode.Checked)
            {
                buf = (ushort)(length - 3);
                sendData[1] = (byte)((0xFF00 & buf) >> 8);
                sendData[2] = (byte)((0x00FF & buf));
                serialPort1.Write(sendData, 0, length);
            }
            else
            {
                buf = (ushort)(length - 2);
                sendData[0] = (byte)((0xFF00 & buf) >> 8);
                sendData[1] = (byte)((0x00FF & buf));
                udpClient.BeginSend(sendData, length - 1,
                    textBox_dstIp.Text, int.Parse(textBox_dstPort.Text),
                    SendCallback, udpClient);
            }

        }
    }
}
