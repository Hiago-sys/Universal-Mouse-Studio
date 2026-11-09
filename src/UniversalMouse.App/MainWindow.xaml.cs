using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using UniversalMouse.Windows;
namespace UniversalMouse.App;
public partial class MainWindow : Window
{
 private readonly StudioViewModel _vm=new();
 private ForegroundApplicationWatcher? _foreground;
 private bool _closeReady;
 public MainWindow(){InitializeComponent();DataContext=_vm;SourceInitialized+=OnSourceInitialized;Closing+=OnClosing;}
 private async void OnSourceInitialized(object? sender,EventArgs e)
 {
  await _vm.InitializeAsync(new WindowInteropHelper(this).Handle,Lab);
  try{_foreground=new ForegroundApplicationWatcher();_foreground.ApplicationChanged+=OnForeground;}catch(Exception ex){_vm.ReportForegroundError(ex.Message);}
 }
 private void OnForeground(string executable)=>Dispatcher.BeginInvoke(()=>_vm.NotifyForeground(executable));
 private async void OnClosing(object? sender,CancelEventArgs e)
 {
  if(_closeReady)return;e.Cancel=true;_vm.StopCommand.Execute(null);
  try{await _vm.SavePendingAsync();}catch(Exception ex){if(MessageBox.Show("Não foi possível salvar: "+ex.Message+"\nFechar sem salvar?","Perfis",MessageBoxButton.YesNo,MessageBoxImage.Warning)!=MessageBoxResult.Yes)return;}
  _foreground?.Dispose();_vm.Dispose();_closeReady=true;Close();
 }
 private void OnTestMouseDown(object sender,MouseButtonEventArgs e)
 {
  string? button=e.ChangedButton switch{MouseButton.Left=>"Left",MouseButton.Right=>"Right",MouseButton.Middle=>"Middle",MouseButton.XButton1=>"X1",MouseButton.XButton2=>"X2",_=>null};if(button is not null)_vm.RecordClick(button);e.Handled=true;
 }
 private void OnTestWheel(object sender,MouseWheelEventArgs e){_vm.RecordWheel();e.Handled=true;}
}
