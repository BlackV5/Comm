using System.Windows;

namespace WpfApp1
{
    public partial class QuickSendEditDialog : Window
    {
        private QuickSendItem editingItem;

        public QuickSendEditDialog(QuickSendItem item, Window owner)
        {
            InitializeComponent();

            this.editingItem = item;

            this.txtName.Text = item.Name;
            this.txtContent.Text = item.Content;
            this.chkHex.IsChecked = item.IsHex;

            // 窗口居中于主窗口
            if (owner != null)
            {
                this.Left = owner.Left + (owner.Width - this.Width) / 2;
                this.Top = owner.Top + (owner.Height - this.Height) / 2;
            }
        }

        private void btnOK_Click(object sender, RoutedEventArgs e)
        {
            editingItem.Name = string.IsNullOrWhiteSpace(this.txtName.Text) ? "未命名" : this.txtName.Text;
            editingItem.Content = this.txtContent.Text;
            editingItem.IsHex = this.chkHex.IsChecked ?? false;

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
