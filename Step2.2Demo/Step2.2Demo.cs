using System;
using System.Collections.Generic;

namespace Step2_2Demo
{
    // ============================================================
    // 第2步：方法参数用接口解耦
    // ============================================================
    //
    // 【与第1步的对比】
    //
    // 第1步（Step2.1Demo）：无接口，具体类型参数
    //   LogPlugin.Initialize(LogContext context)    ← 参数是具体类
    //   DbPlugin.Initialize(DbContext context)      ← 参数是具体类
    //   CachePlugin.Initialize(CacheContext context)← 参数是具体类
    //   → 宿主认识每个具体类型，不能 foreach
    //
    // 第2步（Step2.2Demo）：方法参数用接口类型
    //   IPlugin.Initialize(IPluginContext context)  ← 参数是接口
    //   → 宿主只认接口，能 List<IPlugin> + foreach
    //   → 但插件内部要强转访问特有属性（第2步的局限）
    //
    // 【保留第1步的所有类型和构造函数】
    //   LogContext(string logPath, string logLevel)        ← 不变
    //   DbContext(string connectionString, int timeout)    ← 不变
    //   CacheContext(string redisUrl, int expiration)      ← 不变
    //   只是让它们实现 IPluginContext 接口
    // ============================================================


    // ========== 接口定义 ==========
    // 第1步没有这两个接口，第2步新增

    /// <summary>统一上下文接口（标记接口）</summary>
    public interface IPluginContext
    {
        // 第1步的三个上下文没有公共属性，所以接口是空标记
        // 作用：让所有上下文可以统一引用为 IPluginContext
    }

    /// <summary>统一插件接口</summary>
    public interface IPlugin
    {
        string Name { get; }
        void Initialize(IPluginContext context);  // ← 参数是接口，不是具体类
    }


    // ========== 上下文：和第1步完全一样的构造函数和属性，只是实现 IPluginContext ==========

    /// <summary>日志上下文：有日志路径和级别</summary>
    public class LogContext : IPluginContext
    {
        public LogContext(string logPath, string logLevel)
        {
            LogPath = logPath;
            LogLevel = logLevel;
        }

        public string LogPath { get; }
        public string LogLevel { get; }
    }

    /// <summary>数据库上下文：有连接字符串和超时</summary>
    public class DbContext : IPluginContext
    {
        public DbContext(string connectionString, int timeoutSeconds)
        {
            ConnectionString = connectionString;
            TimeoutSeconds = timeoutSeconds;
        }

        public string ConnectionString { get; }
        public int TimeoutSeconds { get; }
    }

    /// <summary>缓存上下文：有 Redis 地址和过期时间</summary>
    public class CacheContext : IPluginContext
    {
        public CacheContext(string redisUrl, int expirationSeconds)
        {
            RedisUrl = redisUrl;
            ExpirationSeconds = expirationSeconds;
        }

        public string RedisUrl { get; }
        public int ExpirationSeconds { get; }
    }


    // ========== 插件：实现 IPlugin，参数改为 IPluginContext ==========
    // 对比第1步：
    //   第1步：Initialize(LogContext context)     ← 直接访问 context.LogPath
    //   第2步：Initialize(IPluginContext context) ← 先强转再访问

    /// <summary>日志插件</summary>
    public class LogPlugin : IPlugin
    {
        public string Name => "LogPlugin";

        public void Initialize(IPluginContext context)
        {
            // 第1步：直接访问 context.LogPath
            // 第2步：先强转，再访问
            var logContext = (LogContext)context;
            Console.WriteLine($"  [{Name}] 初始化，路径={logContext.LogPath}，级别={logContext.LogLevel}");
        }
    }

    /// <summary>数据库插件</summary>
    public class DbPlugin : IPlugin
    {
        public string Name => "DbPlugin";

        public void Initialize(IPluginContext context)
        {
            var dbContext = (DbContext)context;
            Console.WriteLine($"  [{Name}] 初始化，连接={dbContext.ConnectionString}，超时={dbContext.TimeoutSeconds}s");
        }
    }

    /// <summary>缓存插件</summary>
    public class CachePlugin : IPlugin
    {
        public string Name => "CachePlugin";

        public void Initialize(IPluginContext context)
        {
            var cacheContext = (CacheContext)context;
            Console.WriteLine($"  [{Name}] 初始化，Redis={cacheContext.RedisUrl}，过期={cacheContext.ExpirationSeconds}s");
        }
    }


    // ========== 宿主：只依赖接口 ==========
    // 对比第1步：
    //   第1步：private LogPlugin _log; private DbPlugin _db; private CachePlugin _cache;
    //   第2步：private List<(IPlugin, IPluginContext)> _plugins;
    //
    //   第1步：逐个写死调用
    //   第2步：foreach 统一调度
    public class PluginHost
    {
        // 存的是配对（插件 + 上下文），一次注册搞定
        private readonly List<(IPlugin Plugin, IPluginContext Context)> _plugins
            = new List<(IPlugin, IPluginContext)>();

        // 一次注册：插件 + 它的上下文
        // 参数是接口 → 接受任何实现
        public void AddPlugin(IPlugin plugin, IPluginContext context)
        {
            _plugins.Add((plugin, context));
        }

        // 统一调度：不需要外部传参
        public void StartAll()
        {
            Console.WriteLine($"宿主开始调度，共 {_plugins.Count} 个插件\n");

            foreach (var (plugin, context) in _plugins)
            {
                plugin.Initialize(context);  // ← 通过接口调用
            }
        }
    }


    // ========== 主程序 ==========
    public class Program
    {
        public static void Main()
        {
            Console.WriteLine("========== 第2步：方法参数用接口解耦 ==========\n");

            // --- 创建宿主 ---
            var host = new PluginHost();

            // --- 创建上下文 ---
            var logContext = new LogContext("/var/log/app.log", "Info");
            var dbContext = new DbContext("Server=localhost;Db=app;", 30);
            var cacheContext = new CacheContext("redis://localhost:6379", 3600);

            // --- 注册多种插件（一次注册：插件 + 上下文）---
            host.AddPlugin(new LogPlugin(), logContext);
            host.AddPlugin(new DbPlugin(), dbContext);
            host.AddPlugin(new CachePlugin(), cacheContext);

            // --- 宿主统一调度 ---
            host.StartAll();

            // --- 强转失败演示 ---
            Console.WriteLine("\n--- 强转失败演示 ---\n");
            try
            {
                var logPlugin = new LogPlugin();
                // 给 LogPlugin 传了 DbContext → 内部强转失败
                logPlugin.Initialize(dbContext);
            }
            catch (InvalidCastException ex)
            {
                Console.WriteLine($"  [异常] {ex.Message}");
                Console.WriteLine("  → 编译期没报错（IPluginContext 是合法参数）");
                Console.WriteLine("  → 运行时才崩溃（强转失败）");
            }

            // --- 分析 ---
            Console.WriteLine("\n=== 分析 ===\n");
            PrintAnalysis();
        }

        static void PrintAnalysis()
        {
            Console.WriteLine("【第1步 vs 第2步 逐行对比】");
            Console.WriteLine();
            Console.WriteLine("  上下文（构造函数不变，多了实现接口）：");
            Console.WriteLine("    第1步: public class LogContext                    { ... }");
            Console.WriteLine("    第2步: public class LogContext : IPluginContext   { ... }");
            Console.WriteLine("    → 构造函数 LogContext(string logPath, string logLevel) 不变");
            Console.WriteLine("    → 属性 LogPath、LogLevel 不变");
            Console.WriteLine("    → 只是多了 : IPluginContext");
            Console.WriteLine();
            Console.WriteLine("  插件（参数类型变了，内部强转）：");
            Console.WriteLine("    第1步: void Initialize(LogContext context)");
            Console.WriteLine("           → 直接访问 context.LogPath");
            Console.WriteLine("    第2步: void Initialize(IPluginContext context)");
            Console.WriteLine("           → 强转: var logCtx = (LogContext)context;");
            Console.WriteLine("           → 再访问: logCtx.LogPath");
            Console.WriteLine();
            Console.WriteLine("  宿主（从逐个字段变成 List + foreach）：");
            Console.WriteLine("    第1步:");
            Console.WriteLine("      private LogPlugin _logPlugin;");
            Console.WriteLine("      private DbPlugin _dbPlugin;");
            Console.WriteLine("      private CachePlugin _cachePlugin;");
            Console.WriteLine("      _logPlugin.Initialize(logContext);    // 逐个写死");
            Console.WriteLine("      _dbPlugin.Initialize(dbContext);");
            Console.WriteLine("      _cachePlugin.Initialize(cacheContext);");
            Console.WriteLine();
            Console.WriteLine("    第2步:");
            Console.WriteLine("      private List<IPlugin> _plugins;");
            Console.WriteLine("      foreach (var pair in pairs)");
            Console.WriteLine("          pair.Plugin.Initialize(pair.Context);  // 统一调度");
            Console.WriteLine();
            Console.WriteLine("【方法参数用接口的4个好处】");
            Console.WriteLine();
            Console.WriteLine("  好处1：统一调度");
            Console.WriteLine("    参数是 IPlugin → List<IPlugin> + foreach");
            Console.WriteLine("    第1步：参数是具体类 → 逐个字段，不能循环");
            Console.WriteLine();
            Console.WriteLine("  好处2：松耦合");
            Console.WriteLine("    参数是 IPlugin → 宿主不认识 LogPlugin/DbPlugin/CachePlugin");
            Console.WriteLine("    第1步：参数是具体类 → 宿主必须认识每个具体类");
            Console.WriteLine();
            Console.WriteLine("  好处3：可替换");
            Console.WriteLine("    参数是 IPlugin → 任何实现 IPlugin 的类都能传进来");
            Console.WriteLine("    第1步：参数是 LogPlugin → 只能传 LogPlugin");
            Console.WriteLine();
            Console.WriteLine("  好处4：可测试");
            Console.WriteLine("    参数是 IPlugin → 可以 mock: new Mock<IPlugin>()");
            Console.WriteLine("    第1步：参数是具体类 → 难 mock");
            Console.WriteLine();
            Console.WriteLine("【第2步的局限：强转】");
            Console.WriteLine("  插件内部访问特有属性要强转：");
            Console.WriteLine("    var logCtx = (LogContext)context;  ← 运行时强转");
            Console.WriteLine("  传错类型 → InvalidCastException（如上面演示）");
            Console.WriteLine("  → 编译期不安全，运行时才崩溃");
            Console.WriteLine();
            Console.WriteLine("【依赖倒置原则】");
            Console.WriteLine("  第1步：宿主 → LogPlugin（依赖具体）");
            Console.WriteLine("  第2步：宿主 → IPlugin ← LogPlugin（依赖抽象）");
            Console.WriteLine("  方法参数用接口 = 依赖倒置的体现");
            Console.WriteLine();
            Console.WriteLine("【解法预告：第3步泛型接口】");
            Console.WriteLine("  IPlugin<in TContext> : IPlugin");
            Console.WriteLine("    void Initialize(TContext context)  ← 泛型参数");
            Console.WriteLine("  → 编译期类型安全，不用强转");
        }
    }
}
