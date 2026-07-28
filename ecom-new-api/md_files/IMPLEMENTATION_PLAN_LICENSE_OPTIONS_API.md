# Implementation Plan: GET /license-options Endpoint

## 🎯 Objective

Implement the `GET /license-options` endpoint in the ecom-new-api C# project to replace the existing PHP API endpoint.

**Current PHP endpoint:** `GET https://cartapi.webroot.com/license-options?locale=en_US&message_key=A5A3CD6F-788D-4D20-9E32-3362E88ED732`

**Target C# endpoint:** `GET /api/v1/license-options?locale=en_US&message_key=A5A3CD6F-788D-4D20-9E32-3362E88ED732`

---

## 📋 Current Architecture (Already Implemented)

```
Controller → Service → Repository → EF Core → SQL Server DB
```

**Working endpoints:**
- ✅ `POST /cart/cart-orders` (creates cart orders)
- ✅ `SelectCartOrderAsync` (internal, reads cart after creation)

**Missing endpoints:**
- ❌ `GET /license-options` (CRITICAL - blocks frontend)
- ❌ `GET /configure`
- ❌ `GET /upgrade`

---

## 🔍 API Contract Analysis

### Request Parameters

```
GET /license-options?locale=en_US&message_key=A5A3CD6F-788D-4D20-9E32-3362E88ED732

Query Parameters:
- locale: string (BCP-47 format, e.g., "en_US", "fr_CA", "de_DE")
- message_key: string (UUID format, the license key)
```

### Response Structure (from real production data)

```json
{
  "license": { /* Current license details */ },
  "license_verified": true,
  "license_profile": {
    "SAEP": { /* Primary product profile */ },
    "PLRM": { /* Secondary product profile */ },
    "PLRCB": { /* Secondary product profile */ },
    "OTEDR": { /* Secondary product profile */ },
    "OTMDR": { /* Secondary product profile */ },
    "SDNS": { /* Secondary product profile */ },
    "SECA": { /* Secondary product profile */ }
  },
  "license_site_id": null,
  "upgrade_categories": {
    "OTMDRC": { /* Upgrade option */ },
    "SAEP": { /* Upgrade option */ },
    /* ... more upgrade options */
  },
  "billing_models": {
    "11": { /* Annual billing */ },
    "12": { /* Monthly billing */ }
  }
}
```

---

## 📁 Project Structure

Create the following files in your existing ecom-new-api project:

```
ecom-new-api/
├── Controllers/
│   └── LicenseController.cs                    (NEW - to be created)
├── Services/
│   ├── ILicenseService.cs                      (NEW - to be created)
│   └── LicenseService.cs                       (NEW - to be created)
├── Repositories/
│   ├── ILicenseRepository.cs                   (NEW - to be created)
│   └── EfLicenseRepository.cs                  (NEW - to be created)
├── Models/
│   ├── DTOs/
│   │   ├── LicenseOptionsResponse.cs           (NEW - to be created)
│   │   ├── LicenseDto.cs                       (NEW - to be created)
│   │   ├── LicenseProfileDto.cs                (NEW - to be created)
│   │   ├── UpgradeCategoryDto.cs               (NEW - to be created)
│   │   └── BillingModelDto.cs                  (NEW - to be created)
│   └── Entities/
│       ├── License.cs                          (NEW - to be created)
│       ├── LicenseCategory.cs                  (NEW - to be created)
│       └── LicenseProfile.cs                   (NEW - to be created)
└── Program.cs                                   (UPDATE - register new services)
```

---

## 🔨 Implementation Steps

### Step 1: Create DTOs (Data Transfer Objects)

These classes define the API response structure.

#### **File: `Models/DTOs/LicenseDto.cs`**

```csharp
namespace EcomNewApi.Models.DTOs;

public class LicenseDto
{
    public string Keycode { get; set; }
    public string ProductLineDescription { get; set; }
    public int LicenseStatusId { get; set; }
    public string LicenseTypeDescription { get; set; }
    public int LicenseKeycodeTypeId { get; set; }
    public int MaxDailyActivations { get; set; }
    public DateTime? LicenseExpirationDate { get; set; }
    public string? ParentKeycode { get; set; }
    public int LicenseSeats { get; set; }
    public int? ConsumedSeats { get; set; }
    public int SeatsUsed { get; set; }
    public int? StorageGb { get; set; }
    public string LicenseCategoryName { get; set; }
    public string LicenseCategoryDescription { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string CapabilityTypeDescription { get; set; }
    public string LicenseKey { get; set; }
    public string? LicenseAttributeDescription { get; set; }
    public string? LicenseAttributeTag { get; set; }
    public int? LicenseAttributeLicenseValue { get; set; }
    public string? LicenseAttributeLicenseValueDescription { get; set; }
    public DateTime? LicenseAttributeLastModified { get; set; }
    public string? OemType { get; set; }
    public int PortalFlag { get; set; }
    public int RenewalCount { get; set; }
    public string LicenseOriginChannelName { get; set; }
    public DateTime LicenseOriginalActivationDate { get; set; }
    public int EmailOptIn { get; set; }
    public string LicenseDistributionMethodCode { get; set; }
    public DateTime? NextBillDate { get; set; }
    public int DaysRemaining { get; set; }
    public bool IsExpired { get; set; }
}
```

#### **File: `Models/DTOs/LicenseProfileDto.cs`**

```csharp
namespace EcomNewApi.Models.DTOs;

public class LicenseProfileDto
{
    public string LicenseCategoryName { get; set; }
    public string LicenseCategoryDescription { get; set; }
    public int LicenseSeats { get; set; }
    public int? StorageGb { get; set; }
    public int LicenseKeycodeTypeId { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime ExpirationDate { get; set; }
    public int? LicenseAttributeId { get; set; }
    public string? LicenseAttributeDescription { get; set; }
    public int? LicenseAttributeLicenseValue { get; set; }
    public string? LicenseAttributeLicenseValueDescription { get; set; }
    public string CategoryTypeName { get; set; }
    public int ItemHierarchyId { get; set; }
    public string ItemHierarchyName { get; set; }
    public int LicenseStatusId { get; set; }
    public string LicenseStatusDescription { get; set; }
    public string? AutorenewalCycleName { get; set; }
    public string? AutorenewalCycle { get; set; }
    public int? UsagePricingModelId { get; set; }
    public string? UsagePricingModelName { get; set; }
    public int? RetentionModelId { get; set; }
    public string? RetentionModelName { get; set; }
    public int? RetentionTerm { get; set; }
    public int? RetentionModelTypeId { get; set; }
    public int? ProductPlatformId { get; set; }
    public string? ProductPlatformName { get; set; }
    public int? LicenseAutorenewalValue { get; set; }
    public int LicenseCategoryId { get; set; }
    public int? ProductPricingLevelId { get; set; }
    public string? PricingLevel { get; set; }
    public string? PricingLevelDescription { get; set; }
    public string? LicenseVaultJson { get; set; }
    public int MostRecentOrderTerm { get; set; }
}
```

#### **File: `Models/DTOs/UpgradeCategoryDto.cs`**

```csharp
namespace EcomNewApi.Models.DTOs;

public class UpgradeCategoryDto
{
    public string LicenseCategoryName { get; set; }
    public string UpgradeLicenseCategoryName { get; set; }
    public int ItemHierarchyId { get; set; }
    public string ItemHierarchyName { get; set; }
}
```

#### **File: `Models/DTOs/BillingModelDto.cs`**

```csharp
namespace EcomNewApi.Models.DTOs;

public class BillingModelDto
{
    public int ProductTypeId { get; set; }
    public string ProductTypeDescription { get; set; }
    public string LicenseAttributeDescription { get; set; }
    public int LicenseAttributeLicenseValue { get; set; }
    public string LicenseAttributeLicenseValueDescription { get; set; }
}
```

#### **File: `Models/DTOs/LicenseOptionsResponse.cs`**

```csharp
namespace EcomNewApi.Models.DTOs;

public class LicenseOptionsResponse
{
    public LicenseDto License { get; set; }
    public bool LicenseVerified { get; set; }
    public Dictionary<string, LicenseProfileDto> LicenseProfile { get; set; }
    public string? LicenseSiteId { get; set; }
    public Dictionary<string, UpgradeCategoryDto> UpgradeCategories { get; set; }
    public Dictionary<string, BillingModelDto> BillingModels { get; set; }
}
```

---

### Step 2: Create Entity Models

These represent database tables.

#### **File: `Models/Entities/License.cs`**

```csharp
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EcomNewApi.Models.Entities;

[Table("license")]
public class License
{
    [Key]
    [Column("license_id")]
    public int LicenseId { get; set; }

    [Column("license_key")]
    [MaxLength(36)]
    public string LicenseKey { get; set; }

    [Column("keycode")]
    [MaxLength(40)]
    public string Keycode { get; set; }

    [Column("license_category_id")]
    public int LicenseCategoryId { get; set; }

    [Column("license_seats")]
    public int LicenseSeats { get; set; }

    [Column("storage_gb")]
    public int? StorageGb { get; set; }

    [Column("license_status_id")]
    public int LicenseStatusId { get; set; }

    [Column("license_keycode_type_id")]
    public int LicenseKeycodeTypeId { get; set; }

    [Column("start_date")]
    public DateTime StartDate { get; set; }

    [Column("end_date")]
    public DateTime EndDate { get; set; }

    [Column("license_expiration_date")]
    public DateTime? LicenseExpirationDate { get; set; }

    [Column("parent_keycode")]
    [MaxLength(40)]
    public string? ParentKeycode { get; set; }

    [Column("max_daily_activations")]
    public int MaxDailyActivations { get; set; }

    [Column("consumed_seats")]
    public int? ConsumedSeats { get; set; }

    [Column("seats_used")]
    public int SeatsUsed { get; set; }

    [Column("portal_flag")]
    public int PortalFlag { get; set; }

    [Column("renewal_count")]
    public int RenewalCount { get; set; }

    [Column("license_original_activation_date")]
    public DateTime LicenseOriginalActivationDate { get; set; }

    [Column("email_opt_in")]
    public int EmailOptIn { get; set; }

    [Column("next_bill_date")]
    public DateTime? NextBillDate { get; set; }

    // Navigation properties
    [ForeignKey("LicenseCategoryId")]
    public LicenseCategory? LicenseCategory { get; set; }
}
```

#### **File: `Models/Entities/LicenseCategory.cs`**

```csharp
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EcomNewApi.Models.Entities;

[Table("license_category")]
public class LicenseCategory
{
    [Key]
    [Column("license_category_id")]
    public int LicenseCategoryId { get; set; }

    [Column("license_category_name")]
    [MaxLength(50)]
    public string LicenseCategoryName { get; set; }

    [Column("license_category_description")]
    [MaxLength(255)]
    public string LicenseCategoryDescription { get; set; }

    [Column("product_line_id")]
    public int? ProductLineId { get; set; }

    [Column("item_hierarchy_id")]
    public int? ItemHierarchyId { get; set; }
}
```

---

### Step 3: Create Repository Layer

#### **File: `Repositories/ILicenseRepository.cs`**

```csharp
using EcomNewApi.Models.Entities;

namespace EcomNewApi.Repositories;

public interface ILicenseRepository
{
    /// <summary>
    /// Get license by license key (UUID format)
    /// </summary>
    Task<License?> GetLicenseByKeyAsync(string licenseKey);

    /// <summary>
    /// Get license by keycode (alphanumeric format)
    /// </summary>
    Task<License?> GetLicenseByKeycodeAsync(string keycode);

    /// <summary>
    /// Get all license profiles (primary + secondary products) for a given keycode
    /// </summary>
    Task<Dictionary<string, object>> GetLicenseProfilesAsync(string keycode);

    /// <summary>
    /// Get available upgrade categories for a license
    /// </summary>
    Task<Dictionary<string, object>> GetUpgradeCategoriesAsync(string licenseCategoryName, int licenseSeats);

    /// <summary>
    /// Get billing models (annual vs monthly)
    /// </summary>
    Task<Dictionary<string, object>> GetBillingModelsAsync();
}
```

#### **File: `Repositories/EfLicenseRepository.cs`**

```csharp
using EcomNewApi.Data;
using EcomNewApi.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace EcomNewApi.Repositories;

public class EfLicenseRepository : ILicenseRepository
{
    private readonly EcomDbContext _context;

    public EfLicenseRepository(EcomDbContext context)
    {
        _context = context;
    }

    public async Task<License?> GetLicenseByKeyAsync(string licenseKey)
    {
        return await _context.Licenses
            .Include(l => l.LicenseCategory)
            .FirstOrDefaultAsync(l => l.LicenseKey == licenseKey);
    }

    public async Task<License?> GetLicenseByKeycodeAsync(string keycode)
    {
        return await _context.Licenses
            .Include(l => l.LicenseCategory)
            .FirstOrDefaultAsync(l => l.Keycode == keycode);
    }

    public async Task<Dictionary<string, object>> GetLicenseProfilesAsync(string keycode)
    {
        // TODO: This query needs to replicate the stored procedure logic
        // For now, return empty dictionary - will be implemented in Phase 2
        
        // The stored procedure likely queries multiple tables:
        // - license (main table)
        // - license_category (product types)
        // - license_attribute (settings like auto-renewal)
        // - usage_pricing_model (usage-based pricing)
        // - retention_model (backup retention)
        // - product_platform (platform-specific configs)
        
        // Example query structure:
        var profiles = await _context.Licenses
            .Where(l => l.Keycode == keycode)
            .Select(l => new
            {
                l.LicenseCategory.LicenseCategoryName,
                Profile = new
                {
                    l.LicenseCategory.LicenseCategoryName,
                    l.LicenseCategory.LicenseCategoryDescription,
                    l.LicenseSeats,
                    l.StorageGb,
                    l.LicenseKeycodeTypeId,
                    l.StartDate,
                    ExpirationDate = l.EndDate,
                    // Add more fields as needed
                }
            })
            .ToDictionaryAsync(x => x.LicenseCategoryName, x => (object)x.Profile);

        return profiles;
    }

    public async Task<Dictionary<string, object>> GetUpgradeCategoriesAsync(string licenseCategoryName, int licenseSeats)
    {
        // TODO: Query the database for available upgrade options
        // This likely involves:
        // - license_category_upgrade table (maps current category → upgrade options)
        // - Filtering by seat count (some upgrades only available at certain tiers)
        
        // Placeholder implementation
        return new Dictionary<string, object>();
    }

    public async Task<Dictionary<string, object>> GetBillingModelsAsync()
    {
        // TODO: Query billing model configuration
        // Likely stored in: license_attribute table or billing_model table
        
        // Hardcoded for now - replace with actual query
        return new Dictionary<string, object>
        {
            ["11"] = new
            {
                ProductTypeId = 2,
                ProductTypeDescription = "Renewal",
                LicenseAttributeDescription = "Auto Billing",
                LicenseAttributeLicenseValue = 11,
                LicenseAttributeLicenseValueDescription = "Overage"
            },
            ["12"] = new
            {
                ProductTypeId = 2,
                ProductTypeDescription = "Renewal",
                LicenseAttributeDescription = "Auto Billing",
                LicenseAttributeLicenseValue = 12,
                LicenseAttributeLicenseValueDescription = "Utility"
            }
        };
    }
}
```

---

### Step 4: Create Service Layer

#### **File: `Services/ILicenseService.cs`**

```csharp
using EcomNewApi.Models.DTOs;

namespace EcomNewApi.Services;

public interface ILicenseService
{
    /// <summary>
    /// Get license options for the configure page
    /// </summary>
    /// <param name="messageKey">License key (UUID format)</param>
    /// <param name="locale">Locale string (e.g., "en_US")</param>
    /// <returns>License options response with profiles and upgrade paths</returns>
    Task<LicenseOptionsResponse> GetLicenseOptionsAsync(string messageKey, string locale);
}
```

#### **File: `Services/LicenseService.cs`**

```csharp
using EcomNewApi.Models.DTOs;
using EcomNewApi.Repositories;

namespace EcomNewApi.Services;

public class LicenseService : ILicenseService
{
    private readonly ILicenseRepository _licenseRepository;
    private readonly ILogger<LicenseService> _logger;

    public LicenseService(ILicenseRepository licenseRepository, ILogger<LicenseService> logger)
    {
        _licenseRepository = licenseRepository;
        _logger = logger;
    }

    public async Task<LicenseOptionsResponse> GetLicenseOptionsAsync(string messageKey, string locale)
    {
        _logger.LogInformation("Getting license options for message_key={MessageKey}, locale={Locale}", 
            messageKey, locale);

        // Step 1: Parse locale (e.g., "en_US" → language: "en", country: "US")
        var (languageCode, countryCode) = ParseLocale(locale);
        _logger.LogDebug("Parsed locale: language={Language}, country={Country}", 
            languageCode, countryCode);

        // Step 2: Get license by key
        var license = await _licenseRepository.GetLicenseByKeyAsync(messageKey);
        if (license == null)
        {
            _logger.LogWarning("License not found for message_key={MessageKey}", messageKey);
            throw new NotFoundException($"License not found: {messageKey}");
        }

        _logger.LogInformation("Found license: keycode={Keycode}, category={Category}, seats={Seats}",
            license.Keycode, license.LicenseCategory?.LicenseCategoryName, license.LicenseSeats);

        // Step 3: Get license profiles (primary + secondary products)
        var licenseProfiles = await _licenseRepository.GetLicenseProfilesAsync(license.Keycode);
        _logger.LogDebug("Retrieved {Count} license profiles", licenseProfiles.Count);

        // Step 4: Get upgrade categories (available upgrade paths)
        var upgradeCategories = await _licenseRepository.GetUpgradeCategoriesAsync(
            license.LicenseCategory?.LicenseCategoryName ?? "", 
            license.LicenseSeats);
        _logger.LogDebug("Retrieved {Count} upgrade categories", upgradeCategories.Count);

        // Step 5: Get billing models
        var billingModels = await _licenseRepository.GetBillingModelsAsync();

        // Step 6: Calculate days remaining
        var daysRemaining = (license.EndDate - DateTime.UtcNow).Days;
        var isExpired = daysRemaining < 0;

        // Step 7: Map to DTO
        var response = new LicenseOptionsResponse
        {
            License = new LicenseDto
            {
                Keycode = license.Keycode,
                ProductLineDescription = license.LicenseCategory?.LicenseCategoryDescription ?? "",
                LicenseStatusId = license.LicenseStatusId,
                LicenseTypeDescription = "OEM", // TODO: Get from database
                LicenseKeycodeTypeId = license.LicenseKeycodeTypeId,
                MaxDailyActivations = license.MaxDailyActivations,
                LicenseExpirationDate = license.LicenseExpirationDate,
                ParentKeycode = license.ParentKeycode,
                LicenseSeats = license.LicenseSeats,
                ConsumedSeats = license.ConsumedSeats,
                SeatsUsed = license.SeatsUsed,
                StorageGb = license.StorageGb,
                LicenseCategoryName = license.LicenseCategory?.LicenseCategoryName ?? "",
                LicenseCategoryDescription = license.LicenseCategory?.LicenseCategoryDescription ?? "",
                StartDate = license.StartDate,
                EndDate = license.EndDate,
                CapabilityTypeDescription = "full", // TODO: Get from database
                LicenseKey = license.LicenseKey,
                LicenseAttributeDescription = "Auto Renewal", // TODO: Get from license_attribute table
                LicenseAttributeTag = "AUTORENEWAL", // TODO: Get from license_attribute table
                LicenseAttributeLicenseValue = 1, // TODO: Get from license_attribute table
                LicenseAttributeLicenseValueDescription = "Active", // TODO: Get from license_attribute table
                OemType = "Fixed expiration", // TODO: Get from database
                PortalFlag = license.PortalFlag,
                RenewalCount = license.RenewalCount,
                LicenseOriginChannelName = "Online", // TODO: Get from database
                LicenseOriginalActivationDate = license.LicenseOriginalActivationDate,
                EmailOptIn = license.EmailOptIn,
                LicenseDistributionMethodCode = "ENTP", // TODO: Get from database
                NextBillDate = license.NextBillDate,
                DaysRemaining = daysRemaining,
                IsExpired = isExpired
            },
            LicenseVerified = true,
            LicenseProfile = ConvertToLicenseProfileDtos(licenseProfiles),
            LicenseSiteId = null, // TODO: Get from partner association
            UpgradeCategories = ConvertToUpgradeCategoryDtos(upgradeCategories),
            BillingModels = ConvertToBillingModelDtos(billingModels)
        };

        _logger.LogInformation("Successfully built license options response for message_key={MessageKey}", 
            messageKey);

        return response;
    }

    private (string languageCode, string countryCode) ParseLocale(string locale)
    {
        // Parse "en_US" → ("en", "US")
        var parts = locale.Split('_');
        if (parts.Length != 2)
        {
            _logger.LogWarning("Invalid locale format: {Locale}, using defaults", locale);
            return ("en", "US");
        }

        return (parts[0], parts[1]);
    }

    private Dictionary<string, LicenseProfileDto> ConvertToLicenseProfileDtos(
        Dictionary<string, object> profiles)
    {
        // TODO: Implement proper conversion from dynamic objects to DTOs
        // This is a placeholder - actual implementation depends on database schema
        return new Dictionary<string, LicenseProfileDto>();
    }

    private Dictionary<string, UpgradeCategoryDto> ConvertToUpgradeCategoryDtos(
        Dictionary<string, object> upgradeCategories)
    {
        // TODO: Implement proper conversion
        return new Dictionary<string, UpgradeCategoryDto>();
    }

    private Dictionary<string, BillingModelDto> ConvertToBillingModelDtos(
        Dictionary<string, object> billingModels)
    {
        // TODO: Implement proper conversion
        return billingModels.ToDictionary(
            kvp => kvp.Key,
            kvp => new BillingModelDto
            {
                // Map properties from dynamic object
            });
    }
}

public class NotFoundException : Exception
{
    public NotFoundException(string message) : base(message) { }
}
```

---

### Step 5: Create Controller

#### **File: `Controllers/LicenseController.cs`**

```csharp
using EcomNewApi.Models.DTOs;
using EcomNewApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace EcomNewApi.Controllers;

[ApiController]
[Route("api/v1")]
public class LicenseController : ControllerBase
{
    private readonly ILicenseService _licenseService;
    private readonly ILogger<LicenseController> _logger;

    public LicenseController(ILicenseService licenseService, ILogger<LicenseController> logger)
    {
        _licenseService = licenseService;
        _logger = logger;
    }

    /// <summary>
    /// Get license options for the configure page
    /// </summary>
    /// <param name="message_key">License key (UUID format)</param>
    /// <param name="locale">Locale string (e.g., "en_US")</param>
    /// <returns>License options with profiles and upgrade paths</returns>
    [HttpGet("license-options")]
    [ProducesResponseType(typeof(LicenseOptionsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<LicenseOptionsResponse>> GetLicenseOptions(
        [FromQuery] string message_key,
        [FromQuery] string locale)
    {
        _logger.LogInformation("GET /license-options called with message_key={MessageKey}, locale={Locale}",
            message_key, locale);

        // Validate input
        if (string.IsNullOrWhiteSpace(message_key))
        {
            _logger.LogWarning("Missing required parameter: message_key");
            return BadRequest(new { error = "message_key is required" });
        }

        if (string.IsNullOrWhiteSpace(locale))
        {
            _logger.LogWarning("Missing required parameter: locale");
            return BadRequest(new { error = "locale is required" });
        }

        try
        {
            var result = await _licenseService.GetLicenseOptionsAsync(message_key, locale);
            
            _logger.LogInformation("Successfully retrieved license options for message_key={MessageKey}",
                message_key);
            
            return Ok(result);
        }
        catch (NotFoundException ex)
        {
            _logger.LogWarning(ex, "License not found: {MessageKey}", message_key);
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving license options for message_key={MessageKey}", 
                message_key);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }
}
```

---

### Step 6: Register Services in Dependency Injection

#### **File: `Program.cs` (UPDATE)**

Add these registrations to your existing `Program.cs`:

```csharp
// Add these lines in the service registration section

// License services
builder.Services.AddScoped<ILicenseService, LicenseService>();
builder.Services.AddScoped<ILicenseRepository, EfLicenseRepository>();
```

---

### Step 7: Update DbContext

#### **File: `Data/EcomDbContext.cs` (UPDATE)**

Add DbSets for license tables:

```csharp
public class EcomDbContext : DbContext
{
    // Existing DbSets
    public DbSet<CartOrder> CartOrders { get; set; }
    public DbSet<CartOrderItem> CartOrderItems { get; set; }
    public DbSet<CartJson> CartJsons { get; set; }

    // NEW: Add these DbSets
    public DbSet<License> Licenses { get; set; }
    public DbSet<LicenseCategory> LicenseCategories { get; set; }

    // ... rest of your DbContext configuration
}
```

---

## 🧪 Testing Strategy

### Manual Testing with Swagger

1. Start your API project
2. Navigate to Swagger UI (usually `https://localhost:5001/swagger`)
3. Find the `GET /api/v1/license-options` endpoint
4. Test with real data:
   ```
   message_key: A5A3CD6F-788D-4D20-9E32-3362E88ED732
   locale: en_US
   ```
5. Verify response matches expected JSON structure

### Compare with PHP API

Use a tool like Postman or curl to compare responses:

```bash
# PHP API (existing)
curl "https://cartapi.webroot.com/license-options?locale=en_US&message_key=A5A3CD6F-788D-4D20-9E32-3362E88ED732"

# C# API (new)
curl "https://localhost:5001/api/v1/license-options?locale=en_US&message_key=A5A3CD6F-788D-4D20-9E32-3362E88ED732"
```

Compare the JSON responses field-by-field.

---

## 📊 Database Queries Needed

The repository methods need to query these tables (based on PHP stored procedure analysis):

### Core Tables
- `license` - Main license data
- `license_category` - Product types (SAEP, WSAC, etc.)
- `license_attribute` - Settings (auto-renewal, billing model)
- `license_status` - Status descriptions
- `license_keycode_type` - Keycode type descriptions
- `license_type` - Type descriptions (OEM, etc.)

### Related Tables
- `license_category_upgrade` - Upgrade paths
- `usage_pricing_model` - Usage-based pricing configs
- `retention_model` - Backup retention settings
- `product_platform` - Platform-specific settings
- `vault` - Data center configurations
- `product_pricing_level` - Pricing tiers
- `partner` - Partner associations (for site_id)

### Example Complex Query (for reference)

```sql
SELECT 
    l.license_id,
    l.license_key,
    l.keycode,
    lc.license_category_name,
    lc.license_category_description,
    l.license_seats,
    l.start_date,
    l.end_date,
    DATEDIFF(DAY, GETDATE(), l.end_date) AS days_remaining,
    la.license_attribute_description,
    la.license_attribute_tag,
    la.license_attribute_license_value
FROM license l
INNER JOIN license_category lc ON l.license_category_id = lc.license_category_id
LEFT JOIN license_attribute la ON l.license_id = la.license_id
WHERE l.license_key = @message_key
    AND l.license_status_id = 1 -- Active
```

---

## 🚀 Implementation Phases

### Phase 1: Mock Implementation (Day 1-2)
1. ✅ Create all DTO classes
2. ✅ Create service and repository interfaces
3. ✅ Return hardcoded data matching the JSON structure
4. ✅ Test with Swagger UI
5. ✅ Verify frontend can consume the API

**Goal:** Unblock frontend team immediately

### Phase 2: Database Integration (Day 3-5)
1. ✅ Create entity models
2. ✅ Update DbContext
3. ✅ Implement actual database queries in repository
4. ✅ Map entities to DTOs
5. ✅ Test with real database

**Goal:** Replace hardcoded data with real data

### Phase 3: Business Logic (Day 6-7)
1. ✅ Implement locale parsing (en_US → language + country)
2. ✅ Calculate days remaining / expiration logic
3. ✅ Retrieve upgrade categories based on current license
4. ✅ Get billing models
5. ✅ Handle edge cases (expired licenses, invalid keys)

**Goal:** Match PHP API behavior exactly

### Phase 4: Optimization & Polish (Day 8-10)
1. ✅ Add caching (Redis/IMemoryCache)
2. ✅ Add logging
3. ✅ Add error handling
4. ✅ Performance testing
5. ✅ Load testing

**Goal:** Production-ready performance

---

## ⚠️ Known Challenges & Solutions

### Challenge 1: Complex License Profile Query
**Problem:** The stored procedure queries multiple tables with complex JOINs.

**Solution:** 
- Phase 1: Return hardcoded profiles
- Phase 2: Use EF Core Include/ThenInclude for navigation properties
- Phase 3: Optimize with raw SQL if needed

### Challenge 2: Locale Parsing
**Problem:** Need to convert "en_US" → "en" + "USA" (ISO3 country code).

**Solution:**
```csharp
private (string languageCode, string locationCode) ParseLocale(string locale)
{
    var parts = locale.Split('_');
    var language = parts[0]; // "en"
    var country = parts[1];  // "US"
    
    // Convert US → USA using country lookup table
    var locationCode = await _countryRepository.GetIso3CodeAsync(country);
    return (language, locationCode);
}
```

### Challenge 3: Upgrade Categories Logic
**Problem:** Determining which products a customer can upgrade to.

**Solution:** Query `license_category_upgrade` table with seat tier filtering.

---

## 📝 Validation Checklist

Before marking as complete, verify:

- [ ] Endpoint returns 200 OK for valid license keys
- [ ] Endpoint returns 404 for invalid license keys
- [ ] Endpoint returns 400 for missing parameters
- [ ] Response JSON structure matches PHP API exactly
- [ ] Field names match (snake_case vs camelCase handled)
- [ ] All required fields are populated
- [ ] Null fields are handled correctly
- [ ] Date formats match expected format
- [ ] Locale parsing works for all supported locales
- [ ] License profiles include both primary and secondary products
- [ ] Upgrade categories are correctly populated
- [ ] Billing models are included
- [ ] Days remaining is calculated correctly
- [ ] Performance is acceptable (< 500ms response time)

---

## 🎯 Success Criteria

The endpoint is complete when:

1. ✅ Frontend JavaScript can call the endpoint and render the configure page
2. ✅ Response matches PHP API structure (validated with automated tests)
3. ✅ All license data is retrieved from the database (no hardcoded values)
4. ✅ Error handling is robust (404, 400, 500 responses)
5. ✅ Logging is comprehensive
6. ✅ Performance meets requirements (< 500ms for 95th percentile)
7. ✅ Unit tests pass
8. ✅ Integration tests pass

---

## 📚 Reference Documentation

- **Real API Response:** See `intermediate-cart-unique-summary.txt` (section #1)
- **PHP Implementation:** `apps/partner_api/controllers/CartLicenseOptionController.php`
- **Model Layer:** `li3_wr/models/cart_order/PartnerCartNewOption.php`
- **Architecture Overview:** `END_TO_END_ARCHITECTURE.md`
- **Migration Guide:** `CONFIGURE_PAGE_MIGRATION_GUIDE.md`

---

## 🆘 Need Help?

If you encounter issues:

1. Check logs for error messages
2. Use Swagger UI to test the endpoint
3. Compare response with PHP API using Postman
4. Verify database connection string
5. Check that all tables exist in the database
6. Ensure EF Core migrations are up to date

---

**Next Steps After This Endpoint:**
- Implement `GET /cart/bundle-pricing` (pricing calculations)
- Implement `GET /configure` (configurator data)
- Implement `GET /upgrade` (upgrade options)
