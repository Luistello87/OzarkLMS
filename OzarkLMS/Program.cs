using Microsoft.EntityFrameworkCore;
using OzarkLMS.Data;
using OzarkLMS.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddSignalR();
builder.Services.AddScoped<ISelfTestService, SelfTestService>();

builder.Services.AddDbContext<OzarkLMS.Data.AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddAuthentication(Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromMinutes(60);
    });

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<OzarkLMS.Data.AppDbContext>();
        // OzarkLMS.Data.DbInitializer.Initialize(context);
        
        // Backfill Vote Counts (One-time fix for existing data)
        context.Database.ExecuteSqlRaw(
            "UPDATE \"Posts\" p " +
            "SET \"UpvoteCount\" = (SELECT COUNT(*) FROM \"PostVotes\" pv WHERE pv.\"PostId\" = p.\"Id\" AND pv.\"Value\" = 1), " +
            "    \"DownvoteCount\" = (SELECT COUNT(*) FROM \"PostVotes\" pv WHERE pv.\"PostId\" = p.\"Id\" AND pv.\"Value\" = -1)");

    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred creating the DB.");
    }
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles(); // Enable static files for uploads
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapHub<OzarkLMS.Hubs.VoteHub>("/voteHub");

app.Run();
