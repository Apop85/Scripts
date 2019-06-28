/*MINUS - Beispiele*/
--Beispiel 1:
SELECT
    last_name
FROM
    contacts
MINUS
SELECT
    last_name
FROM
    employees
ORDER BY
    last_name;


--Beispiel 2:
SELECT
  product_id
FROM
  products
MINUS
SELECT
  product_id
FROM
  inventories;
