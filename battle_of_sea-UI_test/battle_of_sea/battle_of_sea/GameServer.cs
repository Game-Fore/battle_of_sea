namespace battle_of_sea
{
    using battle_of_sea.Game;
    using battle_of_sea.Network;
    using System.Collections.Generic;

    /// <summary>
    /// Глобальный синглтон для всех игровых операций
    /// Используется всеми протоколами (TCP, WebSocket)
    /// </summary>
    public class GameServer
    {
        private static GameServer? _instance;
        private static readonly object _lock = new object();

        public static GameServer Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                        {
                            _instance = new GameServer();
                            Console.WriteLine("[GameServer] ✅ Global GameServer instance created");
                        }
                    }
                }
                return _instance;
            }
        }

        public GameManager GameManager { get; private set; }
        private List<WebSocketConnection> _allConnections = new();

        private GameServer()
        {
            GameManager = new GameManager();
        }

        public void AddConnection(WebSocketConnection connection)
        {
            lock (_lock)
            {
                _allConnections.Add(connection);
                Console.WriteLine($"[GameServer] ✅ WebSocket connection added. Total: {_allConnections.Count}");
            }
        }

        public void RemoveConnection(WebSocketConnection connection)
        {
            lock (_lock)
            {
                _allConnections.Remove(connection);
                Console.WriteLine($"[GameServer] WebSocket connection removed. Total: {_allConnections.Count}");
            }
        }

        public List<WebSocketConnection> GetAllConnections()
        {
            lock (_lock)
            {
                var connections = _allConnections.ToList();
                Console.WriteLine($"[GameServer] GetAllConnections: returning {connections.Count} connections");
                return connections;
            }
        }
    }
}

