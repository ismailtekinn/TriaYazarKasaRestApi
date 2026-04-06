using System;
using System.IO.Ports;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace TriaYazarKasaRestApi.Business.Libraries.Hugin.Internal
{
    public interface IConnection
    {
        void Open();
        bool IsOpen { get; }
        void Close();
        int FPUTimeout { get; set; }
        object ToObject();
        int BufferSize { get; set; }
    }

    public class MySerialPort : SerialPort
    {
        public MySerialPort(string portName, int baudRate) : base(portName, baudRate)
        {
        }
    }

    public class SerialConnection : IConnection
    {
        private readonly string _portName;
        private readonly int _baudRate;
        private static MySerialPort? _sp;
        private static int _supportedBufferSize = 4096;

        public SerialConnection(string portName, int baudRate)
        {
            _portName = portName;
            _baudRate = baudRate;

            try
            {
                if (IsOpen)
                    Close();
            }
            catch
            {
            }
        }

        public void Open()
        {
            _sp = new MySerialPort(_portName, _baudRate);
            _sp.WriteTimeout = 4500;
            _sp.ReadTimeout = 4500;
            _sp.ReadBufferSize = _supportedBufferSize;
            _sp.WriteBufferSize = _supportedBufferSize;
            _sp.Encoding = Encoding.GetEncoding(1254);
            _sp.Open();
        }

        public bool IsOpen => _sp != null && _sp.IsOpen;

        public void Close()
        {
            if (_sp != null && _sp.IsOpen)
                _sp.Close();
        }

        public int FPUTimeout
        {
            get => _sp?.ReadTimeout ?? 4500;
            set
            {
                if (_sp != null)
                    _sp.ReadTimeout = value;
            }
        }

        public object ToObject()
        {
            if (_sp == null)
                throw new InvalidOperationException("Serial port acik degil.");

            return _sp;
        }

        public int BufferSize
        {
            get => _sp?.ReadBufferSize ?? _supportedBufferSize;
            set
            {
                var wasOpen = IsOpen;
                if (wasOpen)
                    Close();

                _supportedBufferSize = value;

                if (wasOpen)
                    Open();
            }
        }
    }

    public class TCPConnection : IConnection, IDisposable
    {
        private Socket? _client;
        private readonly string _ipAddress;
        private readonly int _port;
        private static int _supportedBufferSize = 4096;

        public TCPConnection(string ipAddress, int port)
        {
            _ipAddress = ipAddress;
            _port = port;
        }

        ~TCPConnection()
        {
            Dispose();
        }

        public void Open()
        {
            Close();

            var endPoint = new IPEndPoint(IPAddress.Parse(_ipAddress), _port);
            _client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            _client.ReceiveTimeout = 4500;
            _client.ReceiveBufferSize = _supportedBufferSize;
            _client.SendBufferSize = _supportedBufferSize;
            _client.Connect(endPoint);
        }

        public bool IsOpen => _client != null && _client.Connected;

        public void Close()
        {
            if (_client != null)
            {
                try
                {
                    if (_client.Connected)
                        _client.Shutdown(SocketShutdown.Both);
                }
                catch
                {
                }

                try
                {
                    _client.Close();
                }
                catch
                {
                }
            }
        }

        public int FPUTimeout
        {
            get => _client?.ReceiveTimeout ?? 4500;
            set
            {
                if (_client != null)
                    _client.ReceiveTimeout = value;
            }
        }

        public int BufferSize
        {
            get => _client?.SendBufferSize ?? _supportedBufferSize;
            set
            {
                var wasOpen = IsOpen;
                if (wasOpen)
                    Close();

                _supportedBufferSize = value;

                if (wasOpen)
                    Open();
            }
        }

        public void Dispose()
        {
            try
            {
                Close();
            }
            catch
            {
            }
        }

        public object ToObject()
        {
            if (_client == null)
                throw new InvalidOperationException("TCP baglantisi acik degil.");

            return _client;
        }
    }
}
