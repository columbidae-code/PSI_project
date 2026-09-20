var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls("http://localhost:5159");

// Container services
builder.Services.AddControllers();

// Allowing the React dev server
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactApp", policy =>
    {
        policy.WithOrigins("http://localhost:5173") // Vite default port
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

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

app.UseAuthorization();

app.MapControllers();

app.Run();
