/*INTERSECT - Beispiele*/
-- Beispiel 1
SELECT
    last_name
FROM
    contacts
INTERSECT 
SELECT
    last_name
FROM
    employees
ORDER BY
    last_name;
    
    
-- Beispiel 2
select order_id from orders
INTERSECT
select order_id from order_items;
