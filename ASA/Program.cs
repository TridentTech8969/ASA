using IndustrialSolutions.Email;
using IndustrialSolutions.Hubs;
using IndustrialSolutions.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
//builder.Services.Configure<SmtpOptions>(builder.Configuration.GetSection("SmtpOptions"));
builder.Services.Configure<EmailImapOptions>(o =>
{
    o.Username = "youremail@gmail.com"; // TODO: set yours
    o.AppPassword = "xxxxxxxxxxxxxxxx"; // TODO: set yours
    o.FilterLabel = "Contact Form"; // or null to include all INBOX
});
builder.Services.AddSignalR();
builder.Services.AddSingleton<EmailCache>();
builder.Services.AddSingleton<ImapEmailReader>();
builder.Services.AddHostedService<EmailSyncService>();
var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.MapHub<EmailHub>("/hubs/email");
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
