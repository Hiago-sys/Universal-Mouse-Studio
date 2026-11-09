using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using Microsoft.Win32;
using UniversalMouse.Core.Diagnostics;
using UniversalMouse.Core.Hardware;
using UniversalMouse.Core.Motion;
using UniversalMouse.Core.Profiles;
using UniversalMouse.Windows;
using MouseDevice = UniversalMouse.Windows.MouseDevice;
namespace UniversalMouse.App;

public sealed partial class StudioViewModel : Observable, IDisposable
{
 private readonly IWindowsSettingsService _windows;
 private readonly IDesktopInputService _input;
 private readonly ProfileStore _store=new(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"UniversalMouseStudio"));
 private readonly PollingEstimator _polling=new();
 private readonly ClickDiagnostics _clicks=new();
 private readonly DispatcherTimer _timer=new(){Interval=TimeSpan.FromMilliseconds(100)};
 private readonly DispatcherTimer _saveTimer=new(){Interval=TimeSpan.FromMilliseconds(900)};
 private RawInputService? _raw; private MotionLab? _lab;
 private WindowsMouseSettings? _opening;
 private bool _switchingProfile;
 private bool _loading=true,_disposed,_refreshing,_pollingActive,_calibrating,_storageReady;
 private double _resetAfterMilliseconds=80;
 private double _pollingUntil,_calibrationCounts; private int _scrollTicks,_horizontalScrollTicks; private int _connectionChanges;
 private bool _cursorMode;
 private string _status="Preparando serviços…",_deviceDetails="Nenhum mouse selecionado.",_profileName="",_profileExecutable="",_calibrationCentimeters="10",_calibrationResult="Aguardando medição.",_selectedPreset="Equilibrado";
 private StudioProfile? _selectedProfile;private MouseDevice? _selectedDevice;
 public ObservableCollection<MouseDevice> Devices {get;}=[];
 public ObservableCollection<StudioProfile> Profiles {get;}=[];
 public ObservableCollection<string> CapabilityLines {get;}=[];
 public string[] Presets {get;}=["Desativado","Leve","Equilibrado","Forte","Personalizado"];
 public Dictionary<string,string> ButtonActions {get;}=new(){["original"]="Função original",["copy"]="Copiar (Ctrl+C)",["paste"]="Colar (Ctrl+V)",["cut"]="Recortar (Ctrl+X)",["undo"]="Desfazer (Ctrl+Z)",["volume-up"]="Aumentar volume",["volume-down"]="Diminuir volume",["play-pause"]="Reproduzir / pausar"};
 public string Status {get=>_status;private set=>Set(ref _status,value);}
 public string DeviceDetails {get=>_deviceDetails;private set=>Set(ref _deviceDetails,value);}
 public string DeviceName=>SelectedDevice?.Name??"Nenhum mouse disponível";
 public string DeviceSummary=>SelectedDevice is {} d?$"{d.Connection} • VID {d.VendorId?.ToString("X4")??"não informado"} / PID {d.ProductId?.ToString("X4")??"não informado"}":"Conecte um dispositivo e atualize a lista.";
 public string SystemSummary=>$"Ponteiro {PointerSpeed}/20 • Aceleração {(EnhancePointerPrecision?"ativada":"desativada")} (valores dos controles; releia para confirmar o Windows)";
 public string ActiveProfileSummary=>$"Perfil em edição: {ProfileName}";
 public string HookStatus=>_input.IsRunning?(_cursorMode?"Hook ativo • correção do cursor desktop (experimental)":"Hook ativo • apenas remapeamento lateral"):_input.LastError is {} error?$"Hook desligado • {error}":"Hook desligado • entrada original do Windows";
 public string LabSummary=>_lab?.Summary??"Laboratório aguardando inicialização.";
 public string PollingSummary {get;private set;}="Frequência: ainda não medida.";
 public string PollingHistory {get;private set;}="";
 public string ClickSummary {get;private set;}="Nenhum teste de cliques realizado.";
 public string DiagnosticsSummary=>$"{Devices.Count} dispositivos observados • {_connectionChanges} notificações de conexão • {_scrollTicks} passos verticais / {_horizontalScrollTicks} horizontais na sessão de teste.";
 public string PersistenceSummary=>$"Armazenamento local: {(_storageReady?"pronto":"indisponível; consulte a mensagem de erro")}. Configurações do Windows exigem Aplicar.";
 public string ProfileName {get=>_profileName;set {if(Set(ref _profileName,value)){Changed();Notify(nameof(ActiveProfileSummary));}}}
 public string ProfileExecutable {get=>_profileExecutable;set {if(Set(ref _profileExecutable,value))Changed();}}
 public string CalibrationCentimeters {get=>_calibrationCentimeters;set=>Set(ref _calibrationCentimeters,value);}
 public string CalibrationResult {get=>_calibrationResult;private set=>Set(ref _calibrationResult,value);}
 public MouseDevice? SelectedDevice {get=>_selectedDevice;set {if(Set(ref _selectedDevice,value)){UpdateDeviceDetails();_lab?.Clear();_polling.Reset();_pollingActive=false;_calibrating=false;Notify(nameof(DeviceName));Notify(nameof(DeviceSummary));}}}
 public StudioProfile? SelectedProfile
 {
  get=>_selectedProfile;
  set
  {
   if(value is null||_switchingProfile||value==_selectedProfile)return;
   if(!_loading&&_storageReady&&_selectedProfile is not null)
   {
    try {var old=CaptureProfile();old.Validate();int index=Profiles.IndexOf(_selectedProfile);_switchingProfile=true;if(index>=0)Profiles[index]=old;RunAsync(()=>_store.SaveAsync(Profiles.ToArray()));}
    catch(Exception ex){Status="Corrija o perfil antes de alternar: "+ex.Message;return;}
    finally{_switchingProfile=false;}
   }
   if(Set(ref _selectedProfile,value))LoadProfile(value);
  }
 }
 public string SelectedPreset {get=>_selectedPreset;set {if(Set(ref _selectedPreset,value))ApplyPreset(value);}}
 public ICommand RefreshCommand {get;}
 public ICommand ReadSettingsCommand {get;}
 public ICommand ApplySettingsCommand {get;}
 public ICommand RestoreCommand {get;}
 public ICommand StartCommand {get;}
 public ICommand StopCommand {get;}
 public ICommand StartButtonsCommand {get;}
 public ICommand ToggleLabCommand {get;}
 public ICommand ClearLabCommand {get;}
 public ICommand ReplayLabCommand {get;}
 public ICommand StartPollingCommand {get;}
 public ICommand StopPollingCommand {get;}
 public ICommand ResetDiagnosticsCommand {get;}
 public ICommand SaveProfileCommand {get;}
 public ICommand NewProfileCommand {get;}
 public ICommand DuplicateProfileCommand {get;}
 public ICommand DeleteProfileCommand {get;}
 public ICommand ApplyProfileCommand {get;}
 public ICommand ImportProfileCommand {get;}
 public ICommand ExportProfileCommand {get;}
 public ICommand ResetProfilesCommand {get;}
 public ICommand ReportCommand {get;}
 public ICommand StartCalibrationCommand {get;}
 public ICommand FinishCalibrationCommand {get;}
 public StudioViewModel(IWindowsSettingsService? windows=null,IDesktopInputService? input=null)
 {
  _windows=windows??new WindowsSettingsService();_input=input??new DesktopInputService();
  RefreshCommand=new ActionCommand(()=>RunAsync(RefreshAsync));ReadSettingsCommand=new ActionCommand(()=>RunAsync(ReadSettingsAsync));ApplySettingsCommand=new ActionCommand(()=>RunAsync(ApplySettingsAsync));RestoreCommand=new ActionCommand(()=>RunAsync(RestoreAsync));
  StartCommand=Command(()=>Start(true));StopCommand=Command(Stop);StartButtonsCommand=Command(()=>Start(false));
  ToggleLabCommand=Command(()=>{if(_lab is not null){_lab.Capturing=!_lab.Capturing;if(_lab.Capturing)_timer.Start();Notify(nameof(LabSummary));}});
  ClearLabCommand=Command(()=>{_lab?.Clear();Notify(nameof(LabSummary));});ReplayLabCommand=Command(()=>{_lab?.Replay();Notify(nameof(LabSummary));});
  StartPollingCommand=Command(()=>{_polling.Reset();_pollingActive=true;_timer.Start();_pollingUntil=Now+10000;Status="Teste de 10 segundos iniciado. Mova continuamente o mouse selecionado.";});
  StopPollingCommand=Command(()=>{_pollingActive=false;UpdateMetrics();});ResetDiagnosticsCommand=Command(()=>{_polling.Reset();_clicks.Reset();_scrollTicks=_horizontalScrollTicks=0;_pollingActive=false;UpdateMetrics();});
  SaveProfileCommand=new ActionCommand(()=>RunAsync(SaveCurrentAsync));NewProfileCommand=new ActionCommand(()=>RunAsync(()=>CreateProfileAsync(false)));DuplicateProfileCommand=new ActionCommand(()=>RunAsync(()=>CreateProfileAsync(true)));
  DeleteProfileCommand=new ActionCommand(()=>RunAsync(DeleteProfileAsync));ApplyProfileCommand=new ActionCommand(()=>RunAsync(async()=>{await ApplySettingsAsync();Status="Perfil aplicado globalmente ao Windows. O estado de ativação do hook não foi alterado.";}));
  ImportProfileCommand=new ActionCommand(()=>RunAsync(ImportAsync));ExportProfileCommand=new ActionCommand(()=>RunAsync(ExportAsync));ResetProfilesCommand=new ActionCommand(()=>RunAsync(ResetProfilesAsync));ReportCommand=new ActionCommand(()=>RunAsync(ReportAsync));
  StartCalibrationCommand=Command(()=>{_calibrationCounts=0;_calibrating=true;CalibrationResult="Capturando contagens relativas do mouse selecionado…";});FinishCalibrationCommand=Command(FinishCalibration);
  _timer.Tick+=OnTick;_saveTimer.Tick+=OnSaveTick;_input.StateChanged+=OnInputStateChanged;
 }
 private ICommand Command(Action action)=>new ActionCommand(()=>{try{action();}catch(Exception ex){TechnicalLog.Record("command",ex.HResult);Status="Erro: "+ex.Message;}});
 private static double Now=>Stopwatch.GetTimestamp()*1000.0/Stopwatch.Frequency;
 public async Task InitializeAsync(IntPtr hwnd,MotionLab lab)
 {
  _lab=lab;
  try{_opening=await Task.Run(_windows.Read);LoadSettings(_opening);}catch(Exception ex){Status="Não foi possível ler os ajustes do Windows: "+ex.Message;}
  try{var profiles=await _store.LoadAsync();foreach(var profile in profiles)Profiles.Add(profile);_storageReady=true;SelectedProfile=Profiles.FirstOrDefault();Status=_store.LastWarning??"Pronto. Nenhuma correção de entrada ativada.";}catch(Exception ex){Status="Falha ao carregar perfis: "+ex.Message;}
  try{_raw=new RawInputService(hwnd);_raw.SampleReceived+=OnSample;_raw.DevicesChanged+=OnDevicesChanged;await RefreshAsync();}catch(Exception ex){Status="Captura Raw Input indisponível: "+ex.Message;}
  _loading=false;ConfigureLab();Notify(nameof(PersistenceSummary));
 }
 private async Task RefreshAsync()
 {
  if(_raw is null||_refreshing)return;_refreshing=true;
  try{string? previous=SelectedDevice?.Id;var list=await Task.Run(_raw.EnumerateDevices);if(_disposed)return;Devices.Clear();foreach(var device in list)Devices.Add(device);SelectedDevice=Devices.FirstOrDefault(d=>d.Id==previous)??Devices.FirstOrDefault();Notify(nameof(DiagnosticsSummary));}
  finally{_refreshing=false;}
 }
 private void OnDevicesChanged(){_connectionChanges++;RunAsync(RefreshAsync);}
 private void OnSample(RawMouseSample sample)
 {
  try{OnSampleCore(sample);}catch(Exception ex){if(_lab is not null)_lab.Capturing=false;_input.Stop();TechnicalLog.Record("raw-observer",ex.HResult);Status="Captura interrompida: "+ex.Message;}
 }
 private void OnSampleCore(RawMouseSample sample)
 {
  if(_disposed||SelectedDevice is not {} device||sample.DeviceId!=device.Id)return;
  if(sample.DeltaX!=0||sample.DeltaY!=0){if(_pollingActive)_polling.Add(sample.TimestampMilliseconds);_lab?.Add(sample.DeltaX,sample.DeltaY,sample.TimestampMilliseconds);if(_calibrating)_calibrationCounts+=Math.Sqrt((double)sample.DeltaX*sample.DeltaX+(double)sample.DeltaY*sample.DeltaY);}
  if(PageIndex is 6 or 9){_scrollTicks+=sample.WheelDelta/120;_horizontalScrollTicks+=sample.HorizontalWheelDelta/120;}
 }
 public void ReportForegroundError(string message){AutoProfiles=false;Status="Automação de perfis indisponível: "+message;}
 public async Task SavePendingAsync(){if(_storageReady)await SaveCurrentAsync();}
 private void OnInputStateChanged()=>System.Windows.Application.Current.Dispatcher.BeginInvoke(()=>{if(!_disposed){Notify(nameof(HookStatus));if(_input.LastError is not null)TechnicalLog.Record("desktop-input",1);}});
 private void OnTick(object? sender,EventArgs e)
 {
  bool wasPolling=_pollingActive;
  if(_pollingActive&&Now>=_pollingUntil){_pollingActive=false;Status="Teste de frequência concluído (estimativa por eventos).";}
  if(PageIndex==3||_lab is {Capturing:true}){_lab?.InvalidateVisual();Notify(nameof(LabSummary));}if(wasPolling)UpdateMetrics();if(_lab is not {Capturing:true}&&!_pollingActive)_timer.Stop();
 }
 private void UpdateMetrics()
 {
  var p=_polling.Snapshot();PollingSummary=p.AverageHz is {} hz?$"{(_pollingActive?"Medindo":"Estimativa")} • média {hz:F0} Hz • pico {p.MaximumObservedHz:F0} Hz • desvio de intervalo {p.IntervalStandardDeviationMilliseconds:F3} ms • {p.Intervals} intervalos":"Inconclusivo: poucos eventos de movimento para estimar frequência.";
  PollingHistory=string.Join("  ",p.RecentHz.TakeLast(18).Select(h=>$"{h:F0}"));var c=_clicks.Snapshot();ClickSummary=$"{c.TotalClicks} cliques • {c.PossibleDuplicateIntervals} intervalos <35 ms • {c.Assessment}";
  Notify(nameof(PollingSummary));Notify(nameof(PollingHistory));Notify(nameof(ClickSummary));Notify(nameof(DiagnosticsSummary));
 }
 public void RecordClick(string button){_clicks.Record(button,Now);UpdateMetrics();}
 public void RecordWheel(){UpdateMetrics();}
 private void UpdateDeviceDetails()
 {
  if(SelectedDevice is not {} d){DeviceDetails="Nenhum mouse selecionado.";return;}
  DeviceDetails=$"Nome: {d.Name}\nFabricante: {d.Manufacturer}\nVID: {d.VendorId?.ToString("X4")??"Não informado pelo dispositivo"}\nPID: {d.ProductId?.ToString("X4")??"Não informado pelo dispositivo"}\nConexão: {d.Connection}\nBotões informados pelo Windows: {d.ButtonCount?.ToString()??"Não informado pelo dispositivo"}\nIdentificador/interface: {d.DevicePath}";
  var labels=new Dictionary<CapabilitySupport,string>{{CapabilitySupport.WindowsNative,"Windows"},{CapabilitySupport.Software,"Software"},{CapabilitySupport.Hardware,"Hardware"},{CapabilitySupport.SpecificIntegration,"Integração específica"},{CapabilitySupport.Unsupported,"Não suportado"},{CapabilitySupport.Unknown,"Desconhecido"}};
  CapabilityLines.Clear();foreach(var c in new DeviceCapabilityManager().GetCapabilities(d.VendorId?.ToString("X4"),d.ProductId?.ToString("X4")))CapabilityLines.Add($"{c.Name} · {labels[c.Support]}\n{c.Detail}");
 }
 private MotionSettings Motion(bool enabled)=>new(){Enabled=enabled,Strength=Strength/100,ToleranceDegrees=Tolerance,Horizontal=Horizontal,Vertical=Vertical,HorizontalStrength=HorizontalStrength/100,VerticalStrength=VerticalStrength/100,Diagonal=Diagonal,DiagonalDegrees=DiagonalDegrees,Smoothing=Smoothing/100,DetectionDistance=DetectionDistance,HysteresisDegrees=Hysteresis,ResetAfterMilliseconds=_resetAfterMilliseconds};
 private void ConfigureLab()=>_lab?.Configure(Motion(LabCorrection));
 private void Changed()
 {
  if(_loading)return;try{ConfigureLab();if(_input.IsRunning){_input.ConfigureButtons(X1Action,X2Action);_input.Start(Motion(_cursorMode));}Notify(nameof(SystemSummary));if(_storageReady){_saveTimer.Stop();_saveTimer.Start();}}catch(Exception ex){_input.Stop();Status="Processamento desligado: "+ex.Message;}
 }
 private void Start(bool motion)
 {
  if(_input.IsRunning)throw new InvalidOperationException("Desative o hook antes de trocar de modo.");
  _cursorMode=motion;_input.ConfigureButtons(X1Action,X2Action);_input.Start(Motion(motion));Status="Ativação solicitada. Verifique o estado do hook; Ctrl+Alt+F11 para interromper.";
 }
 private void Stop(){_input.Stop();_cursorMode=false;Status="Desativação solicitada; a entrada original será preservada.";Notify(nameof(HookStatus));}
 private WindowsMouseSettings Settings()=>new(){PointerSpeed=PointerSpeed,EnhancePointerPrecision=EnhancePointerPrecision,SwapButtons=SwapButtons,DoubleClickMilliseconds=DoubleClickMilliseconds,WheelLines=PageScroll?-1:WheelLines,InactiveScroll=InactiveScroll,ClickLock=ClickLock};
 private async Task ReadSettingsAsync(){LoadSettings(await Task.Run(_windows.Read));Status="Valores reais do Windows carregados nos controles.";}
 private readonly SemaphoreSlim _settingsGate=new(1,1);
 private async Task ApplySettingsAsync(){var settings=Settings();if(!await _settingsGate.WaitAsync(0))throw new InvalidOperationException("Aguarde a operação atual do Windows.");try{await Task.Run(()=>_windows.Apply(settings));Status="O Windows confirmou os ajustes globais aplicados.";}finally{_settingsGate.Release();}}
 private async Task RestoreAsync(){var opening=_opening??throw new InvalidOperationException("Não existe snapshot válido da abertura.");if(!await _settingsGate.WaitAsync(0))throw new InvalidOperationException("Aguarde a operação atual do Windows.");try{await Task.Run(()=>_windows.Apply(opening));LoadSettings(await Task.Run(_windows.Read));Status="Configurações da abertura restauradas e confirmadas pelo Windows.";}finally{_settingsGate.Release();}}
 private void LoadSettings(WindowsMouseSettings s)
 {
  bool loading=_loading;_loading=true;PointerSpeed=s.PointerSpeed;EnhancePointerPrecision=s.EnhancePointerPrecision;SwapButtons=s.SwapButtons;DoubleClickMilliseconds=s.DoubleClickMilliseconds;PageScroll=s.WheelLines==-1;WheelLines=Math.Clamp(s.WheelLines,0,100);InactiveScroll=s.InactiveScroll;ClickLock=s.ClickLock;_loading=loading;Notify(nameof(SystemSummary));
 }
 private void LoadProfile(StudioProfile p)
 {
  _loading=true;ProfileName=p.Name;ProfileExecutable=p.ApplicationExecutable??"";PointerSpeed=p.PointerSpeed;EnhancePointerPrecision=p.EnhancePointerPrecision;SwapButtons=p.SwapButtons;DoubleClickMilliseconds=p.DoubleClickMilliseconds;WheelLines=Math.Clamp(p.WheelLines,0,100);PageScroll=p.WheelLines==-1;InactiveScroll=p.InactiveScroll;ClickLock=p.ClickLock;
  var m=p.Motion;_resetAfterMilliseconds=m.ResetAfterMilliseconds;Strength=m.Strength*100;Tolerance=m.ToleranceDegrees;Horizontal=m.Horizontal;Vertical=m.Vertical;HorizontalStrength=m.HorizontalStrength*100;VerticalStrength=m.VerticalStrength*100;Diagonal=m.Diagonal;DiagonalDegrees=m.DiagonalDegrees;Smoothing=m.Smoothing*100;DetectionDistance=m.DetectionDistance;Hysteresis=m.HysteresisDegrees;_selectedPreset="Personalizado";Notify(nameof(SelectedPreset));_loading=false;ConfigureLab();if(_input.IsRunning){_input.ConfigureButtons(X1Action,X2Action);_input.Start(Motion(_cursorMode));}
 }
 private StudioProfile CaptureProfile(Guid? id=null)=>new(){Id=id??SelectedProfile?.Id??Guid.NewGuid(),Name=ProfileName.Trim(),ApplicationExecutable=string.IsNullOrWhiteSpace(ProfileExecutable)?null:ProfileExecutable.Trim(),Motion=Motion(false),PointerSpeed=PointerSpeed,EnhancePointerPrecision=EnhancePointerPrecision,SwapButtons=SwapButtons,DoubleClickMilliseconds=DoubleClickMilliseconds,WheelLines=PageScroll?-1:WheelLines,InactiveScroll=InactiveScroll,ClickLock=ClickLock};
 private async Task SaveCurrentAsync()
 {
  if(!_storageReady)throw new InvalidOperationException("Armazenamento de perfis indisponível; arquivos originais foram preservados.");
  _saveTimer.Stop();var p=CaptureProfile();p.Validate();int index=SelectedProfile is null?-1:Profiles.IndexOf(SelectedProfile);if(index<0)Profiles.Add(p);else Profiles[index]=p;_selectedProfile=p;Notify(nameof(SelectedProfile));await _store.SaveAsync(Profiles.ToArray());Status="Perfil salvo localmente.";
 }
 private async Task CreateProfileAsync(bool duplicate)
 {
  var p=duplicate?CaptureProfile(Guid.NewGuid()) with{Name="Cópia de "+ProfileName}:new StudioProfile{Name="Novo perfil"};p.Validate();Profiles.Add(p);SelectedProfile=p;await SaveCurrentAsync();
 }
 private async Task DeleteProfileAsync()
 {
  if(SelectedProfile is null)return;if(Profiles.Count<2)throw new InvalidOperationException("Mantenha ao menos um perfil.");
  if(MessageBox.Show("Excluir o perfil selecionado?","Perfis",MessageBoxButton.YesNo,MessageBoxImage.Question)!=MessageBoxResult.Yes)return;
  Profiles.Remove(SelectedProfile);SelectedProfile=Profiles[0];await _store.SaveAsync(Profiles.ToArray());Status="Perfil excluído.";
 }
 private async Task ImportAsync(){var dialog=new OpenFileDialog{Filter="Perfil JSON|*.json",CheckFileExists=true};if(dialog.ShowDialog()!=true)return;var p=await _store.ImportAsync(dialog.FileName);Profiles.Add(p);SelectedProfile=p;await SaveCurrentAsync();}
 private async Task ExportAsync(){var p=CaptureProfile();p.Validate();var dialog=new SaveFileDialog{Filter="Perfil JSON|*.json",FileName="perfil-mouse.json"};if(dialog.ShowDialog()==true){await _store.ExportAsync(p,dialog.FileName);Status="Perfil exportado.";}}
 private async Task ResetProfilesAsync()
 {
  if(MessageBox.Show("Substituir todos os perfis pelos seis iniciais?","Perfis",MessageBoxButton.YesNo,MessageBoxImage.Question)!=MessageBoxResult.Yes)return;
  Profiles.Clear();foreach(var p in StudioProfile.CreateDefaults())Profiles.Add(p);SelectedProfile=Profiles[0];await _store.SaveAsync(Profiles.ToArray());Status="Perfis iniciais restaurados.";
 }
 private void ApplyPreset(string preset)
 {
  if(_loading||preset=="Personalizado")return;
  if(preset=="Desativado"){Stop();LabCorrection=false;Strength=0;return;}
  Strength=preset=="Leve"?35:preset=="Forte"?100:65;Tolerance=preset=="Leve"?5:preset=="Forte"?12:8;Horizontal=Vertical=true;Smoothing=0;Diagonal=false;LabCorrection=true;
 }
 private async Task ReportAsync()
 {
  var dialog=new SaveFileDialog{Filter="Relatório JSON|*.json",FileName="mouse-diagnostics.json"};if(dialog.ShowDialog()!=true)return;
  var report=new {Version="0.1.0",CreatedUtc=DateTimeOffset.UtcNow,DeviceCount=Devices.Count,Notifications=_connectionChanges,Polling=_polling.Snapshot(),Clicks=_clicks.Snapshot(),VerticalScrollSteps=_scrollTicks,HorizontalScrollSteps=_horizontalScrollTicks,HookRunning=_input.IsRunning,HardwareValidation="Não realizada nesta distribuição",Scope="Somente dados agregados; sem trajetórias ou identificadores persistentes."};
  await File.WriteAllTextAsync(dialog.FileName,JsonSerializer.Serialize(report,new JsonSerializerOptions{WriteIndented=true}));Status="Relatório técnico exportado sem trajetórias.";
 }
 private void FinishCalibration()
 {
  _calibrating=false;if(!double.TryParse(CalibrationCentimeters,System.Globalization.NumberStyles.Float,System.Globalization.CultureInfo.CurrentCulture,out double cm)||!double.IsFinite(cm)||cm<=0||cm>1000)throw new ArgumentException("Informe uma distância válida em centímetros (0 a 1000).");
  CalibrationResult=_calibrationCounts>0?$"Estimativa: {_calibrationCounts/(cm/2.54):F0} contagens/polegada • {_calibrationCounts:F0} contagens em {cm:F2} cm. Soma do caminho; ruído e desvios afetam o valor. Não é leitura de DPI físico.":"Inconclusivo: nenhum movimento relativo capturado.";
 }
 private async void RunAsync(Func<Task> work){try{await work();}catch(Exception ex){TechnicalLog.Record("async-command",ex.HResult);if(!_disposed)Status="Erro: "+ex.Message;}}
 private void OnSaveTick(object? sender,EventArgs e){_saveTimer.Stop();RunAsync(SaveCurrentAsync);}
 public void NotifyForeground(string executable)
 {
  if(!AutoProfiles||_disposed)return;var p=Profiles.FirstOrDefault(p=>string.Equals(p.ApplicationExecutable,executable,StringComparison.OrdinalIgnoreCase));if(p is null||p.Id==SelectedProfile?.Id)return;
  RunAsync(async()=>{try{SelectedProfile=p;await ApplySettingsAsync();Status=$"Perfil automático: {p.Name}. Ajustes globais aplicados.";}catch(Exception ex){AutoProfiles=false;_input.Stop();Status="Troca automática desativada: "+ex.Message;}});
 }
 private void ChangeTheme()
 {
  var resources=System.Windows.Application.Current.Resources;void Color(string key,string color)=>resources[key]=new SolidColorBrush((Color)ColorConverter.ConvertFromString(color));
  Color("BackgroundBrush",LightTheme?"#F1F5FA":"#0C1320");Color("SurfaceBrush",LightTheme?"#E3EAF3":"#141F30");Color("CardBrush",LightTheme?"#D4DFED":"#1B293D");Color("TextBrush",LightTheme?"#152238":"#EBF0F8");Color("MutedBrush",LightTheme?"#415571":"#A5B6CD");Color("AccentBrush",LightTheme?"#006C58":"#51D6BD");
 }
 public void Dispose(){if(_disposed)return;_disposed=true;_saveTimer.Stop();_timer.Stop();_input.StateChanged-=OnInputStateChanged;_input.Dispose();_raw?.Dispose();_lab?.Clear();}
}
