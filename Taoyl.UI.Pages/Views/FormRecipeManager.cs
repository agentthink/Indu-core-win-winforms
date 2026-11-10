using Sunny.UI;
using System;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Taoyl.Application.Configuration.Models;
using Taoyl.Application.Configuration.Models.Params;
using Taoyl.UI.FormMain.Helpers;
using Taoyl.UI.Pages.ViewModels;

namespace Taoyl.UI.Pages
{
    public partial class FormRecipeManager : UIPage
    {
        public RecipeEditVM<MachineARecipeConfig> ViewModel { get; set; }

        // ============ Dgv 列字段 ============
        private DataGridViewTextBoxColumn colRecipeKey;
        private DataGridViewTextBoxColumn colRecipeDisp;
        private DataGridViewTextBoxColumn colRecipeMin;
        private DataGridViewTextBoxColumn colRecipeMax;
        private DataGridViewTextBoxColumn colRecipeValue;
        private DataGridViewTextBoxColumn colRecipeUnit;
        private DataGridViewTextBoxColumn colRecipeComment;

        // 配方列表列
        private DataGridViewTextBoxColumn colRecipeListName;

        public FormRecipeManager() => InitializeComponent();

        // ============ 生命周期 ============
        protected override void OnLoad(EventArgs e)
        {
            if (ViewModel == null) throw new InvalidOperationException("RecipeEditVM 未注入");
            UiDpi.SetRedraw(this, false);
            base.OnLoad(e);

            // 抑制布局 + 递归双缓冲，避免首次加载时控件逐个弹出导致闪烁
            SuspendLayout();
            try
            {
                UiDpi.EnableDoubleBuffered(this);

                SetupRecipeListDgv();
                SetupRecipeParamsDgv();
                BindViewModel();
                BridgeEvents();
            }
            finally
            {
                ResumeLayout(false);
            }

            // DPI 补偿必须在 ResumeLayout(false) 之后：
            // SuspendLayout 期间 ColumnHeadersHeightSizeMode 切换被抑制，紧接着的 ColumnHeadersHeight 会被当作 AutoSize 忽略
            ApplyDgvDpiCompensation(dgvRecipes);
            ApplyDgvDpiCompensation(dgvRecipeParams);
        }
        /// <summary>
        /// 首帧显示完成（Form.Shown 每实例仅触发一次，与 OnLoad 配对）：WinForms 保证 OnShown 前
        /// 窗口定位/尺寸消息（WM_SIZE/WM_WINDOWPOSCHANGED）已处理完毕——此处恢复禁绘 + 整页重绘，
        /// 终态一次成帧。缓存复用切回（Remove→Add→Show）不重走 Load/Shown，本恢复不重复执行。
        /// </summary>
        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            UiDpi.SetRedraw(this, true);
            Invalidate(true);
        }

        // ==================== 配方 dgv 通用初始化 ====================

        private static void StyleDgv(UIDataGridView dgv, bool fullStyle = true)
        {
            dgv.AutoGenerateColumns = false;
            dgv.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgv.MultiSelect = false;
            dgv.AllowUserToAddRows = false;
            dgv.AllowUserToDeleteRows = false;
            dgv.AllowUserToResizeRows = false;
            dgv.BackgroundColor = Color.White;
            dgv.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgv.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
            {
                Font = new Font("微软雅黑", 12F, FontStyle.Bold),
                Alignment = DataGridViewContentAlignment.MiddleCenter,
                BackColor = Color.FromArgb(0, 40, 170),
                ForeColor = Color.White,
            };
            dgv.EnableHeadersVisualStyles = false;
            if (fullStyle) dgv.ScrollBars = ScrollBars.Both;
        }

        /// <summary>
        /// DGV DPI 补偿（三件套：字体/行高/列头），必须在 ResumeLayout(false) 之后调用
        /// </summary>
        private static void ApplyDgvDpiCompensation(UIDataGridView dgv)
        {
            // 系数统一 UiDpi.DpiScale（Program.cs 入口按 Graphics.DpiX 算好 GlobalFontScale，勿用 DeviceDpi——实测返回 96 不可靠）
            float dpiScale = UiDpi.DpiScale;

            // 控件 Font 保持 12F 物理：UIDataGridView 列头字体跟随控件 Font 且列头绘制走 GlobalFontScale，
            // 物理放大会双重放大（24F×200%=48F 视觉太大）；12F×200%=24F 视觉，与按钮一致
            dgv.Font = new Font("微软雅黑", 12F);
            // 单元格字体物理放大（DataGridView 原生绘制不走 GlobalFontScale，12F×scale 才是 24F 视觉）
            // Designer 显式 Font（宋体）优先于控件 Font，必须一并重建
            dgv.DefaultCellStyle.Font = new Font("微软雅黑", 12F * dpiScale);
            dgv.RowsDefaultCellStyle.Font = new Font("微软雅黑", 12F * dpiScale);

            // 行高基线 38（100% 设计值，StyleDgv 原值）
            dgv.RowTemplate.Height = (int)Math.Round(38 * dpiScale);
            // 列头高度：弃用 AutoSize（运行时改字体后不重算必切），按 DPI 手算
            dgv.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.EnableResizing;
            dgv.ColumnHeadersHeight = (int)Math.Round(36 * dpiScale);

            dgv.Invalidate();
        }

        private static DataGridViewTextBoxColumn AddDgvColumn(UIDataGridView dgv, Type modelType,
            string dataProperty, int fillWeight, bool readOnly = true,
            DataGridViewContentAlignment align = DataGridViewContentAlignment.MiddleCenter)
        {
            var prop = modelType.GetProperty(dataProperty);
            var desc = prop?.GetCustomAttributes(typeof(DescriptionAttribute), false)
                .Cast<DescriptionAttribute>().FirstOrDefault();

            var col = new DataGridViewTextBoxColumn
            {
                Name = $"{dgv.Name}Col{dataProperty}",
                HeaderText = desc?.Description ?? dataProperty,
                DataPropertyName = dataProperty,
                FillWeight = fillWeight,
                DefaultCellStyle = { Alignment = align },
                ReadOnly = readOnly,
            };
            dgv.Columns.Add(col);
            return col;
        }

        private void SetupRecipeListDgv()
        {
            StyleDgv(dgvRecipes, fullStyle: false);
            dgvRecipes.RowHeadersVisible = false;
            dgvRecipes.Columns.Clear();
            colRecipeListName = AddDgvColumn(dgvRecipes, typeof(RecipeBase),
                nameof(RecipeBase.RecipeName), 100);
        }

        private void SetupRecipeParamsDgv()
        {
            var t = typeof(RecipeParameter);
            StyleDgv(dgvRecipeParams);
            dgvRecipeParams.Columns.Clear();
            colRecipeKey   = AddDgvColumn(dgvRecipeParams, t, nameof(RecipeParameter.Key),         35);
            colRecipeDisp  = AddDgvColumn(dgvRecipeParams, t, nameof(RecipeParameter.DisplayName), 50);
            colRecipeMin   = AddDgvColumn(dgvRecipeParams, t, nameof(RecipeParameter.Min),         35);
            colRecipeMax   = AddDgvColumn(dgvRecipeParams, t, nameof(RecipeParameter.Max),         35);
            colRecipeValue = AddDgvColumn(dgvRecipeParams, t, nameof(RecipeParameter.Value),       35);
            colRecipeUnit  = AddDgvColumn(dgvRecipeParams, t, nameof(RecipeParameter.Unit),        25);
            colRecipeComment = AddDgvColumn(dgvRecipeParams, t, nameof(RecipeParameter.Comment),  50);
        }

        // ==================== 数据绑定 ====================

        private void BindViewModel()
        {
            var r = ViewModel;
            btnRecipeNew.DataBindings.Add("Enabled", r, nameof(r.CanNew));
            btnRecipeEdit.DataBindings.Add("Enabled", r, nameof(r.CanEdit));
            btnRecipeSave.DataBindings.Add("Enabled", r, nameof(r.CanSave));
            btnRecipeCancel.DataBindings.Add("Enabled", r, nameof(r.CanCancel));
            btnRecipeDelete.DataBindings.Add("Enabled", r, nameof(r.CanDelete));
            btnRecipeImport.DataBindings.Add("Enabled", r, nameof(r.CanImport));
            btnRecipeExport.DataBindings.Add("Enabled", r, nameof(r.CanExport));
            btnRecipeSwitch.DataBindings.Add("Enabled", r, nameof(r.CanSwitch));

            // 配方列表 + 参数表（一次性 DataSource 绑定，VM 自动同步内容）
            dgvRecipes.DataSource = r.RecipeList;
            dgvRecipeParams.DataSource = r.SelectedParams;

            // 当前使用配方状态
            lblCurrentRecipe.DataBindings.Add("Text", r, nameof(r.SelectedCurrentName));

            // 元数据
            txtRecipeName.DataBindings.Add("Text", r, nameof(r.SelectedName));
            txtRecipeComment.DataBindings.Add("Text", r, nameof(r.SelectedComment));
            txtRecipeModifiedBy.DataBindings.Add("Text", r, nameof(r.SelectedModifiedBy));
            txtRecipeModifiedTime.DataBindings.Add("Text", r, nameof(r.SelectedModifiedTime));

            // 元数据 ReadOnly
            txtRecipeName.DataBindings.Add("ReadOnly", r, nameof(r.IsBrowsing));
            txtRecipeComment.DataBindings.Add("ReadOnly", r, nameof(r.IsBrowsing));
            txtRecipeModifiedBy.DataBindings.Add("ReadOnly", r, nameof(r.IsBrowsing));
            txtRecipeModifiedTime.DataBindings.Add("ReadOnly", r, nameof(r.IsBrowsing));
        }

        // ==================== 事件桥接 ====================

        private void BridgeEvents()
        {
            var r = ViewModel;
            btnRecipeNew.Click    += (s, e) => r.NewCommand.Execute(null);
            btnRecipeEdit.Click   += (s, e) => r.EditCommand.Execute(null);
            btnRecipeSave.Click   += (s, e) => r.SaveCommand.Execute(null);
            btnRecipeCancel.Click += (s, e) => r.CancelCommand.Execute(null);
            btnRecipeDelete.Click += (s, e) => r.DeleteCommand.Execute(null);
            btnRecipeSwitch.Click += (s, e) => r.SwitchCommand.Execute(null);

            btnRecipeImport.Click += (s, e) =>
            {
                using (var dlg = new OpenFileDialog { Title = "导入配方", Filter = "JSON|*.json" })
                    if (dlg.ShowDialog() == DialogResult.OK) r.ImportCommand.Execute(dlg.FileName);
            };
            btnRecipeExport.Click += (s, e) =>
            {
                if (r.SelectedRecipe == null) return;
                using (var dlg = new SaveFileDialog { Title = "导出配方", Filter = "JSON|*.json",
                       FileName = r.SelectedRecipe.RecipeName + ".json" })
                    if (dlg.ShowDialog() == DialogResult.OK) r.ExportCommand.Execute(dlg.FileName);
            };

            dgvRecipes.CellClick += (s, e) =>
            {
                if (e.RowIndex < 0 || e.RowIndex >= r.RecipeList.Count) return;
                r.SelectedRecipe = r.RecipeList[e.RowIndex];
            };

            // VM 状态订阅
            r.SelectionChanged += () =>
            {
                if (r.SelectedRecipe == null)
                {
                    dgvRecipes.ClearSelection();
                    return;
                }
                int idx = r.RecipeList.IndexOf(r.SelectedRecipe);
                if (idx < 0) return;
                dgvRecipes.ClearSelection();
                dgvRecipes.Rows[idx].Selected = true;
                dgvRecipes.CurrentCell = dgvRecipes.Rows[idx].Cells[0];
            };
            r.StateOrListChanged += () =>
            {
                bool e = r.IsEditing || r.IsCreating;
                colRecipeDisp.ReadOnly    = !e;
                colRecipeUnit.ReadOnly    = !e;
                colRecipeComment.ReadOnly = !e;
                colRecipeMin.ReadOnly     = !e;
                colRecipeMax.ReadOnly     = !e;
                colRecipeValue.ReadOnly   = !e;
            };

            // 初始同步：DataGridView 渲染未就绪时延迟到 DataBindingComplete
            dgvRecipes.DataBindingComplete += (s, e) =>
            {
                if (r.SelectedRecipe == null)
                {
                    dgvRecipes.ClearSelection();
                    return;
                }
                int idx = r.RecipeList.IndexOf(r.SelectedRecipe);
                if (idx < 0) return;
                dgvRecipes.ClearSelection();
                dgvRecipes.Rows[idx].Selected = true;
                dgvRecipes.CurrentCell = dgvRecipes.Rows[idx].Cells[0];
            };
        }

        // ==================== 辅助 ====================
    }
}
