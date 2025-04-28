using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace test12
{
    public partial class KoktelokWindow : Window
    {
        public KoktelokWindow()
        {
            InitializeComponent();
            LoadKoktelok();
        }

        private void LoadKoktelok()
        {
            string connectionString = ConfigurationManager.ConnectionStrings["MyDatabaseConnectionString"].ConnectionString;

            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    string query = @"
                        SELECT k.Név AS KoktelNev, i.Név AS ItalNev
                        FROM Koktél k
                        JOIN Összetevők o ON k.ID = o.Koktél_ID
                        JOIN Italok i ON o.Ital_ID = i.ID
                        ORDER BY k.Név
                    ";

                    SqlCommand command = new SqlCommand(query, connection);
                    SqlDataReader reader = command.ExecuteReader();

                    Dictionary<string, List<string>> koktelok = new Dictionary<string, List<string>>();

                    while (reader.Read())
                    {
                        string koktelNev = reader["KoktelNev"].ToString();
                        string italNev = reader["ItalNev"].ToString();

                        if (!koktelok.ContainsKey(koktelNev))
                        {
                            koktelok[koktelNev] = new List<string>();
                        }
                        koktelok[koktelNev].Add(italNev);
                    }

                    foreach (var koktel in koktelok)
                    {
                        TextBlock koktelNevTextBlock = new TextBlock
                        {
                            Text = koktel.Key,
                            FontSize = 18,
                            FontWeight = FontWeights.Bold,
                            Margin = new Thickness(0, 10, 0, 0)
                        };

                        stackPanelKoktelok.Children.Add(koktelNevTextBlock);

                        foreach (var ital in koktel.Value)
                        {
                            TextBlock italTextBlock = new TextBlock
                            {
                                Text = "- " + ital,
                                Margin = new Thickness(10, 2, 0, 0)
                            };
                            stackPanelKoktelok.Children.Add(italTextBlock);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Hiba történt a koktélok betöltésekor: {ex.Message}");
            }
        }
    }
}
