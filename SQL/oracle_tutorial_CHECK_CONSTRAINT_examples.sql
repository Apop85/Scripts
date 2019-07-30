/*CHECK CONSTRAINT Beispiele*/
/*https://www.oracletutorial.com/oracle-basics/oracle-check-constraint/*/

/*BEISPIELAUFBAU*/
-- CREATE TABLE table_name (
--    ...
--    CONSTRAINT choosen_name,
--    column_name data_type CHECK (expression),
--    ...
-- );

CREATE TABLE parts_list (
    part_id NUMBER GENERATED BY DEFAULT AS IDENTITY,
    part_name VARCHAR2(255) NOT NULL,
    buy_price NUMBER(9,2) CHECK(buy_price > 0), -- Preis muss gr�sser 0 sein.
    PRIMARY KEY(part_id)
);

ALTER TABLE parts_list
ADD cost NUMBER(9,2);

/*Hinzuf�gen neuer Constraint Checks*/
ALTER TABLE parts_list
ADD CONSTRAINT check_positive_cost CHECK(cost > 0); -- Preis muss gr�sser 0 sein.

ALTER TABLE parts_list
ADD CONSTRAINT check_valid_cost CHECK(cost > buy_price); -- Preis muss gr�sser dem Einkaufspreis sein

/*Provozierter Fehler*/
INSERT INTO parts_list(part_id, part_name, buy_price, cost)
VALUES(1,'Zahnrad',5,4);

INSERT INTO parts_list(part_id, part_name, buy_price, cost)
VALUES(1,'Zahnrad',-5,4);

/*Constraint deaktivieren*/
ALTER TABLE parts_list
DISABLE CONSTRAINT check_positive_cost;

/*Constraint reaktivieren*/
ALTER TABLE parts_list
ENABLE CONSTRAINT check_positive_cost;


/*Folgende Einschr�nkungen gibt es bei CHECK CONSTRAINTS:
1. Man kann Checks nur auf Tabellen, nicht auf Ansichten/Auswertungen anwenden
2. Die Pr�fung mittels CHECK kann sich auf alle Spalten einer Tabelle beziehen, 
   jedoch niemals auf eine externe Tabelle verweisen
3. Der Ausdruck kann folgende Konstrukte nicht verarbeiten:
   - Nicht deterministische Funktionen wie SYSDATE, CURRENT_DATE und CURRENT_TIMESTAMP
   - Subqueries oder skalierbare Subqueries-Ausdr�cke
   - Aufruf benutzerdefinierter Funktionen
   - Verschachtelte Spalten oder Attribute
   - Datums-Konstanten die nicht vollst�ndig definiert sind.*/

DROP TABLE parts_list PURGE;
