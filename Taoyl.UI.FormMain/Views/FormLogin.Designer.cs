namespace Taoyl.UI.FormMain
{
    partial class FormLogin
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
        //private void InitializeComponent()
        //{
        //    this.components = new System.ComponentModel.Container();
        //    this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
        //    this.ClientSize = new System.Drawing.Size(800, 450);
        //    this.Text = "FormLogin";
        //}

        #endregion

        private Sunny.UI.UITableLayoutPanel rootTableLayoutPanel;
        private Sunny.UI.UITableLayoutPanel topTableLayoutPanel;
        private Sunny.UI.UILabel uiLabel1;
        private Sunny.UI.UITextBox txtUsername;
        private Sunny.UI.UILabel uiLabel2;
        private Sunny.UI.UITextBox txtPassword;
        private Sunny.UI.UITableLayoutPanel downTableLayoutPanel;
        private Sunny.UI.UIButton btnLogin;
        private Sunny.UI.UIButton btnCancel;
    }
}