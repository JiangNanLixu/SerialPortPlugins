namespace SerialPortUtility
{
    /// <summary>
    /// 构造器基类
    /// </summary>
    public interface IParser
    {
        /// <summary>
        /// 单个字节数据解析
        /// </summary>
        /// <param name="data">单个字节</param>
        /// <returns>解析结果</returns>
        int parseByte(byte data);
    }
}
