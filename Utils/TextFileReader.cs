using System;
using System.IO;
using System.Text;

namespace ETBTool.Utils
{
    /// <summary>
    /// 读取文本文件时自动识别编码：UTF-8（含 BOM）、UTF-16、GB18030/GBK。
    /// 游戏或第三方工具生成的 txt/xml 日志经常是 GBK，直接按 UTF-8 读会把中文
    /// 变成问号或乱码。
    /// </summary>
    public static class TextFileReader
    {
        private static bool _codePagesRegistered;

        public static string ReadAllText(string path)
        {
            var bytes = File.ReadAllBytes(path);
            if (bytes.Length == 0) return string.Empty;

            if (bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF)
                return new UTF8Encoding(false).GetString(bytes, 3, bytes.Length - 3);
            if (bytes.Length >= 2 && bytes[0] == 0xFF && bytes[1] == 0xFE)
                return Encoding.Unicode.GetString(bytes, 2, bytes.Length - 2);
            if (bytes.Length >= 2 && bytes[0] == 0xFE && bytes[1] == 0xFF)
                return Encoding.BigEndianUnicode.GetString(bytes, 2, bytes.Length - 2);

            try
            {
                // 严格 UTF-8：遇到非法字节序列会抛异常，从而进入 GBK 分支。
                return new UTF8Encoding(false, true).GetString(bytes);
            }
            catch (DecoderFallbackException)
            {
                try
                {
                    if (!_codePagesRegistered)
                    {
                        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
                        _codePagesRegistered = true;
                    }
                    return Encoding.GetEncoding(936).GetString(bytes);
                }
                catch
                {
                    return Encoding.UTF8.GetString(bytes);
                }
            }
        }
    }
}
