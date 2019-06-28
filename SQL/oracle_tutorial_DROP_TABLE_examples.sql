/*DROP TABLE Beispiele*/
/*https://www.oracletutorial.com/oracle-basics/oracle-drop-table/*/

CREATE TABLE test_table (
       somewhat1 NUMBER NOT NULL,
       somewhat2 VARCHAR2(255) NOT NULL,
       somewhat3 VARCHAR2(255) NOT NULL,
       PRIMARY KEY (somewhat1));
       
INSERT INTO test_table (somewhat1, somewhat2, somewhat3) VALUES (1, 'Blah', 'Blubb');

DROP TABLE test_table;

SELECT * FROM all_cons_columns;

/*DROP CASCADE Beispiel*/
CREATE TABLE brands(brand_id NUMBER PRIMARY KEY,
                    brand_name varchar2(50)
);
 
CREATE TABLE cars(car_id NUMBER PRIMARY KEY,
                  make VARCHAR(50) NOT NULL,
                  model VARCHAR(50) NOT NULL,
                  year NUMBER NOT NULL,
                  plate_number VARCHAR(25),
                  brand_id NUMBER NOT NULL,
 
                  CONSTRAINT fk_brand
                  FOREIGN KEY (brand_id)
                  REFERENCES brands(brand_id) ON DELETE CASCADE
);

/*Da die beiden Tabellen brands und cars verknüpft sind lässt sich brands nicht mit DROP TABLE brands löschen*/
-- DROP TABLE brands;

/*Alle Verknüpfungen anzeigen*/
SELECT a.table_name, a.column_name, a.constraint_name, 
       c.owner, c.r_owner,
       c_pk.table_name r_table_name, c_pk.constraint_name r_pk
FROM all_cons_columns a
JOIN all_constraints c ON a.owner = c.owner
                       AND a.constraint_name = c.constraint_name
JOIN all_constraints c_pk ON c.r_owner = c_pk.owner
                          AND c.r_constraint_name = c_pk.constraint_name
WHERE c.constraint_type = 'R'
      AND a.table_name = 'CARS'
      
DROP TABLE cars PURGE;
