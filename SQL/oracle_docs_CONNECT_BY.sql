/*Tutorial CONNECT BY*/
/*https://docs.oracle.com/cd/B19306_01/server.102/b14200/queries003.htm*/

SELECT employee_id, last_name, manager_id
FROM employees
CONNECT BY PRIOR employee_id = manager_id; -- Beschreibt wie SQL die Hierarchie durchlaufen soll. 

/*Mit Rekursionstiefenangabe*/
SELECT employee_id, last_name, manager_id, LEVEL
FROM employees
CONNECT BY PRIOR employee_id = manager_id
ORDER BY LEVEL;

/*Inklusive START WITH anweisung*/
SELECT employee_id, last_name, manager_id, LEVEL
FROM employees
START WITH employee_id = 1  -- Definiert LEVEL=1 nodes
CONNECT BY PRIOR employee_id = manager_id
ORDER SIBLINGS BY last_name;

select * from employees
