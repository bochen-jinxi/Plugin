using System;
using System.Collections.Generic;

namespace Step2_3Demo
{
    // ============================================================
    // 第3步：泛型抽象类，确保强类型（不使用泛型接口）
    // ============================================================
    //
    // 【与第2步的对比】
    //
    // 第2步（Step2.2Demo）：非泛型接口，插件内部强转
    //   IPlugin.Initialize(IPluginContext context)
    //   → 插件内部: var logCtx = (LogContext)context;  ← 运行时强转，可能崩溃
    //
    // 第3步（Step2.3Demo）：泛型抽象类，编译期类型安全
    //   PluginBase<TContext> : IPlugin  ← 直接实现非泛型接口
    //   → 子类用 OnInitialize(TContext) 直接访问属性，不用强转
    //   → 显式接口实现做类型守卫
    //
    // 【两层设计】
    //   IPlugin                ← 非泛型接口，宿主用它统一调度（管"相遇"）
    //   PluginBase<TContext>   ← 泛型抽象类，强类型 + 公共逻辑 + 类型守卫（管"翻译"）
    //
    // 【不使用泛型接口】
    //   不需要 IPlugin<in TContext>
    //   泛型抽象类本身就提供了强类型
    //   PluginBase<LogContext> 的 OnInitialize(LogContext) 已经保证类型安全
    // ============================================================


    // ========== 接口定义 ==========

    /// <summary>上下文描述符：把 Source 和 Detail 两个 string 封装成一个值类型</summary>
    /// <remarks>
    /// 为什么用 readonly struct：
    ///   1. 值类型，栈分配，没有堆开销
    ///   2. 不可变 → 天生线程安全
    ///   3. 语义上是"一个值"（Source+Detail 配对），不是"一个有身份的对象"
    ///   4. 不能为 null，避免空引用
    ///
    /// 合并的好处：
    ///   - 接口从 2 个属性变 1 个，更简洁
    ///   - 以后加字段只改 struct，接口不变
    ///   - 可以给 struct 加方法（IsValid/ToDisplayString）
    /// </remarks>
    public readonly struct ContextDescriptor
    {
        /// <summary>目标地址：日志路径 / 连接字符串 / Redis 地址</summary>
        public string Source { get; }

        /// <summary>附加参数：日志级别 / 超时秒数 / 过期秒数</summary>
        public string Detail { get; }

        public ContextDescriptor(string source, string detail)
        {
            Source = source;
            Detail = detail;
        }

        /// <summary>格式化显示</summary>
        public override string ToString() => $"Source={Source}, Detail={Detail}";
    }

    /// <summary>统一上下文接口（薄契约：只有一个 Descriptor 属性）</summary>
    /// <remarks>
    /// 为什么用接口而非抽象类：
    ///   1. 契约薄——只有一个属性 Descriptor，没有行为，接口够用
    ///   2. 不变量已被 ContextDescriptor 承担——readonly struct 保证 Source+Detail 不可变
    ///   3. 不需要抽象类构造函数来"强制初始化"——struct 默认值不崩，只读属性必须构造时赋值
    /// </remarks>
    public interface IPluginContext
    {
        /// <summary>上下文描述符（封装 Source + Detail）</summary>
        ContextDescriptor Descriptor { get; }
    }

    /// <summary>非泛型插件接口：宿主用它统一调度</summary>
    public interface IPlugin
    {
        string Name { get; }
        void Initialize(IPluginContext context);  // ← 宿主通过这个调用
    }

    // 注意：没有 IPlugin<in TContext> 泛型接口
    // 泛型抽象类 PluginBase<TContext> 直接实现 IPlugin


    // ========== 上下文：构造函数 2 个参数全部放入接口 ==========
    // 第一个参数 → Source（string）
    // 第二个参数 → Detail（string，int 转 string）
    // 具体类仍保留类型安全的特有属性供插件内部使用

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


    // ========== 泛型抽象基类：直接实现 IPlugin ==========
    // 这是第3步的核心：PluginBase<TContext> : IPlugin
    //
    // 不需要泛型接口，泛型抽象类本身就提供强类型：
    //   1. protected abstract void OnInitialize(TContext context)
    //      → 子类用强类型 TContext，不用强转
    //
    //   2. public void Initialize(TContext context)
    //      → 外部代码可以直接用强类型调用
    //
    //   3. void IPlugin.Initialize(IPluginContext context)  ← 显式接口实现
    //      → 宿主通过 IPlugin 调用时走这里
    //      → 内部做类型检查 + 委托给 OnInitialize

    public abstract class PluginBase<TContext> : IPlugin
        where TContext : IPluginContext
    {
        // 子类提供插件名
        public abstract string Name { get; }

        // 子类实现具体的初始化逻辑，参数是强类型 TContext，不用强转！
        protected abstract void OnInitialize(TContext context);

        // 强类型初始化方法：编译期安全
        // 这是普通 public 方法（不是接口方法），有具体类型引用时可以直接调用
        public void Initialize(TContext context)
        {
            // context.Descriptor.Source 可以直接访问，因为 TContext : IPluginContext 有 Descriptor 属性
            Console.WriteLine($"  [{Name}] Initialize(TContext) 被调用，TContext = {typeof(TContext).Name}，Source = {context.Descriptor.Source}");
            OnInitialize(context);  // ← 直接传 TContext，不用强转
        }

        // 显式接口实现：IPlugin.Initialize(IPluginContext)
        // 宿主通过 IPlugin 接口调用时走这个方法
        // 注意：这是显式接口实现，只能通过 IPlugin 类型访问，不能通过 PluginBase<T> 访问
        void IPlugin.Initialize(IPluginContext context)
        {
            // 类型检查：传入的 context 是否是 TContext？
            if (!(context is TContext typedContext))
            {
                throw new ArgumentException(
                    $"上下文类型不匹配。期望 {typeof(TContext).Name}，实际 {context.GetType().Name}。",
                    nameof(context));
            }

            // 类型匹配 → 委托给强类型方法
            Initialize(typedContext);
        }

      
    }


    // ========== 插件：继承泛型基类，不用强转 ==========
    // 对比第2步：
    //   第2步：public class LogPlugin : IPlugin
    //          void Initialize(IPluginContext context) { var ctx = (LogContext)context; ... }
    //          → 手动强转
    //
    //   第3步：public class LogPlugin : PluginBase<LogContext>
    //          protected override void OnInitialize(LogContext context) { ... }
    //          → 直接用 LogContext，不用强转！

    public class LogPlugin : PluginBase<LogContext>
    {
        public override string Name => "LogPlugin";

        protected override void OnInitialize(LogContext context)
        {
            // context.Descriptor.Source：目标地址（日志路径）
            // context.Descriptor.Detail：附加参数（日志级别）
            Console.WriteLine($"    路径={context.Descriptor.Source}，级别={context.Descriptor.Detail}");
        }
    }

    public class DbPlugin : PluginBase<DbContext>
    {
        public override string Name => "DbPlugin";

        protected override void OnInitialize(DbContext context)
        {
            // context.Descriptor.Source：通过描述符访问连接字符串
            // context.TimeoutSeconds 是 DbContext 特有属性
            Console.WriteLine($"    连接={context.Descriptor.Source}，超时={context.TimeoutSeconds}s");
        }
    }

    public class CachePlugin : PluginBase<CacheContext>
    {
        public override string Name => "CachePlugin";

        protected override void OnInitialize(CacheContext context)
        {
            // context.Descriptor.Source：通过描述符访问 Redis 地址
            // context.ExpirationSeconds 是 CacheContext 特有属性
            Console.WriteLine($"    Redis={context.Descriptor.Source}，过期={context.ExpirationSeconds}s");
        }
    }


    // ========== 宿主：只依赖非泛型 IPlugin ==========
    // AddPlugin 同时接收插件和上下文，一次注册搞定
    // 不需要先 AddPlugin 再传 pairs
    public class PluginHost
    {
        private readonly List<(IPlugin Plugin, IPluginContext Context)> _plugins
            = new List<(IPlugin, IPluginContext)>();

        // 一次注册：插件 + 它的上下文
        public void AddPlugin(IPlugin plugin, IPluginContext context)
        {
            _plugins.Add((plugin, context));
        }

        // 统一调度：不需要外部传参，用注册时存的配对
        public void StartAll()
        {
            Console.WriteLine($"宿主开始调度，共 {_plugins.Count} 个插件\n");

            foreach (var (plugin, context) in _plugins)
            {
                // 宿主通过 IPluginContext.Descriptor 访问，不用强转
                Console.WriteLine($"  调度 {plugin.Name}，Descriptor = {context.Descriptor}");
                // 宿主通过 IPlugin.Initialize(IPluginContext) 调用
                plugin.Initialize(context);
            }
        }
    }


    // ========== 主程序 ==========
    public class Program
    {
        public static void Main()
        {
            Console.WriteLine("========== 第3步：泛型抽象类，确保强类型（无泛型接口） ==========\n");

            // --- 创建上下文 ---
            var logContext = new LogContext("/var/log/app.log", "Info");
            var dbContext = new DbContext("Server=localhost;Db=app;", 30);
            var cacheContext = new CacheContext("redis://localhost:6379", 3600);

            // --- 创建插件 ---
            // LogPlugin : PluginBase<LogContext> : IPlugin
            var logPlugin = new LogPlugin();
            var dbPlugin = new DbPlugin();
            var cachePlugin = new CachePlugin();

            // --- 强类型方法调用：编译期类型安全 ---
            Console.WriteLine("【Demo A】强类型方法调用（编译期类型安全）\n");
            logPlugin.Initialize(logContext);     // ← Initialize(LogContext)
            dbPlugin.Initialize(dbContext);       // ← Initialize(DbContext)
            cachePlugin.Initialize(cacheContext); // ← Initialize(CacheContext)

            // --- 编译期类型安全演示 ---
            Console.WriteLine("\n【Demo B】编译期类型安全\n");
            Console.WriteLine("  如果取消注释下面这行：");
            Console.WriteLine("    logPlugin.Initialize(dbContext);  ← 编译错误！");
            Console.WriteLine("  错误: 无法从 DbContext 转换为 LogContext");
            Console.WriteLine("  → 编译期就报错，不用等运行时");

            // logPlugin.Initialize(dbContext);  // ← 编译错误！CS1503

            // --- 宿主统一调度：通过非泛型 IPlugin ---
            Console.WriteLine("\n【Demo C】宿主通过 IPlugin 统一调度\n");
            var host = new PluginHost();
            // 一次注册：插件 + 上下文，不用分两步
            host.AddPlugin(logPlugin, logContext);
            host.AddPlugin(dbPlugin, dbContext);
            host.AddPlugin(cachePlugin, cacheContext);
            host.StartAll();

            // --- 类型不匹配时的友好报错 ---
            Console.WriteLine("\n【Demo D】类型不匹配时的友好报错\n");
            try
            {
                IPlugin plugin = logPlugin;
                plugin.Initialize(dbContext);  // ← 传了 DbContext 给 LogPlugin
            }
            catch (ArgumentException ex)
            {
                Console.WriteLine($"  [异常] {ex.Message}");
                Console.WriteLine("  → 不是 InvalidCastException，而是 ArgumentException");
                Console.WriteLine("  → 错误信息明确指出期望类型和实际类型");
            }

            // --- 分析 ---
            Console.WriteLine("\n=== 分析 ===\n");
            PrintAnalysis();
        }

        static void PrintAnalysis()
        {
            Console.WriteLine("【第2步 vs 第3步 逐行对比】");
            Console.WriteLine();
            Console.WriteLine("  接口（不变）：");
            Console.WriteLine("    IPlugin { void Initialize(IPluginContext ctx); }");
            Console.WriteLine("    → 第2步和第3步都用这个非泛型接口");
            Console.WriteLine("    → 第3步没有新增泛型接口 IPlugin<in TContext>");
            Console.WriteLine();
            Console.WriteLine("  插件基类（第3步新增）：");
            Console.WriteLine("    public abstract class PluginBase<TContext> : IPlugin");
            Console.WriteLine("    {");
            Console.WriteLine("      // 抽象方法：子类用强类型 TContext");
            Console.WriteLine("      protected abstract void OnInitialize(TContext ctx);");
            Console.WriteLine();
            Console.WriteLine("      // 强类型方法：外部代码可以直接调用");
            Console.WriteLine("      public void Initialize(TContext ctx) { OnInitialize(ctx); }");
            Console.WriteLine();
            Console.WriteLine("      // 显式接口实现：宿主通过 IPlugin 调用时走这里");
            Console.WriteLine("      void IPlugin.Initialize(IPluginContext ctx)");
            Console.WriteLine("      {");
            Console.WriteLine("        if (!(ctx is TContext t)) throw ...;");
            Console.WriteLine("        Initialize(t);  // 委托给强类型方法");
            Console.WriteLine("      }");
            Console.WriteLine("    }");
            Console.WriteLine();
            Console.WriteLine("  插件：");
            Console.WriteLine("    第2步: public class LogPlugin : IPlugin");
            Console.WriteLine("           {");
            Console.WriteLine("             void Initialize(IPluginContext context)");
            Console.WriteLine("             {");
            Console.WriteLine("               var ctx = (LogContext)context;  ← 手动强转");
            Console.WriteLine("               ctx.LogPath ...");
            Console.WriteLine("             }");
            Console.WriteLine("           }");
            Console.WriteLine();
            Console.WriteLine("    第3步: public class LogPlugin : PluginBase<LogContext>");
            Console.WriteLine("           {");
            Console.WriteLine("             protected override void OnInitialize(LogContext context)");
            Console.WriteLine("             {");
            Console.WriteLine("               context.LogPath ...  ← 直接用，不用强转");
            Console.WriteLine("             }");
            Console.WriteLine("           }");
            Console.WriteLine();
            Console.WriteLine("【泛型抽象类的三个作用】");
            Console.WriteLine();
            Console.WriteLine("  作用1：强类型");
            Console.WriteLine("    OnInitialize(LogContext context) ← 参数是具体类型");
            Console.WriteLine("    → 直接访问 context.LogPath，不用强转");
            Console.WriteLine("    → 编译期保证类型安全");
            Console.WriteLine();
            Console.WriteLine("  作用2：公共逻辑复用");
            Console.WriteLine("    PluginBase<TContext> 可以放公共逻辑（日志、防重复初始化等）");
            Console.WriteLine("    → 子类不用重复写");
            Console.WriteLine();
            Console.WriteLine("  作用3：显式接口实现做类型守卫");
            Console.WriteLine("    void IPlugin.Initialize(IPluginContext context)");
            Console.WriteLine("    → 宿主通过 IPlugin 调用时，先检查类型再委托");
            Console.WriteLine("    → 类型不匹配抛 ArgumentException（比 InvalidCastException 明确）");
            Console.WriteLine();
            Console.WriteLine("【为什么不需要泛型接口？】");
            Console.WriteLine("    泛型抽象类 PluginBase<TContext> 已经提供了强类型");
            Console.WriteLine("    OnInitialize(TContext) 的参数就是具体类型");
            Console.WriteLine("    → 子类直接用，不用强转");
            Console.WriteLine("    → 编译期保证类型安全");
            Console.WriteLine();
            Console.WriteLine("    如果有泛型接口 IPlugin<in TContext>：");
            Console.WriteLine("    → 额外多一层接口抽象");
            Console.WriteLine("    → 可以用 IPlugin<LogContext> 做类型安全的引用");
            Console.WriteLine("    → 但对于本 demo，泛型抽象类已经够用");
            Console.WriteLine();
            Console.WriteLine("【两层设计各司其职】");
            Console.WriteLine("    IPlugin              ← 宿主统一调度（管'相遇'）");
            Console.WriteLine("    PluginBase<TContext> ← 强类型 + 公共逻辑 + 类型守卫（管'翻译'）");
            Console.WriteLine();
            Console.WriteLine("【类型安全对比】");
            Console.WriteLine("    第1步：Initialize(LogContext)     ← 安全但强耦合");
            Console.WriteLine("    第2步：Initialize(IPluginContext) ← 松耦合但要强转");
            Console.WriteLine("    第3步：OnInitialize(TContext)     ← 既松耦合又安全（泛型抽象类）");
        }
    }
}
