namespace Monklongpae.SingleR
{
    public class StrogeManage : IStrogeManage
    {
        private readonly Dictionary<string, string> _connections = new Dictionary<string, string>();

        public void AddConnection(string connectionId, string userId)
        {
            if (!_connections.ContainsKey(connectionId))
            {
                _connections[connectionId] = userId;
            }
        }

        public void RemoveConnection(string connectionId)
        {
            if (_connections.ContainsKey(connectionId))
            {
                _connections.Remove(connectionId);
            }
        }

        public Dictionary<string, string> GetConnections()
        {
            return _connections;
        }
    }
}
