using System;
using System.IO.Ports;
using Weighbridge.Core.Helpers;

namespace Weighbridge.Core.Services
{
    public class SerialPortService : IDisposable
    {
        private SerialPort _serialPort;
        private bool _disposed;
        public event Action<string> OnWeightReceived;
        public event Action<string> OnError;
        public void Initialize()
        {
            var config = ConfigManager.wbConfig;
            _serialPort = new SerialPort()
            {
                PortName = config.portName,
                BaudRate = Convert.ToInt32(config.baudRate),
                DataBits = Convert.ToInt32(config.dataBits),
                Parity = Parity.Even,
                StopBits = StopBits.One,
                Handshake = Handshake.None,
                ReadTimeout = Convert.ToInt32(config.readTimeout),
                WriteTimeout = Convert.ToInt32(config.writeTimeout),
                NewLine = "\r\n"
            };

            _serialPort.DataReceived += SerialPort_DataReceived;
        }

        public void StartReading()
        {
            if (_serialPort == null)
                throw new InvalidOperationException("SerialPort belum diinisialisasi. Panggil Initialize() terlebih dahulu.");

            if (!_serialPort.IsOpen)
                _serialPort.Open();

            _serialPort.DiscardInBuffer();
        }

        public SerialPort Port => _serialPort;

        private void SerialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            try
            {
                string rawData = _serialPort.ReadExisting();
                string weight = DataConversionHelper.ParseWeight(rawData);
                OnWeightReceived?.Invoke(weight);
            }
            catch (Exception ex)
            {
                OnError?.Invoke(ex.Message);
            }
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                if (_serialPort != null && _serialPort.IsOpen)
                {
                    try { _serialPort.Close(); } catch { }
                }
                _serialPort?.Dispose();
                _disposed = true;
            }
        }
    }
}
