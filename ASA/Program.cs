using IndustrialSolutions.Email;
using IndustrialSolutions.Services;
using IndustrialSolutions.Hubs;

var builder = WebApplication.CreateBuilder(args);

// Configure Email IMAP Options
builder.Services.Configure<EmailImapOptions>(o =>
{
    o.Username = "eternalvision2025@gmail.com";
    o.AppPassword = "gvvy enkz fjjo iccp";
    o.FilterLabel = null; // Start with null
});

builder.Services.AddControllersWithViews();
builder.Services.AddSignalR();

// ADD THESE LINES - they're probably missing!
builder.Services.AddSingleton<EmailCache>();
builder.Services.AddSingleton<ImapEmailReader>();
builder.Services.AddHostedService<EmailSyncService>(); // This one is critical!

var app = builder.Build();

// Configure pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

// app.UseAuthentication(); // Uncomment when you add authentication
// app.UseAuthorization();   // Uncomment when you add authentication

// Map SignalR hub
app.MapHub<EmailHub>("/hubs/email");

// Map routes
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Emails}/{action=Index}/{id?}");

app.Run();