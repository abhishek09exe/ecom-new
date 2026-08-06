# Project Guide

## Purpose
This document defines the current project structure and the required rules for adding new controllers and services.

## Current Structure

```text
ecom-new/
|- ecom-new.slnx
|- README.md
|- Project.md
|- ecom-new-api/
|  |- Program.cs
|  |- Controllers/
|  |  |- BundlePricingController.cs
|  |  |- CartOrdersController.cs
|  |  |- LicenseOptionsController.cs
|  |- Services/
|  |  |- ServiceResult.cs
|  |  |- CartOrders/
|  |  |  |- ICartOrderService.cs
|  |  |  |- CartOrderService.cs
|  |  |- LicenseOptions/
|  |  |  |- ILicenseOptionsService.cs
|  |  |  |- LicenseOptionsService.cs
|  |  |- Pricing/
|  |     |- IPricingService.cs
|  |     |- PricingService.cs
|  |     |- CurrencyService.cs
|  |     |- MessageKeyService.cs
|  |- Repositories/
|  |  |- Cart/
|  |  |  |- ICartOrderRepository.cs
|  |  |  |- CartOrderRepository.cs
|  |  |- LicenseOptions/
|  |  |  |- ILicenseOptionsRepository.cs
|  |  |  |- LicenseOptionsRepository.cs
|  |  |- Pricing/
|  |     |- IPricingRepository.cs
|  |     |- PricingRepository.cs
|  |- Models/
|  |- Data/
|  |- Helpers/
|- ecom-new-api.Tests/
   |- Controllers/
   |- Services/
   |- Repositories/
```

## Layering Rules (Must Follow)

1. Controllers must stay thin.
   - No direct database access.
   - No business orchestration logic.
   - Validate request shape and delegate to services.

2. Services own business logic and orchestration.
   - Services should depend on interfaces, not concrete repositories.
   - Cross-repository composition belongs in services.

3. Repositories own data access.
   - DB calls (EF/SQL/SP calls) must stay in repositories.
   - Repository methods should represent data operations, not HTTP behavior.

4. Keep feature-based folders.
   - New feature code must be grouped by feature under Services and Repositories.
   - Example: Services/FeatureX and Repositories/FeatureX.

5. Use interface + implementation pairs.
   - IServiceName + ServiceName
   - IRepositoryName + RepositoryName

6. Register all new dependencies in Program.cs.
   - AddScoped<IServiceName, ServiceName>()
   - AddScoped<IRepositoryName, RepositoryName>()

7. Use ServiceResult for service outcomes.
   - Return success, validation, not-found, and error states through ServiceResult where applicable.

8. Keep naming and namespace aligned with folder path.
   - Services/CartOrders/*.cs => namespace ecom_new_api.Services.CartOrders
   - Repositories/Pricing/*.cs => namespace ecom_new_api.Repositories.Pricing

## Rules For Adding A New Controller

When adding a new controller, complete all of the following:

1. Create controller in Controllers/ using the pattern FeatureNameController.
2. Inject a feature service interface (not repository, not DbContext).
3. Add endpoint attributes and response type metadata.
4. Validate request/query parameters at boundary level.
5. Map service results to IActionResult consistently.
6. Add controller tests under ecom-new-api.Tests/Controllers/.

## Rules For Adding A New Service

When adding a new service, complete all of the following:

1. Create/choose a feature folder under Services/.
2. Add interface and implementation in that folder.
3. Keep service logic focused on orchestration and business rules.
4. Depend on repository interfaces only.
5. Add DI registration to Program.cs.
6. Add service tests under ecom-new-api.Tests/Services/.

## Pull Request Checklist

Before merging, verify:

1. Folder and namespace match feature conventions.
2. DI registrations are added/updated in Program.cs.
3. Controller has no direct data access.
4. Service has unit tests for happy path and failure path.
5. New endpoints include validation and typed responses.
6. dotnet test ecom-new.slnx passes.

## Notes

This file is a living guideline. If architecture changes, update this document in the same PR that introduces the change.
