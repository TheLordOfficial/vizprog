using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Windows;
using System.Windows.Controls;

namespace test12
{
    public partial class KoktelModositWindow : Window
    {
        private int koktelID;
        private string eredetiNev;

        public KoktelModositWindow(int id, string koktelNev)
        {
            InitializeComponent();
            koktelID = id;
            eredetiNev = koktelNev;
            this.Title = "Koktél módosítása: " + koktelNev;

            textBoxKoktelNev.Text = koktelNev;

            LoadItalok();
        }

        private class Ital
        {
            public int ID { get; set; }
            public string Nev { get; set; }

            public override string ToString()
            {
                return Nev;
            }
        }

        private void LoadItalok()
        {
            string connectionString = ConfigurationManager.ConnectionStrings["MyDatabaseConnectionString"].ConnectionString;
            List<Ital> italok = new List<Ital>();

            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    string query = "SELECT ID, Név FROM Italok";

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                Ital ital = new Ital();
                                ital.ID = (int)reader["ID"];
                                ital.Nev = reader["Név"].ToString();
                                italok.Add(ital);
                            }
                        }
                    }
                }

                listBoxItalok.ItemsSource = italok;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Hiba történt az italok betöltésekor: " + ex.Message);
            }
        }

        private void ButtonModosit_Click(object sender, RoutedEventArgs e)
        {
            string koktelNev = textBoxKoktelNev.Text.Trim();
            var kivalasztottItalok = listBoxItalok.SelectedItems;

            if (string.IsNullOrEmpty(koktelNev))
            {
                MessageBox.Show("Kérlek, adj meg egy koktélnevet.");
                return;
            }

            if (kivalasztottItalok.Count == 0)
            {
                MessageBox.Show("Válassz legalább egy összetevőt.");
                return;
            }

            string connectionString = ConfigurationManager.ConnectionStrings["MyDatabaseConnectionString"].ConnectionString;

            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    string updateKoktelQuery = "UPDATE Koktél SET Név = @Név WHERE ID = @ID";
                    using (SqlCommand cmd = new SqlCommand(updateKoktelQuery, connection))
                    {
                        cmd.Parameters.AddWithValue("@Név", koktelNev);
                        cmd.Parameters.AddWithValue("@ID", koktelID);
                        cmd.ExecuteNonQuery();
                    }

                    string deleteOsszetevoQuery = "DELETE FROM Összetevők WHERE Koktél_ID = @KoktelID";
                    using (SqlCommand cmd = new SqlCommand(deleteOsszetevoQuery, connection))
                    {
                        cmd.Parameters.AddWithValue("@KoktelID", koktelID);
                        cmd.ExecuteNonQuery();
                    }

                    foreach (object obj in kivalasztottItalok)
                    {
                        Ital ital = obj as Ital;
                        if (ital != null)
                        {
                            string insertOsszetevoQuery = "INSERT INTO Összetevők (Koktél_ID, Ital_ID) VALUES (@KoktelID, @ItalID)";
                            using (SqlCommand cmd = new SqlCommand(insertOsszetevoQuery, connection))
                            {
                                cmd.Parameters.AddWithValue("@KoktelID", koktelID);
                                cmd.Parameters.AddWithValue("@ItalID", ital.ID);
                                cmd.ExecuteNonQuery();
                            }
                        }
                    }
                }

                MessageBox.Show("Koktél sikeresen módosítva!");
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Hiba történt a koktél módosításakor: " + ex.Message);
            }
        }
    }
}
