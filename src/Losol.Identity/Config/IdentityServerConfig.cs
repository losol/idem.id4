namespace Losol.Identity.Config
{
    public enum ConfigurationType
    {
        InMemory,
        Database
    }

    public class IdentityServerConfig
    {
        public ConfigurationType ConfigurationType { get; set; } = ConfigurationType.InMemory;

        public KeyStoreConfig KeyStore { get; set; } = new KeyStoreConfig();
    }

    public class KeyStoreConfig
    {
        public LocalMachineCertStorage LocalMachine { get; set; }
    }

    public class LocalMachineCertStorage
    {
        public string CommonName { get; set; }
    }
}
