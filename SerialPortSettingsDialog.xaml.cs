using System;
using System.IO.Ports;
using System.Windows;

namespace Comm
{
    public partial class SerialPortSettingsDialog : Window
    {
        public string BaudRate { get; private set; }
        public string DataBits { get; private set; }
        public string StopBits { get; private set; }
        public string Parity { get; private set; }

        public SerialPortSettingsDialog(string currentBaud, string currentDataBits, string currentStopBits, string currentParity)
        {
            InitializeComponent();

            // 初始化波特率
            int[] baudRates = { 300, 600, 1200, 2400, 4800, 9600, 14400, 19200,
                                28800, 38400, 57600, 115200, 128000, 230400, 256000, 460800, 921600 };
            foreach (int baud in baudRates)
                this.comboBaud.Items.Add(baud.ToString());

            // 设置当前选中的波特率
            if (!string.IsNullOrEmpty(currentBaud) && this.comboBaud.Items.Contains(currentBaud))
                this.comboBaud.SelectedItem = currentBaud;
            else
                this.comboBaud.SelectedIndex = 6;  // 默认 9600

            // 初始化数据位
            string[] dataBits = { "5", "6", "7", "8" };
            foreach (string db in dataBits)
                this.comboDataBits.Items.Add(db);

            // 设置当前选中的数据位
            if (!string.IsNullOrEmpty(currentDataBits) && this.comboDataBits.Items.Contains(currentDataBits))
                this.comboDataBits.SelectedItem = currentDataBits;
            else
                this.comboDataBits.SelectedIndex = 3;  // 默认 8

            // 初始化停止位
            string[] stopBits = { "1", "1.5", "2" };
            foreach (string sb in stopBits)
                this.comboStopBits.Items.Add(sb);

            // 设置当前选中的停止位
            if (!string.IsNullOrEmpty(currentStopBits) && this.comboStopBits.Items.Contains(currentStopBits))
                this.comboStopBits.SelectedItem = currentStopBits;
            else
                this.comboStopBits.SelectedIndex = 0;  // 默认 1

            // 初始化校验位
            string[] parity = { "无", "奇校验", "偶校验", "标记", "空格" };
            foreach (string p in parity)
                this.comboParity.Items.Add(p);

            // 设置当前选中的校验位
            if (!string.IsNullOrEmpty(currentParity) && this.comboParity.Items.Contains(currentParity))
                this.comboParity.SelectedItem = currentParity;
            else
                this.comboParity.SelectedIndex = 0;  // 默认 无

            // 绑定选择变更事件，实时预览
            this.comboBaud.SelectionChanged += UpdatePreview;
            this.comboDataBits.SelectionChanged += UpdatePreview;
            this.comboStopBits.SelectionChanged += UpdatePreview;
            this.comboParity.SelectionChanged += UpdatePreview;

            UpdatePreview(null, null);
        }

        private void UpdatePreview(object sender, EventArgs e)
        {
            string baud = this.comboBaud.SelectedItem?.ToString() ?? "9600";
            string dataBits = this.comboDataBits.SelectedItem?.ToString() ?? "8";
            string stopBits = this.comboStopBits.SelectedItem?.ToString() ?? "1";
            string parity = this.comboParity.SelectedItem?.ToString() ?? "无";
            this.labPreview.Text = $"{baud}bps, {dataBits}数据位, {stopBits}停止位, {parity}";
        }

        private void btnOK_Click(object sender, RoutedEventArgs e)
        {
            BaudRate = this.comboBaud.SelectedItem?.ToString() ?? "9600";
            DataBits = this.comboDataBits.SelectedItem?.ToString() ?? "8";
            StopBits = this.comboStopBits.SelectedItem?.ToString() ?? "1";
            Parity = this.comboParity.SelectedItem?.ToString() ?? "无";
            this.DialogResult = true;
            this.Close();
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
    }
}
