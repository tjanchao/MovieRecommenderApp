using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using movieRecommender.Data;
using movieRecommender.Identity;
using movieRecommender.Security;
using movieRecommender.Seeding;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();

// 003-004 FR-005. Deny-by-default: a page is protected because nobody opened it up, not
// because somebody remembered to close it. The failure mode of the opposite choice is a
// *missing* attribute, and nothing in a code review or a test run draws attention to an
// attribute that isn't there (§3.1). The cost is that /Index, /Privacy, /Error and the
// three account pages each carry an explicit [AllowAnonymous].
builder.Services.AddAuthorization(options =>
    options.FallbackPolicy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());

// 003-004 FR-006: the acting user comes from the authentication cookie and from nowhere
// else. Scoped, because "who is acting" is a property of the request.
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, CurrentUser>();

// Persistence (ADR-0001). File-backed SQLite: accounts have to survive an application
// restart mid-demo (EC-15, SUC-003), and the same store serves the film cache in 005-008.
// Since 003-004 this is also where ownership is enforced (ADR-0003) — the context takes
// ICurrentUser and scopes every personal read to it.
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

// 003-004 FR-011 / FR-019. The fixture is loaded — and validated — only in Development;
// everywhere else it is empty, which is what switches seeding and one-click sign-in off.
// Both services are registered unconditionally so that the pages depending on them resolve
// in every environment and answer with a not-found rather than a resolution failure (EC-17).
builder.Services.AddSingleton<DemoSeedFixtureLoader>();
builder.Services.AddSingleton(serviceProvider =>
    serviceProvider.GetRequiredService<IHostEnvironment>().IsDevelopment()
        ? serviceProvider.GetRequiredService<DemoSeedFixtureLoader>().Load()
        : new DemoSeedFixture());
builder.Services.AddSingleton<DemoSignIn>();
builder.Services.AddScoped<DemoDataSeeder>();

var app = builder.Build();

// Single process on a single machine (NFR-006): migrating on start keeps the demo to
// one command. Revisit if this ever grows a second instance.
using (var scope = app.Services.CreateScope())
{
    scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().Database.Migrate();

    // 003-004 FR-011. Development only (FR-019), idempotent (FR-012), and it calls nothing
    // external (FR-013) — a fresh clone with no keys and no network still gets two accounts
    // with ratings and history.
    if (app.Environment.IsDevelopment())
    {
        try
        {
            await scope.ServiceProvider.GetRequiredService<DemoDataSeeder>().SeedAsync();
        }
        catch (Exception exception)
        {
            // NFR-004: abort startup naming what failed, rather than starting an unseeded
            // app. Discovering an empty demo at `dotnet run` is recoverable; discovering it
            // on stage is not.
            throw new InvalidOperationException(
                $"Demo seeding failed, so the application did not start: {exception.Message}", exception);
        }
    }
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

// 003-004 FR-009 / SC-009. After UseRouting, so the endpoint — and therefore whether the
// response may contain personal data — is known; before UseAuthorization, because the
// login redirect it issues for a protected page short-circuits the pipeline and would
// otherwise escape the header entirely.
app.UsePersonalDataCacheControl();

app.UseAuthentication();
app.UseStaleAuthCookieCleanup();
app.UseAuthorization();

// Stylesheets and scripts are not personal data. Without this the fallback policy above
// would demand a login for the site's own CSS.
app.MapStaticAssets().AllowAnonymous();
app.MapRazorPages()
   .WithStaticAssets();

app.Run();
