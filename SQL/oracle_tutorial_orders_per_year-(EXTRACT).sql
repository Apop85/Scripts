--Bestellungen pro Jahr
SELECT EXTRACT(YEAR FROM order_date) Yahr, count(*) Bestellungen
FROM orders
GROUP BY EXTRACT(YEAR FROM order_date)
ORDER BY Yahr
