using System;
using System.Windows.Input;
using Xamarin.Forms;

namespace App.Mobile.Remote.ViewModels
{
	public class TextCommandViewModel : ViewModelBase
	{
		public TextCommandViewModel()
		{
		}

		/// <inheritdoc />
		public TextCommandViewModel(string text, ICommand command)
		{
			_command = command;
			_text = text;
		}

		/// <inheritdoc />
		public TextCommandViewModel(string text, Action<object> command)
		{
			_command = new Command(command);
			_text = text;
		}

		private ICommand _command;

		public ICommand Command
		{
			get => _command;
			set => SetValue(ref _command, value, nameof(Command));
		}

		private string _text;

		public string Text
		{
			get => _text;
			set => SetValue(ref _text, value, nameof(Text));
		}
	}
}