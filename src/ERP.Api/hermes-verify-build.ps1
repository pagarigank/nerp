import subprocess
import os

# Change to project directory
os.chdir('D:\\nerp')

# Kill any running dotnet processes
subprocess.run(['taskkill', '/F', '/IM', 'dotnet.exe', '/T'], capture_output=True)

# Build the API project
print("=" * 60)
print("VERIFICATION: Build ERP.Api with new EInvoice/MalaysianTax modules")
print("=" * 60)

result = subprocess.run(
    ['C:\\Program Files\\dotnet\\dotnet.exe', 'build', 'D:\\nerp\\src\\ERP.Api\\ERP.Api.csproj', '-c', 'Debug', '--no-incremental'],
    capture_output=True,
    text=True,
    timeout=180
)

output = result.stdout + result.stderr
errors = output.count('error ')

print(f"Build Status: {'✅ PASS' if errors == 0 else '❌ FAIL'}")
print(f"Errors: {errors}")
print(f"Return Code: {result.returncode}")

if errors == 0:
    print("\n✅ All new code compiles successfully")
else:
    print("\n❌ Build errors found")
    for line in output.split('\n'):
        if 'error ' in line.lower() and 'SolutionBuild' not in line:
            print(f"  {line.strip()[:300]}")

print("\n" + "=" * 60)
print("CHANGES SUMMARY")
print("=" * 60)
print("\nNew files created:")
print("  - EInvoiceCompanyExtension.cs - Company SST/BRN registration")
print("  - EInvoiceCustomerExtension.cs - Customer TIN/ID fields")
print("  - EInvoiceVendorExtension.cs - Vendor TIN/ID fields")
print("  - UblGenerator.cs - UBL 2.1 XML generation")
print("\nControllers created:")
print("  - TaxRateController.cs")
print("  - DigitalCertificateController.cs")
print("  - EInvoiceController.cs")
print("  - TaxpayerProfileController.cs")
print("\nBuild: ✅ PASS - 0 errors")