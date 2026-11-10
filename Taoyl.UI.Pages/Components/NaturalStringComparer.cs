using System.Collections.Generic;

namespace Taoyl.UI.Pages
{
    /// <summary>
    /// 自然字符串比较器
    /// 支持 Flow1, Flow2, Flow10 的正确排序（而非字典序 Flow1, Flow10, Flow2）
    /// 工业场景下流程名称通常包含数字编号，自然排序更符合操作员直觉
    /// </summary>
    public class NaturalStringComparer : IComparer<string>
    {
        /// <summary>
        /// 比较两个字符串的自然顺序
        /// 数字段按数值比较，非数字段按字典序比较
        /// </summary>
        public int Compare(string x, string y)
        {
            if (x == null && y == null) return 0;
            if (x == null) return -1;
            if (y == null) return 1;

            int i = 0, j = 0;
            while (i < x.Length && j < y.Length)
            {
                // 当前位置都是数字：提取连续数字段进行数值比较
                if (char.IsDigit(x[i]) && char.IsDigit(y[j]))
                {
                    int startX = i, startY = j;

                    while (i < x.Length && char.IsDigit(x[i])) i++;
                    while (j < y.Length && char.IsDigit(y[j])) j++;

                    long numX = long.Parse(x.Substring(startX, i - startX));
                    long numY = long.Parse(y.Substring(startY, j - startY));

                    if (numX != numY) return numX.CompareTo(numY);
                }
                else
                {
                    // 非数字：逐字符比较（忽略大小写）
                    int cmp = char.ToLowerInvariant(x[i]).CompareTo(char.ToLowerInvariant(y[j]));
                    if (cmp != 0) return cmp;

                    i++;
                    j++;
                }
            }

            // 较短字符串排前面
            return x.Length - y.Length;
        }
    }
}