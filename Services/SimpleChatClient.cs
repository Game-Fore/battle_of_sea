using System;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace BattleOfSea.Services
{
    // Клиент для чата по TCP
    public class SimpleChatClient : IDisposable
    {
        private TcpClient? _tcpClient;        // Объект для TCP-соединения
        private NetworkStream? _stream;       // Поток для отправки/получения данных
        private bool _isConnected = false;   

        // События для оповещения о разных ситуациях
        public event Action<string>? MessageReceived;     // Новое сообщение
        public event Action<string>? StatusChanged;       // Изменился статус
        public event Action<bool>? ConnectionChanged;     // Подключение/отключение

        // Проверяет, подключены ли мы сейчас
        public bool IsConnected => _isConnected && (_tcpClient?.Connected ?? false);

        // Подключиться к серверу
        public async Task<bool> ConnectAsync(string server, int port)
        {
            try
            {
                
                StatusChanged?.Invoke($"Подключение к {server}:{port}...");

                // Создаем TCP-клиент
                _tcpClient = new TcpClient();
                // Пытаемся подключиться
                await _tcpClient.ConnectAsync(server, port);
                // Получаем поток для обмена данными
                _stream = _tcpClient.GetStream();
                _isConnected = true;

                ConnectionChanged?.Invoke(true);
                StatusChanged?.Invoke($"Успешно подключено к {server}:{port}");

                // Запускаем в фоне чтение сообщений от сервера
                _ = Task.Run(ReceiveMessagesAsync);

                return true;
            }
            catch (Exception ex)
            {
                // Если ошибка - сообщаем и отключаемся
                StatusChanged?.Invoke($"❌ Ошибка подключения: {ex.Message}");
                Disconnect();
                return false;
            }
        }

        // Отправить сообщение на сервер
        public async Task SendMessageAsync(string message)
        {
            // Проверяем, подключены ли мы
            if (!IsConnected)
            {
                StatusChanged?.Invoke("❌ Нет подключения к серверу");
                return;
            }

            try
            {
                // Преобразуем текст в байты, добавляем перевод строки
                byte[] data = Encoding.UTF8.GetBytes(message + "\n");
                // Отправляем байты по сети
                if (_stream != null)
                {
                    await _stream.WriteAsync(data, 0, data.Length);
                }
            }
            catch (Exception ex)
            {
                // Если ошибка - сообщаем и отключаемся
                StatusChanged?.Invoke($"Ошибка отправки: {ex.Message}");
                Disconnect();
            }
        }

        // Получать сообщения от сервера (работает в фоне)
        private async Task ReceiveMessagesAsync()
        {
            // Буфер для приема данных
            byte[] buffer = new byte[4096];
            // Собираем сообщение по частям
            StringBuilder messageBuilder = new StringBuilder();

            try
            {
                // Пока подключены - слушаем сервер
                while (IsConnected && _stream != null)
                {
                    // Ждем данные от сервера
                    int bytesRead = await _stream.ReadAsync(buffer, 0, buffer.Length);

                    // Если 0 байт - сервер отключился
                    if (bytesRead == 0)
                    {
                        StatusChanged?.Invoke("Сервер разорвал соединение");
                        Disconnect();
                        break;
                    }

                    // Преобразуем байты в текст
                    string chunk = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    // Добавляем к накопленному
                    messageBuilder.Append(chunk);

                    // Получаем весь накопленный текст
                    string data = messageBuilder.ToString();
                    int newLineIndex;

                    // Ищем переводы строки - это разделитель сообщений
                    while ((newLineIndex = data.IndexOf('\n')) >= 0)
                    {
                        // Берем текст до перевода строки
                        string message = data.Substring(0, newLineIndex).Trim();
                        // Оставляем остаток
                        data = data.Substring(newLineIndex + 1);

                        // Если сообщение не пустое - отправляем в UI
                        if (!string.IsNullOrEmpty(message))
                        {
                            MessageReceived?.Invoke(message);
                        }
                    }

                    // Очищаем сборщик, оставляя необработанный остаток
                    messageBuilder.Clear();
                    messageBuilder.Append(data);
                }
            }
            catch (Exception ex)
            {
                // Если ошибка приема
                StatusChanged?.Invoke($"Ошибка приема: {ex.Message}");
                Disconnect();
            }
        }

        // Отключиться от сервера
        public void Disconnect()
        {
            // Проверяем, подключены ли мы
            if (_isConnected)
            {
                _isConnected = false;
                // Закрываем поток
                _stream?.Close();
                // Закрываем соединение
                _tcpClient?.Close();

                ConnectionChanged?.Invoke(false);
                StatusChanged?.Invoke("Отключено от сервера");
            }
        }

        // Очистка ресурсов при уничтожении объекта
        public void Dispose()
        {
            Disconnect();
        }
    }
}
