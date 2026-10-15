using AssetIQ.Business.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

var csvRelativePath = builder.Configuration["AssetIQ:Dataset:RollingMillCsvRelativePath"]
    ?? "../../data/rolling-mill/AssetIQ_RollingMill_2000Rows_Demo.csv";
var csvAbsolutePath = Path.GetFullPath(Path.Combine(builder.Environment.ContentRootPath, csvRelativePath));

builder.Services.AddControllersWithViews();
builder.Services.AddAssetIQBusiness(builder.Configuration, csvAbsolutePath);

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Dashboard}/{action=Index}/{id?}");

app.Run();
