using ecom_new_api.Data;
using ecom_new_api.Infrastructure;
using ecom_new_api.Repositories;
using ecom_new_api.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// ── MVC / Swagger ───────────────────────────────────────────────────────────────
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = SnakeCaseNamingPolicy.Instance;
        options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// ── EF Core DbContext ────────────────────────────────────────────────────────────
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("EcomDb"),
        sql => sql.CommandTimeout(60)));

// ── Repositories ────────────────────────────────────────────────────────────────
builder.Services.AddScoped<ICartOrderRepository, CartOrderRepository>();

// ── Services ────────────────────────────────────────────────────────────────────
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

// ── HTTP pipeline ────────────────────────────────────────────────────────────────
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Ecom Cart API v1"));
}

app.UseHttpsRedirection();

// TODO: REPLACE WITH ACTUAL — add middleware here in order:
//   app.UseMiddleware<CartBootstrapMiddleware>();
//   app.UseMiddleware<CsrfValidationMiddleware>();
//   app.UseMiddleware<CsiAuthMiddleware>();
//   app.UseMiddleware<PermissionMiddleware>();
//   app.UseMiddleware<AccountContextMiddleware>();
//   app.UseMiddleware<LocaleMiddleware>();

app.MapControllers();

app.Run();
