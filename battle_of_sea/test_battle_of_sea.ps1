# ----------------------------------------
# Тест TCP JSON для двух игроков (battle_of_sea)
# ----------------------------------------

function Connect-Player($name) {
    $client = New-Object System.Net.Sockets.TcpClient("127.0.0.1",5000)
    $stream = $client.GetStream()
    $writer = New-Object System.IO.StreamWriter($stream,[System.Text.Encoding]::UTF8)
    $writer.AutoFlush = $true
    $reader = New-Object System.IO.StreamReader($stream,[System.Text.Encoding]::UTF8)

    # Подключение
    $json = "{""type"":""connect"",""payload"":{""playerName"":""$name""}}"
    Write-Host "$name connecting: $json"
    $writer.Write("$json`n")
    Write-Host "Server response: $($reader.ReadLine())"

    return @{Client=$client; Writer=$writer; Reader=$reader; Name=$name}
}

# Подключаем двух игроков
$player1 = Connect-Player "Alex"
$player2 = Connect-Player "Bob"

# Ждём, пока сервер создаст игру
Start-Sleep -Milliseconds 500

# -----------------------
# Расставляем тестовые корабли
# -----------------------
# Для теста — 1 корабль на одну клетку
$player1BoardShips = @(@{x=0;y=0}, @{x=1;y=0})
$player2BoardShips = @(@{x=0;y=0}, @{x=1;y=0})

Write-Host "Test ships placed on boards (simulated on server)"

# -----------------------
# Функция для выстрела
# -----------------------
function Shoot($shooter, $target, $x, $y) {
    $json = "{""type"":""shoot"",""payload"":{""x"":$x,""y"":$y}}"
    $shooter.Writer.Write("$json`n")

    # Читаем результат для стреляющего
    $result = $shooter.Reader.ReadLine()
    Write-Host "$($shooter.Name) shot: $result"

    # Читаем уведомление для противника
    $opponentResult = $target.Reader.ReadLine()
    Write-Host "$($target.Name) sees opponent shot: $opponentResult"
}

# -----------------------
# Игровой цикл тест (несколько выстрелов)
# -----------------------
# 1 ход: Alex
Shoot $player1 $player2 0 0

# 2 ход: Bob
Shoot $player2 $player1 0 0

# 3 ход: Alex
Shoot $player1 $player2 1 0

# 4 ход: Bob
Shoot $player2 $player1 1 0

# -----------------------
# Закрываем соединения
# -----------------------
$player1.Writer.Close(); $player1.Reader.Close(); $player1.Client.Close()
$player2.Writer.Close(); $player2.Reader.Close(); $player2.Client.Close()
Write-Host "Connections closed"
