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
    /// Lógica de interacción para UserControlReportes.xaml
    /// </summary>
    public partial class UserControlReportes : UserControl
    {
        public UserControlReportes()
        {
            InitializeComponent();
            CargarResumenHoy(); 

            dpDesde.SelectedDate = DateTime.Now;
            dpHasta.SelectedDate = DateTime.Now;
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
        private void CargarResumenHoy()
        {
            try
            {
                using (SqlConnection con = LibreriasCXN.Conexion())
                {
                    con.Open();
                    string query = @"
                        SELECT 
                            (SELECT ISNULL(SUM(TotalPagar), 0) FROM Salidas WHERE CAST(HoraSalida AS DATE) = CAST(GETDATE() AS DATE)) as Ingresos,
                            (SELECT COUNT(*) FROM Entradas WHERE CAST(HoraIngreso AS DATE) = CAST(GETDATE() AS DATE)) as TotalAutos,
                            (SELECT COUNT(*) FROM Entradas WHERE Estatus = 'Activo') as Ocupados";

                    SqlCommand cmd = new SqlCommand(query, con);
                    SqlDataReader dr = cmd.ExecuteReader();

                    if (dr.Read())
                    {
                        lblIngresoHoy.Text = string.Format("{0:C}", dr["Ingresos"]);
                        lblAutosHoy.Text = dr["TotalAutos"].ToString();

                        int ocupados = Convert.ToInt32(dr["Ocupados"]);
                        double porcentaje = (ocupados / 40.0) * 100;
                        lblOcupacion.Text = $"{porcentaje:N0} %";
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error en KPIs: " + ex.Message);
            }
        }

        private void btnGenerarReporte_Click(object sender, RoutedEventArgs e)
        {
            if (dpDesde.SelectedDate == null || dpHasta.SelectedDate == null)
            {
                MessageBox.Show("Por favor selecciona un rango de fechas.");
                return;
            }

            string fechaInicio = dpDesde.SelectedDate.Value.ToString("yyyy-MM-dd") + " 00:00:00";
            string fechaFin = dpHasta.SelectedDate.Value.ToString("yyyy-MM-dd") + " 23:59:59";

            string reporteSeleccionado = (cmbTipoReporte.SelectedItem as ComboBoxItem)?.Content.ToString();
            string query = "";

            switch (reporteSeleccionado)
            {
                case "Corte de Caja Diario":
                    query = $@"SELECT E.Placa, 
                               E.HoraIngreso as [Entrada], 
                               S.HoraSalida as [Salida], 
                               S.TiempoEstacionado as [Duración], 
                               S.TotalPagar as [Cobro]
                        FROM Salidas S
                        INNER JOIN Entradas E ON S.idEntrada = E.idEntrada
                        WHERE S.HoraSalida >= '{fechaInicio}' AND S.HoraSalida <= '{fechaFin}'
                        ORDER BY S.HoraSalida DESC";
                    break;

                case "Lista de Entradas y Salidas":
                    query = $@"
                SELECT Placa, HoraIngreso as [Fecha y Hora], 'ENTRADA' as [Movimiento], Cajon 
                FROM Entradas 
                WHERE HoraIngreso >= '{fechaInicio}' AND HoraIngreso <= '{fechaFin}'
                UNION ALL
                SELECT E.Placa, S.HoraSalida as [Fecha y Hora], 'SALIDA' as [Movimiento], E.Cajon 
                FROM Salidas S 
                INNER JOIN Entradas E ON S.idEntrada = E.idEntrada
                WHERE S.HoraSalida >= '{fechaInicio}' AND S.HoraSalida <= '{fechaFin}'
                ORDER BY [Fecha y Hora] DESC";
                    break;

                case "Ingresos Mensuales":
                    query = @"SELECT DATENAME(MONTH, HoraSalida) as Mes, 
                               COUNT(*) as [Autos], 
                               SUM(TotalPagar) as [Total]
                      FROM Salidas 
                      GROUP BY DATENAME(MONTH, HoraSalida), MONTH(HoraSalida)
                      ORDER BY MONTH(HoraSalida)";
                    break;

                case "Vehículos con más tiempo":
                    query = $@"SELECT TOP 20 E.Placa, E.HoraIngreso, S.HoraSalida, S.TiempoEstacionado, S.TotalPagar
                       FROM Salidas S
                       INNER JOIN Entradas E ON S.idEntrada = E.idEntrada
                       ORDER BY S.TotalPagar DESC";
                    break;
            }

            if (!string.IsNullOrEmpty(query))
            {
                LlenarGrid(query);
            }
        }
        private void LlenarGrid(string sql)
        {
            try
            {
                using (SqlConnection con = LibreriasCXN.Conexion())
                {
                    SqlDataAdapter da = new SqlDataAdapter(sql, con);
                    DataTable dt = new DataTable();
                    da.Fill(dt);
                    dgReportes.ItemsSource = dt.DefaultView;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al consultar: " + ex.Message);
            }
        }

        private void btnExportarExcel_Click(object sender, RoutedEventArgs e)
        {
            if (dgReportes.ItemsSource == null) { MessageBox.Show("No hay datos."); return; }

            Microsoft.Win32.SaveFileDialog sfd = new Microsoft.Win32.SaveFileDialog();
            sfd.Filter = "Libro de Excel (*.xls)|*.xls";
            sfd.FileName = "Reporte_" + (cmbTipoReporte.SelectedItem as ComboBoxItem).Content.ToString();

            if (sfd.ShowDialog() == true)
            {
                try
                {
                    System.Text.StringBuilder sb = new System.Text.StringBuilder();
                    sb.Append("<html><head><meta charset='utf-8'><style>");
                    sb.Append("table { border-collapse: collapse; font-family: Arial; }");
                    sb.Append(".header { background-color: #217346; color: white; font-weight: bold; }");
                    sb.Append("td, th { border: 1px solid #ccc; padding: 5px; }");
                    sb.Append(".title { font-size: 20px; color: #217346; font-weight: bold; }");
                    sb.Append("</style></head><body>");

                    sb.Append($"<div class='title'>PARKING EXPRESS - REPORTE</div>");
                    sb.Append($"<div>Tipo: {(cmbTipoReporte.SelectedItem as ComboBoxItem).Content}</div>");
                    sb.Append($"<div>Fecha: {DateTime.Now}</div><br/>");

                    sb.Append("<table><tr class='header'>");
                    foreach (var col in dgReportes.Columns) sb.Append($"<th>{col.Header}</th>");
                    sb.Append("</tr>");

                    foreach (var item in dgReportes.Items)
                    {
                        sb.Append("<tr>");
                        foreach (var col in dgReportes.Columns)
                        {
                            var cellContent = col.GetCellContent(item) as TextBlock;
                            sb.Append($"<td>{cellContent?.Text ?? ""}</td>");
                        }
                        sb.Append("</tr>");
                    }
                    sb.Append("</table></body></html>");

                    System.IO.File.WriteAllText(sfd.FileName, sb.ToString(), System.Text.Encoding.UTF8);
                    MessageBox.Show("Excel generado con éxito.");
                }
                catch (Exception ex) { MessageBox.Show("Error: " + ex.Message); }
            }
        }

        private void btnExportarPDF_Click(object sender, RoutedEventArgs e)
        {
            if (dgReportes.ItemsSource == null) return;

            System.Text.StringBuilder html = new System.Text.StringBuilder();
            html.Append("<html><head><meta charset='utf-8'><style>");
            html.Append("body { font-family: 'Segoe UI', sans-serif; padding: 30px; color: #333; }"); // Reduje un poco el padding
            html.Append(".header { border-bottom: 3px solid #C0392B; padding-bottom: 10px; margin-bottom: 20px; }");
            html.Append("table { width: 100%; border-collapse: collapse; margin-top: 20px; }");
            html.Append("th { background-color: #C0392B; color: white; padding: 12px; text-align: left; }");
            html.Append("td { padding: 10px; border-bottom: 1px solid #ddd; }");
            html.Append(".footer { margin-top: 30px; font-size: 10px; color: #7f8c8d; text-align: center; }");
            html.Append("</style></head><body>");

            html.Append("<div class='header'><h1>PARKING EXPRESS</h1>");
            html.Append($"<h3>REPORTE: {(cmbTipoReporte.SelectedItem as ComboBoxItem).Content}</h3>");
            html.Append($"<p>Generado el: {DateTime.Now:dd/MM/yyyy HH:mm}</p></div>");

            html.Append("<table><thead><tr>");
            foreach (var col in dgReportes.Columns) html.Append($"<th>{col.Header}</th>");
            html.Append("</tr></thead><tbody>");

            foreach (var item in dgReportes.Items)
            {
                html.Append("<tr>");
                foreach (var col in dgReportes.Columns)
                {
                    var cellContent = col.GetCellContent(item) as TextBlock;
                    html.Append($"<td>{cellContent?.Text ?? ""}</td>");
                }
                html.Append("</tr>");
            }
            html.Append("</tbody></table>");
            html.Append("<div class='footer'>Este documento es un reporte oficial del sistema SystemParking</div>");
            html.Append("</body></html>");

            try
            {
                string tempPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "tempReporte.html");
                System.IO.File.WriteAllText(tempPath, html.ToString(), System.Text.Encoding.UTF8);

                var vista = new Window
                {
                    Title = "Vista Previa de Reporte",
                    Width = 700,       
                    Height = 650,        
                    MinWidth = 500,     
                    MinHeight = 400,
                    WindowStartupLocation = WindowStartupLocation.CenterScreen,
                    Topmost = true    
                };

                var browser = new WebBrowser();
                browser.Navigate(tempPath);

                vista.Content = browser;
                vista.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al generar vista previa: " + ex.Message);
            }
        }
    }
}
