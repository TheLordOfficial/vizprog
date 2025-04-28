using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Windows;

namespace test12
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            LoadItalok(); // Italok betöltése a listába
        }

        private void LoadItalok()
        {
            string connectionString = ConfigurationManager.ConnectionStrings["MyDatabaseConnectionString"].ConnectionString;
            List<string> italok = new List<string>();

            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    string query = "SELECT Név FROM Italok";

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                italok.Add(reader["Név"].ToString());
                            }
                        }
                    }
                }

                listBoxItalok.ItemsSource = italok;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Hiba történt az italok betöltésekor: {ex.Message}");
            }
        }

        private void ButtonMentes_Click(object sender, RoutedEventArgs e)
        {
            string koktelNev = textBoxKoktelNev.Text.Trim();
            var kivalasztottItalok = listBoxItalok.SelectedItems;

            if (string.IsNullOrEmpty(koktelNev))
            {
                MessageBox.Show("Kérlek adj meg egy koktélnevet.");
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
                int koktelID;

                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    // Új koktél beszúrása
                    string insertKoktelQuery = "INSERT INTO Koktél (Név) OUTPUT INSERTED.ID VALUES (@Név)";
                    using (SqlCommand cmd = new SqlCommand(insertKoktelQuery, connection))
                    {
                        cmd.Parameters.AddWithValue("@Név", koktelNev);
                        koktelID = (int)cmd.ExecuteScalar();
                    }

                    // Összetevők hozzárendelése a koktélhoz
                    foreach (var italNev in kivalasztottItalok)
                    {
                        string insertOsszetevoQuery = @"
INSERT INTO Összetevők (Koktél_ID, Ital_ID)
VALUES (@KoktelID, (SELECT TOP 1 ID FROM Italok WHERE Név = @ItalNev))"; // <-- Itt TOP 1

                        using (SqlCommand cmd = new SqlCommand(insertOsszetevoQuery, connection))
                        {
                            cmd.Parameters.AddWithValue("@KoktelID", koktelID);
                            cmd.Parameters.AddWithValue("@ItalNev", italNev.ToString());
                            cmd.ExecuteNonQuery();
                        }
                    }
                }

                MessageBox.Show("Koktél sikeresen hozzáadva!");
                textBoxKoktelNev.Clear();
                listBoxItalok.UnselectAll();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Hiba történt a koktél mentésekor: {ex.Message}");
            }
        }

        private void ButtonKoktelokListazasa_Click(object sender, RoutedEventArgs e)
        {
            KoktelokWindow koktelokWindow = new KoktelokWindow();
            koktelokWindow.Show();
        }

    }
}
