using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ChatClient.BaseClasses;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace ChatClient
{
    public class ChatViewModel : ViewModelBase
    {
        private ChatModel _model;
        private string _name;
        private string[] _colours;

        public ChatViewModel() : this (new ChatModel())
        {

        }

        public ChatViewModel(ChatModel model)
        {
            _model = model;
            _colours = new string[] { "Black", "Red", "Blue", "White", "Green", "Yellow" };

            _name = "TODO";
        }

        public string[] AllColours
        {
            get { return _colours; }
        }

        private string _colour = "Black";
        public string Colour
        {
            get { return _colour; }
            set { SetField<string>(ref _colour, value, "Colour"); }
        }

        private string _message = string.Empty;
        public string Message
        {
            get { return _message; }
            set { SetField<string>(ref _message, value, "Message"); }
        }


        public ICommand SendMessage
        {
            get
            {
                return new BaseClasses.SimpleCommand
                    (
                    (x) => (Message ?? "").Length > 0,
                    (x) =>
                    {
                        _model.SendMessage(_name, Message, Colour);
                        Message = string.Empty;
                    },
                    this
                    );
            }
        }

        public ObservableCollection<MessageServer.Message> Messages
        {
            get { return _model.Messages; }
            set
            {
                if (!EqualityComparer<ObservableCollection<MessageServer.Message>>.Default.Equals(_model.Messages, value))
                {
                    _model.Messages = value;
                    OnPropertyChanged("Messages");
                }
            }
        }

    }
}
