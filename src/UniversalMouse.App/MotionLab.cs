using System.Windows;
using System.Windows.Media;
using UniversalMouse.Core.Motion;
namespace UniversalMouse.App;
/// <summary>Bounded in-memory comparison; never injects pointer events.</summary>
public sealed class MotionLab : FrameworkElement
{
 private readonly List<(double X,double Y,double Time)> _samples=[];
 private readonly List<Point> _raw=[new(0,0)], _corrected=[new(0,0)];
 private readonly MotionProcessor _processor=new();
 private double _error; private int _count, _correctedCount; private double _angle;
 public bool Capturing {get;set;}
 public int SampleCount=>_samples.Count;
 public int CorrectedSampleCount=>_correctedCount;
 public Point RawPosition=>_raw[^1];
 public Point CorrectedPosition=>_corrected[^1];
 public MotionSettings Settings {get;private set;}=new();
 public string Summary => _count==0
  ? $"{(Capturing?"CAPTURANDO — mova o mouse selecionado; se continuar em zero, use Escolher mouse pelo movimento.":"LABORATÓRIO PAUSADO — clique Iniciar captura ou Escolher mouse pelo movimento.")} • 0 amostras"
  : $"{(Capturing?"CAPTURANDO":_count>=3000?"LIMITE ATINGIDO — inicie uma nova captura":"PAUSADO")} • {_count}/3000 amostras • {_correctedCount} corrigidas • ângulo {_angle:F1}° • correção média {_error/_count:F2} contagens/evento • {(Settings.Enabled?"algoritmo ativo":"algoritmo desligado")} • posição bruta ({_raw[^1].X:F1}; {_raw[^1].Y:F1}) / corrigida ({_corrected[^1].X:F1}; {_corrected[^1].Y:F1})";
 public void Configure(MotionSettings settings)
 {
  if(settings==Settings)return;
  Settings=settings;_processor.Configure(settings);
  if(_samples.Count>0){bool capturing=Capturing;Replay();Capturing=capturing;}
 }
 public void Add(double dx,double dy,double timestamp)
 {
  if(!Capturing || (dx==0&&dy==0))return;
  if(_samples.Count>=3000){Capturing=false;return;}
  _samples.Add((dx,dy,timestamp)); Process(dx,dy,timestamp);
 }
 private void Process(double dx,double dy,double timestamp)
 {
  var r=_processor.Process(dx,dy,timestamp); var p=_raw[^1];var q=_corrected[^1];
  _raw.Add(new(p.X+dx,p.Y+dy));_corrected.Add(new(q.X+r.X,q.Y+r.Y));_angle=r.AngleDegrees;
  if(r.IsCorrected)_correctedCount++;
  _error+=Math.Sqrt((dx-r.X)*(dx-r.X)+(dy-r.Y)*(dy-r.Y));_count++;
 }
 public void Clear(){_samples.Clear();_raw.Clear();_corrected.Clear();_raw.Add(new());_corrected.Add(new());_processor.Reset();_count=0;_correctedCount=0;_error=0;_angle=0;InvalidateVisual();}
 public void Replay(){Capturing=false;_raw.Clear();_corrected.Clear();_raw.Add(new());_corrected.Add(new());_processor.Reset();_count=0;_correctedCount=0;_error=0;foreach(var s in _samples)Process(s.X,s.Y,s.Time);InvalidateVisual();}
 protected override void OnRender(DrawingContext dc)
 {
  base.OnRender(dc);double w=ActualWidth,h=ActualHeight;if(w<1||h<1)return;
  dc.DrawRoundedRectangle(new SolidColorBrush(Color.FromRgb(16,26,41)),null,new Rect(0,0,w,h),8,8);
  var gridPen=new Pen(new SolidColorBrush(Color.FromRgb(34,49,67)),1);gridPen.Freeze();
  for(double x=0;x<w;x+=40)dc.DrawLine(gridPen,new(x,0),new(x,h));for(double y=0;y<h;y+=40)dc.DrawLine(gridPen,new(0,y),new(w,y));
  double minX=0,maxX=0,minY=0,maxY=0;foreach(var p in _raw.Concat(_corrected)){minX=Math.Min(minX,p.X);maxX=Math.Max(maxX,p.X);minY=Math.Min(minY,p.Y);maxY=Math.Max(maxY,p.Y);}
  double scale=Math.Min((w-32)/Math.Max(100,maxX-minX),(h-32)/Math.Max(100,maxY-minY));
  Point Map(Point p)=>new(w/2+(p.X-(maxX+minX)/2)*scale,h/2+(p.Y-(maxY+minY)/2)*scale);
  DrawPath(dc,_raw,Map,new SolidColorBrush(Color.FromRgb(139,156,180)),1.5);
  DrawPath(dc,_corrected,Map,new SolidColorBrush(Color.FromRgb(81,214,189)),2.5);
 }
 private static void DrawPath(DrawingContext dc,List<Point> points,Func<Point,Point> map,Brush brush,double width)
 {
  if(points.Count<2)return;var geometry=new StreamGeometry();using(var context=geometry.Open()){context.BeginFigure(map(points[0]),false,false);for(int i=1;i<points.Count;i++)context.LineTo(map(points[i]),true,false);}geometry.Freeze();dc.DrawGeometry(null,new Pen(brush,width),geometry);
 }
}
