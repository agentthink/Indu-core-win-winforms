using System;
using System.ComponentModel;
using Taoyl.Application.Product.Models.Entities;

namespace Taoyl.Application.Product.Models.Dtos
{
    /// <summary>
    /// 产品采集记录 DTO：服务接口 / 流程域快照 / UI 绑定共用（实体不出仓储层）。
    /// 与实体 <see cref="ProductCellInfo"/> 全字段同构（55/55，名 + 类型逐一对应），映射登记为空配置。
    /// [Description] = UI 中文列头（勿与实体的落库列注释混同——后者归 SugarColumn.ColumnDescription）。
    /// </summary>
    public class ProductCellInfoDto
    {
        [Description("序列号")]
        public int SNumber { get; set; }

        [Description("工序")]
        public string ProcName { get; set; }

        [Description("开始时间")]
        public DateTime? EntryTime { get; set; }

        [Description("电芯条码")]
        public string KeyName { get; set; }

        [Description("前扫码工位")]
        public string PreSfcNO { get; set; }

        [Description("后扫码条码")]
        public string Aftersfc { get; set; }

        [Description("后扫码工位")]
        public string AfterSfcNO { get; set; }

        [Description("一次注液前称重")]
        public string FirstWeight { get; set; }

        [Description("前称重量")]
        public string PreWeight { get; set; }

        [Description("后称重量")]
        public string AftWeight { get; set; }

        [Description("需注液量")]
        public string InjectValue { get; set; }

        [Description("实际注液量")]
        public string ActualInjectValue { get; set; }

        [Description("保有量")]
        public string QuantiytyStock { get; set; }

        [Description("注液量偏差值")]
        public string InjectSupple { get; set; }

        [Description("保有量设定值")]
        public string LiInjeRreference { get; set; }

        [Description("保有量上限")]
        public string QuantiytyStockUP { get; set; }

        [Description("保有量下限")]
        public string QuantiytyStockLow { get; set; }

        [Description("补液量")]
        public string InjectBu { get; set; }

        [Description("前称重工位")]
        public string PreWeightNO { get; set; }

        [Description("后称重工位")]
        public string AfterWeightNO { get; set; }

        [Description("注液工站号")]
        public string StationNo { get; set; }

        [Description("工位杯号")]
        public string StationCupNumber { get; set; }

        [Description("泵号")]
        public string PumpNO { get; set; }

        [Description("注液前抽真空时间")]
        public string VacuumTime { get; set; }

        [Description("高真空")]
        public string HighVacuum { get; set; }

        [Description("注液真空保持时间")]
        public string VacumKeepTime { get; set; }

        [Description("高正压")]
        public string PosPress { get; set; }

        [Description("高正压保持时间")]
        public string PosPressKeepTime { get; set; }

        [Description("吹残液压力")]
        public string DeadNightPressure { get; set; }

        [Description("吹残液时间")]
        public string DeadNightTime { get; set; }

        [Description("测漏结果")]
        public string LeakResult { get; set; }

        [Description("注液用时")]
        public string InjectTime { get; set; }

        [Description("总吸收时间")]
        public string AbsorbTime { get; set; }

        [Description("前扫码结果")]
        public string Result { get; set; }

        [Description("前称结果")]
        public string PreWeightResult { get; set; }

        [Description("后扫码结果")]
        public string AftersfcResult { get; set; }

        [Description("后称结果")]
        public string AftWeightResult { get; set; }

        [Description("条码对比结果")]
        public string CodeComparison { get; set; }

        [Description("注液结果")]
        public string InjectionResult { get; set; }

        [Description("MES进站结果")]
        public string InstiationMes { get; set; }

        [Description("MES出站结果")]
        public string OutstationMes { get; set; }

        [Description("更新时间")]
        public string UpdateTime { get; set; }

        [Description("注液开始时间")]
        public string InjectStartTime { get; set; }

        [Description("注液结束时间")]
        public string InjectEndTime { get; set; }

        [Description("胶钉压入高度")]
        public string NailPressHeight { get; set; }

        [Description("胶钉压入平整度")]
        public string NailPressFlatness { get; set; }

        [Description("倾斜修正参数")]
        public string TiltCorrection { get; set; }

        [Description("位置修正参数")]
        public string PositionCorrection { get; set; }

        [Description("胶钉检测结果")]
        public string NailPressResult { get; set; }

        [Description("复投标记")]
        public int ReFlag { get; set; }

        [Description("回流标记")]
        public int BfFlag { get; set; }

        [Description("电池型号")]
        public string BatteryType { get; set; }

        [Description("设备编号")]
        public string EquNo { get; set; }

        [Description("操作者")]
        public string Operator { get; set; }

        [Description("班次")]
        public string Shift { get; set; }
    }
}
