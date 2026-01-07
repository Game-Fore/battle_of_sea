Write-Host "=== CONNECT ALEX ==="

$client = New-Object System.Net.Sockets.TcpClient("localhost", 5000)
$stream = $client.GetStream()
$reader = New-Object System.IO.StreamReader($stream)
$writer = New-Object System.IO.StreamWriter($stream)
$writer.AutoFlush = $true

$writer.WriteLine('{ "type":"connect", "payload":{ "playerName":"Alex" } }')
$response = $reader.ReadLine()
Write-Host "Server:" $response

$playerId = (ConvertFrom-Json $response).Payload.playerId
Write-Host "Alex playerId:" $playerId

Start-Sleep -Seconds 1

Write-Host "`n=== DISCONNECT ALEX ==="
$client.Close()

Start-Sleep -Seconds 2

Write-Host "`n=== RECONNECT ALEX ==="

$client2 = New-Object System.Net.Sockets.TcpClient("localhost", 5000)
$stream2 = $client2.GetStream()
$reader2 = New-Object System.IO.StreamReader($stream2)
$writer2 = New-Object System.IO.StreamWriter($stream2)
$writer2.AutoFlush = $true

$reconnectJson = "{ `"type`":`"reconnect`", `"payload`":{ `"playerId`":`"$playerId`" } }"
$writer2.WriteLine($reconnectJson)

$response2 = $reader2.ReadLine()
Write-Host "Server:" $response2

Write-Host "`n=== DONE ==="

$client2.Close()
