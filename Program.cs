using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using movieRecommender.Data;
using movieRecommender.Identity;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();

// Persistence (ADR-0001). File-backed SQLite: accounts have to survive an application
// restart mid-demo (EC-15, SUC-003), and the same store serves the film cache in 005-008.
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

// Authentication (ADR-0002). Identity supplies FR-004, FR-015 and FR-017; the cookie
// schemes are wired up without AddIdentity so no role tables come along (§1.3).
builder.Services.AddAuthentication(IdentityConstants.ApplicationScheme)
    .AddIdentityCookies();

builder.Services.AddIdentityCore<ApplicationUser>(options =>
{
    // FR-002: length only, no composition rules. Identity's defaults (6 chars plus
    // digit, upper, lower and symbol) are overridden wholesale — see §3.1.
    options.Password.RequiredLength = 8;
    options.Password.RequireDigit = false;
    options.Password.RequireLowercase = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequiredUniqueChars = 1;

    // FR-015: five consecutive failures, five minutes. Short on purpose — a mistyped
    // password on stage should not end the demo.
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
    options.Lockout.AllowedForNewUsers = true;

    // §5.4: uniqueness is enforced at the store, so two concurrent sign-ups still
    // yield one account (EC-6).
    options.User.RequireUniqueEmail = true;
    // The username is the normalized email. Identity's default character allow-list
    // would reject otherwise-valid addresses, so it is switched off.
    options.User.AllowedUserNameCharacters = string.Empty;

    // No mail transport exists on localhost, which is why confirmation is a non-goal
    // rather than deferred work (§9.2).
    options.SignIn.RequireConfirmedAccount = false;
    options.SignIn.RequireConfirmedEmail = false;
    options.SignIn.RequireConfirmedPhoneNumber = false;
})
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddSignInManager()
    .AddDefaultTokenProviders();

// Must follow AddIdentityCore, which registers the stock factory this replaces.
builder.Services.AddScoped<IUserClaimsPrincipalFactory<ApplicationUser>, AppUserClaimsPrincipalFactory>();

// FR-017. Secure is unconditional, which is why the https profile is the default one:
// over plain http the cookie is stored and never returned, so login appears to succeed
// and instantly forgets the user (EC-17).
builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.Name = "movieRecommender.auth";
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Lax;

    // FR-010: "Remember me" persists 14 days, sliding on activity. Without it the
    // handler issues a session cookie instead and these two never apply (FR-009).
    options.ExpireTimeSpan = TimeSpan.FromDays(14);
    options.SlidingExpiration = true;

    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.ReturnUrlParameter = "returnUrl";
});

// EC-8: a cookie whose user is gone from the store resolves to anonymous on the next
// request, not up to 30 minutes later (Identity's default revalidation interval).
// Affordable at the scale of NFR-006.
builder.Services.Configure<SecurityStampValidatorOptions>(options =>
    options.ValidationInterval = TimeSpan.Zero);

// NFR-002: one verification should cost 50–250 ms. Pinned rather than inherited so the
// tension with NFR-001's 500 ms budget stays visible to whoever reads this next.
builder.Services.Configure<PasswordHasherOptions>(options => options.IterationCount = 210_000);

// NFR-003: see DecoyPasswordHash.
builder.Services.AddSingleton<DecoyPasswordHash>();

// NFR-004 / EC-15: keys on disk rather than in memory, so restarting the app does not
// sign every "Remember me" user out mid-demo.
var keyRingPath = Path.Combine(builder.Environment.ContentRootPath, "App_Data", "keys");
Directory.CreateDirectory(keyRingPath);
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(keyRingPath))
    .SetApplicationName("movieRecommender");

var app = builder.Build();

// Single process on a single machine (NFR-006): migrating on start keeps the demo to
// one command. Revisit if this ever grows a second instance.
using (var scope = app.Services.CreateScope())
{
    scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().Database.Migrate();
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseRouting();

app.UseAuthentication();
app.UseStaleAuthCookieCleanup();
app.UseAuthorization();

app.MapStaticAssets();
app.MapRazorPages()
   .WithStaticAssets();

app.Run();
