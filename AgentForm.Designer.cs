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
        private Button settingsButton = null!;
        private Panel composerPanel = null!;
        private Panel composerCard = null!;
        private TextBox messageTextBox = null!;
        private Panel composerFooterPanel = null!;
        private CheckBox askBeforeEditsCheckBox = null!;
        private Button sendButton = null!;

        // Settings panel controls
        private Panel settingsOverlayPanel = null!;
        private Panel settingsInnerPanel = null!;
        private Label settingsTitleLabel = null!;
        private Label ollamaUrlLabel = null!;
        private TextBox ollamaUrlTextBox = null!;
        private Button fetchModelsButton = null!;
        private Label modelLabel = null!;
        private ComboBox modelComboBox = null!;
        private Label workspaceLabel = null!;
        private TextBox workspaceTextBox = null!;
        private Button browseWorkspaceButton = null!;
        private Label systemPromptLabel = null!;
        private TextBox systemPromptTextBox = null!;
        private Button saveSettingsButton = null!;
        private Button closeSettingsButton = null!;

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
            settingsButton = new Button();
            composerPanel = new Panel();
            composerCard = new Panel();
            messageTextBox = new TextBox();
            composerFooterPanel = new Panel();
            askBeforeEditsCheckBox = new CheckBox();
            sendButton = new Button();
            rtbAgent = new RichTextBox();
            settingsOverlayPanel = new Panel();
            settingsInnerPanel = new Panel();
            settingsTitleLabel = new Label();
            ollamaUrlLabel = new Label();
            ollamaUrlTextBox = new TextBox();
            fetchModelsButton = new Button();
            modelLabel = new Label();
            modelComboBox = new ComboBox();
            workspaceLabel = new Label();
            workspaceTextBox = new TextBox();
            browseWorkspaceButton = new Button();
            systemPromptLabel = new Label();
            systemPromptTextBox = new TextBox();
            saveSettingsButton = new Button();
            closeSettingsButton = new Button();
            headerPanel.SuspendLayout();
            composerPanel.SuspendLayout();
            composerCard.SuspendLayout();
            composerFooterPanel.SuspendLayout();
            settingsOverlayPanel.SuspendLayout();
            settingsInnerPanel.SuspendLayout();
            SuspendLayout();
            // 
            // headerPanel
            // 
            headerPanel.BackColor = Color.FromArgb(17, 21, 27);
            headerPanel.Controls.Add(sessionLabel);
            headerPanel.Controls.Add(sessionArrowLabel);
            headerPanel.Controls.Add(settingsButton);
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
            sessionLabel.Size = new Size(108, 19);
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
            // settingsButton
            // 
            settingsButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            settingsButton.BackColor = Color.Transparent;
            settingsButton.Cursor = Cursors.Hand;
            settingsButton.FlatAppearance.BorderSize = 0;
            settingsButton.FlatAppearance.MouseOverBackColor = Color.FromArgb(30, 36, 47);
            settingsButton.FlatStyle = FlatStyle.Flat;
            settingsButton.Font = new Font("Segoe UI", 13F, FontStyle.Regular, GraphicsUnit.Point, 162);
            settingsButton.ForeColor = Color.FromArgb(204, 211, 221);
            settingsButton.Location = new Point(646, 6);
            settingsButton.Name = "settingsButton";
            settingsButton.Size = new Size(34, 34);
            settingsButton.TabIndex = 2;
            settingsButton.Text = "⚙";
            settingsButton.UseVisualStyleBackColor = false;
            settingsButton.Click += settingsButton_Click;
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
            sendButton.Text = ">";
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
            // settingsOverlayPanel
            // 
            settingsOverlayPanel.BackColor = Color.FromArgb(180, 0, 0, 0);
            settingsOverlayPanel.Controls.Add(settingsInnerPanel);
            settingsOverlayPanel.Dock = DockStyle.Fill;
            settingsOverlayPanel.Location = new Point(0, 46);
            settingsOverlayPanel.Name = "settingsOverlayPanel";
            settingsOverlayPanel.Size = new Size(690, 757);
            settingsOverlayPanel.TabIndex = 10;
            settingsOverlayPanel.Visible = false;
            settingsOverlayPanel.Click += closeSettingsButton_Click;
            // 
            // settingsInnerPanel
            // 
            settingsInnerPanel.Anchor = AnchorStyles.None;
            settingsInnerPanel.BackColor = Color.FromArgb(22, 27, 34);
            settingsInnerPanel.Controls.Add(settingsTitleLabel);
            settingsInnerPanel.Controls.Add(ollamaUrlLabel);
            settingsInnerPanel.Controls.Add(ollamaUrlTextBox);
            settingsInnerPanel.Controls.Add(fetchModelsButton);
            settingsInnerPanel.Controls.Add(modelLabel);
            settingsInnerPanel.Controls.Add(modelComboBox);
            settingsInnerPanel.Controls.Add(workspaceLabel);
            settingsInnerPanel.Controls.Add(workspaceTextBox);
            settingsInnerPanel.Controls.Add(browseWorkspaceButton);
            settingsInnerPanel.Controls.Add(systemPromptLabel);
            settingsInnerPanel.Controls.Add(systemPromptTextBox);
            settingsInnerPanel.Controls.Add(saveSettingsButton);
            settingsInnerPanel.Controls.Add(closeSettingsButton);
            settingsInnerPanel.Location = new Point(245, 328);
            settingsInnerPanel.Name = "settingsInnerPanel";
            settingsInnerPanel.Padding = new Padding(24);
            settingsInnerPanel.Size = new Size(440, 430);
            settingsInnerPanel.TabIndex = 0;
            // 
            // settingsTitleLabel
            // 
            settingsTitleLabel.Font = new Font("Segoe UI", 13F, FontStyle.Bold);
            settingsTitleLabel.ForeColor = Color.FromArgb(229, 233, 240);
            settingsTitleLabel.Location = new Point(24, 20);
            settingsTitleLabel.Name = "settingsTitleLabel";
            settingsTitleLabel.Size = new Size(200, 28);
            settingsTitleLabel.TabIndex = 0;
            settingsTitleLabel.Text = "Settings";
            // 
            // ollamaUrlLabel
            // 
            ollamaUrlLabel.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            ollamaUrlLabel.ForeColor = Color.FromArgb(171, 181, 195);
            ollamaUrlLabel.Location = new Point(24, 60);
            ollamaUrlLabel.Name = "ollamaUrlLabel";
            ollamaUrlLabel.Size = new Size(120, 18);
            ollamaUrlLabel.TabIndex = 1;
            ollamaUrlLabel.Text = "Ollama URL";
            // 
            // ollamaUrlTextBox
            // 
            ollamaUrlTextBox.BackColor = Color.FromArgb(13, 17, 23);
            ollamaUrlTextBox.BorderStyle = BorderStyle.FixedSingle;
            ollamaUrlTextBox.Font = new Font("Segoe UI", 10F);
            ollamaUrlTextBox.ForeColor = Color.FromArgb(214, 220, 229);
            ollamaUrlTextBox.Location = new Point(24, 80);
            ollamaUrlTextBox.Name = "ollamaUrlTextBox";
            ollamaUrlTextBox.Size = new Size(300, 25);
            ollamaUrlTextBox.TabIndex = 2;
            // 
            // fetchModelsButton
            // 
            fetchModelsButton.BackColor = Color.FromArgb(36, 42, 54);
            fetchModelsButton.Cursor = Cursors.Hand;
            fetchModelsButton.FlatAppearance.BorderColor = Color.FromArgb(60, 68, 82);
            fetchModelsButton.FlatStyle = FlatStyle.Flat;
            fetchModelsButton.Font = new Font("Segoe UI", 9F);
            fetchModelsButton.ForeColor = Color.FromArgb(204, 211, 221);
            fetchModelsButton.Location = new Point(332, 79);
            fetchModelsButton.Name = "fetchModelsButton";
            fetchModelsButton.Size = new Size(82, 27);
            fetchModelsButton.TabIndex = 3;
            fetchModelsButton.Text = "List";
            fetchModelsButton.UseVisualStyleBackColor = false;
            fetchModelsButton.Click += fetchModelsButton_Click;
            // 
            // modelLabel
            // 
            modelLabel.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            modelLabel.ForeColor = Color.FromArgb(171, 181, 195);
            modelLabel.Location = new Point(24, 118);
            modelLabel.Name = "modelLabel";
            modelLabel.Size = new Size(120, 18);
            modelLabel.TabIndex = 4;
            modelLabel.Text = "Model";
            // 
            // modelComboBox
            // 
            modelComboBox.BackColor = Color.FromArgb(13, 17, 23);
            modelComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
            modelComboBox.FlatStyle = FlatStyle.Flat;
            modelComboBox.Font = new Font("Segoe UI", 10F);
            modelComboBox.ForeColor = Color.FromArgb(214, 220, 229);
            modelComboBox.Location = new Point(24, 138);
            modelComboBox.Name = "modelComboBox";
            modelComboBox.Size = new Size(390, 25);
            modelComboBox.TabIndex = 5;
            // 
            // workspaceLabel
            // 
            workspaceLabel.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            workspaceLabel.ForeColor = Color.FromArgb(171, 181, 195);
            workspaceLabel.Location = new Point(24, 178);
            workspaceLabel.Name = "workspaceLabel";
            workspaceLabel.Size = new Size(120, 18);
            workspaceLabel.TabIndex = 6;
            workspaceLabel.Text = "Workspace";
            // 
            // workspaceTextBox
            // 
            workspaceTextBox.BackColor = Color.FromArgb(13, 17, 23);
            workspaceTextBox.BorderStyle = BorderStyle.FixedSingle;
            workspaceTextBox.Font = new Font("Segoe UI", 10F);
            workspaceTextBox.ForeColor = Color.FromArgb(214, 220, 229);
            workspaceTextBox.Location = new Point(24, 198);
            workspaceTextBox.Name = "workspaceTextBox";
            workspaceTextBox.ReadOnly = true;
            workspaceTextBox.Size = new Size(300, 25);
            workspaceTextBox.TabIndex = 7;
            // 
            // browseWorkspaceButton
            // 
            browseWorkspaceButton.BackColor = Color.FromArgb(36, 42, 54);
            browseWorkspaceButton.Cursor = Cursors.Hand;
            browseWorkspaceButton.FlatAppearance.BorderColor = Color.FromArgb(60, 68, 82);
            browseWorkspaceButton.FlatStyle = FlatStyle.Flat;
            browseWorkspaceButton.Font = new Font("Segoe UI", 9F);
            browseWorkspaceButton.ForeColor = Color.FromArgb(204, 211, 221);
            browseWorkspaceButton.Location = new Point(332, 197);
            browseWorkspaceButton.Name = "browseWorkspaceButton";
            browseWorkspaceButton.Size = new Size(82, 27);
            browseWorkspaceButton.TabIndex = 8;
            browseWorkspaceButton.Text = "Lookup";
            browseWorkspaceButton.UseVisualStyleBackColor = false;
            browseWorkspaceButton.Click += browseWorkspaceButton_Click;
            // 
            // systemPromptLabel
            // 
            systemPromptLabel.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            systemPromptLabel.ForeColor = Color.FromArgb(171, 181, 195);
            systemPromptLabel.Location = new Point(24, 238);
            systemPromptLabel.Name = "systemPromptLabel";
            systemPromptLabel.Size = new Size(200, 18);
            systemPromptLabel.TabIndex = 11;
            systemPromptLabel.Text = "System Prompt";
            // 
            // systemPromptTextBox
            // 
            systemPromptTextBox.BackColor = Color.FromArgb(13, 17, 23);
            systemPromptTextBox.BorderStyle = BorderStyle.FixedSingle;
            systemPromptTextBox.Font = new Font("Segoe UI", 9F);
            systemPromptTextBox.ForeColor = Color.FromArgb(214, 220, 229);
            systemPromptTextBox.Location = new Point(24, 258);
            systemPromptTextBox.Multiline = true;
            systemPromptTextBox.Name = "systemPromptTextBox";
            systemPromptTextBox.ScrollBars = ScrollBars.Vertical;
            systemPromptTextBox.Size = new Size(390, 100);
            systemPromptTextBox.TabIndex = 12;
            // 
            // saveSettingsButton
            // 
            saveSettingsButton.BackColor = Color.FromArgb(210, 113, 69);
            saveSettingsButton.Cursor = Cursors.Hand;
            saveSettingsButton.FlatAppearance.BorderSize = 0;
            saveSettingsButton.FlatStyle = FlatStyle.Flat;
            saveSettingsButton.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            saveSettingsButton.ForeColor = Color.White;
            saveSettingsButton.Location = new Point(24, 374);
            saveSettingsButton.Name = "saveSettingsButton";
            saveSettingsButton.Size = new Size(190, 36);
            saveSettingsButton.TabIndex = 9;
            saveSettingsButton.Text = "Save";
            saveSettingsButton.UseVisualStyleBackColor = false;
            saveSettingsButton.Click += saveSettingsButton_Click;
            // 
            // closeSettingsButton
            // 
            closeSettingsButton.BackColor = Color.FromArgb(36, 42, 54);
            closeSettingsButton.Cursor = Cursors.Hand;
            closeSettingsButton.FlatAppearance.BorderColor = Color.FromArgb(60, 68, 82);
            closeSettingsButton.FlatStyle = FlatStyle.Flat;
            closeSettingsButton.Font = new Font("Segoe UI", 10F);
            closeSettingsButton.ForeColor = Color.FromArgb(204, 211, 221);
            closeSettingsButton.Location = new Point(224, 374);
            closeSettingsButton.Name = "closeSettingsButton";
            closeSettingsButton.Size = new Size(190, 36);
            closeSettingsButton.TabIndex = 10;
            closeSettingsButton.Text = "Close";
            closeSettingsButton.UseVisualStyleBackColor = false;
            closeSettingsButton.Click += closeSettingsButton_Click;
            // 
            // AgentForm
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.FromArgb(12, 15, 20);
            ClientSize = new Size(690, 894);
            Controls.Add(settingsOverlayPanel);
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
            settingsOverlayPanel.ResumeLayout(false);
            settingsInnerPanel.ResumeLayout(false);
            settingsInnerPanel.PerformLayout();
            ResumeLayout(false);
        }

        #endregion

        private RichTextBox rtbAgent;
    }
}
