namespace Monklongpae.SingleR
{
    public interface IStrogeManage
    {
        void AddConnection(string connectionId, string userId);
        void RemoveConnection(string connectionId);
        Dictionary<string, string> GetConnections();
    }
}
