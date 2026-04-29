using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using SystemParking.Conexion;

namespace SystemParking
{
    /// <summary>
    /// Lógica de interacción para UserControlCupos.xaml
    /// </summary>
    public partial class UserControlCupos : UserControl
    {
        public UserControlCupos()
        {
            InitializeComponent();
            DibujarMapa();
        }
        private void DibujarMapa()
        {
            try
            {
                gridCajones.Children.Clear();
                int totalCupos = 0;

                using (SqlConnection con = LibreriasCXN.Conexion())
                {
                    con.Open();

                    SqlCommand cmdConfig = new SqlCommand("SELECT CuposTotales FROM Configuracion WHERE idConfig = 1", con);
                    totalCupos = (int)cmdConfig.ExecuteScalar();

                    Dictionary<int, string> ocupados = new Dictionary<int, string>();
                    string queryEntradas = "SELECT Cajon, Placa, HoraIngreso FROM Entradas WHERE Estatus = 'Activo'";
                    SqlCommand cmdEntradas = new SqlCommand(queryEntradas, con);
                    SqlDataReader dr = cmdEntradas.ExecuteReader();

                    while (dr.Read())
                    {
                        int numCajon = (int)dr["Cajon"];
                        DateTime hora = Convert.ToDateTime(dr["HoraIngreso"]);
                        string info = dr["Placa"].ToString() + "|" + hora.ToString("HH:mm");

                        // CORRECCIÓN: Usamos indexación directa para evitar el error de llave duplicada
                        // Si el cajón ya existe, se queda con la última información leída.
                        ocupados[numCajon] = info;
                    }
                    dr.Close();

                    int libres = 0;
                    for (int i = 1; i <= totalCupos; i++)
                    {
                        Border cajon = new Border { Margin = new Thickness(4), CornerRadius = new CornerRadius(5) };
                        StackPanel content = new StackPanel { VerticalAlignment = VerticalAlignment.Center };

                        if (ocupados.ContainsKey(i))
                        {
                            cajon.Background = (Brush)new System.Windows.Media.BrushConverter().ConvertFromString("#E74C3C");
                            string[] datos = ocupados[i].Split('|');

                            content.Children.Add(new TextBlock { Text = i.ToString("D2"), Foreground = Brushes.White, FontSize = 9, HorizontalAlignment = HorizontalAlignment.Center });

                            Viewbox vb = new Viewbox { Height = 20 };
                            vb.Child = new TextBlock { Text = datos[0], Foreground = Brushes.White, FontWeight = FontWeights.Bold };
                            content.Children.Add(vb);

                            content.Children.Add(new TextBlock { Text = datos[1] + "h", Foreground = Brushes.White, FontSize = 10, HorizontalAlignment = HorizontalAlignment.Center });
                        }
                        else
                        {
                            cajon.Background = (Brush)new System.Windows.Media.BrushConverter().ConvertFromString("#2ECC71");
                            content.Children.Add(new TextBlock { Text = i.ToString("D2"), Foreground = Brushes.White, FontWeight = FontWeights.Bold, HorizontalAlignment = HorizontalAlignment.Center });
                            libres++;
                        }

                        cajon.Child = content;
                        gridCajones.Children.Add(cajon);
                    }

                    lblCuposLibres.Text = libres.ToString();
                    lblCapacidadTotal.Text = $"Capacidad Total: {totalCupos} espacios";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al generar mapa: " + ex.Message);
            }
        }
        private void btnCerrarVista_Click(object sender, RoutedEventArgs e)
        {
            var parent = VisualTreeHelper.GetParent(this) as ContentControl;
            if (parent == null) // A veces el padre es un Panel (Grid)
            {
                var panel = VisualTreeHelper.GetParent(this) as Panel;
                panel?.Children.Clear();
            }
        }
    }
}
