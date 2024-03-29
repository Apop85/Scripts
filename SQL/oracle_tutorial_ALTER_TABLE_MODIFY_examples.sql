/*Beispiele f�r ALTER TABLE MODIFY*/
/*https://www.oracletutorial.com/oracle-basics/oracle-alter-table-modify-column/*/

DROP TABLE accounts PURGE;

CREATE TABLE accounts(
    account_id NUMBER GENERATED BY DEFAULT AS IDENTITY,
    first_name VARCHAR2(25) NOT NULL,
    last_name VARCHAR2(25) NOT NULL,
    email VARCHAR2(100),
    phone VARCHAR2(12),
    full_name VARCHAR2(51) GENERATED ALWAYS AS(
              first_name || ' ' || last_name),
    PRIMARY KEY(account_id)
);

INSERT INTO accounts(first_name, last_name, phone) VALUES ('Trinity', 'Knox', '410-555-0197');
INSERT INTO accounts(first_name, last_name, phone) VALUES ('Mellissa', 'Porter', '410-555-0198');
INSERT INTO accounts(first_name, last_name, phone) VALUES ('Leeanna', 'Bowman', '410-555-0199');

/*Bestimmte Spalte ausblenden (Kann nur noch mit SELECT column_name FROM table_name aufgerufen werden)*/
ALTER TABLE accounts
MODIFY full_name INVISIBLE;

/*Entferne NULL - Eintr�ge aus email Spalte*/
UPDATE accounts
SET email = 'k/A'
WHERE email IS NULL;

/*M�glichket der NULL-Values deaktivieren, wenn die Tabelle Nullwerte aufweist wird dies einen Error erzeugen*/
ALTER TABLE accounts
MODIFY email VARCHAR2(100) NOT NULL;

SELECT * FROM accounts;

