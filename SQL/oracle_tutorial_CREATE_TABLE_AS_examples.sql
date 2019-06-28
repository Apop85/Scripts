/*Create table AS*/
/*https://www.oracletutorial.com/oracle-basics/oracle-insert-into-select/*/

DROP TABLE sales_2017 PURGE;

CREATE TABLE sales_2017 AS
       SELECT * FROM sales
       WHERE 1 = 0;
       
INSERT INTO sales_2017
       SELECT customer_id,
              product_id,
              order_date,
              SUM(quantity*unit_price) amount
       FROM orders
       INNER JOIN order_items USING (order_id)
       WHERE status = 'Shipped' AND EXTRACT(YEAR FROM order_date) = 2017
       GROUP BY customer_id,
                product_id,
                order_date;
                
SELECT * FROM sales_2017
ORDER BY order_date DESC, 
         total DESC;
         
        
