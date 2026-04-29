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
using System.Windows.Threading;
using SystemParking.Conexion;

namespace SystemParking
{
    /// <summary>
    /// Lógica de interacción para UserControlEntradas.xaml
    /// </summary>
    public partial class UserControlEntradas : UserControl
    {
        DispatcherTimer timer;
        public UserControlEntradas()
        {
            InitializeComponent();
            // 2. La lógica del reloj DEBE ir aquí adentro
            txtPlaca.Focus();
            IniciarReloj();
        }

        // Para que la hora se vea en tiempo real mientras el usuario escribe
        private void IniciarReloj()
        {
            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(1);
            timer.Tick += (s, e) => lblHoraEntrada.Text = DateTime.Now.ToString("HH:mm:ss");
            timer.Start();
        }
        private void btnCerrarVista_Click(object sender, RoutedEventArgs e)
        {
            // Esta línea busca al "padre" (el Grid del MainWindow) y le dice que se limpie
            var parent = VisualTreeHelper.GetParent(this) as ContentControl;
            if (parent == null) // A veces el padre es un Panel (Grid)
            {
                var panel = VisualTreeHelper.GetParent(this) as Panel;
                panel?.Children.Clear();
            }
        }

        private void btnRegistrar_Click(object sender, RoutedEventArgs e)
        {
            string placa = txtPlaca.Text.Trim();

            if (string.IsNullOrEmpty(placa))
            {
                MessageBox.Show("Por favor, ingrese la placa del vehículo.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                using (SqlConnection con = LibreriasCXN.Conexion())
                {
                    con.Open();

                    // 1. BUSCAR EL PRIMER CAJÓN DISPONIBLE
                    // Esta consulta busca el primer número entre 1 y el Máximo de configuración 
                    // que no esté en la tabla Entradas con estatus 'Activo'
                    string queryCajon = @"
                        DECLARE @Total INT = (SELECT CuposTotales FROM Configuracion WHERE idConfig = 1);
                        
                        WITH Numeros AS (
                            SELECT 1 AS n
                            UNION ALL
                            SELECT n + 1 FROM Numeros WHERE n < @Total
                        )
                        SELECT TOP 1 n FROM Numeros 
                        WHERE n NOT IN (SELECT Cajon FROM Entradas WHERE Estatus = 'Activo')
                        OPTION (MAXRECURSION 1000);";

                    int cajonAsignado = 0;
                    using (SqlCommand cmdCajon = new SqlCommand(queryCajon, con))
                    {
                        var result = cmdCajon.ExecuteScalar();
                        if (result == null)
                        {
                            MessageBox.Show("¡Estacionamiento Lleno! No hay cajones disponibles.", "Lleno", MessageBoxButton.OK, MessageBoxImage.Stop);
                            return;
                        }
                        cajonAsignado = Convert.ToInt32(result);
                    }

                    // 2. INSERTAR LA ENTRADA
                    string queryInsert = @"INSERT INTO Entradas (Placa, Cajon, HoraIngreso, idUsuarioEntrada, Estatus) 
                                         VALUES (@placa, @cajon, GETDATE(), @idUser, 'Activo')";

                    using (SqlCommand cmdInsert = new SqlCommand(queryInsert, con))
                    {
                        cmdInsert.Parameters.AddWithValue("@placa", placa);
                        cmdInsert.Parameters.AddWithValue("@cajon", cajonAsignado);
                        // Suponiendo que tienes el ID del usuario logueado, si no, usa 1 por ahora
                        cmdInsert.Parameters.AddWithValue("@idUser", 1);

                        cmdInsert.ExecuteNonQuery();

                        MessageBox.Show($"¡Entrada Registrada!\nPlaca: {placa}\nCajón Asignado: {cajonAsignado}",
                                        "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);

                        txtPlaca.Clear();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al registrar entrada: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
