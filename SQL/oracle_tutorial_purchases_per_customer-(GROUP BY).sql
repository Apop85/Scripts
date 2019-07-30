-- Anzahl Bestellungen pro Kunde
SELECT cus.customer_id Kunden_ID, cus.name Kundenname, count(*) Bestellungen
FROM customers cus, orders ord
WHERE cus.customer_id = ord.customer_id
GROUP BY cus.customer_id, cus.name
ORDER BY count(*) DESC
