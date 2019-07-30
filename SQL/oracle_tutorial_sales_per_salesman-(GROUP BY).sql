--Anzahl Verkäufe pro Verkäufer
SELECT ord.salesman_id, empl.first_name || ' ' || empl.last_name Angestellter, count(*) Verkäufe
FROM orders ord, employees empl
WHERE ord.salesman_id = empl.employee_id
GROUP BY salesman_id, empl.first_name || ' ' || empl.last_name
