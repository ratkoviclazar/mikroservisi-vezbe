# Opis projekta
Platforma za upravljanje događajima
===============================================

Opis sistema
---------------

Ova aplikacija predstavlja platformu namenjenu organizaciji i upravljanju stručnim događajima u okviru fakulteta. Sistem omogućava organizatorima da kreiraju, uređuju i pregledaju događaje, kao i da upravljaju podacima o lokacijama, predavačima i prijavama učesnika.

Podržani tipovi događaja uključuju:

*   konferencije
    
*   seminare
    
*   radionice
    
*   predavanja
    

Cilj sistema je centralizacija svih informacija i pojednostavljenje procesa organizacije događaja i evidencije učesnika.

Tehnološki stek
------------------

### Backend

*   **C#**
    
*   **ASP.NET Core Web API**
    
    *   Razvoj nezavisnih mikroservisa
        
*   **Entity Framework Core**
    
    *   ORM za rad sa bazom podataka
        
*   **SQL Server**
    
    *   Relaciona baze (svaki mikroservis može imati svoju bazu)

### Frontend

*   **React**
    
    *   Komponentni pristup izradi korisničkog interfejsa
        
*   **HTML / CSS / TypeScript / Tailwind**
    
    *   Osnovna struktura i stilizacija
        
Arhitektura sistema
-------------------

Sistem je zasnovan na **mikroservisnoj arhitekturi**, gde je aplikacija podeljena na više nezavisnih servisa. Svaki servis je odgovoran za jednu poslovnu celinu i može se razvijati, testirati i skalirati nezavisno.

Glavni mikroservisi
-------------------

### 1\. Dogadjaj Service

Zadužen za upravljanje događajima:

*   kreiranje i izmena događaja
    
*   definisanje agende, trajanja i cene
    
*   povezivanje sa lokacijom i predavačima
    

### 2\. Lokacija Service

Upravlja podacima o lokacijama:

*   naziv i adresa
    
*   kapacitet
    
*   dostupnost lokacije
    

### 3\. Predavac Service

Zadužen za predavače:

*   ime, prezime, titula
    
*   oblasti stručnosti
    
*   povezivanje sa događajima
    

### 4\. Registracija Service

Upravlja prijavama učesnika:

*   evidencija prijava
    
*   povezivanje učesnika sa događajem
    
*   kontrola kapaciteta događaja
