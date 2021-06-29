using Microsoft.Win32;
using System;
using System.Collections.Generic;

namespace SerialPortUtility
{
    /// <summary>
    /// USB设备类型
    /// </summary>
    public enum PortService : byte
    {
        /// <summary>
        /// CH340
        /// </summary>
        CH341SER_A64 = 0,
        /// <summary>
        /// STM32
        /// </summary>
        usbser,
        /// <summary>
        /// 服务数量
        /// </summary>
        Num
    }
    /// <summary>
    /// 设备
    /// </summary>
    public struct PortDevice
    {
        public string portName;
        public PortService devType;
        public int skip;
    }
    /// <summary>
    /// 端口工具
    /// </summary>
    public class PortUtil
    {
        /// <summary>
        /// 获取所有可用设备个数
        /// </summary>
        /// <returns></returns>
        public static int GetDeviceCountAvailable()
        {
            int count = 0;
            foreach (var item in GetDeviceListAvailable())
            {
                count += item.Value.Count;
            }

            return count;
        }
        /// <summary>
        /// 获取所有可用设备
        /// </summary>
        /// <returns></returns>
        public static Dictionary<PortService, List<PortDevice>> GetDeviceListAvailable()
        {
            Dictionary<PortService, List<PortDevice>> allDevices = new Dictionary<PortService, List<PortDevice>>();
            for (int i = 0; i < (int)PortService.Num; i++)
            {
                PortService devType = (PortService)i;
                try
                {
                    List<PortDevice> devices = GetPortServiceListAvailable(devType);
                    allDevices.Add(devType, devices);
                }
                catch { }
            }

            return allDevices;
        }
        /// <summary>
        /// 获取服务可用设备个数
        /// </summary>
        /// <param name="devType"></param>
        /// <returns></returns>
        public static int GetPortServiceCountAvailable(PortService devType)
        {
            try
            {
                List<PortDevice> devices = GetPortServiceListAvailable(devType);
                return devices.Count;
            }
            catch
            {
                return 0;
            }
        }
        /// <summary>
        /// 获取服务可用设备
        /// </summary>
        /// <returns></returns>
        public static List<PortDevice> GetPortServiceListAvailable(PortService devType)
        {
            List<PortDevice> devices = new List<PortDevice>();

            try
            {
                RegistryKey usbService = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\" + devType.ToString() + @"\Enum", false);

                int num = (int)usbService.GetValue("Count");
                for (int i = 0; i < num; i++)
                {
                    string dev = (string)usbService.GetValue(i.ToString());

                    string[] pv = dev.Split(new char[2] { '&', '\\' }, StringSplitOptions.RemoveEmptyEntries);

                    string usbPath = string.Empty;
                    string subUsbPath = string.Empty;

                    if (pv.Length > 0)
                    {

                        usbPath = pv[1] + "&" + pv[2];

                        if (pv[3].StartsWith("MI"))
                        {
                            usbPath += "&" + pv[3];
                        }

                        subUsbPath = dev.Replace(pv[0] + "\\" + usbPath + "\\", "");
                    }

                    RegistryKey usbKey = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Enum\USB\" + usbPath, false);
                    RegistryKey subUsbKey = usbKey.OpenSubKey(subUsbPath, false);
                    RegistryKey devParam = subUsbKey.OpenSubKey("Device Parameters", false);

                    PortDevice device;
                    device.portName = (string)devParam.GetValue("PortName");
                    device.skip = i;
                    device.devType = devType;

                    devices.Add(device);
                }
            }
            catch
            {
                throw new UnportException(string.Format("未找到服务<color=yellow>[{0}]</color>可用串口!", devType.ToString()));
            }

            return devices;
        }
        /// <summary>
        /// 获得端口名
        /// </summary>
        /// <param name="index"></param>
        /// <param name="devType"></param>
        /// <returns></returns>
        public static bool GetPortName(int index, out string portName, PortService devType = PortService.CH341SER_A64)
        {
            portName = string.Empty;

            try
            {
                List<PortDevice> portLists = GetPortServiceListAvailable(devType);
                if (index <= portLists.Count)
                {
                    portName = portLists[index].portName;
                    return true;
                }
            }
            catch
            {
                throw new UnportException(string.Format("未找到服务<color=yellow>[{0}]</color>可用串口!", devType.ToString())); ;
            }

            return false;
        }
    }
}
