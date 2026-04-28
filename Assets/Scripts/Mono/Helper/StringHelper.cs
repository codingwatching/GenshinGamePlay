using System;

namespace TaoTie
{
    public class StringHelper
    {
        public static string SafeFormat(string format, params object[] args)
        {
            if (format == null) return null;
            if (args == null) args = Array.Empty<object>();

            int maxIndex = -1;
            bool inPlaceholder = false;
            for (int i = 0; i < format.Length; i++)
            {
                char c = format[i];
                if (!inPlaceholder && c == '{')
                {
                    // 检查是否为转义 {{
                    if (i + 1 < format.Length && format[i + 1] == '{')
                    {
                        i++; // 跳过第二个 {
                        continue;
                    }
                    inPlaceholder = true;
                    // 读索引数字
                    int start = i + 1;
                    int end = start;
                    while (end < format.Length && char.IsDigit(format[end])) end++;
                    if (end > start)
                    {
                        int index = int.Parse(format.Substring(start, end - start));
                        if (index > maxIndex) maxIndex = index;
                    }
                    i = end - 1;
                }
                else if (inPlaceholder && c == '}')
                {
                    inPlaceholder = false;
                }
            }

            if (maxIndex >= args.Length)
            {
                var newArgs = new object[maxIndex + 1];
                Array.Copy(args, newArgs, args.Length);
                args = newArgs;
            }
            return string.Format(format, args);
        }
    }
}