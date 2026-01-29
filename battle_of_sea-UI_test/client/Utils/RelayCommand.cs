// Команда утилита
using System;
using System.Windows.Input;

namespace BattleOfSea.Utils
{
    // Реализация команды для MVVM
    public class RelayCommand : ICommand
    {
        private readonly Action<object?> _execute;
        private readonly Func<object?, bool>? _canExecute;

        // Конструктор команды (публичный)
        public RelayCommand(Action<object?> execute, Func<object?, bool>? canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        // Проверка возможности выполнения команды (публичный метод)
        public bool CanExecute(object? parameter) => _canExecute?.Invoke(parameter) ?? true;

        // Выполнение команды (публичный метод)
        public void Execute(object? parameter) => _execute(parameter);

        // Событие изменения состояния команды (публичное событие)
        public event EventHandler? CanExecuteChanged;

        // Вызов события изменения состояния команды (публичный метод)
        public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }
}