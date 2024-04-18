
namespace HomeKit.Net.Traffic
{
    public interface IPublishClient
    {
        void OnPublish(string requestedName, string actualName);
    }
}
