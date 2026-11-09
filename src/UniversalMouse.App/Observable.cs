using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
namespace UniversalMouse.App;
public abstract class Observable : INotifyPropertyChanged
{
 public event PropertyChangedEventHandler? PropertyChanged;
 protected bool Set<T>(ref T field,T value,[CallerMemberName]string name="") { if(EqualityComparer<T>.Default.Equals(field,value))return false;field=value;Notify(name);return true; }
 protected void Notify([CallerMemberName]string name="")=>PropertyChanged?.Invoke(this,new(name));
}
public sealed class ActionCommand(Action action) : ICommand
{
 public bool CanExecute(object? parameter)=>true;
 public void Execute(object? parameter)=>action();
 public event EventHandler? CanExecuteChanged {add{} remove{}}
}
