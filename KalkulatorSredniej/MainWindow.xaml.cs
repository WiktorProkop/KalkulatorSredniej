using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace KalkulatorSredniej
{
    public partial class MainWindow : Window
    {
        // Dane przedmiotów trzymamy w trzech listach.
        // Element o tym samym indeksie w każdej liście dotyczy tego samego przedmiotu,
        // np. nazwyPrzedmiotow[0], oceny[0] i wagi[0] to jeden przedmiot.
        private List<string> nazwyPrzedmiotow = new List<string>();
        private List<double> oceny = new List<double>();
        private List<int> wagi = new List<int>();

        // Tablica ocen, które istnieją w skali ocen na uczelni
        private double[] dozwoloneOceny = { 2.0, 3.0, 3.5, 4.0, 4.5, 5.0 };

        // Ograniczenie liczby przedmiotów, żeby lista nie rosła bez końca
        private const int MaksLiczbaPrzedmiotow = 30;

        public MainWindow()
        {
            InitializeComponent();
            WyczyscWyniki();
            txtPrzedmiot.Focus();
        }

        // ===================== OBSŁUGA PRZYCISKÓW =====================

        // Dodaje nowy przedmiot do listy po sprawdzeniu poprawności danych
        private void btnDodaj_Click(object sender, RoutedEventArgs e)
        {
            if (nazwyPrzedmiotow.Count >= MaksLiczbaPrzedmiotow)
            {
                PokazBlad("Można dodać maksymalnie " + MaksLiczbaPrzedmiotow + " przedmiotów.");
                return;
            }

            // --- 1. Sprawdzenie nazwy przedmiotu ---
            // Trim() usuwa spacje z początku i końca, żeby " " nie było traktowane jako nazwa
            string nazwa = txtPrzedmiot.Text.Trim();

            if (nazwa == "")
            {
                PokazBlad("Wpisz nazwę przedmiotu.");
                txtPrzedmiot.Focus();
                return;
            }

            if (CzyPrzedmiotIstnieje(nazwa))
            {
                PokazBlad("Przedmiot \"" + nazwa + "\" jest już na liście.");
                txtPrzedmiot.Focus();
                txtPrzedmiot.SelectAll();
                return;
            }

            // --- 2. Sprawdzenie oceny ---
            // Zamieniamy przecinek na kropkę, żeby użytkownik mógł wpisać "4,5" albo "4.5".
            // Potem parsujemy z CultureInfo.InvariantCulture, gdzie separatorem jest kropka,
            // dzięki temu program działa tak samo na polskim i angielskim Windowsie.
            string tekstOceny = txtOcena.Text.Trim().Replace(',', '.');

            if (tekstOceny == "")
            {
                PokazBlad("Wpisz ocenę.");
                txtOcena.Focus();
                return;
            }

            double ocena;
            bool czyLiczba = double.TryParse(tekstOceny, NumberStyles.AllowDecimalPoint,
                                             CultureInfo.InvariantCulture, out ocena);
            if (!czyLiczba)
            {
                PokazBlad("Ocena musi być liczbą, np. 4 lub 4,5.");
                txtOcena.Focus();
                txtOcena.SelectAll();
                return;
            }

            if (!CzyDozwolonaOcena(ocena))
            {
                PokazBlad("Ocena " + ocena + " nie istnieje w skali ocen.\n" +
                          "Dozwolone oceny: 2,0; 3,0; 3,5; 4,0; 4,5; 5,0.");
                txtOcena.Focus();
                txtOcena.SelectAll();
                return;
            }

            // --- 3. Sprawdzenie wagi ---
            // SelectedIndex == -1 oznacza, że nic nie zostało wybrane z listy rozwijanej
            if (cmbWaga.SelectedIndex == -1)
            {
                PokazBlad("Wybierz wagę przedmiotu (punkty ECTS) z listy.");
                cmbWaga.Focus();
                return;
            }

            // Zawartość elementu ComboBox to tekst, więc konwertujemy go na liczbę całkowitą
            ComboBoxItem wybranaWaga = (ComboBoxItem)cmbWaga.SelectedItem;
            int waga = Convert.ToInt32(wybranaWaga.Content);

            // --- 4. Dane są poprawne - zapisujemy je w listach ---
            nazwyPrzedmiotow.Add(nazwa);
            oceny.Add(ocena);
            wagi.Add(waga);

            OdswiezListe();
            WyczyscPolaFormularza();
            WyczyscWyniki(); // stare wyniki są już nieaktualne
            txtPrzedmiot.Focus();
        }

        // Usuwa przedmiot zaznaczony na liście
        private void btnUsun_Click(object sender, RoutedEventArgs e)
        {
            if (nazwyPrzedmiotow.Count == 0)
            {
                PokazInformacje("Lista jest pusta - nie ma czego usuwać.");
                return;
            }

            int indeks = lstPrzedmioty.SelectedIndex;
            if (indeks == -1)
            {
                PokazInformacje("Najpierw zaznacz na liście przedmiot do usunięcia.");
                return;
            }

            MessageBoxResult odpowiedz = MessageBox.Show(
                "Czy na pewno usunąć przedmiot \"" + nazwyPrzedmiotow[indeks] + "\"?",
                "Potwierdzenie", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (odpowiedz == MessageBoxResult.Yes)
            {
                // Usuwamy element o tym samym indeksie ze wszystkich trzech list
                nazwyPrzedmiotow.RemoveAt(indeks);
                oceny.RemoveAt(indeks);
                wagi.RemoveAt(indeks);

                OdswiezListe();
                WyczyscWyniki();
            }
        }

        // Usuwa wszystkie przedmioty z listy
        private void btnWyczysc_Click(object sender, RoutedEventArgs e)
        {
            if (nazwyPrzedmiotow.Count == 0)
            {
                PokazInformacje("Lista jest już pusta.");
                return;
            }

            MessageBoxResult odpowiedz = MessageBox.Show(
                "Czy na pewno usunąć wszystkie przedmioty z listy?",
                "Potwierdzenie", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (odpowiedz == MessageBoxResult.Yes)
            {
                nazwyPrzedmiotow.Clear();
                oceny.Clear();
                wagi.Clear();

                OdswiezListe();
                WyczyscWyniki();
                WyczyscPolaFormularza();
            }
        }

        // Liczy średnią ważoną i pozostałe statystyki, a potem wyświetla je w oknie
        private void btnOblicz_Click(object sender, RoutedEventArgs e)
        {
            if (nazwyPrzedmiotow.Count == 0)
            {
                PokazBlad("Dodaj przynajmniej jeden przedmiot, aby obliczyć średnią.");
                txtPrzedmiot.Focus();
                return;
            }

            double srednia = ObliczSredniaWazona();
            int sumaWag = ObliczSumeWag();

            // Szukanie najwyższej i najniższej oceny - zaczynamy od pierwszej oceny
            // i porównujemy ją z kolejnymi
            double najwyzsza = oceny[0];
            double najnizsza = oceny[0];
            for (int i = 1; i < oceny.Count; i++)
            {
                if (oceny[i] > najwyzsza)
                {
                    najwyzsza = oceny[i];
                }
                if (oceny[i] < najnizsza)
                {
                    najnizsza = oceny[i];
                }
            }

            // Wyświetlenie wyników ("F2" - dwa miejsca po przecinku, "0.0" - jedno)
            txtLiczba.Text = nazwyPrzedmiotow.Count.ToString();
            txtSumaWag.Text = sumaWag.ToString();
            txtNajwyzsza.Text = najwyzsza.ToString("0.0");
            txtNajnizsza.Text = najnizsza.ToString("0.0");
            txtSrednia.Text = srednia.ToString("F2");

            txtKomunikat.Text = PrzygotujKomunikat(srednia, najnizsza);

            // Kolor komunikatu: czerwony, gdy jest ocena niedostateczna, w przeciwnym razie zielony
            if (najnizsza < 3.0)
            {
                txtKomunikat.Foreground = Brushes.Red;
            }
            else
            {
                txtKomunikat.Foreground = Brushes.Green;
            }
        }

        // Pyta o potwierdzenie przy zamykaniu okna, jeśli na liście są dane
        private void Window_Closing(object sender, CancelEventArgs e)
        {
            if (nazwyPrzedmiotow.Count > 0)
            {
                MessageBoxResult odpowiedz = MessageBox.Show(
                    "Wprowadzone dane zostaną utracone. Czy na pewno zamknąć program?",
                    "Zamykanie programu", MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (odpowiedz == MessageBoxResult.No)
                {
                    e.Cancel = true; // anulujemy zamknięcie okna
                }
            }
        }

        // ===================== WŁASNE METODY =====================

        // Średnia ważona = suma (ocena * waga) podzielona przez sumę wag
        private double ObliczSredniaWazona()
        {
            double sumaIloczynow = 0;
            for (int i = 0; i < oceny.Count; i++)
            {
                sumaIloczynow = sumaIloczynow + oceny[i] * wagi[i];
            }

            int sumaWag = ObliczSumeWag();

            // Zabezpieczenie przed dzieleniem przez zero
            if (sumaWag == 0)
            {
                return 0;
            }

            return sumaIloczynow / sumaWag;
        }

        // Zwraca sumę wag (punktów ECTS) wszystkich przedmiotów
        private int ObliczSumeWag()
        {
            int suma = 0;
            foreach (int waga in wagi)
            {
                suma = suma + waga;
            }
            return suma;
        }

        // Sprawdza, czy podana ocena znajduje się w tablicy dozwolonych ocen
        private bool CzyDozwolonaOcena(double ocena)
        {
            foreach (double dozwolona in dozwoloneOceny)
            {
                if (ocena == dozwolona)
                {
                    return true;
                }
            }
            return false;
        }

        // Sprawdza, czy przedmiot o takiej nazwie jest już na liście
        // (bez rozróżniania wielkości liter, więc "Fizyka" i "fizyka" to ten sam przedmiot)
        private bool CzyPrzedmiotIstnieje(string nazwa)
        {
            foreach (string istniejaca in nazwyPrzedmiotow)
            {
                if (istniejaca.ToLower() == nazwa.ToLower())
                {
                    return true;
                }
            }
            return false;
        }

        // Zwraca komunikat słowny zależny od wyniku
        private string PrzygotujKomunikat(double srednia, double najnizszaOcena)
        {
            if (najnizszaOcena < 3.0)
            {
                return "Uwaga: masz przynajmniej jeden niezaliczony przedmiot (ocena 2,0).";
            }
            else if (srednia >= 4.5)
            {
                return "Bardzo dobry wynik, gratulacje!";
            }
            else if (srednia >= 4.0)
            {
                return "Dobry wynik.";
            }
            else if (srednia >= 3.5)
            {
                return "Wynik dość dobry.";
            }
            else
            {
                return "Wynik dostateczny - jest jeszcze co poprawiać.";
            }
        }

        // Przepisuje dane z list do kontrolki ListBox
        private void OdswiezListe()
        {
            lstPrzedmioty.Items.Clear();

            for (int i = 0; i < nazwyPrzedmiotow.Count; i++)
            {
                string wiersz = (i + 1) + ". " + nazwyPrzedmiotow[i]
                              + "   |   ocena: " + oceny[i].ToString("0.0")
                              + "   |   waga: " + wagi[i];
                lstPrzedmioty.Items.Add(wiersz);
            }

            grpLista.Header = "Lista przedmiotów (" + nazwyPrzedmiotow.Count + ")";
        }

        // Czyści pola formularza po dodaniu przedmiotu
        private void WyczyscPolaFormularza()
        {
            txtPrzedmiot.Text = "";
            txtOcena.Text = "";
            cmbWaga.SelectedIndex = -1;
        }

        // Ustawia w polach wyników kreski, gdy wyniki nie są policzone
        private void WyczyscWyniki()
        {
            txtLiczba.Text = "-";
            txtSumaWag.Text = "-";
            txtNajwyzsza.Text = "-";
            txtNajnizsza.Text = "-";
            txtSrednia.Text = "-";
            txtKomunikat.Text = "";
        }

        // Wyświetla okienko z ostrzeżeniem o błędnych danych
        private void PokazBlad(string tresc)
        {
            MessageBox.Show(tresc, "Błąd danych", MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        // Wyświetla okienko z informacją
        private void PokazInformacje(string tresc)
        {
            MessageBox.Show(tresc, "Informacja", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}
