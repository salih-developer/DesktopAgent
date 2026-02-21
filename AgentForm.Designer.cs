namespace DesktopAgent
{
    partial class AgentForm
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        private Panel headerPanel = null!;
        private Label sessionLabel = null!;
        private Label sessionArrowLabel = null!;
        private Button addButton = null!;
        private Panel composerPanel = null!;
        private Panel composerCard = null!;
        private TextBox messageTextBox = null!;
        private Panel composerFooterPanel = null!;
        private CheckBox askBeforeEditsCheckBox = null!;
        private Label contextFileLabel = null!;
        private Label usageLabel = null!;
        private Button attachButton = null!;
        private Button slashButton = null!;
        private Button sendButton = null!;

        /// <summary>
        ///  Clean up any resources being used.
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
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            headerPanel = new Panel();
            sessionLabel = new Label();
            sessionArrowLabel = new Label();
            addButton = new Button();
            composerPanel = new Panel();
            composerCard = new Panel();
            messageTextBox = new TextBox();
            composerFooterPanel = new Panel();
            askBeforeEditsCheckBox = new CheckBox();
            contextFileLabel = new Label();
            usageLabel = new Label();
            attachButton = new Button();
            slashButton = new Button();
            sendButton = new Button();
            rtbAgent = new RichTextBox();
            headerPanel.SuspendLayout();
            composerPanel.SuspendLayout();
            composerCard.SuspendLayout();
            composerFooterPanel.SuspendLayout();
            SuspendLayout();
            // 
            // headerPanel
            // 
            headerPanel.BackColor = Color.FromArgb(17, 21, 27);
            headerPanel.Controls.Add(sessionLabel);
            headerPanel.Controls.Add(sessionArrowLabel);
            headerPanel.Controls.Add(addButton);
            headerPanel.Dock = DockStyle.Top;
            headerPanel.Location = new Point(0, 0);
            headerPanel.Name = "headerPanel";
            headerPanel.Size = new Size(690, 46);
            headerPanel.TabIndex = 0;
            // 
            // sessionLabel
            // 
            sessionLabel.AutoSize = true;
            sessionLabel.Font = new Font("Segoe UI", 10F, FontStyle.Bold, GraphicsUnit.Point, 162);
            sessionLabel.ForeColor = Color.FromArgb(229, 233, 240);
            sessionLabel.Location = new Point(14, 13);
            sessionLabel.Name = "sessionLabel";
            sessionLabel.Size = new Size(72, 19);
            sessionLabel.TabIndex = 0;
            sessionLabel.Text = "Desktop Agent";
            // 
            // sessionArrowLabel
            // 
            sessionArrowLabel.AutoSize = true;
            sessionArrowLabel.Font = new Font("Segoe UI", 10F, FontStyle.Regular, GraphicsUnit.Point, 162);
            sessionArrowLabel.ForeColor = Color.FromArgb(161, 170, 182);
            sessionArrowLabel.Location = new Point(86, 13);
            sessionArrowLabel.Name = "sessionArrowLabel";
            sessionArrowLabel.Size = new Size(16, 19);
            sessionArrowLabel.TabIndex = 1;
            sessionArrowLabel.Text = "v";
            // 
            // addButton
            // 
            addButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            addButton.BackColor = Color.Transparent;
            addButton.Cursor = Cursors.Hand;
            addButton.FlatAppearance.BorderSize = 0;
            addButton.FlatStyle = FlatStyle.Flat;
            addButton.Font = new Font("Segoe UI", 12F, FontStyle.Regular, GraphicsUnit.Point, 162);
            addButton.ForeColor = Color.FromArgb(204, 211, 221);
            addButton.Location = new Point(646, 8);
            addButton.Name = "addButton";
            addButton.Size = new Size(32, 30);
            addButton.TabIndex = 2;
            addButton.Text = "+";
            addButton.UseVisualStyleBackColor = false;
            // 
            // composerPanel
            // 
            composerPanel.BackColor = Color.FromArgb(12, 15, 20);
            composerPanel.Controls.Add(composerCard);
            composerPanel.Dock = DockStyle.Bottom;
            composerPanel.Location = new Point(0, 803);
            composerPanel.Name = "composerPanel";
            composerPanel.Padding = new Padding(12, 6, 12, 12);
            composerPanel.Size = new Size(690, 91);
            composerPanel.TabIndex = 2;
            // 
            // composerCard
            // 
            composerCard.BackColor = Color.FromArgb(24, 29, 37);
            composerCard.Controls.Add(messageTextBox);
            composerCard.Controls.Add(composerFooterPanel);
            composerCard.Dock = DockStyle.Fill;
            composerCard.Location = new Point(12, 6);
            composerCard.Name = "composerCard";
            composerCard.Padding = new Padding(10, 9, 10, 8);
            composerCard.Size = new Size(666, 73);
            composerCard.TabIndex = 0;
            // 
            // messageTextBox
            // 
            messageTextBox.BackColor = Color.FromArgb(24, 29, 37);
            messageTextBox.BorderStyle = BorderStyle.None;
            messageTextBox.Dock = DockStyle.Top;
            messageTextBox.Font = new Font("Segoe UI", 10F, FontStyle.Regular, GraphicsUnit.Point, 162);
            messageTextBox.ForeColor = Color.FromArgb(214, 220, 229);
            messageTextBox.Location = new Point(10, 9);
            messageTextBox.Name = "messageTextBox";
            messageTextBox.PlaceholderText = "Queue another message...";
            messageTextBox.Size = new Size(646, 18);
            messageTextBox.TabIndex = 0;
            // 
            // composerFooterPanel
            // 
            composerFooterPanel.Controls.Add(askBeforeEditsCheckBox);
            composerFooterPanel.Controls.Add(contextFileLabel);
            composerFooterPanel.Controls.Add(usageLabel);
            composerFooterPanel.Controls.Add(attachButton);
            composerFooterPanel.Controls.Add(slashButton);
            composerFooterPanel.Controls.Add(sendButton);
            composerFooterPanel.Dock = DockStyle.Bottom;
            composerFooterPanel.Location = new Point(10, 37);
            composerFooterPanel.Name = "composerFooterPanel";
            composerFooterPanel.Size = new Size(646, 28);
            composerFooterPanel.TabIndex = 1;
            // 
            // askBeforeEditsCheckBox
            // 
            askBeforeEditsCheckBox.AutoSize = true;
            askBeforeEditsCheckBox.Checked = true;
            askBeforeEditsCheckBox.CheckState = CheckState.Checked;
            askBeforeEditsCheckBox.Cursor = Cursors.Hand;
            askBeforeEditsCheckBox.ForeColor = Color.FromArgb(171, 181, 195);
            askBeforeEditsCheckBox.Location = new Point(0, 4);
            askBeforeEditsCheckBox.Name = "askBeforeEditsCheckBox";
            askBeforeEditsCheckBox.Size = new Size(110, 19);
            askBeforeEditsCheckBox.TabIndex = 0;
            askBeforeEditsCheckBox.Text = "Ask before edits";
            askBeforeEditsCheckBox.UseVisualStyleBackColor = true;
            // 
            // contextFileLabel
            // 
            contextFileLabel.AutoSize = true;
            contextFileLabel.ForeColor = Color.FromArgb(120, 132, 148);
            contextFileLabel.Location = new Point(118, 5);
            contextFileLabel.Name = "contextFileLabel";
            contextFileLabel.Size = new Size(119, 15);
            contextFileLabel.TabIndex = 1;
            contextFileLabel.Text = "AccountController.cs";
            // 
            // usageLabel
            // 
            usageLabel.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            usageLabel.AutoSize = true;
            usageLabel.ForeColor = Color.FromArgb(157, 167, 180);
            usageLabel.Location = new Point(509, 5);
            usageLabel.Name = "usageLabel";
            usageLabel.Size = new Size(57, 15);
            usageLabel.TabIndex = 2;
            usageLabel.Text = "79% used";
            // 
            // attachButton
            // 
            attachButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            attachButton.Cursor = Cursors.Hand;
            attachButton.FlatAppearance.BorderSize = 0;
            attachButton.FlatStyle = FlatStyle.Flat;
            attachButton.ForeColor = Color.FromArgb(205, 212, 223);
            attachButton.Location = new Point(568, 0);
            attachButton.Name = "attachButton";
            attachButton.Size = new Size(24, 24);
            attachButton.TabIndex = 3;
            attachButton.Text = "o";
            attachButton.UseVisualStyleBackColor = true;
            // 
            // slashButton
            // 
            slashButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            slashButton.Cursor = Cursors.Hand;
            slashButton.FlatAppearance.BorderSize = 0;
            slashButton.FlatStyle = FlatStyle.Flat;
            slashButton.ForeColor = Color.FromArgb(205, 212, 223);
            slashButton.Location = new Point(593, 0);
            slashButton.Name = "slashButton";
            slashButton.Size = new Size(24, 24);
            slashButton.TabIndex = 4;
            slashButton.Text = "/";
            slashButton.UseVisualStyleBackColor = true;
            // 
            // sendButton
            // 
            sendButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            sendButton.BackColor = Color.FromArgb(210, 113, 69);
            sendButton.Cursor = Cursors.Hand;
            sendButton.FlatAppearance.BorderSize = 0;
            sendButton.FlatStyle = FlatStyle.Flat;
            sendButton.ForeColor = Color.White;
            sendButton.Location = new Point(620, 1);
            sendButton.Name = "sendButton";
            sendButton.Size = new Size(24, 24);
            sendButton.TabIndex = 5;
            sendButton.Text = "[]";
            sendButton.UseVisualStyleBackColor = false;
            sendButton.Click += sendButton_Click;
            // 
            // rtbAgent
            // 
            rtbAgent.BackColor = Color.LightGray;
            rtbAgent.BorderStyle = BorderStyle.None;
            rtbAgent.Dock = DockStyle.Fill;
            rtbAgent.Font = new Font("Segoe UI", 10F);
            rtbAgent.ForeColor = Color.FromArgb(30, 41, 59);
            rtbAgent.Location = new Point(0, 46);
            rtbAgent.Margin = new Padding(3, 2, 3, 2);
            rtbAgent.Name = "rtbAgent";
            rtbAgent.ReadOnly = true;
            rtbAgent.Size = new Size(690, 757);
            rtbAgent.TabIndex = 3;
            rtbAgent.Text = "";
            // 
            // AgentForm
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.FromArgb(12, 15, 20);
            ClientSize = new Size(690, 894);
            Controls.Add(rtbAgent);
            Controls.Add(composerPanel);
            Controls.Add(headerPanel);
            ForeColor = Color.FromArgb(226, 230, 237);
            MinimumSize = new Size(540, 740);
            Name = "AgentForm";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "Desktop Agent";
            headerPanel.ResumeLayout(false);
            headerPanel.PerformLayout();
            composerPanel.ResumeLayout(false);
            composerCard.ResumeLayout(false);
            composerCard.PerformLayout();
            composerFooterPanel.ResumeLayout(false);
            composerFooterPanel.PerformLayout();
            ResumeLayout(false);
        }

        #endregion

        private RichTextBox rtbAgent;
    }
}
