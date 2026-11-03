using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Http.Headers;

namespace Step2_6Demo
{
    // ============================================================
    // 第6步：逆变（in TContext），通用插件适配多种上下文
    // ============================================================
    //
    // 【与第5步的对比】
    //
    // 第5步（Step2.5Demo）：IPlugin<TContext>（无型变）
    //   → IPlugin<LogContext> 和 IPlugin<DbContext> 互不相关
    //   → 一个"通用插件"没法适配多种上下文
    //
    // 第6步（Step2.6Demo）：IPlugin<in TContext>（逆变）
    //   → IPlugin<IPluginContext> 可以赋给 IPlugin<LogContext>
    //   → 一个"面向泛化上下文的插件"可以当作"面向具体上下文的插件"用
    //
    // 【逆变的含义】
    //   正常：子类型关系是"顺"的（LogContext 是 IPluginContext 的子）
    //   逆变：IPlugin 的类型参数关系"逆"过来
    //     IPlugin<IPluginContext> → IPlugin<LogContext>
    //     （父类型插件 → 子类型插件）
    //
    // 【逆变的前提】
    //   in TContext 要求 TContext 只出现在"输入位置"（方法参数）
    //   void Initialize(TContext context);  ← TContext 是输入，OK
    //   不能有返回 TContext 的方法（输出位置）
    //
    // 【逆变的实际价值】
    //   写一个"通用插件"（处理任意 IPluginContext）
    //   → 它可以被当作"LogPlugin"用，也可以被当作"DbPlugin"用
    //   → 一处实现，多处适配
    // ============================================================


    // ========== 值类型：上下文描述符 ==========

    public readonly struct ContextDescriptor
    {
        public string Source { get; }
        public string Detail { get; }

        public ContextDescriptor(string source, string detail)
        {
            Source = source;
            Detail = detail;
        }

        public override string ToString() => $"Source={Source}, Detail={Detail}";
    }


    // ========== 接口定义 ==========

    public interface IPluginContext
    {
        ContextDescriptor Descriptor { get; }
    }

    public interface IPlugin
    {
        string Name { get; }
        void Initialize(IPluginContext context);
    }

    // ★ 第6步核心：加 in 逆变
    //   in TContext 表示 TContext 只用于"输入"（方法参数）
    //   → 允许 IPlugin<父> 赋给 IPlugin<子>
    public interface IPlugin<in TContext> : IPlugin
        where TContext : IPluginContext
    {
        void Initialize(TContext context);   // TContext 在输入位置 → 允许逆变
    }


    // ========== 上下文实现 ==========

    public sealed class LogContext : IPluginContext
    {
       
        public LogContext(string logPath, string logLevel)
        {
            Descriptor = new ContextDescriptor(logPath, logLevel);
        }
        public ContextDescriptor Descriptor { get; }
    }

    public sealed class DbContext : IPluginContext
    {
        public DbContext(string connectionString, int timeoutSeconds)
        {
            Descriptor = new ContextDescriptor(connectionString, timeoutSeconds.ToString());
            TimeoutSeconds = timeoutSeconds;
        }
        public ContextDescriptor Descriptor { get; }
        public int TimeoutSeconds { get; }
    }

    public sealed class CacheContext : IPluginContext
    {
        public CacheContext(string redisUrl, int expirationSeconds)
        {
            Descriptor = new ContextDescriptor(redisUrl, expirationSeconds.ToString());
            ExpirationSeconds = expirationSeconds;
        }
        public ContextDescriptor Descriptor { get; }
        public int ExpirationSeconds { get; }
    }


    // ========== 泛型抽象基类（不变）==========

    public abstract class PluginBase<TContext> : IPlugin<TContext>
        where TContext : IPluginContext
    {
        public abstract string Name { get; }

        protected abstract void OnInitialize(TContext context);

        public void Initialize(TContext context)
        {
            Console.WriteLine($"  [{Name}] Initialize(TContext)，TContext = {typeof(TContext).Name}");
            OnInitialize(context);
        }

        void IPlugin.Initialize(IPluginContext context)
        {
           
            if (!(context is TContext typedContext))
            {
                throw new ArgumentException(
                    $"上下文类型不匹配。期望 {typeof(TContext).Name}，实际 {context.GetType().Name}。",
                    nameof(context));
            }
            Initialize(typedContext);
        }
    }


    // ========== 具体插件（不变）==========

    public class LogPlugin : PluginBase<LogContext>
    {
        public override string Name => "LogPlugin";
        protected override void OnInitialize(LogContext context)
        {
            Console.WriteLine($"    路径={context.Descriptor.Source}，级别={context.Descriptor.Detail}");
        }
    }

    public class DbPlugin : PluginBase<DbContext>
    {
        public override string Name => "DbPlugin";
        protected override void OnInitialize(DbContext context)
        {
            Console.WriteLine($"    连接={context.Descriptor.Source}，超时={context.TimeoutSeconds}s");
        }
    }

    public class CachePlugin : PluginBase<CacheContext>
    {
        public override string Name => "CachePlugin";
        protected override void OnInitialize(CacheContext context)
        {
            Console.WriteLine($"    Redis={context.Descriptor.Source}，过期={context.ExpirationSeconds}s");
        }
    }


    // ========== ★ 新增：通用上下文插件 ==========
    //
    // 面向"泛化的 IPluginContext"，能处理任意上下文
    // 通过逆变，它可以被当作 LogPlugin / DbPlugin / CachePlugin 用

    public class UniversalContextPlugin : PluginBase<IPluginContext>
    {
        public override string Name => "UniversalContextPlugin";

        protected override void OnInitialize(IPluginContext context)
        {
            Console.WriteLine($"    处理任意上下文：{context.Descriptor}");
        }
    }


    // ========== 主程序 ==========

    public class Program
    {
        public static void Main()
        {
            Console.WriteLine("========== 第6步：逆变（in TContext），通用插件适配多种上下文 ==========\n");

            // --- Demo A：逆变赋值 ---
            Console.WriteLine("【Demo A】逆变赋值 IPlugin<IPluginContext> → IPlugin<LogContext>\n");
            IPlugin<IPluginContext> universal = new UniversalContextPlugin();
            IPlugin<LogContext> asLogPlugin = universal;   // ★ 逆变赋值
            asLogPlugin.Initialize(new LogContext("/var/log/app.log", "Info"));
            Console.WriteLine("  → (in TContext) 让 IPlugin<IPluginContext> 能赋给 IPlugin<LogContext>");

            // --- Demo B：一个通用插件适配多种上下文 ---
            Console.WriteLine("\n【Demo B】一个通用插件，适配多种上下文\n");
            IPlugin<LogContext> forLog = universal;      // 逆变到 LogContext
            IPlugin<DbContext> forDb = universal;        // 逆变到 DbContext
            IPlugin<CacheContext> forCache = universal;  // 逆变到 CacheContext
            
            Console.WriteLine("  同一个 universal 对象，赋给三种泛型接口引用：");
            forLog.Initialize(new LogContext("/var/log/app.log", "Info"));
            forDb.Initialize(new DbContext("Server=localhost;", 30));
            forCache.Initialize(new CacheContext("redis://localhost", 3600));
            Console.WriteLine("  → 一处实现（通用插件），多处适配（三种上下文）");

            // --- Demo F：逆变作为形参传递 ---
            Console.WriteLine("\n【Demo F】逆变作为形参传递（泛型形参）\n");
            Console.WriteLine("  定义一个方法：");
            Console.WriteLine("    static void CallPlugin<TContext>(IPlugin<TContext> plugin, TContext context)");
            Console.WriteLine("        where TContext : IPluginContext");
            Console.WriteLine("    {");
            Console.WriteLine("        plugin.Initialize(context);  // 类型安全");
            Console.WriteLine("    }");
            Console.WriteLine();
            Console.WriteLine("  调用时，传入 universal（父类型插件）：");
            CallPlugin<LogContext>(universal, new LogContext("/var/log/app.log", "Info"));
            CallPlugin<DbContext>(universal, new DbContext("Server=localhost;", 30));
            CallPlugin<CacheContext>(universal, new CacheContext("redis://localhost", 3600));
            Console.WriteLine("  → 形参 IPlugin<TContext> 是逆变的");
            Console.WriteLine("  → 能传父类型插件（universal 是 IPlugin<IPluginContext>）");
            Console.WriteLine("  → 通过逆变适配给 IPlugin<LogContext> 等形参");
            Console.WriteLine("  → 方法内部 plugin.Initialize(context) 类型安全");

            // --- Demo C：逆变的方向 ---
            Console.WriteLine("\n【Demo C】逆变的方向\n");
            Console.WriteLine("  子类型关系（正常，顺）：");
            Console.WriteLine("    LogContext : IPluginContext   （LogContext 是子，IPluginContext 是父）");
            Console.WriteLine("  逆变方向（逆着来）：");
            Console.WriteLine("    IPlugin<IPluginContext> → IPlugin<LogContext>");
            Console.WriteLine("    （父类型插件 → 子类型插件）");

            // --- Demo D：对比——没有 in 会怎样 ---
            Console.WriteLine("\n【Demo D】如果没有 in 逆变\n");
            Console.WriteLine("  public interface IPlugin<TContext>   // 没有 in");
            Console.WriteLine("  IPlugin<IPluginContext> u = ...;");
            Console.WriteLine("  IPlugin<LogContext> p = u;   // ✗ 编译错误！");
            Console.WriteLine("  → 没有 in，IPlugin<IPluginContext> 不能赋给 IPlugin<LogContext>");
            Console.WriteLine("  → 通用插件就无法适配具体上下文");

            // --- Demo E：逆变的限制 ---
            Console.WriteLine("\n【Demo E】逆变的限制（in 只能在输入位置）\n");
            Console.WriteLine("  in TContext 要求 TContext 只出现在'输入位置'：");
            Console.WriteLine("    void Initialize(TContext context);   ✓ 输入位置（方法参数）");
            Console.WriteLine("    // TContext GetContext();            ✗ 输出位置（返回值）");
            Console.WriteLine("  → 逆变的前提：TContext '只进不出'");
            Console.WriteLine("  → 如果 TContext 也出现在返回值，就不能用 in");

            // --- 分析 ---
            Console.WriteLine("\n=== 分析 ===\n");
            PrintAnalysis();
        }

        static void PrintAnalysis()
        {
            Console.WriteLine("【第5步 vs 第6步 对比】");
            Console.WriteLine();
            Console.WriteLine("  第5步：IPlugin<TContext>（无型变）");
            Console.WriteLine("    IPlugin<LogContext> 和 IPlugin<DbContext> 互不相关");
            Console.WriteLine("    → 通用插件无法适配具体上下文");
            Console.WriteLine();
            Console.WriteLine("  第6步：IPlugin<in TContext>（逆变）");
            Console.WriteLine("    IPlugin<IPluginContext> → IPlugin<LogContext>（可赋值）");
            Console.WriteLine("    → 通用插件可以适配任意具体上下文");
            Console.WriteLine();
            Console.WriteLine("【逆变三条规则】");
            Console.WriteLine();
            Console.WriteLine("  规则1：方向是'逆'的");
            Console.WriteLine("    子类型：Derived : Base（顺）");
            Console.WriteLine("    逆变：IPlugin<Base> → IPlugin<Derived>（逆）");
            Console.WriteLine();
            Console.WriteLine("  规则2：TContext 只进不出");
            Console.WriteLine("    in TContext → TContext 只能在输入位置（方法参数）");
            Console.WriteLine("    不能出现在输出位置（返回值 / out 参数）");
            Console.WriteLine();
            Console.WriteLine("  规则3：只能加在接口/委托上");
            Console.WriteLine("    interface IPlugin<in TContext>  ✓");
            Console.WriteLine("    // class PluginBase<in TContext>  ✗（类不能带型变）");
            Console.WriteLine();
            Console.WriteLine("【逆变的实际价值】");
            Console.WriteLine();
            Console.WriteLine("  价值1：写一个通用插件，适配多种上下文");
            Console.WriteLine("    UniversalContextPlugin : PluginBase<IPluginContext>");
            Console.WriteLine("    → 可当 LogPlugin / DbPlugin / CachePlugin 用");
            Console.WriteLine();
            Console.WriteLine("  价值2：减少重复代码");
            Console.WriteLine("    不用为每种上下文写一个'通用处理'插件");
            Console.WriteLine("    写一个面向 IPluginContext 的，逆变适配所有");
            Console.WriteLine();
            Console.WriteLine("  价值3：符合'依赖倒置'");
            Console.WriteLine("    插件依赖抽象的 IPluginContext");
            Console.WriteLine("    通过逆变适配具体上下文");
            Console.WriteLine();
            Console.WriteLine("【协变 vs 逆变（对照）】");
            Console.WriteLine();
            Console.WriteLine("  协变 out T：子类型关系'顺'着走");
            Console.WriteLine("    IEnumerable<out T>：IEnumerable<Derived> → IEnumerable<Base>");
            Console.WriteLine("    T 只出现在'输出位置'（返回值）");
            Console.WriteLine();
            Console.WriteLine("  逆变 in T：子类型关系'逆'着走");
            Console.WriteLine("    IPlugin<in T>：IPlugin<Base> → IPlugin<Derived>");
            Console.WriteLine("    T 只出现在'输入位置'（参数）");
            Console.WriteLine();
            Console.WriteLine("  记忆口诀：");
            Console.WriteLine("    out 出（返回值）→ 协变（顺）");
            Console.WriteLine("    in  入（参数）  → 逆变（逆）");
        }

        // ★ 新增：逆变作为形参传递的示例方法
        // 形参 IPlugin<TContext> 是逆变的接口
        // 调用时，能传父类型的插件（通过逆变适配）
        static void CallPlugin<TContext>(IPlugin<TContext> plugin, TContext context)
            where TContext : IPluginContext
        {
          
            plugin.Initialize(context);  // 类型安全
        }
    }
}
