# ----------------------------------------
# Полный тест TCP JSON для battle_of_sea
# ----------------------------------------

function Connect-Player($name) {
    $client = New-Object System.Net.Sockets.TcpClient("127.0.0.1",5000)
    $stream = $client.GetStream()
    $writer = New-Object System.IO.StreamWriter($stream,[System.Text.Encoding]::UTF8)
    $writer.AutoFlush = $true
    $reader = New-Object System.IO.StreamReader($stream,[System.Text.Encoding]::UTF8)

    # Подключение
    $json = '{ "type":"connect", "payload":{ "playerName":"'+$name+'" } }'
    Write-Host "$name connecting: $json"
    $writer.Write("$json`n")
    Write-Host "Server response: $($reader.ReadLine())"

    return @{Client=$client; Writer=$writer; Reader=$reader; Name=$name}
}

# -----------------------
# Подключаем игроков
# -----------------------
$player1 = Connect-Player "Alex"
$player2 = Connect-Player "Bob"

Start-Sleep -Milliseconds 500

Write-Host "Test ships placed (серверная доска) — 3 клетки на каждого"

# -----------------------
# Функция выстрела
# -----------------------
function Shoot($shooter, $target, $x, $y) {
    $json = '{ "type":"shoot", "payload":{ "x":'+$x+', "y":'+$y+' } }'
    $shooter.Writer.Write("$json`n")

    $result = $shooter.Reader.ReadLine()
    Write-Host "$($shooter.Name) shot: $result"

    $opponentResult = $target.Reader.ReadLine()
    Write-Host "$($target.Name) sees opponent shot: $opponentResult"

    # Возвращаем флаг game_over
    if ($result -match '"game_over"') { return $true }
    return $false
}

# -----------------------
# Генератор случайных координат для выстрелов
# -----------------------
$usedCoordinates = @{}
function Get-RandomCoordinates {
    do {
        $x = Get-Random -Minimum 0 -Maximum 10
        $y = Get-Random -Minimum 0 -Maximum 10
    } while ($usedCoordinates["$x,$y"])
    $usedCoordinates["$x,$y"] = $true
    return @($x,$y)
}

# -----------------------
# Цикл игры
# -----------------------
$gameOver = $false
$currentShooter = $player1
$currentTarget  = $player2

while (-not $gameOver) {
    $coords = Get-RandomCoordinates
    $x = $coords[0]
    $y = $coords[1]

    $gameOver = Shoot $currentShooter $currentTarget $x $y

    # Меняем ход
    $temp = $currentShooter
    $currentShooter = $currentTarget
    $currentTarget = $temp

    Start-Sleep -Milliseconds 200
}

# -----------------------
# Закрываем соединения
# -----------------------
$player1.Writer.Close(); $player1.Reader.Close(); $player1.Client.Close()
$player2.Writer.Close(); $player2.Reader.Close(); $player2.Client.Close()
Write-Host "Connections closed"
