using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Omuni
{
    public class command
    {
        enum BoardType
        {
            PHONE_Address = 0x80,
            PC_Address = 0x00,
            Motor_Address = 0x03,
            Sensor_Address = 0x82,
        }
        enum CMD_ID
        {
            Request = 0x30,
            Confirm,
            Indication,
            Remote_Request,
            Remote_Confirm,
            Remote_Indication
        }
        enum MotorCMD
        {
            Get_BoardAddress = 0x00,
            Set_MotorRot,
            Set_MotorDirection,
            Set_MotorFree,
            Set_MotorBrake,
            Set_MotorInit,
            Set_MotorDeinit,
            NG_CMD = 0xFF
        }

        public int MotorCommand(bool mode,byte id,byte src,byte dst,byte cmd,ref byte[] data)
        {
            int dummy;
            ushort length = 0;
            ushort buf = 0;

            Form1.sendData[length++] = (byte)(0x7E);
            length = (ushort)(length + 2);
            if(!mode)
            {
                Form1.sendData[length++] = 0x30;
                dummy = Form1.sendData[length - 1];
            }
            else
            {
                Form1.sendData[length++] = 0x33;
                dummy = Form1.sendData[length - 1];
                Form1.sendData[length++] = 0x00;
                dummy += Form1.sendData[length - 1];
                Form1.sendData[length++] = 0x03;
                dummy += Form1.sendData[length - 1];
            }
            Form1.sendData[length++] = cmd;
            dummy += Form1.sendData[length - 1];
            //Form1.sendData[length++] = direction;
            dummy += Form1.sendData[length - 1];
           // buf = duty;
            Form1.sendData[length++] = (byte)((buf & 0xFF00) >> 8);
            dummy += Form1.sendData[length - 1];
            Form1.sendData[length++] = (byte)(buf & 0x00FF);
            dummy += Form1.sendData[length - 1];
            //buf = time;
            Form1.sendData[length++] = (byte)((buf & 0xFF00) >> 8);
            dummy += Form1.sendData[length - 1];
            Form1.sendData[length++] = (byte)(buf & 0x00FF);
            dummy += Form1.sendData[length - 1];
            buf = (ushort)(length - 3);
            Form1.sendData[length++] = (byte)(0xff - (dummy));
            Form1.sendData[1] = (byte)((0xFF00 & buf) >> 8);
            Form1.sendData[2] = (byte)((0x00FF & buf));
           // SerialPortForm1.serialPort1.Write(Form1.sendData, 0, length);
            return 0;
        }
    }
}
