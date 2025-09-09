
using sourcelist.Models;
using sourcelist.Services;
using Microsoft.AspNetCore.Authentication.Negotiate;
using sourcelist.Infrastructure;
using sourcelist.Provider;


var builder = WebApplication.CreateBuilder(args); 

builder.Services.AddControllersWithViews();

builder.Services.AddAuthentication(NegotiateDefaults.AuthenticationScheme)
    .AddNegotiate();

builder.Services.AddSingleton<IConnectionString, ConnectionString>();

builder.Services.AddScoped<ISourceListService, SourceListService>();


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
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
