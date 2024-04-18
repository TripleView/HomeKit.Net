namespace HomeKit.Net.Traffic
{
    public class ServiceBrowseResult
    {
        public readonly string Name;
        public readonly string ServiceType;

        public ServiceBrowseResult(string name, string serviceType)
        {
            Name = name;
            ServiceType = Abbreviate(serviceType);
        }

        public override string ToString()
        {
            return $"[{Name}] {ServiceType}";
        }

        internal static string Abbreviate(string name)
        {
            const string suffix = ".local.";
            if (name.EndsWith(suffix))
                return name.Substring(0, name.Length - suffix.Length);
            return name;
        }
    }
}
