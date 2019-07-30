/*UNION - Beispiele*/
-- Beispiel 1
SELECT first_name || ' ' || last_name Name, email, 'contact'
FROM contacts
UNION SELECT first_name || ' ' || last_name Name, email, 'employee'
FROM employees
ORDER BY Name;

-- Beispiel 2 (Ohne Doppeleinträge)
SELECT last_name FROM employees
UNION SELECT last_name
FROM contacts
ORDER BY last_name

-- Beispiel 3 (Mit Doppeleinträgen)
SELECT last_name FROM employees
UNION ALL SELECT last_name
FROM contacts
ORDER BY last_name
