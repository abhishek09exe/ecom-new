Text
  
CREATE PROCEDURE [dbo].[usp_cart_select_message_key]  
(  
    @message_key VARCHAR(36),  
    @license_category_name VARCHAR(20) = NULL,  
    @years INT = NULL,  
    @seats INT = NULL,  
    @sku VARCHAR(200) = NULL  
)  
  
/*   
 DATE  AUTHOR  REMARKS  
 2017-07-11 esmart  Initial creation.  
 2018-07-30 esmart  Add license_keycode_type_id  
 2020-08-06 wbarton  For license_message_key, changing the payment_header_id and customer_id to reference the license's most recent order data rather than license_message_value, which has data-maintenance issues.   
 2024-03-05 rambasna Ecom-4248 For zuora product price, return zuora campagn_id and rate_plan_id  
  
 DESCRIPTION  
 select message key data  
  
*/  
  
AS  
SET NOCOUNT ON;  
  
DECLARE @message_key_type VARCHAR(50),  
        @message_key_json NVARCHAR(MAX),  
        @keycode VARCHAR(40),  
        @license_id INT,  
        @cart_order_id INT,  
        @response_code INT,  
        @message VARCHAR(100);  
  
BEGIN TRY  
  
    -- ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------  
    -- 1.) select  
    -- ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------  
    --1.0) zoura_campaign_id  
  
    IF ISNUMERIC(@message_key) = 0  
    BEGIN  
        -- 1.1) license_key  
        IF @message_key_type IS NULL  
            SELECT @message_key_type = 'license_key'  
            FROM license_key  
            WHERE license_key = @message_key;  
  
        -- 1.1.1) license_message  
        IF @message_key_type IS NULL  
        BEGIN  
            SELECT @message_key_type = 'license_message_key',  
                   @license_id = l.license_id,  
                   @keycode = l.keycode  
            FROM license_message m  
                INNER JOIN license l  
                    ON l.license_id = m.license_id  
            WHERE m.license_message_key = @message_key;  
        END;  
  
        -- 1.1.2) cart_discount_key  
        IF @message_key_type IS NULL  
        BEGIN  
            SELECT @message_key_type = 'cart_discount_key'  
            FROM cart_discount  
            WHERE cart_discount_key = @message_key;  
        END;  
  
        -- 1.1.3) cart_discount_message_key  
        IF @message_key_type IS NULL  
        BEGIN  
            SELECT @message_key_type = 'cart_discount_message_key'  
            FROM cart_discount_message  
            WHERE cart_discount_message_key = @message_key;  
        END;  
    END;  
  
    -- 1.1.4) zuora_campaign_id  
    IF ISNUMERIC(@message_key) = 1  
    BEGIN  
        IF EXISTS  
        (  
            SELECT 1  
            FROM dbo.zuora_product_pricing  
            WHERE campaign_id = @message_key  
        )  
        BEGIN  
            SELECT @message_key_type = 'zuora_campaign_id'  
            FROM dbo.zuora_product_pricing  
            WHERE campaign_id = @message_key;  
        END;  
    END;  
  
    -- select message_key_type = @message_key_type  
  
    -- ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------  
    -- 2.)   
    -- ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------  
    --2.0) zoura_campaign_id   
    IF @message_key_type = 'zuora_campaign_id'  
    BEGIN  
  
        IF @sku IS NOT NULL  
        BEGIN  
            SELECT @message_key_json =  
            (  
                SELECT DISTINCT  
                       zp.campaign_id,  
                       lc.license_category_name,  
                       zp.years,  
                       zp.seats,  
                       zp.rate_plan_id,  
   zp.sku,  
                       ((zp.renewal_price - zp.retail_price) / zp.renewal_price) * 100 discount  
                FROM dbo.zuora_product_pricing zp  
                    LEFT JOIN license_category lc  
                        ON lc.license_category_id = zp.license_category_id  
                WHERE campaign_id = @message_key  
                      AND ISNULL(zp.years, 0) = ISNULL(@years, 0)  
                      AND ISNULL(zp.seats, 0) = ISNULL(@seats, 0)  
                      AND ISNULL(lc.license_category_name, 0) = ISNULL(@license_category_name, 0)  
                      AND zp.sku = @sku  
                FOR JSON PATH, WITHOUT_ARRAY_WRAPPER  
            );  
        END;  
        ELSE IF @license_category_name IS NOT NULL AND @years IS NOT NULL AND @seats IS NOT null  
     SELECT @message_key_json =  
            (  
                SELECT DISTINCT  
                       zp.campaign_id,  
                       lc.license_category_name,  
                       zp.years,  
                       zp.seats,  
                       zp.rate_plan_id,  
                       zp.sku,  
                       ((zp.renewal_price - zp.retail_price) / zp.renewal_price) * 100 discount  
                FROM dbo.zuora_product_pricing zp  
                    LEFT JOIN license_category lc  
                        ON lc.license_category_id = zp.license_category_id  
                WHERE campaign_id = @message_key  
                      AND ISNULL(zp.years, 0) = ISNULL(@years, 0)  
                      AND ISNULL(zp.seats, 0) = ISNULL(@seats, 0)  
                      AND ISNULL(lc.license_category_name, 0) = ISNULL(@license_category_name, 0)  
                FOR JSON PATH, WITHOUT_ARRAY_WRAPPER  
            );  
  
        SELECT message_key_type = @message_key_type,  
               message_key_json = @message_key_json;  
        RETURN;  
    END;  
  
    -- 2.1) license_key data  
    IF @message_key_type = 'license_key'  
    BEGIN  
        SELECT @message_key_json =  
        (  
            SELECT l.license_id,  
                   l.keycode,  
                   customer_id,  
                   l.license_keycode_type_id  
            FROM license_key k  
                INNER JOIN license l  
                    ON k.license_id = l.license_id  
            WHERE k.license_key = @message_key  
            FOR JSON PATH, WITHOUT_ARRAY_WRAPPER  
        );  
  
        SELECT message_key_type = @message_key_type,  
               message_key_json = @message_key_json;  
        RETURN;  
    END;  
  
    -- 2.2) license_message data  
    IF @message_key_type = 'license_message_key'  
    BEGIN  
        -- select @message_key_type  
  
        DECLARE @license_message_values TABLE  
        (  
            message_value_type_name VARCHAR(50),  
            value_id VARCHAR(20)  
        );  
        DECLARE @payment_header_id INT,  
                @customer_id INT;  
  
        INSERT INTO @license_message_values  
        (  
            message_value_type_name,  
            v.value_id  
        )  
        SELECT t.message_value_type_name,  
               v.value_id  
        FROM license_message m  
            INNER JOIN license_message_value v  
                ON v.license_message_id = m.license_message_id  
            INNER JOIN message_value_type t  
                ON v.message_value_type_id = t.message_value_type_id  
        WHERE m.license_message_key = @message_key;  
  
        --Setting @payment_header_id and @customer_id to values from the license's most recent order  
        SELECT @payment_header_id = oph.payment_header_id,  
               @customer_id = co.customer_id  
        FROM order_payment_header oph  
            INNER JOIN  
            (  
                SELECT oil.license_id,  
                       MAX(oi.order_header_id) AS most_recent_order  
                FROM order_item_license oil  
                    INNER JOIN order_item oi  
                        ON oi.order_item_id = oil.order_item_id  
                WHERE oil.license_id = @license_id  
               GROUP BY oil.license_id  
            ) oi  
                ON oph.order_header_id = oi.most_recent_order  
            INNER JOIN dbo.customer_order co  
                ON co.order_header_id = oph.order_header_id  
                   AND co.customer_type_id = 1;  
  
        SELECT @message_key_json =  
        (  
            SELECT license_id = @license_id,  
                   keycode = @keycode,  
                   payment_header_id = @payment_header_id,  
                   customer_id = @customer_id,  
                   cart_discount_id =  
        (  
            SELECT value_id  
            FROM @license_message_values  
            WHERE message_value_type_name = 'cart_discount_id'  
        )   ,  
                   p_rc =  
        (  
            SELECT value_id  
            FROM @license_message_values  
            WHERE message_value_type_name = 'p_rc'  
        )   ,  
                   p_rsc =  
        (  
            SELECT value_id  
            FROM @license_message_values  
            WHERE message_value_type_name = 'p_rsc'  
        )   ,  
                   p_ac =  
        (  
            SELECT value_id  
            FROM @license_message_values  
            WHERE message_value_type_name = 'p_ac'  
        )   ,  
                   trx_rc =  
        (  
            SELECT value_id  
            FROM @license_message_values  
            WHERE message_value_type_name = 'trx_rc'  
        )   ,  
                   trx_rsc =  
        (  
            SELECT value_id  
            FROM @license_message_values  
            WHERE message_value_type_name = 'trx_rsc'  
        )   ,  
                   trx_ac =  
        (  
            SELECT value_id  
            FROM @license_message_values  
            WHERE message_value_type_name = 'trx_ac'  
        )  
            FOR JSON PATH, WITHOUT_ARRAY_WRAPPER  
        );  
  
        SELECT message_key_type = @message_key_type,  
               message_key_json = @message_key_json;  
        RETURN;  
    END;  
  
    -- 2.3) cart_discount_key data  
    IF @message_key_type = 'cart_discount_key'  
    BEGIN  
  
        SELECT @message_key_json =  
        (  
            SELECT d.cart_discount_id,  
                   i.license_category_name,  
                   i.license_seats,  
                   i.storage_gb,  
                   years = CONVERT(DECIMAL(4, 2), i.years),  
                   i.cart_discount_method_id,  
                   discount = CONVERT(DECIMAL(10, 4), i.discount),  
                   license_keycode_type_id = CASE  
                                                 WHEN i.license_category_id IN ( 220, 230, 231 ) THEN  
                                                     3  
                                                 ELSE  
                                                     1  
                                             END  
            FROM cart_discount d  
                INNER JOIN cart_discount_item i  
                    ON i.cart_discount_id = d.cart_discount_id  
            WHERE d.cart_discount_key = @message_key  
            FOR JSON PATH, WITHOUT_ARRAY_WRAPPER  
        );  
  
        SELECT message_key_type = @message_key_type,  
               message_key_json = @message_key_json;  
        RETURN;  
    END;  
  
    -- 2.3) cart_discount_message_key data  
    IF @message_key_type = 'cart_discount_message_key'  
    BEGIN  
  
        DECLARE @discount_message_values TABLE  
        (  
            message_value_type_name VARCHAR(50),  
            value_id VARCHAR(20)  
        );  
  
        INSERT INTO @discount_message_values  
        (  
            message_value_type_name,  
            v.value_id  
        )  
        SELECT t.message_value_type_name,  
               v.value_id  
        FROM cart_discount_message m  
            INNER JOIN cart_discount_message_value v  
                ON v.cart_discount_message_id = m.cart_discount_message_id  
            INNER JOIN message_value_type t  
                ON v.message_value_type_id = t.message_value_type_id  
        WHERE m.cart_discount_message_key = @message_key;  
  
        SELECT @cart_order_id = value_id  
        FROM @discount_message_values  
        WHERE message_value_type_name = 'cart_order_id';  
  
        IF @cart_order_id IS NOT NULL  
        BEGIN  
            IF  
            (  
                SELECT cart_order_status_id  
                FROM cart_order  
                WHERE cart_order_id = @cart_order_id  
            ) = 2  
            BEGIN  
                SELECT @message_key_json = NULL;  
            END;  
            ELSE  
            BEGIN  
                SELECT @message_key_json =  
                (  
                    SELECT cart_order_id,  
                           vendor_order_code  
                    FROM cart_order  
                    WHERE cart_order_id = @cart_order_id  
                    FOR JSON PATH, WITHOUT_ARRAY_WRAPPER  
                );  
            END;  
        END;  
  
        SELECT message_key_type = @message_key_type,  
               message_key_json = @message_key_json;  
        RETURN;  
    END;  
  
--end  
END TRY  
BEGIN CATCH  
  
    SET @response_code = -200;  
    SET @message = CASE  
                       WHEN @message IS NULL THEN  
                           'select failed'  
                       ELSE  
                           @message  
                   END;  
  
    DECLARE @DBName NVARCHAR(128);  
    SET @DBName = DB_NAME();  
    EXEC usp_LogError @ErrorDB = @DBName;  
  
END CATCH;  
  