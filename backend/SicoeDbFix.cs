using System;
using System.Data;
using Oracle.ManagedDataAccess.Client;

class Program {
    static void Main() {
        string cs = "Data Source=(DESCRIPTION=(ADDRESS_LIST=(ADDRESS=(PROTOCOL=tcp)(HOST=scan-cludbadm3.policia.gov.co)(PORT=1538)))(CONNECT_DATA=(SERVER=dedicated)(SERVICE_NAME=TELEPOL_WLGENERAL)));User ID=USR_COEST;Password=S1C03_2026*;Pooling=true;Min Pool Size=5;Max Pool Size=100;Validate Connection=true;Connection Timeout=60";
        using (var conn = new OracleConnection(cs)) {
            conn.Open();
            using (var cmd = conn.CreateCommand()) {
                try {
                    cmd.CommandText = "ALTER TABLE USR_COEST.LINEA_MANDO DROP CONSTRAINT UQ_LINEA_MANDO_IDENT_VIGENTE";
                    cmd.ExecuteNonQuery();
                    Console.WriteLine("Constraint UQ_LINEA_MANDO_IDENT_VIGENTE dropped successfully.");
                } catch (Exception ex) {
                    Console.WriteLine("Error dropping constraint: " + ex.Message);
                }
                
                try {
                    cmd.CommandText = "CREATE UNIQUE INDEX UQ_LINEA_MANDO_IDENT_ACTIVO ON USR_COEST.LINEA_MANDO (CASE WHEN VIGENTE = 1 THEN IDENTIFICACION ELSE NULL END)";
                    cmd.ExecuteNonQuery();
                    Console.WriteLine("Unique index UQ_LINEA_MANDO_IDENT_ACTIVO created successfully.");
                } catch (Exception ex) {
                    Console.WriteLine("Error creating index: " + ex.Message);
                }
            }
        }
    }
}
