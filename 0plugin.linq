<Query Kind="Program" />

/*
//第1版  基础实现（直接调用）
void Main()
{
	SamplePlugin x = new SamplePlugin();
	x.Initialize(new SamplePluginContext("context"));

	CachePlugin x2 = new CachePlugin();
	x2.Initialize(new CachePluginContext("redisUrl", 0));

}

 

public class SamplePlugin  
{
	public void Initialize(SamplePluginContext context)
	{
		 
		Console.WriteLine($"  [{context.GetType().Name}] 初始化，Tag={context.Tag}");
	}
}


public class CachePlugin  
{

	public void Initialize(CachePluginContext context)
	{
		var newContext =context;
		Console.WriteLine($"  [{newContext.GetType().Name}] 初始化，Redis={(newContext).RedisUrl}，过期={(newContext).ExpirationSeconds}s");
	}
}




public class SamplePluginContext  
{
	public string Tag { get; }
	public SamplePluginContext(string tag)
	{
		Tag = tag;
	}
}

public class CachePluginContext  
{
	public CachePluginContext(string redisUrl, int expirationSeconds)
	{
		RedisUrl = redisUrl;
		ExpirationSeconds = expirationSeconds;
	}

	public string RedisUrl { get; }
	public int ExpirationSeconds { get; }
}




*/

/*
//第2版 引入接口统一管理
void Main()
{
	IPlugin samplePlugin=new SamplePlugin();
	 

	IPlugin cachePlugin = new CachePlugin();
	 

	var sequence = new List<IPlugin>() { samplePlugin, cachePlugin };
	var dic = new Dictionary<Type, IPluginContext>();
	dic.Add(typeof(SamplePlugin),new SamplePluginContext("context"));
	dic.Add(typeof(CachePlugin),new CachePluginContext("redisUrl",0));
	foreach (var element in sequence)
	{
		var pluginContext=dic[element.GetType()];
		element.Initialize(pluginContext);
	}
	
	
}

public interface IPlugin
{ 
	void Initialize(IPluginContext context);
	   
}

public interface IPluginContext
{	 

}

public class SamplePlugin :IPlugin
{
	public   void Initialize(IPluginContext context)
	{
		 var newContext=(SamplePluginContext)context;
		 Console.WriteLine($"  [{newContext.GetType().Name}] 初始化，Tag={newContext.Tag}");
	}
 }


public class CachePlugin :IPlugin
{
	 
	public   void Initialize(IPluginContext context)
	{
		var newContext=(CachePluginContext)context;
		Console.WriteLine($"  [{newContext.GetType().Name}] 初始化，Redis={(newContext).RedisUrl}，过期={(newContext).ExpirationSeconds}s");
	}
}


 

public class SamplePluginContext:IPluginContext
{
	public string Tag { get; }
	public SamplePluginContext(string tag)
	{
		Tag = tag;
	}
}

public class CachePluginContext:IPluginContext
{
	public CachePluginContext(string redisUrl, int expirationSeconds)
	{
		RedisUrl = redisUrl;
		ExpirationSeconds = expirationSeconds;
	}

	public string RedisUrl { get; }
	public int ExpirationSeconds { get; }
}







*/

/*
//第3版引入泛型抽象基类逻辑复用与封装

void Main()
{
	IPlugin samplePlugin=new SamplePlugin();
	 

	IPlugin cachePlugin = new CachePlugin();
	 

	var sequence = new List<IPlugin>() { samplePlugin, cachePlugin };
	var dic = new Dictionary<Type, IPluginContext>();
	dic.Add(typeof(SamplePlugin),new SamplePluginContext("context"));
	dic.Add(typeof(CachePlugin),new CachePluginContext("redisUrl",0));
	foreach (var element in sequence)
	{
		var pluginContext=dic[element.GetType()];
		element.Initialize(pluginContext);
	}
}

public interface IPlugin
{ 
	void Initialize(IPluginContext context);
}

public interface IPluginContext
{	

}

public abstract class BasicPlugin<TPluginContext> : IPlugin where TPluginContext : IPluginContext
{
	protected abstract void OnInitialize(TPluginContext context);
	
	void IPlugin.Initialize(IPluginContext context)
	{
		var newContext=(TPluginContext)context;
		this.OnInitialize(newContext);
	}
}

public class SamplePlugin :BasicPlugin<SamplePluginContext>
{
	protected override void OnInitialize(SamplePluginContext context)
	{
		Console.WriteLine($"  [{context.GetType().Name}] 初始化，Tag={context.Tag}");
	}
}


public class CachePlugin :BasicPlugin<CachePluginContext>
{
	protected override void OnInitialize(CachePluginContext context)
	{
			Console.WriteLine($"  [{context.GetType().Name}] 初始化，Redis={(context).RedisUrl}，过期={(context).ExpirationSeconds}s");
	}
}


 

public class SamplePluginContext:IPluginContext
{
	public string Tag { get; }
	public SamplePluginContext(string tag)
	{
		Tag = tag;
	}
}

public class CachePluginContext:IPluginContext
{
	public CachePluginContext(string redisUrl, int expirationSeconds)
	{
		RedisUrl = redisUrl;
		ExpirationSeconds = expirationSeconds;
	}

	public string RedisUrl { get; }
	public int ExpirationSeconds { get; }
}



*/






/*
//第4版引入泛型接口与逆变极致灵活

  void Main()
{
	IPlugin samplePlugin=new SamplePlugin();
	IPlugin cachePlugin = new CachePlugin();
	IPlugin<IPluginContext> commonPlugin = new CommonPlugin();
	 IPlugin<SamplePluginContext> samplePlugin2=commonPlugin;
	 samplePlugin2.Initialize(new SamplePluginContext("context"));
 
	 

	var sequence = new List<IPlugin>() { samplePlugin, cachePlugin };
	var dic = new Dictionary<Type, IPluginContext>();
	dic.Add(typeof(SamplePlugin),new SamplePluginContext("context"));
	dic.Add(typeof(CachePlugin),new CachePluginContext("redisUrl",0));
	foreach (var element in sequence)
	{
		var pluginContext=dic[element.GetType()];
		element.Initialize(pluginContext);
	}
}

public interface IPlugin
{ 
	void Initialize(IPluginContext context);
}
public interface IPlugin<in TPluginContext>:IPlugin where TPluginContext:IPluginContext
{
	void Initialize(TPluginContext context);
}

public interface IPluginContext
{	

}

public abstract class BasicPlugin<TPluginContext> : IPlugin<TPluginContext> where TPluginContext : IPluginContext
{
	public void Initialize(TPluginContext context)
	{
		this.OnInitialize(context);
	}

	protected abstract void OnInitialize(TPluginContext context);
	
	void IPlugin.Initialize(IPluginContext context)
	{
		var newContext=(TPluginContext)context;
		this.OnInitialize(newContext);
	}
}


public class CommonPlugin : BasicPlugin<IPluginContext>
{
	protected override void OnInitialize(IPluginContext context)
	{
		Console.WriteLine($"  [{context.GetType().Name}] 初始化");
	}
}


public class SamplePlugin :BasicPlugin<SamplePluginContext>
{
	protected override void OnInitialize(SamplePluginContext context)
	{
		Console.WriteLine($"  [{context.GetType().Name}] 初始化，Tag={context.Tag}");
	}
}


public class CachePlugin :BasicPlugin<CachePluginContext>
{
	protected override void OnInitialize(CachePluginContext context)
	{
			Console.WriteLine($"  [{context.GetType().Name}] 初始化，Redis={(context).RedisUrl}，过期={(context).ExpirationSeconds}s");
	}
}


 

public class SamplePluginContext:IPluginContext
{
	public string Tag { get; }
	public SamplePluginContext(string tag)
	{
		Tag = tag;
	}
}

public class CachePluginContext:IPluginContext
{
	public CachePluginContext(string redisUrl, int expirationSeconds)
	{
		RedisUrl = redisUrl;
		ExpirationSeconds = expirationSeconds;
	}

	public string RedisUrl { get; }
	public int ExpirationSeconds { get; }
}















*/


void Main()
{
	IPlugin samplePlugin = new SamplePlugin();
	IPlugin cachePlugin = new CachePlugin();
	IPlugin<IPluginContext> commonPlugin = new CommonPlugin();
	 IPlugin<SamplePluginContext> samplePlugin2=commonPlugin;
	 samplePlugin2.Initialize(new SamplePluginContext("context"));
 
	 CallPlugin<SamplePluginContext>(commonPlugin,new SamplePluginContext("context"));

	var sequence = new List<IPlugin>() { samplePlugin, cachePlugin };
	var dic = new Dictionary<Type, IPluginContext>();
	dic.Add(typeof(SamplePlugin),new SamplePluginContext("context"));
	dic.Add(typeof(CachePlugin),new CachePluginContext("redisUrl",0));
	foreach (var element in sequence)
	{
		var pluginContext=dic[element.GetType()];
		element.Initialize(pluginContext);
	}
}

public interface IPlugin
{ 
	void Initialize(IPluginContext context);
}
public interface IPlugin<in TPluginContext>:IPlugin where TPluginContext:IPluginContext
{
	void Initialize(TPluginContext context);
}

public interface IPluginContext
{	

}

public abstract class BasicPlugin<TPluginContext> : IPlugin<TPluginContext> where TPluginContext : IPluginContext
{
	public void Initialize(TPluginContext context)
	{
		this.OnInitialize(context);
	}

	protected abstract void OnInitialize(TPluginContext context);
	
	void IPlugin.Initialize(IPluginContext context)
	{
		if(context is TPluginContext newContext)
		this.OnInitialize(newContext);
	}
}

 
  void CallPlugin<TContext>(IPlugin<TContext> plugin, TContext context)
	where TContext : IPluginContext
{

	plugin.Initialize(context);  // 类型安全
}

public class CommonPlugin : BasicPlugin<IPluginContext>
{
	protected override void OnInitialize(IPluginContext context)
	{
		Console.WriteLine($"  [{context.GetType().Name}] 初始化");
	}
}


public class SamplePlugin :BasicPlugin<SamplePluginContext>
{
	protected override void OnInitialize(SamplePluginContext context)
	{
		Console.WriteLine($"  [{context.GetType().Name}] 初始化，Tag={context.Tag}");
	}
}


public class CachePlugin :BasicPlugin<CachePluginContext>
{
	protected override void OnInitialize(CachePluginContext context)
	{
			Console.WriteLine($"  [{context.GetType().Name}] 初始化，Redis={(context).RedisUrl}，过期={(context).ExpirationSeconds}s");
	}
}


 

public class SamplePluginContext:IPluginContext
{
	public string Tag { get; }
	public SamplePluginContext(string tag)
	{
		Tag = tag;
	}
}

public class CachePluginContext:IPluginContext
{
	public CachePluginContext(string redisUrl, int expirationSeconds)
	{
		RedisUrl = redisUrl;
		ExpirationSeconds = expirationSeconds;
	}

	public string RedisUrl { get; }
	public int ExpirationSeconds { get; }
}



