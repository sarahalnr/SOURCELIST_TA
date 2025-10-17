
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Negotiate;
using Microsoft.Extensions.FileSystemGlobbing.Internal.Patterns;
using sourcelist.Infrastructure;
using sourcelist.Models;
using sourcelist.Provider;
using sourcelist.Services;
using BCrypt.Net;
using sourcelist.Data;
using Microsoft.EntityFrameworkCore;


var builder = WebApplication.CreateBuilder(args); 

builder.Services.AddControllersWithViews();

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login"; 
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Home/AccessDenied"; 
        options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
    });

builder.Services.AddSingleton<IConnectionString, ConnectionString>();

builder.Services.AddScoped<ISourceListService, SourceListService>();

builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<ISupplierService, SupplierService>();


builder.Services.Configure<LDAPOptions>(builder.Configuration.GetSection("LDAP"));
builder.Services.AddScoped<ILDAPService, LDAPService>();

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(120); 
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

builder.Services.AddAuthorization(); builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddControllersWithViews();

var app = builder.Build();

// app.UsePathBase("/e-fair"); 

string hashBaru = BCrypt.Net.BCrypt.HashPassword("password123");
Console.WriteLine("$2a$10$N9qo8uLOickgx2ZMRZoMyeIjZAgcfl7p92ldGxad68LJZdL17lhWy");
Console.WriteLine(hashBaru);
Console.WriteLine("$2a$10$N9qo8uLOickgx2ZMRZoMyeIjZAgcfl7p92ldGxad68LJZdL17lhWy");

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Account}/{action=Login}/{id?}");

app.Run();

