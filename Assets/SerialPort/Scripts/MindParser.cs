using UnityEngine;

namespace SerialPortUtility
{
    public class MindData
    {
        public int sig;
        public int att;
        public int med;
    }

    /// <summary>
    /// 脑波数据解释器
    /// </summary>
    public class MindParser : IParser
    {
        SerialPortUtilityPro pro;
        MindData mind;
        public MindParser() { }

        public MindParser(SerialPortUtilityPro pro)
        {
            this.pro = pro;
            mind = new MindData();
        }

        public int parseByte(byte data)
        {
            parsePacketPayload();
            return 1;
        }

        void parsePacketPayload()
        {
            
            mind.sig = 0;
            mind.att = 50;
            mind.med = 50;
            pro.ReadEventFire(mind);
        }
    }
}
