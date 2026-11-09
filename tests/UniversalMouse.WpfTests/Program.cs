using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using UniversalMouse.App;
using UniversalMouse.Core.Motion;

internal static class Program
{
 [STAThread]
 private static int Main()
 {
  int passed=0;
  try
  {
   System.Windows.Media.RenderOptions.ProcessRenderMode=System.Windows.Interop.RenderMode.SoftwareOnly;
   var app=new UniversalMouse.App.App();app.InitializeComponent();
   var window=new MainWindow();
   Check(window.Title.Contains("0.1.1")&&window.FindName("Lab") is MotionLab,"XAML real e laboratório são carregados");
   var lab=new MotionLab();
   lab.Configure(new MotionSettings{Enabled=true});lab.Capturing=true;
   for(int i=0;i<640;i++)lab.Add(1,i%8==7?1:0,i*2);
   Check(lab.SampleCount==640&&lab.CorrectedSampleCount==80&&Near(lab.RawPosition.Y,80)&&Near(lab.CorrectedPosition.Y,28),"Laboratório real corrige pacotes inteiros sintetizados para o teste");
   lab.Configure(new MotionSettings{Enabled=true,Strength=1});
   Check(lab.SampleCount==640&&lab.Capturing&&Near(lab.CorrectedPosition.Y,0),"Alterar intensidade recalcula a trajetória sem pausar captura");
   lab.Measure(new Size(800,260));lab.Arrange(new Rect(0,0,800,260));lab.UpdateLayout();
   var image=new RenderTargetBitmap(800,260,96,96,PixelFormats.Pbgra32);image.Render(lab);
   var pixels=new byte[800*260*4];image.CopyPixels(pixels,800*4,0);int green=0,gray=0;
   for(int i=0;i<pixels.Length;i+=4){if(pixels[i]==189&&pixels[i+1]==214&&pixels[i+2]==81)green++;if(pixels[i]==180&&pixels[i+1]==156&&pixels[i+2]==139)gray++;}
   Check(green>100&&gray>10,$"WPF desenha trajetórias corrigida e bruta diferentes (verde={green}, cinza={gray})");
   Directory.CreateDirectory("artifacts");var encoder=new PngBitmapEncoder();encoder.Frames.Add(BitmapFrame.Create(image));using(var file=File.Create("artifacts/wpf-motion-lab.png"))encoder.Save(file);
   lab.Configure(new MotionSettings{Enabled=false});
   Check(lab.CorrectedSampleCount==0&&lab.RawPosition==lab.CorrectedPosition,"Desativar algoritmo reproduz a mesma trajetória bruta");
   lab.Capturing=false;lab.Add(100,100,9999);
   Check(lab.SampleCount==640,"Captura pausada não aceita novas amostras");
   lab.Configure(new MotionSettings{Enabled=true,Strength=1});lab.Replay();
   Check(!lab.Capturing&&lab.CorrectedSampleCount==80&&Near(lab.CorrectedPosition.Y,0),"Repetir reprocessa dados reais do controle sem reinjetar entrada");
   lab.Clear();
   Check(lab.SampleCount==0&&lab.CorrectedSampleCount==0&&lab.RawPosition==new Point()&&lab.CorrectedPosition==new Point(),"Limpar remove trajetórias e estatísticas transitórias");
   lab.Capturing=true;for(int i=0;i<3100;i++)lab.Add(1,0,i);
   Check(lab.SampleCount==3000&&!lab.Capturing,"Limite de captura interrompe o teste em 3000 amostras");
   Console.WriteLine($"RESULT {passed}/{passed} testes WPF aprovados; entradas sintetizadas, sem mouse físico nem hook global.");
   return 0;
  }
  catch(Exception ex){Console.Error.WriteLine($"FAIL após {passed} testes WPF: {ex}");return 1;}
  void Check(bool condition,string message){if(!condition)throw new InvalidOperationException(message);passed++;Console.WriteLine("PASS "+message);}
 }
 private static bool Near(double a,double b)=>Math.Abs(a-b)<1e-8;
}
