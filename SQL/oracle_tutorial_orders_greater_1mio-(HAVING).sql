/*Bestellungen mit dem Gesamtwert >1Mio auslesen*/
SELECT order_id, SUM(unit_price*quantity) order_value
FROM order_items
GROUP BY order_id HAVING SUM(unit_price*quantity) > 1000000
ORDER BY order_value DESC;
