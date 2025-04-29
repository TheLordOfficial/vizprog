using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Windows;
using System.Windows.Controls;

namespace test12
{
    public partial class KoktelokWindow : Window
    {
        public KoktelokWindow()
        {
            InitializeComponent();
            FrissitKoktelokListat();
        }

        private void FrissitKoktelokListat()
        {
            stackPanelKoktelok.Children.Clear();

            string connectionString = ConfigurationManager.ConnectionStrings["MyDatabaseConnectionString"].ConnectionString;

            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    string query = @"SELECT ID, Név FROM Koktél ORDER BY Név";

                    SqlCommand command = new SqlCommand(query, connection);
                    SqlDataReader reader = command.ExecuteReader();

                    while (reader.Read())
                    {
                        int koktelID = (int)reader["ID"];
                        string koktelNev = reader["Név"].ToString();

                        StackPanel koktelSor = new StackPanel
                        {
                            Orientation = Orientation.Horizontal,
                            Margin = new Thickness(0, 10, 0, 0),
                            HorizontalAlignment = HorizontalAlignment.Left
                        };

                        TextBlock koktelNevTextBlock = new TextBlock
                        {
                            Text = koktelNev,
                            FontSize = 16,
                            Width = 150,
                            VerticalAlignment = VerticalAlignment.Center
                        };

                        Button modositButton = new Button
                        {
                            Content = "Módosítás",
                            Margin = new Thickness(5, 0, 0, 0),
                            Tag = koktelID
                        };
                        modositButton.Click += ModositButton_Click;

                        Button torlesButton = new Button
                        {
                            Content = "Törlés",
                            Margin = new Thickness(5, 0, 0, 0),
                            Tag = koktelID
                        };
                        torlesButton.Click += TorlesButton_Click;

                        Button reszletekButton = new Button
                        {
                            Content = "Részletek",
                            Margin = new Thickness(5, 0, 0, 0),
                            Tag = koktelID
                        };
                        reszletekButton.Click += ReszletekButton_Click;

                        koktelSor.Children.Add(koktelNevTextBlock);
                        koktelSor.Children.Add(modositButton);
                        koktelSor.Children.Add(torlesButton);
                        koktelSor.Children.Add(reszletekButton);

                        stackPanelKoktelok.Children.Add(koktelSor);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Hiba történt a koktélok betöltésekor: {ex.Message}");
            }
        }

        private void ModositButton_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            int koktelID = (int)button.Tag;

            string koktelNev = "";

            string connectionString = ConfigurationManager.ConnectionStrings["MyDatabaseConnectionString"].ConnectionString;
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                string query = "SELECT Név FROM Koktél WHERE ID = @ID";
                SqlCommand command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@ID", koktelID);
                SqlDataReader reader = command.ExecuteReader();
                if (reader.Read())
                {
                    koktelNev = reader["Név"].ToString();
                }
            }

            KoktelModositWindow modositWindow = new KoktelModositWindow(koktelID, koktelNev);
            modositWindow.Closed += (s, args) => FrissitKoktelokListat();
            modositWindow.ShowDialog();
        }

        private void TorlesButton_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            int koktelID = (int)button.Tag;

            if (MessageBox.Show("Biztosan törölni szeretnéd ezt a koktélt?", "Megerősítés", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                string connectionString = ConfigurationManager.ConnectionStrings["MyDatabaseConnectionString"].ConnectionString;

                try
                {
                    using (SqlConnection connection = new SqlConnection(connectionString))
                    {
                        connection.Open();

                        string deleteOsszetevokQuery = "DELETE FROM Összetevők WHERE Koktél_ID = @ID";
                        using (SqlCommand cmd = new SqlCommand(deleteOsszetevokQuery, connection))
                        {
                            cmd.Parameters.AddWithValue("@ID", koktelID);
                            cmd.ExecuteNonQuery();
                        }

                        string deleteKoktelQuery = "DELETE FROM Koktél WHERE ID = @ID";
                        using (SqlCommand cmd = new SqlCommand(deleteKoktelQuery, connection))
                        {
                            cmd.Parameters.AddWithValue("@ID", koktelID);
                            cmd.ExecuteNonQuery();
                        }
                    }

                    MessageBox.Show("Koktél sikeresen törölve!");
                    FrissitKoktelokListat();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Hiba történt a törléskor: {ex.Message}");
                }
            }
        }

        private void ReszletekButton_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            int koktelID = (int)button.Tag;

            string connectionString = ConfigurationManager.ConnectionStrings["MyDatabaseConnectionString"].ConnectionString;

            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    string query = @"
                        SELECT i.Név
                        FROM Összetevők o
                        INNER JOIN Italok i ON o.Ital_ID = i.ID
                        WHERE o.Koktél_ID = @ID";

                    SqlCommand command = new SqlCommand(query, connection);
                    command.Parameters.AddWithValue("@ID", koktelID);

                    List<string> osszetevok = new List<string>();

                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            osszetevok.Add(reader["Név"].ToString());
                        }
                    }

                    if (osszetevok.Count == 0)
                    {
                        MessageBox.Show("Nincs összetevő ehhez a koktélhoz.");
                    }
                    else
                    {
                        string uzenet = "Összetevők:\n" + string.Join("\n", osszetevok);
                        MessageBox.Show(uzenet, "Koktél összetevői");
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Hiba történt az összetevők lekérésekor: {ex.Message}");
            }
        }

        private void KeresesButton_Click(object sender, RoutedEventArgs e)
        {
            string keresesiSzoveg = textBoxOsszetevok.Text.Trim();
            if (string.IsNullOrEmpty(keresesiSzoveg))
            {
                FrissitKoktelokListat();
                return;
            }

            string connectionString = ConfigurationManager.ConnectionStrings["MyDatabaseConnectionString"].ConnectionString;

            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    string queryKoktelok = @"
                        SELECT DISTINCT k.ID, k.Név
                        FROM Koktél k
                        LEFT JOIN Összetevők o ON k.ID = o.Koktél_ID
                        LEFT JOIN Italok i ON o.Ital_ID = i.ID
                        WHERE i.Név LIKE @Kereses OR k.Név LIKE @Kereses";

                    SqlCommand commandKoktelok = new SqlCommand(queryKoktelok, connection);
                    commandKoktelok.Parameters.AddWithValue("@Kereses", "%" + keresesiSzoveg + "%");

                    List<int> talaltKoktelok = new List<int>();
                    Dictionary<int, string> koktelNevek = new Dictionary<int, string>();

                    using (SqlDataReader reader = commandKoktelok.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            int koktelID = (int)reader["ID"];
                            string koktelNev = reader["Név"].ToString();

                            talaltKoktelok.Add(koktelID);
                            koktelNevek[koktelID] = koktelNev;
                        }
                    }

                    if (talaltKoktelok.Count == 0)
                    {
                        MessageBox.Show("Nincs találat.");
                        return;
                    }

                    string queryOsszetevok = @"
                        SELECT k.ID AS KoktelID, i.Név AS ItalNev
                        FROM Koktél k
                        JOIN Összetevők o ON k.ID = o.Koktél_ID
                        JOIN Italok i ON o.Ital_ID = i.ID
                        WHERE k.ID IN (" + string.Join(",", talaltKoktelok) + @")
                        ORDER BY k.Név, i.Név";

                    SqlCommand commandOsszetevok = new SqlCommand(queryOsszetevok, connection);

                    Dictionary<int, List<string>> koktelOsszetevok = new Dictionary<int, List<string>>();

                    using (SqlDataReader reader = commandOsszetevok.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            int koktelID = (int)reader["KoktelID"];
                            string italNev = reader["ItalNev"].ToString();

                            if (!koktelOsszetevok.ContainsKey(koktelID))
                            {
                                koktelOsszetevok[koktelID] = new List<string>();
                            }

                            koktelOsszetevok[koktelID].Add(italNev);
                        }
                    }

                    stackPanelKoktelok.Children.Clear();

                    foreach (var koktelID in talaltKoktelok)
                    {
                        TextBlock koktelNevTextBlock = new TextBlock
                        {
                            Text = koktelNevek[koktelID],
                            FontSize = 18,
                            FontWeight = FontWeights.Bold,
                            Margin = new Thickness(0, 10, 0, 0)
                        };
                        stackPanelKoktelok.Children.Add(koktelNevTextBlock);

                        if (koktelOsszetevok.ContainsKey(koktelID))
                        {
                            foreach (var osszetevo in koktelOsszetevok[koktelID])
                            {
                                TextBlock osszetevoTextBlock = new TextBlock
                                {
                                    Text = "- " + osszetevo,
                                    Margin = new Thickness(10, 2, 0, 0),
                                    FontSize = 14
                                };
                                stackPanelKoktelok.Children.Add(osszetevoTextBlock);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Hiba történt a kereséskor: {ex.Message}");
            }
        }
    }
}
