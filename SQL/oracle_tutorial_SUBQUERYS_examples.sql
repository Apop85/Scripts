/*Beispiele für Subquerys*/

/*Ohne Subquerys*/
SELECT MAX(list_price) FROM products;
SELECT * from products WHERE list_price = 8867.99;

/*Mit Subquerys*/
/*Auslesen des teuersten Produkts (Subquery innerhalb der WHERE-Klausel)*/
SELECT product_id, product_name, list_price
FROM products
WHERE list_price = ( 
      /*Da sich diese Unteranfrage innerhalb 
      des WHERE-Blocks befindet nennt man diese 
      "Nested Subquery"*/
      SELECT MAX(list_price) FROM products
      );

/*Gerundeter Durchschnittspreis (Subquery innerhalb der SELECT-Klausel)*/      
SELECT product_name, list_price,
       ROUND((SELECT AVG(list_price) FROM products p1
       WHERE p1.category_id = p2.category_id), 2) avg_list_price
FROM products p2
ORDER BY product_name;

/*Top-10 Bestellungen mit den höchsten Werten*/
SELECT order_id, order_value
FROM ( SELECT order_id, SUM(quantity*unit_price) order_value
       FROM order_items
       GROUP BY order_id
       ORDER BY order_value DESC
       )
WHERE rownum < 11;

/*Produkte mit höherem Preis als der Durchschnitt*/
SELECT product_id, product_name, list_price FROM products
WHERE list_price > 
      (SELECT AVG(list_price) FROM products)
ORDER BY product_name;

/*Verkäufer mit einem Umsatz von >= 100k im Jahr 2017*/
SELECT employee_id, first_name, last_name FROM employees
WHERE employee_id IN (SELECT salesman_id
                     FROM orders
                     INNER JOIN order_items USING (order_id)
                     WHERE status = 'Shipped'
                     GROUP BY salesman_id,
                           EXTRACT(YEAR FROM order_date)
                     HAVING SUM(quantity*unit_price) >= 100000
                            AND EXTRACT(YEAR FROM order_date) = 2017
                            AND salesman_id IS NOT NULL)
ORDER BY first_name, last_name;
                     
/*Kunden ohne Bestellung im Jahr 2017*/
SELECT name FROM customers
WHERE customer_id NOT IN (
                         SELECT customer_id FROM orders
                         WHERE EXTRACT(YEAR FROM order_date) = 2017)
ORDER BY name;

/*Günstigstes Produkt*/
SELECT product_id, product_name, list_price FROM products
WHERE list_price = (SELECT MIN(list_price) FROM products);

/*Produkte finden die in ihrer Kategorie über dem Durchschnittspreis liegen*/
SELECT product_id, product_name, list_price FROM products p1
WHERE list_price > (SELECT AVG(list_price) 
                   FROM products 
                   WHERE category_id = p1.category_id);
                   
/*Angabe des Durschnittspreises pro Kategorie*/
SELECT category_id, ROUND((
                          SELECT AVG(standard_cost) FROM products
                          WHERE category_id = p1.category_id),2) avg_standard_cost
FROM products p1
GROUP BY category_id;

/*Alle Kunden die keine Bestellung eingetragen haben*/              
SELECT customer_id, name FROM customers
WHERE NOT EXISTS( SELECT * FROM orders 
                  WHERE orders.customer_id = customers.customer_id)
ORDER BY name;
