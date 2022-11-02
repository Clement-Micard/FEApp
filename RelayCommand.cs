using System;
using System.Windows.Input;

namespace FEApp
{
	public class RelayCommand : ICommand
	{
		readonly Action<object> _execute;
		readonly Func<object, bool> _canExecute;
		public event EventHandler CanExecuteChanged;

		/// <summary>
		/// RelayCommand constructor.
		/// </summary>
		/// <param name="execute">The execution Action.</param>
		/// <param name="canExecute">Can execute boolean.</param>
		public RelayCommand(Action<object> execute, Func<object, bool> canExecute)
		{
			_execute = execute;
			_canExecute = canExecute;
		}

        /// <summary>
        /// Returns if either the command can be executed or not.
        /// </summary>
        /// <param name="parameter"></param>
        /// <returns>Returns True if the command can be executed, else False.</returns>
        public bool CanExecute(object parameter)
		{
			if (_canExecute != null)
			{
				return _canExecute(parameter);
			}
			else
			{
				return false;
			}
		}

		/// <summary>
		/// Execute the given command.
		/// </summary>
		/// <param name="parameter"></param>
		public void Execute(object parameter)
		{
			_execute(parameter);
		}
	}
}
