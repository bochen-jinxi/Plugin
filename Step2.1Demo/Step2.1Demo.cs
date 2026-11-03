using System;

namespace Step2_1Demo
{
    // ============================================================
    // 第1步：多种插件（无接口，直接具体类型，每个插件上下文不同）
    // ============================================================
    //
    // 【思路】
    // 不定义 IPlugin / IPluginContext 接口
    // 每个插件有自己的上下文类型，属性各不相同
    // 宿主直接依赖每个具体插件类型和具体上下文类型
    //
    // 【结果】
    // 代码能跑，但宿主必须认识每个插件的类型和每个上下文的类型
    // 上下文创建逻辑写死在宿主里，每加一个插件就要改宿主三处
    // ============================================================


    // ========== 每个插件有自己的上下文，属性完全不同 ==========

    /// <summary>日志上下文：有日志路径和级别</summary>
    public class LogContext
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
    public class DbContext
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
    public class CacheContext
    {
        public CacheContext(string redisUrl, int expirationSeconds)
        {
            RedisUrl = redisUrl;
            ExpirationSeconds = expirationSeconds;
        }

        public string RedisUrl { get; }
        public int ExpirationSeconds { get; }
    }


    // ========== 多种插件：具体类，无公共接口，签名各不相同 ==========

    /// <summary>日志插件：只认 LogContext</summary>
    public class LogPlugin
    {
        public string Name => "LogPlugin";

        public void Initialize(LogContext context)
        {
            Console.WriteLine($"  [{Name}] 初始化，路径={context.LogPath}，级别={context.LogLevel}");
        }
    }

    /// <summary>数据库插件：只认 DbContext</summary>
    public class DbPlugin
    {
        public string Name => "DbPlugin";

        public void Initialize(DbContext context)
        {
            Console.WriteLine($"  [{Name}] 初始化，连接={context.ConnectionString}，超时={context.TimeoutSeconds}s");
        }
    }

    /// <summary>缓存插件：只认 CacheContext</summary>
    public class CachePlugin
    {
        public string Name => "CachePlugin";

        public void Initialize(CacheContext context)
        {
            Console.WriteLine($"  [{Name}] 初始化，Redis={context.RedisUrl}，过期={context.ExpirationSeconds}s");
        }
    }


    // ========== 宿主：直接依赖每个具体类型 ==========
    // 没有接口，宿主必须认识：
    //   - 每个插件的具体类型（LogPlugin、DbPlugin、CachePlugin）
    //   - 每个上下文的具体类型（LogContext、DbContext、CacheContext）
    //   - 每个上下文怎么构造（需要哪些参数）
    public class PluginHost
    {
        private LogPlugin _logPlugin;
        private DbPlugin _dbPlugin;
        private CachePlugin _cachePlugin;

        public PluginHost(LogPlugin log, DbPlugin db, CachePlugin cache)
        {
            _logPlugin = log;
            _dbPlugin = db;
            _cachePlugin = cache;
        }

        public void StartAll()
        {
            Console.WriteLine("宿主开始调度\n");

            // 每个插件需要不同的上下文，宿主必须知道每个上下文怎么造
            var logContext = new LogContext("/var/log/app.log", "Info");
            _logPlugin.Initialize(logContext);

            var dbContext = new DbContext("Server=localhost;Db=app;", 30);
            _dbPlugin.Initialize(dbContext);

            var cacheContext = new CacheContext("redis://localhost:6379", 3600);
            _cachePlugin.Initialize(cacheContext);
        }
    }


    // ========== 主程序 ==========
    public class Program
    {
        public static void Main()
        {
            Console.WriteLine("========== 第1步：多种插件（无接口，上下文各不同） ==========\n");

            // --- 创建插件 ---
            var logPlugin = new LogPlugin();
            var dbPlugin = new DbPlugin();
            var cachePlugin = new CachePlugin();

            // --- 宿主调度 ---
            // 宿主内部为每个插件创建对应的上下文
            var host = new PluginHost(logPlugin, dbPlugin, cachePlugin);
            host.StartAll();

            // --- 分析 ---
            Console.WriteLine();
            Console.WriteLine("=== 分析 ===");
            Console.WriteLine();
            Console.WriteLine("【写法】无接口，每个插件有自己的上下文类型");
            Console.WriteLine();
            Console.WriteLine("  上下文（三个类，属性完全不同）：");
            Console.WriteLine("    LogContext   { string LogPath; string LogLevel; }");
            Console.WriteLine("    DbContext    { string ConnectionString; int TimeoutSeconds; }");
            Console.WriteLine("    CacheContext { string RedisUrl; int ExpirationSeconds; }");
            Console.WriteLine();
            Console.WriteLine("  插件（签名各不相同）：");
            Console.WriteLine("    LogPlugin.Initialize(LogContext c)");
            Console.WriteLine("    DbPlugin.Initialize(DbContext c)");
            Console.WriteLine("    CachePlugin.Initialize(CacheContext c)");
            Console.WriteLine("    → 参数类型不同，没有公共签名");
            Console.WriteLine();
            Console.WriteLine("  宿主：");
            Console.WriteLine("    private LogPlugin _logPlugin;");
            Console.WriteLine("    private DbPlugin _dbPlugin;");
            Console.WriteLine("    private CachePlugin _cachePlugin;");
            Console.WriteLine("    → 认识每个插件类型");
            Console.WriteLine();
            Console.WriteLine("    var logContext = new LogContext(...);");
            Console.WriteLine("    var dbContext = new DbContext(...);");
            Console.WriteLine("    var cacheContext = new CacheContext(...);");
            Console.WriteLine("    → 认识每个上下文类型，知道每个怎么造");
            Console.WriteLine();
            Console.WriteLine("【能跑吗？】能。功能完全正常。");
            Console.WriteLine();
            Console.WriteLine("【问题1：上下文创建写死在宿主里】");
            Console.WriteLine("  宿主里硬编码了：");
            Console.WriteLine("    new LogContext(\"/var/log/app.log\", \"Info\")");
            Console.WriteLine("    new DbContext(\"Server=localhost;Db=app;\", 30)");
            Console.WriteLine("    new CacheContext(\"redis://localhost:6379\", 3600)");
            Console.WriteLine("  → 配置硬编码，无法从配置文件/环境变量读取");
            Console.WriteLine("  → 换个环境（测试/生产）就要改宿主代码");
            Console.WriteLine();
            Console.WriteLine("【问题2：加插件要改宿主三处】");
            Console.WriteLine("  加一个 EmailPlugin + EmailContext：");
            Console.WriteLine("    ① 加字段 private EmailPlugin _emailPlugin;");
            Console.WriteLine("    ② 改构造函数加参数 EmailPlugin email");
            Console.WriteLine("    ③ StartAll 里加：");
            Console.WriteLine("       var emailCtx = new EmailContext(...);");
            Console.WriteLine("       _emailPlugin.Initialize(emailCtx);");
            Console.WriteLine("  → 宿主改动越来越大");
            Console.WriteLine();
            Console.WriteLine("【问题3：不能统一调度】");
            Console.WriteLine("  三个插件的 Initialize 签名不同：");
            Console.WriteLine("    Initialize(LogContext)    ← 只接受 LogContext");
            Console.WriteLine("    Initialize(DbContext)     ← 只接受 DbContext");
            Console.WriteLine("    Initialize(CacheContext)  ← 只接受 CacheContext");
            Console.WriteLine("  没有公共接口 → 不能放进同一个 List → 不能 foreach");
            Console.WriteLine();
            Console.WriteLine("【问题4：宿主知道得太多】");
            Console.WriteLine("  宿主知道：");
            Console.WriteLine("    - 有哪些插件（LogPlugin、DbPlugin、CachePlugin）");
            Console.WriteLine("    - 每个插件需要什么上下文（LogContext、DbContext、CacheContext）");
            Console.WriteLine("    - 每个上下文需要什么参数（路径、连接字符串、Redis地址...）");
            Console.WriteLine("    - 每个参数的值是什么（/var/log/app.log、localhost...）");
            Console.WriteLine("  → 宿主变成了'什么都知道'的上帝类");
            Console.WriteLine();
            Console.WriteLine("【问题5：无法替换上下文创建方式】");
            Console.WriteLine("  现在上下文在宿主里直接 new");
            Console.WriteLine("  想改成从配置文件创建？想改成从数据库读？想改成工厂创建？");
            Console.WriteLine("  → 都要改宿主代码");
            Console.WriteLine();
            Console.WriteLine("【对比：如果用接口 + 工厂】");
            Console.WriteLine("  接口：IPlugin { void Initialize(IPluginContext ctx); }");
            Console.WriteLine("  工厂：IPluginContext Create(IPlugin plugin)");
            Console.WriteLine("  宿主：foreach (var p in plugins) p.Initialize(factory.Create(p))");
            Console.WriteLine("  → 宿主不认识任何具体类型，不认识任何上下文");
            Console.WriteLine("  → 加插件只需注册，宿主代码不变");
            Console.WriteLine();
            Console.WriteLine("【结论】");
            Console.WriteLine("  上下文各不同 + 无接口 = 宿主必须知道所有细节");
            Console.WriteLine("  → 插件类型、上下文类型、上下文参数、参数值");
            Console.WriteLine("  → 这是耦合最重的写法");
            Console.WriteLine();
            Console.WriteLine("  解法演进：");
            Console.WriteLine("    ① 引入 IPlugin 接口 → 统一插件契约");
            Console.WriteLine("    ② 引入 IPluginContext 接口 → 统一上下文契约");
            Console.WriteLine("    ③ 引入工厂/策略 → 把上下文创建从宿主移出去");
            Console.WriteLine("    ④ 引入泛型 IPlugin<TContext> → 编译期类型安全");
        }
    }
}
