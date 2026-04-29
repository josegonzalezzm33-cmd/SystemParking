using System.Net.NetworkInformation;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace SystemParking
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        DispatcherTimer timer = new DispatcherTimer();
        public MainWindow()
        {
            InitializeComponent();
            lblNombreUsuario.Text = UsuarioSesion.NombreReal;
            // Configurar Reloj y Red
            timer.Interval = TimeSpan.FromSeconds(1);
            timer.Tick += Timer_Tick;
            timer.Start();

            CheckInternet();
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            lblReloj.Text = DateTime.Now.ToString("hh:mm:ss tt");

            // Probar cada 2 segundos mientras lo testeas
            if (DateTime.Now.Second % 2 == 0)
            {
                CheckInternet();
            }
        }
        private async void CheckInternet()
        {
            bool hayInternet = await Task.Run(() =>
            {
                try
                {
                    System.Net.NetworkInformation.Ping miPing = new System.Net.NetworkInformation.Ping();
                    String host = "8.8.8.8";
                    byte[] buffer = new byte[32];
                    int timeout = 1000; 
                    System.Net.NetworkInformation.PingOptions opciones = new System.Net.NetworkInformation.PingOptions();
                    System.Net.NetworkInformation.PingReply respuesta = miPing.Send(host, timeout, buffer, opciones);

                    return (respuesta.Status == System.Net.NetworkInformation.IPStatus.Success);
                }
                catch
                {
                    return false;
                }
            });

            if (hayInternet)
            {
                indicatorRed.Fill = new SolidColorBrush(Color.FromRgb(26, 188, 156));
                lblStatusRed.Text = "Sistema Online";
            }
            else
            {
                indicatorRed.Fill = new SolidColorBrush(Color.FromRgb(231, 76, 60)); 
                lblStatusRed.Text = "Modo Offline";
            }
        }

        private void btnSalir_Click(object sender, RoutedEventArgs e)
        {

            Application.Current.Shutdown();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            ContenedorPrincipal.Children.Clear();

            UserControlEntradas vistaEntradas = new UserControlEntradas();

            ContenedorPrincipal.Children.Add(vistaEntradas);
        }

        private void btnSalidas_Click(object sender, RoutedEventArgs e)
        {
            ContenedorPrincipal.Children.Clear();
            UserControlSalidas vistaSalidas = new UserControlSalidas();
            ContenedorPrincipal.Children.Add(vistaSalidas);
        }

        private void btnReportes_Click(object sender, RoutedEventArgs e)
        {
            ContenedorPrincipal.Children.Clear();
            UserControlReportes vistaReportes = new UserControlReportes();

            ContenedorPrincipal.Children.Add(vistaReportes);
        }

        private void btnEstadoCupos_Click(object sender, RoutedEventArgs e)
        {
            ContenedorPrincipal.Children.Clear();

            UserControlCupos vistaCupos = new UserControlCupos();

            ContenedorPrincipal.Children.Add(vistaCupos);
        }

        private void btnConfiguracion_Click(object sender, RoutedEventArgs e)
        {
            ContenedorPrincipal.Children.Clear();
            UserControlConfiguracion vistaConfig = new UserControlConfiguracion();

            ContenedorPrincipal.Children.Add(vistaConfig);
        }
    }
}