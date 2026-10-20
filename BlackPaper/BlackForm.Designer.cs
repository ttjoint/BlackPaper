namespace BlackPaper
{
    partial class BlackForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(BlackForm));
            this.contextMenuStrip1 = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.toolStripComboBox1 = new System.Windows.Forms.ToolStripComboBox();
            this.ToolStripMenuItem_state = new System.Windows.Forms.ToolStripMenuItem();
            this.自动调整亮度ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.使用整屏压暗ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.开机自动启动ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.退出ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.timer1 = new System.Windows.Forms.Timer(this.components);
            this.notifyIcon2 = new System.Windows.Forms.NotifyIcon(this.components);
            this.contextMenuStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // contextMenuStrip1
            // 
            this.contextMenuStrip1.BackColor = System.Drawing.SystemColors.ControlLight;
            this.contextMenuStrip1.Font = new System.Drawing.Font("Microsoft YaHei UI", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.contextMenuStrip1.ImageScalingSize = new System.Drawing.Size(20, 20);
            this.contextMenuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripComboBox1,
            this.ToolStripMenuItem_state,
            this.自动调整亮度ToolStripMenuItem,
            this.使用整屏压暗ToolStripMenuItem,
            this.开机自动启动ToolStripMenuItem,
            this.退出ToolStripMenuItem});
            this.contextMenuStrip1.Name = "contextMenuStrip1";
            this.contextMenuStrip1.Size = new System.Drawing.Size(195, 170);
            // 
            // toolStripComboBox1
            // 
            this.toolStripComboBox1.BackColor = System.Drawing.SystemColors.ScrollBar;
            this.toolStripComboBox1.Items.AddRange(new object[] {
            "0--透明",
            "0.2",
            "0.4",
            "0.6",
            "0.8",
            "1--全黑"});
            this.toolStripComboBox1.Name = "toolStripComboBox1";
            this.toolStripComboBox1.Size = new System.Drawing.Size(121, 32);
            this.toolStripComboBox1.SelectedIndexChanged += new System.EventHandler(this.toolStripComboBox1_SelectedIndexChanged);
            // 
            // ToolStripMenuItem_state
            // 
            this.ToolStripMenuItem_state.Font = new System.Drawing.Font("Microsoft YaHei UI", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.ToolStripMenuItem_state.ForeColor = System.Drawing.SystemColors.ControlText;
            this.ToolStripMenuItem_state.Name = "ToolStripMenuItem_state";
            this.ToolStripMenuItem_state.Size = new System.Drawing.Size(194, 32);
            this.ToolStripMenuItem_state.Text = "当前亮度：%";
            // 
            // 自动调整亮度ToolStripMenuItem
            // 
            this.自动调整亮度ToolStripMenuItem.Font = new System.Drawing.Font("Microsoft YaHei UI", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.自动调整亮度ToolStripMenuItem.ForeColor = System.Drawing.Color.MidnightBlue;
            this.自动调整亮度ToolStripMenuItem.Name = "自动调整亮度ToolStripMenuItem";
            this.自动调整亮度ToolStripMenuItem.Size = new System.Drawing.Size(194, 32);
            this.自动调整亮度ToolStripMenuItem.Text = "自动亮度 @";
            this.自动调整亮度ToolStripMenuItem.Click += new System.EventHandler(this.自动调整亮度ToolStripMenuItem_Click);
            // 
            // 使用整屏压暗ToolStripMenuItem
            // 
            this.使用整屏压暗ToolStripMenuItem.Font = new System.Drawing.Font("Microsoft YaHei UI", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.使用整屏压暗ToolStripMenuItem.ForeColor = System.Drawing.Color.DarkGreen;
            this.使用整屏压暗ToolStripMenuItem.Name = "使用整屏压暗ToolStripMenuItem";
            this.使用整屏压暗ToolStripMenuItem.Size = new System.Drawing.Size(194, 32);
            this.使用整屏压暗ToolStripMenuItem.Text = "整屏压暗模式";
            this.使用整屏压暗ToolStripMenuItem.Click += new System.EventHandler(this.使用整屏压暗ToolStripMenuItem_Click);
            // 
            // 开机自动启动ToolStripMenuItem
            // 
            this.开机自动启动ToolStripMenuItem.Font = new System.Drawing.Font("Microsoft YaHei UI", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.开机自动启动ToolStripMenuItem.ForeColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.开机自动启动ToolStripMenuItem.Name = "开机自动启动ToolStripMenuItem";
            this.开机自动启动ToolStripMenuItem.Size = new System.Drawing.Size(194, 32);
            this.开机自动启动ToolStripMenuItem.Text = "开机自动启动";
            this.开机自动启动ToolStripMenuItem.Click += new System.EventHandler(this.开机自动启动ToolStripMenuItem_Click);
            // 
            // 退出ToolStripMenuItem
            // 
            this.退出ToolStripMenuItem.Font = new System.Drawing.Font("Microsoft YaHei UI", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.退出ToolStripMenuItem.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(192)))), ((int)(((byte)(64)))), ((int)(((byte)(0)))));
            this.退出ToolStripMenuItem.Name = "退出ToolStripMenuItem";
            this.退出ToolStripMenuItem.Size = new System.Drawing.Size(194, 32);
            this.退出ToolStripMenuItem.Text = "退出";
            this.退出ToolStripMenuItem.Click += new System.EventHandler(this.退出ToolStripMenuItem_Click);
            // 
            // timer1
            // 
            this.timer1.Enabled = true;
            this.timer1.Interval = 300000;
            this.timer1.Tick += new System.EventHandler(this.timer1_Tick);
            // 
            // notifyIcon2
            // 
            this.notifyIcon2.BalloonTipIcon = System.Windows.Forms.ToolTipIcon.Info;
            this.notifyIcon2.ContextMenuStrip = this.contextMenuStrip1;
            this.notifyIcon2.Text = "notifyIcon2";
            this.notifyIcon2.Visible = true;
            this.notifyIcon2.MouseDoubleClick += new System.Windows.Forms.MouseEventHandler(this.notifyIcon2_MouseDoubleClick);
            // 
            // BlackForm
            // 
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.BackColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.ClientSize = new System.Drawing.Size(917, 365);
            this.ControlBox = false;
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "BlackForm";
            this.Opacity = 0.01D;
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.Text = "BlackPaper";
            this.WindowState = System.Windows.Forms.FormWindowState.Normal;
            this.Load += new System.EventHandler(this.Form1_Load);
            this.contextMenuStrip1.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion
        private System.Windows.Forms.ContextMenuStrip contextMenuStrip1;
        private System.Windows.Forms.ToolStripMenuItem 退出ToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem 开机自动启动ToolStripMenuItem;
        private System.Windows.Forms.Timer timer1;
        private System.Windows.Forms.ToolStripMenuItem 自动调整亮度ToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem 使用整屏压暗ToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem ToolStripMenuItem_state;
        private System.Windows.Forms.ToolStripComboBox toolStripComboBox1;
        private System.Windows.Forms.NotifyIcon notifyIcon2;
    }
}

