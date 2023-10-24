public class Startup
{
    public Startup(IConfiguration configuration)
    {
        Configuration = configuration;
    }

    public IConfiguration Configuration { get; }

    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSwaggerGen();
    }

    public void Configure(WebApplication app)
    {
        app.MapControllers();
        app.UseHttpsRedirection();      
        app.Run();
    }
}