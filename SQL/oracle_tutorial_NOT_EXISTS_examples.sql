/*Find Customers without orders*/
/*https://www.oracletutorial.com/oracle-basics/oracle-not-exists/*/

SELECT name FROM customers
WHERE NOT EXISTS ( SELECT NULL FROM orders 
                   WHERE orders.customer_id = customers.customer_id )
ORDER BY name;

/*Export customers without orders into archive*/
DROP TABLE customers_archive PURGE;

CREATE TABLE customers_archive AS
       SELECT * FROM customers
       WHERE NOT EXISTS ( SELECT NULL
                          FROM orders
                          WHERE orders.customer_id = customers.customer_id );

/*Set credit limit to 0 if there was no order*/
UPDATE customers
SET credit_limit = 0
WHERE NOT EXISTS ( SELECT NULL
                   FROM orders
                   WHERE orders.customer_id = customers.customer_id
                   AND EXTRACT (YEAR FROM order_date) = 2017 );

COMMIT;

/*Delete customers without orders in 2016/2017*/
DELETE FROM customers
WHERE NOT EXISTS ( SELECT NULL
                   FROM orders
                   WHERE orders.customer_id = customers.customer_id
                   AND EXTRACT (YEAR FROM order_date) IN (2016,2017)
                   );
