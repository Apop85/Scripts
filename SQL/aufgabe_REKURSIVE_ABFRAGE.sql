/*AUFGABE ZU "how_to_REKURSIVE_ABFRAGE.sql"*/
/*http://www.mayeruli.de/db2/rekursives-sql.php*/
/*
Aufgabe:
Verwendete Tabelle: orga (falls nicht vorhanden, erst how_to_REKURSIVE_ABFRAGE.sql ausführen)
Aufgabestellung: Welche Vorgesetzten hat Roedldoedl 1121? Roedldoedl 1121 muss dabei selbst in der Liste auftauchen.
*/

/*Aufgabe 1: 
1. Welche Spalten sollen in der Ergebnismenge Auftauchen und/oder werden für die Rekursionsbedingung benötigt?
Antwort: persnr, name

2. Wie lautet das SELECT Statement für den Satz, von dem die Rekursion ausgehen soll?
Antwort: SELECT persnr, name
         FROM orga
         WHERE persnr = 1121;

3. Wie lautet die Rekursionsbedingung?
Antwort (Verbal): Wenn orga(A).persnr = orga(B).chef_ist
*/

/*Wie lautet das vollständige WITH-Statement?*/
WITH result_table (persnr, name, chef_ist) AS (
     SELECT persnr, name, chef_ist FROM orga
     WHERE persnr = 1121
     UNION ALL
     SELECT A.persnr, A.name, A.chef_ist FROM orga A
     INNER JOIN result_table B ON A.persnr = B.chef_ist
)
SELECT * FROM result_table;

/*Aufgabe 2: 
Und wenn die Personalnummer von Roedldoedl nicht bekannt ist (und erst ermittelt werden muss 
und er selbst NICHT in der Liste aufteuchen soll?
*/

WITH result_table_2 (persnr, name, chef_ist) AS (
     SELECT persnr, name, chef_ist FROM orga
     WHERE name = 'Roedldoedl 1121'
     UNION ALL
     SELECT A.persnr, A.name, A.chef_ist FROM orga A
     INNER JOIN result_table_2 B ON A.persnr = B.chef_ist
)
SELECT * FROM result_table_2
WHERE name != 'Roedldoedl 1121';
