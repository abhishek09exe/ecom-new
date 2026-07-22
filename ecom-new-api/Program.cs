using Microsoft.EntityFrameworkCore;
using ecom_new_api.Configuration;
using ecom_new_api.Data;
using ecom_new_api.Middleware;
using ecom_new_api.Repositories;
using ecom_new_api.Services;

var builder = WebApplication.CreateBuilder(args);

// ── Configuration ───────────────────────────────────────────────────────────────
builder.Services.AddSingleton<ICartOrderValidationConfig>(
    new CartOrderValidationConfig(builder.Configuration));

// ── Database (Entity Framework Core) ────────────────────────────────────────────────
builder.Services.AddDbContext<CartOrderDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"))
);

// ── MVC / Swagger ───────────────────────────────────────────────────────────────
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// ── Repositories ────────────────────────────────────────────────────────────────────
// CartOrderDbContext is registered above and ready for dependency injection.
// CartOrderRepository now provides Section 1 lookup methods (database access only).
builder.Services.AddScoped<ICartOrderRepository, CartOrderRepository>();

// ── Services ────────────────────────────────────────────────────────────────────
builder.Services.AddScoped<CartOrderPreparationService>();      // Section 1: Data loading
builder.Services.AddScoped<ProductDeterminationService>();      // Section 2.1: Primary product dates
builder.Services.AddScoped<ICartOrderService, CartOrderService>();

// ── Middleware pipeline (not yet implemented) ────────────────────────────────────
// TODO: REPLACE WITH ACTUAL — register these middleware classes once implemented:
//   builder.Services.AddScoped<CartBootstrapMiddleware>();   // session / vendor_order_code
//   builder.Services.AddScoped<CsrfValidationMiddleware>();  // X-WRCART-CSRF header
//   builder.Services.AddScoped<CsiAuthMiddleware>();         // X-CSI-USER / X-CSI-USER-ID
//   builder.Services.AddScoped<PermissionMiddleware>();      // cart_order.create check
//   builder.Services.AddScoped<AccountContextMiddleware>();  // username, csi_user_id, p_rc, trx_rc
//   builder.Services.AddScoped<LocaleMiddleware>();          // X-CSI-LOCALE header

var app = builder.Build();

// ── Exception Handler (must be first!) ─────────────────────────────────────────────
// ✅ ADDED: Global exception handler to catch and log unhandled exceptions
// Returns proper error response without exposing stack traces to clients
app.UseMiddleware<ExceptionHandlerMiddleware>();

// ── HTTP pipeline ────────────────────────────────────────────────────────────────
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => 
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Ecom Cart API v1");
        c.RoutePrefix = "swagger";
    });
}
else
{
    app.UseHttpsRedirection();
}

// TODO: REPLACE WITH ACTUAL — add middleware here in order:
//   app.UseMiddleware<CartBootstrapMiddleware>();
//   app.UseMiddleware<CsrfValidationMiddleware>();
//   app.UseMiddleware<CsiAuthMiddleware>();
//   app.UseMiddleware<PermissionMiddleware>();
//   app.UseMiddleware<AccountContextMiddleware>();
//   app.UseMiddleware<LocaleMiddleware>();

app.MapControllers();

app.Run();
