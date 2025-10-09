
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Negotiate;
using Microsoft.Extensions.FileSystemGlobbing.Internal.Patterns;
using sourcelist.Infrastructure;
using sourcelist.Models;
using sourcelist.Provider;
using sourcelist.Services;


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


builder.Services.Configure<LDAPOptions>(builder.Configuration.GetSection("LDAP"));
builder.Services.AddScoped<ILDAPService, LDAPService>();

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(120); 
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

builder.Services.AddAuthorization();

var app = builder.Build();

// app.UsePathBase("/e-fair"); 

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

