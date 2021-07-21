using System;
using System.IO;
using System.IO.Ports;
using System.Runtime.InteropServices;
using System.Threading;
using UnityEngine;

#pragma warning disable 0414

namespace SerialPortUtility
{
    [System.Serializable]
    public class SPUPEventObject : UnityEngine.Events.UnityEvent<object> { }
    [System.Serializable]
    public class SPUPSystemEventObject : UnityEngine.Events.UnityEvent<SerialPortUtility.SerialPortUtilityPro, string> { }

#if UNITY_5_5_OR_NEWER
    [DefaultExecutionOrder(-100)]
#endif
    [AddComponentMenu("SerialPort/SerialPort Utility Pro")]
    public class SerialPortUtilityPro : MonoBehaviour
    {
        #region Defines
        //spapmain.cpp
        //SPAP snapConfig
        [StructLayout(LayoutKind.Sequential)]
        public struct SpupConfig
        {
            //Config
            public PortService PortService;
            public int BaudRate;    //32bit x6
            public Parity Parity;
            public StopBits StopBits;
            public Int32 DataBit;

            public int Skip;
        }

        public enum DataLayout
        {
            Bytes = 0,
            String,
            CharArray,
        }
        #endregion

        #region Params
        public bool IsAutoOpen = true;
        public PortService portService = PortService.CH341SER_A64;
        public DataLayout dataLayout = DataLayout.Bytes;

        [SerializeField]
        private string[] AvailableParserTypeNames = null;
        [SerializeField]
        private string ParserTypeName = null;

        public int BaudRate = 9600;
        public Parity Parity = Parity.None;
        public StopBits StopBit = StopBits.One;
        public Int32 DataBit = 8;
        /// <summary>
        /// 设备编号
        /// </summary>
        public int Skip = 0;

        public SPUPEventObject ReadCompleteEventObject = new SPUPEventObject();
        public string ReadCompleteEventObjectType = "";
        public GameObject ReadClassMembersObject = null;

        public SPUPSystemEventObject SystemEventObject = new SPUPSystemEventObject();

        //for Editor
        [SerializeField]
        private bool ExpandConfig = true;
        [SerializeField]
        private bool ExpandEventConfig = false;

        private const int SPAPHANDLE_ERROR = (-1);
        private const int READDATA_ERROR_DISCONNECT = (-2);
        private const int SPAPHANDLE_UNFIND = (-3);
        private const int SPAPHANDLE_PERMISSION = (1);

        public int SerialPortHandle = SPAPHANDLE_ERROR;
        public bool IsErrFinished = false;

        /// <summary>
        /// 串口接口
        /// </summary>
        private SerialPort _serialPort;
        /// <summary>
        /// 数据解析器
        /// </summary>
        public IParser CurrentParser;
        /// <summary>
        /// 读取线程
        /// </summary>
        private Thread _thread;
        /// <summary>
        /// 断开重连延迟
        /// </summary>
        private int _delayBeforeReconnecting;
        /// <summary>
        /// 读超时
        /// </summary>
        public int _readTimeout = 500;
        /// <summary>
        /// 写超时
        /// </summary>
        public int _writeTimeout = 500;
        /// <summary>
        /// 线程继续标志位
        /// </summary>
        private bool _keepReading = true;
        /// <summary>
        /// 串口名称
        /// </summary>
        private string _portName = "";
        /// <summary>
        /// 获取串口名称
        /// </summary>
        public string PortName
        {
            get
            {
                return _portName;
            }
        }
        /// <summary>
        /// 获取端口可用
        /// </summary>
        public bool FindAvaliablePort { get; private set; } = false;
        private bool isQuiting = false;
        #endregion

        #region Builtin Methods
        // Start is called before the first frame update
        void Start()
        {
            if (dataLayout == DataLayout.Bytes)
            {
                Type parserType = Utility.Assembly.GetType(ParserTypeName);
                if (parserType == null)
                {
                    Debug.LogErrorFormat("Can not find procedure type '{0}'.", ParserTypeName);
                    return;
                }

                CurrentParser = (IParser)Activator.CreateInstance(parserType, new object[] { this });

                if (CurrentParser == null)
                {
                    Debug.LogError("Entrance procedure is invalid.");
                    return;
                }
            }
            
            Connect();
        }
        // OnApplicationQuit is called when application exit
        void OnApplicationQuit()
        {
            isQuiting = true;
            Close();
        }
        #endregion

        #region Custom Methods
        /// <summary>
        /// 开启线程
        /// </summary>
        public void Open()
        {
            if (IsOpened()) //Opened
                return;

            IsErrFinished = false;

            SpupConfig config;
            config.PortService = portService;
            config.BaudRate = BaudRate;
            config.Parity = Parity;
            config.DataBit = DataBit;
            config.StopBits = StopBit;
            config.Skip = Skip;

            FindAvaliablePort = PortUtil.GetPortName(config.Skip, out _portName, config.PortService);
            if (!FindAvaliablePort)
            {
                SerialPortHandle = SPAPHANDLE_UNFIND;

                SystemEventFire("UNFIND");
                Invoke("Connect", 3);

                return;
            }

            _serialPort = new SerialPort(_portName, config.BaudRate, config.Parity, config.DataBit, config.StopBits)
            {
                ReadTimeout = _readTimeout,
                WriteTimeout = _writeTimeout
            };

            try
            {
                if (!_serialPort.IsOpen)
                {
                    _serialPort.Open();
                }
                Loom.RunAsync(() =>
                {
                    _thread = new Thread(new ThreadStart(Reading))
                    {
                        IsBackground = true
                    };
                    _thread.Start();
                });

                SerialPortHandle = SPAPHANDLE_PERMISSION; 
            }
            catch
            {
                SerialPortHandle = SPAPHANDLE_ERROR;
            }

            if (SerialPortHandle == SPAPHANDLE_PERMISSION)
            {
                Debug.Log("<color=yellow>" + _portName + "打开成功</color>");
                SystemEventFire("OPENED");
            }
            else if (SerialPortHandle == SPAPHANDLE_ERROR)
            {
                Debug.LogError("<color=red>" + _portName + "打开错误</color>");
                SystemEventFire("ERROR");
            }
        }
        /// <summary>
        /// 关闭线程
        /// </summary>
        public void Close()
        {
            _keepReading = false;
            CloseDevice();

            if (_thread != null)
            {
                _thread.Abort();
                Debug.Log("<color=yellow>线程已被释放</color>");
            }
            
            SerialPortHandle = SPAPHANDLE_ERROR;
            SystemEventFire("CLOSED");
        }
        /// <summary>
        /// 关闭设备
        /// </summary>
        private void CloseDevice()
        {
            if (_serialPort == null)
                return;

            try
            {
                _serialPort.Close();
                Debug.Log("<color=yellow>串口已被关闭</color>");
            }
            catch (IOException)
            {
                // Nothing to do, not a big deal, don't try to cleanup any further.
                Debug.LogError("<color=red>串口关闭失败</color>"); 
            }

            FindAvaliablePort = false;
            _serialPort = null;
        }
        /// <summary>
        /// 串口数据读取
        /// </summary>
        public void Reading()
        {
            while (_keepReading)
            {
                try
                {
                    switch (dataLayout)
                    {
                        case DataLayout.Bytes:
                            ReadBytes();
                            break;
                        case DataLayout.String:
                            ReadString();
                            break;
                        case DataLayout.CharArray:
                            break;
                        default:
                            break;
                    }
                }
                catch (Exception ioe)
                {                
                    if (!isQuiting)
                    {
                        SerialPortHandle = SPAPHANDLE_ERROR;
                        Debug.LogWarning("Exception: " + ioe.Message + " StackTrace: " + ioe.StackTrace);
                        CloseDevice();
                        SystemEventFire("EDISCONNECTED");

                        Loom.QueueOnMainThread(() => Connect());
                    }
                    break;
                }
            }
        }
        /// <summary>
        /// 读取字节
        /// </summary>
        private void ReadBytes()
        {
            try
            {
                byte[] recv = new byte[1024];
                int recvLen = _serialPort.Read(recv, 0, _serialPort.BytesToRead);

                if (recvLen > 0)
                {
                    for (int i = 0; i < recvLen; i++)
                    {
                        CurrentParser.parseByte(recv[i]);
                    }
                }
            }
            catch (TimeoutException e)
            {
                Debug.Log("TimeOut Exception : " + e.StackTrace);
            }
            Thread.Sleep(1);
        }
        /// <summary>
        /// 读取字符串
        /// </summary>
        private void ReadString()
        {
            try
            {
                string message = _serialPort.ReadLine();

                if (!string.IsNullOrEmpty(message))
                {
                    ReadEventFire(message);
                }
            } catch { }
        }
        /// <summary>
        /// 读取字符数组
        /// </summary>
        private void ReadCharArray() { }
        /// <summary>
        /// 发送字节数据
        /// </summary>
        /// <param name="data"></param>
        public void SendData(byte[] data)
        {
            if (_serialPort != null && _serialPort.IsOpen)
            {
                _serialPort.Write(data, 0, data.Length);
            }
        }
        /// <summary>
        /// 发送字符串
        /// </summary>
        /// <param name="message"></param>
        public void SendData(string message)
        {
            if (_serialPort != null && _serialPort.IsOpen)
            {
                _serialPort.Write(message);
            }
        }
        /// <summary>
        /// 发送字符数组
        /// </summary>
        /// <param name="chs"></param>
        public void SendData(char[] chs)
        {
            if (_serialPort != null && _serialPort.IsOpen)
            {
                _serialPort.Write(chs, 0, chs.Length);
            }
        }

        public bool IsOpened() => SerialPortHandle >= 0;
        public bool IsErrorFinished() => IsErrFinished;

        /// <summary>
        /// 分发系统事件
        /// </summary>
        /// <param name="message"></param>
        private void SystemEventFire(string message)
        {
            if (SystemEventObject != null)
            {
                Loom.QueueOnMainThread(()=> 
                {
                    SystemEventObject.Invoke(this, message);
                });
            }
        }
        /// <summary>
        /// 分发接收事件
        /// </summary>
        /// <param name="message"></param>
        public void ReadEventFire(object message)
        {
            if (ReadCompleteEventObject != null)
            {
                Loom.QueueOnMainThread(()=> 
                {
                    ReadCompleteEventObject.Invoke(message);
                });
            }
        }
        /// <summary>
        /// 连接设备
        /// </summary>
        private void Connect()
        {
            if (IsAutoOpen)
            {
                Open();
            }
        }
        #endregion
    }
}
