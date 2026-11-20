KALKULATOR ŚREDNIEJ WAŻONEJ OCEN - projekt WPF (C# + XAML)

Jak uruchomić w Visual Studio Code (Windows):

1. Zainstaluj .NET SDK 8 (lub nowszy): https://dotnet.microsoft.com/download
   Sprawdzenie w terminalu:  dotnet --version
2. W VS Code zainstaluj rozszerzenie "C# Dev Kit" (Microsoft).
3. File -> Open Folder... -> wybierz ten folder (KalkulatorSredniej).
4. Otwórz terminal (Ctrl + `) i wpisz:
       dotnet run

Jeśli masz tylko .NET 9 lub 10 i pojawi się błąd o wersji,
w pliku KalkulatorSredniej.csproj zmień net8.0-windows na net9.0-windows
albo net10.0-windows (zależnie od wyniku "dotnet --version").

Pliki projektu:
- KalkulatorSredniej.csproj  - ustawienia projektu (WPF, .NET)
- App.xaml / App.xaml.cs     - start aplikacji, wskazuje okno główne
- MainWindow.xaml            - wygląd okna (interfejs)
- MainWindow.xaml.cs         - logika programu (obsługa przycisków, obliczenia)
