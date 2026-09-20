using System.Runtime.InteropServices;

namespace Core; // корінний namespace = ім'я проєкту Core

public sealed record EnvironmentReport(
    string Title,
    string Student,
    string Domain,
    string OsDescription,
    string FrameworkDescription,
    string ProcessArchitecture,
    string DetectedRid,
    string ReportedRid,
    string BaseDirectory,
    string BuildNote);

public static class EnvironmentInfo
{
#if NET10_0_OR_GREATER
    private const string BuildNote = "збірка під net10.0";
#else
    private const string BuildNote = "збірка під net8.0";
#endif

    public static EnvironmentReport Collect() => new(
        Title: "CrossApp – практикум з крос-платформного програмування",
        Student: "Моргулець Валентин, група ФЕІ-37",
        Domain: "Замовлення (клієнти, товари, замовлення, рядки замовлення)",
        OsDescription: RuntimeInformation.OSDescription,
        FrameworkDescription: RuntimeInformation.FrameworkDescription,
        ProcessArchitecture: RuntimeInformation.ProcessArchitecture.ToString(),
        DetectedRid: DetectRid(),
        ReportedRid: RuntimeInformation.RuntimeIdentifier,
        BaseDirectory: AppContext.BaseDirectory,
        BuildNote: BuildNote);

    // Ручне визначення RID: показує, з чого складається рядок win-x64.
    private static string DetectRid()
    {
        string os =
            RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "win" :
            RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ? "linux" :
            RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? "osx" : "unknown";

        string arch = RuntimeInformation.ProcessArchitecture switch
        {
            Architecture.X64 => "x64",
            Architecture.X86 => "x86",
            Architecture.Arm64 => "arm64",
            Architecture.Arm => "arm",
            _ => "unknown"
        };

        return $"{os}-{arch}";
    }
}
