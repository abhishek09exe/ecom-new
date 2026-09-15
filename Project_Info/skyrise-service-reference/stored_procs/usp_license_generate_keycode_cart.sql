-- Source: QA DB via sp_helptext. Referenced in doc 04 (04_SKYRISE_KEYCODE_GENERATION.md), Section 4.
-- This is the DB-side keycode fallback algorithm used when the Skyrise call fails or returns no keycode.
-- Two distinct output formats depending on product_family_id (see Section 4 for the plain-English summary):
--   - families 100,200,300,50,51,55: SHA1-hash based, with collision detection against the `license` table
--   - all other families: sequence + custom base-26 checksum
-- Also contains the QA/test keycode-modification marker (environment.keycode_modification_string).

CREATE PROCEDURE [dbo].[usp_license_generate_keycode_cart]
(
 @product_id int,
 @license_distribution_method_id int,
 @keycode varchar(40) output,
 @response_code int output,
 @message varchar(100) output
)
AS
/*
 DATE       AUTHOR   REMARKS
 2006-08-31 esmart   Initial creation.
 2006-10-03 esmart   Add check for missing product version, use product_version from root_product_id
 2010-08-25 esmart   NULL product version set = '1.0'
 2011-08-22 esmart   Add support for new keycode format (MD5 hash)
 2011-08-31 towens   added support for keycode collision - change from MD5 to SHA1
 2011-12-19 esmart   Add environment lookup to all
 2012-06-13 esmart   Add Android to new keycode format

 DESCRIPTION
 Generates new keycode
*/
set nocount on

declare @product_family_prefix varchar(2),
  @product_family_id int,
  @product_version varchar(2),
  @product_version_char varchar(2),
  @license_distribution_method_code varchar(4),
  @keycode_sequence_id int,
  @keycode_sequece_string varchar(12),
  @base int

set @base = 26
set @keycode_sequece_string = ''

-- check @product_id
select @response_code =  case when count(*) = 0 then -103 else 0  end,
  @message = case when count(*) = 0 then 'Invalid product_id or non-keycoded product' else '' end
from product
where product_id = @product_id and uses_keycode = 1

if @response_code < 0
 begin
 set @keycode = ' '
 return
 end

-- check @license_distribution_method_id
select @response_code =  case when count(*) = 0 then -107 else 0  end,
  @message = case when count(*) = 0 then 'Invalid license_distribution_method_id' else '' end
from license_distribution_method
where license_distribution_method_id = @license_distribution_method_id

if @response_code < 0
 begin
 set @keycode = null
 return
 end

-- ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
-- 1.) get product family prefix
-- ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

select @product_family_prefix = product_family_prefix,
  @product_family_id = p.product_family_id
from product_family f
inner join product p
 on f.product_family_id = p.product_family_id
where product_id = @product_id

-- ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
-- 2.) get max major and minor version for product_line
-- ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

select @product_version = replace(max(product_version),'.','')
from product_versions
where product_id = @product_id

if @product_version is null
 begin
 select @product_version = replace(max(product_version),'.','')
 from product_versions v
 inner join product p
  on p.root_product_id = v.product_id
 where p.product_id = @product_id
 end

if @product_version is null
 begin
 select @product_version = '10'
 end

set @product_version_char = char(ascii('A') + ascii(left(@product_version, 1)) - ascii('0')) + CHAR(ascii('A') + ascii(right(@product_version, 1)) - ascii('0'))

-- ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
-- 3.) get license distribution method code
-- ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

select @license_distribution_method_code = license_distribution_method_code
from license_distribution_method
where license_distribution_method_id = @license_distribution_method_id

-- ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
-- 4.) get keycode sequence id
-- ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

insert into keycode_sequence (insert_date)
values (getdate())

set @keycode_sequence_id = scope_identity()

-- ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
-- 5.) create sequence portion of keycode
-- ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

while @keycode_sequence_id >= @base
 begin
 set @keycode_sequece_string = char(@keycode_sequence_id % @base + ascii('A')) + @keycode_sequece_string
 set @keycode_sequence_id = @keycode_sequence_id / @base
 end

set @keycode_sequece_string = CHAR(@keycode_sequence_id + ASCII('A')) + @keycode_sequece_string;

while len(@keycode_sequece_string) < 9
 begin
 set @keycode_sequece_string = 'A' + @keycode_sequece_string;
 end

-- ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
-- 6.) keycode minus checksum
-- ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

set @keycode =  @product_family_prefix + @product_version_char + @license_distribution_method_code + @keycode_sequece_string

-- ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
-- 7.) check sum
-- ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

declare @sum int,
  @zn int,
  @ch int,
  @chk_sum varchar(3)

-- this is our checksum building component of the keycode generator.
set @sum = 0
set @zn = 1

while @zn < 18
 begin
 set @ch = ascii(substring(@keycode,@zn,1))
 set @sum = @sum + (( @ch * 128 ) * 5 ) + (( @ch * 1024 ) * 3 ) + ( @ch * 17 )
 set @zn = @zn + 1
 end

set @sum = @sum % ( 26 * 26 * 26 )

-- base26 the checksum
set @chk_sum = ''
while @sum >= @base
 begin
 set @chk_sum = (char((@sum % @base) + ascii('A'))) + @chk_sum
 set @sum = @sum / @base
 end

set @chk_sum = char(@sum + ascii('A')) + @chk_sum

-- pad the returned value of checksums with As representing 0s
while datalength(@chk_sum) < 3
 begin
 set @chk_sum = 'A' + @chk_sum
 end

-- ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
-- 7.) generate the keycode
-- ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

if @product_family_id in (100,200,300,50,51,55)
begin
 declare @md5_string varchar(40)
 declare @cnt int
 declare @inc int -- this will be the new starting point is the ketcode has a collision
 set @inc = 2

 select @md5_string = upper(convert(varchar(40),sys.fn_sqlvarbasetostr(HashBytes('SHA1',@keycode))))
 set @md5_string = REPLACE(REPLACE(@md5_string,'0',''),'1','')

 if len(@md5_string) < 14
 begin
  select @md5_string = upper(convert(varchar(40),sys.fn_sqlvarbasetostr(HashBytes('SHA1',@keycode))))
  set @md5_string = REPLACE(REPLACE(@md5_string,'0',''),'1','')
 end

 select @keycode = @product_family_prefix+left(right(@md5_string,14),2)+@license_distribution_method_code+right(@md5_string,12)
 select @cnt = count(*) from license where keycode = @keycode -- check for keycode collision

 while @cnt > 0 -- if there is a collision loop until there is not one
 begin
  select @keycode = @product_family_prefix+left(right(@md5_string,14),2)+@license_distribution_method_code+SUBSTRING(@md5_string,@inc,12)
  select @cnt = count(*) from license where keycode = @keycode
  set @inc = @inc + 1
 end -- end the keycode loop sillyness
end
else
begin
 select @keycode = @keycode + @chk_sum
end

-- ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
-- 8.) update for test keycode from QA
-- ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

declare @keycode_modification_string varchar(4)
select @keycode_modification_string = keycode_modification_string from environment

if @keycode_modification_string is not null
begin
 if @product_family_id in (100,200,300,50,51,55)
 begin
  select @keycode = substring(@keycode,1,16) + @keycode_modification_string
 end
 else
 begin
  select @keycode = substring(@keycode,1,8) + substring(@keycode,13,8) + @keycode_modification_string
 end
end
