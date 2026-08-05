Predare proiect CommandTrack
1. Scopul proiectului
CommandTrack este o platformă demonstrativă de tip command-and-control pentru monitorizarea unor unități operaționale, precum roboți tereștri.
Sistemul trebuie să permită:
înregistrarea și afișarea unităților;
actualizarea statusului operațional;
primirea de heartbeat-uri;
detectarea automată a unităților inactive;
colectarea și păstrarea istoricului telemetriei;
simularea unei platforme reale;
afișarea datelor într-o aplicație desktop HQ;
schimbarea manuală a statusului din HQ;
afișarea graficelor pentru baterie și viteză.
Proiectul este momentan un MVP local pentru demo, nu o aplicație pregătită pentru producție.
2. Starea curentă a repository-ului
Branch curent: master
master este sincronizat cu GitHub.
Ultimul PR integrat: Update vulnerable OpenAPI package
Branch-ul local feature/project-stabilization a fost șters.
Ultimul build verificat:
2 succeeded
0 failed
Pachetul vulnerabil Microsoft.OpenApi 2.0.0 a fost actualizat la o versiune sigură din seria 2.x, folosită în proiect.
API-ul, simulatorul și HQ-ul au fost testate după actualizare și funcționează.
Workflow-ul folosit este:
master
→ feature/<functionalitate>
→ commit și push
→ Pull Request pe GitHub
→ merge în master
→ pull local
→ build
→ ștergere branch local
Nu se lucrează direct pe master.
3. Arhitectura soluției
Soluția conține șapte proiecte.
CommandTrack.Domain
Conține modelul de domeniu:
OperationalUnit
UnitTelemetry
enum-ul UnitStatus
Nu trebuie să depindă de API, Entity Framework sau WPF.
CommandTrack.Infrastructure
Conține accesul la SQL Server prin Entity Framework Core:
CommandTrackDbContext
configurațiile entităților;
migrațiile EF Core.
CommandTrack.Api
ASP.NET Core Web API.
Conține:
controllerele;
serviciile aplicației;
background service-ul pentru detectarea unităților offline;
configurarea dependency injection;
configurarea SQL Server.
CommandTrack.Shared
Conține DTO-urile și request models folosite în comun de API, simulator și HQ:
UnitDto
CreateUnitRequest
UpdateUnitStatusRequest
UnitTelemetryDto
CreateUnitTelemetryRequest
CommandTrack.PlatformSimulator
Aplicație console care simulează o platformă operațională.
Trimite periodic:
heartbeat;
telemetrie;
baterie;
poziție;
viteză.
CommandTrack.HQ
Aplicație WPF desktop pentru monitorizare.
Momentan folosește code-behind în MainWindow.xaml.cs, nu MVVM.
CommandTrack.Api.Tests
Proiect de teste existent. Acoperirea cu teste este încă redusă și trebuie extinsă ulterior.
Fluxul principal este:
Platform Simulator
        ↓ HTTP
CommandTrack API
        ↓ EF Core
SQL Server
        ↑ HTTP
CommandTrack HQ
4. Modelul de date
OperationalUnit
Câmpuri importante:
Id
CallSign
Type
Status
CreatedAtUtc
LastSeenAtUtc
Reguli:
CallSign este obligatoriu;
CallSign este unic în baza de date;
lungimea maximă a CallSign este 50;
Type este obligatoriu;
lungimea maximă a Type este 100;
statusul este stocat în SQL Server ca string;
o unitate nouă pornește cu statusul Offline.
Statusuri disponibile:
Offline
Online
Busy
Maintenance
UnitTelemetry
Câmpuri:
Id
UnitId
BatteryPercent
Latitude
Longitude
SpeedKph
RecordedAtUtc
Validări:
baterie între 0 și 100;
latitudine între -90 și 90;
longitudine între -180 și 180;
viteza nu poate fi negativă;
UnitId trebuie să fie valid.
Relația este:
OperationalUnit 1 → N UnitTelemetry
Ștergerea unei unități șterge în cascadă telemetria asociată.
Există index pe:
UnitId + RecordedAtUtc
5. Funcționalități implementate în API
Endpoint-uri existente:
GET /api/units
GET /api/units/{id}
POST /api/units
PATCH /api/units/{id}/status
POST /api/units/{id}/heartbeat
POST /api/units/{id}/telemetry
GET /api/units/{id}/telemetry/latest
GET /api/units/{id}/telemetry/history?limit=50
Crearea unei unități
validează datele;
refuză call sign duplicat;
returnează 409 Conflict pentru duplicate;
returnează 201 Created la succes.
Actualizarea statusului
Permite:
Offline
Online
Busy
Maintenance
Heartbeat
Heartbeat-ul:
actualizează LastSeenAtUtc;
trece o unitate din Offline în Online;
nu trebuie să suprascrie Busy;
nu trebuie să suprascrie Maintenance.
Cerință importantă:
Offline + heartbeat → Online
Busy + heartbeat → Busy
Maintenance + heartbeat → Maintenance
Dacă operatorul setează manual Offline cât timp simulatorul continuă să trimită heartbeat-uri, următorul heartbeat va readuce unitatea la Online.
Monitorizarea automată Offline
Există un BackgroundService în API.
Comportament:
verificare la fiecare 5 secunde;
dacă o unitate nu a trimis heartbeat de peste aproximativ 15 secunde;
statusul este schimbat automat în Offline;
schimbarea este salvată în SQL Server;
este scris un mesaj în log.
Telemetrie
API-ul:
salvează fiecare măsurătoare ca rând nou;
păstrează istoricul;
poate returna ultima telemetrie;
poate returna ultimele N măsurători.
GetHistoryAsync:
verifică existența unității;
normalizează limita între 1 și 200;
ia cele mai recente înregistrări;
le returnează cronologic, de la cea mai veche la cea mai nouă.
6. Simulatorul
Proiect:
CommandTrack.PlatformSimulator
Configurația actuală folosește:
API: http://localhost:5076/
Unit ID: 89da676e-384e-4bb0-adec-1d4bfac22c8b
Unitatea simulată este ROBOT-02.
La fiecare 5 secunde:
trimite heartbeat;
generează telemetrie;
trimite telemetria la API;
afișează răspunsul în consolă.
Date simulate:
bateria pornește aproximativ de la 95%;
bateria scade lent;
latitudinea și longitudinea se modifică ușor;
viteza este generată aleator;
timestamp-ul folosește UTC.
Există handling de bază pentru:
API indisponibil;
timeout;
unitate inexistentă;
anulare cu Ctrl+C.
Limitări:
ID-ul unității este hard-coded;
URL-ul API este hard-coded;
simulează o singură unitate;
nu există fișier separat de configurare;
nu există retry/backoff avansat.
7. Aplicația HQ
Proiect WPF:
CommandTrack.HQ
URL-ul API este momentan hard-coded:
http://localhost:5076/
Dashboard
Afișează carduri pentru:
Total
Online
Offline
Busy
Maintenance
Tabelul afișează:
call sign;
tip;
status;
baterie;
viteză;
poziție;
last seen.
Statusurile folosesc culori diferite:
Online: verde;
Offline: roșu;
Busy: portocaliu;
Maintenance: mov.
Refresh
buton manual Refresh;
refresh automat la fiecare 5 secunde;
selecția unității trebuie păstrată după refresh;
valorile din tabel și panoul de detalii se actualizează automat.
Panoul de detalii
Pentru unitatea selectată afișează:
ID;
call sign;
tip;
status;
baterie;
viteză;
coordonate;
data creării;
ultima comunicare;
momentul ultimei telemetrii.
Modificarea manuală a statusului
Există:
un ComboBox închis la culoare;
statusurile Offline, Online, Busy, Maintenance;
butonul Update Status;
apel către endpoint-ul PATCH;
mesaj de succes sau eroare.
Grafice
HQ încarcă ultimele 30 de măsurători pentru unitatea selectată.
Grafice native WPF, fără bibliotecă externă:
baterie: linie verde;
viteză: linie albastră;
desenate cu Canvas, Polyline, Line și Ellipse;
afișează valoarea curentă;
afișează intervalul de timp.
Cerințe UI importante:
tema trebuie să rămână dark;
rândul selectat nu trebuie să devină alb;
selectorul de status trebuie să aibă fundal închis și text lizibil;
panoul de detalii poate folosi scroll vertical;
interfața trebuie să fie potrivită pentru demo.
8. SQL Server și Entity Framework
Se folosește:
SQL Server local;
Entity Framework Core;
migrații Code First.
Tabele principale:
OperationalUnits
UnitTelemetry
__EFMigrationsHistory
Connection string-ul este în configurația API-ului.
Migrațiile existente includ:
crearea inițială;
persistența unităților;
LastSeenAtUtc;
telemetria unităților.
Nu este necesară o migrare pentru schimbări care afectează doar controllere, servicii sau UI.
9. Decizii tehnice importante
ASP.NET Core Web API pentru backend.
SQL Server local.
EF Core Code First.
WPF pentru aplicația HQ.
HttpClient pentru comunicarea dintre componente.
DTO-uri comune în CommandTrack.Shared.
enum-ul UnitStatus este stocat ca string.
grafice native WPF, fără pachet extern.
refresh și heartbeat la interval de 5 secunde.
prag Offline de aproximativ 15 secunde.
implementare incrementală pe branch-uri feature.
testare manuală cu Postman înainte de integrarea în HQ.
momentan se preferă implementări simple, clare și demonstrabile în locul unei arhitecturi foarte complexe.
10. Probleme și limitări cunoscute
Configurația URL-urilor
Atât HQ, cât și simulatorul folosesc momentan:
http://localhost:5076/
hard-coded.
Trebuie verificat și stabilizat launchSettings.json, astfel încât API-ul să pornească mereu pe portul 5076, sau URL-urile trebuie mutate în configurație.
Aceasta era următoarea etapă planificată înainte de trecerea la Codex.
Startup order
API, simulator și HQ sunt configurate să pornească împreună.
Uneori simulatorul sau HQ pot porni înainte ca API-ul să fie pregătit. În acest caz:
simulatorul raportează temporar connection refused;
HQ poate necesita un refresh după câteva secunde.
Nu există încă un mecanism de health-check/retry complet.
Securitate
Nu există încă:
autentificare;
autorizare reală;
roluri;
HTTPS stabil pentru toate componentele;
secret management;
rate limiting.
UseAuthorization() există, dar nu există un sistem de autentificare implementat.
Performanță
HQ face:
un request pentru lista unităților;
câte un request pentru ultima telemetrie a fiecărei unități;
un request separat pentru istoricul unității selectate.
Pentru multe unități va apărea un model N+1 de request-uri. Este acceptabil pentru demo, dar trebuie optimizat ulterior.
Istoricul telemetriei
nu există retenție;
tabelul va crește continuu;
nu există arhivare;
nu există agregare;
nu există paginare completă.
Simulator
o singură unitate;
ID hard-coded;
fără configurare prin appsettings;
fără scenarii de avarie;
fără mai multe tipuri de platforme.
HQ
folosește code-behind, nu MVVM;
nu există hartă;
nu există export;
nu există notificări/alerte;
graficele nu au tooltip, zoom sau selecție temporală;
axa bateriei este fixă 0–100, deci variațiile mici pot părea aproape plate.
Teste
Testele automate trebuie extinse pentru:
duplicate call sign;
validarea telemetriei;
heartbeat;
tranzițiile de status;
monitorizarea Offline;
endpoint-ul history;
limitele query parameter-ului limit.
11. Cerințe care nu sunt evidente doar din cod
Proiectul este construit pentru o demonstrație vizuală și trebuie să fie ușor de pornit local.

Utilizatorul dorește ghidare incrementală:
câte un pas;
fișierul exact de modificat;
build după schimbări;
test manual;
commit și PR după închiderea unei etape.

Nu trebuie făcute refactorizări majore fără să fie păstrat comportamentul actual.

Busy și Maintenance sunt stări controlate de operator și nu trebuie anulate de heartbeat.

Offline este atât o stare manuală, cât și una automată. Dacă platforma încă trimite heartbeat, starea manuală Offline este temporară.

HQ trebuie să păstreze unitatea selectată între refresh-uri.

UI-ul trebuie să rămână dark, lizibil și potrivit pentru prezentare.

Toate timestamp-urile sunt salvate UTC și afișate local în HQ.

Simulatorul și HQ-ul trebuie să continue să folosească același URL de API.

La final se dorește documentație:
README GitHub;
documentație tehnică;
diagramă de arhitectură;
schema bazei de date;
lista endpoint-urilor;
ghid de instalare și rulare;
capturi din HQ, Postman și SQL Server;
limitări și dezvoltări viitoare.

12. Următorul pas recomandat
Următoarea etapă planificată este stabilizarea configurației de pornire a API-ului.
Workflow recomandat:
checkout master
git pull
build solution
create branch feature/api-launch-profile
Apoi:
verifică CommandTrack.Api/Properties/launchSettings.json;
confirmă dacă applicationUrl conține http://localhost:5076;
asigură un port stabil;
mută ulterior URL-ul API din HQ și simulator în configurație;
pornește API + simulator + HQ;
verifică heartbeat, dashboard și grafice;
commit, push și PR.
După stabilizarea porturilor, prioritățile recomandate sunt:
1. Configurație comună pentru URL-uri și simulator
2. Retry/health-check la pornire
3. Teste automate
4. Curățare UI și erori
5. Hartă sau vizualizare poziție
6. Export/raportare
7. Documentație finală
13. Instrucțiune pentru Codex
Înainte de orice modificare:
inspectează repository-ul real;
verifică branch-ul curent;
rulează build-ul;
nu presupune că structura fișierelor este identică cu acest rezumat;
păstrează comportamentele descrise mai sus;
fă modificările incremental;
explică fișierele schimbate și motivul;
rulează build/test după fiecare etapă importantă;
nu face commit direct pe master;
folosește branch-uri feature/... și PR-uri către master.