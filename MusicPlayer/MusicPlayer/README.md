# Reproductor de música

Reproductor de escritorio para Windows, hecho en WPF sobre .NET 10, inspirado en
Apple Music. Reproduce canciones locales (.mp3) con letra sincronizada (.lrc) y
un fondo animado con los colores dominantes de la carátula.

## Requisitos

- Windows 10/11
- Visual Studio 2022 (17.12 o superior) con el workload **".NET Desktop Development"**
  instalado, o el SDK de .NET 10 con el workload de escritorio:
  ```
  dotnet workload install wasm-tools
  ```
  (el instalador del SDK de .NET para Windows ya incluye lo necesario para WPF;
  si usas solo la CLI, asegúrate de tener el SDK "windows desktop" instalado).

## Cómo abrir y correr

1. Abre `MusicPlayer.csproj` con Visual Studio (doble clic, o `Archivo > Abrir > Proyecto`).
2. Visual Studio restaurará automáticamente los paquetes NuGet (NAudio, TagLibSharp,
   CommunityToolkit.Mvvm, ColorThiefDotNet). Si prefieres la terminal:
   ```
   dotnet restore
   dotnet build
   dotnet run
   ```
3. Presiona F5 (o `dotnet run`) para ejecutar.
4. En la pestaña **Biblioteca**, haz clic en "Elegir carpeta" y selecciona la
   carpeta donde tengas tus archivos `.mp3`.
5. Para que la letra sincronizada aparezca, cada `.mp3` necesita un archivo
   `.lrc` con **el mismo nombre exacto**, en la misma carpeta.
   Ejemplo: `This Is How I Disappear.mp3` + `This Is How I Disappear.lrc`.
6. Presiona el botón ▶ junto a una canción para reproducirla; automáticamente
   puedes ir a la pestaña "Reproduciendo" para ver la carátula, el título y la
   letra resaltándose en tiempo real.

## Formato del archivo .lrc

Cada línea lleva el tiempo entre corchetes, seguido del texto:

```
[00:12.30]By streetlight this dark night
[00:16.80]Drain all the blood and give the kids a show
[00:21.10]A séance down below
```

## Estructura del proyecto

```
MusicPlayer/
├── Models/          → Song.cs, LyricLine.cs
├── Services/         → MusicLibraryService, LyricsService, ColorExtractionService
├── ViewModels/       → PlayerViewModel (estado compartido), LibraryViewModel
├── Views/            → LibraryView.xaml (pestaña 1), PlayerView.xaml (pestaña 2)
├── Converters/       → LyricColorConverter (colorea letra pasada/actual/futura)
├── MainWindow.xaml   → TabControl con las dos pestañas
└── App.xaml          → registro del conversor como recurso global
```

## Próximos pasos sugeridos

- Agregar barra de progreso con seek (arrastrar para adelantar/atrasar la canción).
- Guardar la última carpeta usada (para no tener que elegirla cada vez que abres el programa).
- Scroll automático de la letra para mantener la línea activa centrada en pantalla.
- Botones de siguiente/anterior canción y una cola de reproducción.
