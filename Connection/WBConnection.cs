using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Weighbridge.Core.Config;

namespace Weighbridge.Connection
{
    public class WBConnection
    {
        private SerialPort _serialPort;
        public event Action<double> OnWeightReceived;
        public event Action<string> OnRawDataReceived;

        public bool IsConnected => _serialPort?.IsOpen ?? false;

        public WBConnection(WBConfig config)
        {
            _serialPort = new SerialPort();
            _serialPort.PortName = config.portName;
            _serialPort.BaudRate = Convert.ToInt32(config.baudRate);
            _serialPort.Parity = Parity.Even;
            _serialPort.DataBits = Convert.ToInt32(config.dataBits);
            _serialPort.StopBits = StopBits.One;
            //_serialPort.Handshake = Handshake.None;

            //_serialPort.DataReceived += _serialPort_DataReceived;
        }

        public void Connect()
        {
            GetAvailablePorts();
            if (!_serialPort.IsOpen)
            {
                _serialPort.DataReceived += (s, e) =>
                {
                    try
                    {
                        string raw = _serialPort.ReadExisting();

                        if (string.IsNullOrWhiteSpace(raw))
                        {
                            OnRawDataReceived?.Invoke("No data received");  // fire event with message
                            return;
                        }

                        OnRawDataReceived?.Invoke(raw);

                        double? weight = ParseWeight(raw);
                        if (weight.HasValue)
                            OnWeightReceived?.Invoke(weight.Value);
                    }
                    catch {  }
                };

                _serialPort.Open();
            }
            else
            {
                throw new Exception();
            }
        }

        public void Disconnect()
        {
            if (_serialPort.IsOpen)
            {
                _serialPort.Close();
                _serialPort.Dispose();
            }
            else
            {
                throw new Exception();
            }
        }

        //private void _serialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        //{
        //    string raw = _serialPort.ReadExisting();
        //    double weight = ParseWeight(raw);

        //    // Send weight to the main form
        //    WeightReceived?.Invoke(weight);
        //}

        //private double ParseWeight(string raw)
        //{
        //    try
        //    {
        //        // Adjust this format to match your indicator output
        //        string cleaned = raw.Replace("kg", "").Trim();

        //        if (double.TryParse(cleaned, out double result))
        //            return result;

        //        return 0;
        //    }
        //    catch
        //    {
        //        return 0;
        //    }
        //}

        private double? ParseWeight(string raw)
        {
            // Extracts the first number found in the raw serial string.
            // Handles formats like: "  0045.20 KG", "ST,GS,+00045.20kg", "W+ 00123.5 kg"
            var match = Regex.Match(raw, @"[+-]?\d+\.?\d*");
            if (match.Success && double.TryParse(match.Value, out double value))
                return value;

            return null;
        }

        public static string[] GetAvailablePorts() => SerialPort.GetPortNames();
    }
}
