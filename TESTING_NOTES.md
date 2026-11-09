# Testing Notes

## 1. Test Results

Total tests: 19  
Passed: [VERIFY USING DOTNET TEST]  
Failed: [VERIFY USING DOTNET TEST]

## 2. Line Coverage

Domain: [COPY ACTUAL PERCENTAGE FROM COVERAGE REPORT]%  
Application: [COPY ACTUAL PERCENTAGE FROM COVERAGE REPORT]%

## 3. Test Data Libraries

- **Moq:** Used to mock IInventoryService, IOrderRepository, and INotificationService to isolate CheckoutService and verify dependency interactions.
- **AutoFixture:** Used to automatically generate valid Order and OrderItem test data, with customized quantity and unit price values.
- **Bogus:** Used to generate realistic customer names and email addresses with a fixed seed to ensure reproducible test data.

## 4. Discount Business Rule

No, it is not entirely fair because an order worth 999.99 EGP costs 999.99 EGP, while an order worth 1,000 EGP costs only 900 EGP after the discount, creating a significant pricing difference for almost identical purchase amounts.

## 5. Bugs Found

No confirmed implementation bugs were found during testing. However, the discount threshold creates a potential business logic issue because a small increase in the subtotal can result in a significantly lower final price.