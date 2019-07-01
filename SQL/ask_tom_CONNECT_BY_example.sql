/*asktom CONNECT BY Tutorial*/

/* 
Einschränkungen von CONNECT BY: 
- ORDER BY übersteuert die gefundene Hirarchie und kann somit nicht sinnvoll benutzt werden.
- Falls eine Sortierung gewünscht ist, die Daten mittels CREATE TABLE AS SELECT & ORDER BY 
  kopieren und darüber CONNECT BY laufen lassen. 
- Ein JOIN oder eine Subquery ist nicht erlaubt.
- Eine View, die ein JOIN beinhaltet, kann nicht verwendet werden.
- Die Master-Detail Beziehung muss demzufolge in der gleichen Tabelle abgelegt sein. 

***Einschränkungen sind ab der Oracle Version 9i obsolet***

Eigenschaften der START WITH - Klausel
Die START WITH Klausel bestimmt den Einstiegspunkt in die Hierarchie
Eigenschaften:
- Eine Bedingung (Konstante, Variable, Sub-Query) wird angegeben.
- Mehrere Einstiegspunkte sind erlaubt.
- Die START WITH Klausel ist optional
- Ohne START WITH Klausel, produziert SQL ab jedem gefundenen Datensatz einmal die Hierarchie.
- Die Hierarchien sind meist in der gleichen Tabelle wie die Daten abgelegt (rekursive Relation), 
  die Beziehung von Master zu Detail ist eine MAY BE- Beziehung (NULL-Werte sind erlaubt)
  Der Einstiegspunkt lautet darum häufig: START WITH attribut IS NULL


*/

/*08 - Hierarchische Abfragen.pdf*/
/*Diese Hierarchie startet beim Mitarbeiter Jones und findet alle seine Angestellten.*/
SELECT empno, ename, job, mgr
FROM emp
START WITH ename = 'JONES'      -- Einstiegspunkt für die Hierarchische Abfrage
CONNECT BY PRIOR empno = mgr;   

/*Mit der START WITH Klausel wird der Einstiegspunkt in die Hierarchie bestimmt. 
Eigenschaften: - Der Einstiegspunk in Form einer Condition kann der Abgleich auf eine Konstante, Variable
                 oder auch eine Sub-Query sein
               - Ab dem definierten Einstiegspunkt wird die Hierarchie produziert, 
                 mehrere Einstiegspunkte sind erlaubt
               - Die START WITH Klausel ist optional
               - Wenn keine START WITH Klausel definiert wird, produziert SQL ab jedem gefundenen Datensatz
                 einmal die Hierarchie (Wird in der Regel nicht verwendet, da die Ausgabe unübersichtlich wird)
               - Die Hierarchien sind meist in der gleichen Tabelle wie die Daten abgelegt (rekursive Relation),
                 die Beziehung von Master zu Detail ist eine MAY BE-Beziehung (NULL-Werte sind erlaubt).
               - Der Einstiegspunkt lautet darum häufig:
                 START WITH attribut IS NULL
                 Diese Condition trifft nur auf den hierarchisch höchsten Datensatz zu
               - Falls der Einstiegspunkt auf einen Datensatz in der untersten Hierarchiestufe zeigt, wird die 
                 Query nach dem ersten Datensatz (entsprechend dem START WITH) bereits beendet.
*/

SELECT empno, ename, job, mgr
FROM emp
START WITH ename = 'ADAMS'
CONNECT BY PRIOR empno = mgr;

/*
CONNECT BY Attribute:
- PRIOR - Bedeutet Vorgänger und definiert, wie die Abhängigkeit zum aktuellen Knoten zum Vorgänger aussieht.
- Das PRIOR-Attribut bestimmt die Suchrichtung
- PRIOR attribut1 = attribut2 bedeutet, dass der Vorgängerwert aus attribut1 nun im attribut2 stehen muss
- PRIOR empno = mgr sucht alle Angestellten, deren mgr der vorhergehenden empno entspricht.
***Auf welcher Seite der Condition PRIOR steht, hat keinen Einfluss, wichtig is, mit welchem Attribut 
   PRIOR verbunden wird.***
*/

-- Suchrichtung von unten nach oben
SELECT empno, ename, job, mgr
FROM emp
START WITH ename = 'JONES'
CONNECT BY PRIOR empno = mgr;

-- Suchrichtung von oben nach unten
SELECT empno, ename, job, mgr
FROM emp
START WITH ename = 'ADAMS'
CONNECT BY PRIOR mgr = empno;

/*Es können auch mehrere CONNECT BY Conditions definiert werden*/
SELECT empno, ename, job, sal, mgr
FROM emp
START WITH mgr IS NULL
CONNECT BY PRIOR empno = mgr
        AND PRIOR job = 'PRESIDENT';


/*Conditions ohne PRIOR beziehen sich auf Werte der aktuellen Reihe*/
SELECT empno, ename, job, sal, mgr
FROM emp
START WITH mgr IS NULL
CONNECT BY PRIOR empno = mgr
        AND PRIOR job = 'PRESIDENT'
        AND sal > 2500;

/*Alle Unergebenen vom President*/
SELECT empno, ename, job, mgr
FROM emp
START WITH mgr IS NULL
CONNECT BY PRIOR empno = mgr;


/*Alle Untergebenen deren Beruf nicht Manager ist.*/
SELECT empno, ename, job, mgr
FROM emp
WHERE job <> 'MANAGER'
START WITH mgr IS NULL
CONNECT BY PRIOR empno = mgr;


/*Pseudo Spalten mittels PRIOR*/
/*
- Die Werte des vorhergehenden Durchlaufs können mit PRIOR angesprochen werden. Die Pseudospalte PRIOR kann
  in der CONNECT BY-Klausel verwendet werden, um die Werte des Vorgängers anzusprechen.
- PRIOR kann auch in der SELECT-Liste eingesetzt werden, um auf jedem Knoten die Werte des Vorgängers anzuzeigen.
- In der nachfolgenden Abfrage werden ab dem Angestellten KING alle Mitarbeiter aus dem Departement 30
  angezeigt, sowie über PRIOR die Werte des jeweiligen Vorgängers.
  
- Mit LEVEL kann die Hierarchiestufe angegeben werden. 
- In einer hierarchischen Abfrage kann zusätzlich zur Pseudo-Reihe PRIOR auch die Pseudo-Reihe LEVEL verwendet werden
- Der Einstiegspnkt in die Hierarchie trägt immer LEVEL 1, unabhängig davon, ob die Hierarchie von 
  oben nach unten oder umgekehrt durchlaufen wird.
*/
SELECT empno, ename, job, mgr
       ,PRIOR ename "Chef"     -- Daten des vorhergehenden Durchlaufs
       ,PRIOR job "Beruf Chef"
FROM emp
START WITH mgr IS NULL
CONNECT BY PRIOR empno = mgr
        AND deptno = 30;


/*Abfrage mit LEVEL Ausgabe*/
SELECT LEVEL, empno, ename, job, mgr
FROM emp
START WITH mgr IS NULL
CONNECT BY PRIOR empno = mgr;

/*Auch die Pseudo-Reihe LEVEL kann in der CONNECT BY-Klausel verwendet werden, um Einschränkungen zu definieren*/        
/*Ausgabe der ersten zwei Hierarchiestufen:*/
SELECT LEVEL, empno, ename, job, mgr
FROM emp
START WITH mgr IS NULL
CONNECT BY PRIOR empno = mgr
        AND LEVEL <= 2;          -- Stoppt bei Rekursionstiefe von 2
        
/*Nur Angestellte der 2. und 4. Hierarchiestufe*/
SELECT LEVEL, empno, ename, job, mgr
FROM emp
WHERE LEVEL IN (2,4)
START WITH mgr IS NULL
CONNECT BY PRIOR empno = mgr;

/*
Die Ausgabe einer hierachischen Abfrage wird eigentlich erst brauchbar, wenn diese optisch 
erkennbar die Hierarchie aufweist.
Zur Darstellung der Hierarchie wird mit der Pseudo-Reihe LEVEL innerhalb der SQL Funktion LPAD gearbeitet.
*/

-- BEISPIEL --
SELECT LPAD ('>', 2*(LEVEL-1)) || ename "Name"
       , empno, job, LEVEL
FROM emp
START WITH mgr IS NULL
CONNECT BY PRIOR empno = mgr;

/*
Bei einer hierarchischen Abfrage werden Datensätze auf der gleichen Ebene in der Reihenfolge 
angezeigt, in der sie aus der Datenbank gelesen wurden. Die Sortierung der Einträge auf der gleichen Ebene
kann nicht über eine herkömmliche ORDER BY-Klausel gelöst werden, da keine Rücksicht auf die zuvor aufgebaute
Hierarchie nimmt und sie dadurch zerstört.
*/

SELECT LPAD ('>', 2*(LEVEL-1)) || ename "Name"
       , empno, job, LEVEL
FROM emp
START WITH mgr IS NULL
CONNECT BY PRIOR empno = mgr
ORDER BY ename;                -- Bei hierarchischen Abfragen unbrauchbar

/*ORDER SIBLINGS --> Sortierung innerhalb der Hierarchie*/
/*
Mit Oracle 9i wurde die ORDER SIBLINGS BY-Klausel eingeführt, welech die Sortierung eines Resultats unter
Berücksichtigung der vorhandenen Hierarchie zulässt. Das Keyword SIBLINGS ist nur im Zusammenhang mit
hierarchischen Abfragen zulässig.
ACHTUNG: ORDER SIBLINGS BY LEVEL, PRIOR oder ROWNUM ist nicht gestattet (Fehlermeldung ab Oracle 10g), 
daher darf hier nicht nach der LPAD-Funktion sortiert werden.
*/

SELECT LPAD ('>', 2*(LEVEL-1)) || ename "Name"
       , empno, job, LEVEL
FROM emp
START WITH mgr IS NULL
CONNECT BY PRIOR empno = mgr
ORDER SIBLINGS BY ename;

/*SYS_CONNECT_BY_PATH*/
/*
- Mit SYS_CONNECT_BY_PATH kann der vollständige Pfad vom Root-Element her zurückgegeben werden.
- Das Trennzeichen, welches zwischen den einzelnen Elementen angezeigt werden soll, ist frei wählbar und
  wird als zweiter Parameter mitgegeben.
*/

SELECT SYS_CONNECT_BY_PATH(ename, '/') "Name", empno, job, LEVEL
FROM emp
START WITH mgr IS NULL
CONNECT BY PRIOR empno = mgr
ORDER SIBLINGS BY ename;

/*CONNECT_BY_ISLEAF*/
/*
Mit CONNECT_BY_ISLEAF können Hierarchie-Endprodukte identifiziert werden
- Über die Funktion CONNECT_BY_ISLEAF kann für die einzelnen Datensätze 
  festgestellt werden, ob es sich bei diesen im aktuellen Query-Resultat 
  um einen Hierarchie-Endpunkt handelt, oder ob weitere Hierarchiestufen 
  unterhalb des Datensatzes existieren.
*/

SELECT ename, empno, job, LEVEL, CONNECT_BY_ISLEAF leaf -- CONNECT_BY_ISLEAF gibt eine 1 aus, 
FROM emp                                                -- wenn es sich um den Letzten Ast handelt
START WITH mgr IS NULL                                  -- ansonsten wird 0 ausgegeben.
CONNECT BY PRIOR empno = mgr
ORDER SIBLINGS BY ename;

/*Über die Funktion CONNECT_BY_ROOT kann der jeweilige Einstiegspunkt (ROOT) 
jedes Elements in der Hierarchie eruiert werden. Es ist eigentlich das Gegenteil von 
CONNECT_BY_ISLEAF*/

/*LOOP IN DEN DATEN*/
/*Vor der Version 10g führte ein Loop in den Daten, welche über die hierarchischen Query ausgewertet wurden
immer zu einem Fehler. Mittels CONNECT BY NOCYCLE PRIOR kann dieser Fehler unterbunden werden.*/

UPDATE emp SET mgr = 7934 WHERE ename = 'KING'; -- Führe Daten zu einem Loop zusammen als Anschauungsbeispiel

-- VERSION MIT FEHLERMELDUNG:
SELECT ename, empno, job, LEVEL
FROM emp
START WITH ename = 'KING'
CONNECT BY PRIOR empno = mgr
ORDER SIBLINGS BY ename;

-- VERSION OHNE FEHLERMELDUNG:
SELECT ename, empno, job, LEVEL
FROM emp
START WITH ename = 'KING'
CONNECT BY NOCYCLE PRIOR empno = mgr
ORDER SIBLINGS BY ename;

-- Loop ausfindig machen:
/*Um herauszufinden wo im Datensatz der Loop entsteht kann die Funktion CONNECT_BY_ISCYCLE verwendet werden*/
SELECT ename, empno, job, LEVEL, CONNECT_BY_ISCYCLE "Loop"
FROM emp
START WITH ename = 'KING'
CONNECT BY NOCYCLE PRIOR empno = mgr
ORDER SIBLINGS BY ename;

UPDATE emp SET mgr = 7839 WHERE ename = 'KING'; -- Loop rückgängig machen


/*--------------------------------------------------------------------------------------------------*/
/*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ANDERE BEISPIELE~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/
/*--------------------------------------------------------------------------------------------------*/

/*https://asktom.oracle.com/pls/asktom/f?p=100:11:0::::P11_QUESTION_ID:489772591421*/

select ename, empno, mgr from emp 
START WITH mgr IS NULL
CONNECT BY PRIOR empno = mgr;

/*https://oracle-base.com/articles/misc/hierarchical-queries*/
DROP TABLE tab1 PURGE;

CREATE TABLE tab1 (
  id        NUMBER,
  parent_id NUMBER,
  CONSTRAINT tab1_pk PRIMARY KEY (id),
  CONSTRAINT tab1_tab1_fk FOREIGN KEY (parent_id) REFERENCES tab1(id)
);

CREATE INDEX tab1_parent_id_idx ON tab1(parent_id);

INSERT INTO tab1 VALUES (1, NULL);
INSERT INTO tab1 VALUES (2, 1);
INSERT INTO tab1 VALUES (3, 2);
INSERT INTO tab1 VALUES (4, 2);
INSERT INTO tab1 VALUES (5, 4);
INSERT INTO tab1 VALUES (6, 4);
INSERT INTO tab1 VALUES (7, 1);
INSERT INTO tab1 VALUES (8, 7);
INSERT INTO tab1 VALUES (9, 1);
INSERT INTO tab1 VALUES (10, 9);
INSERT INTO tab1 VALUES (11, 10);
INSERT INTO tab1 VALUES (12, 9);
COMMIT;

SELECT id,
       parent_id,
       RPAD('.', (level-1)*2, '.') || id AS tree,
       level,
       CONNECT_BY_ROOT id AS root_id,
       LTRIM(SYS_CONNECT_BY_PATH(id, '-'), '-') AS path,
       CONNECT_BY_ISLEAF AS leaf
FROM   tab1
START WITH parent_id IS NULL
CONNECT BY parent_id = PRIOR id
ORDER SIBLINGS BY id;

SELECT id, 
       parent_id,
       RPAD('.', (level-1)*2, '.') || id AS tree,
       level, 
       CONNECT_BY_ROOT id AS root_id,
       LTRIM(SYS_CONNECT_BY_PATH(id, '-'), '-') AS path,
       CONNECT_BY_ISLEAF AS leaf,
       CONNECT_BY_ISCYCLE AS cycle
FROM tab1
START WITH id = 1
CONNECT BY NOCYCLE parent_id = PRIOR id
ORDER SIBLINGS BY id;
