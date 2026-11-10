using System;
using System.Globalization;
using System.Threading;
using Taoyl.Application.LogDriver;
using Taoyl.Application.Product.Interfaces;
using Taoyl.Application.Product.Models.Dtos;
using Taoyl.Application.TaskProcess.Models;
using Taoyl.Application.TaskProcess.Services;

namespace Taoyl.Application.Business.Processes
{
    /// <summary>
    /// 产品落库测试流程：每轮走「插入 → 按条码补录两次」，验采集到落库到数据页展示整条链路。
    ///
    ///   步序 10 · Insert（条码 / 时间 / 工序 / 前称，其余留空）
    ///   步序 20 · 按条码取回 → 补后称与注液量 → Update
    ///   步序 30 · 再取回 → 补胶钉与最终判定 → Update
    ///   步序 40 · 回步序 10
    ///
    /// 🔴 补录前必须按条码取回完整 DTO：Insert 不回填自增 SNumber，而 Update 按主键定位，
    ///    SNumber=0 时更新 0 行且不报错（静默失效）。
    /// 🔴 只 Insert / Update，不调 DeleteByKeyName；落库失败只记日志，不进异常态。
    /// 🔴 默认禁用：测的时候在流程监控页选中 → 启用 → 启动；重启恢复禁用。
    /// </summary>
    internal class ProductTestProcess : ProcessBase
    {
        /// <summary>测试条码前缀：数据页一眼能认出演示数据</summary>
        private const string BarcodePrefix = "TEST-";

        private readonly IProductCellInfoService productService;
        private readonly Random random = new Random();

        /// <summary>本轮条码。looper 单线程推进，跨步持有安全。</summary>
        private string currentKeyName;

        /// <summary>本轮前称重量（步序 20 据此推算后称与注液量）。</summary>
        private double currentPreWeight;

        public ProductTestProcess(IProductCellInfoService productService)
        {
            this.productService = productService ?? throw new ArgumentNullException(nameof(productService));
            Category = ProcessCategory.Production;

            // 只在本类构造里关：不动基类默认值，其余流程照旧启用
            BehaviorConfig.IsEnabled = false;

            // 落库测试失败不该牵连其他流程
            BehaviorConfig.PauseOthersOnException = false;
        }

        public override void InitProcess()
        {
            ProcessName = "测试-产品落库";
            WaitTime = 1000;
        }

        /// <summary>productService 是容器单例，不归流程释放；本类无订阅。</summary>
        protected override void OnDispose() { }

        protected override ProcessStepResult ExecuteStep(int step, CancellationToken token)
        {
            switch (step)
            {
                case 10:
                    return InsertFirstStation();

                case 20:
                    return UpdateSecondStation();

                case 30:
                    return UpdateThirdStation();

                default:
                    return ProcessStepResult.Jump(10);
            }
        }

        /// <summary>步序 10：第一工位插入，只填一次能拿到的字段。</summary>
        private ProcessStepResult InsertFirstStation()
        {
            currentKeyName = BarcodePrefix + DateTime.Now.ToString("yyyyMMddHHmmssfff", CultureInfo.InvariantCulture);
            currentPreWeight = 52.0 + random.NextDouble() * 0.4 - 0.2;

            ProductCellInfoDto dto = new ProductCellInfoDto
            {
                KeyName = currentKeyName,
                EntryTime = DateTime.Now,
                ProcName = "注液",
                PreSfcNO = "SFC" + currentKeyName.Substring(BarcodePrefix.Length),
                StationNo = "ST-01",
                PreWeightNO = "PW-TEST",
                PreWeight = currentPreWeight.ToString("F2", CultureInfo.InvariantCulture),
                Result = "OK",
                BatteryType = "IFR18650-3200",
                EquNo = "ZYKJ-TEST",
                Operator = "tester",
                Shift = "演示"
            };

            try
            {
                int rows = productService.Insert(dto);
                LogHelp.Info(string.Format("[{0}] 步序[10] 插入 {1}，影响 {2} 行", ProcessName, dto.KeyName, rows));
                return rows > 0 ? ProcessStepResult.Next() : ProcessStepResult.Jump(10);
            }
            catch (Exception ex)
            {
                LogHelp.Error(ex, string.Format("[{0}] 步序[10] 插入 {1} 失败", ProcessName, dto.KeyName));
                return ProcessStepResult.Jump(10);
            }
        }

        /// <summary>步序 20：第二工位补录，后称与注液量按前称推算。</summary>
        private ProcessStepResult UpdateSecondStation()
        {
            ProductCellInfoDto dto = GetCurrentRecord(20);
            if (dto == null) return ProcessStepResult.Jump(10);

            double inject = 53.0 + random.NextDouble() * 0.2 - 0.1;

            dto.Aftersfc = dto.KeyName;
            dto.AfterSfcNO = "SFC-SCAN-02";
            dto.AfterWeightNO = "AW-TEST";
            dto.AftWeight = (currentPreWeight + inject).ToString("F2", CultureInfo.InvariantCulture);
            dto.InjectValue = inject.ToString("F3", CultureInfo.InvariantCulture);
            dto.ActualInjectValue = (inject + random.NextDouble() * 0.02 - 0.01).ToString("F3", CultureInfo.InvariantCulture);
            dto.InjectTime = (3.0 + random.NextDouble() * 0.5).ToString("F2", CultureInfo.InvariantCulture);
            dto.AftersfcResult = "OK";
            dto.AftWeightResult = "OK";

            return TryUpdate(20, dto) ? ProcessStepResult.Next() : ProcessStepResult.Jump(10);
        }

        /// <summary>步序 30：第三工位补录，胶钉与最终判定。</summary>
        private ProcessStepResult UpdateThirdStation()
        {
            ProductCellInfoDto dto = GetCurrentRecord(30);
            if (dto == null) return ProcessStepResult.Jump(10);

            dto.NailPressHeight = (112.0 + random.NextDouble() * 0.8).ToString("F2", CultureInfo.InvariantCulture);
            dto.NailPressFlatness = (0.08 + random.NextDouble() * 0.02).ToString("F3", CultureInfo.InvariantCulture);
            dto.NailPressResult = "OK";
            dto.InjectionResult = "OK";
            dto.UpdateTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);

            return TryUpdate(30, dto) ? ProcessStepResult.Next() : ProcessStepResult.Jump(10);
        }

        /// <summary>按条码取回完整记录：Update 的主键 SNumber 只能从库里带出来。取不到返回 null。</summary>
        private ProductCellInfoDto GetCurrentRecord(int step)
        {
            if (string.IsNullOrEmpty(currentKeyName))
            {
                return null;
            }

            try
            {
                ProductCellInfoDto dto = productService.GetByKeyName(currentKeyName);
                if (dto == null)
                {
                    LogHelp.Warn(string.Format("[{0}] 步序[{1}] 记录 {2} 不存在（可能被清理），重新开始一轮",
                        ProcessName, step, currentKeyName));
                }
                return dto;
            }
            catch (Exception ex)
            {
                LogHelp.Error(ex, string.Format("[{0}] 步序[{1}] 按条码 {2} 查询失败", ProcessName, step, currentKeyName));
                return null;
            }
        }

        /// <summary>按主键更新，失败只记日志。</summary>
        private bool TryUpdate(int step, ProductCellInfoDto dto)
        {
            try
            {
                int rows = productService.Update(dto);
                LogHelp.Info(string.Format("[{0}] 步序[{1}] 更新 {2}，影响 {3} 行", ProcessName, step, dto.KeyName, rows));
                return rows > 0;
            }
            catch (Exception ex)
            {
                LogHelp.Error(ex, string.Format("[{0}] 步序[{1}] 更新 {2} 失败", ProcessName, step, dto.KeyName));
                return false;
            }
        }
    }
}
