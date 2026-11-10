using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace Taoyl.Application.TranslateDriver.Translation
{
    /// <summary>
    /// 控件翻译策略的中央管理器。
    /// 采用三级优先级策略来为给定的控件选择最合适的翻译器。
    /// </summary>
    public class ControlTranslationStrategy : IControlTranslationStrategy
    {
        
        // 第一级策略：实例翻译器字典，拥有翻译最高优先级。
        private readonly Dictionary<Control, IControlTranslator> _instanceTranslators =
            new Dictionary<Control, IControlTranslator>();

        // 第二级策略：精确类型映射翻译器字典。
        private readonly Dictionary<Type, IControlTranslator> _exactMappings =
            new Dictionary<Type, IControlTranslator>();

        // 第三级策略：默认的通用翻译器，优先级最低。
        private readonly StandardControlTranslator _defaultTranslator = new StandardControlTranslator();

        /// <summary>排除类型集合（子类自动被排除）</summary>
        private readonly HashSet<Type> _excludedTypes = new HashSet<Type>();

        /// <summary>无操作空翻译器</summary>
        private readonly NoOpTranslator _noOpTranslator = new NoOpTranslator();


        /// <summary>
        /// 初始化策略管理器，并预注册一些通用的类型翻译器。
        /// </summary>
        public ControlTranslationStrategy()
        {
            // 注册处理复杂控件的翻译器
            _exactMappings[typeof(DataGridView)] = new DataGridViewTranslator();
            _exactMappings[typeof(TreeView)] = new TreeViewTranslator();
            _exactMappings[typeof(TabControl)] = new TabControlTranslator();
            // SunnyUI的UITabControl继承自TabControl，由上述注册的基类匹配逻辑覆盖（无需单独注册）
            _exactMappings[typeof(ListView)] = new ListViewTranslator();

            // SunnyUI UISwitch 运行时注册：UISwitch 继承 UIControl（无 WinForms 文本基类），
            // 开关两态文本 ActiveText/InactiveText 由 UISwitchTranslator 反射处理（Type.GetType 不编译期依赖）
            var uiSwitchType = Type.GetType("Sunny.UI.UISwitch, SunnyUI");
            if (uiSwitchType != null)
                _exactMappings[uiSwitchType] = new UISwitchTranslator();
        }

        /// <summary>注册或覆盖某个控件类型的精确翻译器</summary>
        public void RegisterExactTranslator(Type controlType, IControlTranslator translator)
        {
            if (controlType == null || translator == null) throw new ArgumentNullException();
            if (!typeof(Control).IsAssignableFrom(controlType))
                throw new ArgumentException("类型必须派生自 Control", nameof(controlType));
            _exactMappings[controlType] = translator;
        }

        /// <summary>泛型便捷注册</summary>
        public void RegisterExactTranslator<T>(IControlTranslator translator) where T : Control
            => RegisterExactTranslator(typeof(T), translator);

        /// <summary>移除某类型的精确翻译器，恢复默认行为</summary>
        public bool UnregisterExactTranslator(Type controlType) => _exactMappings.Remove(controlType);

        /// <summary>添加排除类型（及其所有子类）</summary>
        public void AddExcludedType(Type type)
        {
            if (typeof(Control).IsAssignableFrom(type))
                _excludedTypes.Add(type);
        }

        /// <summary>批量添加排除类型</summary>
        public void AddExcludedTypes(params Type[] types)
        {
            foreach (var t in types) AddExcludedType(t);
        }

        /// <summary>移除排除</summary>
        public bool RemoveExcludedType(Type type) => _excludedTypes.Remove(type);

        /// <summary>
        /// 推荐排除的控件类型（工控场景常不需要翻译；输入类控件 Text 可能携带用户数据，必须排除）。
        /// WinForms 类型编译期引用；SunnyUI 输入控件按类型名运行时解析
        /// （引擎不编译期依赖 SunnyUI，主程序携带 SunnyUI 时排除自动生效）。
        /// </summary>
        public static Type[] RecommendedExcludedTypes
        {
            get
            {
                var types = new List<Type>
                {
                    // 输入/数值类控件：Text 是数据不是界面文本，不参与翻译与提取（参数值保护）
                    typeof(TextBox), typeof(MaskedTextBox), typeof(NumericUpDown), typeof(DateTimePicker),
                    // 展示/交互类控件
                    typeof(RichTextBox), typeof(ProgressBar), typeof(TrackBar),
                    typeof(HScrollBar), typeof(VScrollBar), typeof(PictureBox),
                    typeof(ComboBox)
                };

                // SunnyUI 输入控件（类型名按 SunnyUI 3.9.1 dll 核对；运行时解析，缺失静默跳过）
                // 注：UISwitch 不在排除之列——开关文本 ActiveText/InactiveText 是界面文本，由 UISwitchTranslator 翻译
                AddSunnyUITypes(types,
                    "Sunny.UI.UITextBox", "Sunny.UI.UIDoubleUpDown", "Sunny.UI.UIDatePicker",
                    "Sunny.UI.UITimePicker", "Sunny.UI.UIComboDataGridView",
                    "Sunny.UI.UIComboBox", "Sunny.UI.UIRichTextBox", "Sunny.UI.UILine",
                    "Sunny.UI.UIScrollBar", "Sunny.UI.UITrackBar");

                return types.ToArray();
            }
        }

        /// <summary>
        /// 按程序集限定名解析 SunnyUI 控件类型并加入排除集合（引擎不编译期依赖 SunnyUI）。
        /// </summary>
        private static void AddSunnyUITypes(List<Type> types, params string[] typeNames)
        {
            foreach (var typeName in typeNames)
            {
                var type = Type.GetType(typeName + ", SunnyUI");
                if (type != null)
                    types.Add(type);
            }
        }

        /// <summary>
        /// 注册一个手动控件翻译器（基于委托的实例翻译器）
        /// </summary>
        public void RegisterManualTranslator(Control control, Action<Control, string> applyTranslation, Func<Control, string> extractText)
        {
            if (control == null) throw new ArgumentNullException(nameof(control));
            if (applyTranslation == null) throw new ArgumentNullException(nameof(applyTranslation));
            if (extractText == null) throw new ArgumentNullException(nameof(extractText));

            var manualTranslator = new ManualControlTranslator(applyTranslation, extractText);
            RegisterInstanceTranslator(control, manualTranslator);
        }


        /// <summary>
        /// 获取最适合指定控件的翻译器。
        /// 此方法按顺序检查三级策略：实例 -> 类型 -> 默认。
        /// </summary>
        /// <param name="control">需要翻译的控件。</param>
        /// <returns>一个最匹配的 IControlTranslator 实例。</returns>
        public IControlTranslator GetTranslator(Control control)
        { 
            if (control == null) return _defaultTranslator;

            // ---- 0级：排除检查（最高优先级） ----
            var controlType = control.GetType();
            foreach (var excludedType in _excludedTypes)
            {
                if (excludedType.IsAssignableFrom(controlType))
                    return _noOpTranslator;
            }

            // 第一级：检查是否有为该控件实例专门注册的翻译器。
            if (_instanceTranslators.TryGetValue(control, out var instanceTranslator))
            {
                return instanceTranslator;
            }


            // 第二级：检查是否有处理该控件类型的精确映射翻译器。
            if (_exactMappings.TryGetValue(controlType, out var exactTranslator))
            {
                return exactTranslator;
            }
            //检查是否继承自已映射类型（如Catl.UIButton继承Button）
            foreach (var mapping in _exactMappings)
            {
                if (mapping.Key.IsAssignableFrom(controlType))
                    return mapping.Value;
            }
            
            // 第三级：如果以上两级都未命中，则返回默认的通用翻译器。
            return _defaultTranslator;
        }

        /// <summary>
        /// 为一个特定的控件实例注册一个定制的翻译器。
        /// </summary>
        /// <param name="control">要注册的控件实例。</param>
        /// <param name="translator">为该实例定制的翻译器。</param>
        public void RegisterInstanceTranslator(Control control, IControlTranslator translator)
        {
            if (control == null || translator == null) return;

            _instanceTranslators[control] = translator;

            // 关键：订阅控件的Disposed事件，以在控件销毁时自动清理字典，防止内存泄漏。
            control.Disposed += OnInstanceControlDisposed;
        }

        /// <summary>
        /// 当通过实例注册的控件被销毁时，执行清理操作。
        /// </summary>
        private void OnInstanceControlDisposed(object sender, EventArgs e)
        {
            if (sender is Control control)
            {
                _instanceTranslators.Remove(control);
                // 清理后，取消订阅，避免重复执行
                control.Disposed -= OnInstanceControlDisposed;
            }
        }
    }
}
