# مستند API سامانهٔ دایادایموند

متن زیر عیناً همان مستندی است که Mahsa در تاریخ ۲۰۲۶-۱۰-۲۸ در چت فرستاد. متن از PDF کپی شده، برای همین ترتیب بعضی کلیدها و مقدارهای فارسی به‌هم ریخته است.
نسخهٔ مرتب‌شدهٔ همین سرویس‌ها در کد `src/DayaDiamond.Client` و در [dayadiamond-client-README.md](dayadiamond-client-README.md) آمده است.

```text
سرویس های پایه
دریافت Jwt:
دریافت توکن جهت فراخوانی سرویس ها
Method: Post
Request : : {BaseUrl}/account/login
Payload:
{
“username”:”x”,
“password”:”x”
}
Response:
{
"isSuccessful": true,
"error": null,
"result": {
"accesToken": “ey…”
}
}
مستند سرویس ها ی سامانه دایادایموند
قیمت دارایی :
با فراخونی سرویس ذیل، می توان قیمت دارایی ارسالی را دریافت نمایید
Method: Get
Authorization Header: JwtToken
Request: {BaseUrl}/price/token
Response:
{
 "isSuccessful": true,
 "error": null,
 "result": {
شناسه دارایی ,"DAYADIAMOND ":"assetCode "
مبلغ توکن به تومان 1605046 :"tokenPrice "
 }
}
مستند سرویس ها ی سامانه دایادایموند
لیست درگاه ها:
Method: Get
Authorization Header: JwtToken
Request: {BaseUrl}/gateway/list
Response:
{
"isSuccessful": true,
"error": null,
"result": [
{
"id": 5,
"title": "OmidPay",
,"امید پی " :"description"
"message": null
},
{…}
]
مستند سرویس ها ی سامانه دایادایموند
پروفایل مشتری
ثبت نام مشتری:
برای ثبت نام مشتری ، شماره موبایل کاربر Encrypt شده برای سرویس دایا ارسال میشود. با استفاده
از پارامترهای ارسالی، ما باید بتوانیم شماره موبایل رو Decrypt کنیم
Method: Post
Authorization Header: JwtToken
Request: {BaseUrl}/user/getOrAdd
Payload
{
"code":"rhk90iA8cemlSI7i0Y5guA=="
}
Response :
{
"phoneNumber":"09121111111",
"profileCompleted":true,
"addressCompleted":false
}
مستند سرویس ها ی سامانه دایادایموند
روش Encrypt شماره موبایل :
نمونه کد سی شار پ جهت انکریپت شماره موبایل
public string Encrypt (string mobile)
{
 byte[] plainText = Encoding.UTF8.GetBytes(mobile);
 byte[] key = Convert.FromBase64String(_base64Key);
 byte[] iv = Convert.FromBase64String(_base64IV);
 using (Aes aesAlg = Aes.Create())
 {
 aesAlg.Key = key;
 aesAlg.IV = iv;
 aesAlg.Mode = CipherMode.ECB;
 aesAlg.Padding = PaddingMode.PKCS7;
 using (ICryptoTransform encryptor =
 aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV))
 {
 byte[] encryptedBytes = encryptor.TransformFinalBlock(
 plainText, 0, plainText.Length);
 return Convert.ToBase64String(encryptedBytes);
 }
 }
}
برای دایادایموند باید اطالعات ذیل ارسال شود
key = Convert.FromBase64String(_base64Key); -1
iv = Convert.FromBase64String(_base64IV); -2
مستند سرویس ها ی سامانه دایادایموند
افزودن یا ویرایش پروفایل مشتری:
کاربر بعد از ثبت نام ، باید اطالعات پروفایل خود را تکمیل نماید
Method: Post
Authorization Header: JwtToken
Request: {BaseUrl}/profile/upsertInfo
Payload
{
"phoneNumber":"09121111111",
"nationalCode":"0011111111",
,"نظری ":"lastName"
,"مرتضی":"fatherName"
,"سید حامد":"firstName"
"persianBirthDate":"1360/11/12",
"gender":"Male",
"sheba":"360560611828006835245102"
}
Response:
{
"isSuccessful":true,
"error":null,
"result":null
}
مستند سرویس ها ی سامانه دایادایموند
افزودن یا ویرایش آدرس:
با استفاده از این سرویس، اطالعات محل سکونت کاربر ثبت میشود که در قرارداد مورد استفاده قرار
میگیرد
Method: Post
Authorization Header: JwtToken
Request: {BaseUrl}/profile/upsertAddress
Payload
{
"phoneNumber":"09121111111",
,"تهران":"city"
"postalCode":"1234567890",
,"تهران":"province"
,"خ ولیعصر":"street"
"telephoneNumber":"02188888888"
}
Response:
{
"isSuccessful":true,
"error":null,
"result":null
}
مستند سرویس ها ی سامانه دایادایموند
خرید دارایی
قوانین خرید:
Method: Get
Authorization Header: JwtToken
Request: {BaseUrl}/buy/policy
Response:
{
"isSuccessful": true,
"error": null,
"result": {
حداقل تعداد توکن قابل خریداری 0.5, :"minAmount"
 حداکثر تعداد توکن قابل خریداری 120, :"maxAmount"
درصد کارمزد 0, :"feePercentage"
روز تعطیل است یا خیر ,false :"isHoliday"
مدت بازخرید ,null :"withdrawDuration"
"message": null
}
}
مستند سرویس ها ی سامانه دایادایموند
دریافت مبلغ خرید:
Method: Get
Authorization Header: JwtToken
Request: {BaseUrl}/price/buy?amount=3&assetCode=DAYADIAMOND
Response:
{
"isSuccessful":true,
"error":null,
"result":{
مبلغ قابل پرداخت :4797513"price"
}
}
مستند سرویس ها ی سامانه دایادایموند
ثبت درخواست خرید:
Method: Post
Authorization Header: JwtToken
Request: {BaseUrl}/buy/request
Payload: {
 شماره موبایل کاربر ,“0912” :“phoneNumber“
تعداد توکن مورد نیاز :3,"amount"
شناسه درگاه :5,"gatewayId"
آدرس برگشت بعد از درگاه ThirdParty-returnUrl://https:"returnUrl"
}
Response:
{
"isSuccessful": true,
"error": null,
"result": {
"payUrl": "{BaseUrl} /gateway/redirectToIpg/?paymentExtId=60fb4005-d534-492a-a2ac-6beea991b08e",
“paymentExtId”:” 60fb4005-d534-492a-a2ac-6beea991b08e”
}
}
نکته : بعد از دریافت PayUrl، جهت هدایت کاربر به درگاه بانکی است
مستند سرویس ها ی سامانه دایادایموند
بازخرید دارایی
بررسی درخواست بازخرید:
بررسی اینکه کاربر میتواند درخواست فروش ثبت کند یا خیر. برای مثال ، تایم بازخرید ، بروزنبودن
قیمت و ...
Method: Get
Authorization Header: JwtToken
Request URL:
{BaseUrl}/withdraw/validateRequest?tokenAmount=2&phoneNumber=09191751976
Response:
{
"isSuccessful":true,
"error":null,
"result":null
}
```
