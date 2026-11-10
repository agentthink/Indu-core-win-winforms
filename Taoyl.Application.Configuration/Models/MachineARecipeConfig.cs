using System;
using System.Collections.Generic;
using Taoyl.Application.Configuration.Models.Params;

namespace Taoyl.Application.Configuration.Models
{
    /// <summary>
    /// A 机型配方���置，继承 RecipeBase 并在构造函数中定义参数骨架。
    /// 参数常量通过静态类 P 暴露，业务逻辑层引用常量保证类型安全。
    /// </summary>
    public class MachineARecipeConfig : RecipeBase
    {
        /// <summary>参数键编译期常量。</summary>
        public static class P
        {
            public const string PreWeight       = "PreWeight";
            public const string PostWeight      = "PostWeight";
            public const string InjectionVolume = "InjectionVolume";
            public const string VacuumPressure  = "VacuumPressure";
            public const string RefillVolume    = "RefillVolume";
            public const string ResistanceValue = "ResistanceValue";
        }

        public MachineARecipeConfig()
        {
            RecipeName   = "新配方";
            Version      = "1.0";
            ModifiedTime = DateTime.Now;
            Comment      = "";

            Parameters = new List<RecipeParameter>
            {
                new RecipeParameter { Key = P.PreWeight,       DisplayName = "前称重", Unit = "g",    Min = 0,   Max = 1000,  Value = 0 },
                new RecipeParameter { Key = P.PostWeight,      DisplayName = "后称重", Unit = "g",    Min = 0,   Max = 1000,  Value = 0 },
                new RecipeParameter { Key = P.InjectionVolume, DisplayName = "注液量", Unit = "ml",   Min = 0,   Max = 500,   Value = 0 },
                new RecipeParameter { Key = P.VacuumPressure,  DisplayName = "真空压力",Unit = "MPa",  Min = 0,   Max = 10,    Value = 0 },
                new RecipeParameter { Key = P.RefillVolume,    DisplayName = "补液量", Unit = "ml",   Min = 0,   Max = 200,   Value = 0 },
                new RecipeParameter { Key = P.ResistanceValue, DisplayName = "电阻值", Unit = "Ω",    Min = 0,   Max = 10000, Value = 0 },
            };
        }
    }
}
