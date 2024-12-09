using PatientVideoChatApp.DapperContexts;
using PatientVideoChatApp.Hubs;
using PatientVideoChatApp.IRepository;
using PatientVideoChatApp.Repository;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddScoped<IApiRepository, ApiRepository>();
builder.Services.AddScoped<DapperContext>();
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", builder =>
        builder.SetIsOriginAllowed(_ => true) 
               .AllowAnyMethod()             
               .AllowAnyHeader()             
               .AllowCredentials());         
});
builder.Services.AddSignalR();
var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}
app.UseStaticFiles();

app.UseCors("AllowAll");

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=JoinVideoCall}/{id?}");

app.MapHub<VideoHub>("/video-chat");

app.Run();
