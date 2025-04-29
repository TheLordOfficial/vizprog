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
VALUES (@KoktelID, (SELECT TOP 1 ID FROM Italok WHERE Név = @ItalNev))";

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

        private void ButtonUjItal_Click(object sender, RoutedEventArgs e)
        {
            string ujItalNev = textBoxUjItal.Text.Trim();

            if (string.IsNullOrEmpty(ujItalNev))
            {
                MessageBox.Show("Kérlek adj meg egy ital nevet.");
                return;
            }

            string connectionString = ConfigurationManager.ConnectionStrings["MyDatabaseConnectionString"].ConnectionString;

            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    // Először megnézzük, hogy létezik-e már az ital
                    string checkQuery = "SELECT COUNT(*) FROM Italok WHERE Név = @Nev";
                    using (SqlCommand checkCmd = new SqlCommand(checkQuery, connection))
                    {
                        checkCmd.Parameters.AddWithValue("@Nev", ujItalNev);
                        int count = (int)checkCmd.ExecuteScalar();

                        if (count == 0)
                        {
                            // Ha nem létezik, akkor beszúrjuk
                            string insertQuery = "INSERT INTO Italok (Név, Ital_Típus, Összetevők) VALUES (@Nev, @Tipus, @Osszetevok)";
                            using (SqlCommand insertCmd = new SqlCommand(insertQuery, connection))
                            {
                                insertCmd.Parameters.AddWithValue("@Nev", ujItalNev);
                                insertCmd.Parameters.AddWithValue("@Tipus", "Egyéb");
                                insertCmd.Parameters.AddWithValue("@Osszetevok", "-");
                                insertCmd.ExecuteNonQuery();
                            }
                        }
                        else
                        {
                            MessageBox.Show("Ez az ital már létezik az adatbázisban.");
                            return;
                        }
                    }
                }

                // Újratöltjük az italokat, hogy a frissen hozzáadott ital is megjelenjen
                LoadItalok();

                textBoxUjItal.Clear();
                MessageBox.Show("Új összetevő sikeresen hozzáadva!");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Hiba történt az új ital hozzáadásakor: {ex.Message}");
            }
        }
    }
}
