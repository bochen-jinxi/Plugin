using System;

namespace Step2Demo
{
    // ============================================================
    // 第0步：最简单的版本（一句代码）
    // ============================================================

    // --- 插件 ---
    public class SamplePlugin
    {
        public string Name => "SamplePlugin";

        public void Initialize(SamplePluginContext context)
        {
            Console.WriteLine($"  [{Name}] 初始化，Tag={context.Tag}");
        }
    }

    // --- 插件上下文 ---
    public class SamplePluginContext
    {
        public SamplePluginContext(string tag)
        {
            Tag = tag;
        }

        public string Tag { get; }
    }

    // ============================================================
    // 主程序：一句代码搞定
    // ============================================================
    public class Program
    {
        public static void Main()
        {
            Console.WriteLine("========== 第0步：最简单的版本（一句代码） ==========\n");

            var plugin = new SamplePlugin();
            var context = new SamplePluginContext("Demo-tag");
            plugin.Initialize(context);  // ← 就这一句

            Console.WriteLine();
            Console.WriteLine("=== 分析 ===");
            Console.WriteLine("一句代码：plugin.Initialize(context)");
            Console.WriteLine();
            Console.WriteLine("前提条件：");
            Console.WriteLine("  1. 只有一种插件（SamplePlugin）");
            Console.WriteLine("  2. 只有一种上下文（SamplePluginContext）");
            Console.WriteLine("  3. 永远不扩展");
            Console.WriteLine();
            Console.WriteLine("如果满足以上条件 → 一句代码就够了");
            Console.WriteLine("如果不满足 → 需要引入接口、工厂、策略等（第1步~第7步）");
        }
    }
}
