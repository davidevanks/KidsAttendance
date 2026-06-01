using KidsAttendance.Application;
using KidsAttendance.Infrastructure;
using KidsAttendance.Infrastructure.Persistence;
using KidsAttendance.Infrastructure.Persistence.Entities;
using KidsAttendance.Infrastructure.Security;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews(options =>
{
    options.ModelBindingMessageProvider.SetValueMustNotBeNullAccessor(_ => "Este campo es requerido.");
    options.ModelBindingMessageProvider.SetValueIsInvalidAccessor(_ => "El valor ingresado no es válido.");
    options.ModelBindingMessageProvider.SetAttemptedValueIsInvalidAccessor((value, field) => $"El valor '{value}' no es válido para {field}.");
    options.ModelBindingMessageProvider.SetMissingBindRequiredValueAccessor(field => $"El campo {field} es obligatorio.");
    options.ModelBindingMessageProvider.SetMissingKeyOrValueAccessor(() => "Se requiere un valor para este campo.");
});
builder.Services.AddHttpContextAccessor();
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services
    .AddIdentityCore<AppUser>(options =>
    {
        options.Password.RequireDigit = false;
        options.Password.RequireLowercase = false;
        options.Password.RequireUppercase = false;
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequiredLength = 1;
        options.Password.RequiredUniqueChars = 1;
        options.User.RequireUniqueEmail = false;
        options.User.AllowedUserNameCharacters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        options.Lockout.AllowedForNewUsers = false;
        options.Lockout.MaxFailedAccessAttempts = int.MaxValue;
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.Zero;
    })
    .AddRoles<AppRole>()
    .AddEntityFrameworkStores<KidsAttendanceDbContext>()
    .AddSignInManager<SignInManager<AppUser>>()
    .AddDefaultTokenProviders();
builder.Services
    .AddAuthentication(IdentityConstants.ApplicationScheme)
    .AddCookie(IdentityConstants.ApplicationScheme, options =>
    {
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.SlidingExpiration = true;
        options.ExpireTimeSpan = TimeSpan.FromHours(24);
    });
builder.Services.AddAuthorization();
builder.Services.Configure<AdminSeedOptions>(builder.Configuration.GetSection(AdminSeedOptions.SectionName));
builder.Services.AddScoped<AdminSeedService>();

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

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Dashboard}/{action=Index}/{id?}")
    .WithStaticAssets();

using (var scope = app.Services.CreateScope())
{
    var adminSeedService = scope.ServiceProvider.GetRequiredService<AdminSeedService>();
    await adminSeedService.EnsureSeedAsync();
}

app.Run();
