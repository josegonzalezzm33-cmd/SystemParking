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
using System.Windows.Shapes;
using SystemParking.Conexion;

namespace SystemParking
{
    /// <summary>
    /// Lógica de interacción para LoginWindow.xaml
    /// </summary>
    public partial class LoginWindow : Window
    {
        public LoginWindow()
        {
            InitializeComponent();
            this.Loaded += Login_Loaded;
        }

        private void Login_Loaded(object sender, RoutedEventArgs e)
        {
            txtUsuario.Focus();
            Keyboard.Focus(txtUsuario);
        }


        private void btnEntrar_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtUsuario.Text) || string.IsNullOrWhiteSpace(txtPassword.Password))
            {
                MessageBox.Show("Por favor, ingresa tus credenciales completas.", "Campos vacíos", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                using (SqlConnection con = LibreriasCXN.Conexion())
                {
                    con.Open();

                    string query = "SELECT idUsuario, Nombre, Usuario FROM Usuarios WHERE Usuario = @user AND Contraseña = @pass AND EstaActivo = 1";

                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@user", txtUsuario.Text);
                        cmd.Parameters.AddWithValue("@pass", txtPassword.Password);

                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read()) 
                            {

                                UsuarioSesion.IdUsuario = Convert.ToInt32(reader["idUsuario"]);
                                UsuarioSesion.NombreReal = reader["Nombre"].ToString();
                                UsuarioSesion.NombreUsuario = reader["Usuario"].ToString();

                                MainWindow principal = new MainWindow();
                                principal.Show();
                                this.Close();
                            }
                            else
                            {
                                MessageBox.Show("Usuario o contraseña incorrectos, o la cuenta se encuentra desactivada.",
                                                "Acceso Denegado", MessageBoxButton.OK, MessageBoxImage.Error);

                                txtPassword.Clear();
                                txtUsuario.Focus();
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error de conexión: " + ex.Message, "Error Crítico", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnCerrar_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }





        private void txtUsuario_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                txtPassword.Focus();
            }
        }

        private void txtPassword_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                btnEntrar_Click(sender, e);
            }
        }
    }
}
