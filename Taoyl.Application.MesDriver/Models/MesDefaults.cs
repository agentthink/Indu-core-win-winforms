namespace Taoyl.Application.MesDriver.Models
{
    /// <summary>
    /// 数值默认值的唯一来源。
    ///
    /// 🔴 存在的理由：15000 / 1 / 500 这组值曾散落七处，改默认值要改七处，漏一处的症状是
    /// 「不同入口给出不同默认值」——界面上新建的接口 15 秒、手改 JSON 缺字段的接口 30 秒，
    /// 不报错、只在特定入口可见。
    ///
    /// 🔴 UI 层引用 string 字段时写 MesDefaults.TimeoutMs.ToString()：
    /// const 表达式不能调 ToString()，但实例字段初始化器允许方法调用。
    /// </summary>
    public static class MesDefaults
    {
        /// <summary>单次请求超时（毫秒）。</summary>
        public const int TimeoutMs = 15000;

        /// <summary>总尝试次数（含首次）。1 = 不重试。</summary>
        public const int MaxAttempts = 1;

        /// <summary>重试间隔（毫秒）。0 与「未配置」在 GetDelayMs 里同义，都会回落本值。</summary>
        public const int BackoffMs = 500;
    }
}
