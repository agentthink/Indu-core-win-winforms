using System.Collections.Generic;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Taoyl.Application.MesDriver.Models;

namespace Taoyl.UI.Pages.Models.Mes
{
    /// <summary>
    /// 右栏草稿：驱动侧 MesApiDefinition 的界面镜像。
    /// 空构造 = 新增语义，装载走 <see cref="LoadFrom"/>，收集走 <see cref="ToDefinition"/>。
    /// 无业务方法：不做校验（留 VM），不做切换搬运。
    ///
    /// 🔴 正文只有两份载体：原文一份（Json / Text / Xml 共用）+ 表格一份（Form）。
    /// 三个原文面板各有自己的文本框，但都绑同一个 BodyRawText——形态只决定驱动侧怎么处理这段文本
    /// （Content-Type、合并还是原样），不决定它存在哪，所以切形态正文不会丢。
    ///
    /// 🔴 三个数值字段（超时 / 尝试次数 / 重试间隔）用 string 承载：输入框天然是字符串，
    /// 用 int 绑定会在清空输入框时触发 string→int 转换失败。收集时 TryParse，校验在 VM 的 TryValidate。
    /// </summary>
    public partial class MesApiEditModel : ObservableObject
    {
        #region 基本信息

        [ObservableProperty]
        private string name;

        [ObservableProperty]
        private string displayName;

        [ObservableProperty]
        private MesHttpMethod method = MesHttpMethod.Post;

        [ObservableProperty]
        private string url;

        /// <summary>超时时间（毫秒）。string 承载理由见类注释；初始值取自 MesDefaults。</summary>
        [ObservableProperty]
        private string timeoutMsText = MesDefaults.TimeoutMs.ToString();

        #endregion

        #region 正文（原文一份 + 表格一份）

        /// <summary>正文形态。新建接口默认「无」（对齐 Postman none）。</summary>
        [ObservableProperty]
        private MesBodyType bodyType = MesBodyType.None;

        /// <summary>
        /// 正文原文。Json / Text / Xml 三种形态共用这一份——它们的编辑行为完全相同，
        /// 差别只在驱动侧怎么处理（合并 / 原样 / Content-Type），那是形态的事，不是字段的事。
        /// </summary>
        [ObservableProperty]
        private string bodyRawText;

        /// <summary>Form 形态的键值对（界面编辑缓冲，收集时拼成数组文本）。</summary>
        public BindingList<MesHeader> BodyFormItems { get; } = new BindingList<MesHeader>();

        #endregion

        #region 授权

        [ObservableProperty]
        private MesAuthType authType;

        [ObservableProperty]
        private string authToken;

        [ObservableProperty]
        private string authUserName;

        [ObservableProperty]
        private string authPassword;

        [ObservableProperty]
        private string authKeyName;

        [ObservableProperty]
        private string authKeyValue;

        [ObservableProperty]
        private MesKeyIn authKeyIn = MesKeyIn.Header;

        #endregion

        #region 调用参数

        /// <summary>尝试次数（含首次）。string 承载，理由同 TimeoutMsText。</summary>
        [ObservableProperty]
        private string maxAttemptsText = MesDefaults.MaxAttempts.ToString();

        /// <summary>重试间隔（毫秒）。string 承载，理由同 TimeoutMsText。</summary>
        [ObservableProperty]
        private string backoffMsText = MesDefaults.BackoffMs.ToString();

        /// <summary>是否验证 SSL 服务器证书（默认关，与 MesApiDefinition 同规则；走公网时打开）。</summary>
        [ObservableProperty]
        private bool enableCertificateValidation;

        #endregion

        /// <summary>查询参数行（编辑态可增删）。</summary>
        public BindingList<MesHeader> QueryParams { get; } = new BindingList<MesHeader>();

        /// <summary>请求头行（编辑态可增删）。</summary>
        public BindingList<MesHeader> Headers { get; } = new BindingList<MesHeader>();

        #region 构造 / 装载 / 收集

        /// <summary>空模型：各字段默认值恰好是「新增接口」语义（无正文 / Post / 15s / 1 次 / 500ms / Header）。</summary>
        public MesApiEditModel()
        {
        }

        /// <summary>
        /// 原地装载：把配置灌进本实例，不换对象——界面绑定直接持有本实例，
        /// 换实例会让所有绑定静默指向旧对象。传 null = 恢复新增语义。
        /// </summary>
        public void LoadFrom(MesApiDefinition definition)
        {
            // 驱动侧 MesApiDefinition.BodyType 默认 Json（供「按定义调用」用），
            // 界面新建接口默认 None（对齐 Postman）——两个默认值都合理，不能统一，这里显式覆盖。
            Apply(definition ?? new MesApiDefinition { BodyType = MesBodyType.None });
        }

        private void Apply(MesApiDefinition definition)
        {
            Name = definition.Name;
            DisplayName = definition.DisplayName;
            Method = definition.Method;
            Url = definition.Url;
            TimeoutMsText = definition.TimeoutMs.ToString();

            BodyType = definition.BodyType;
            BodyRawText = definition.BodyType == MesBodyType.Form ? null : definition.Body;
            FillFormItems(definition.BodyType == MesBodyType.Form ? definition.Body : null);

            MesAuthConfig auth = definition.Auth ?? new MesAuthConfig();
            AuthType = auth.Type;
            AuthToken = auth.Token;
            AuthUserName = auth.UserName;
            AuthPassword = auth.Password;
            AuthKeyName = auth.KeyName;
            AuthKeyValue = auth.KeyValue;
            AuthKeyIn = auth.KeyIn;

            MesRetryPolicy retry = definition.Retry ?? new MesRetryPolicy();
            MaxAttemptsText = retry.MaxAttempts.ToString();
            BackoffMsText = retry.BackoffMs.ToString();

            EnableCertificateValidation = definition.EnableCertificateValidation;

            FillHeaders(QueryParams, definition.QueryParams);
            FillHeaders(Headers, definition.Headers);
        }

        /// <summary>收集为驱动侧定义（保存前调用）。产出的对象与本模型再无关联。</summary>
        public MesApiDefinition ToDefinition()
        {
            return new MesApiDefinition
            {
                Name = (Name ?? string.Empty).Trim(),
                DisplayName = (DisplayName ?? string.Empty).Trim(),
                Method = Method,
                Url = (Url ?? string.Empty).Trim(),
                TimeoutMs = ParseInt(TimeoutMsText, MesDefaults.TimeoutMs),
                BodyType = BodyType,
                Body = CollectBody(),
                Auth = new MesAuthConfig
                {
                    Type = AuthType,
                    Token = AuthToken,
                    UserName = AuthUserName,
                    Password = AuthPassword,
                    KeyName = AuthKeyName,
                    KeyValue = AuthKeyValue,
                    KeyIn = AuthKeyIn
                },
                Retry = new MesRetryPolicy
                {
                    MaxAttempts = ParseInt(MaxAttemptsText, MesDefaults.MaxAttempts),
                    BackoffMs = ParseInt(BackoffMsText, MesDefaults.BackoffMs)
                },
                EnableCertificateValidation = EnableCertificateValidation,
                QueryParams = CollectHeaders(QueryParams),
                Headers = CollectHeaders(Headers)
            };
        }

        /// <summary>当前形态的正文：无正文为 null，表单拼数组文本，其余取原文。</summary>
        private string CollectBody()
        {
            if (BodyType == MesBodyType.None)
                return null;

            return BodyType == MesBodyType.Form ? GetFormBodyText() : BodyRawText;
        }

        /// <summary>列表就地重建：换整个集合实例会让 DGV 的 DataSource 失效。</summary>
        private static void FillHeaders(BindingList<MesHeader> target, List<MesHeader> source)
        {
            target.RaiseListChangedEvents = false;
            try
            {
                target.Clear();

                if (source == null)
                    return;

                foreach (MesHeader header in source)
                {
                    if (header != null)
                        target.Add(header.Clone());
                }
            }
            finally
            {
                target.RaiseListChangedEvents = true;
                target.ResetBindings();
            }
        }

        private static List<MesHeader> CollectHeaders(BindingList<MesHeader> source)
        {
            var list = new List<MesHeader>();
            foreach (MesHeader item in source)
            {
                if (item != null && !string.IsNullOrWhiteSpace(item.Name))
                    list.Add(new MesHeader(item.Name.Trim(), item.Value ?? string.Empty, item.Description));
            }
            return list;
        }

        /// <summary>
        /// 数值文本 → int。解析失败回落默认值——保存前的最终防线是 VM 的 TryValidate，
        /// 这里只防「绕过校验直接收集」的情况。
        /// </summary>
        private static int ParseInt(string text, int fallback)
        {
            int value;
            return int.TryParse((text ?? string.Empty).Trim(), out value) ? value : fallback;
        }

        #endregion

        #region Form 正文的文本视图（键值行 ↔ 数组文本）

        /// <summary>
        /// 键值行 → 数组文本。空表返回空串而不是 "[]"——空串等价「无正文」。
        /// 🔴 与驱动侧 FormBodyBuilder.ParseConfigArray 是**同一份键值语义**（跳过空名、Value 缺失当空串），
        /// 但本侧多存一个 Description：它只服务界面的按键说明，一个字节都不会进报文，
        /// 驱动侧的 JObject 里没有这个字段也不需要。改键值规则必须两边同步，编译期查不出。
        /// </summary>
        private string GetFormBodyText()
        {
            var array = new JArray();
            foreach (MesHeader item in BodyFormItems)
            {
                if (item == null || string.IsNullOrWhiteSpace(item.Name))
                    continue;

                array.Add(new JObject
                {
                    ["Name"] = item.Name.Trim(),
                    ["Value"] = item.Value ?? string.Empty,
                    ["Description"] = item.Description ?? string.Empty
                });
            }

            return array.Count == 0 ? string.Empty : array.ToString(Formatting.None);
        }

        private void FillFormItems(string bodyText)
        {
            BodyFormItems.RaiseListChangedEvents = false;
            try
            {
                BodyFormItems.Clear();

                foreach (MesHeader item in ParseFormBodyItems(bodyText))
                    BodyFormItems.Add(item);
            }
            finally
            {
                BodyFormItems.RaiseListChangedEvents = true;
                BodyFormItems.ResetBindings();
            }
        }

        /// <summary>
        /// 数组文本 → 键值行。**只认数组格式**（[{"Name":…,"Value":…,"Description":…}]）：
        /// 配置由界面自己写，没有更早的格式需要兼容。解析失败返回空表而不是抛——
        /// 界面宁可显示空表让人看见问题，驱动侧相反（宁可调用失败也不发空正文）。
        /// 🔴 非对象项**跳过**：手改 JSON 时写了字符串/数字，跳过比崩在装载路径上好。
        /// </summary>
        private static List<MesHeader> ParseFormBodyItems(string text)
        {
            var items = new List<MesHeader>();
            if (string.IsNullOrWhiteSpace(text))
                return items;

            JArray array;
            try
            {
                array = JArray.Parse(text);
            }
            catch
            {
                return items;
            }

            foreach (JToken element in array)
            {
                var obj = element as JObject;
                if (obj == null)
                    continue;

                string name = (string)obj["Name"];
                if (string.IsNullOrWhiteSpace(name))
                    continue;

                items.Add(new MesHeader(name, TokenText(obj["Value"]), TokenText(obj["Description"])));
            }

            return items;
        }

        /// <summary>JSON 值 → 文本。缺失与 null 都当空串（JValue 的 ToString 对 null 返回空串）。</summary>
        private static string TokenText(JToken token)
        {
            return token == null ? string.Empty : token.ToString();
        }

        #endregion
    }
}
