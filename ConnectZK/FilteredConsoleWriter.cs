using System;
using System.IO;
using System.Text;

namespace ConnectZK
{
    /// <summary>
    /// TextWriter que filtra logs específicos del SDK zkemkeeper pero permite salida JSON
    /// </summary>
    public class FilteredConsoleWriter : TextWriter
    {
        private readonly TextWriter _originalWriter;
        private readonly StringBuilder _lineBuffer = new StringBuilder();
        private static readonly string[] _zkLogPatterns = new[]
        {
            "zkemkeeper",
            "ZKEM",
            "Connect_Net",
            "Disconnect",
            "ReadAllUserID",
            "ReadGeneralLogData",
            "SSR_GetAllUserInfo",
            "GetUserTmpExStr",
            "[ZK]",
            "COM:",
            "Interop"
        };

        public FilteredConsoleWriter(TextWriter originalWriter)
        {
            _originalWriter = originalWriter;
        }

        public override Encoding Encoding => _originalWriter.Encoding;

        public override void Write(char value)
        {
            if (value == '\n' || value == '\r')
            {
                FlushLine();
            }
            else
            {
                _lineBuffer.Append(value);
            }
        }

        public override void Write(string value)
        {
            if (string.IsNullOrEmpty(value))
                return;

            foreach (char c in value)
            {
                Write(c);
            }
        }

        public override void WriteLine(string value)
        {
            _lineBuffer.Append(value);
            FlushLine();
        }

        private void FlushLine()
        {
            if (_lineBuffer.Length == 0)
                return;

            string line = _lineBuffer.ToString();
            _lineBuffer.Clear();

            // Si la línea NO contiene patrones de logs de ZK, la escribimos
            bool isZkLog = false;
            foreach (var pattern in _zkLogPatterns)
            {
                if (line.IndexOf(pattern, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    isZkLog = true;
                    break;
                }
            }

            // Solo escribir si NO es un log de ZK o si es JSON (comienza con { o [)
            string trimmed = line.TrimStart();
            if (!isZkLog || trimmed.StartsWith("{") || trimmed.StartsWith("["))
            {
                _originalWriter.WriteLine(line);
            }
        }

        public override void Flush()
        {
            FlushLine();
            _originalWriter.Flush();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                Flush();
            }
            base.Dispose(disposing);
        }
    }
}
