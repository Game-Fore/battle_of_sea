using battle_of_sea.Protocol;
using battle_of_sea.Network;
using System;
using System.Timers;

namespace battle_of_sea.Game
{
    public class GameSession
    {
        public Player Player1 { get; }
        public Player Player2 { get; }
        public bool IsFinished { get; private set; }
        public bool Player1Ready { get; set; } = false;
        public bool Player2Ready { get; set; } = false;

        public string CurrentTurnPlayerId { get; private set; }

        private readonly System.Timers.Timer _turnTimer;
        private const int TurnTimeMs = 30_000; // 30 секунд
        
        // Событие завершения игры
        public event Action<GameSession>? GameFinished;
        
        public GameSession(Player p1, Player p2)
        {
            Player1 = p1;
            Player2 = p2;
            CurrentTurnPlayerId = p1.Id;

            _turnTimer = new System.Timers.Timer(TurnTimeMs);
            _turnTimer.AutoReset = false;
            _turnTimer.Elapsed += OnTurnTimeout;

            StartTurnTimer();
        }

        public Player GetCurrentPlayer() =>
            CurrentTurnPlayerId == Player1.Id ? Player1 : Player2;

        public Player GetOpponentPlayer() =>
            CurrentTurnPlayerId == Player1.Id ? Player2 : Player1;

        public bool BothPlayersReady => Player1Ready && Player2Ready;

        public void SwitchTurn()
        {
            CurrentTurnPlayerId = GetOpponentPlayer().Id;
            StartTurnTimer();
        }

        private void StartTurnTimer()
        {
            _turnTimer.Stop();
            _turnTimer.Start();
        }

        private async void OnTurnTimeout(object sender, ElapsedEventArgs e)
        {
            var timedOutPlayer = GetCurrentPlayer();
            var opponent = GetOpponentPlayer();

            Console.WriteLine($"Turn timeout: {timedOutPlayer.Name}");

            // уведомляем обоих
            await SendMessageToPlayer(timedOutPlayer, 
                new ServerMessage { Type = "turn_timeout" });

            await SendMessageToPlayer(opponent, 
                new ServerMessage { Type = "your_turn" });

            SwitchTurn();
        }

        public async Task ProcessShotAsync(Player shooter, int x, int y)
        {
            // Проверка хода (дополнительная защита)
            if (shooter.Id != CurrentTurnPlayerId)
            {
                await SendMessageToPlayer(shooter, new ServerMessage
                {
                    Type = "error",
                    Payload = new { message = "Not your turn" }
                });
                return;
            }

            var opponent = GetOpponentPlayer();

            var result = opponent.Board.Shoot(x, y);


            // Результат стреляющему
            await SendMessageToPlayer(shooter, new ServerMessage
            {
                Type = "ShootResult",
                Payload = new { x, y, result = result.ToString() }
            });

            // Результат противнику
            await SendMessageToPlayer(opponent, new ServerMessage
            {
                Type = "OpponentShoot",
                Payload = new { x, y, result = result.ToString() }
            });

            // Победа?
            if (opponent.Board.IsDefeated())
            {
                _turnTimer.Stop();
                

                await SendMessageToPlayer(shooter, new ServerMessage
                {
                    Type = "GameOver",
                    Payload = new { winner = shooter.Name }
                });

                await SendMessageToPlayer(opponent, new ServerMessage
                {
                    Type = "GameOver",
                    Payload = new { winner = shooter.Name }
                });
                IsFinished = true;
                
                // Вызываем событие завершения игры
                GameFinished?.Invoke(this);
                return;
            }

            // Передаём ход и перезапускаем таймер
            SwitchTurn();

            await SendMessageToPlayer(GetCurrentPlayer(), 
                new ServerMessage { Type = "YourTurn" });
        }

        private async Task SendMessageToPlayer(Player player, ServerMessage message)
        {
            try
            {
                if (player.Connection is WebSocketConnection wsConnection)
                {
                    await wsConnection.SendAsync(message);
                }
                else if (player.Connection is ClientConnection tcpConnection)
                {
                    await tcpConnection.SendAsync(message);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending message to player {player.Name}: {ex.Message}");
            }
        }
    }
}