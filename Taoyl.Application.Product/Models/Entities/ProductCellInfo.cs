using System;
using SqlSugar;

namespace Taoyl.Application.Product.Models.Entities
{
    /// <summary>
    /// 产品采集记录（电芯注液，落库实体）。字段照抄老项目 cellinfo（55 列，列名/顺序一一对应）。
    /// 两处显式修正：EntryTime 由 string 改 DateTime?（时间筛选/排序/默认双键需要真时间语义）；
    /// 其余列按设计决策保持 string（含测量值列）——查询页对 string 列的 &lt; &lt;= &gt; &gt;= 是字典序语义。
    /// 列注释归 <see cref="SugarColumnAttribute.ColumnDescription"/>（UI 列头在 DTO 上）。
    /// 只由仓储使用，出仓储层一律转 <see cref="Dtos.ProductCellInfoDto"/>（数据访问边界死规则）。
    /// </summary>
    [SugarTable("product_cellinfo", TableDescription = "产品采集记录_电芯注液")]
    public class ProductCellInfo
    {
        [SugarColumn(ColumnName = "snumber", IsPrimaryKey = true, IsIdentity = true, ColumnDescription = "序列号")]
        public int SNumber { get; set; }

        [SugarColumn(ColumnName = "procname", Length = 50, IsNullable = true, ColumnDescription = "工序")]
        public string ProcName { get; set; }

        [SugarColumn(ColumnName = "entertime", IsNullable = true, ColumnDescription = "开始时间")]
        public DateTime? EntryTime { get; set; }

        [SugarColumn(ColumnName = "keyname", Length = 50, IsNullable = false, ColumnDescription = "电芯条码")]
        public string KeyName { get; set; }

        [SugarColumn(ColumnName = "presfcno", Length = 50, IsNullable = true, ColumnDescription = "前扫码工位")]
        public string PreSfcNO { get; set; }

        [SugarColumn(ColumnName = "aftersfc", Length = 50, IsNullable = true, ColumnDescription = "后扫码条码")]
        public string Aftersfc { get; set; }

        [SugarColumn(ColumnName = "aftersfcno", Length = 50, IsNullable = true, ColumnDescription = "后扫码工位")]
        public string AfterSfcNO { get; set; }

        [SugarColumn(ColumnName = "firstweight", Length = 50, IsNullable = true, ColumnDescription = "一次注液前称重")]
        public string FirstWeight { get; set; }

        [SugarColumn(ColumnName = "preweight", Length = 50, IsNullable = true, ColumnDescription = "前称重量")]
        public string PreWeight { get; set; }

        [SugarColumn(ColumnName = "aftweight", Length = 50, IsNullable = true, ColumnDescription = "后称重量")]
        public string AftWeight { get; set; }

        [SugarColumn(ColumnName = "injectvalue", Length = 50, IsNullable = true, ColumnDescription = "需注液量")]
        public string InjectValue { get; set; }

        [SugarColumn(ColumnName = "actualinjectvalue", Length = 50, IsNullable = true, ColumnDescription = "实际注液量")]
        public string ActualInjectValue { get; set; }

        [SugarColumn(ColumnName = "quantiytystock", Length = 50, IsNullable = true, ColumnDescription = "保有量")]
        public string QuantiytyStock { get; set; }

        [SugarColumn(ColumnName = "injectsupple", Length = 50, IsNullable = true, ColumnDescription = "注液量偏差值")]
        public string InjectSupple { get; set; }

        [SugarColumn(ColumnName = "liinjerreference", Length = 50, IsNullable = true, ColumnDescription = "保有量设定值")]
        public string LiInjeRreference { get; set; }

        [SugarColumn(ColumnName = "quantiytystockup", Length = 50, IsNullable = true, ColumnDescription = "保有量上限")]
        public string QuantiytyStockUP { get; set; }

        [SugarColumn(ColumnName = "quantiytystocklow", Length = 50, IsNullable = true, ColumnDescription = "保有量下限")]
        public string QuantiytyStockLow { get; set; }

        [SugarColumn(ColumnName = "injectbu", Length = 50, IsNullable = true, ColumnDescription = "补液量")]
        public string InjectBu { get; set; }

        [SugarColumn(ColumnName = "preweightno", Length = 50, IsNullable = true, ColumnDescription = "前称重工位")]
        public string PreWeightNO { get; set; }

        [SugarColumn(ColumnName = "afterweightno", Length = 50, IsNullable = true, ColumnDescription = "后称重工位")]
        public string AfterWeightNO { get; set; }

        [SugarColumn(ColumnName = "stationno", Length = 50, IsNullable = true, ColumnDescription = "注液工站号")]
        public string StationNo { get; set; }

        [SugarColumn(ColumnName = "stationcupnumber", Length = 50, IsNullable = true, ColumnDescription = "工位杯号")]
        public string StationCupNumber { get; set; }

        [SugarColumn(ColumnName = "pumpno", Length = 50, IsNullable = true, ColumnDescription = "泵号")]
        public string PumpNO { get; set; }

        [SugarColumn(ColumnName = "vacuumtime", Length = 50, IsNullable = true, ColumnDescription = "注液前抽真空时间")]
        public string VacuumTime { get; set; }

        [SugarColumn(ColumnName = "highvacuum", Length = 50, IsNullable = true, ColumnDescription = "高真空")]
        public string HighVacuum { get; set; }

        [SugarColumn(ColumnName = "vacumkeeptime", Length = 50, IsNullable = true, ColumnDescription = "注液真空保持时间")]
        public string VacumKeepTime { get; set; }

        [SugarColumn(ColumnName = "pospress", Length = 50, IsNullable = true, ColumnDescription = "高正压")]
        public string PosPress { get; set; }

        [SugarColumn(ColumnName = "pospresskeeptime", Length = 50, IsNullable = true, ColumnDescription = "高正压保持时间")]
        public string PosPressKeepTime { get; set; }

        [SugarColumn(ColumnName = "deadnightpressure", Length = 50, IsNullable = true, ColumnDescription = "吹残液压力")]
        public string DeadNightPressure { get; set; }

        [SugarColumn(ColumnName = "deadnighttime", Length = 50, IsNullable = true, ColumnDescription = "吹残液时间")]
        public string DeadNightTime { get; set; }

        [SugarColumn(ColumnName = "leakresult", Length = 50, IsNullable = true, ColumnDescription = "测漏结果")]
        public string LeakResult { get; set; }

        [SugarColumn(ColumnName = "injecttime", Length = 50, IsNullable = true, ColumnDescription = "注液用时")]
        public string InjectTime { get; set; }

        [SugarColumn(ColumnName = "absorbtime", Length = 50, IsNullable = true, ColumnDescription = "总吸收时间")]
        public string AbsorbTime { get; set; }

        [SugarColumn(ColumnName = "result", Length = 50, IsNullable = true, ColumnDescription = "前扫码结果")]
        public string Result { get; set; }

        [SugarColumn(ColumnName = "preweightresult", Length = 50, IsNullable = true, ColumnDescription = "前称结果")]
        public string PreWeightResult { get; set; }

        [SugarColumn(ColumnName = "aftersfcresult", Length = 50, IsNullable = true, ColumnDescription = "后扫码结果")]
        public string AftersfcResult { get; set; }

        [SugarColumn(ColumnName = "aftweightresult", Length = 50, IsNullable = true, ColumnDescription = "后称结果")]
        public string AftWeightResult { get; set; }

        [SugarColumn(ColumnName = "codecomparison", Length = 50, IsNullable = true, ColumnDescription = "条码对比结果")]
        public string CodeComparison { get; set; }

        [SugarColumn(ColumnName = "injectionresult", Length = 50, IsNullable = true, ColumnDescription = "注液结果")]
        public string InjectionResult { get; set; }

        [SugarColumn(ColumnName = "instiationmes", Length = 50, IsNullable = true, ColumnDescription = "MES进站结果")]
        public string InstiationMes { get; set; }

        [SugarColumn(ColumnName = "outstationmes", Length = 50, IsNullable = true, ColumnDescription = "MES出站结果")]
        public string OutstationMes { get; set; }

        [SugarColumn(ColumnName = "updatetime", Length = 50, IsNullable = true, ColumnDescription = "更新时间")]
        public string UpdateTime { get; set; }

        [SugarColumn(ColumnName = "injectstarttime", Length = 50, IsNullable = true, ColumnDescription = "注液开始时间")]
        public string InjectStartTime { get; set; }

        [SugarColumn(ColumnName = "injectendtime", Length = 50, IsNullable = true, ColumnDescription = "注液结束时间")]
        public string InjectEndTime { get; set; }

        [SugarColumn(ColumnName = "nailpressheight", Length = 50, IsNullable = true, ColumnDescription = "胶钉压入高度")]
        public string NailPressHeight { get; set; }

        [SugarColumn(ColumnName = "nailpressflatness", Length = 50, IsNullable = true, ColumnDescription = "胶钉压入平整度")]
        public string NailPressFlatness { get; set; }

        [SugarColumn(ColumnName = "tiltcorrection", Length = 50, IsNullable = true, ColumnDescription = "倾斜修正参数")]
        public string TiltCorrection { get; set; }

        [SugarColumn(ColumnName = "positioncorrection", Length = 50, IsNullable = true, ColumnDescription = "位置修正参数")]
        public string PositionCorrection { get; set; }

        [SugarColumn(ColumnName = "nailpressresult", Length = 50, IsNullable = true, ColumnDescription = "胶钉检测结果")]
        public string NailPressResult { get; set; }

        [SugarColumn(ColumnName = "reflag", IsNullable = false, ColumnDescription = "复投标记")]
        public int ReFlag { get; set; }

        [SugarColumn(ColumnName = "bfflag", IsNullable = false, ColumnDescription = "回流标记")]
        public int BfFlag { get; set; }

        [SugarColumn(ColumnName = "batterytype", Length = 50, IsNullable = true, ColumnDescription = "电池型号")]
        public string BatteryType { get; set; }

        [SugarColumn(ColumnName = "equno", Length = 50, IsNullable = true, ColumnDescription = "设备编号")]
        public string EquNo { get; set; }

        [SugarColumn(ColumnName = "operator", Length = 50, IsNullable = true, ColumnDescription = "操作者")]
        public string Operator { get; set; }

        [SugarColumn(ColumnName = "shift", Length = 50, IsNullable = true, ColumnDescription = "班次")]
        public string Shift { get; set; }
    }
}
