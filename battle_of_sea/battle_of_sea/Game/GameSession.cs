using battle_of_sea.Protocol;
using System;
using System.Timers;

namespace battle_of_sea.Game
{
    public class GameSession
    {
        public Player Player1 { get; }
        public Player Player2 { get; }
        public bool IsFinished { get; private set; }

        public string CurrentTurnPlayerId { get; private set; }

        private readonly System.Timers.Timer _turnTimer;
        private const int TurnTimeMs = 30_000; // 30 секунд
        
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
            await timedOutPlayer.Connection.SendAsync(
                new ServerMessage { Type = "turn_timeout" });

            await opponent.Connection.SendAsync(
                new ServerMessage { Type = "your_turn" });

            SwitchTurn();
        }
        public async Task ProcessShotAsync(Player shooter, int x, int y)
        {
            // Проверка хода (дополнительная защита)
            if (shooter.Id != CurrentTurnPlayerId)
            {
                await shooter.Connection.SendAsync(new ServerMessage
                {
                    Type = "error",
                    Payload = new { message = "Not your turn" }
                });
                return;
            }

            var opponent = GetOpponentPlayer();

            var result = opponent.Board.Shoot(x, y);


            // Результат стреляющему
            await shooter.Connection.SendAsync(new ServerMessage
            {
                Type = "shoot_result",
                Payload = new { x, y, result = result.ToString() }
            });

            // Результат противнику
            await opponent.Connection.SendAsync(new ServerMessage
            {
                Type = "opponent_shot",
                Payload = new { x, y, result = result.ToString() }
            });

            // Победа?
            if (opponent.Board.IsDefeated())
            {
                _turnTimer.Stop();
                

                await shooter.Connection.SendAsync(new ServerMessage
                {
                    Type = "game_over",
                    Payload = new { winner = shooter.Name }
                });

                await opponent.Connection.SendAsync(new ServerMessage
                {
                    Type = "game_over",
                    Payload = new { winner = shooter.Name }
                });
                IsFinished = true;
                return;
            }

            // Передаём ход и перезапускаем таймер
            SwitchTurn();

            await GetCurrentPlayer().Connection.SendAsync(
                new ServerMessage { Type = "your_turn" });
        }

    }
}
