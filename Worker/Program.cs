using Worker.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;

string configFile = "appsettings.json";
foreach (var arg in args)
{
	if (arg.StartsWith("--config"))
	{
		var parts = arg.Split('=');
		if (parts.Length == 2)
			configFile = parts[1];
		else if (args.Length > 1)
			configFile = args[Array.IndexOf(args, arg) + 1];
	}
}

var builder = Host.CreateApplicationBuilder();
builder.Configuration.AddJsonFile(configFile, optional: false, reloadOnChange: true);
builder.Services.AddHostedService<Worker.Worker>();
builder.Services.AddDbContext<Worker.HandlerDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));


var host = builder.Build();
Worker.ServiceProviderAccessor.Instance = host.Services;
host.Run();
