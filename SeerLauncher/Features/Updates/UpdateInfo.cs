namespace SeerLauncher.Features.Updates
{
    public class UpdateInfo
    {
        public string Version { get; set; }
        public string GlobalUrl { get; set; }
        public string CnUrl { get; set; }
        public string Info { get; set; }
        public bool IsForceUpdate { get; set; }
    }
}
