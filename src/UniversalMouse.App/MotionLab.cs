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
 private double _error; private int _count; private double _angle;
 public bool Capturing {get;set;}
 public MotionSettings Settings {get;private set;}=new();
 public string Summary=>$"{(Capturing?"Capturando":"Pausado")} • {_count} amostras / 3000 • ângulo {_angle:F1}° • correção média {(_count==0?0:_error/_count):F2} contagens/evento • {(Settings.Enabled?"algoritmo ativo":"algoritmo desligado")}";
 public void Configure(MotionSettings settings){Settings=settings;_processor.Configure(settings);}
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
  _error+=Math.Sqrt((dx-r.X)*(dx-r.X)+(dy-r.Y)*(dy-r.Y));_count++;
 }
 public void Clear(){_samples.Clear();_raw.Clear();_corrected.Clear();_raw.Add(new());_corrected.Add(new());_processor.Reset();_count=0;_error=0;_angle=0;InvalidateVisual();}
 public void Replay(){Capturing=false;_raw.Clear();_corrected.Clear();_raw.Add(new());_corrected.Add(new());_processor.Reset();_count=0;_error=0;foreach(var s in _samples)Process(s.X,s.Y,s.Time);InvalidateVisual();}
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
