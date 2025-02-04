using System;
using System.IO.Ports;
using System.Linq;
using System.Windows.Forms;
using System.Management;
using System.Threading.Tasks;
using System.IO;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Button;
using System.Text;
using System.Collections.Generic;
using Microsoft.VisualBasic;
using KopiLua;
using System.ComponentModel.Design;
using static KopiLua.Lua;
using System.Runtime.InteropServices;

namespace AutomatedTestBenchForEmbeddedSystems
{
    public partial class Form1 : Form
    {
        private bool _connected = false;
        private Dictionary<string, List<string>> testSequences = new Dictionary<string, List<string>>();
        private Lua.lua_State luaState;
        public Form1()
        {
            InitializeComponent();
            InitializeSerialPortControls();
            InitializeLua();
        }
        private void InitializeLua()
        {
            luaState = Lua.luaL_newstate();
            Lua.luaL_openlibs(luaState);
            Lua.lua_register(luaState, "sendCommand", SendCommand);
        }
        private int SendCommand(lua_State L)
        {
            Lua.CharPtr charPtr = Lua.lua_tostring(L, 1); 
            string command = charPtr.ToString(); 

            if (_connected)
            {
                serialPort.Write(command + "\r\n");
                LogMessage($"Lua Sent: {command}");
            }
            return 0;
        }


        private void buttonRunLuaScript_Click(object sender, EventArgs e)
        {
            string script = textBoxLuaScript.Text;
            if (!string.IsNullOrWhiteSpace(script))
            {
                if (Lua.luaL_loadstring(luaState, script) == 0)
                {
                    Lua.lua_pcall(luaState, 0, Lua.LUA_MULTRET, 0);
                    LogMessage("Executed Lua Script");
                }
                else
                {
                    LogMessage("Lua Script Error");
                }
            }
        }

        private void InitializeSerialPortControls()
        {
            comboBoxBaud.Items.AddRange(new object[] { 2400, 4800, 9600, 19200, 38400, 57600, 115200, 230400 });
            comboBoxBaud.SelectedIndex = 2;

            comboBoxDataBits.Items.AddRange(new object[] { 5, 6, 7, 8 });
            comboBoxDataBits.SelectedIndex = 3;

            comboBoxStopBits.Items.AddRange(Enum.GetNames(typeof(StopBits)));
            comboBoxStopBits.SelectedItem = "One";

            comboBoxParity.Items.AddRange(Enum.GetNames(typeof(Parity)));
            comboBoxParity.SelectedItem = "None";

            comboBoxHandshake.Items.AddRange(Enum.GetNames(typeof(Handshake)));
            comboBoxHandshake.SelectedItem = "None";

            buttonSerial.BackColor = System.Drawing.Color.Red;
        }

        private void comboBoxPort_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (comboBoxPort.SelectedItem != null)
            {
                serialPort.PortName = comboBoxPort.SelectedItem.ToString().Split(' ')[0];
            }
        }
        private void comboBoxPort_DropDown(object sender, EventArgs e)
        {
            comboBoxPort.Items.Clear();
            var ports = SerialPort.GetPortNames()
                .Select(p => $"{p} ({GetPortDescription(p)})").ToArray();
            comboBoxPort.Items.AddRange(ports);
        }
        private string GetPortDescription(string port)
        {
            try
            {
                using (var searcher = new ManagementObjectSearcher($"SELECT * FROM Win32_PnPEntity WHERE Name LIKE '%{port}%'"))
                {
                    var obj = searcher.Get().Cast<ManagementObject>().FirstOrDefault();
                    return obj?["Caption"]?.ToString() ?? "Unknown";
                }
            }
            catch { return "Unknown"; }
        }
        private void comboBoxBaud_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (comboBoxBaud.SelectedItem != null)
            {
                serialPort.BaudRate = (int)comboBoxBaud.SelectedItem;
            }
        }

        private void buttonSerial_Click(object sender, EventArgs e)
        {
            if (_connected)
            {
                DisconnectSerial();
            }
            else
            {
                ConnectSerial();
            }
        }
        private void ConnectSerial()
        {
            try
            {
                serialPort.PortName = comboBoxPort.SelectedItem?.ToString().Split(' ')[0] ?? "";
                serialPort.BaudRate = (int)comboBoxBaud.SelectedItem;
                serialPort.DataBits = (int)comboBoxDataBits.SelectedItem;
                serialPort.StopBits = (StopBits)Enum.Parse(typeof(StopBits), comboBoxStopBits.SelectedItem.ToString());
                serialPort.Parity = (Parity)Enum.Parse(typeof(Parity), comboBoxParity.SelectedItem.ToString());
                serialPort.Handshake = (Handshake)Enum.Parse(typeof(Handshake), comboBoxHandshake.SelectedItem.ToString());
                serialPort.ReadTimeout = 500;
                serialPort.WriteTimeout = 500;
                serialPort.Encoding = Encoding.ASCII;

                serialPort.Open();
                buttonSerial.Text = "Disconnect";
                _connected = true;
                buttonSerial.BackColor = System.Drawing.Color.Green;
                LogMessage("Connected to " + serialPort.PortName);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message);
            }
        }

        private void DisconnectSerial()
        {
            try
            {
                serialPort.Close();
                buttonSerial.Text = "Connect";
                _connected = false;
                buttonSerial.BackColor = System.Drawing.Color.Red;
                LogMessage("Disconnected");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message);
            }
        }
        private void LogMessage(string message)
        {
            string logEntry = $"{DateTime.Now:HH:mm:ss} - {message}";
            textBoxLog.AppendText(logEntry + Environment.NewLine);
            File.AppendAllText("serial_log.txt", logEntry + Environment.NewLine);
        }
        private void serialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            try
            {
                string data = serialPort.ReadLine();
                Invoke(new Action(() => { LogMessage("Received: " + data); }));
            }
            catch (TimeoutException) { }
        }

        private void buttonSendMessage_Click(object sender, EventArgs e)
        {
            if (_connected && !string.IsNullOrWhiteSpace(textBoxSendMessage.Text))
            {
                string command = textBoxSendMessage.Text + "\r\n";
                serialPort.Write(command);
                LogMessage("Sent: " + command.Trim());
            }
        }

        private void buttonAddTest_Click(object sender, EventArgs e)
        {
            string testName = Microsoft.VisualBasic.Interaction.InputBox("Enter test name:", "New Test Sequence", "Test1");
            if (!string.IsNullOrWhiteSpace(testName) && !testSequences.ContainsKey(testName))
            {
                testSequences[testName] = new List<string>();
                listBoxTestSequences.Items.Add(testName);
            }
        }

        private void buttonRunTestSequence_Click(object sender, EventArgs e)
        {
            if (!_connected)
            {
                MessageBox.Show("Connect to a serial port first!");
                return;
            }

            if (listBoxTestSequences.SelectedItem == null)
            {
                MessageBox.Show("Select a test sequence.");
                return;
            }

            string selectedTest = listBoxTestSequences.SelectedItem.ToString();
            if (!testSequences.ContainsKey(selectedTest) || testSequences[selectedTest].Count == 0)
            {
                MessageBox.Show("No commands in this test sequence.");
                return;
            }

            Task.Run(() => RunTestSequence(testSequences[selectedTest]));
        }

        private async Task RunTestSequence(List<string> commands)
        {
            foreach (var command in commands)
            {
                if (!_connected) break;

                try
                {
                    serialPort.Write(command + "\r\n");

                    Invoke(new Action(() => LogMessage($"Sent: {command}")));
                }
                catch (Exception ex)
                {
                    Invoke(new Action(() => MessageBox.Show($"Error: {ex.Message}")));
                    break;
                }

                await Task.Delay(1000); 
            }
        }


        private void buttonAddCommand_Click(object sender, EventArgs e)
        {
            if (listBoxTestSequences.SelectedItem == null)
            {
                MessageBox.Show("Select a test sequence first.");
                return;
            }

            string selectedTest = listBoxTestSequences.SelectedItem.ToString();
            string command = textBoxSendMessage.Text.Trim();

            if (!string.IsNullOrWhiteSpace(command))
            {
                testSequences[selectedTest].Add(command);
                listBoxCommands.Items.Add(command);  
                textBoxSendMessage.Clear();
            }
        }
    }
}
