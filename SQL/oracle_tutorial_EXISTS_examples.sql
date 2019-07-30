/*Beispiel für das EXISTS-Statement*/

/*Alle Kunden mit einer Bestellung suchen*/
SELECT name FROM customers c
WHERE EXISTS ( SELECT 1 FROM orders WHERE customer_id = c.customer_id) -- WOZU SELECT 1?
ORDER BY name;

/*Namen von Warenhäusern aktualisieren*/
UPDATE warehouses w
SET warehouse_name = warehouse_name || ', USA'
WHERE EXISTS (SELECT 1 FROM locations 
              WHERE country_id = 'US'
              AND warehouse_name NOT LIKE '%, USA'
              AND location_id = w.location_id
              );
/*Verifiziere ob umbenennen der Warenhäuser geklappt hat*/
SELECT warehouse_name FROM warehouses
INNER JOIN locations USING(location_id)
WHERE country_id = 'US';




