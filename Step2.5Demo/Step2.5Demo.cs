using System;
using System.Collections.Generic;

namespace Step2_5Demo
{
    // ============================================================
    // 第5步：泛型接口 IPlugin<TContext>，提供接口层面的类型安全访问
    // ============================================================
    //
    // 【与第4步的对比】
    //
    // 第4步（Step2.4Demo）：PluginBase<TContext> : IPlugin（只实现非泛型接口）
    //   → 强类型方法 Initialize(TContext) 是 public 方法，不是接口方法
    //   → 通过 IPlugin 引用只能走 Initialize(IPluginContext)，要类型检查
    //   → 没有泛型接口，无法用 IPlugin<LogContext> 做类型安全的引用
    //
    // 第5步（Step2.5Demo）：PluginBase<TContext> : IPlugin<TContext> : IPlugin
    //   → 泛型接口 IPlugin<TContext> 有 Initialize(TContext) — 接口层面的强类型
    //   → 通过 IPlugin<LogContext> 引用可以直接调 Initialize(LogContext)，编译期安全
    //   → 不用走显式接口实现的运行时类型检查
    //
    // 【三层接口设计】
    //   IPlugin                    ← 非泛型，宿主统一调度（管"相遇"）
    //   IPlugin<TContext> : IPlugin ← 泛型，类型安全访问（管"相认"）
    //   PluginBase<TContext>        ← 泛型抽象类，实现 IPlugin<TContext>（管"翻译"）
    //
    // 【为什么需要泛型接口？】
    //   第4步：IPlugin plugin = logPlugin; plugin.Initialize(context) → 运行时类型检查
    //   第5步：IPlugin<LogContext> plugin = logPlugin; plugin.Initialize(context) → 编译期安全
    //   → 泛型接口提供了"接口层面的类型安全"，不只是"方法层面的类型安全"
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

    // 非泛型插件接口：宿主用它统一调度（管"相遇"）
    public interface IPlugin
    {
        string Name { get; }
        void Initialize(IPluginContext context);
    }

    // 泛型插件接口：类型安全访问（管"相认"）
    // 继承 IPlugin，所以实现 IPlugin<TContext> 的类也满足 IPlugin
    public interface IPlugin<TContext> : IPlugin
        where TContext : IPluginContext
    {
        // 强类型初始化：参数是 TContext，不是 IPluginContext
        // 通过 IPlugin<LogContext> 引用调用时，编译期保证类型安全
        void Initialize(TContext context);
    }


    // ========== 上下文实现 ==========
    //
    // sealed（封闭类）：不能被继承
    //   → 保证 context is TContext 是精确匹配（没有子类干扰继承链）
    //   → 不用反射（不用 GetType() == typeof()）

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


    // ========== 泛型抽象基类：实现泛型接口 IPlugin<TContext> ==========
    //
    // 第4步：PluginBase<TContext> : IPlugin          ← 只实现非泛型接口
    // 第5步：PluginBase<TContext> : IPlugin<TContext> ← 实现泛型接口
    //
    // 实现两个版本的 Initialize：
    //   1. public void Initialize(TContext)          ← 实现 IPlugin<TContext>.Initialize(TContext)
    //      → 通过 IPlugin<TContext> 引用调用，编译期安全
    //   2. void IPlugin.Initialize(IPluginContext)   ← 显式实现 IPlugin.Initialize(IPluginContext)
    //      → 通过 IPlugin 引用调用，运行时类型检查

    public abstract class PluginBase<TContext> : IPlugin<TContext>
        where TContext : IPluginContext
    {
        public abstract string Name { get; }

        protected abstract void OnInitialize(TContext context);

        // 实现 IPlugin<TContext>.Initialize(TContext) — 强类型
        // 通过 IPlugin<LogContext> 引用调用时走这个方法，编译期安全
        public void Initialize(TContext context)
        {
            Console.WriteLine($"  [{Name}] Initialize(TContext) 被调用，TContext = {typeof(TContext).Name}，Source = {context.Descriptor.Source}");
            OnInitialize(context);
        }

        // 显式接口实现：IPlugin.Initialize(IPluginContext)
        // 通过 IPlugin 引用调用时走这个方法，运行时类型检查
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


    // ========== 插件实现 ==========

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


    // ========== 抽象工厂 ==========

    public interface IPluginFactory
    {
        IPlugin CreatePlugin();
        IPluginContext CreateContext();
    }

    public class LogPluginFactory : IPluginFactory
    {
        private readonly string _logPath;
        private readonly string _logLevel;

        public LogPluginFactory(string logPath, string logLevel)
        {
            _logPath = logPath;
            _logLevel = logLevel;
        }

        public IPlugin CreatePlugin() => new LogPlugin();
        public IPluginContext CreateContext() => new LogContext(_logPath, _logLevel);
    }

    public class DbPluginFactory : IPluginFactory
    {
        private readonly string _connectionString;
        private readonly int _timeoutSeconds;

        public DbPluginFactory(string connectionString, int timeoutSeconds)
        {
            _connectionString = connectionString;
            _timeoutSeconds = timeoutSeconds;
        }

        public IPlugin CreatePlugin() => new DbPlugin();
        public IPluginContext CreateContext() => new DbContext(_connectionString, _timeoutSeconds);
    }

    public class CachePluginFactory : IPluginFactory
    {
        private readonly string _redisUrl;
        private readonly int _expirationSeconds;

        public CachePluginFactory(string redisUrl, int expirationSeconds)
        {
            _redisUrl = redisUrl;
            _expirationSeconds = expirationSeconds;
        }

        public IPlugin CreatePlugin() => new CachePlugin();
        public IPluginContext CreateContext() => new CacheContext(_redisUrl, _expirationSeconds);
    }


    // ========== 宿主：通过工厂统一调度 ==========

    public class PluginHost
    {
        private readonly List<IPluginFactory> _factories = new List<IPluginFactory>();

        public void AddFactory(IPluginFactory factory)
        {
            _factories.Add(factory);
        }

        public void StartAll()
        {
            Console.WriteLine($"宿主开始调度，共 {_factories.Count} 个工厂\n");

            foreach (var factory in _factories)
            {
                var plugin = factory.CreatePlugin();
                var context = factory.CreateContext();

                Console.WriteLine($"  调度 {plugin.Name}，Descriptor = {context.Descriptor}");
                plugin.Initialize(context);
            }
        }
    }


    // ========== 主程序 ==========
    public class Program
    {
        public static void Main()
        {
            Console.WriteLine("========== 第5步：泛型接口 IPlugin<TContext>，接口层面类型安全 ==========\n");

            // --- Demo A：通过泛型接口 IPlugin<TContext> 类型安全访问 ---
            Console.WriteLine("【Demo A】通过 IPlugin<TContext> 泛型接口类型安全访问\n");
            IPlugin<LogContext> logPluginGeneric = new LogPlugin();
            var logContext = new LogContext("/var/log/app.log", "Info");
            // IPlugin<LogContext>.Initialize(LogContext) → 编译期安全，不走运行时类型检查
            logPluginGeneric.Initialize(logContext);

            // --- Demo B：泛型接口编译期类型安全 ---
            Console.WriteLine("\n【Demo B】IPlugin<TContext> 编译期类型安全\n");
            Console.WriteLine("  IPlugin<LogContext> plugin = new LogPlugin();");
            Console.WriteLine("  plugin.Initialize(logContext);  ← 编译通过，类型匹配");
            Console.WriteLine("  // plugin.Initialize(dbContext); ← 编译错误！CS1503");
            Console.WriteLine("  → 泛型接口在编译期就保证类型安全，不用等运行时");

            // --- Demo C：非泛型接口 IPlugin 仍可统一调度 ---
            Console.WriteLine("\n【Demo C】IPlugin 非泛型接口仍可统一调度（兼容）\n");
            IPlugin plugin = logPluginGeneric;  // IPlugin<LogContext> → IPlugin（隐式转换）
            Console.WriteLine($"  IPlugin plugin = logPluginGeneric;  ← 隐式转换");
            Console.WriteLine($"  plugin.Initialize(logContext);  ← 走显式接口实现，运行时类型检查");
            plugin.Initialize(logContext);

            // --- Demo D：宿主通过工厂统一调度 ---
            Console.WriteLine("\n【Demo D】宿主通过工厂统一调度\n");
            var host = new PluginHost();
            host.AddFactory(new LogPluginFactory("/var/log/app.log", "Info"));
            host.AddFactory(new DbPluginFactory("Server=localhost;Db=app;", 30));
            host.AddFactory(new CachePluginFactory("redis://localhost:6379", 3600));
            host.StartAll();

            // --- Demo E：类型不匹配时的友好报错 ---
            Console.WriteLine("\n【Demo E】通过 IPlugin 调用时类型不匹配的友好报错\n");
            try
            {
                IPlugin p = new LogPlugin();
                var wrongContext = new DbContext("Server=x;", 10);
                p.Initialize(wrongContext);  // ← 走显式接口实现，运行时类型检查
            }
            catch (ArgumentException ex)
            {
                Console.WriteLine($"  [异常] {ex.Message}");
                Console.WriteLine("  → 通过 IPlugin 调用走运行时检查");
                Console.WriteLine("  → 通过 IPlugin<TContext> 调用在编译期就阻止");
            }

            // --- 分析 ---
            Console.WriteLine("\n=== 分析 ===\n");
            PrintAnalysis();
        }

        static void PrintAnalysis()
        {
            Console.WriteLine("【第4步 vs 第5步 对比】");
            Console.WriteLine();
            Console.WriteLine("  第4步：PluginBase<TContext> : IPlugin");
            Console.WriteLine("    → 强类型 Initialize(TContext) 是 public 方法，不是接口方法");
            Console.WriteLine("    → 无法用接口引用做类型安全调用");
            Console.WriteLine("    → IPlugin plugin = logPlugin;");
            Console.WriteLine("      plugin.Initialize(context)  ← 走 IPlugin.Initialize(IPluginContext)，运行时检查");
            Console.WriteLine();
            Console.WriteLine("  第5步：PluginBase<TContext> : IPlugin<TContext> : IPlugin");
            Console.WriteLine("    → 强类型 Initialize(TContext) 是泛型接口方法");
            Console.WriteLine("    → 可以用泛型接口引用做类型安全调用");
            Console.WriteLine("    → IPlugin<LogContext> plugin = logPlugin;");
            Console.WriteLine("      plugin.Initialize(logContext)  ← 走 IPlugin<TContext>.Initialize(TContext)，编译期安全");
            Console.WriteLine();
            Console.WriteLine("【三层接口设计】");
            Console.WriteLine();
            Console.WriteLine("  IPlugin                    ← 非泛型，宿主统一调度（管'相遇'）");
            Console.WriteLine("    { string Name; void Initialize(IPluginContext); }");
            Console.WriteLine();
            Console.WriteLine("  IPlugin<TContext> : IPlugin ← 泛型，类型安全访问（管'相认'）");
            Console.WriteLine("    { void Initialize(TContext); }");
            Console.WriteLine();
            Console.WriteLine("  PluginBase<TContext>        ← 泛型抽象类（管'翻译'）");
            Console.WriteLine("    : IPlugin<TContext>");
            Console.WriteLine("    → public void Initialize(TContext)       ← 实现泛型接口");
            Console.WriteLine("    → void IPlugin.Initialize(IPluginContext) ← 显式实现非泛型接口");
            Console.WriteLine();
            Console.WriteLine("【两种调用路径】");
            Console.WriteLine();
            Console.WriteLine("  路径1：IPlugin<LogContext> plugin = logPlugin;");
            Console.WriteLine("         plugin.Initialize(logContext);");
            Console.WriteLine("         → 走 public Initialize(TContext)");
            Console.WriteLine("         → 编译期类型安全，不走运行时检查");
            Console.WriteLine();
            Console.WriteLine("  路径2：IPlugin plugin = logPlugin;");
            Console.WriteLine("         plugin.Initialize(context);");
            Console.WriteLine("         → 走显式 void IPlugin.Initialize(IPluginContext)");
            Console.WriteLine("         → 运行时类型检查（is TContext），不匹配抛 ArgumentException");
            Console.WriteLine();
            Console.WriteLine("【泛型接口的好处】");
            Console.WriteLine();
            Console.WriteLine("  好处1：接口层面的类型安全");
            Console.WriteLine("    第4步：类型安全只在 PluginBase<T> 的 public 方法层面");
            Console.WriteLine("    第5步：类型安全提升到 IPlugin<T> 接口层面");
            Console.WriteLine("    → 可以用接口引用做类型安全调用，不用引用具体类");
            Console.WriteLine();
            Console.WriteLine("  好处2：为型变（in/out）做准备");
            Console.WriteLine("    IPlugin<in TContext> 可以加逆变修饰");
            Console.WriteLine("    → IPlugin<LogContext> 可以赋值给 IPlugin<IPluginContext>");
            Console.WriteLine("    → 这是后续协变/逆变设计的基础");
            Console.WriteLine();
            Console.WriteLine("  好处3：多态 + 类型安全兼得");
            Console.WriteLine("    IPlugin：多态（统一调度所有插件）");
            Console.WriteLine("    IPlugin<T>：类型安全（编译期保证参数类型）");
            Console.WriteLine("    → 非泛型管'相遇'，泛型管'相认'");
            Console.WriteLine();
            Console.WriteLine("【代价】");
            Console.WriteLine();
            Console.WriteLine("  1. 多一层接口（IPlugin<TContext>）");
            Console.WriteLine("  2. PluginBase<T> 要实现两个版本的 Initialize");
            Console.WriteLine("  3. 但这层接口提供了'接口层面的类型安全'，值得");
        }
    }
}
