using System;
using System.Collections.Generic;

namespace Step2_4Demo
{
    // ============================================================
    // 第4步：抽象工厂模式，替换 List<(IPlugin, IPluginContext)>
    // ============================================================
    //
    // 【与第3步的对比】
    //
    // 第3步（Step2.3Demo）：宿主手动注册插件+上下文配对
    //   host.AddPlugin(logPlugin, logContext);
    //   → 调用方负责创建插件和上下文，宿主只存配对
    //   → List<(IPlugin, IPluginContext)> 存的是"已创建的对象"
    //
    // 第4步（Step2.4Demo）：宿主通过工厂创建插件+上下文
    //   host.AddFactory(new LogPluginFactory(path, level));
    //   → 调用方注册工厂，宿主负责创建插件和上下文
    //   → List<IPluginFactory> 存的是"能创建对象的工厂"
    //
    // 【抽象工厂模式】
    //   IPluginFactory          ← 抽象工厂接口（创建插件+上下文配对）
    //   LogPluginFactory 等     ← 具体工厂（知道怎么创建特定配对）
    //   PluginHost              ← 客户端（通过工厂创建，不直接 new）
    //
    // 【为什么用抽象工厂？】
    //   1. 创建逻辑内聚：工厂知道"LogPlugin 配 LogContext"，不会配错
    //   2. 宿主解耦：宿主不依赖具体插件类/上下文类，只依赖工厂接口
    //   3. 延迟创建：工厂注册时不创建对象，StartAll 时才创建
    //   4. 可扩展：新增插件类型只需新增工厂，宿主不用改
    // ============================================================


    // ========== 值类型：上下文描述符（不变量承担者）==========

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


    // ========== 接口定义（薄契约）==========

    public interface IPluginContext
    {
        ContextDescriptor Descriptor { get; }
    }

    public interface IPlugin
    {
        string Name { get; }
        void Initialize(IPluginContext context);
    }


    // ========== 上下文实现 ==========

    public class LogContext : IPluginContext
    {
        public LogContext(string logPath, string logLevel)
        {
            Descriptor = new ContextDescriptor(logPath, logLevel);
        }

        public ContextDescriptor Descriptor { get; }
    }

    public class DbContext : IPluginContext
    {
        public DbContext(string connectionString, int timeoutSeconds)
        {
            Descriptor = new ContextDescriptor(connectionString, timeoutSeconds.ToString());
            TimeoutSeconds = timeoutSeconds;
        }

        public ContextDescriptor Descriptor { get; }
        public int TimeoutSeconds { get; }
    }

    public class CacheContext : IPluginContext
    {
        public CacheContext(string redisUrl, int expirationSeconds)
        {
            Descriptor = new ContextDescriptor(redisUrl, expirationSeconds.ToString());
            ExpirationSeconds = expirationSeconds;
        }

        public ContextDescriptor Descriptor { get; }
        public int ExpirationSeconds { get; }
    }


    // ========== 泛型抽象基类（强类型 + 类型守卫）==========

    public abstract class PluginBase<TContext> : IPlugin
        where TContext : IPluginContext
    {
        public abstract string Name { get; }

        protected abstract void OnInitialize(TContext context);

        public void Initialize(TContext context)
        {
            Console.WriteLine($"  [{Name}] Initialize(TContext) 被调用，TContext = {typeof(TContext).Name}，Source = {context.Descriptor.Source}");
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


    // ========== 抽象工厂：创建插件 + 上下文配对 ==========
    //
    // 抽象工厂模式的核心：
    //   工厂知道"哪种插件配哪种上下文"，调用方不用关心
    //   宿主通过工厂接口创建，不依赖具体插件类/上下文类

    public interface IPluginFactory
    {
        IPlugin CreatePlugin();
        IPluginContext CreateContext();
    }


    // ========== 具体工厂：每个工厂负责一种插件+上下文配对 ==========

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


    // ========== 宿主：用工厂替换 List<(IPlugin, IPluginContext)> ==========
    //
    // 第3步：List<(IPlugin, IPluginContext)> _plugins  ← 存已创建的对象
    // 第4步：List<IPluginFactory> _factories           ← 存能创建对象的工厂
    //
    // 变化：
    //   AddPlugin(plugin, context) → AddFactory(factory)
    //   StartAll 直接遍历配对       → StartAll 通过工厂创建再调度

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
                // 延迟创建：在这里才创建插件和上下文
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
            Console.WriteLine("========== 第4步：抽象工厂模式，替换 List<(IPlugin, IPluginContext)> ==========\n");

            // --- Demo A：工厂创建插件+上下文配对 ---
            Console.WriteLine("【Demo A】通过工厂创建插件+上下文配对\n");
            var logFactory = new LogPluginFactory("/var/log/app.log", "Info");
            var dbFactory = new DbPluginFactory("Server=localhost;Db=app;", 30);
            var cacheFactory = new CachePluginFactory("redis://localhost:6379", 3600);

            var logPlugin = logFactory.CreatePlugin();
            var logContext = logFactory.CreateContext();
            Console.WriteLine($"  工厂创建：{logPlugin.Name} + {logContext.Descriptor}");
            logPlugin.Initialize(logContext);
            
            // --- Demo B：宿主通过工厂统一调度 ---
            Console.WriteLine("\n【Demo B】宿主通过工厂统一调度\n");
            var host = new PluginHost();
            // 注册工厂，不创建对象
            host.AddFactory(logFactory);
            host.AddFactory(dbFactory);
            host.AddFactory(cacheFactory);
            // StartAll 时才通过工厂创建
            host.StartAll();

            // --- Demo C：工厂保证配对正确 ---
            Console.WriteLine("\n【Demo C】工厂保证插件+上下文配对正确\n");
            Console.WriteLine("  LogPluginFactory.CreatePlugin() → LogPlugin");
            Console.WriteLine("  LogPluginFactory.CreateContext() → LogContext");
            Console.WriteLine("  → 工厂内部知道配对关系，不会配错");
            Console.WriteLine("  → 对比第3步：host.AddPlugin(logPlugin, dbContext) ← 调用方可能配错！");

            // --- Demo D：新增插件只需新增工厂，宿主不用改 ---
            Console.WriteLine("\n【Demo D】新增插件类型只需新增工厂\n");
            Console.WriteLine("  新增 EmailPlugin：");
            Console.WriteLine("    1. 写 EmailPlugin : PluginBase<EmailContext>");
            Console.WriteLine("    2. 写 EmailPluginFactory : IPluginFactory");
            Console.WriteLine("    3. host.AddFactory(new EmailPluginFactory(...))");
            Console.WriteLine("  → 宿主代码不用改（只依赖 IPluginFactory）");
            Console.WriteLine("  → 符合开闭原则：对扩展开放，对修改关闭");

            // --- Demo E：类型不匹配时的友好报错 ---
            Console.WriteLine("\n【Demo E】类型不匹配时的友好报错\n");
            try
            {
                IPlugin plugin = new LogPlugin();
                var wrongContext = new DbContext("Server=x;", 10);
                plugin.Initialize(wrongContext);
            }
            catch (ArgumentException ex)
            {
                Console.WriteLine($"  [异常] {ex.Message}");
                Console.WriteLine("  → 工厂模式下不会发生（工厂保证配对正确）");
                Console.WriteLine("  → 但如果手动绕过工厂，类型守卫仍然生效");
            }

            // --- 分析 ---
            Console.WriteLine("\n=== 分析 ===\n");
            PrintAnalysis();
        }

        static void PrintAnalysis()
        {
            Console.WriteLine("【第3步 vs 第4步 对比】");
            Console.WriteLine();
            Console.WriteLine("  第3步（手动注册配对）：");
            Console.WriteLine("    List<(IPlugin, IPluginContext)> _plugins");
            Console.WriteLine("    host.AddPlugin(logPlugin, logContext);  ← 调用方创建+配对");
            Console.WriteLine("    → 存的是'已创建的对象'");
            Console.WriteLine("    → 调用方负责创建插件和上下文");
            Console.WriteLine("    → 调用方可能配错：AddPlugin(logPlugin, dbContext)");
            Console.WriteLine();
            Console.WriteLine("  第4步（工厂创建配对）：");
            Console.WriteLine("    List<IPluginFactory> _factories");
            Console.WriteLine("    host.AddFactory(new LogPluginFactory(...));  ← 调用方只注册工厂");
            Console.WriteLine("    → 存的是'能创建对象的工厂'");
            Console.WriteLine("    → 工厂负责创建插件和上下文，保证配对正确");
            Console.WriteLine("    → 不会配错：LogPluginFactory 只能创建 LogPlugin + LogContext");
            Console.WriteLine();
            Console.WriteLine("【抽象工厂模式的结构】");
            Console.WriteLine();
            Console.WriteLine("  IPluginFactory（抽象工厂）");
            Console.WriteLine("    ├── CreatePlugin()   → IPlugin");
            Console.WriteLine("    └── CreateContext()  → IPluginContext");
            Console.WriteLine();
            Console.WriteLine("  LogPluginFactory（具体工厂）");
            Console.WriteLine("    ├── CreatePlugin()   → new LogPlugin()");
            Console.WriteLine("    └── CreateContext()  → new LogContext(path, level)");
            Console.WriteLine();
            Console.WriteLine("  DbPluginFactory（具体工厂）");
            Console.WriteLine("    ├── CreatePlugin()   → new DbPlugin()");
            Console.WriteLine("    └── CreateContext()  → new DbContext(connStr, timeout)");
            Console.WriteLine();
            Console.WriteLine("  CachePluginFactory（具体工厂）");
            Console.WriteLine("    ├── CreatePlugin()   → new CachePlugin()");
            Console.WriteLine("    └── CreateContext()  → new CacheContext(url, expiration)");
            Console.WriteLine();
            Console.WriteLine("【抽象工厂的四个好处】");
            Console.WriteLine();
            Console.WriteLine("  好处1：创建逻辑内聚");
            Console.WriteLine("    工厂知道'LogPlugin 配 LogContext'，不会配错");
            Console.WriteLine("    → 对比第3步：AddPlugin(logPlugin, dbContext) 能编译但运行崩");
            Console.WriteLine();
            Console.WriteLine("  好处2：宿主解耦");
            Console.WriteLine("    宿主只依赖 IPluginFactory，不依赖 LogPlugin/LogContext 等具体类");
            Console.WriteLine("    → 新增插件类型，宿主代码不用改");
            Console.WriteLine();
            Console.WriteLine("  好处3：延迟创建");
            Console.WriteLine("    工厂注册时不创建对象，StartAll 时才创建");
            Console.WriteLine("    → 可以先注册100个工厂，按需创建");
            Console.WriteLine();
            Console.WriteLine("  好处4：开闭原则");
            Console.WriteLine("    新增 EmailPlugin → 写 EmailPluginFactory → host.AddFactory(...)");
            Console.WriteLine("    → 宿主不用改，符合'对扩展开放，对修改关闭'");
            Console.WriteLine();
            Console.WriteLine("【代价】");
            Console.WriteLine();
            Console.WriteLine("  1. 多了工厂类（3 个具体工厂 + 1 个接口）");
            Console.WriteLine("  2. 调用方多一步（先 new 工厂再注册）");
            Console.WriteLine("  3. 如果只有一种插件，工厂是过度设计");
            Console.WriteLine("    → 但当前有 3 种插件，且可能扩展，工厂合理");
        }
    }
}
