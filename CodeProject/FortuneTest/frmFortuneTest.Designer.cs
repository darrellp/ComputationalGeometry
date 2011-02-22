using NetTrace;

namespace FortuneTest
{
	partial class frmFortuneTest
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
			this.pnlDraw = new System.Windows.Forms.Panel();
			this.btnSinglePoint = new System.Windows.Forms.Button();
			this.btnVTriangle = new System.Windows.Forms.Button();
			this.btnHTriangle = new System.Windows.Forms.Button();
			this.btnCompute = new System.Windows.Forms.Button();
			this.btnTraceTags = new System.Windows.Forms.Button();
			this.btnClear = new System.Windows.Forms.Button();
			this.btnCHTriangle = new System.Windows.Forms.Button();
			this.btnRectangle = new System.Windows.Forms.Button();
			this.btnWriteTags = new System.Windows.Forms.Button();
			this.btnReadLastPts = new System.Windows.Forms.Button();
			this.btnWritePts = new System.Windows.Forms.Button();
			this.sfdPoints = new System.Windows.Forms.SaveFileDialog();
			this.ofdPoints = new System.Windows.Forms.OpenFileDialog();
			this.sstMain = new System.Windows.Forms.StatusStrip();
			this.toolStripStatusLabel1 = new System.Windows.Forms.ToolStripStatusLabel();
			this.sstlblGenIndex = new System.Windows.Forms.ToolStripStatusLabel();
			this.sstMain.SuspendLayout();
			this.SuspendLayout();
			// 
			// pnlDraw
			// 
			this.pnlDraw.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(224)))), ((int)(((byte)(192)))));
			this.pnlDraw.Location = new System.Drawing.Point(12, 12);
			this.pnlDraw.Name = "pnlDraw";
			this.pnlDraw.Size = new System.Drawing.Size(366, 358);
			this.pnlDraw.TabIndex = 0;
			this.pnlDraw.MouseDown += new System.Windows.Forms.MouseEventHandler(this.pnlDraw_MouseDown);
			this.pnlDraw.MouseMove += new System.Windows.Forms.MouseEventHandler(this.pnlDraw_MouseMove);
			this.pnlDraw.Paint += new System.Windows.Forms.PaintEventHandler(this.panel1_Paint);
			// 
			// btnSinglePoint
			// 
			this.btnSinglePoint.Location = new System.Drawing.Point(395, 13);
			this.btnSinglePoint.Name = "btnSinglePoint";
			this.btnSinglePoint.Size = new System.Drawing.Size(106, 23);
			this.btnSinglePoint.TabIndex = 1;
			this.btnSinglePoint.Text = "Single Point";
			this.btnSinglePoint.UseVisualStyleBackColor = true;
			this.btnSinglePoint.Click += new System.EventHandler(this.btnSinglePoint_Click);
			// 
			// btnVTriangle
			// 
			this.btnVTriangle.Location = new System.Drawing.Point(395, 42);
			this.btnVTriangle.Name = "btnVTriangle";
			this.btnVTriangle.Size = new System.Drawing.Size(106, 24);
			this.btnVTriangle.TabIndex = 2;
			this.btnVTriangle.Text = "Vertical Triangle";
			this.btnVTriangle.UseVisualStyleBackColor = true;
			this.btnVTriangle.Click += new System.EventHandler(this.btnVTriangle_Click);
			// 
			// btnHTriangle
			// 
			this.btnHTriangle.Location = new System.Drawing.Point(395, 72);
			this.btnHTriangle.Name = "btnHTriangle";
			this.btnHTriangle.Size = new System.Drawing.Size(106, 24);
			this.btnHTriangle.TabIndex = 3;
			this.btnHTriangle.Text = "Horizontal Triangle";
			this.btnHTriangle.UseVisualStyleBackColor = true;
			this.btnHTriangle.Click += new System.EventHandler(this.btnHTriangle_Click);
			// 
			// btnCompute
			// 
			this.btnCompute.Location = new System.Drawing.Point(395, 344);
			this.btnCompute.Name = "btnCompute";
			this.btnCompute.Size = new System.Drawing.Size(106, 26);
			this.btnCompute.TabIndex = 4;
			this.btnCompute.Text = "Compute";
			this.btnCompute.UseVisualStyleBackColor = true;
			this.btnCompute.Click += new System.EventHandler(this.btnCompute_Click);
			// 
			// btnTraceTags
			// 
			this.btnTraceTags.Location = new System.Drawing.Point(395, 311);
			this.btnTraceTags.Name = "btnTraceTags";
			this.btnTraceTags.Size = new System.Drawing.Size(106, 27);
			this.btnTraceTags.TabIndex = 5;
			this.btnTraceTags.Text = "Trace Tags";
			this.btnTraceTags.UseVisualStyleBackColor = true;
			this.btnTraceTags.Click += new System.EventHandler(this.btnTraceTags_Click);
			// 
			// btnClear
			// 
			this.btnClear.Location = new System.Drawing.Point(395, 278);
			this.btnClear.Name = "btnClear";
			this.btnClear.Size = new System.Drawing.Size(106, 27);
			this.btnClear.TabIndex = 6;
			this.btnClear.Text = "Clear";
			this.btnClear.UseVisualStyleBackColor = true;
			this.btnClear.Click += new System.EventHandler(this.btnClear_Click);
			// 
			// btnCHTriangle
			// 
			this.btnCHTriangle.Location = new System.Drawing.Point(395, 102);
			this.btnCHTriangle.Name = "btnCHTriangle";
			this.btnCHTriangle.Size = new System.Drawing.Size(106, 24);
			this.btnCHTriangle.TabIndex = 7;
			this.btnCHTriangle.Text = "Ctr Horz Triangle";
			this.btnCHTriangle.UseVisualStyleBackColor = true;
			this.btnCHTriangle.Click += new System.EventHandler(this.btnCHTriangle_Click);
			// 
			// btnRectangle
			// 
			this.btnRectangle.Location = new System.Drawing.Point(395, 132);
			this.btnRectangle.Name = "btnRectangle";
			this.btnRectangle.Size = new System.Drawing.Size(106, 24);
			this.btnRectangle.TabIndex = 8;
			this.btnRectangle.Text = "Rectangle";
			this.btnRectangle.UseVisualStyleBackColor = true;
			this.btnRectangle.Click += new System.EventHandler(this.btnRectangle_Click);
			// 
			// btnWriteTags
			// 
			this.btnWriteTags.Location = new System.Drawing.Point(395, 185);
			this.btnWriteTags.Name = "btnWriteTags";
			this.btnWriteTags.Size = new System.Drawing.Size(106, 24);
			this.btnWriteTags.TabIndex = 9;
			this.btnWriteTags.Text = "Write Tags";
			this.btnWriteTags.UseVisualStyleBackColor = true;
			this.btnWriteTags.Click += new System.EventHandler(this.btnWriteTags_Click);
			// 
			// btnReadLastPts
			// 
			this.btnReadLastPts.Location = new System.Drawing.Point(395, 245);
			this.btnReadLastPts.Name = "btnReadLastPts";
			this.btnReadLastPts.Size = new System.Drawing.Size(106, 27);
			this.btnReadLastPts.TabIndex = 10;
			this.btnReadLastPts.Text = "Read Last Pts";
			this.btnReadLastPts.UseVisualStyleBackColor = true;
			this.btnReadLastPts.Click += new System.EventHandler(this.btnReadLastPts_Click);
			// 
			// btnWritePts
			// 
			this.btnWritePts.Location = new System.Drawing.Point(395, 215);
			this.btnWritePts.Name = "btnWritePts";
			this.btnWritePts.Size = new System.Drawing.Size(106, 24);
			this.btnWritePts.TabIndex = 11;
			this.btnWritePts.Text = "Write Points";
			this.btnWritePts.UseVisualStyleBackColor = true;
			this.btnWritePts.Click += new System.EventHandler(this.btnWritePts_Click);
			// 
			// sfdPoints
			// 
			this.sfdPoints.FileName = "generators.txt";
			this.sfdPoints.Filter = "Point files|*.txt";
			this.sfdPoints.Title = "Open Points File";
			// 
			// ofdPoints
			// 
			this.ofdPoints.FileName = "generators.txt";
			this.ofdPoints.Filter = "Point files|*.txt";
			this.ofdPoints.Title = "Open Points File";
			// 
			// sstMain
			// 
			this.sstMain.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
			this.toolStripStatusLabel1,
			this.sstlblGenIndex});
			this.sstMain.Location = new System.Drawing.Point(0, 376);
			this.sstMain.Name = "sstMain";
			this.sstMain.Size = new System.Drawing.Size(528, 22);
			this.sstMain.TabIndex = 12;
			this.sstMain.Text = "statusStrip1";
			// 
			// toolStripStatusLabel1
			// 
			this.toolStripStatusLabel1.Name = "toolStripStatusLabel1";
			this.toolStripStatusLabel1.Size = new System.Drawing.Size(87, 17);
			this.toolStripStatusLabel1.Text = "Generator Index";
			// 
			// sstlblGenIndex
			// 
			this.sstlblGenIndex.ForeColor = System.Drawing.Color.Blue;
			this.sstlblGenIndex.Name = "sstlblGenIndex";
			this.sstlblGenIndex.Size = new System.Drawing.Size(32, 17);
			this.sstlblGenIndex.Text = "None";
			// 
			// frmFortuneTest
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(528, 398);
			this.Controls.Add(this.sstMain);
			this.Controls.Add(this.btnWritePts);
			this.Controls.Add(this.btnReadLastPts);
			this.Controls.Add(this.btnWriteTags);
			this.Controls.Add(this.btnRectangle);
			this.Controls.Add(this.btnCHTriangle);
			this.Controls.Add(this.btnClear);
			this.Controls.Add(this.btnTraceTags);
			this.Controls.Add(this.btnCompute);
			this.Controls.Add(this.btnHTriangle);
			this.Controls.Add(this.btnVTriangle);
			this.Controls.Add(this.btnSinglePoint);
			this.Controls.Add(this.pnlDraw);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
			this.Name = "frmFortuneTest";
			this.Text = "Fortune Voronoi algorithm testbed";
			this.sstMain.ResumeLayout(false);
			this.sstMain.PerformLayout();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Panel pnlDraw;
		private System.Windows.Forms.Button btnSinglePoint;
		private System.Windows.Forms.Button btnVTriangle;
		private System.Windows.Forms.Button btnHTriangle;
		private System.Windows.Forms.Button btnCompute;
		private System.Windows.Forms.Button btnTraceTags;
		private System.Windows.Forms.Button btnClear;
		private System.Windows.Forms.Button btnCHTriangle;
		private System.Windows.Forms.Button btnRectangle;
		private System.Windows.Forms.Button btnWriteTags;
		private System.Windows.Forms.Button btnReadLastPts;
		private System.Windows.Forms.Button btnWritePts;
		private System.Windows.Forms.SaveFileDialog sfdPoints;
		private System.Windows.Forms.OpenFileDialog ofdPoints;
		private System.Windows.Forms.StatusStrip sstMain;
		private System.Windows.Forms.ToolStripStatusLabel toolStripStatusLabel1;
		private System.Windows.Forms.ToolStripStatusLabel sstlblGenIndex;
	}
}

