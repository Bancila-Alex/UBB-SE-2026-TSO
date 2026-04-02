using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows.Input;

namespace ChatModule.ViewModels
{
    public class RelayCommand : ICommand
    {
        private readonly Func<Task> _execute;
        private readonly Func<bool>? _canExecute;
        private bool _running;

        public RelayCommand(Func<Task> execute, Func<bool>? canExecute = null)
        {
            _execute = execute;
            _canExecute = canExecute;
        }

        public event EventHandler? CanExecuteChanged;

        public bool CanExecute(object? parameter) => !_running && (_canExecute?.Invoke() ?? true);

        public async void Execute(object? parameter)
        {
            _running = true;
            RaiseCanExecuteChanged();
            try
            {
                await _execute();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"RelayCommand execution failed: {ex}");
            }
            finally
            {
                _running = false;
                RaiseCanExecuteChanged();
            }
        }

        public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }

    public class RelayCommand<T> : ICommand
    {
        private readonly Func<T, Task> _execute;

        public RelayCommand(Func<T, Task> execute)
        {
            _execute = execute;
        }

        public event EventHandler? CanExecuteChanged;

        public bool CanExecute(object? parameter) => true;

        public async void Execute(object? parameter)
        {
            try
            {
                if (parameter is T t)
                {
                    await _execute(t);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"RelayCommand<{typeof(T).Name}> execution failed: {ex}");
            }
        }
    }
}
