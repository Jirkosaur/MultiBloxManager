using System.Drawing;
using System.Windows.Forms;

namespace MultiBloxManager
{
    partial class MainForm
    {
        private System.ComponentModel.IContainer components = null;

        private Panel topPanel;
        private Label lblAppTitle;
        private Label lblStatus;
        private Button btnToggle;

        private Panel logBorderPanel;
        private RichTextBox rtbConsole;

        private Panel bottomPanel;
        private Label lblFooter;
        private Button btnClear;

        private NotifyIcon trayIcon;
        private ContextMenuStrip trayContextMenu;
        private ToolStripMenuItem menuOpen;
        private ToolStripMenuItem menuToggle;
        private ToolStripSeparator menuSeparator1;
        private ToolStripMenuItem menuGitHub;
        private ToolStripSeparator menuSeparator2;
        private ToolStripMenuItem menuExit;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();

            // Main Window specs
            this.ClientSize = new Size(420, 360);
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Text = "MultiBloxManager";
            this.BackColor = Color.FromArgb(30, 30, 30); // #1E1E1E
            this.ForeColor = Color.FromArgb(212, 212, 212); // #D4D4D4

            // Top Header Panel
            this.topPanel = new Panel();
            this.topPanel.Location = new Point(12, 12);
            this.topPanel.Size = new Size(396, 52);
            this.topPanel.BackColor = Color.FromArgb(37, 37, 38); // #252526

            this.lblAppTitle = new Label();
            this.lblAppTitle.Text = "MultiBloxManager";
            this.lblAppTitle.Font = new Font("Segoe UI", 12F, FontStyle.Bold, GraphicsUnit.Point);
            this.lblAppTitle.ForeColor = Color.White;
            this.lblAppTitle.Location = new Point(10, 8);
            this.lblAppTitle.AutoSize = true;

            this.lblStatus = new Label();
            this.lblStatus.Text = "● Monitoring Active";
            this.lblStatus.Font = new Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point);
            this.lblStatus.ForeColor = Color.FromArgb(78, 201, 176); // #4EC9B0
            this.lblStatus.Location = new Point(11, 30);
            this.lblStatus.AutoSize = true;

            this.btnToggle = new Button();
            this.btnToggle.Text = "Pause";
            this.btnToggle.Font = new Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point);
            this.btnToggle.Location = new Point(300, 11);
            this.btnToggle.Size = new Size(86, 30);
            this.btnToggle.FlatStyle = FlatStyle.Flat;
            this.btnToggle.FlatAppearance.BorderSize = 1;
            this.btnToggle.FlatAppearance.BorderColor = Color.FromArgb(62, 62, 66); // #3E3E42
            this.btnToggle.BackColor = Color.FromArgb(45, 45, 48);
            this.btnToggle.ForeColor = Color.FromArgb(212, 212, 212);
            this.btnToggle.Cursor = Cursors.Hand;
            this.btnToggle.Click += new System.EventHandler(this.BtnToggle_Click);

            this.topPanel.Controls.Add(this.lblAppTitle);
            this.topPanel.Controls.Add(this.lblStatus);
            this.topPanel.Controls.Add(this.btnToggle);

            // Log Console Border Panel
            this.logBorderPanel = new Panel();
            this.logBorderPanel.Location = new Point(12, 72);
            this.logBorderPanel.Size = new Size(396, 246);
            this.logBorderPanel.BackColor = Color.FromArgb(62, 62, 66); // Border #3E3E42
            this.logBorderPanel.Padding = new Padding(1);

            // Log Console RichTextBox
            this.rtbConsole = new RichTextBox();
            this.rtbConsole.Dock = DockStyle.Fill;
            this.rtbConsole.BackColor = Color.FromArgb(37, 37, 38); // #252526
            this.rtbConsole.ForeColor = Color.FromArgb(212, 212, 212); // #D4D4D4
            this.rtbConsole.BorderStyle = BorderStyle.None;
            this.rtbConsole.Font = new Font("Consolas", 8.5F, FontStyle.Regular, GraphicsUnit.Point);
            this.rtbConsole.ReadOnly = true;
            this.rtbConsole.ScrollBars = RichTextBoxScrollBars.Vertical;
            this.logBorderPanel.Controls.Add(this.rtbConsole);

            // Bottom Footer Panel
            this.bottomPanel = new Panel();
            this.bottomPanel.Location = new Point(12, 324);
            this.bottomPanel.Size = new Size(396, 26);
            this.bottomPanel.BackColor = Color.Transparent;

            this.lblFooter = new Label();
            this.lblFooter.Text = "Multi-Instance Engine | Pure Native Win32";
            this.lblFooter.Font = new Font("Segoe UI", 7.5F, FontStyle.Regular, GraphicsUnit.Point);
            this.lblFooter.ForeColor = Color.FromArgb(133, 133, 133); // #858585
            this.lblFooter.Location = new Point(2, 6);
            this.lblFooter.AutoSize = true;

            this.btnClear = new Button();
            this.btnClear.Text = "Clear Log";
            this.btnClear.Font = new Font("Segoe UI", 7.5F, FontStyle.Regular, GraphicsUnit.Point);
            this.btnClear.Size = new Size(65, 22);
            this.btnClear.Location = new Point(331, 2);
            this.btnClear.FlatStyle = FlatStyle.Flat;
            this.btnClear.FlatAppearance.BorderSize = 1;
            this.btnClear.FlatAppearance.BorderColor = Color.FromArgb(62, 62, 66);
            this.btnClear.BackColor = Color.FromArgb(37, 37, 38);
            this.btnClear.ForeColor = Color.FromArgb(170, 170, 170);
            this.btnClear.Cursor = Cursors.Hand;
            this.btnClear.Click += new System.EventHandler(this.BtnClear_Click);

            this.bottomPanel.Controls.Add(this.lblFooter);
            this.bottomPanel.Controls.Add(this.btnClear);

            // Tray Context Menu
            this.trayContextMenu = new ContextMenuStrip(this.components);
            this.trayContextMenu.BackColor = Color.FromArgb(37, 37, 38);
            this.trayContextMenu.ForeColor = Color.FromArgb(212, 212, 212);
            this.trayContextMenu.ShowImageMargin = false;

            this.menuOpen = new ToolStripMenuItem("Open MultiBloxManager", null, this.MenuOpen_Click);
            this.menuToggle = new ToolStripMenuItem("Pause Monitoring", null, this.MenuToggle_Click);
            this.menuSeparator1 = new ToolStripSeparator();
            this.menuGitHub = new ToolStripMenuItem("Visit GitHub", null, this.MenuGitHub_Click);
            this.menuSeparator2 = new ToolStripSeparator();
            this.menuExit = new ToolStripMenuItem("Exit", null, this.MenuExit_Click);

            this.trayContextMenu.Items.AddRange(new ToolStripItem[] {
                this.menuOpen,
                this.menuToggle,
                this.menuSeparator1,
                this.menuGitHub,
                this.menuSeparator2,
                this.menuExit
            });

            // Tray Icon
            this.trayIcon = new NotifyIcon(this.components);
            this.trayIcon.Text = "MultiBloxManager";
            this.trayIcon.ContextMenuStrip = this.trayContextMenu;
            this.trayIcon.Visible = true;
            this.trayIcon.DoubleClick += new System.EventHandler(this.TrayIcon_DoubleClick);

            // Assemble main form
            this.Controls.Add(this.topPanel);
            this.Controls.Add(this.logBorderPanel);
            this.Controls.Add(this.bottomPanel);
        }
    }
}
