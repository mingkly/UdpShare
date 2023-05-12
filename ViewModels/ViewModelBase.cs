using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace UdpQuickShare.ViewModels
{
    public class ViewModelBase : ObservableObject
    {
        protected void Set<T>(ref T t,T value, [CallerMemberName] string caller = "")
        {
            SetProperty(ref t, value, caller);
        }
    }
}
