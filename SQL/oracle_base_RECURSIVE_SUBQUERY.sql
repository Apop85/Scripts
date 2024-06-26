/*Oracle Base Rekursive Subquerys Beispiel*/
/*https://oracle-base.com/articles/11g/recursive-subquery-factoring-11gr2*/

CREATE TABLE tabelle1 (
  id        NUMBER,
  parent_id NUMBER,
  CONSTRAINT tabelle1_pk PRIMARY KEY (id),
  CONSTRAINT tabelle1_tabelle1_fk FOREIGN KEY (parent_id) REFERENCES tabelle1(id)
);

CREATE INDEX tabelle1_parent_id_idx ON tabelle1(parent_id);

INSERT INTO tabelle1 VALUES (1, NULL);
INSERT INTO tabelle1 VALUES (2, 1);
INSERT INTO tabelle1 VALUES (3, 2);
INSERT INTO tabelle1 VALUES (4, 2);
INSERT INTO tabelle1 VALUES (5, 4);
INSERT INTO tabelle1 VALUES (6, 4);
INSERT INTO tabelle1 VALUES (7, 1);
INSERT INTO tabelle1 VALUES (8, 7);
INSERT INTO tabelle1 VALUES (9, 1);
INSERT INTO tabelle1 VALUES (10, 9);
INSERT INTO tabelle1 VALUES (11, 10);
INSERT INTO tabelle1 VALUES (12, 9);
COMMIT;

WITH tab1(id, parent_id) AS ( 
     -- Anker
     SELECT id, parent_id FROM tabelle1
     WHERE parent_id IS NULL
     UNION ALL
     -- Rekursion
     SELECT tab2.id, tab2.parent_id
     FROM tabelle1 tab2, tab1
     WHERE tab2.parent_id = tab1.id
)
SELECT * FROM tab1;

DROP TABLE tabelle1 PURGE;
