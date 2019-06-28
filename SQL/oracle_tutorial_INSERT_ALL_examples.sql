/*INSERT ALL Statement*/
/*https://www.oracletutorial.com/oracle-basics/oracle-insert-all/*/

DROP TABLE fruits;

CREATE TABLE fruits(
             fruit_name VARCHAR(100) PRIMARY KEY,
             color VARCHAR(100) NOT NULL
             );
             
INSERT ALL INTO fruits(fruit_name, color)
           VALUES ('Apple', 'Red')
           
           INTO fruits(fruit_name, color)
           VALUES ('Orange', 'Orange')
           
           INTO fruits(fruit_name, color)
           VALUES ('Banana', 'Yellow')
SELECT 1 FROM dual;

SELECT * FROM fruits;

/*In mehrere Tabellen einfügen*/
CREATE TABLE small_orders(
             order_id NUMBER(12) NOT NULL,
             customer_id NUMBER(6) NOT NULL,
             amount NUMBER(8,2)
);

CREATE TABLE medium_orders AS
SELECT * FROM small_orders;

CREATE TABLE big_orders AS
SELECT *FROM small_orders;

INSERT ALL WHEN amount < 1000000 THEN
           INTO 
