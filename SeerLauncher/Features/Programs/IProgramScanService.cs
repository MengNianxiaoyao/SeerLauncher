using System.Collections.Generic;

namespace SeerLauncher.Features.Programs
{
    public interface IProgramScanService
    {
        List<string> Scan(string directory, string selfName, IList<string> keywords);
        List<string> MergeConfiguredAndScanned(IList<string> configured, IList<string> scanned);
    }
}
