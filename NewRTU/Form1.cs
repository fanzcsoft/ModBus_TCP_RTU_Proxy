using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO.Ports;
using System.Linq;
using System.Net.Sockets;
using System.Text;

using System.Windows.Forms;

namespace NewRTU
{
    enum msgStatus
    {
        Common,
        Send,
        Recived,
        Error,
        Connect,
        Success
    };
    
    public partial class Form1 : Form
    {
        #region Переменные
        delegate void SetTextCallback(string text, msgStatus msgstatus);
        Dictionary<int, modbus> deviceList;
        public int DataRate { get; set; }
        public int DataBits { get; set; }
        public Parity DataParity { get; set; }
        public StopBits DataStopBit { get; set; }
        bool inWorkFlag;
        Mbsettings mbs;
        Logger log;
        Server server;
        #endregion
        public Form1()
        {
            InitializeComponent();
            log = new Logger();
        }
        private void Init()
        {
            try
            {
                System.Drawing.Icon ico = new Icon("cyberduck.ico");
                this.Icon = ico;
            }
            catch
            { 
            
            }
             mbs = Mbsettings.LoadFromFile();

           server = new Server();
           server.OnDataRecieve += server_OnDataRecieve;
            server.OnClientConnect+=server_OnClientConnect;
            server.OnServerError += server_OnServerError;
            inWorkFlag = false;
            DataRate = 9600;
            DataBits = 8;
           // DataParity = Parity.Even;
           // DataStopBit = StopBits.One;
           // Parity p;
         //  p= (Parity)Enum.Parse(typeof(Parity), comboBox3.SelectedItem.ToString(), true);
          //  DataParity = p;
            DataParity = mbs.Parity;

            //StopBits sb;
           // sb = (StopBits)Enum.Parse(typeof(StopBits), comboBox4.SelectedItem.ToString(), true);
           // DataStopBit = sb;

            comboBox3.SelectedItem = mbs.Parity;
            comboBox4.SelectedItem = mbs.StopBit;
            checkBox1.Checked = mbs.Autostart;
            DataStopBit = mbs.StopBit;
            deviceList = new Dictionary<int, modbus>();
        }

        void server_OnServerError(object sender, NotyArgs e)
        {
            this.BeginInvoke(new SetTextCallback(SetText), new object[] { "Ошибка сокет-сервера: " + e.Message, msgStatus.Error });
           
        }

        private void server_OnClientConnect(object sender, ClientConnectArgs e)
        {
            this.BeginInvoke(new SetTextCallback(SetText), new object[] { "Client connected, Client EndPoint: " + e.RemoteEndPoint, msgStatus.Connect });    
        }
        private void button1_Click(object sender, EventArgs e)
        {
            if(!inWorkFlag)
            {
            Start();
           
            }
            else 
            {
                this.BeginInvoke(new SetTextCallback(SetText), new object[] { "Сервер уже запущен ", msgStatus.Common});    
            }
        }
        #region Стоп старт
        public void Start()
        {
            log.LogTofile("Команда запуска сервера");
            Init();
            server.Start(502);
            comboBox4.Enabled = false;
            comboBox3.Enabled = false;
            checkBox1.Enabled = false;
            inWorkFlag = true;
            SetText("Сервер запущен", msgStatus.Common);
            log.LogTofile("Сервер запущен");
        }
        public void Stop()
        {
            try
            {
                log.LogTofile("команда остановки сервера");
                inWorkFlag = false;
                server.Stop();
                foreach (modbus mb in deviceList.Values)
                {
                    mb.Close();
                }
                SetText("Сервер остановлен, все порты закрыты", msgStatus.Common);
                log.LogTofile("сервер остановлен");
            }
            catch (Exception)
            {
                
                throw;
            }               
            
        }
        #endregion
        #region Обработка запросов
        /// <summary>
        /// Парсим запрос, получаем stationId проверяем есть ли такой в списке, если нет оздаем
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void server_OnDataRecieve(object sender, DataRecieveArgs e)
        {
            //если ведется опрос продолжаем инче выходим из функции
            if (inWorkFlag)
            {
                byte[] data = null;
                byte[] response = null;
                try
                {
                    Socket socket = (Socket)sender;
                     data = e.Data;
                    this.BeginInvoke(new SetTextCallback(SetText), new object[] { "Запрос: " + Logger.BytePrinter(data), msgStatus.Recived });
                   //Получение ответа
                    //ответ может быть с кодом ошибки, пустым, или валидным
                  response=GetResponsMessage(data);

                  if (response != null)
                  {
                      if (inWorkFlag && (socket.Connected) && server.isOnline && (server!=null))
                      {                         
                          server.sendPackage(response, socket);
                          this.BeginInvoke(new SetTextCallback(SetText), new object[] { "Ответ: " + Logger.BytePrinter(response), msgStatus.Send });
                      }
                      else
                      {
                          this.BeginInvoke(new SetTextCallback(SetText), new object[] {"Сервер или клиент отключены, опрос не производится", msgStatus.Common });
                      }
                  }
                  else 
                  {
                      this.BeginInvoke(new SetTextCallback(SetText), new object[] { "Ответ: ошибка" , msgStatus.Error });
                  }
                   
                }
                catch (Exception ex)
                {
                    this.BeginInvoke(new SetTextCallback(SetText), new object[] { "Ошибка в блоке обработки запроса" + ex.Message, msgStatus.Error });
                    return;
                }
                          
            }
            else
            {
                return;
            }
        }
        #endregion
        #region  Создание модбас устройства
        modbus getPortByStationId(int stationAddress)
        {
            modbus mb = null;           
                //Проверяем есть ли ком порта соответсвующий stationId
                string[] ports = SerialPort.GetPortNames();
                bool _portFound = false;
                foreach (string p in ports)
                {
                    if (p == "COM" + stationAddress.ToString())
                    {
                        _portFound = true;
                    }
                }
                //Порт соответсвующий stationId найден
                if (_portFound)
                {

                    if (deviceList.ContainsKey(stationAddress))
                    {
                        mb = (deviceList.SingleOrDefault(d => d.Key == stationAddress).Value);      
                    }
                    else
                    {
                        mb = new modbus();  
                    }
                    //Порт не открыт после создания и после его закрытия вследствии нажантия на кнопку стоп
                    if (!mb.isOpen)
                    {
                        if (mb.Open("COM" + stationAddress, DataRate, DataBits, DataParity, DataStopBit))
                        {
                            deviceList.Add(stationAddress, mb);
                            this.BeginInvoke(new SetTextCallback(SetText), new object[] { "Порт для StationId  " + stationAddress + " успешно открыт. MBStatus: " + mb.modbusStatus, msgStatus.Success });
                        }
                        else
                        {
                            throw new Exception("Не удалось открыть порт. " + mb.modbusStatus);
                        }

                    }
                   
                }
                else
                {
                    this.BeginInvoke(new SetTextCallback(SetText), new object[] {"Com port соответсвующий StationId не найден stationId: " + stationAddress, msgStatus.Common });                   
                }  
         
            return mb;
        }
        #endregion
        #region Формирование ответа
        byte[] GetResponsMessage(byte[] request)
        {
            byte stationAddress = 0;
            List<byte> _response = null;
            //Возможна ошибка выхода за приделы массива, в этом случае вернем null
            try
            {
                //разбирваем запрос что бы получить stationId
                stationAddress = request[6];
                //Ответ 
                _response = new List<byte>();
                //Переписываем заголовок запроса в ответ 
                for (int i = 0; i < 5; i++)
                {
                    _response.Add(request[i]);
                }
            }
            catch (Exception e)
            {
                return null;
            }

                try
                {
               
                    //проверяем есть ли данный компорт в списке устройств
                    //если он есть. значит он был добавлен в предыдущих обработках  
                    //если порт еще не существует в списке, создаем порт, и добовляем его туда

                   // modbus mb;
                    modbus mb = getPortByStationId(stationAddress);
                    //если нет соответсвуещего порта
                    if (mb == null)
                    {
                        return errorReturn(request);
                    }
                    //Если порт открыт

                    //Определить какую modbus функцию использовать 
                    byte _functionCode = request[7];
                    bool successFlag = false;

                    //---------------------------------------
                   //Перед выполнением запросов к порту проверим открыт ли он
                    if (!mb.isOpen)
                    {
                        throw new Exception("Порт закрыт не возможно производить операции чтения и записи  stationId: " + stationAddress);                       
                    }
                    //функция записи
                    //
                    if (_functionCode == 6)
                    {
                        //Получаем стартовый адресс
                        byte[] _start = { request[9], request[8] };
                        //Получаем значение для записи
                        byte[] _value = { request[11], request[10] };
                        Int16 strt = BitConverter.ToInt16(_start, 0);
                        Int16 val = BitConverter.ToInt16(_value, 0);
                        successFlag = mb.SendFc6(1, (ushort)strt, (ushort)val);
                        if (!successFlag)
                        {
                            throw new Exception("ModBus функция выполнена с ошибкой запрос "+Logger.BytePrinter(request)+" FC " + _functionCode + "  stationId: " + stationAddress);
                        }
                        return request;
                    }
                    //f-ции чтения
                    if ((_functionCode == 3) || (_functionCode == 4))
                    {
                       List<byte> resp=new List<byte> ();
                        //Получаем стартовый адресс
                        byte[] _start = { request[9], request[8] };
                        //Получаем количество регистров
                        byte[] _regcount = { request[11], request[10] };

                        Int16 regcount = BitConverter.ToInt16(_regcount, 0);
                        Int16 start = BitConverter.ToInt16(_start, 0);
                        //Так как ТРД10 воспринимает запросы не более чем для 10 регистов
                        //Приходится разбивать запросы по максимум 10 регистров

                        //Выходной массив значений
                        short[] values = new short[regcount];
                        //Массив значений для подзапроса
                        short[] values10 = new short[10];

                        //Алгоритм следующий
                        //В цикле делаем запросы по 10 регстров максимум, количество итераций цикал подсчитывется так:
                        //Если остаток от деления исходного количества регистров на 10 больше 0, то есть будет 
                        //N Итераций по 10 и еще одна итерация по остатку, всего получается N+1 иетрация
                        //В последней этерации будет использоватся массив не из 10 а из остатка регистров

                        //Если же остаток от деления равен 0
                        //Ток тогда количество итераций N

                        int _counter=0;
                        int _maxcount=0;
                        if (regcount % 10 > 0)
                        {
                            _maxcount = regcount / 10;
                            _maxcount += 1;
                        }
                        else
                        {
                            _maxcount = regcount / 10;                        
                        }
                        while (_counter <_maxcount)
                        {
                            int _lastNum = 10;
                            //Последняя итерация, проверяем если с остатоком деление значит на последней этерации массив не 10 а из остатка
                            if ((_counter == _maxcount - 1) && (regcount % 10 > 0))
                            {
                                _lastNum = regcount % 10;
                                values10 = new short[_lastNum];
                            }
                            if (_functionCode == 3)
                            {
                                //successFlag = mb.SendFc3(1, (ushort)start, (ushort)regcount, ref values, ref resp);
                                successFlag = mb.SendFc3(1, (ushort)(start + 10 * _counter), (ushort)_lastNum, ref values10, ref resp);
                                
                            }
                            if (_functionCode == 4)
                            {
                                //successFlag = mb.SendFc4(1, (ushort)start, (ushort)regcount, ref values, ref resp);
                                successFlag = mb.SendFc4(1, (ushort)(start + 10 * _counter), (ushort)_lastNum, ref values10, ref resp);
                            }
                            if (!successFlag)
                            {
                                throw new Exception(@"ModBus функция выполнена с ошибкой" + "\n" +
                                    "Запрос: " + Logger.BytePrinter(request) + "\n" +
                                    "Ответ устройства на итерации: " + _counter + ": "+ Logger.BytePrinter(resp.ToArray()) + "\n" + 
                                   " FC " + _functionCode + "  stationId: " + stationAddress + "modbus status: " + mb.modbusStatus);
                            } 
                            for (int i = 0; i <_lastNum; i++)
                            {
                                values[i + 10 * _counter] = values10[i];
                            }
                            
                            _counter++;
                            successFlag = false;
                        }
                        //Формируем массив значений для полного ответа
                        List<byte> valuesResponse = new List<byte>();
                        for (int i = 0; i < regcount; i++)
                        {
                            valuesResponse.AddRange(BitConverter.GetBytes(values[i]).Reverse());
                        }
                        //Получем длину ответа
                        byte valuesLen = (byte)valuesResponse.Count;
                        byte allLen = (byte)(valuesLen + 3);
                        //Формируем полный ответ, он включает в себя заголовок. злинну, адресс, длну значений, и полученые значения
                        _response.Add(allLen);
                        _response.Add(stationAddress);
                        _response.Add(_functionCode);
                        _response.Add(valuesLen);
                        foreach (byte b in valuesResponse)
                        {
                            _response.Add(b);
                        }
                    }     
            }
                  //есил исключение то отправляем ответ с кодом ошибки 131 3  
            catch (Exception excap)
            {
                this.BeginInvoke(new SetTextCallback(SetText), new object[] { excap.Message , msgStatus.Error });
                _response.Add(request[7]);
                _response.Add(request[6]);
                _response.Add(131);
                _response.Add(3);
            }
            return _response.ToArray();
        }
        #endregion
        #region Ответ ошибка

        byte[] errorReturn(byte[] request)
        {

            byte stationAddress = 0;
            List<byte> _response = null;
            //Возможна ошибка выхода за приделы массива, в этом случае вернем null
            
          

            try
            {
                if (request.Length < 7)
                {
                    throw new Exception("запрос не достаточной длинны");
                }
                stationAddress = request[6];
                //Ответ 
                _response = new List<byte>();
                //Переписываем заголовок запроса в ответ 
                for (int i = 0; i < 5; i++)
                {
                    _response.Add(request[i]);
                }
                _response.Add(request[7]);
                _response.Add(request[6]);
                _response.Add(131);
                _response.Add(3);
            }
            catch
            {
                throw new Exception("Ошибка при формировании ответа 131 3");
            }
            return _response.ToArray();
        }
        #endregion
        #region Вывод текста в текстовое поле
        private void SetText(string text, msgStatus msgStatus)
        {
            try
            {
                Color col = Color.Black;
                if (msgStatus == msgStatus.Recived)
                {
                    col = Color.Blue;
                }
                if (msgStatus == msgStatus.Send)
                {
                    col = Color.Green;
                }
                if (msgStatus == msgStatus.Connect)
                {
                    col = Color.DarkOrange;
                    log.LogTofile(text);
                }
                if (msgStatus == msgStatus.Error)
                {
                    col = Color.Red;
                    log.LogTofile(text);
                }
                if (msgStatus == msgStatus.Common)
                {
                    col = Color.DarkSeaGreen;
                }
                if (msgStatus == msgStatus.Success)
                {
                    col = Color.Violet;
                    log.LogTofile(text);
                }
                richTextBox1.SelectionStart = richTextBox1.TextLength;
                richTextBox1.SelectionLength = 0;

                richTextBox1.SelectionColor = col;
                richTextBox1.AppendText(DateTime.Now.ToShortTimeString() + ": " + text);
                richTextBox1.AppendText("\n");
                richTextBox1.SelectionColor = richTextBox1.ForeColor;
            }
            catch (Exception ex)
            {
                log.LogTofile("Ошибка вывода на экран (setText) " + ex.Message);
            }
        }
        #endregion

        private void button2_Click(object sender, EventArgs e)
        {
            Stop();
            comboBox4.Enabled = true;
            comboBox3.Enabled = true;
            checkBox1.Enabled = true;
        }

        private void richTextBox1_TextChanged(object sender, EventArgs e)
        {
            try
            {
                richTextBox1.SelectionStart = richTextBox1.Text.Length; //Set the current caret position at the end
                richTextBox1.ScrollToCaret(); //Now scroll it automatically
                if (richTextBox1.Lines.Length > 1000)
                {
                    // richTextBox1.Select(0, richTextBox1.GetFirstCharIndexFromLine(richTextBox1.Lines.Length - 20));
                    //richTextBox1.SelectedText = "";
                    richTextBox1.Text = "";
                }
            }
            catch (Exception ex)
            {
                log.LogTofile("Ошибка вывода на экран " + ex.Message);
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            richTextBox1.BackColor = Color.Black;
            richTextBox1.Font = new System.Drawing.Font("Tahoma", 12);
            comboBox3.DataSource = Enum.GetValues(typeof(Parity));
            comboBox4.DataSource = Enum.GetValues(typeof(StopBits));
            //comboBox3.SelectedItem = Parity.Even;
           // comboBox4.SelectedItem = StopBits.One;
            Init();
            if (mbs.Autostart)
            {
                Start();
            }
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
           
        }

        private void сохранитьНастройкиToolStripMenuItem_Click(object sender, EventArgs e)
        {
            mbs.Autostart = checkBox1.Checked;
            mbs.Parity = (Parity)comboBox3.SelectedItem;
            mbs.StopBit = (StopBits)comboBox4.SelectedIndex;
            Mbsettings.SaveToFile(mbs);
        }
    }
}
