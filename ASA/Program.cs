using IndustrialSolutions.Models.Entities;
using IndustrialSolutions.Services;
using IndustrialSolutions.Hubs;
using Microsoft.EntityFrameworkCore;
// Use alias to avoid namespace conflicts
using EmailConfig = IndustrialSolutions.Email.EmailImapOptions;

var builder = WebApplication.CreateBuilder(args);

// Configure Database with the scaffolded context
builder.Services.AddDbContext<IndustrialSolutionsEmailsContext>(options =>
{
    // Use SQL Server with your connection string
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
        ?? "Server=(localdb)\\mssqllocaldb;Database=IndustrialSolutionsEmails;Trusted_Connection=true;MultipleActiveResultSets=true;TrustServerCertificate=true";

    options.UseSqlServer(connectionString, sqlOptions =>
    {
        // Add retry on failure for transient errors
        sqlOptions.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(30),
            errorNumbersToAdd: null);
    });

    // For development - enable sensitive data logging
    if (builder.Environment.IsDevelopment())
    {
        options.EnableSensitiveDataLogging();
        options.EnableDetailedErrors();
    }
});

// Configure Email IMAP Options
builder.Services.Configure<EmailConfig>(o =>
{
    o.Username = "eternalvision2025@gmail.com";
    o.AppPassword = "gvvy enkz fjjo iccp";
    o.FilterLabel = null; // Start with null
    o.SyncIntervalSeconds = 60; // Sync every minute
    o.Host = "imap.gmail.com";
    o.Port = 993;
    o.UseSsl = true;
    o.TimeZoneId = "India Standard Time"; // Adjust as needed
    o.MaxEmailsToFetch = 200;
});

// Add MVC and SignalR
builder.Services.AddControllersWithViews();
builder.Services.AddSignalR();

// Register services with proper scoping
builder.Services.AddScoped<IEmailRepository, EmailRepository>();
builder.Services.AddSingleton<ImapEmailReader>();

// Register EmailSyncService as both singleton and hosted service
builder.Services.AddSingleton<EmailSyncService>();
builder.Services.AddHostedService<EmailSyncService>(provider =>
    provider.GetRequiredService<EmailSyncService>());

// Add logging
builder.Services.AddLogging(logging =>
{
    logging.AddConsole();
    logging.AddDebug();
    if (builder.Environment.IsDevelopment())
    {
        logging.SetMinimumLevel(LogLevel.Information);
    }
});

var app = builder.Build();

// Initialize database
await InitializeDatabaseAsync(app);

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
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();

// Database initialization method
static async Task InitializeDatabaseAsync(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<IndustrialSolutionsEmailsContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    try
    {
        logger.LogInformation("Checking database connection...");

        // Test if we can connect to the database
        await context.Database.CanConnectAsync();
        logger.LogInformation("Database connection successful!");

        // Check if database exists and create if not
        var created = await context.Database.EnsureCreatedAsync();
        if (created)
        {
            logger.LogInformation("Database created successfully!");

            // Insert sample data if database was just created
            await SeedSampleDataAsync(context, logger);
        }
        else
        {
            logger.LogInformation("Database already exists.");
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Database initialization failed. Error: {Error}", ex.Message);

        // Provide helpful error message
        if (ex.Message.Contains("cannot open database") || ex.Message.Contains("login failed"))
        {
            logger.LogError("Database connection failed. Please ensure:");
            logger.LogError("1. SQL Server LocalDB is installed and running");
            logger.LogError("2. Connection string is correct");
            logger.LogError("3. Database permissions are properly configured");
            logger.LogError("Current connection string: {ConnectionString}",
                context.Database.GetConnectionString());
        }

        // Don't crash the application, but log the error
        logger.LogWarning("Application will continue without database connectivity.");
    }
}

// Seed sample data
static async Task SeedSampleDataAsync(IndustrialSolutionsEmailsContext context, ILogger logger)
{
    try
    {
        // Check if we already have data
        if (await context.Emails.AnyAsync())
        {
            logger.LogInformation("Sample data already exists, skipping seed.");
            return;
        }

        logger.LogInformation("Seeding sample data...");

        // Add sample emails
        var sampleEmails = new[]
        {
            new IndustrialSolutions.Models.Entities.Email
            {
                UniqueEmailId = "sample1@INBOX",
                GmailUid = 1001,
                Folder = "INBOX",
                FromName = "Contact from Website",
                FromEmail = "contact@yourwebsite.com",
                Subject = "Industrial Equipment Inquiry",
                Snippet = "Hello, I am interested in your industrial solutions...",
                ReceivedUtc = DateTime.UtcNow.AddHours(-2),
                ReceivedLocal = DateTime.UtcNow.AddHours(-2).ToString("dd-MM-yyyy HH:mm"),
                Unread = true,
                HasAttachments = false,
                Company = "ABC Manufacturing Ltd",
                Phone = "+91-9876543210",
                Message = "We are looking for industrial equipment suppliers for our new manufacturing unit.",
                IsContactForm = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new IndustrialSolutions.Models.Entities.Email
            {
                UniqueEmailId = "sample2@INBOX",
                GmailUid = 1002,
                Folder = "INBOX",
                FromName = "John Smith",
                FromEmail = "john.smith@example.com",
                Subject = "Product Catalog Request",
                Snippet = "Could you please send me your latest product catalog?",
                ReceivedUtc = DateTime.UtcNow.AddHours(-5),
                ReceivedLocal = DateTime.UtcNow.AddHours(-5).ToString("dd-MM-yyyy HH:mm"),
                Unread = false,
                HasAttachments = false,
                IsContactForm = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }
        };

        context.Emails.AddRange(sampleEmails);
        await context.SaveChangesAsync();

        logger.LogInformation("Sample data seeded successfully! Added {Count} sample emails.", sampleEmails.Length);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Failed to seed sample data: {Error}", ex.Message);
    }
}