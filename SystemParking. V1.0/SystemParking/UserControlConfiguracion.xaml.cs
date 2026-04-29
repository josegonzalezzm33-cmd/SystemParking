using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
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
    /// </summary>
    public partial class UserControlConfiguracion : UserControl
    {
        int idUsuarioSeleccionado = 0;
        public UserControlConfiguracion()
        {
            InitializeComponent();
        }

        private void CargarConfiguracionActual()
        {
            try
            {
                using (SqlConnection con = LibreriasCXN.Conexion())
                {
                    con.Open();
                    // Solo traemos la fila con ID 1 que es la configuración global
                    string query = "SELECT TarifaHora, CuposTotales FROM Configuracion WHERE idConfig = 1";

                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        SqlDataReader dr = cmd.ExecuteReader();
                        if (dr.Read())
                        {
                            // Llenamos tus TextBox con lo que hay en la DB
                            txtTarifa.Text = string.Format("{0:F2}", dr["TarifaHora"]);
                            txtCapacidad.Text = dr["CuposTotales"].ToString();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al leer la configuración: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnCerrarVista_Click(object sender, RoutedEventArgs e)
        {
            var parent = VisualTreeHelper.GetParent(this) as ContentControl;
            if (parent == null)
            {
                var panel = VisualTreeHelper.GetParent(this) as Panel;
                panel?.Children.Clear();
            }
        }
        private void CargarUsuarios()
        {
            try
            {
                using (SqlConnection con = LibreriasCXN.Conexion())
                {
                    con.Open();
                    string query = "SELECT idUsuario, Nombre, Usuario, EstaActivo FROM Usuarios";

                    SqlDataAdapter da = new SqlDataAdapter(query, con);
                    DataTable dt = new DataTable();
                    da.Fill(dt);

                    dgUsuarios.ItemsSource = dt.DefaultView;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al cargar la tabla: " + ex.Message);
            }
        }
        private void btnRegistrarUsuario_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtNombreReal.Text) || string.IsNullOrWhiteSpace(txtNuevoUsuario.Text)) return;

            try
            {
                using (SqlConnection con = LibreriasCXN.Conexion())
                {
                    con.Open();
                    string query = "";

                    if (idUsuarioSeleccionado == 0) 
                    {
                        query = "INSERT INTO Usuarios (Nombre, Usuario, Contraseña, EstaActivo) VALUES (@nombre, @user, @pass, @activo)";
                    }
                    else 
                    {
                        string updatePass = !string.IsNullOrWhiteSpace(txtNuevoPass.Password) ? ", Contraseña=@pass" : "";
                        query = $"UPDATE Usuarios SET Nombre=@nombre, Usuario=@user, EstaActivo=@activo {updatePass} WHERE idUsuario=@id";
                    }

                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@nombre", txtNombreReal.Text);
                        cmd.Parameters.AddWithValue("@user", txtNuevoUsuario.Text);
                        cmd.Parameters.AddWithValue("@pass", txtNuevoPass.Password);
                        cmd.Parameters.AddWithValue("@activo", chkNuevoUsuarioActivo.IsChecked == true ? 1 : 0);
                        cmd.Parameters.AddWithValue("@id", idUsuarioSeleccionado);

                        cmd.ExecuteNonQuery();
                    }
                }

                MessageBox.Show(idUsuarioSeleccionado == 0 ? "Registrado!" : "Actualizado!");

                idUsuarioSeleccionado = 0;
                btnRegistrarUsuario.Content = "REGISTRAR USUARIO";
                btnRegistrarUsuario.Background = (System.Windows.Media.Brush)new System.Windows.Media.BrushConverter().ConvertFrom("#1ABC9C");

                txtNombreReal.Clear();
                txtNuevoUsuario.Clear();
                txtNuevoPass.Clear();
                CargarUsuarios();
            }
            catch (Exception ex) { MessageBox.Show(ex.Message); }
        }

        private void btnVerPass_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            txtPassVisible.Text = txtNuevoPass.Password;
            txtNuevoPass.Visibility = Visibility.Collapsed;
            txtPassVisible.Visibility = Visibility.Visible;
        }

        private void btnVerPass_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            txtPassVisible.Visibility = Visibility.Collapsed;
            txtNuevoPass.Visibility = Visibility.Visible;
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            CargarUsuarios();
            CargarConfiguracionActual();

        }

        private void btnModificar_Click(object sender, RoutedEventArgs e)
        {
            DataRowView fila = (DataRowView)((Button)e.Source).DataContext;

            if (fila != null)
            {
                try
                {
                    idUsuarioSeleccionado = Convert.ToInt32(fila["idUsuario"]);

                    using (SqlConnection con = LibreriasCXN.Conexion())
                    {
                        con.Open();
                        string query = "SELECT Nombre, Usuario, Contraseña, EstaActivo FROM Usuarios WHERE idUsuario = @id";
                        SqlCommand cmd = new SqlCommand(query, con);
                        cmd.Parameters.AddWithValue("@id", idUsuarioSeleccionado);

                        SqlDataReader dr = cmd.ExecuteReader();

                        if (dr.Read())
                        {

                            txtNombreReal.Text = dr["Nombre"].ToString();
                            txtNuevoUsuario.Text = dr["Usuario"].ToString();
                            txtNuevoPass.Password = dr["Contraseña"].ToString();
                            chkNuevoUsuarioActivo.IsChecked = Convert.ToBoolean(dr["EstaActivo"]);

                            btnRegistrarUsuario.Content = "ACTUALIZAR DATOS";
                            btnRegistrarUsuario.Background = System.Windows.Media.Brushes.Orange;

                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error al recuperar datos completos: " + ex.Message);
                }
            }
        }

        private void btnGuardarConfig_Click(object sender, RoutedEventArgs e)
        {
            // Validaciones básicas para evitar que el programa truene
            if (!decimal.TryParse(txtTarifa.Text, out decimal tarifa))
            {
                MessageBox.Show("La tarifa debe ser un número válido (ej: 15.00)");
                return;
            }

            if (!int.TryParse(txtCapacidad.Text, out int cupos))
            {
                MessageBox.Show("La cantidad de lugares debe ser un número entero.");
                return;
            }

            try
            {
                using (SqlConnection con = LibreriasCXN.Conexion())
                {
                    con.Open();
                    string query = "UPDATE Configuracion SET TarifaHora = @tarifa, CuposTotales = @cupos WHERE idConfig = 1";

                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@tarifa", tarifa);
                        cmd.Parameters.AddWithValue("@cupos", cupos);

                        int filasAfectadas = cmd.ExecuteNonQuery();

                        if (filasAfectadas > 0)
                        {
                            MessageBox.Show("¡Configuración guardada! El sistema se ajustará a " + cupos + " lugares.",
                                            "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al guardar: " + ex.Message, "Error Crítico", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
