/*Ausgabe von Status und Anzahl Auftr�ge pro Kunde*/
SELECT customer_id, status, count(*) FROM orders
GROUP BY ROLLUP(customer_id, status)
ORDER BY customer_id ASC, status DESC;

/* Musterl�sung:
SELECT customer_id, status, SUM(quantity*unit_price) sales
FROM orders
INNER JOIN order_items USING(order_id)
GROUP BY ROLLUP(customer_id, status);
*/
