using System;
using System.Collections.Generic;
using Taoyl.Application.Configuration.Models.Params;

namespace Taoyl.Application.Configuration.Interfaces
{
    /// <summary>
    /// 泛型配方管理服务接口，提供配方列表的加载、切换、增删改、导入导出能力。
    /// </summary>
    /// <typeparam name="TRecipe">配方类型，必须继承 RecipeBase 且有无参构造函数</typeparam>
    public interface IRecipeService<TRecipe> where TRecipe : RecipeBase, new()
    {
        /// <summary>
        /// 所有已加载配方的只读列表。
        /// </summary>
        IReadOnlyList<TRecipe> Recipes { get; }

        /// <summary>
        /// 当前激活的配方引用。
        /// </summary>
        TRecipe CurrentRecipe { get; }

        /// <summary>
        /// 加载所有配方文件到内存，恢复上次激活的配方。
        /// </summary>
        void LoadAll();

        /// <summary>
        /// 切换到指定名称的配方。切换成功后触发 OnRecipeSwitched 事件。
        /// </summary>
        /// <param name="recipeName">目标配方名称</param>
        void SwitchRecipe(string recipeName);

        /// <summary>
        /// 保存指定配方到文件。采用原子写入（.bak 备份 → .tmp → 替换）。
        /// </summary>
        /// <param name="recipe">要保存的配方实例</param>
        void SaveRecipe(TRecipe recipe);

        /// <summary>
        /// 新建配方并持久化到文件。
        /// </summary>
        /// <param name="name">新配方名称，必须唯一</param>
        /// <returns>新建的配方实例</returns>
        TRecipe CreateNewRecipe(string name);

        /// <summary>
        /// 删除指定名称的配方。不允许删除当前激活的配方。
        /// </summary>
        /// <param name="recipeName">要删除的配方名称</param>
        void DeleteRecipe(string recipeName);

        /// <summary>
        /// 从外部文件导入配方。同名时弹框确认后覆盖。
        /// </summary>
        /// <param name="filePath">导入文件的完整路径</param>
        void ImportRecipe(string filePath);

        /// <summary>
        /// 导出指定配方到外部文件。
        /// </summary>
        /// <param name="recipeName">要导出的配方名称</param>
        /// <param name="filePath">目标文件路径</param>
        void ExportRecipe(string recipeName, string filePath);

        /// <summary>
        /// 配方切换事件，可能在后台线程触发，UI 订阅需 Invoke/BeginInvoke。
        /// </summary>
        event Action<TRecipe> OnRecipeSwitched;
    }
}
