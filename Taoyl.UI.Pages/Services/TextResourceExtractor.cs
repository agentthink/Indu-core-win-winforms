using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Taoyl.Application.TranslateDriver.Core;

namespace Taoyl.UI.Pages.Services
{
    /// <summary>
    /// 使用 Roslyn 从 .cs 源码中提取 ToTranslate 调用的扫描器。
    /// 原内嵌于 FrmResourceScanner，抽为独立类便于维护与测试。
    /// </summary>
    public class TextResourceExtractor
    {
        /// <summary>
        /// 从指定 .cs 文件中提取所有 ToTranslate 调用，生成文本资源条目。
        /// </summary>
        public List<ResourceEntry> ExtractFromFile(string filePath)
        {
            var entries = new List<ResourceEntry>();
            var code = File.ReadAllText(filePath);
            var tree = CSharpSyntaxTree.ParseText(code);
            var root = tree.GetRoot();

            // 获取命名空间（第一个命名空间声明）
            var namespaceDecl = root.DescendantNodes().OfType<NamespaceDeclarationSyntax>().FirstOrDefault();
            string namespaceName = namespaceDecl?.Name.ToString() ?? "Default";

            // 遍历所有类声明
            var classDecls = root.DescendantNodes().OfType<ClassDeclarationSyntax>();
            foreach (var classDecl in classDecls)
            {
                string className = classDecl.Identifier.Text;
                var invocations = classDecl.DescendantNodes().OfType<InvocationExpressionSyntax>();

                foreach (var inv in invocations)
                {
                    // 查找方法名为 "ToTranslate" 的调用
                    string methodName = null;
                    if (inv.Expression is MemberAccessExpressionSyntax memberAccess)
                        methodName = memberAccess.Name.Identifier.Text;
                    else if (inv.Expression is MemberBindingExpressionSyntax memberBinding)
                        methodName = memberBinding.Name.Identifier.Text;

                    if (methodName != "ToTranslate")
                        continue;

                    // 获取调用者字符串（fallback）
                    string fallback = GetStringLiteralValue(inv.Expression is MemberAccessExpressionSyntax ma ? ma.Expression : null);
                    if (string.IsNullOrEmpty(fallback))
                        continue;

                    var arguments = inv.ArgumentList.Arguments;
                    string targetClassName = className;
                    string key = fallback; // 默认语义键等于原文

                    // 解析重载形式
                    if (inv.Expression is MemberAccessExpressionSyntax memberExpr)
                    {
                        if (memberExpr.Name is GenericNameSyntax genericName)
                        {
                            // 泛型版本：ToTranslate<T>(this string fallback, string key, params object[] args)
                            var typeArg = genericName.TypeArgumentList.Arguments.FirstOrDefault();
                            if (typeArg != null)
                            {
                                targetClassName = typeArg.ToString();

                                // 防御：约定 ToTranslate<T> 的 T 必须是调用点所在类（Text 资源归产生者，
                                // 命名空间=源文件命名空间，两者一致才能与消费端查询路径吻合）。
                                // 写错（如 VM 里写窗体类）会导致提取产物归属与消费端查询路径不一致而永远未命中。
                                string targetSimpleName = targetClassName;
                                int lastDot = targetClassName.LastIndexOf('.');
                                if (lastDot >= 0)
                                    targetSimpleName = targetClassName.Substring(lastDot + 1);
                                if (targetSimpleName != className)
                                    System.Diagnostics.Debug.WriteLine(
                                        $"[TextResourceExtractor] {Path.GetFileName(filePath)}: ToTranslate<{targetClassName}> " +
                                        $"泛型实参与调用点类 {className} 不一致，Text 资源将归属 {namespaceName}.{targetClassName}，" +
                                        "消费端按调用点类查询会未命中。约定：T 必须等于调用点所在类。");
                            }

                            if (arguments.Count >= 1)
                                key = GetStringLiteralValue(arguments[0].Expression);
                        }
                        else
                        {
                            // 非泛型版本
                            if (arguments.Count >= 1)
                            {
                                var firstArg = arguments[0].Expression;
                                if (firstArg is ThisExpressionSyntax)
                                {
                                    // this.ToTranslate(key, args...)
                                    if (arguments.Count >= 2)
                                        key = GetStringLiteralValue(arguments[1].Expression);
                                }
                                else if (firstArg is TypeOfExpressionSyntax typeOf)
                                {
                                    // typeof(SomeType).ToTranslate(key, args...)
                                    targetClassName = typeOf.Type.ToString();
                                    if (arguments.Count >= 2)
                                        key = GetStringLiteralValue(arguments[1].Expression);
                                }
                                else
                                {
                                    // 静态调用：fallback.ToTranslate(key, args...)
                                    key = GetStringLiteralValue(firstArg);
                                }
                            }
                        }
                    }

                    if (string.IsNullOrEmpty(key))
                        key = fallback;

                    var entry = ResourceEntry.CreateText(namespaceName, targetClassName, key, fallback);
                    entries.Add(entry);
                }
            }

            return entries;
        }

        private string GetStringLiteralValue(ExpressionSyntax expr)
        {
            if (expr == null)
                return null;

            if (expr is LiteralExpressionSyntax literal && literal.Kind() == SyntaxKind.StringLiteralExpression)
            {
                return literal.Token.ValueText;
            }

            // 简单处理括号表达式：("xxx")
            if (expr is ParenthesizedExpressionSyntax paren)
                return GetStringLiteralValue(paren.Expression);

            // 暂不处理变量、方法调用等复杂情况
            return null;
        }
    }
}
