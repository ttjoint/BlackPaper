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
            this.contextMenuStrip1 = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.ToolStripMenuItem_state = new System.Windows.Forms.ToolStripMenuItem();
            this.sep1 = new System.Windows.Forms.ToolStripSeparator();
            this.设置ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.自动调整亮度ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.平滑过渡ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.启用屏幕压暗ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.sep2 = new System.Windows.Forms.ToolStripSeparator();
            this.使用整屏压暗ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.开机自动启动ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.sep3 = new System.Windows.Forms.ToolStripSeparator();
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
            this.ToolStripMenuItem_state,
            this.sep1,
            this.启用屏幕压暗ToolStripMenuItem,
            this.设置ToolStripMenuItem,
            this.自动调整亮度ToolStripMenuItem,
            this.sep2,
            this.平滑过渡ToolStripMenuItem,
            this.使用整屏压暗ToolStripMenuItem,
            this.开机自动启动ToolStripMenuItem,
            this.sep3,
            this.退出ToolStripMenuItem});
            this.contextMenuStrip1.Name = "contextMenuStrip1";
            this.contextMenuStrip1.Size = new System.Drawing.Size(181, 220);
            // 
            // ToolStripMenuItem_state
            // 
            this.ToolStripMenuItem_state.Enabled = false;
            this.ToolStripMenuItem_state.Font = new System.Drawing.Font("Microsoft YaHei UI", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.ToolStripMenuItem_state.ForeColor = System.Drawing.SystemColors.ControlText;
            this.ToolStripMenuItem_state.Name = "ToolStripMenuItem_state";
            this.ToolStripMenuItem_state.Size = new System.Drawing.Size(180, 22);
            this.ToolStripMenuItem_state.Text = "亮度：%";
            // 
            // sep1
            // 
            this.sep1.Name = "sep1";
            this.sep1.Size = new System.Drawing.Size(159, 6);
            // 
            // 设置ToolStripMenuItem
            // 
            this.设置ToolStripMenuItem.Font = new System.Drawing.Font("Microsoft YaHei UI", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.设置ToolStripMenuItem.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(33)))), ((int)(((byte)(110)))), ((int)(((byte)(190)))));
            this.设置ToolStripMenuItem.Name = "设置ToolStripMenuItem";
            this.设置ToolStripMenuItem.Size = new System.Drawing.Size(162, 22);
            this.设置ToolStripMenuItem.Text = "设置…";
            this.设置ToolStripMenuItem.Click += new System.EventHandler(this.设置ToolStripMenuItem_Click);
            // 
            // 自动调整亮度ToolStripMenuItem
            // 
            this.自动调整亮度ToolStripMenuItem.Font = new System.Drawing.Font("Microsoft YaHei UI", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.自动调整亮度ToolStripMenuItem.ForeColor = System.Drawing.Color.MidnightBlue;
            this.自动调整亮度ToolStripMenuItem.Name = "自动调整亮度ToolStripMenuItem";
            this.自动调整亮度ToolStripMenuItem.Size = new System.Drawing.Size(180, 22);
            this.自动调整亮度ToolStripMenuItem.Text = "自动调节";
            this.自动调整亮度ToolStripMenuItem.Click += new System.EventHandler(this.自动调整亮度ToolStripMenuItem_Click);
            // 
            // 平滑过渡ToolStripMenuItem
            // 
            this.平滑过渡ToolStripMenuItem.Font = new System.Drawing.Font("Microsoft YaHei UI", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.平滑过渡ToolStripMenuItem.ForeColor = System.Drawing.Color.MidnightBlue;
            this.平滑过渡ToolStripMenuItem.Name = "平滑过渡ToolStripMenuItem";
            this.平滑过渡ToolStripMenuItem.Size = new System.Drawing.Size(180, 22);
            this.平滑过渡ToolStripMenuItem.Text = "渐变";
            this.平滑过渡ToolStripMenuItem.Click += new System.EventHandler(this.平滑过渡ToolStripMenuItem_Click);
            // 
            // 启用屏幕压暗ToolStripMenuItem
            // 
            this.启用屏幕压暗ToolStripMenuItem.Font = new System.Drawing.Font("Microsoft YaHei UI", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.启用屏幕压暗ToolStripMenuItem.ForeColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.启用屏幕压暗ToolStripMenuItem.Name = "启用屏幕压暗ToolStripMenuItem";
            this.启用屏幕压暗ToolStripMenuItem.Size = new System.Drawing.Size(180, 22);
            this.启用屏幕压暗ToolStripMenuItem.Text = "启用亮度遮罩";
            this.启用屏幕压暗ToolStripMenuItem.Click += new System.EventHandler(this.启用屏幕压暗ToolStripMenuItem_Click);
            // 
            // sep2
            // 
            this.sep2.Name = "sep2";
            this.sep2.Size = new System.Drawing.Size(159, 6);
            // 
            // 使用整屏压暗ToolStripMenuItem
            // 
            this.使用整屏压暗ToolStripMenuItem.Font = new System.Drawing.Font("Microsoft YaHei UI", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.使用整屏压暗ToolStripMenuItem.ForeColor = System.Drawing.Color.DarkGreen;
            this.使用整屏压暗ToolStripMenuItem.Name = "使用整屏压暗ToolStripMenuItem";
            this.使用整屏压暗ToolStripMenuItem.Size = new System.Drawing.Size(180, 22);
            this.使用整屏压暗ToolStripMenuItem.Text = "完整覆盖模式";
            this.使用整屏压暗ToolStripMenuItem.Click += new System.EventHandler(this.使用整屏压暗ToolStripMenuItem_Click);
            // 
            // 开机自动启动ToolStripMenuItem
            // 
            this.开机自动启动ToolStripMenuItem.Font = new System.Drawing.Font("Microsoft YaHei UI", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.开机自动启动ToolStripMenuItem.ForeColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.开机自动启动ToolStripMenuItem.Name = "开机自动启动ToolStripMenuItem";
            this.开机自动启动ToolStripMenuItem.Size = new System.Drawing.Size(180, 22);
            this.开机自动启动ToolStripMenuItem.Text = "开机启动";
            this.开机自动启动ToolStripMenuItem.Click += new System.EventHandler(this.开机自动启动ToolStripMenuItem_Click);
            // 
            // sep3
            // 
            this.sep3.Name = "sep3";
            this.sep3.Size = new System.Drawing.Size(159, 6);
            // 
            // 退出ToolStripMenuItem
            // 
            this.退出ToolStripMenuItem.Font = new System.Drawing.Font("Microsoft YaHei UI", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.退出ToolStripMenuItem.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(192)))), ((int)(((byte)(64)))), ((int)(((byte)(0)))));
            this.退出ToolStripMenuItem.Name = "退出ToolStripMenuItem";
            this.退出ToolStripMenuItem.Size = new System.Drawing.Size(162, 22);
            this.退出ToolStripMenuItem.Text = "退出";
            this.退出ToolStripMenuItem.Click += new System.EventHandler(this.退出ToolStripMenuItem_Click);
            // 
            // timer1
            // 
            this.timer1.Enabled = true;
            this.timer1.Interval = 60000;
            this.timer1.Tick += new System.EventHandler(this.timer1_Tick);
            // 
            // notifyIcon2
            // 
            this.notifyIcon2.BalloonTipIcon = System.Windows.Forms.ToolTipIcon.Info;
            this.notifyIcon2.ContextMenuStrip = this.contextMenuStrip1;
            this.notifyIcon2.Text = "BlackPaper";
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
        private System.Windows.Forms.ToolStripMenuItem 平滑过渡ToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem 启用屏幕压暗ToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem 使用整屏压暗ToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem 设置ToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem ToolStripMenuItem_state;
        private System.Windows.Forms.ToolStripSeparator sep1;
        private System.Windows.Forms.ToolStripSeparator sep2;
        private System.Windows.Forms.ToolStripSeparator sep3;
        private System.Windows.Forms.NotifyIcon notifyIcon2;
    }
}
