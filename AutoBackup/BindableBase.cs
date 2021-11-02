using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace MYLibrary.Bindings
{
    public class BindableBase : INotifyPropertyChanged
    {
        //For .Net 4.5 or higher version
        protected void UpdateProperty<T>(ref T properValue, T newValue, [CallerMemberName] string properName = "", bool bForceUpdate = false)
        {
            if (object.Equals(properValue, newValue) && !bForceUpdate)
                return;

            properValue = newValue;
            OnPropertyChanged(properName);
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        //protected void UpdateProperty<T>(ref T properValue, T newValue,  string properName = "", bool bForceUpdate = false)
        //{
        //    if (object.Equals(properValue, newValue) && !bForceUpdate)
        //        return;

        //    properValue = newValue;
        //    OnPropertyChanged(properName);
        //}

        //public event PropertyChangedEventHandler PropertyChanged;
        //protected void OnPropertyChanged(string propertyName = "")
        //{
        //    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        //}
    }
}
