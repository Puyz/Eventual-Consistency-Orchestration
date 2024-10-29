# Saga Nedir?
Saga, birden fazla bağımsız servisteki transaction'ların sistematik bir şekilde işlenerek veri tutarlılığının sağlanmasını amaçlayan bir tasarım desenidir. İlk olarak 1987 yılında (bknz: [SAGAS](https://www.cs.cornell.edu/andru/cs711/2002fa/reading/sagas.pdf)) başlıklı akademik bir makalede tanımlanmıştır. Bu desenin temel mantığı, bir transaction'ın kullanıcı etkileşimi gibi dış bir tetikleyiciyle başlatılmasının ardından, her bir servis transaction'ının başarı durumuna göre sonraki servisin transaction'ının tetiklenmesi esasına dayanır. Bu sayede, mikroservis mimarisi kullanan bir projede, bir işlem sonucunda tüm servislerdeki veriler uyumlu bir şekilde işlenmiş olur. 

Eğer bir transaction sırasında hata oluşur ya da iş mantığı gereği işlem iptal edilmesi gerekirse, tüm süreç durdurulur ve o ana kadar yapılan işlemler geri alınır **(Compensable Transaction)**. Böylece, tüm süreçlerin geri alınabilmesi ile **Atomicity prensibi** korunmuş olur.

- ***Saga, microservice’ler arasında doğru transaction yönetimi ve veri tutarlılığı sağlayan bir design pattern’dır.*** 

Saga, mikroservisler arasında tutarlı bir transaction yönetimi sağlayan bir tasarım desenidir. Bu desenin uygulanması için iki temel yaklaşım geliştirilmiştir: **Events/Choreography** ve **Command/Orchestration**. Şimdi bu yaklaşımlardan biri olan **Choreography** yaklaşımını inceleyelim.


## Saga – Command/Orchestration Implemantasyonu

![image](https://github.com/user-attachments/assets/24a41ae4-bd9e-4961-a4db-81abd72d95e0)

Bu yaklaşımda servisler arası distributed transaction merkezi bir denetleyici ile koordine edilir. Bu denetleyiciye **Saga State Machine** ya da bir başka isim olarak **Saga Orchestrator** denmektedir. 

Saga Orchestrator, servisler arasındaki tüm işlemleri yönetir ve olaylara göre hangi işlemin gerçekleştirileceğini söyler.

Saga Orchestrator, diğer ismi olan Saga State Machine adından da anlaşıldığı üzere her kullanıcıdan gelen isteğe dair uygulama state’ini(durum) tutmakta, yorumlamakta ve gerektiğinde telafi edici işlemleri uygulamaktadır.


Misal olarak yukardaki şematize edilmiş e-ticaret senaryosunu ele alırsak eğer;

![image](https://github.com/user-attachments/assets/eb3e7948-ea27-4278-b295-f710eda2f830)

* **Order Service** sipariş isteğini alır ve durumunu **Suspend** olarak kaydeder. Ardından bu siparişe dair geri kalan işlemleri başlatmak için **Saga Orchestrator**‘a ***ORDER_CREATED*** türünden komut göndererek sipariş oluşturma transaction’ını başlatır.

* **Saga Orchestrator**, ***EXECUTE_PAYMENT*** komutunu **Payment Service**‘e gönderir. İlgili servis ödeme alındığına dair başarılı ya da başarısız bilgiyi tekrar **Saga Orchestrator**‘a döndürür.

* **Saga Orchestrator**, ***UPDATE_STOCK*** türünden komutu **Stock Service**‘e gönderir ve ilgili servis stok bilgisinin güncellenmesini gerçekleştirir. Yine nihai durum başarılı ya da başarısız olarak **orchestrator**’a döndürülür.

* **Orchestrator**, ***ORDER_DELIVER*** komutunu **Delivery Service**‘e gönderir ve ilgili servis siparişin kargolandığı bilgisini başarılı yahut başarısız olarak döndürür.

* Ve en nihayetinde sipariş durumu **Completed** olarak güncellenir.

- ***Saga Orchestration implemantasyonunda Saga Choreography’de olduğu gibi asynchronous messaging pattern tercih edilmektedir.***

Yukarıdaki senaryoda servislerden herhangi birinde olası bir hata meydana geldiği taktirde aşağıdaki gibi hareket edilmelidir. Bu örnekte hata ‘Stock Service’ üzerinden misallendirilmektedir;


![image](https://github.com/user-attachments/assets/0f0e86d1-ae0a-46e9-acde-21367423cc75)

* **Stock Service**‘de sipariş edilen ürün adedi stok miktarından fazla ise **orchestrator**’a ***OUT_OF_STOCK*** komutu gönderilmektedir.

* Ardından **orchestrator** işlem sürecinde başarısızlık olduğu bilgisini alıp *rollback* işlemlerini başlatır. *(Compensable Transaction)*

Her işlem için Saga üzerinde state bilgisi tutmak hangi adımda süreci yanlış yönettiğinizi görmeyi kolaylaştıracaktır.
#
#### Orchestration implemantasyonunun faydalarına gelirsek eğer;

* Birçok servisin bulunduğu ve zaman içinde servislerin eklendiği karmaşık iş akışları için idealdir. Burada alt eşik 4 adet servistir. 4’ün üzerinde servis söz konusuysa eğer orchestration implemantasyonu tercih edilir.

* Her servisin ve bu servislerin faaliyetlerinin üzerinde merkezi kontrol sağlar.

* Orchestration implemantasyonu, tek taraflı olarak Saga katılımcılarına(servisler) bağlı olduğundan dolayı döngüsel bağımlılıklar söz konusu değildir.

* Her bir servisin diğer servisle ilgili bilmesi gereken herhangi bir şeye ihtiyacı yoktur! Haliyle böylece Separation of Concerns söz konusudur.

* Uygulaması ve test etmesi choreography implemantasyonuna nazaran daha kolaydır.

* Yapılan işler doğrusal kalacağından dolayı geri alma yahut telafi yönetimleri daha kolaydır.

**! Orchestration implemantasyonunun tek dezavantajı tüm iş akışının yönetiminin Saga Orchestrator tarafından gerçekleştiriliyor olmasıdır diyebiliriz.**

###
**! Saga Orchestrator, subscribe olan tüm servisleri çağırabilir ancak servisler orchestrator’ü çağıramaz, çağırmamalıdır!**
