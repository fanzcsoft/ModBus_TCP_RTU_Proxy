using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace NewRTU
{
    public delegate void OnClientConnectHandler(object sender, ClientConnectArgs e);
    public delegate void OnClientDisconnectHandler(object sender, ClientDisconnectArgs e);
    public delegate void OnServerErrorHandler(object sender, NotyArgs e);
    public delegate void OnDataRecieveHandler(object sender, DataRecieveArgs e);

    public class Server
    {
        public event OnClientConnectHandler OnClientConnect;
        public event OnServerErrorHandler OnServerError;
        public event OnClientDisconnectHandler OnClientDisconnect;
        public event OnDataRecieveHandler OnDataRecieve;
        public bool isOnline;
         Socket _serverSocket;
         byte[] _buffer = new byte[1024];
         Dictionary<Socket, byte[]> _buffersList;

        public Server()
        {
            try
            {
                _serverSocket = new Socket
                (AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                _buffersList = new Dictionary<Socket, byte[]>();
            }
            catch (Exception ex)
            {
                if (OnServerError != null)
                {
                    OnServerError(null, new NotyArgs(ex.Message));
                }
            }
        }
        public void Start(int port)
        {
            try
            {
                  ushort _timeout = 2000;
                  isOnline = true;
                   // ushort _refresh = 10;
                _serverSocket.Bind(new IPEndPoint(IPAddress.Any, port));
                _serverSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.SendTimeout, _timeout);
                _serverSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveTimeout, _timeout);
                _serverSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.NoDelay, 1);
                _serverSocket.Listen(50);
                _serverSocket.BeginAccept(_serverSocket.ReceiveBufferSize, new AsyncCallback(AcceptCallback), null);

            }
            catch (Exception ex)
            {
                if (OnServerError != null)
                {
                    OnServerError(null, new NotyArgs(ex.Message));
                }
            }
        }

        private void AcceptCallback(IAsyncResult ar)
        {
            
            try
            {
                if (isOnline == true)
                {
                    //byte[] buffer = new byte[_serverSocket.ReceiveBufferSize];
                    byte[] buffer;
                    Socket socket = (Socket)_serverSocket.EndAccept(out buffer, ar);
                    _buffersList.Add(socket, buffer);
                    if (OnClientConnect != null)
                    {
                        OnClientConnect(socket, new ClientConnectArgs(socket.RemoteEndPoint));
                    }
                    if (OnDataRecieve != null)
                    {
                        OnDataRecieve(socket, new DataRecieveArgs(buffer));
                    }
                    // socket.BeginReceive(_buffer, 0, _buffer.Length, SocketFlags.None, new AsyncCallback(ReceiveCallback), socket);
                    //_serverSocket.BeginAccept(new AsyncCallback(AcceptCallback), null);
                    _serverSocket.BeginAccept(_serverSocket.ReceiveBufferSize, new AsyncCallback(AcceptCallback), null);
                }
            }
            catch (Exception ex)
            {
                if (OnServerError != null)
                {
                    OnServerError(null, new NotyArgs(ex.Message));
                }
            }
        }

        private void ReceiveCallback(IAsyncResult ar)
        {
            Socket socket = (Socket)ar.AsyncState;
            try
            {
                if (isOnline == true)
                {
                    int received = socket.EndReceive(ar);
                    if (received == 0)
                    {
                        // socket.Disconnect(true);
                        return;
                    }
                    byte[] dataBuff = new byte[received];
                    byte[] _buf = _buffersList.SingleOrDefault(k => k.Key == socket).Value;
                    // _buf = new byte[dataBuff.Length];
                    Array.Copy(_buf, dataBuff, received);

                    // DataPackageBase data=IDTP.GetInstanceByType(dataBuff);

                    if (OnDataRecieve != null)
                    {
                        OnDataRecieve(socket, new DataRecieveArgs(dataBuff));
                    }
                    // socket.BeginReceive(_buffer, 0, _buffer.Length, SocketFlags.None, new AsyncCallback(ReceiveCallback), socket);
                }
            }
            catch (Exception ex)
            {

                if (OnClientDisconnect != null)
                {
                    OnClientDisconnect(null, new ClientDisconnectArgs());
                }

                if (OnServerError != null)
                {
                    OnServerError(null, new NotyArgs(ex.Message));
                }
            }
        }

        public void sendPackage(byte[] _data, Socket socket)
        {
            // DataPackage _data = new DataPackage("First package", 9999, true);
            // DroidMessage _data = new DroidMessage("Hello socket server");
            try
            {
                if (isOnline == true)
                {
                    byte[] data = _data;
                    //_clientSocket.Send(buffer);
                    // socket.Send(_data);

                    socket.BeginSend(data, 0, data.Length, SocketFlags.None, new AsyncCallback(SendCallback), socket);
                    byte[] _buf = _buffersList.SingleOrDefault(k => k.Key == socket).Value;

                    // socket.BeginReceive(_buffer, 0, _buffer.Length, SocketFlags.None, new AsyncCallback(ReceiveCallback), socket);
                    socket.BeginReceive(_buf, 0, _buf.Length, SocketFlags.None, new AsyncCallback(ReceiveCallback), socket);
                }
            }
            catch (Exception ex)
            {
                if (OnServerError != null)
                {
                    OnServerError(null, new NotyArgs(ex.Message));
                }
            }
        }
        private static void SendCallback(IAsyncResult ar)
        {
            if (ar.IsCompleted == false) 
            { 
                throw(new Exception("Sending error"));
            }
           // Socket socket = (Socket)ar.AsyncState;
          //  socket.EndSend(ar);
        }
        public  void Stop()
        {
            try
            {
                isOnline = false;
               
               _serverSocket.Close();
                foreach (Socket s in _buffersList.Keys)
                {
                    // s.Shutdown(SocketShutdown.Both);
                    s.Close();
                }
                //  _serverSocket.Shutdown(SocketShutdown.Both);
               

                //_serverSocket.Close();

                _buffersList = new Dictionary<Socket, byte[]>();
            }
            catch (Exception ex)
            {
                if (OnServerError != null)
                {
                    OnServerError(null, new NotyArgs(ex.Message + " Не корректная остановка сервера. Необходима перезагрузка системы!"));
                }
            }
            finally
            {
                _buffersList = new Dictionary<Socket, byte[]>();
            }
        }
    }
}
