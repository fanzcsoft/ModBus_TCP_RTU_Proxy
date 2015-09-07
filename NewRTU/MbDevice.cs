using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NewRTU
{
    public class MbDevice
    {
        private SerialPort sp = new SerialPort();
        public bool Open(string portName, int baudRate, int databits, Parity parity, StopBits stopBits)
        {
            //Ensure port isn't already opened:
            if (!sp.IsOpen)
            {
                //Assign desired settings to the serial port:
                sp.PortName = portName;
                sp.BaudRate = baudRate;
                sp.DataBits = databits;
                sp.Parity = parity;
                sp.StopBits = stopBits;
                //These timeouts are default and cannot be editted through the class at this point:
                sp.ReadTimeout = 2000;
                sp.WriteTimeout = 2000;
                try
                {
                    sp.Open();
                }
                catch (Exception err)
                {
                   
                    return false;
                }
                
                return true;
            }
            else
            {
               
                return false;
            }
        }
        #region Check Response
        private bool CheckResponse(byte[] response)
        {
            //Perform a basic CRC check:
            byte[] CRC = new byte[2];
            GetCRC(response, ref CRC);
            if (CRC[0] == response[response.Length - 2] && CRC[1] == response[response.Length - 1])
                return true;
            else
                return false;
        }
        #endregion

        #region Get Response
        private void GetResponse(ref byte[] response)
        {
            //There is a bug in .Net 2.0 DataReceived Event that prevents people from using this
            //event as an interrupt to handle data (it doesn't fire all of the time).  Therefore
            //we have to use the ReadByte command for a fixed length as it's been shown to be reliable.
            for (int i = 0; i < response.Length; i++)
            {
                response[i] = (byte)(sp.ReadByte());
            }
        }
        #endregion
        #region CRC Computation
        private void GetCRC(byte[] message, ref byte[] CRC)
        {
            //Function expects a modbus message of any length as well as a 2 byte CRC array in which to 
            //return the CRC values:

            ushort CRCFull = 0xFFFF;
            byte CRCHigh = 0xFF, CRCLow = 0xFF;
            char CRCLSB;

            for (int i = 0; i < (message.Length) - 2; i++)
            {
                CRCFull = (ushort)(CRCFull ^ message[i]);

                for (int j = 0; j < 8; j++)
                {
                    CRCLSB = (char)(CRCFull & 0x0001);
                    CRCFull = (ushort)((CRCFull >> 1) & 0x7FFF);

                    if (CRCLSB == 1)
                        CRCFull = (ushort)(CRCFull ^ 0xA001);
                }
            }
            CRC[1] = CRCHigh = (byte)((CRCFull >> 8) & 0xFF);
            CRC[0] = CRCLow = (byte)(CRCFull & 0xFF);
        }
        #endregion
        #region Send to serial
        public byte[] SendToMb(byte[] request)
        {
           List<byte> _response = new List<byte>();
            //Переписываем заголовок запроса в ответ  зю циаголовок не включает себя станцию и длинну
            for (int i = 0; i < 6; i++)
            {
                _response.Add(request[i]);
            }
            //Формируем запрос для RTU
            List<byte> requestMB = new List<byte>();
            for (int j = 6; j < request.Length; j++)
            {
                requestMB.Add(request[j]);
            }
            byte[] crc = new byte[2];
            byte[] nocrc =requestMB.ToArray();//баз crc
            GetCRC(nocrc, ref crc);
            requestMB.Add(crc[1]);
            requestMB.Add(crc[2]);

            sp.Write(requestMB.ToArray(), 0, requestMB.ToArray().Length);
            byte[] responRTU=null;
            if (request[7] == 3)
            {
                //Получаем количество регистров
                byte[] _regcount = { request[11], request[10] };
                //Получаем стартовый адресс
                byte[] _start = { request[9], request[8] };

                Int16 regcount = BitConverter.ToInt16(_regcount, 0);
                Int16 start = BitConverter.ToInt16(_start, 0);
                //Function 3 response buffer:
                responRTU = new byte[5 + 2 * regcount];                
            }
            if (request[7] == 6)
            {

            }
            if (request[7] == 4)
            {
                //Получаем количество регистров
                byte[] _regcount = { request[11], request[10] };
                //Получаем стартовый адресс
                byte[] _start = { request[9], request[8] };

                Int16 regcount = BitConverter.ToInt16(_regcount, 0);
                Int16 start = BitConverter.ToInt16(_start, 0);
                //Function 3 response buffer:
                responRTU = new byte[5 + 2 * regcount];    
            }
            GetResponse(ref responRTU);
            foreach (byte b in responRTU)
            {
                _response.Add(b);
            }
            return _response.ToArray();
        }
        #endregion
    }

}
