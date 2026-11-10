using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Taoyl.Application.Configuration.Interfaces;
using Taoyl.Application.Configuration.Models.Params;
using Taoyl.Application.Configuration.Serialization;

namespace Taoyl.Application.Configuration.Services
{
    /// <summary>
    /// 泛型配方管理服务实现，负责配方的加载、切换、增删改、导入导出。
    /// 写操作加锁保证线程安全，只读操作不加锁。
    /// </summary>
    /// <typeparam name="TRecipe">配方类型</typeparam>
    public class RecipeService<TRecipe> : IRecipeService<TRecipe> where TRecipe : RecipeBase, new()
    {
        #region 构造函数

        private readonly IConfigSerializer serializer;
        private readonly object lockObj = new object();
        private readonly string recipesDir;
        private readonly string indexFile;
        private List<TRecipe> recipes = new List<TRecipe>();
        private TRecipe current;
        private readonly Dictionary<TRecipe, string> recipeFileNames = new Dictionary<TRecipe, string>();

        /// <summary>
        /// 构造配方服务实例。
        /// </summary>
        /// <param name="serializer">配置序列化器</param>
        public RecipeService(IConfigSerializer serializer)
        {
            this.serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
            string configDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config");
            recipesDir = Path.Combine(configDir, "Recipes");
            indexFile = Path.Combine(recipesDir, "RecipeIndex.json");
        }

        #endregion

        #region 公共属性与事件

        /// <inheritdoc />
        public IReadOnlyList<TRecipe> Recipes => recipes;

        /// <inheritdoc />
        public TRecipe CurrentRecipe => current;

        /// <inheritdoc />
        public event Action<TRecipe> OnRecipeSwitched;

        #endregion

        #region 加载与初始化

        /// <inheritdoc />
        public void LoadAll()
        {
            lock (lockObj)
            {
                recipeFileNames.Clear();

                // 1. 确保目录存在
                if (!Directory.Exists(recipesDir))
                {
                    Directory.CreateDirectory(recipesDir);
                    recipes = new List<TRecipe>();
                    current = null;
                    return;
                }

                // 2. 读取索引文件
                RecipeIndex index = LoadIndex();

                // 3. 遍历配方目录中的 .json 文件
                var loadedRecipes = new List<TRecipe>();
                var recipeFiles = Directory.GetFiles(recipesDir, "*.json")
                    .Where(f => !Path.GetFileName(f).Equals("RecipeIndex.json", StringComparison.OrdinalIgnoreCase))
                    .ToList();

                foreach (var file in recipeFiles)
                {
                    try
                    {
                        string json = File.ReadAllText(file);
                        TRecipe recipe = serializer.Deserialize<TRecipe>(json);
                        if (recipe != null)
                        {
                            recipe.MarkClean();
                            loadedRecipes.Add(recipe);
                            recipeFileNames[recipe] = file;
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"[RecipeService] 配方文件反序列化失败: {file}, 异常: {ex.Message}");
                    }
                }

                // 4. 一致性校验：索引列表与实际文件对齐
                if (index != null)
                {
                    List<string> validNames = loadedRecipes.Select(r => r.RecipeName).ToList();
                    List<string> indexNames = index.RecipeNames
                        .Where(name => validNames.Contains(name))
                        .ToList();
                    foreach (var recipe in loadedRecipes)
                    {
                        if (!indexNames.Contains(recipe.RecipeName))
                            indexNames.Add(recipe.RecipeName);
                    }
                    index.RecipeNames = indexNames;
                }

                recipes = loadedRecipes;

                // 5. 恢复上次激活的配方
                if (index != null && !string.IsNullOrEmpty(index.CurrentRecipeName))
                {
                    current = recipes.FirstOrDefault(r => r.RecipeName == index.CurrentRecipeName);
                }
                if (current == null && recipes.Count > 0)
                {
                    current = recipes[0];
                }
            }
        }

        #endregion

        #region 配方操作

        /// <inheritdoc />
        public void SwitchRecipe(string recipeName)
        {
            lock (lockObj)
            {
                TRecipe target = recipes.FirstOrDefault(r => r.RecipeName == recipeName);
                if (target == null)
                    throw new ArgumentException($"配方不存在: {recipeName}", nameof(recipeName));

                current = target;
                SaveIndex();

                OnRecipeSwitched?.Invoke(current);
            }
        }

        /// <inheritdoc />
        public void SaveRecipe(TRecipe recipe)
        {
            if (recipe == null)
                throw new ArgumentNullException(nameof(recipe));

            lock (lockObj)
            {
                if (recipes.Any(r => r != recipe && r.RecipeName == recipe.RecipeName))
                    throw new InvalidOperationException($"配方名称已存在: {recipe.RecipeName}");

                string filePath = GetRecipeFilePath(recipe.RecipeName);
                if (recipeFileNames.TryGetValue(recipe, out string oldPath) &&
                    !string.Equals(oldPath, filePath, StringComparison.OrdinalIgnoreCase) &&
                    File.Exists(oldPath))
                {
                    BackupAndDelete(oldPath);
                }

                string dir = Path.GetDirectoryName(filePath);
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                string json = serializer.Serialize(recipe);
                AtomicWrite(filePath, json);
                recipeFileNames[recipe] = filePath;
                recipe.MarkClean();
                SaveIndex();
            }
        }

        /// <inheritdoc />
        public TRecipe CreateNewRecipe(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("配方名称不能为空", nameof(name));
            if (name.Equals("RecipeIndex", StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("配方名称不能使用保留名 RecipeIndex", nameof(name));

            lock (lockObj)
            {
                if (recipes.Any(r => r.RecipeName == name))
                    throw new InvalidOperationException($"配方名称已存在: {name}");

                TRecipe recipe = new TRecipe();
                recipe.RecipeName = name;
                recipe.Version = "1.0";
                recipe.ModifiedTime = DateTime.Now;
                recipe.Comment = "";

                recipes.Add(recipe);
                recipeFileNames[recipe] = GetRecipeFilePath(name);
                return recipe;
            }
        }

        /// <inheritdoc />
        public void DeleteRecipe(string recipeName)
        {
            lock (lockObj)
            {
                // 防御性拒绝删除当前激活的配方
                if (current != null && current.RecipeName == recipeName)
                    throw new InvalidOperationException($"不允许删除当前激活的配方: {recipeName}");

                TRecipe target = recipes.FirstOrDefault(r => r.RecipeName == recipeName);
                if (target == null)
                    throw new ArgumentException($"配方不存在: {recipeName}", nameof(recipeName));

                string filePath = GetRecipeFilePath(recipeName);
                BackupAndDelete(filePath);
                recipeFileNames.Remove(target);

                recipes.Remove(target);
                SaveIndex();
            }
        }

        /// <inheritdoc />
        public void ImportRecipe(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentException("导入文件路径不能为空", nameof(filePath));
            if (!File.Exists(filePath))
                throw new FileNotFoundException("导入文件不存在", filePath);

            string json = File.ReadAllText(filePath);
            TRecipe imported = serializer.Deserialize<TRecipe>(json);
            if (imported == null)
                throw new InvalidOperationException("反序列化导入文件失败");

            lock (lockObj)
            {
                string destPath = GetRecipeFilePath(imported.RecipeName);

                // 检查同名
                TRecipe existing = recipes.FirstOrDefault(r => r.RecipeName == imported.RecipeName);
                if (existing != null)
                {
                    // 覆盖：更新现有配方
                    recipes.Remove(existing);
                    if (recipeFileNames.TryGetValue(existing, out string oldPath) &&
                        !string.Equals(oldPath, destPath, StringComparison.OrdinalIgnoreCase))
                        BackupAndDelete(oldPath);
                    recipeFileNames.Remove(existing);
                    imported.ModifiedTime = existing.ModifiedTime;
                }

                string destJson = serializer.Serialize(imported);
                AtomicWrite(destPath, destJson);
                imported.MarkClean();

                recipes.Add(imported);
                recipeFileNames[imported] = destPath;
                SaveIndex();
            }
        }

        /// <inheritdoc />
        public void ExportRecipe(string recipeName, string filePath)
        {
            if (string.IsNullOrWhiteSpace(recipeName))
                throw new ArgumentException("配方名称不能为空", nameof(recipeName));
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentException("导出文件路径不能为空", nameof(filePath));

            // 创建快照避免并发遍历时集合被修改
            var snapshot = recipes.ToList();
            TRecipe target = snapshot.FirstOrDefault(r => r.RecipeName == recipeName);
            if (target == null)
                throw new ArgumentException($"配方不存在: {recipeName}", nameof(recipeName));

            string json = serializer.Serialize(target);
            string dir = Path.GetDirectoryName(filePath);
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);
            File.WriteAllText(filePath, json);
        }

        #endregion

        #region 文件与索引操作

        /// <summary>
        /// 获取指定配方名称的完整文件路径。
        /// </summary>
        private string GetRecipeFilePath(string recipeName)
        {
            return Path.Combine(recipesDir, recipeName + ".json");
        }

        /// <summary>
        /// 从 RecipeIndex.json 加载索引信息。
        /// </summary>
        private RecipeIndex LoadIndex()
        {
            try
            {
                if (File.Exists(indexFile))
                {
                    string json = File.ReadAllText(indexFile);
                    return serializer.Deserialize<RecipeIndex>(json);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[RecipeService] 索引文件读取失败: {ex.Message}");
            }
            return new RecipeIndex();
        }

        /// <summary>
        /// 将当前配方列表状态保存到 RecipeIndex.json。
        /// </summary>
        private void SaveIndex()
        {
            RecipeIndex index = new RecipeIndex
            {
                CurrentRecipeName = current?.RecipeName,
                RecipeNames = recipes.Select(r => r.RecipeName).ToList()
            };

            string json = serializer.Serialize(index);
            AtomicWrite(indexFile, json);
        }

        #endregion

        #region 原子写入

        /// <summary>
        /// 原子写入文件：先备份旧文件为 .bak，再写 .tmp，
        /// 删除原文件后将 .tmp 重命名为正式文件名。
        /// </summary>
        private static void AtomicWrite(string filePath, string json, bool backup = true)
        {
            string dir = Path.GetDirectoryName(filePath);
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            // 1. 备份旧文件到 Backup/ 子目录，带时间戳
            if (backup && File.Exists(filePath))
            {
                string backupDir = Path.Combine(dir, "Backup");
                if (!Directory.Exists(backupDir))
                    Directory.CreateDirectory(backupDir);
                string backupName = Path.GetFileNameWithoutExtension(filePath)
                    + "_" + DateTime.Now.ToString("yyyyMMddHHmmss") + ".json";
                File.Copy(filePath, Path.Combine(backupDir, backupName));
            }

            // 2. 写入临时文件
            File.WriteAllText(filePath + ".tmp", json);

            // 3. 删除原文件
            if (File.Exists(filePath))
                File.Delete(filePath);

            // 4. 重命名临时文件
            File.Move(filePath + ".tmp", filePath);
        }

        /// <summary>
        /// 将指定文件备份到 Backup/ 目录后删除原文件。
        /// </summary>
        private void BackupAndDelete(string filePath)
        {
            if (!File.Exists(filePath))
                return;

            string backupDir = Path.Combine(recipesDir, "Backup");
            if (!Directory.Exists(backupDir))
                Directory.CreateDirectory(backupDir);

            string backupName = Path.GetFileNameWithoutExtension(filePath)
                + "_" + DateTime.Now.ToString("yyyyMMddHHmmss") + ".json";
            File.Copy(filePath, Path.Combine(backupDir, backupName));
            File.Delete(filePath);
        }

        #endregion
    }
}
