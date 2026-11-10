using Sunny.UI;
using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using Taoyl.UI.FormMain.Helpers;
using Taoyl.UI.Pages.ViewModels;

namespace Taoyl.UI.Pages
{
    /// <summary>
    /// 文本资源扫描器（从 .cs 源码提取 ToTranslate 调用）
    ///
    /// 本窗体只保留 View 职责：文件对话框、路径输入、扫描/导出触发、结果网格展示。
    /// 扫描/导出/去重逻辑全部在 ResourceScannerViewModel。
    /// </summary>
    public partial class FrmResourceScanner : UIForm
    {
        #region ViewModel

        /// <summary>
        /// 扫描器视图模型（工具弹窗，直接自建，不走 Autofac）
        /// </summary>
        public ResourceScannerViewModel ViewModel { get; } = new ResourceScannerViewModel();

        #endregion

        #region DPI 字体（缓存复用，Dispose 释放）

        private float dgvDpiScale = 1F;

        private Font cellFont;   // 控件/单元格字体（标准 DataGridView 原生绘制，物理放大 12F × scale）
        private Font headerFont; // 列头字体（加粗）

        #endregion

        public FrmResourceScanner()
        {
            InitializeComponent();
            UiDpi.EnableDoubleBuffered(this);
            // UIForm 基类默认标题是 "UIForm"，不设会在任务管理器 / Alt+Tab 里露出来
            Text = "资源扫描";
            SetupDgv();
            BindEvents();
        }

        #region DGV 初始化（DPI 三件套 + 行号）

        private void SetupDgv()
        {
            dgvDpiScale = UiDpi.DpiScale;

            // dgv_resource 是标准 DataGridView（非 SunnyUI），列头与单元格均为原生绘制，
            // 全部走物理放大（12F × scale），不走 GlobalFontScale
            cellFont = new Font("微软雅黑", 12F * dgvDpiScale);
            headerFont = new Font("微软雅黑", 12F * dgvDpiScale, FontStyle.Bold);

            // 列保持 AutoGenerate（固定列无语言列，手动建列无收益）；DPI 三件套/列头/行号走 UiDgvHelper
            dgv_resource.AutoGenerateColumns = true;
            UiDgvHelper.Setup(dgv_resource, cellFont, headerFont, cellFont);
        }

        #endregion

        #region 事件绑定

        private void BindEvents()
        {
            btn_start.Click += async (s, e) => await ViewModel.ScanCommand.ExecuteAsync(txt_resourcePath.Text);
            btn_folder.Click += Btn_folder_Click;
            btn_CS.Click += Btn_CS_Click;
            btn_export.Click += Btn_export_Click;

            ViewModel.PropertyChanged += OnViewModelPropertyChanged;
        }

        private void OnViewModelPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(ResourceScannerViewModel.ResultTable):
                    dgv_resource.DataSource = ViewModel.ResultTable;
                    // 扫描结果归属列只读：Namespace/ClassName 由源码解析决定，不允许人工修改
                    if (dgv_resource.Columns.Contains("命名空间"))
                        dgv_resource.Columns["命名空间"].ReadOnly = true;
                    if (dgv_resource.Columns.Contains("类名"))
                        dgv_resource.Columns["类名"].ReadOnly = true;
                    break;

                case nameof(ResourceScannerViewModel.IsScanning):
                    bool scanning = ViewModel.IsScanning;
                    btn_start.Enabled = !scanning;
                    btn_folder.Enabled = !scanning;
                    btn_CS.Enabled = !scanning;
                    btn_export.Enabled = !scanning;
                    break;

                case nameof(ResourceScannerViewModel.StatusMessage):
                    if (!string.IsNullOrEmpty(ViewModel.StatusMessage))
                        UIMessageTip.Show(ViewModel.StatusMessage);
                    break;
            }
        }

        #endregion

        #region 文件对话框

        private void Btn_folder_Click(object sender, EventArgs e)
        {
            using (var fbd = new FolderBrowserDialog())
            {
                if (fbd.ShowDialog() == DialogResult.OK)
                    txt_resourcePath.Text = fbd.SelectedPath;
            }
        }

        private void Btn_CS_Click(object sender, EventArgs e)
        {
            using (var ofd = new OpenFileDialog())
            {
                ofd.Filter = "C#文件|*.cs";
                if (ofd.ShowDialog() == DialogResult.OK)
                    txt_resourcePath.Text = ofd.FileName;
            }
        }

        private void Btn_export_Click(object sender, EventArgs e)
        {
            using (var sfd = new SaveFileDialog())
            {
                sfd.Filter = "Excel文件|*.xlsx|JSON文件|*.json";
                // 预填不带扩展名：SaveFileDialog.AutoExtension 按选中过滤器自动附加（修复 JSON 导出固定 .xlsx 后缀的问题）
                sfd.FileName = ViewModel.GetSuggestedFileName();

                if (sfd.ShowDialog() != DialogResult.OK) return;

                if (sfd.FilterIndex == 1) // Excel
                    ViewModel.ExportExcelCommand.Execute(sfd.FileName);
                else // JSON
                    ViewModel.ExportJsonCommand.Execute(sfd.FileName);
            }
        }

        #endregion

        #region 资源释放

        /// <summary>
        /// 释放运行时创建的字体（Designer.cs 已有 Dispose override，此处用 OnFormClosing 释放自定义 GDI 资源）
        /// </summary>
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            cellFont?.Dispose();
            headerFont?.Dispose();
            cellFont = null;
            headerFont = null;
            base.OnFormClosing(e);
        }

        #endregion
    }
}
