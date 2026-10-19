using beveikZinojau.lt.Data;
using beveikZinojau.lt.Scoring;
using beveikZinojau.lt.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

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
builder.Services.AddScoped<StatsTracker>();
// Scoring
builder.Services.Configure<ScoringConfig>(builder.Configuration.GetSection("Scoring"));
builder.Services.AddSingleton<ScoringCalculator>();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));
// Container services
builder.Services.AddControllers();
builder.Services.AddScoped<GameService>();
builder.Services.AddScoped<AuthService>();

builder.Services.AddSignalR();
builder.Services.AddSingleton<GameRoomManager>();


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

var app = builder.Build();

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
