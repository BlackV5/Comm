using System;
using System.IO.Ports;
using System.Windows;
using System.Windows.Input;
using System.Windows.Controls;

namespace Comm
{
    public partial class MainWindow : Window
    {
        public SerialPort serialPort1 = new SerialPort();
        private string buffer = "";
        private int receiveCount = 0;
        private int sendCount = 0;

        // 隐藏的串口参数（从 Settings 加载）
        private string currentDataBits = "8";
        private string currentStopBits = "1";
        private string currentParity = "无";

        public MainWindow()
        {
            InitializeComponent();

            LoadWindowSettings();
            InitializeSerialPort();
            this.QuickSendControl.SetSerialPort(this.serialPort1, this);
        }

        // ========== 加载窗口设置 ==========
        private void LoadWindowSettings()
        {
            try
            {
                double width = Properties.Settings.Default.WindowWidth;
                double height = Properties.Settings.Default.WindowHeight;

                if (width > 100 && height > 100)
                {
                    this.Width = width;
                    this.Height = height;
                }

                double left = Properties.Settings.Default.WindowLeft;
                double top = Properties.Settings.Default.WindowTop;

                var screenWidth = SystemParameters.WorkArea.Width;
                var screenHeight = SystemParameters.WorkArea.Height;

                if (left >= 0 && left < screenWidth - 100)
                    this.Left = left;
                if (top >= 0 && top < screenHeight - 100)
                    this.Top = top;

                if (Properties.Settings.Default.WindowMaximized)
                {
                    this.WindowState = WindowState.Maximized;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("加载窗口设置失败：" + ex.Message);
            }
        }

        // ========== 保存窗口设置 ==========
        private void SaveWindowSettings()
        {
            try
            {
                if (this.WindowState == WindowState.Maximized)
                {
                    Properties.Settings.Default.WindowMaximized = true;
                    Properties.Settings.Default.WindowWidth = SystemParameters.WorkArea.Width;
                    Properties.Settings.Default.WindowHeight = SystemParameters.WorkArea.Height;
                    Properties.Settings.Default.WindowLeft = 0;
                    Properties.Settings.Default.WindowTop = 0;
                }
                else
                {
                    Properties.Settings.Default.WindowMaximized = false;
                    Properties.Settings.Default.WindowWidth = this.Width;
                    Properties.Settings.Default.WindowHeight = this.Height;
                    Properties.Settings.Default.WindowLeft = this.Left;
                    Properties.Settings.Default.WindowTop = this.Top;
                }

                Properties.Settings.Default.Save();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("保存窗口设置失败：" + ex.Message);
            }
        }

        // ========== 刷新串口列表 ==========
        private void btnRefreshPorts_Click(object sender, RoutedEventArgs e)
        {
            RefreshPortList();
        }

        // ========== 刷新串口列表（公共方法） ==========
        public void RefreshPortList()
        {
            try
            {
                string currentPort = this.comboPort.SelectedItem?.ToString();
                string[] ports = SerialPort.GetPortNames();

                this.comboPort.Items.Clear();
                foreach (string port in ports)
                {
                    this.comboPort.Items.Add(port);
                }

                if (!string.IsNullOrEmpty(currentPort) && this.comboPort.Items.Contains(currentPort))
                {
                    this.comboPort.SelectedItem = currentPort;
                }
                else if (this.comboPort.Items.Count > 0)
                {
                    this.comboPort.SelectedIndex = 0;
                }

                int count = this.comboPort.Items.Count;
                UpdateStatus($"已刷新，找到 {count} 个串口");
                AppendReceive($"[系统] 刷新串口列表，找到 {count} 个可用串口");
            }
            catch (Exception ex)
            {
                MessageBox.Show("刷新串口列表失败：" + ex.Message, "错误",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ========== 串口初始化 ==========
        private void InitializeSerialPort()
        {
            RefreshPortList();

            // 波特率选项
            this.comboBaud.Items.Clear();
            int[] baudRates = { 300, 600, 1200, 2400, 4800, 9600, 14400, 19200,
                        28800, 38400, 57600, 115200, 128000, 230400, 256000, 460800, 921600 };
            foreach (int baud in baudRates)
                this.comboBaud.Items.Add(baud.ToString());

            // 恢复保存的波特率
            string lastBaud = Properties.Settings.Default.LastBaudRate;
            int baudIndex = this.comboBaud.Items.IndexOf(lastBaud);
            this.comboBaud.SelectedIndex = baudIndex >= 0 ? baudIndex : 6;

            // 从 Settings 加载并清理引号
            currentDataBits = CleanString(Properties.Settings.Default.LastDataBits);
            currentStopBits = CleanString(Properties.Settings.Default.LastStopBits);
            currentParity = CleanString(Properties.Settings.Default.LastParity);

            // 如果为空则使用默认值
            if (string.IsNullOrEmpty(currentDataBits)) currentDataBits = "8";
            if (string.IsNullOrEmpty(currentStopBits)) currentStopBits = "1";
            if (string.IsNullOrEmpty(currentParity)) currentParity = "无";

            this.chkHexSend.IsChecked = Properties.Settings.Default.LastHexSend;

            this.serialPort1.DataReceived += serialPort1_DataReceived;

            this.btnSend.IsEnabled = false;
            this.textBoxSend.IsEnabled = false;
            UpdateStatus($"就绪 | {this.comboBaud.Text}bps, {currentDataBits}数据位, {currentStopBits}停止位, {currentParity}");
        }

        // ========== 更多串口设置 ==========
        private void btnMoreSettings_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new SerialPortSettingsDialog(
                this.comboBaud.Text,        // 当前波特率
                currentDataBits,            // 当前数据位
                currentStopBits,            // 当前停止位
                currentParity               // 当前校验位
            );

            if (dialog.ShowDialog() == true)
            {
                // 更新波特率（如果用户在弹窗中修改了）
                if (this.comboBaud.Items.Contains(dialog.BaudRate))
                {
                    this.comboBaud.SelectedItem = dialog.BaudRate;
                }

                // 保存其他参数
                currentDataBits = dialog.DataBits;
                currentStopBits = dialog.StopBits;
                currentParity = dialog.Parity;

                // 如果串口已打开，提示需要重新打开
                if (this.serialPort1.IsOpen)
                {
                    MessageBox.Show("参数已保存，重新打开串口后生效！", "提示",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }

                UpdateStatus($"参数已更新：{this.comboBaud.Text}bps, {currentDataBits}数据位, {currentStopBits}停止位, {currentParity}");
                SaveSerialSettings();
            }
        }

        // ========== 保存串口参数 ==========
        private void SaveSerialSettings()
        {
            try
            {
                if (this.comboPort.SelectedItem != null)
                    Properties.Settings.Default.LastPort = this.comboPort.SelectedItem.ToString();

                if (this.comboBaud.SelectedItem != null)
                    Properties.Settings.Default.LastBaudRate = this.comboBaud.SelectedItem.ToString();

                Properties.Settings.Default.LastDataBits = currentDataBits;
                Properties.Settings.Default.LastStopBits = currentStopBits;
                Properties.Settings.Default.LastParity = currentParity;

                Properties.Settings.Default.LastHexSend = this.chkHexSend.IsChecked ?? false;
                Properties.Settings.Default.Save();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("保存串口设置失败：" + ex.Message);
            }
        }

        // ========== 获取停止位枚举值 ==========
        private StopBits GetStopBits(string text)
        {
            switch (text)
            {
                case "1": return StopBits.One;
                case "1.5": return StopBits.OnePointFive;
                case "2": return StopBits.Two;
                default: return StopBits.One;
            }
        }

        // ========== 获取校验位枚举值 ==========
        private Parity GetParity(string text)
        {
            switch (text)
            {
                case "无": return Parity.None;
                case "奇校验": return Parity.Odd;
                case "偶校验": return Parity.Even;
                case "标记": return Parity.Mark;
                case "空格": return Parity.Space;
                default: return Parity.None;
            }
        }

        // ========== 打开/关闭串口 ==========
        private void btnOpen_Click(object sender, RoutedEventArgs e)
        {
            if (this.serialPort1.IsOpen)
            {
                try
                {
                    this.serialPort1.Close();
                    this.btnOpen.Content = "打开串口";
                    this.comboPort.IsEnabled = true;
                    this.comboBaud.IsEnabled = true;
                    this.btnMoreSettings.IsEnabled = true;
                    this.btnRefreshPorts.IsEnabled = true;
                    this.btnSend.IsEnabled = false;
                    this.textBoxSend.IsEnabled = false;
                    AppendReceive("串口已关闭");
                    UpdateStatus($"已关闭 | {this.comboBaud.Text}bps, {currentDataBits}数据位, {currentStopBits}停止位, {currentParity}");
                    SaveSerialSettings();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"关闭串口失败：{ex.Message}", "错误",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                try
                {
                    if (this.comboPort.SelectedItem == null)
                    {
                        MessageBox.Show("请选择端口！", "提示",
                            MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    // 清理参数中的引号
                    string cleanDataBits = CleanString(currentDataBits);
                    string cleanStopBits = CleanString(currentStopBits);
                    string cleanParity = CleanString(currentParity);

                    // 如果清理后为空，设置默认值
                    if (string.IsNullOrEmpty(cleanDataBits)) cleanDataBits = "8";
                    if (string.IsNullOrEmpty(cleanStopBits)) cleanStopBits = "1";
                    if (string.IsNullOrEmpty(cleanParity)) cleanParity = "无";

                    // 设置串口参数
                    this.serialPort1.PortName = this.comboPort.Text;
                    this.serialPort1.BaudRate = int.Parse(this.comboBaud.Text);
                    this.serialPort1.DataBits = int.Parse(cleanDataBits);
                    this.serialPort1.StopBits = GetStopBits(cleanStopBits);
                    this.serialPort1.Parity = GetParity(cleanParity);

                    this.serialPort1.Open();

                    this.btnOpen.Content = "关闭串口";
                    this.comboPort.IsEnabled = false;
                    this.comboBaud.IsEnabled = false;
                    this.btnMoreSettings.IsEnabled = false;
                    this.btnRefreshPorts.IsEnabled = false;
                    this.btnSend.IsEnabled = true;
                    this.textBoxSend.IsEnabled = true;

                    buffer = "";
                    receiveCount = 0;
                    sendCount = 0;
                    this.labReceiveCount.Text = "0";
                    this.labSendCount.Text = "0";

                    AppendReceive("串口已打开：" + this.comboPort.Text);
                    AppendReceive($"参数：{this.comboBaud.Text}bps {cleanDataBits}数据位 {cleanStopBits}停止位 {cleanParity}");
                    UpdateStatus($"已打开：{this.comboPort.Text} | {this.comboBaud.Text}bps, {cleanDataBits}数据位, {cleanStopBits}停止位, {cleanParity}");
                    SaveSerialSettings();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"打开串口失败：{ex.Message}\n\n请检查串口参数是否正确！", "错误",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        // ========== 接收数据 ==========
        private void serialPort1_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            this.Dispatcher.Invoke(new Action(() =>
            {
                try
                {
                    string newData = this.serialPort1.ReadExisting();
                    buffer += newData;

                    while (buffer.Contains("\n"))
                    {
                        int index = buffer.IndexOf("\n");
                        string line = buffer.Substring(0, index).TrimEnd('\r');
                        buffer = buffer.Substring(index + 1);

                        if (!string.IsNullOrEmpty(line))
                        {
                            receiveCount += line.Length;
                            this.labReceiveCount.Text = receiveCount.ToString();
                            AppendReceive(line);
                        }
                    }

                    if (buffer.Length > 10240)
                    {
                        AppendReceive("[警告] 缓存溢出，已清空");
                        buffer = "";
                    }
                }
                catch (Exception ex)
                {
                    AppendReceive("接收错误：" + ex.Message);
                }
            }));
        }

        // ========== 发送数据 ==========
        private void btnSend_Click(object sender, RoutedEventArgs e)
        {
            if (!this.serialPort1.IsOpen)
            {
                MessageBox.Show("请先打开串口！", "提示");
                return;
            }

            string sendData = this.textBoxSend.Text;
            if (string.IsNullOrEmpty(sendData))
            {
                MessageBox.Show("请输入要发送的数据！", "提示");
                return;
            }

            try
            {
                if (this.chkHexSend.IsChecked == true)
                {
                    SendHexData(sendData);
                }
                else
                {
                    this.serialPort1.Write(sendData + "\r\n");
                    sendCount += sendData.Length;
                    this.labSendCount.Text = sendCount.ToString();
                    AppendReceive("→ " + sendData);
                }
                this.textBoxSend.Clear();
            }
            catch (Exception ex)
            {
                MessageBox.Show("发送失败：" + ex.Message, "错误");
            }
        }

        // ========== 十六进制发送 ==========
        private void SendHexData(string hexString)
        {
            try
            {
                hexString = hexString.Replace(" ", "").Replace("\t", "");
                if (string.IsNullOrEmpty(hexString))
                {
                    MessageBox.Show("请输入十六进制数据！", "提示");
                    return;
                }

                if (hexString.Length % 2 != 0)
                {
                    MessageBox.Show("十六进制字符串长度必须为偶数！", "提示");
                    return;
                }

                byte[] bytes = new byte[hexString.Length / 2];
                for (int i = 0; i < hexString.Length; i += 2)
                {
                    bytes[i / 2] = Convert.ToByte(hexString.Substring(i, 2), 16);
                }
                this.serialPort1.Write(bytes, 0, bytes.Length);
                sendCount += bytes.Length;
                this.labSendCount.Text = sendCount.ToString();
                AppendReceive("→ [HEX] " + hexString.ToUpper());
            }
            catch (Exception ex)
            {
                MessageBox.Show("十六进制转换失败：" + ex.Message, "错误");
            }
        }

        // ========== 清空接收区 ==========
        private void btnClear_Click(object sender, RoutedEventArgs e)
        {
            this.textBoxReceive.Clear();
            receiveCount = 0;
            sendCount = 0;
            this.labReceiveCount.Text = "0";
            this.labSendCount.Text = "0";
            UpdateStatus("已清空");
        }

        // ========== 保存日志 ==========
        private void SaveLog(object sender, RoutedEventArgs e)
        {
            try
            {
                string logText = this.textBoxReceive.Text;
                if (string.IsNullOrWhiteSpace(logText))
                {
                    MessageBox.Show("接收区没有数据可保存！", "提示",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                string fileName = "串口日志_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".txt";
                string filePath = System.IO.Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                    fileName
                );

                string header = $"=== 串口日志 ===\n保存时间：{DateTime.Now:yyyy-MM-dd HH:mm:ss}\n\n";
                System.IO.File.WriteAllText(filePath, header + logText);

                MessageBox.Show($"日志已保存到桌面：\n{fileName}\n\n共 {logText.Length} 个字符",
                    "保存成功", MessageBoxButton.OK, MessageBoxImage.Information);
                UpdateStatus("日志已保存：" + fileName);
            }
            catch (Exception ex)
            {
                MessageBox.Show("保存失败：" + ex.Message, "错误",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ========== 更新状态 ==========
        private void UpdateStatus(string message)
        {
            this.labStatus.Text = message;
        }

        // ========== 追加接收内容 ==========
        public void AppendReceive(string text)
        {
            string time = DateTime.Now.ToString("HH:mm:ss.fff");
            this.textBoxReceive.AppendText("[" + time + "] " + text + "\r\n");

            if (this.chkAutoScroll.IsChecked == true)
            {
                this.textBoxReceive.ScrollToEnd();
            }
        }

        // ========== 回车发送 ==========
        private void textBoxSend_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                btnSend_Click(sender, e);
                e.Handled = true;
            }
        }

        // ========== 窗口关闭 ==========
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (this.serialPort1.IsOpen)
            {
                this.serialPort1.Close();
            }
            SaveSerialSettings();
            SaveWindowSettings();
        }

        // ========== 窗口大小变化 ==========
        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
        }

        // ========== 窗口位置变化 ==========
        private void Window_LocationChanged(object sender, EventArgs e)
        {
        }

        // ========== 置顶开关 ==========
        private void chkTopMost_Checked(object sender, RoutedEventArgs e)
        {
            this.Topmost = true;
            UpdateStatus("窗口已置顶");
            AppendReceive("[系统] 窗口置顶已开启");
        }

        private void chkTopMost_Unchecked(object sender, RoutedEventArgs e)
        {
            this.Topmost = false;
            UpdateStatus("窗口已取消置顶");
            AppendReceive("[系统] 窗口置顶已关闭");
        }

        // ========== 复制接收区数据 ==========
        private void CopyReceiveData(object sender, RoutedEventArgs e)
        {
            try
            {
                string selectedText = this.textBoxReceive.SelectedText;
                if (string.IsNullOrEmpty(selectedText))
                {
                    if (!string.IsNullOrEmpty(this.textBoxReceive.Text))
                    {
                        Clipboard.SetText(this.textBoxReceive.Text);
                        UpdateStatus($"已复制全部数据 ({this.textBoxReceive.Text.Length} 个字符)");
                        AppendReceive($"[系统] 已复制全部数据 ({this.textBoxReceive.Text.Length} 个字符)");
                    }
                    else
                    {
                        MessageBox.Show("接收区没有数据可复制！", "提示",
                            MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
                else
                {
                    Clipboard.SetText(selectedText);
                    UpdateStatus($"已复制选中的数据 ({selectedText.Length} 个字符)");
                    AppendReceive($"[系统] 已复制选中的数据 ({selectedText.Length} 个字符)");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("复制失败：" + ex.Message, "错误",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ========== 全选接收区数据 ==========
        private void SelectAllReceiveData(object sender, RoutedEventArgs e)
        {
            this.textBoxReceive.SelectAll();
            this.textBoxReceive.Focus();
            UpdateStatus("已全选");
        }

        // ========== 窗口键盘事件 ==========
        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            // Ctrl+C 复制
            if (e.Key == Key.C && Keyboard.Modifiers == ModifierKeys.Control)
            {
                if (!(Keyboard.FocusedElement is TextBox) || Keyboard.FocusedElement == this.textBoxReceive)
                {
                    CopyReceiveData(sender, e);
                    e.Handled = true;
                }
            }
            // Ctrl+A 全选
            else if (e.Key == Key.A && Keyboard.Modifiers == ModifierKeys.Control)
            {
                if (Keyboard.FocusedElement == this.textBoxReceive)
                {
                    SelectAllReceiveData(sender, e);
                    e.Handled = true;
                }
            }
        }

        // ========== 清理字符串中的引号 ==========
        private string CleanString(string input)
        {
            if (string.IsNullOrEmpty(input)) return input;
            return input.Trim('"').Trim('\'');
        }
    }
}
