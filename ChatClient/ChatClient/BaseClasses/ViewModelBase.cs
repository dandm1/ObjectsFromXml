using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatClient.BaseClasses
{
    public class ViewModelBase : INotifyPropertyChanged
    {
        public ViewModelBase()
        {

        }

        #region propertychanged
        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            //PropertyChangedEventHandler handler = PropertyChanged;
            if (PropertyChanged != null)
            {
                if (System.Windows.Application.Current.Dispatcher.CheckAccess())
                {
                    PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
                }
                else
                {
                    System.Windows.Application.Current.Dispatcher.BeginInvoke(
                        (Action)(() =>
                        {
                            PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
                        }));
                }
            }
        }

        protected bool SetField<T>(ref T field, T value, string propertyName)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }
        #endregion

    }
}
