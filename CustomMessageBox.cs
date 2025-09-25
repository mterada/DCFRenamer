using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace dscren
{
    public static class CustomMessageBox
    {
        public static DialogResult Show(string text, string caption = "", MessageBoxButtons buttons = MessageBoxButtons.OK, MessageBoxIcon icon = MessageBoxIcon.None)
        {
            using (var form = new Form())
            {
                form.Text = caption;
                form.StartPosition = FormStartPosition.CenterScreen;
                form.FormBorderStyle = FormBorderStyle.FixedDialog;
                form.MaximizeBox = false;
                form.MinimizeBox = false;
                form.ShowInTaskbar = false;
                form.Size = new Size(640, 360);
                form.Font = new Font("Meiryo UI", 14F, FontStyle.Regular, GraphicsUnit.Point);

                var label = new Label()
                {
                    AutoSize = false,
                    Text = text,
                    Dock = DockStyle.Top,
                    Height = 240,
                    TextAlign = ContentAlignment.MiddleCenter,
                    Font = form.Font
                };
                form.Controls.Add(label);

                var buttonPanel = new FlowLayoutPanel()
                {
                    Dock = DockStyle.Bottom,
                    FlowDirection = FlowDirection.RightToLeft,
                    Height = 60
                };
                form.Controls.Add(buttonPanel);

                DialogResult result = DialogResult.None;
                void AddButton(string btnText, DialogResult dr)
                {
                    var btn = new Button()
                    {
                        Text = btnText,
                        DialogResult = dr,
                        Font = form.Font,
                        Width = 120,
                        Height = 40,
                        Margin = new Padding(10)
                    };
                    btn.Click += (s, e) => { result = dr; form.Close(); };
                    buttonPanel.Controls.Add(btn);
                }

                switch (buttons)
                {
                    case MessageBoxButtons.OK:
                        AddButton("OK", DialogResult.OK);
                        break;
                    case MessageBoxButtons.OKCancel:
                        AddButton("Cancel", DialogResult.Cancel);
                        AddButton("OK", DialogResult.OK);
                        break;
                    case MessageBoxButtons.YesNo:
                        AddButton("No", DialogResult.No);
                        AddButton("Yes", DialogResult.Yes);
                        break;
                    case MessageBoxButtons.YesNoCancel:
                        AddButton("Cancel", DialogResult.Cancel);
                        AddButton("No", DialogResult.No);
                        AddButton("Yes", DialogResult.Yes);
                        break;
                }

                // アイコン表示（必要なら拡張可能）
                // ...

                form.AcceptButton = buttonPanel.Controls.OfType<Button>().FirstOrDefault(b => b.DialogResult == DialogResult.OK || b.DialogResult == DialogResult.Yes);
                form.CancelButton = buttonPanel.Controls.OfType<Button>().FirstOrDefault(b => b.DialogResult == DialogResult.Cancel || b.DialogResult == DialogResult.No);

                form.ShowDialog();
                return result;
            }
        }
    }
}
