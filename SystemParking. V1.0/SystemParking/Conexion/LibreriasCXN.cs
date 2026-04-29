using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;



namespace SystemParking.Conexion
{
    class LibreriasCXN
    {
        public static SqlConnection Conexion()
        {
            string cadena = @"Server=(local)\SQLEXPRESS;Database=SystemParking;Integrated Security=True;TrustServerCertificate=True;";
            return new SqlConnection(cadena);
        }
    }
}
