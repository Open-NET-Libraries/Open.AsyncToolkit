# .NET Framework 4.8 Compatibility Tests

This test project specifically targets .NET Framework 4.8 to test the compatibility of netstandard2.0 libraries and their associated shims.

## Purpose

1. Verify that the Microsoft.Bcl.AsyncInterfaces shim works correctly in .NET Framework 4.8
2. Test the ValueTask compatibility and async interfaces
3. Ensure cancellation token propagation works as expected
4. Validate that all library APIs function correctly when consumed from .NET Framework 4.8

## Notes

- This project only targets .NET Framework 4.8 (not .NET Core or .NET 5+)
- It focuses exclusively on testing the compatibility shims, not full feature testing
- Run these tests to verify changes don't break compatibility with older frameworks
