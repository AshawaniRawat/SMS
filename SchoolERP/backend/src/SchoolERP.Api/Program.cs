using SchoolERP.Api.Extensions;
using SchoolERP.Application.Common;
using SchoolERP.Infrastructure;
using Serilog;
var builder = WebApplication.CreateBuilder(args);
builder.Host.UseSerilog((context, logger) => logger.ReadFrom.Configuration(context.Configuration).WriteTo.Console());
builder.Services.AddApplicationServices().AddInfrastructureServices(builder.Configuration, builder.Environment).AddApiServices(builder.Configuration);
var app = builder.Build();
await app.ApplyDatabaseMigrationsAsync();
app.UseApiPipeline();
app.Run();
public partial class Program { }
