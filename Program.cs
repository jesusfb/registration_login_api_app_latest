using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using WebApi.Authorization;
using WebApi.Helpers;
using WebApi.Services;

var builder = WebApplication.CreateBuilder(args);

// add services to DI container
{
    var services = builder.Services;
    var env = builder.Environment;
 
    // use sql server db in production and sqlite db in development
    //if (env.IsProduction())
    //    services.AddDbContext<DataContext>();
    //else
   services.AddDbContext<DataContext>();
 
    services.AddCors();
    services.AddControllers();

    builder.Services.AddEndpointsApiExplorer();
    /*builder.Services.AddSwaggerGen(c => {
        c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            Name = "Authorization",
            Type = SecuritySchemeType.Http,
            Scheme = "Bearer",
            BearerFormat = "JWT",
            In = ParameterLocation.Header,
            Description = "JWT Authorization header using the Bearer scheme."
        });
        c.AddSecurityRequirement(new OpenApiSecurityRequirement
                 {
                     {
                           new OpenApiSecurityScheme
                             {
                                 Reference = new OpenApiReference
                                 {
                                     Type = ReferenceType.SecurityScheme,
                                     Id = "Bearer"
                                 }
                             },
                             new string[] {}
                     }
                 });
    });
*/
    services.AddSwaggerGen(options =>  
{  
options.EnableAnnotations();  
using (var serviceProvider = services.BuildServiceProvider())  
{  
var provider = serviceProvider.GetRequiredService<IApiVersionDescriptionProvider>();  
String assemblyDescription = typeof(Startup).Assembly.GetCustomAttribute<AssemblyDescriptionAttribute>().Description;  
foreach (var description in provider.ApiVersionDescriptions)  
{  
options.SwaggerDoc(description.GroupName, new Microsoft.OpenApi.Models.OpenApiInfo()  
{  
Title = $"{typeof(Startup).Assembly.GetCustomAttribute<AssemblyProductAttribute>().Product} {description.ApiVersion}",  
Version = description.ApiVersion.ToString(),  
Description = description.IsDeprecated ? $"{assemblyDescription} - DEPRECATED" : $"{assemblyDescription}"  
});  
}  
}  
options.AddSecurityDefinition("basic", new OpenApiSecurityScheme  
{  
Name = "Authorization",  
Type = SecuritySchemeType.Http,  
Scheme = "basic",  
In = ParameterLocation.Header,  
Description = "Basic Authorization header using the Bearer scheme."  
});  
options.AddSecurityRequirement(new OpenApiSecurityRequirement  
{  
{  
new OpenApiSecurityScheme  
{  
Reference = new OpenApiReference  
{  
Type = ReferenceType.SecurityScheme,  
Id = "basic"  
}  
},  
new string[] {}  
}  
              });  
  
            });  

    // configure automapper with all automapper profiles from this assembly
    services.AddAutoMapper(typeof(Program));

    // configure strongly typed settings object
    services.Configure<AppSettings>(builder.Configuration.GetSection("AppSettings"));

    // configure DI for application services
    services.AddScoped<IJwtUtils, JwtUtils>();
    services.AddScoped<IUserService, UserService>();
}

var app = builder.Build();

// migrate any database changes on startup (includes initial db creation)
using (var scope = app.Services.CreateScope())
{
    var dataContext = scope.ServiceProvider.GetRequiredService<DataContext>();    
    dataContext.Database.Migrate();
}
/*
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var context = services.GetRequiredService<DataContext>();
    context.Database.Migrate();
}*/
// configure HTTP request pipeline
{
    // global cors policy
    app.UseCors(x => x
        .AllowAnyOrigin()
        .AllowAnyMethod()
        .AllowAnyHeader());

    // global error handler
    app.UseMiddleware<ErrorHandlerMiddleware>();

    // custom jwt auth middleware
    app.UseMiddleware<JwtMiddleware>();

    app.MapControllers();
}

app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "v1");
    options.RoutePrefix = string.Empty;
});
//app.UseSwaggerUI();
app.Run();
