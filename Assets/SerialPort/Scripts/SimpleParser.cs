using UnityEngine;

namespace SerialPortUtility
{
    /// <summary>
    /// 示例解释器
    /// </summary>
    public class SimpleParser : IParser
    {
        SerialPortUtilityPro pro;

        private const int PARSER_STATE_SYNC = 1;
        private const int PARSER_STATE_COMMAND = 2;
        private const int PARSER_STATE_END = 3;

        private readonly int PARSER_SYNC_BYTE = 251;    // 0xFB
        private readonly int PARSER_SYNC_END = 191;     // 0xBF

        private byte _command;
        private int _parserStatus;

        public SimpleParser()
        {
            this._parserStatus = PARSER_STATE_SYNC;
        }

        public SimpleParser(SerialPortUtilityPro pro)
        {
            this._parserStatus = PARSER_STATE_SYNC;
            this.pro = pro;
        }

        public int parseByte(byte buffer)
        {
            int returnValue = 0;
            switch (this._parserStatus)
            {
                case 1:
                    if ((buffer & 0xFF) != PARSER_SYNC_BYTE)
                        break;
                    this._parserStatus = PARSER_STATE_COMMAND;
                    break;
                case 2:
                    _command = buffer;
                    this._parserStatus = PARSER_STATE_END;
                    break;
                case 3:
                    if ((buffer & 0xFF) != PARSER_SYNC_END)
                    {
                        returnValue = 2;
                    }
                    else
                    {
                        returnValue = 1;
                        parsePacketPayload();
                    }
                    this._parserStatus = PARSER_STATE_SYNC;
                    break;
            }

            return returnValue;
        }

        void parsePacketPayload()
        {
            Debug.Log("Success");
            //pro.ReadEventFire(new byte[] { 0xFB, _command, 0xBF });
        }
    }
}
