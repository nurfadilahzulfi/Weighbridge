using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Weighbridge.Connection;
using Weighbridge.Core.Encryptions;

namespace Weighbridge
{
    public partial class ConfigurationForm : Form
    {
        private bool _isFirstRun;
        private WBConnection wbConn;
        private SerialPort _serialPort;

        public ConfigurationForm() : this(false) { }

        public ConfigurationForm(bool isFirstRun)
        {
            InitializeComponent();
            _isFirstRun = isFirstRun;
        }

        private void ConfigurationForm_Load(object sender, EventArgs e)
        {
            LoadConfig();
            InitializeSerialPort();
        }

        private void btnSvConfig_Click(object sender, EventArgs e)
        {
            if (validateInput())
            {
                SaveConfig();
            }
        }

        private bool validateInput()
        {
            string msg = "";

            if (!validateInput_WBApp(ref msg))
            {
                MessageBox.Show(msg);
                return false;
            }
            else if (!validateInput_SBO(ref msg))
            {
                MessageBox.Show(msg);
                return false;
            }
            else
            {
                return true;
            }
        }

        private bool validateInput_WBApp(ref string msg)
        {
            if (isEmpty(cbPort.Text.Trim()))
                msg = "Weighbridge Port Cannot be Empty";
            else if (isEmpty(tbBaudRate.Text.Trim()))
                msg = "Weighbridge Baud Rate Cannot be Empty";
            else if (isEmpty(tbDataBits.Text.Trim()))
                msg = "Weighbridge Data Bits Cannot be Empty";
            else if (isEmpty(tbReadT.Text.Trim()))
                msg = "Weighbridge Read Timeout Cannot be Empty";
            else if (isEmpty(tbWriteT.Text.Trim()))
                msg = "Weighbridge Write Timeout Cannot be Empty";

            return isEmpty(msg);
        }

        private bool validateInput_SBO(ref string msg)
        {
            if (isEmpty(cbSrvType.Text.Trim()))
                msg = "SBO Database Server Type Cannot be Empty";
            else if (isEmpty(tbDBSrv.Text.Trim()))
                msg = "SBO Database Server Address Cannot be Empty";
            else if (isEmpty(tbDBSBO.Text.Trim()))
                msg = "SBO Company Database Cannot be Empty";
            else if (isEmpty(tbDBUsrnm.Text.Trim()))
                msg = "SBO Company Database Username Cannot be Empty";
            else if (isEmpty(tbDBPass.Text.Trim()))
                msg = "SBO Company Database Password Cannot be Empty";
            else if (isEmpty(tbSBOUsrnm.Text.Trim()))
                msg = "SBO Company User Name Cannot be Empty";
            else if (isEmpty(tbSBOPass.Text.Trim()))
                msg = "SBO Company User Password Cannot be Empty";

            return isEmpty(msg);
        }

        private bool isEmpty(string s)
        {
            return (string.IsNullOrWhiteSpace(s));
        }

        private void LoadConfig()
        {
            if (ConfigManager.wbConfig == null || ConfigManager.sboConfig == null)
            {
                MessageBox.Show("Configuration not loaded. Please restart the application.",
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                this.Close();
                return;
            }

            cbPort.Text = ConfigManager.wbConfig.portName;
            tbBaudRate.Text = ConfigManager.wbConfig.baudRate;
            tbDataBits.Text = ConfigManager.wbConfig.dataBits;
            tbReadT.Text = ConfigManager.wbConfig.readTimeout.ToString();
            tbWriteT.Text = ConfigManager.wbConfig.writeTimeout.ToString();
            tbPrefixWB.Text = ConfigManager.wbConfig.prefix.ToString();

            cbSrvType.Text = ConfigManager.sboConfig.serverType;
            tbDBSrv.Text = ConfigManager.sboConfig.serverName;
            tbDBSBO.Text = ConfigManager.sboConfig.database;
            tbDBUsrnm.Text = ConfigManager.sboConfig.databaseUsername;
            tbDBPass.Text = ConfigManager.sboConfig.databasePassword;
            tbSBOUsrnm.Text = ConfigManager.sboConfig.sboUsername;
            tbSBOPass.Text = ConfigManager.sboConfig.sboPassword;
        }

        private void SaveConfig()
        {
            ModGlobal modGlobal = new ModGlobal();

            Encryption encryption = new Encryption();

            ConfigManager.wbConfig.portName = cbPort.Text;
            ConfigManager.wbConfig.baudRate = tbBaudRate.Text;
            ConfigManager.wbConfig.dataBits = tbDataBits.Text;
            ConfigManager.wbConfig.readTimeout = int.TryParse(tbReadT.Text, out int readTimeout) ? readTimeout : 0;
            ConfigManager.wbConfig.writeTimeout = int.TryParse(tbReadT.Text, out int writeTimeout) ? writeTimeout : 0;
            ConfigManager.wbConfig.prefix = tbPrefixWB.Text;

            ConfigManager.sboConfig.serverType = cbSrvType.Text;
            ConfigManager.sboConfig.serverName = tbDBSrv.Text;
            ConfigManager.sboConfig.database = tbDBSBO.Text;
            ConfigManager.sboConfig.databaseUsername = tbDBUsrnm.Text;
            ConfigManager.sboConfig.databasePassword = encryption.encrypt(tbDBPass.Text);
            ConfigManager.sboConfig.sboUsername = tbSBOUsrnm.Text;
            ConfigManager.sboConfig.sboPassword = encryption.encrypt(tbSBOPass.Text);

            SBOConnection sboConnection = new SBOConnection();

            try
            {

                if (sboConnection.connectionSBOS(ConfigManager.sboConfig))
                {
                    //XMLHandler.SaveConfiguration(_wbConfig, _sboConfig);
                    ConfigManager.Save();
                    MessageBox.Show("Configuration saved successfully, shutting down the Application...", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);

                    Application.Exit();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error - SBO Connection", MessageBoxButtons.OK, MessageBoxIcon.Error);
                //ShowConfigTab();
            }
        }

        private void InitializeSerialPort()
        {
            _serialPort = new SerialPort()
            {
                PortName = ConfigManager.wbConfig.portName,        // Change to your COM port
                BaudRate = Convert.ToInt32(ConfigManager.wbConfig.baudRate),          // Common: 9600, 19200, 38400
                DataBits = Convert.ToInt32(ConfigManager.wbConfig.dataBits),
                Parity = Parity.Even,
                StopBits = StopBits.One,
                Handshake = Handshake.None,
                ReadTimeout = Convert.ToInt32(ConfigManager.wbConfig.readTimeout),       // Increase timeout to 3 seconds
                WriteTimeout = Convert.ToInt32(ConfigManager.wbConfig.writeTimeout),
                NewLine = "\r\n"
            };

            _serialPort.DataReceived += SerialPort_DataReceived;
        }

        private void SerialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            try
            {
                string rawData = _serialPort.ReadExisting(); // or ReadExisting()
                string weight = ParseWeight(rawData);

                // Update UI on main thread
                this.Invoke((MethodInvoker)delegate
                {
                    tbQtyTestTimbang.Text = weight;
                });
            }
            catch (Exception ex)
            {
                this.Invoke((MethodInvoker)delegate
                {
                    tbQtyTestTimbang.Text = "Error " + ex.Message;
                    MessageBox.Show($"Read error: {ex.Message}");
                });
            }
        }

        private string ParseWeight(string rawData)
        {

            rawData = rawData.Trim();

            // Method 1: Extract numeric value using regex
            var match = System.Text.RegularExpressions.Regex.Match(rawData, @"[\d]+\.?[\d]*");
            if (match.Success)
            {

                //return match.Value;
                // Parse as double to handle both "0000050" and "0000050.50"
                if (double.TryParse(match.Value, out double weight))
                {
                    return weight.ToString("0.##"); // "50" or "50.5" (no trailing zeros)
                    //return weight.ToString("F2");
                }
            }

            return "0";
            //return rawData;
        }

        private void btnTestWBConn_Click(object sender, EventArgs e)
        {
            wbConn = new WBConnection(ConfigManager.wbConfig);

            try
            {
                if (!_serialPort.IsOpen)
                {
                    _serialPort.Open();
                    if (_serialPort.IsOpen)
                    {
                        //ShowReadingDialog("Weigh Out");
                        lbStatusWBConnection.Text = "Connected";
                        lbStatusWBConnection.ForeColor = Color.Green;
                    }
                    else
                    {
                        lbStatusWBConnection.Text = "Not Connected";
                        lbStatusWBConnection.ForeColor = Color.Red;
                    }
                    _serialPort.DiscardInBuffer();
                }
                else
                {
                    _serialPort.Close();
                    lbStatusWBConnection.Text = "Connection has closed";
                    lbStatusWBConnection.ForeColor = Color.Blue;
                    _serialPort.DiscardInBuffer();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error - Weighbridge Connection", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
