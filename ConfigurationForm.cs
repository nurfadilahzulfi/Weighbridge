using System;
using System.Drawing;
using System.IO.Ports;
using System.Windows.Forms;
using Weighbridge.Connection;
using Weighbridge.Core.Encryptions;
using Weighbridge.Core.Helpers;
using Weighbridge.Core.Services;

namespace Weighbridge
{
    public partial class ConfigurationForm : Form
    {
        private readonly IConfigurationValidator _validator;
        private readonly bool _isFirstRun;
        private SerialPort _serialPort;

        public ConfigurationForm() : this(false, new ConfigurationValidator()) { }

        public ConfigurationForm(bool isFirstRun) : this(isFirstRun, new ConfigurationValidator()) { }

        public ConfigurationForm(bool isFirstRun, IConfigurationValidator validator)
        {
            InitializeComponent();
            _isFirstRun = isFirstRun;
            _validator = validator ?? new ConfigurationValidator();
        }

        private void ConfigurationForm_Load(object sender, EventArgs e)
        {
            LoadConfig();
            InitializeSerialPort();
            ResponsiveLayoutHelper.Attach(this);
            WatermarkHelper.Apply(this);
        }

        private void btnSvConfig_Click(object sender, EventArgs e)
        {
            if (ValidateInput())
            {
                SaveConfig();
            }
        }

        private bool ValidateInput()
        {
            var wbResult = _validator.ValidateWbConfig(cbPort.Text, tbBaudRate.Text, tbDataBits.Text, tbReadT.Text, tbWriteT.Text);
            if (!wbResult.IsValid)
            {
                MessageBox.Show(wbResult.ErrorMessage);
                return false;
            }

            var sboResult = _validator.ValidateSboConfig(cbSrvType.Text, tbDBSrv.Text, tbDBSBO.Text, tbDBUsrnm.Text, tbDBPass.Text, tbSBOUsrnm.Text, tbSBOPass.Text);
            if (!sboResult.IsValid)
            {
                MessageBox.Show(sboResult.ErrorMessage);
                return false;
            }

            return true;
        }

        private void LoadConfig()
        {
            if (ConfigManager.wbConfig == null || ConfigManager.sboConfig == null)
            {
                MessageBox.Show("Configuration not loaded. Please restart the application.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
            Encryption encryption = new Encryption();

            ConfigManager.wbConfig.portName = cbPort.Text.Trim();
            ConfigManager.wbConfig.baudRate = tbBaudRate.Text.Trim();
            ConfigManager.wbConfig.dataBits = tbDataBits.Text.Trim();
            ConfigManager.wbConfig.readTimeout = int.TryParse(tbReadT.Text, out int readTimeout) ? readTimeout : 0;
            ConfigManager.wbConfig.writeTimeout = int.TryParse(tbWriteT.Text, out int writeTimeout) ? writeTimeout : 0;
            ConfigManager.wbConfig.prefix = tbPrefixWB.Text.Trim();

            ConfigManager.sboConfig.serverType = cbSrvType.Text.Trim();
            ConfigManager.sboConfig.serverName = tbDBSrv.Text.Trim();
            ConfigManager.sboConfig.database = tbDBSBO.Text.Trim();
            ConfigManager.sboConfig.databaseUsername = tbDBUsrnm.Text.Trim();
            ConfigManager.sboConfig.databasePassword = encryption.encrypt(tbDBPass.Text);
            ConfigManager.sboConfig.sboUsername = tbSBOUsrnm.Text.Trim();
            ConfigManager.sboConfig.sboPassword = encryption.encrypt(tbSBOPass.Text);

            SBOConnection sboConnection = new SBOConnection();

            try
            {
                if (sboConnection.connectionSBOS(ConfigManager.sboConfig))
                {
                    ConfigManager.Save();
                    MessageBox.Show("Configuration saved successfully, shutting down the Application...", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    Application.Exit();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error - SBO Connection", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void InitializeSerialPort()
        {
            try
            {
                _serialPort = new SerialPort
                {
                    PortName = string.IsNullOrWhiteSpace(ConfigManager.wbConfig.portName) ? "COM1" : ConfigManager.wbConfig.portName,
                    BaudRate = int.TryParse(ConfigManager.wbConfig.baudRate, out int baud) ? baud : 9600,
                    DataBits = int.TryParse(ConfigManager.wbConfig.dataBits, out int bits) ? bits : 8,
                    Parity = Parity.Even,
                    StopBits = StopBits.One,
                    Handshake = Handshake.None,
                    ReadTimeout = ConfigManager.wbConfig.readTimeout > 0 ? ConfigManager.wbConfig.readTimeout : 3000,
                    WriteTimeout = ConfigManager.wbConfig.writeTimeout > 0 ? ConfigManager.wbConfig.writeTimeout : 3000,
                    NewLine = "\r\n"
                };

                _serialPort.DataReceived += SerialPort_DataReceived;
            }
            catch
            {
            }
        }

        private void SerialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            try
            {
                string rawData = _serialPort.ReadExisting();
                string weight = DataConversionHelper.ParseWeight(rawData);

                if (IsDisposed || !IsHandleCreated) return;
                BeginInvoke((MethodInvoker)delegate
                {
                    tbQtyTestTimbang.Text = weight;
                });
            }
            catch (Exception ex)
            {
                if (IsDisposed || !IsHandleCreated) return;
                BeginInvoke((MethodInvoker)delegate
                {
                    tbQtyTestTimbang.Text = "Error: " + ex.Message;
                });
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (_serialPort != null)
            {
                try { if (_serialPort.IsOpen) _serialPort.Close(); } catch { }
                _serialPort.Dispose();
                _serialPort = null;
            }
            base.OnFormClosing(e);
        }

        private void btnTestWBConn_Click(object sender, EventArgs e)
        {
            try
            {
                if (_serialPort == null)
                {
                    InitializeSerialPort();
                }

                if (!_serialPort.IsOpen)
                {
                    _serialPort.Open();
                    lbStatusWBConnection.Text = "Connected";
                    lbStatusWBConnection.ForeColor = Color.Green;
                    _serialPort.DiscardInBuffer();
                }
                else
                {
                    _serialPort.Close();
                    lbStatusWBConnection.Text = "Connection has closed";
                    lbStatusWBConnection.ForeColor = Color.Blue;
                }
            }
            catch (Exception ex)
            {
                lbStatusWBConnection.Text = "Not Connected";
                lbStatusWBConnection.ForeColor = Color.Red;
                MessageBox.Show(ex.Message, "Error - Weighbridge Connection", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
