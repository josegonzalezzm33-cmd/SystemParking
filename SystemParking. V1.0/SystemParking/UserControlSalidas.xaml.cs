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
    /// Lógica de interacción para UserControlSalidas.xaml
    /// </summary>
    public partial class UserControlSalidas : UserControl
    {
        int idEntradaEncontrada = 0;
        decimal tarifaPorHora = 0;
        public UserControlSalidas()
        {
            InitializeComponent();
            txtBuscaPlaca.Focus();
        }

        private void btnCerrarVista_Click(object sender, RoutedEventArgs e)
        {
            var panel = VisualTreeHelper.GetParent(this) as Panel;
            panel?.Children.Clear();
        }

        private void btnBuscar_Click(object sender, RoutedEventArgs e)
        {
            string placaABuscar = txtBuscaPlaca.Text.Trim();
            if (string.IsNullOrEmpty(placaABuscar)) return;

            try
            {
                using (SqlConnection con = LibreriasCXN.Conexion())
                {
                    con.Open();

                    // 1. CORRECCIÓN: Cambiamos 'Tarifa' por 'TarifaHora' que es como está en tu DB
                    string query = @"
                SELECT E.idEntrada, E.HoraIngreso, C.TarifaHora 
                FROM Entradas E, Configuracion C
                WHERE E.Placa = @placa AND E.Estatus = 'Activo' AND C.idConfig = 1";

                    SqlCommand cmd = new SqlCommand(query, con);
                    cmd.Parameters.AddWithValue("@placa", placaABuscar);

                    SqlDataReader dr = cmd.ExecuteReader();

                    if (dr.Read())
                    {
                        idEntradaEncontrada = Convert.ToInt32(dr["idEntrada"]);
                        DateTime horaEntrada = Convert.ToDateTime(dr["HoraIngreso"]);

                        // CORRECCIÓN: También aquí usamos 'TarifaHora'
                        tarifaPorHora = Convert.ToDecimal(dr["TarifaHora"]);

                        // 2. Calcular tiempo y total
                        DateTime horaSalida = DateTime.Now;
                        TimeSpan diferencia = horaSalida - horaEntrada;

                        double horasTotales = diferencia.TotalHours;

                        // Si quieres cobrar mínimo 1 hora aunque lleven 5 min:
                        if (horasTotales < 1) horasTotales = 1;

                        decimal total = (decimal)horasTotales * tarifaPorHora;

                        // Redondeo de centavos a peso superior
                        decimal totalRedondeado = Math.Ceiling(total);

                        // 3. Mostrar en la interfaz
                        lblResumenEntrada.Text = horaEntrada.ToString("HH:mm:ss");
                        lblTiempo.Text = $"{(int)diferencia.TotalHours}h {diferencia.Minutes}min";
                        lblTotal.Text = $"$ {totalRedondeado:N2}";
                    }
                    else
                    {
                        MessageBox.Show("No se encontró un vehículo activo con esa placa.");
                        LimpiarCampos();
                    }
                }
            }
            catch (Exception ex) { MessageBox.Show("Error: " + ex.Message); }
        }
        private void btnCobrar_Click(object sender, RoutedEventArgs e)
        {
            if (idEntradaEncontrada == 0) return;

            try
            {
                using (SqlConnection con = LibreriasCXN.Conexion())
                {
                    con.Open();
                    SqlTransaction trans = con.BeginTransaction();

                    try
                    {
                        // 1. Insertar en Salidas
                        string insSalida = @"INSERT INTO Salidas (idEntrada, HoraSalida, TiempoEstacionado, TotalPagar, idUsuarioSalida) 
                                           VALUES (@idE, GETDATE(), @tiempo, @total, @idU)";

                        SqlCommand cmdSal = new SqlCommand(insSalida, con, trans);
                        cmdSal.Parameters.AddWithValue("@idE", idEntradaEncontrada);
                        cmdSal.Parameters.AddWithValue("@tiempo", lblTiempo.Text);
                        // Quitamos el signo de peso para guardar el decimal
                        cmdSal.Parameters.AddWithValue("@total", decimal.Parse(lblTotal.Text.Replace("$ ", "")));
                        cmdSal.Parameters.AddWithValue("@idU", 1); // Usuario logueado

                        cmdSal.ExecuteNonQuery();

                        // 2. Actualizar Estatus en Entradas a 'Finalizado'
                        string updEntrada = "UPDATE Entradas SET Estatus = 'Finalizado' WHERE idEntrada = @idE";
                        SqlCommand cmdUpd = new SqlCommand(updEntrada, con, trans);
                        cmdUpd.Parameters.AddWithValue("@idE", idEntradaEncontrada);

                        cmdUpd.ExecuteNonQuery();

                        trans.Commit();
                        MessageBox.Show("¡Cobro exitoso! El cajón ha sido liberado.");
                        LimpiarCampos();
                    }
                    catch (Exception ex)
                    {
                        trans.Rollback();
                        throw ex;
                    }
                }
            }
            catch (Exception ex) { MessageBox.Show("Error al cobrar: " + ex.Message); }
        }
        private void LimpiarCampos()
        {
            idEntradaEncontrada = 0;
            txtBuscaPlaca.Clear();
            lblResumenEntrada.Text = "--:--:--";
            lblTiempo.Text = "0 min";
            lblTotal.Text = "$ 0.00";
        }
    }
}
