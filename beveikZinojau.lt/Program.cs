using beveikZinojau.lt.Data;
using beveikZinojau.lt.Scoring;
using beveikZinojau.lt.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using beveikZinojau.lt.Extensions;
using beveikZinojau.lt.Services.Geography;
using beveikZinojau.lt.Services.Accentuation;

var builder = WebApplication.CreateBuilder(args);

//Connect to supabase
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");


//Identity services
builder.Services.AddIdentity<AppUser, IdentityRole>(options =>
{
    options.Password.RequiredLength = 12;
    options.User.RequireUniqueEmail = true;
})
.AddEntityFrameworkStores<AppDbContext>()
.AddDefaultTokenProviders();

//Login session cookie. Api calls get 401/403 instead of redirect to login
builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.HttpOnly = true;
    options.ExpireTimeSpan = TimeSpan.FromDays(7);
    options.SlidingExpiration = true;

    options.Events.OnRedirectToLogin = context =>
    {
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        return Task.CompletedTask;
    };
    options.Events.OnRedirectToAccessDenied = context =>
    {
        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        return Task.CompletedTask;
    };
});

builder.Services.AddApplicationServices(builder.Configuration);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));
// Container services
builder.Services.AddControllers();
builder.Services.AddScoped<GameService>();
builder.Services.AddScoped<AuthService>();

builder.Services.AddSignalR();


// Allowing the React dev server
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactApp", policy =>
    {
        policy.WithOrigins(
            "http://localhost:5173",
            "http://localhost:5174"
        )
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials();
    });
});

builder.Services.AddMemoryCache();

builder.Services.AddSingleton<ICountryRepository, JsonCountryRepository>();
builder.Services.AddSingleton<GeoQuestionGenerator>();
builder.Services.AddScoped<GeoGameService>();

// Accentuation services
builder.Services.AddSingleton<ParserService>();
builder.Services.AddSingleton<IAccentuationRepository, JsonAccentuationRepository>();
builder.Services.AddScoped<AccentuationGameService>();

var app = builder.Build();

// Generate level1.json - level5.json from .docx files on startup if they don't exist yet
var parser = app.Services.GetRequiredService<ParserService>();
await parser.GenerateAllLevelsJsonAsync();

// HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts(); // Histories (default: 30 days)
}

// app.UseHttpsRedirection();

app.UseRouting();

// Enable the CORS policy to allow React
app.UseCors("AllowReactApp");

app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();

app.MapHub<GameHub>("/gamehub");

app.Run();
