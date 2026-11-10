using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Taoyl.Application.TranslateDriver.Core;
using Taoyl.Application.TranslateDriver.Interfaces;
using Taoyl.Application.TranslateDriver.Storage;
using Taoyl.Application.TranslateDriver.Translation;

namespace Taoyl.Application.TranslateDriver.Management
{
    /// <summary>
    /// 翻译管理器单例类，是整个翻译引擎的核心。
    /// 负责协调资源加载、缓存、翻译策略和API暴露。
    /// </summary>
    public sealed partial class TransManager : ITranslationService, IResourceManager
    {
        #region 单例

        private static readonly Lazy<TransManager> _instance = new Lazy<TransManager>(() => new TransManager());
        public static TransManager Current => _testInstance ?? _instance.Value;

        // 测试专用：替换当前线程/全局的 TransManager 实例
        private static TransManager _testInstance;

        /// <summary>
        /// [仅限测试] 设置一个临时 TransManager 实例，替换 Current 返回值。
        /// 调用 ResetCurrent 恢复默认单例。
        /// </summary>
        internal static void SetCurrentForTesting(TransManager testManager)
        {
            _testInstance = testManager;
        }

        /// <summary>
        /// [仅限测试] 恢复默认单例。
        /// </summary>
        internal static void ResetCurrent()
        {
            _testInstance = null;
        }

        #endregion

        #region 成员变量

        private IResourceCache _cache;
        private IResourceLoader _loader;
        private readonly IControlTranslationStrategy _strategy = new ControlTranslationStrategy();
        private readonly object _loadLock = new object();

        #endregion

        #region 属性与事件

        private string _basePath = "./Language";

        /// <summary>
        /// 获取或设置存储资源文件的根目录路径。
        /// 修改后自动重建加载器，下次 Initialize/LoadResources 按新路径生效。
        /// </summary>
        public string BasePath
        {
            get { return _basePath; }
            set
            {
                _basePath = value;
                _loader = null; // 路径变更后强制重建 loader（硬伤 e）
            }
        }

        /// <summary>
        /// ToTranslate 未命中资源时是否自动注册新条目。
        /// 默认 false：读路径无副作用，翻译提取统一走 FrmResourceConfig"获取UI资源"。
        /// 需要时（如海量文本全球化场景）可手动开启。
        /// </summary>
        public bool AutoRegisterOnMiss { get; set; } = false;

        private LanguageType _currentLanguage = LanguageType.zh_CN;

        /// <summary>
        /// 获取或设置当前的显示语言。设置新语言后会自动触发全局刷新。
        /// </summary>
        public LanguageType CurrentLanguage
        {
            get => _currentLanguage;
            set
            {
                if (_currentLanguage != value)
                {
                    _currentLanguage = value;
                    OnLanguageChanged();
                }
            }
        }

        /// <summary>
        /// 当前语言发生改变时触发的事件。
        /// </summary>
        public event EventHandler<LanguageChangedEventArgs> LanguageChanged;

        /// <summary>
        /// 当一个新的类资源被添加到缓存时触发。
        /// </summary>
        public event EventHandler<ClassChangedEventArgs> ClassChanged;

        #endregion

        private TransManager() : this(new ResourceCache(), null)
        {
        }

        /// <summary>
        /// 使用指定的缓存和加载器创建实例。
        /// 如果 loader 为 null，后续需要调用 Initialize 并设置 BasePath。
        /// </summary>
        internal TransManager(IResourceCache cache, IResourceLoader loader)
        {
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
            _loader = loader;
            // 订阅缓存事件进行转发
            if (_cache is ResourceCache rc)
                rc.ClassChanged += (s, e) => ClassChanged?.Invoke(this, e);
            if (_loader == null)
                Initialize(); // 会按 BasePath 创建默认 loader
        }

        #region 初始化与资源加载

        /// <summary>
        /// 初始化翻译管理器，主要是创建资源加载器。
        /// </summary>
        public void Initialize()
        {
            if (_loader == null)
            {
                _loader = new ResourceLoader(BasePath);
            }
        }

        /// <summary>
        /// 加载所有语言资源文件到缓存中。适合在程序启动或配置界面使用。
        /// </summary>
        public void LoadResources()
        {
            if (_loader == null) Initialize();
            _loader.LoadAll(_cache);
        }

        /// <summary>
        /// 确保指定类的资源已经被加载到缓存中（线程安全的懒加载）。
        /// UI 与 Text 类型独立判断：某类型已加载不阻塞另一类型——
        /// 修复原类级 ContainsClass 判断导致 UI 先加载后 Text 文件永远不被加载、ToTranslate 未命中的问题。
        /// </summary>
        private void EnsureClassLoaded(string namespaceName, string className)
        {
            // 防御：类标识含非法路径字符（如外部导入数据带入换行/引号）时无法定位资源文件，
            // 直接跳过，防止 Path.Combine 抛异常导致界面崩溃
            if (!IsValidClassIdentifier(namespaceName) || !IsValidClassIdentifier(className)) return;

            foreach (ResourceType type in Enum.GetValues(typeof(ResourceType)))
            {
                if (_cache.ContainsClassAndType(namespaceName, className, type)) continue;

                lock (_loadLock)
                {
                    if (_cache.ContainsClassAndType(namespaceName, className, type)) continue;
                    var filePath = Path.Combine(BasePath, type.ToString(), namespaceName, $"{className}.json");
                    if (File.Exists(filePath))
                        _loader.LoadFile(filePath, _cache);
                }
            }
        }

        /// <summary>
        /// 校验类标识（命名空间/类名）是否可作为合法路径段：非空且不含非法路径字符。
        /// </summary>
        private static bool IsValidClassIdentifier(string value)
        {
            if (string.IsNullOrEmpty(value)) return false;
            return value.IndexOfAny(Path.GetInvalidPathChars()) < 0;
        }

        /// <summary>
        /// 重新加载指定类的资源，并刷新所有已打开的窗体。
        /// </summary>
        public void ReloadClass(string namespaceName, string className)
        {
            _cache.ClearClass(namespaceName, className);
            EnsureClassLoaded(namespaceName, className);
            RefreshAllOpenForms();
        }

        /// <summary>
        /// 获取内部的资源缓存接口（用于高级操作，通常不应由外部直接调用）。
        /// </summary>
        public IResourceCache GetCache() => _cache;
        #endregion

        #region 核心翻译API

        /// <summary>
        /// 翻译一个指定的窗体及其所有子控件。
        /// </summary>
        public void TranslateForm(Form form)
        {
            if (form == null) return;

            var type = form.GetType();
            var namespaceName = type.Namespace ?? "Default";
            var className = type.Name;

            try
            {
                EnsureClassLoaded(namespaceName, className);

                // 翻译窗体标题（三段式键：{类型}.{窗体类名}.Text）
                var titleEntry = _cache.Get(namespaceName, className, className + ".Text", ResourceType.UI);
                if (titleEntry != null)
                {
                    var text = titleEntry.GetText(_currentLanguage);
                    if (!string.IsNullOrEmpty(text))
                        form.Text = text;
                }

                // 递归翻译所有子控件
                TranslateControlsRecursive(form.Controls, namespaceName, className);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[TransManager] 翻译窗体失败 {className}: {ex.Message}");
            }
        }

        /// <summary>
        /// 递归地翻译控件集合。
        /// </summary>
        private void TranslateControlsRecursive(Control.ControlCollection controls, string namespaceName, string className)
        {
            foreach (Control ctrl in controls)
            {
                try
                {
                    // 子窗体（如 mainPanel 里的 UIPage，UIPage : Form）跳过：
                    // 它们作为独立窗体由 RefreshAllOpenForms 用自身类型上下文翻译，
                    // 此处用父窗体上下文翻译会查错资源且造成重复翻译
                    if (ctrl is Form)
                        continue;

                    // 统一通过策略中心获取最合适的翻译器
                    var translator = _strategy.GetTranslator(ctrl);
                    // 统一执行翻译
                    translator.Translate(ctrl, _cache, namespaceName, className, _currentLanguage);

                    // 递归处理子控件
                    if (ctrl.Controls.Count > 0)
                        TranslateControlsRecursive(ctrl.Controls, namespaceName, className);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[TransManager] 翻译控件失败 {ctrl.Name}: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// 翻译一个指定的文本资源。
        /// 注意：当资源条目不存在时返回 null，调用方需自行处理回退逻辑。
        /// </summary>
        public string TranslateText(string namespaceName, string className, string key, ResourceType resourceType = ResourceType.Text)
        {
            if (string.IsNullOrEmpty(key)) return null;
            EnsureClassLoaded(namespaceName, className);
            var entry = _cache.Get(namespaceName, className, key, resourceType);
            if (entry == null) return null;
            return entry.GetText(_currentLanguage);
        }

        #endregion

        #region 手动注册API

        /// <summary>
        /// 手动注册一个控件，为其提供定制化的翻译和提取逻辑。
        /// </summary>
        public void RegisterControl(
            Control control,
            string controlName,
            Action<Control, string> applyTranslation,
            Func<Control, string> extractText)
        {
            if (control == null) throw new ArgumentNullException(nameof(control));
            if (applyTranslation == null) throw new ArgumentNullException(nameof(applyTranslation));
            if (extractText == null) throw new ArgumentNullException(nameof(extractText));

            var hostForm = control.FindForm();
            if (hostForm == null) throw new InvalidOperationException("手动注册的控件必须已经位于一个窗体上。");

            var formType = hostForm.GetType();
            var namespaceName = formType.Namespace ?? "Default";
            var className = formType.Name;

            // 确保控件有 Name（资源提取的基石）
            if (string.IsNullOrEmpty(control.Name))
                control.Name = controlName;

            // 委托策略中心完成翻译器实例的创建与注册
            _strategy.RegisterManualTranslator(control, applyTranslation, extractText);

            // 可选：提取一次原文并创建资源占位条目，便于 UI 配置界面发现
            // 三段式键：{类型}.{控件名}.Text（手动注册控件默认属性为 Text）
            var originalText = extractText(control);
            if (!string.IsNullOrEmpty(originalText))
            {
                var entry = ResourceEntry.CreateUI(namespaceName, className, control.Name + ".Text", originalText);
                _cache.AddOrUpdate(entry);
            }
        }

        #endregion

        #region UI资源提取

        /// <summary>
        /// 从一个已打开的窗体实例中提取所有可翻译的UI资源。
        /// </summary>
        public List<ResourceEntry> ExtractUIResources(string namespaceName, string className)
        {
            var result = new List<ResourceEntry>();

            // 查找已打开的目标窗体
            Form targetForm = System.Windows.Forms.Application.OpenForms.Cast<Form>().FirstOrDefault(f => f.GetType().Namespace == namespaceName && f.GetType().Name == className);
            if (targetForm == null) return result;

            var allControls = GetAllControls(targetForm);

            // 统一通过翻译器提取资源
            foreach (var ctrl in allControls)
            {
                var translator = _strategy.GetTranslator(ctrl);
                result.AddRange(translator.ExtractEntries(ctrl, namespaceName, className));
            }

            // 单独提取窗体标题（三段式键：{类型}.{窗体类名}.Text）
            if (!string.IsNullOrEmpty(targetForm.Text))
            {
                var titleEntry = ResourceEntry.CreateUI(namespaceName, className, className + ".Text", targetForm.Text);
                result.Add(titleEntry);
            }

            return result;
        }

        /// <summary>
        /// 递归获取一个控件及其所有子孙控件的列表。
        /// </summary>
        private List<Control> GetAllControls(Control parent)
        {
            var list = new List<Control>();
            foreach (Control c in parent.Controls)
            {
                list.Add(c);
                if (c.Controls.Count > 0)
                    list.AddRange(GetAllControls(c));
            }
            return list;
        }

        #endregion

        #region 语言切换与全局刷新

        private void OnLanguageChanged()
        {
            LanguageChanged?.Invoke(this, new LanguageChangedEventArgs { Language = _currentLanguage });
            RefreshAllOpenForms();
        }

        private void RefreshAllOpenForms()
        {
            var forms = System.Windows.Forms.Application.OpenForms.Cast<Form>().ToList();
            foreach (var form in forms)
            {
                try
                {
                    if (form.InvokeRequired)
                        form.BeginInvoke(new Action(() => TranslateForm(form)));
                    else
                        TranslateForm(form);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[TransManager] 刷新窗体失败 {form.Name}: {ex.Message}");
                }
            }
        }

        #endregion

        #region 配置界面支持API

        public IEnumerable<ResourceEntry> GetClassResourcesMerged(string namespaceName, string className)
        {
            EnsureClassLoaded(namespaceName, className);
            return _cache.GetByClass(namespaceName, className);
        }

        public IEnumerable<string> GetAllClassKeys() => _cache.GetAllClassKeys();

        public bool SaveClassResources(List<ResourceEntry> entries, ResourceType type)
        {
            if (!entries.Any()) return true; // 没有东西要保存，也算成功
            var first = entries.First();
            if (!_loader.SaveClassResources(entries, type))
                return false;
            ReloadClass(first.Namespace, first.ClassName);
            return true;
        }

        public void UpdateResource(ResourceEntry entry) => _cache.AddOrUpdate(entry);

        public bool HasResources(string namespaceName, string className) => _cache.ContainsClass(namespaceName, className);

        public List<ResourceEntry> LoadEntriesFromFile(string filePath)
        {
            try
            {
                if (!File.Exists(filePath)) return new List<ResourceEntry>();
                var json = File.ReadAllText(filePath);
                return JsonConvert.DeserializeObject<List<ResourceEntry>>(json) ?? new List<ResourceEntry>();
            }
            catch
            {
                return new List<ResourceEntry>();
            }
        }

        public void AddRange(IEnumerable<ResourceEntry> entries)
        {
            _cache.AddRange(entries);
        }

        public void ClearClass(string namespaceName, string className) => _cache.ClearClass(namespaceName, className);

        #endregion

        #region 策略配置API（供外部动态调整翻译/排除类型）

        /// <summary>注册或覆盖某控件类型的精确翻译器</summary>
        public void RegisterExactTranslator(Type controlType, IControlTranslator translator)
            => _strategy.RegisterExactTranslator(controlType, translator);

        public void RegisterExactTranslator<T>(IControlTranslator translator) where T : Control
            => _strategy.RegisterExactTranslator<T>(translator);

        /// <summary>移除某控件类型的精确翻译器（恢复默认）</summary>
        public bool UnregisterExactTranslator(Type controlType)
            => _strategy.UnregisterExactTranslator(controlType);

        /// <summary>添加不需要翻译/提取的控件类型（及其子类）</summary>
        public void AddExcludedControlTypes(params Type[] types)
            => _strategy.AddExcludedTypes(types);

        /// <summary>移除某类型的排除状态（重新参与翻译）</summary>
        public bool RemoveExcludedControlType(Type controlType)
            => _strategy.RemoveExcludedType(controlType);

        /// <summary>一键添加推荐排除类型（滚动条、进度条等）</summary>
        public void UseRecommendedExclusions()
            => AddExcludedControlTypes(ControlTranslationStrategy.RecommendedExcludedTypes);

        #endregion
    }

    public class LanguageChangedEventArgs : EventArgs
    {
        public LanguageType Language { get; set; }
    }
}