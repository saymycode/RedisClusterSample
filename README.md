# RedisClusterSample

## Overview
This is a sample project demonstrating the use of Redis clusters in a .NET Core application. The project performs read and write operations on multiple Redis clusters and outputs execution times in the console.

## Project Structure
```
RedisClusterSample
├── RedisClusterSample
│   ├── Program.cs
│   └── redis_clusters.json
└── README.md
```

## Install Dependencies
Ensure you have the .NET SDK installed. You can install the necessary dependencies using the following command:

```bash
dotnet restore
```

## Configure Redis Clusters
Create a `redis_clusters.json` file in the project root directory. The file should contain the cluster configurations in the following format:

```json
{
  "clusters": [
    "localhost:6379", 
    "localhost:6479"
  ],
  "passwords": [
    "localhost:6379", 
    "localhost:6579"
  ],
  "products": [
    "localhost:6379", 
    "localhost:6679"
  ]
}
```

## Testing Read and Write Operations
The application will perform read and write tests on each configured Redis cluster. You will see the execution time for write and read operations in the console output.

### Example Output
```bash
Write Operation - Time: 10ms
Read Operation - Time: 5ms
```

## Contributing
Contributions are welcome! Please feel free to submit a pull request or open an issue for any enhancements or bug fixes.

## Contact
For any questions or suggestions, please contact [berkuluusoy@gmail.com].


---


# RedisClusterSample

## Genel Bakış
Bu, bir .NET Core uygulamasında Redis kümelerinin kullanımını gösteren bir örnek projedir. Proje, birden fazla Redis kümesinde okuma ve yazma işlemleri gerçekleştirir ve konsol çıktısında işlem sürelerini görüntüler.

## Proje Yapısı
```
RedisClusterSample
├── RedisClusterSample
│   ├── Program.cs
│   └── redis_clusters.json
└── README.md
```

## Bağımlılıkları Yükleme
.NET SDK'sının yüklü olduğundan emin olun. Gerekli bağımlılıkları aşağıdaki komut ile yükleyebilirsiniz:

```bash
dotnet restore
```

## Redis Kümelerini Yapılandırma
Proje kök dizininde bir `redis_clusters.json` dosyası oluşturun. Dosya, kümelerin yapılandırmalarını aşağıdaki formatta içermelidir:

```json
{
  "clusters": [
    "localhost:6379", 
    "localhost:6479"
  ],
  "passwords": [
    "localhost:6379", 
    "localhost:6579"
  ],
  "products": [
    "localhost:6379", 
    "localhost:6679"
  ]
}
```

## Okuma ve Yazma İşlemlerini Test Etme
Uygulama, her yapılandırılmış Redis kümesinde okuma ve yazma testleri gerçekleştirecektir. Yazma ve okuma işlemleri için işlem sürelerini konsol çıktısında görebileceksiniz.

### Örnek Çıktı
```bash
Yazma İşlemi - Süre: 10ms
Okuma İşlemi - Süre: 5ms
```

## Katkıda Bulunma
Katkılarınızı bekliyoruz! Herhangi bir geliştirme veya hata düzeltmesi için pull request gönderebilir veya bir sorun açabilirsiniz.

## İletişim
Herhangi bir sorunuz veya öneriniz varsa, lütfen [berkuluusoy@gmail.com] ile iletişime geçin.
