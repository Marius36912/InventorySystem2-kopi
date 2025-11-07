// =======================================================
// README: Inventory System
// Aflevering for uge 6 – Industriel Programmering
// =======================================================

// Projekt:
// Dette program er et simpelt Inventory System skrevet i C# med Avalonia GUI
// Programmet viser et lager, ordrer i kø og ordrer som er blevet behandlet.
// Der er to DataGrids i vinduet,  et til "Queued Orders" og et til "Processed Orders".
// Når brugeren trykker på knappen process next order, flyttes næste ordre fra køen over i listen med færdigbehandlede ordrer.
// Omsætningen opdateres automatisk.

// Opbygning:
// GUI lavet i avalonia XAML.
// Logik placeret i viewmodel med mvvm.
// Domæneklasser er som angivet (Item, Order, Inventory osv.) ligger i models mappe.
// MainWindow.xaml viser data med bindinger til ViewModel.
// Program.cs starter appen via Avalonia’s desktop-lifetime.

// AI-brug:
// Denne aflevering er udviklet med hjælp fra 
// kilde: ChatGPT (OpenAI, 2025).
// ChatGPT har været brugt som feedback og kodeassistent under arbejdet.
// Konkret har jeg brugt AI til at:
// Generere et grundskelet for Avalonia GUI og MVVM-struktur ud fra mine udarbejdede aktivities og noter fra timen.
// Hjælpe med at rette build fejl og forstå bindinger i XAML, samt sparring med noter og pensum for besvarelse af aflevering.
// Den har derudover givet forslag til kommentarer og forenkling af viewmodel koden.
// Brugte også til read me og class diagram skelet, hvor jeg har skrevet om til eget sprog.
// Jeg har selv skrevet og tilpasset al kode, gennemgået logikken,
// og indsat mine egne danske kommentarer for at vise forståelse af pensum.
// Jeg tager fuldt ansvar for den endelige kode, struktur og rapport.
// Jeg er ansvarlig for den afleverede løsning.

//Filervedlagt
// Screen cap
// Flowdiagram
// Class diagram

README.md — InventorySystemRobotControl
## aflevering 7
# InventorySystemRobotControl  
Aflevering – Industriel Programmering (Uge 6 + 7)

## Projektoversigt
Dette projekt kombinerer et **Inventory System** (uge 6) med **robotstyring** (uge 7) i C# og avalonia gui

Uge 6: Lagerstyring og GUI
Uge 7: Udvidelse med når en ordre behandles, sender programmet ur script ti lur sim
Robotten henter op til tre varer fra A,B,C og placerer i S
Jeg satte docker og image op med temrinalen
- Docker Desktop med image `universalrobots/ursim_e-series`

## Her er min Port mapping taget fra terminal
| Funktion | Port |
|-----------|------|
| VNC (interface) | 6080 |
| Dashboard | 29999 |
| URScript (stream) | 30002 |

## Kørselstrin vejledning
### Start URSim i Docker
```bash
docker run -d --name robot-simulator \
  -p 6080:6080 -p 29999:29999 -p 30002:30002 \
  -e ROBOT_MODEL=UR3 \
  universalrobots/ursim_e-series
Åbn URSim-pendant
http://localhost:6080/vnc.html?autoconnect=true 
Tryk Power On og start Brake Release (grøn). 
Valgfrit (dashboard kommandoer) eller run program i rider

printf "power on\n" | nc localhost 29999
printf "brake release\n" | nc localhost 29999
Kør appen fra Rider
1. Run i Rider og GUI starter.
2. Klik Process next order -> robot laver A til S, B til S, C til S pr. ordre
3. Tryk igen (≥ 2 ordrer = kravet “test med mindst to ordrer”).
4. GUI viser opdateret omsætning og flyttede ordrer.

## video
Viser:
1. URSim → Power On + Brake Release (grøn).
2. App startes i Rider.
3. Process next order × 3 → robot bygger A,B,C → S hver gang.
4. GUI viser kø, ordre og pris.

## ai brug
AI-brug:
Denne aflevering er udviklet med hjælp fra 
kilde: ChatGPT (OpenAI, 2025).
ChatGPT har været brugt som feedback og kodeassistent under arbejdet.
Konkret har jeg brugt AI til at:
Generere et grundskelet for Avalonia GUI og MVVM-struktur ud fra mine udarbejdede aktivities og noter fra timen.
Hjælpe med at rette build fejl og forstå bindinger i XAML, samt sparring med noter og pensum for besvarelse af aflevering.
Den har derudover givet forslag til kommentarer og forenkling af viewmodel koden.
Brugte også til read me og class diagram skelet, hvor jeg har skrevet om til eget sprog.
Jeg har selv skrevet og tilpasset al kode, gennemgået logikken,
og indsat mine egne danske kommentarer for at vise forståelse af pensum.
Jeg tager fuldt ansvar for den endelige kode, struktur og rapport.
Jeg er ansvarlig for den afleverede løsning.


// =======================================================

Week 09: Database (Persistent Inventory)

- Programmet bruger SQLite-database (`inventory_main`) til at gemme varer og ordrer.
- Process Order opdaterer tabellerne `Items` og `Orders` i databasen.
- Reset DB nulstiller indholdet uden at slette selve filen.
- Efter genstart bevares ændringerne i databasen.
- Se video-demoen: [link til video]

## ai brug
AI-brug:
Denne aflevering er udviklet med hjælp fra 
kilde: ChatGPT (OpenAI, 2025).
ChatGPT har været brugt som feedback og kodeassistent under arbejdet.
Konkret har jeg brugt AI til at:
Generere et grundskelet for Avalonia GUI og MVVM-struktur ud fra mine udarbejdede aktivities og noter fra timen.
Hjælpe med at rette build fejl og forstå bindinger i XAML, samt sparring med noter og pensum for besvarelse af aflevering.
Den har derudover givet forslag til kommentarer og forenkling af viewmodel koden.
Brugte også til read me og class diagram skelet, hvor jeg har skrevet om til eget sprog.
Jeg har selv skrevet og tilpasset al kode, gennemgået logikken,
og indsat mine egne danske kommentarer for at vise forståelse af pensum.
Jeg tager fuldt ansvar for den endelige kode, struktur og rapport.
Jeg er ansvarlig for den afleverede løsning.
