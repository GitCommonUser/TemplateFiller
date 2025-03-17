using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using TemplateFiller.Data;
using TemplateFiller.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<TemplateFillerContext>();
builder.Services.AddControllersWithViews();

builder.Services.AddScoped<ILogService, LogService>();

var app = builder.Build();


if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
