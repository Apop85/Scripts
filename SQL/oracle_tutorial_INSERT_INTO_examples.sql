/*Verwendung des Insert-Into Statement*/
/*https://www.oracletutorial.com/oracle-basics/oracle-insert-into-select/*/

DROP TABLE sales PURGE;

CREATE TABLE sales (
       customer_id NUMBER,
       product_id NUMBER,
       order_date DATE NOT NULL,
       total NUMBER(9,2) DEFAULT 0 NOT NULL,
       PRIMARY KEY (customer_id,
                    product_id,
                    order_date)
);

INSERT INTO sales(customer_id, product_id, order_date, total)
SELECT customer_id,
       product_id,
       order_date,
       SUM(quantity*unit_price) amount
FROM orders
INNER JOIN order_items USING(order_id)
      WHERE status = 'Shipped'
GROUP BY customer_id, 
         product_id,
         order_date;
         
SELECT * FROM sales
ORDER BY order_date DESC,
         total DESC;      
         
