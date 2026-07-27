using System;
using System.Collections.ObjectModel;
using System.IO.Ports;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;

namespace WpfApp1
{
    public partial class QuickSendControl : UserControl
    {
        private SerialPort serialPort;
        private MainWindow mainWindow;

        // 按钮数据
        public ObservableCollection<QuickSendItem> SendItems { get; set; }

        public QuickSendControl()
        {
            InitializeComponent();
            SendItems = new ObservableCollection<QuickSendItem>();
            LoadSavedItems();
            UpdateButtonCount();
        }

        public void SetSerialPort(SerialPort port, MainWindow window)
        {
            this.serialPort = port;
            this.mainWindow = window;
        }

        // ========== 添加按钮 ==========
        private void btnAdd_Click(object sender, RoutedEventArgs e)
        {
            string name = string.IsNullOrWhiteSpace(this.txtButtonName.Text) ? "新按钮" : this.txtButtonName.Text;
            string content = string.IsNullOrWhiteSpace(this.txtButtonContent.Text) ? "" : this.txtButtonContent.Text;

            if (string.IsNullOrEmpty(content))
            {
                MessageBox.Show("请输入发送内容！", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var item = new QuickSendItem
            {
                Name = name,
                Content = content,
                IsHex = this.mainWindow?.chkHexSend.IsChecked ?? false
            };

            SendItems.Add(item);
            CreateButton(item);
            SaveItems();
            UpdateButtonCount();

            this.txtButtonName.SelectAll();
            this.txtButtonName.Focus();
        }

        // ========== 创建按钮 ==========
        private void CreateButton(QuickSendItem item)
        {
            var border = new Border
            {
                BorderBrush = System.Windows.Media.Brushes.LightGray,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(6),
                Margin = new Thickness(5),
                Padding = new Thickness(8, 4, 8, 4),
                Background = System.Windows.Media.Brushes.White
            };

            var stackPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal
            };

            var btn = new Button
            {
                Content = item.Name,
                Tag = item,
                Style = (Style)this.Resources["SendButtonStyle"]
            };
            btn.Click += BtnSend_Click;
            btn.ContextMenu = CreateContextMenu(item);

            var btnDelete = new Button
            {
                Content = "✕",
                Tag = item,
                Style = (Style)this.Resources["DeleteButtonStyle"]
            };
            btnDelete.Click += BtnDelete_Click;

            stackPanel.Children.Add(btn);
            stackPanel.Children.Add(btnDelete);
            border.Child = stackPanel;
            this.pnlButtons.Children.Add(border);
        }

        // ========== 右键菜单 ==========
        private ContextMenu CreateContextMenu(QuickSendItem item)
        {
            var menu = new ContextMenu();

            var menuEdit = new MenuItem { Header = "✏️ 编辑" };
            menuEdit.Click += (s, e) => EditItem(item);
            menu.Items.Add(menuEdit);

            menu.Items.Add(new Separator());

            var menuUp = new MenuItem { Header = "⬆ 上移" };
            menuUp.Click += (s, e) => MoveItem(item, -1);
            menu.Items.Add(menuUp);

            var menuDown = new MenuItem { Header = "⬇ 下移" };
            menuDown.Click += (s, e) => MoveItem(item, 1);
            menu.Items.Add(menuDown);

            menu.Items.Add(new Separator());

            var menuDelete = new MenuItem { Header = "🗑 删除", Foreground = System.Windows.Media.Brushes.Red };
            menuDelete.Click += (s, e) => DeleteItem(item);
            menu.Items.Add(menuDelete);

            return menu;
        }

        // ========== 编辑 ==========
        private void EditItem(QuickSendItem item)
        {
            var dialog = new QuickSendEditDialog(item, this.mainWindow);
            if (dialog.ShowDialog() == true)
            {
                RefreshButtons();
                SaveItems();
            }
        }

        // ========== 上移/下移 ==========
        private void MoveItem(QuickSendItem item, int direction)
        {
            int index = SendItems.IndexOf(item);
            int newIndex = index + direction;
            if (newIndex < 0 || newIndex >= SendItems.Count) return;

            SendItems.Move(index, newIndex);
            RefreshButtons();
            SaveItems();
        }

        // ========== 删除 ==========
        private void DeleteItem(QuickSendItem item)
        {
            var result = MessageBox.Show($"确定要删除 \"{item.Name}\" 吗？", "确认删除",
                MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result != MessageBoxResult.Yes) return;

            SendItems.Remove(item);
            RefreshButtons();
            SaveItems();
            UpdateButtonCount();
        }

        // ========== 刷新按钮 ==========
        private void RefreshButtons()
        {
            this.pnlButtons.Children.Clear();
            foreach (var item in SendItems)
            {
                CreateButton(item);
            }
        }

        // ========== 发送按钮点击 ==========
        private void BtnSend_Click(object sender, RoutedEventArgs e)
        {
            if (serialPort == null || !serialPort.IsOpen)
            {
                MessageBox.Show("请先打开串口！", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var btn = sender as Button;
            var item = btn?.Tag as QuickSendItem;
            if (item == null) return;

            try
            {
                if (item.IsHex)
                {
                    SendHexData(item.Content, item.Name);
                }
                else
                {
                    serialPort.Write(item.Content + "\r\n");
                    mainWindow?.AppendReceive("→ [快捷] " + item.Content + " (" + item.Name + ")");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("发送失败：" + ex.Message, "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ========== 删除按钮点击 ==========
        private void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            var item = btn?.Tag as QuickSendItem;
            if (item == null) return;
            DeleteItem(item);
        }

        // ========== 十六进制发送 ==========
        private void SendHexData(string hexString, string name = "")
        {
            try
            {
                hexString = hexString.Replace(" ", "").Replace("\t", "");
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
                serialPort.Write(bytes, 0, bytes.Length);

                string displayName = string.IsNullOrEmpty(name) ? "" : " (" + name + ")";
                mainWindow?.AppendReceive("→ [HEX快捷] " + hexString.ToUpper() + displayName);
            }
            catch (Exception ex)
            {
                MessageBox.Show("十六进制转换失败：" + ex.Message, "错误");
            }
        }

        // ========== 导出配置 ==========
        private void btnExport_Click(object sender, RoutedEventArgs e)
        {
            if (SendItems.Count == 0)
            {
                MessageBox.Show("没有快捷按钮可以导出！", "提示",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var saveDialog = new SaveFileDialog
            {
                Title = "导出快捷按钮配置",
                Filter = "JSON文件 (*.json)|*.json|所有文件 (*.*)|*.*",
                FileName = "快捷按钮配置_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".json"
            };

            if (saveDialog.ShowDialog() == true)
            {
                try
                {
                    string json = SerializeToJson(SendItems);
                    System.IO.File.WriteAllText(saveDialog.FileName, json);
                    MessageBox.Show($"配置已导出到：\n{saveDialog.FileName}", "导出成功",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("导出失败：" + ex.Message, "错误",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        // ========== 导入配置 ==========
        private void btnImport_Click(object sender, RoutedEventArgs e)
        {
            var openDialog = new OpenFileDialog
            {
                Title = "导入快捷按钮配置",
                Filter = "JSON文件 (*.json)|*.json|所有文件 (*.*)|*.*"
            };

            if (openDialog.ShowDialog() == true)
            {
                try
                {
                    string json = System.IO.File.ReadAllText(openDialog.FileName);
                    var items = DeserializeFromJson(json);

                    if (items == null || items.Count == 0)
                    {
                        MessageBox.Show("配置文件格式错误或为空！", "导入失败",
                            MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    var result = MessageBox.Show(
                        $"即将导入 {items.Count} 个快捷按钮。\n\n点击 [是] 覆盖当前配置\n点击 [否] 追加到当前配置",
                        "导入确认",
                        MessageBoxButton.YesNoCancel,
                        MessageBoxImage.Question);

                    if (result == MessageBoxResult.Cancel) return;

                    if (result == MessageBoxResult.Yes)
                    {
                        ClearAllButtons();
                    }

                    foreach (var item in items)
                    {
                        SendItems.Add(item);
                        CreateButton(item);
                    }

                    SaveItems();
                    UpdateButtonCount();

                    MessageBox.Show($"成功导入 {items.Count} 个快捷按钮！", "导入成功",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("导入失败：" + ex.Message, "错误",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        // ========== 清空所有按钮 ==========
        private void btnClearAll_Click(object sender, RoutedEventArgs e)
        {
            if (SendItems.Count == 0)
            {
                MessageBox.Show("没有按钮需要清空！", "提示",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var result = MessageBox.Show($"确定要清空所有 {SendItems.Count} 个快捷按钮吗？",
                "确认清空", MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                ClearAllButtons();
                SaveItems();
                UpdateButtonCount();
            }
        }

        private void ClearAllButtons()
        {
            SendItems.Clear();
            this.pnlButtons.Children.Clear();
        }

        private void UpdateButtonCount()
        {
            this.labButtonCount.Text = $"共 {SendItems.Count} 个快捷按钮";
        }

        // ========== 序列化/反序列化 ==========
        private string SerializeToJson(ObservableCollection<QuickSendItem> items)
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.AppendLine("[");
            for (int i = 0; i < items.Count; i++)
            {
                var item = items[i];
                sb.AppendLine("  {");
                sb.AppendLine($"    \"Name\": \"{item.Name.Replace("\"", "\\\"")}\",");
                sb.AppendLine($"    \"Content\": \"{item.Content.Replace("\"", "\\\"")}\",");
                sb.AppendLine($"    \"IsHex\": {item.IsHex.ToString().ToLower()}");
                sb.Append(i < items.Count - 1 ? "  }," : "  }");
                sb.AppendLine();
            }
            sb.AppendLine("]");
            return sb.ToString();
        }

        private ObservableCollection<QuickSendItem> DeserializeFromJson(string json)
        {
            var result = new ObservableCollection<QuickSendItem>();
            try
            {
                string[] lines = json.Split('\n');
                string currentName = "", currentContent = "";
                bool currentIsHex = false, inObject = false;

                foreach (string line in lines)
                {
                    string trimmed = line.Trim();
                    if (trimmed == "{") { inObject = true; currentName = ""; currentContent = ""; currentIsHex = false; }
                    else if (trimmed == "}," || trimmed == "}")
                    {
                        if (inObject && !string.IsNullOrEmpty(currentName))
                            result.Add(new QuickSendItem { Name = currentName, Content = currentContent, IsHex = currentIsHex });
                        inObject = false;
                    }
                    else if (inObject && trimmed.Contains("\"Name\""))
                    {
                        int start = trimmed.IndexOf(":") + 1;
                        currentName = trimmed.Substring(start).Trim().Trim(',').Trim('"');
                    }
                    else if (inObject && trimmed.Contains("\"Content\""))
                    {
                        int start = trimmed.IndexOf(":") + 1;
                        currentContent = trimmed.Substring(start).Trim().Trim(',').Trim('"');
                    }
                    else if (inObject && trimmed.Contains("\"IsHex\""))
                    {
                        int start = trimmed.IndexOf(":") + 1;
                        currentIsHex = trimmed.Substring(start).Trim().Trim(',').ToLower() == "true";
                    }
                }
            }
            catch { }
            return result;
        }

        // ========== 按钮数据序列化（保存到Settings） ==========
        private string SerializeItems(ObservableCollection<QuickSendItem> items)
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            foreach (var item in items)
            {
                if (sb.Length > 0) sb.Append(";");
                sb.Append(item.Name);
                sb.Append("|");
                sb.Append(item.Content.Replace("|", "||"));
                sb.Append("|");
                sb.Append(item.IsHex ? "1" : "0");
            }
            return sb.ToString();
        }

        private ObservableCollection<QuickSendItem> DeserializeItems(string data)
        {
            var result = new ObservableCollection<QuickSendItem>();
            if (string.IsNullOrEmpty(data)) return result;

            string[] items = data.Split(';');
            foreach (string itemStr in items)
            {
                if (string.IsNullOrEmpty(itemStr)) continue;
                string[] parts = itemStr.Split('|');
                if (parts.Length >= 3)
                {
                    result.Add(new QuickSendItem
                    {
                        Name = parts[0],
                        Content = parts[1].Replace("||", "|"),
                        IsHex = parts[2] == "1"
                    });
                }
            }
            return result;
        }

        private void LoadSavedItems()
        {
            try
            {
                string saved = Properties.Settings.Default.QuickSendItems;
                if (!string.IsNullOrEmpty(saved))
                {
                    var items = DeserializeItems(saved);
                    if (items != null && items.Count > 0)
                    {
                        SendItems = items;
                        foreach (var item in items)
                        {
                            CreateButton(item);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("加载快捷按钮失败：" + ex.Message);
            }
        }

        private void SaveItems()
        {
            try
            {
                string data = SerializeItems(SendItems);
                Properties.Settings.Default.QuickSendItems = data;
                Properties.Settings.Default.Save();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("保存快捷按钮失败：" + ex.Message);
            }
        }
    }

    [Serializable]
    public class QuickSendItem
    {
        public string Name { get; set; } = "新按钮";
        public string Content { get; set; } = "";
        public bool IsHex { get; set; } = false;
    }
}
