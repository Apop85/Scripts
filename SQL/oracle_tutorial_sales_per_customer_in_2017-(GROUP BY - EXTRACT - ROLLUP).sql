/*Umsatz pro Kunde 2017*/
SELECT customer_id, 
       SUM(quantity*unit_price) Summe
FROM orders
INNER JOIN order_items USING (order_id)
WHERE status = 'Shipped' 
      AND salesman_id IS NOT NULL 
      AND EXTRACT(YEAR FROM order_date) = 2017
GROUP BY customer_id
ORDER BY summe DESC

/*Gesammtsumme 2017*/
SELECT SUM(summe) Summe_Total FROM (
       SELECT customer_id, 
              SUM(quantity*unit_price) Summe
       FROM orders
       INNER JOIN order_items USING (order_id)
       WHERE status = 'Shipped' 
             AND salesman_id IS NOT NULL 
             AND EXTRACT(YEAR FROM order_date) = 2017
       GROUP BY ROLLUP(customer_id)
       )

/* Alternativ: Gesammtsumme 2017*/
SELECT customer_id, SUM(quantity*unit_price) Summe_Total FROM orders
INNER JOIN order_items USING (ORDER_ID)
WHERE EXTRACT(YEAR FROM order_date) = 2017 AND
      salesman_id IS NOT NULL AND
      status = 'Shipped'
GROUP BY ROLLUP(customer_id)
