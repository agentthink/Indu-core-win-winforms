using System;
using System.Threading;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Taoyl.Application.LogDriver;
using Taoyl.Application.MesDriver.Interfaces;
using Taoyl.Application.MesDriver.Models;
using Taoyl.Application.TaskProcess.Models;
using Taoyl.Application.TaskProcess.Services;

namespace Taoyl.Application.Business.Processes
{
    /// <summary>
    /// MES 测试流程：只打一条接口 —— `httpbin.org-json`（POST + Json），每步只验一种正文覆盖语义，
    /// 断言从 httpbin 回显的 json 字段自动比对，不看人眼。
    ///
    ///   步序 10 · 不传           → 整份配置原样发
    ///   步序 20 · 只传课程数组    → 数组整批替换，配置里那三门课一条不留
    ///   步序 30 · 标量 + 课程     → 姓名 / 年龄由流程给，课程整批替换
    ///   步序 40 · 课程里再嵌数组  → 课程[] → 元素 → 考试[]，多层嵌套原样发出
    ///   步序 50 · 空课程         → 只能发出 []，配置那三门课不会被兜底回来
    ///
    /// 🔴 为什么不用 IF034 那种真实接口：它有 7 个顶层字段 + 三层嵌套，日志一行放不下、断言输出读不动。
    /// `课程` 这棵树同型（标量 + 数组 + 嵌套数组）但短得多，验的是同一套合并语义。
    /// 🔴 断网或 httpbin 挂掉时只记日志、不进异常态；断言自动跳过（⊘），不判失败。
    /// 🔴 默认禁用（IsEnabled = false）：测的时候在流程监控页选中 → 启用 → 启动；重启恢复禁用。
    /// </summary>
    internal class MesTestProcess : ProcessBase
    {
        private const string MergeApi = "httpbin.org-json";

        private readonly IMesClient mesClient;

        public MesTestProcess(IMesClient mesClient)
        {
            this.mesClient = mesClient ?? throw new ArgumentNullException(nameof(mesClient));
            Category = ProcessCategory.Production;

            // 只在本类构造里关：不动基类默认值，其余流程照旧启用
            BehaviorConfig.IsEnabled = false;

            // MES 不通不该牵连其他流程
            BehaviorConfig.PauseOthersOnException = false;
        }

        public override void InitProcess()
        {
            ProcessName = "测试-MES";
            WaitTime = 1500;
        }

        /// <summary>mesClient 是容器单例，不归流程释放；本类无订阅。</summary>
        protected override void OnDispose() { }

        protected override ProcessStepResult ExecuteStep(int step, CancellationToken token)
        {
            switch (step)
            {
                case 10:
                    AssertBody("10 不传（整份配置）", mesClient.Invoke(MergeApi, null, token),
                        ExpectBody(Courses(Course("数学", 90), Course("英语", 85), Course("计算机", 95))));
                    return ProcessStepResult.Next();

                case 20:
                    AssertBody("20 只传课程（整批替换）",
                        mesClient.Invoke(MergeApi, Options(new JObject
                        {
                            { "课程", Courses(Course("数学", 100)) }
                        }), token),
                        ExpectBody(Courses(Course("数学", 100))));
                    return ProcessStepResult.Next();

                case 30:
                    AssertBody("30 标量 + 课程（正确形态）",
                        mesClient.Invoke(MergeApi, Options(new JObject
                        {
                            { "姓名", "李四" },
                            { "年龄", 18 },
                            { "课程", Courses(Course("物理", 88)) }
                        }), token),
                        ExpectBody(Courses(Course("物理", 88)), "李四", 18));
                    return ProcessStepResult.Next();

                case 40:
                    AssertBody("40 课程里再嵌数组",
                        mesClient.Invoke(MergeApi, Options(new JObject
                        {
                            {
                                "课程", Courses(
                                    CourseExams("数学", 100, Exam("期中", 80), Exam("期末", 95)),
                                    Course("英语", 90))
                            }
                        }), token),
                        ExpectBody(Courses(
                            CourseExams("数学", 100, Exam("期中", 80), Exam("期末", 95)),
                            Course("英语", 90))));
                    return ProcessStepResult.Next();

                case 50:
                    AssertBody("50 空课程",
                        mesClient.Invoke(MergeApi, Options(new JObject { { "课程", Courses() } }), token),
                        ExpectBody(Courses()));
                    return ProcessStepResult.Next();

                default:
                    return ProcessStepResult.Jump(10);
            }
        }

        #region 请求与期望值构造

        private static MesCallOptions Options(JObject body)
        {
            return new MesCallOptions { Body = body };
        }

        /// <summary>
        /// 配置正文的三个顶层字段 → 期望正文。课程由调用方给，姓名 / 年龄默认取配置值。
        /// 🔴 这是**硬编码的期望**，不从配置读——从配置读就等于「用配置验证配置」，断言永远通过。
        /// </summary>
        private static JObject ExpectBody(JArray courses, string name = "张三", int age = 20)
        {
            return new JObject
            {
                { "姓名", name },
                { "年龄", age },
                { "课程", courses }
            };
        }

        private static JArray Courses(params JObject[] items)
        {
            return new JArray(items);
        }

        /// <summary>一门课。配置里的课程元素就这两个字段。</summary>
        private static JObject Course(string name, int score)
        {
            return new JObject
            {
                { "名称", name },
                { "分数", score }
            };
        }

        /// <summary>带考试数组的课程：验「数组元素里再嵌数组」也能原样发出。</summary>
        private static JObject CourseExams(string name, int score, params JObject[] exams)
        {
            return new JObject
            {
                { "名称", name },
                { "分数", score },
                { "考试", new JArray(exams) }
            };
        }

        private static JObject Exam(string type, int score)
        {
            return new JObject
            {
                { "类型", type },
                { "分数", score }
            };
        }

        #endregion

        /// <summary>
        /// 断言实际发出的正文。httpbin 把收到的 body 解析后放在响应的 `json` 字段里，
        /// 所以不用人眼去调试页逐条对照——日志里直接出 ✓ / ✗。
        /// 🔴 没拿到 200 或响应不是 JSON 时只告警、不判失败：那是外网问题，不是合并语义问题。
        /// </summary>
        private void AssertBody(string stage, MesResult result, object expected)
        {
            if (result == null)
            {
                LogHelp.Warn(string.Format("[{0}] {1} ✗ 驱动未返回结果", ProcessName, stage));
                return;
            }

            if (result.HttpStatus != 200)
            {
                LogHelp.Warn(string.Format("[{0}] {1} ⊘ 跳过断言：未拿到 200（{2}）", ProcessName, stage,
                    result.HttpStatus.HasValue
                        ? result.HttpStatus.Value.ToString()
                        : MesFailureText.Of(result.Failure)));
                return;
            }

            JToken actual;
            try
            {
                actual = JObject.Parse(result.Response)["json"];
            }
            catch (Exception ex)
            {
                LogHelp.Warn(string.Format("[{0}] {1} ⊘ 跳过断言：响应不是 JSON（{2}）", ProcessName, stage, ex.Message));
                return;
            }

            JToken expect = JToken.FromObject(expected);

            if (JToken.DeepEquals(expect, actual))
            {
                LogHelp.Info(string.Format("[{0}] {1} ✓ 实发={2}", ProcessName, stage, expect.ToString(Formatting.None)));
                return;
            }

            // 不符时两边都打出来：是「少了字段」还是「数组没整批替换」一眼可辨
            LogHelp.Warn(string.Format("[{0}] {1} ✗ 期望={2} 实际={3}", ProcessName, stage,
                expect.ToString(Formatting.None),
                actual == null ? "(响应里没有 json 字段)" : actual.ToString(Formatting.None)));
        }
    }
}
