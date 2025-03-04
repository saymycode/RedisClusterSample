using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;


//hangi parametrenin hangi slave'e yazılacağı bilinecek. 
//ben gidip organizasyon yazdım. bu organizasyonların heppsi mesela organization slave'de olcak ve ben gidip, master'dan organizasyonlu bir şey
//istediğim zaman otomatik olarak gidp organization slave'den okuyacak.
//her şey parametrik olcak. 
//nereye ne yazılcağı nerden okuncağı parrrametrik olcak.
class Program
{
    static async Task Main(string[] args)
    {
        // Master ve 3 Slave Redis sunucularının yapılandırması
        var configurationOptions = new ConfigurationOptions
        {
            AbortOnConnectFail = false,
            ConnectTimeout = 10000,
            SyncTimeout = 10000,
            AllowAdmin = true,
            // Master ve üç Slave node'u ekliyoruz
            EndPoints = 
            {
                { "localhost:6379" }, // Master - 6379 portu
                { "localhost:6479" }, // Slave 1 - 6479 portu
                { "localhost:6579" }, // Slave 2 - 6579 portu
                { "localhost:6679" }  // Slave 3 - 6679 portu
            }
        };

        try
        {
            // Bağlantıyı kur
            var connection = await ConnectionMultiplexer.ConnectAsync(configurationOptions);
            Console.WriteLine("Redis Master-Slave yapısına başarıyla bağlandı!");

            // Master'a yazma işlemi için veritabanı bağlantısı 
            // Not: StackExchange.Redis otomatik olarak yazma işlemleri için master'ı kullanır
            var db = connection.GetDatabase();

            // Verileri Master'a yaz
            await db.StringSetAsync("testKey", "Bu veri Master'a yazıldı");
            Console.WriteLine("Veri Master'a başarıyla yazıldı");

            // Normal okuma işlemi - Varsayılan olarak okuma işlemleri kullanılabilir
            // herhangi bir node'dan (master veya slave) yapılabilir
            var value = await db.StringGetAsync("testKey");
            Console.WriteLine($"Okunan değer: {value}");

            // Okuma işlemini özellikle Slave üzerinden yapmak için CommandFlags kullan
            var valueFromSlave = await db.StringGetAsync("testKey", CommandFlags.PreferSlave);
            Console.WriteLine($"Slave'den okunan değer: {valueFromSlave}");

            // Okuma işlemini özellikle Master üzerinden yapmak için
            var valueFromMaster = await db.StringGetAsync("testKey", CommandFlags.PreferMaster);
            Console.WriteLine($"Master'dan okunan değer: {valueFromMaster}");

            // Redis sunucularının durumunu kontrol et
            Console.WriteLine("\nRedis Sunucuları Durumu:");
            foreach (var endpoint in connection.GetEndPoints())
            {
                var server = connection.GetServer(endpoint);
                Console.WriteLine($"  - Sunucu: {endpoint}, Bağlantı Durumu: {(server.IsConnected ? "Bağlı" : "Bağlı değil")}");
                
                try
                {
                    // Redis INFO komutu ile sunucu rolünü kontrol et
                    var roleInfo = server.Execute("INFO", "replication");
                    var roleStr = roleInfo.ToString();
                    
                    // Role bilgisini ayıkla
                    string role = "Bilinmiyor";
                    if (roleStr.Contains("role:master"))
                    {
                        role = "Master";
                        Console.WriteLine($"    Rol: {role}");
                        
                        // Master için bağlı slave sayısını göster
                        if (roleStr.Contains("connected_slaves:"))
                        {
                            var slavesLine = roleStr.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None)
                                .FirstOrDefault(line => line.StartsWith("connected_slaves:"));
                            
                            if (slavesLine != null)
                            {
                                var slaveCount = slavesLine.Split(':')[1];
                                Console.WriteLine($"    Bağlı Slave Sayısı: {slaveCount}");
                            }
                            
                            // Slave node'ların detaylarını göster
                            for (int i = 0; i < 3; i++)
                            {
                                var slaveInfoLine = roleStr.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None)
                                    .FirstOrDefault(line => line.StartsWith($"slave{i}:"));
                                
                                if (slaveInfoLine != null)
                                {
                                    Console.WriteLine($"    Slave {i} Bilgisi: {slaveInfoLine.Split(':')[1]}");
                                }
                            }
                        }
                    }
                    else if (roleStr.Contains("role:slave"))
                    {
                        role = "Slave";
                        Console.WriteLine($"    Rol: {role}");
                        
                        // Slave için master bağlantı durumunu göster
                        if (roleStr.Contains("master_link_status:"))
                        {
                            var statusLine = roleStr.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None)
                                .FirstOrDefault(line => line.StartsWith("master_link_status:"));
                            
                            if (statusLine != null)
                            {
                                var linkStatus = statusLine.Split(':')[1];
                                Console.WriteLine($"    Master Bağlantı Durumu: {linkStatus}");
                            }
                        }
                        
                        // Slave ip bilgisi
                        if (roleStr.Contains("master_host:"))
                        {
                            var hostLine = roleStr.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None)
                                .FirstOrDefault(line => line.StartsWith("master_host:"));
                            
                            if (hostLine != null)
                            {
                                var masterHost = hostLine.Split(':')[1];
                                Console.WriteLine($"    Master Host: {masterHost}");
                            }
                        }
                        
                        // Slave port bilgisi
                        if (roleStr.Contains("master_port:"))
                        {
                            var portLine = roleStr.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None)
                                .FirstOrDefault(line => line.StartsWith("master_port:"));
                            
                            if (portLine != null)
                            {
                                var masterPort = portLine.Split(':')[1];
                                Console.WriteLine($"    Master Port: {masterPort}");
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"    Sunucu bilgisi alınamadı: {ex.Message}");
                }
            }

            // ------------- PERFORMANS TESTLERİ -------------
            Console.WriteLine("\n========== PERFORMANS TESTLERİ ==========");
            
            int testDataCount = 1000; // Test veri sayısı
            int testIterations = 5;   // Kaç kez test yapılacak
            
            // 1. STANDART VERİ TESTİ
            Console.WriteLine("\n1. STANDART VERİ PERFORMANS TESTİ");
            
            // Test verileri oluştur ve yaz
            Console.WriteLine($"Standart test verilerini yazıyorum ({testDataCount} adet)...");
            var standardData = GenerateTestData("standard:", testDataCount);
            await WriteTestData(db, standardData);
            
            // Replikasyonun tamamlanması için bekle
            Console.WriteLine("Replikasyon için 2 saniye bekleniyor...");
            await Task.Delay(2000);
            
            // Master'dan okuma testi
            Console.WriteLine("\nStandart verileri Master'dan okuma testi:");
            var masterStandardResults = await PerformReadTest(db, standardData.Keys.ToList(), testIterations, CommandFlags.PreferMaster);
            DisplayResults("Master'dan standart okuma", masterStandardResults);
            
            // Slave'den okuma testi
            Console.WriteLine("\nStandart verileri Slave'den okuma testi:");
            var slaveStandardResults = await PerformReadTest(db, standardData.Keys.ToList(), testIterations, CommandFlags.PreferSlave);
            DisplayResults("Slave'den standart okuma", slaveStandardResults);
            
            // 2. KRİTİK VERİ TESTİ
            Console.WriteLine("\n2. KRİTİK VERİ PERFORMANS TESTİ");
            
            // Kritik test verileri oluştur ve yaz
            Console.WriteLine($"Kritik test verilerini yazıyorum ({testDataCount} adet)...");
            var criticalData = GenerateTestData("critical:", testDataCount);
            await WriteTestData(db, criticalData);
            
            // Replikasyonun tamamlanması için bekle
            Console.WriteLine("Replikasyon için 2 saniye bekleniyor...");
            await Task.Delay(2000);
            
            // Master'dan kritik veri okuma testi
            Console.WriteLine("\nKritik verileri Master'dan okuma testi:");
            var masterCriticalResults = await PerformReadTest(db, criticalData.Keys.ToList(), testIterations, CommandFlags.PreferMaster);
            DisplayResults("Master'dan kritik okuma", masterCriticalResults);
            
            // Slave'den kritik veri okuma testi
            Console.WriteLine("\nKritik verileri Slave'den okuma testi:");
            var slaveCriticalResults = await PerformReadTest(db, criticalData.Keys.ToList(), testIterations, CommandFlags.PreferSlave);
            DisplayResults("Slave'den kritik okuma", slaveCriticalResults);
            
            // 3. ÜÇ SLAVE LOAD BALANCING TESTİ
            Console.WriteLine("\n3. ÜÇ SLAVE LOAD BALANCING TESTİ");
            
            // Her slave'e özel verileri yazma
            var slaveSpecificData = GenerateTestData("slave-test:", 300);
            await WriteTestData(db, slaveSpecificData);
            
            // Replikasyonun tamamlanması için bekle
            Console.WriteLine("Replikasyon için 2 saniye bekleniyor...");
            await Task.Delay(2000);
            
            // Yük dağılımı testi - Çoklu slave'den aynı anda okuma
            await PerformMultiSlaveLoadBalancingTest(connection, db, slaveSpecificData.Keys.ToList());
            
            // 4. KARIŞIK OKUMA YAZMA TESTİ (Gerçek dünya senaryosu)
            Console.WriteLine("\n4. KARIŞIK OKUMA/YAZMA TESTİ (Gerçek dünya senaryosu)");
            
            // Hem kritik hem standart veri içeren karışık test
            Console.WriteLine("Karışık okuma/yazma testi başlıyor...");
            await PerformMixedOperationsTest(db, standardData, criticalData);
            
            // 5. ARTIMLI DEĞER (COUNTER) TESTİ
            Console.WriteLine("\n5. ARTIMLI DEĞER (COUNTER) TESTİ");
            
            // Counter testi
            await PerformCounterTest(db, 1000);
            
            // Bağlantıyı kapat
            connection.Close();
            Console.WriteLine("\nRedis bağlantısı kapatıldı.");
        }
        catch (RedisConnectionException ex)
        {
            Console.WriteLine($"Redis bağlantı hatası: {ex.Message}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Beklenmeyen hata: {ex.Message}");
        }
    }
    
    // Test verisi oluştur
    static Dictionary<string, string> GenerateTestData(string keyPrefix, int count)
    {
        var data = new Dictionary<string, string>();
        for (int i = 0; i < count; i++)
        {
            string key = $"{keyPrefix}{i}";
            // Farklı boyutlarda veri oluştur (gerçekçi olması için)
            string value = $"value-{i}-" + new string('X', 20 + (i % 10) * 10);
            data.Add(key, value);
        }
        return data;
    }
    
    // Test verilerini Redis'e yaz
    static async Task WriteTestData(IDatabase db, Dictionary<string, string> data)
    {
        // Batch olarak yaz
        var batch = db.CreateBatch();
        var tasks = new List<Task>();
        
        foreach (var item in data)
        {
            tasks.Add(batch.StringSetAsync(item.Key, item.Value));
        }
        
        batch.Execute();
        await Task.WhenAll(tasks);
    }
    
    // Okuma testi yap
    static async Task<List<double>> PerformReadTest(IDatabase db, List<string> keys, int iterations, CommandFlags flags)
    {
        var results = new List<double>();
        
        for (int iter = 0; iter < iterations; iter++)
        {
            var stopwatch = Stopwatch.StartNew();
            
            // Batch olarak oku
            var batch = db.CreateBatch();
            var tasks = new List<Task<RedisValue>>();
            
            foreach (var key in keys)
            {
                tasks.Add(batch.StringGetAsync(key, flags));
            }
            
            batch.Execute();
            await Task.WhenAll(tasks);
            
            stopwatch.Stop();
            results.Add(stopwatch.ElapsedMilliseconds);
            
            // İterasyonlar arasında kısa bekle
            await Task.Delay(200);
        }
        
        return results;
    }
    
    // Çoklu slave yük dağılımı testi
    static async Task PerformMultiSlaveLoadBalancingTest(ConnectionMultiplexer connection, IDatabase db, List<string> keys)
    {
        Console.WriteLine("Çoklu slave yük dağılımı testi başlıyor...");
        
        // StackExchange.Redis kütüphanesi otomatik olarak slave'ler arasında yük dağılımı yapar
        // Biz burada farklı okuma stratejileri ile test yapacağız
        
        // 1. Standart slave tercihli okuma (kütüphane otomatik olarak load-balance yapar)
        var stopwatch = Stopwatch.StartNew();
        var standardSlaveReadTasks = new List<Task<RedisValue>>();
        
        foreach (var key in keys)
        {
            standardSlaveReadTasks.Add(db.StringGetAsync(key, CommandFlags.PreferSlave));
        }
        
        await Task.WhenAll(standardSlaveReadTasks);
        stopwatch.Stop();
        Console.WriteLine($"Standart slave tercihli okuma: {stopwatch.ElapsedMilliseconds} ms");
        
        // 2. ReadOnly flag ile okuma (Slave'leri tercih eder)
        stopwatch.Restart();
        var readOnlyTasks = new List<Task<RedisValue>>();
        
        foreach (var key in keys)
        {
            readOnlyTasks.Add(db.StringGetAsync(key, CommandFlags.PreferSlave | CommandFlags.DemandSlave));
        }
        
        await Task.WhenAll(readOnlyTasks);
        stopwatch.Stop();
        Console.WriteLine($"ReadOnly (DemandSlave) flag ile okuma: {stopwatch.ElapsedMilliseconds} ms");
        
        // 3. Paralel yük dağılımı testi
        Console.WriteLine("\nParalel yük dağılımı testi:");
        stopwatch.Restart();
        
        // Verinin 3 parçaya bölünmesi (her slave için)
        int chunkSize = keys.Count / 3;
        
        var chunk1 = keys.Take(chunkSize).ToList();
        var chunk2 = keys.Skip(chunkSize).Take(chunkSize).ToList();
        var chunk3 = keys.Skip(chunkSize * 2).ToList();
        
        // Paralel çalıştır
        var parallelTasks = new List<Task>();
        
        parallelTasks.Add(Task.Run(async () => {
            var batch = db.CreateBatch();
            var batchTasks = new List<Task<RedisValue>>();
            
            foreach (var key in chunk1)
            {
                batchTasks.Add(batch.StringGetAsync(key, CommandFlags.PreferSlave));
            }
            
            batch.Execute();
            await Task.WhenAll(batchTasks);
        }));
        
        parallelTasks.Add(Task.Run(async () => {
            var batch = db.CreateBatch();
            var batchTasks = new List<Task<RedisValue>>();
            
            foreach (var key in chunk2)
            {
                batchTasks.Add(batch.StringGetAsync(key, CommandFlags.PreferSlave));
            }
            
            batch.Execute();
            await Task.WhenAll(batchTasks);
        }));
        
        parallelTasks.Add(Task.Run(async () => {
            var batch = db.CreateBatch();
            var batchTasks = new List<Task<RedisValue>>();
            
            foreach (var key in chunk3)
            {
                batchTasks.Add(batch.StringGetAsync(key, CommandFlags.PreferSlave));
            }
            
            batch.Execute();
            await Task.WhenAll(batchTasks);
        }));
        
        await Task.WhenAll(parallelTasks);
        stopwatch.Stop();
        Console.WriteLine($"Paralel yük dağılımı testi: {stopwatch.ElapsedMilliseconds} ms");
        
        // 4. Slave node'ları direkt erişim testi
        // Bu endpoint'ler yapılandırmanızda belirtilen slave adresleridir
        var endpoints = connection.GetEndPoints();
        
        if (endpoints.Length >= 4) // 1 master + 3 slave
        {
            stopwatch.Restart();
            var directSlaveAccessTasks = new List<Task>();
            
            // Not: İlk endpoint genelde master olduğu için atlıyoruz
            // Bu yaklaşım gerçek projelerde riskli olabilir, burada sadece test amaçlı kullanılıyor
            for (int i = 1; i <= 3 && i < endpoints.Length; i++)
            {
                var endpoint = endpoints[i];
                var server = connection.GetServer(endpoint);
                
                // Slave node üzerinde basit bir komut çalıştır
                directSlaveAccessTasks.Add(Task.Run(() => {
                    server.Execute("PING");
                }));
            }
            
            await Task.WhenAll(directSlaveAccessTasks);
            stopwatch.Stop();
            Console.WriteLine($"Slave node'lara direkt erişim testi: {stopwatch.ElapsedMilliseconds} ms");
        }
    }
    
    // Sonuçları ekrana yazdır
    static void DisplayResults(string testName, List<double> results)
    {
        Console.WriteLine($"{testName} sonuçları:");
        Console.WriteLine($"  Min: {results.Min():0.00} ms");
        Console.WriteLine($"  Max: {results.Max():0.00} ms");
        Console.WriteLine($"  Avg: {results.Average():0.00} ms");
        Console.WriteLine($"  İterasyonlar: {string.Join(", ", results.Select(r => $"{r:0.00} ms"))}");
    }
    
    // Karışık okuma/yazma testi
    static async Task PerformMixedOperationsTest(IDatabase db, Dictionary<string, string> standardData, Dictionary<string, string> criticalData)
    {
        // Standart ve kritik verilerden örneklem al
        var standardKeys = standardData.Keys.Take(100).ToList();
        var criticalKeys = criticalData.Keys.Take(100).ToList();
        
        Console.WriteLine("Karışık okuma/yazma testi başlıyor...");
        var stopwatch = Stopwatch.StartNew();
        
        // 1. Kritik verilerin bir kısmını güncelle (Master'a yazma)
        var writeTasks = new List<Task>();
        for (int i = 0; i < 20; i++)
        {
            string key = criticalKeys[i];
            string newValue = $"updated-critical-{DateTime.Now.Ticks}";
            writeTasks.Add(db.StringSetAsync(key, newValue));
        }
        await Task.WhenAll(writeTasks);
        Console.WriteLine($"Kritik veri güncellemesi: {stopwatch.ElapsedMilliseconds} ms");
        
        // 2. Master'dan kritik verileri oku
        stopwatch.Restart();
        var criticalReadTasks = new List<Task<RedisValue>>();
        foreach (var key in criticalKeys)
        {
            criticalReadTasks.Add(db.StringGetAsync(key, CommandFlags.PreferMaster));
        }
        await Task.WhenAll(criticalReadTasks);
        Console.WriteLine($"Master'dan kritik veri okuma: {stopwatch.ElapsedMilliseconds} ms");
        
        // 3. Slave'den standart verileri oku
        stopwatch.Restart();
        var standardReadTasks = new List<Task<RedisValue>>();
        foreach (var key in standardKeys)
        {
            standardReadTasks.Add(db.StringGetAsync(key, CommandFlags.PreferSlave));
        }
        await Task.WhenAll(standardReadTasks);
        Console.WriteLine($"Slave'den standart veri okuma: {stopwatch.ElapsedMilliseconds} ms");
        
        // 4. Eşzamanlı karışık işlemler (gerçek dünya senaryosu)
        Console.WriteLine("\nEşzamanlı karışık işlemler başlıyor...");
        stopwatch.Restart();
        
        var mixedTasks = new List<Task>();
        
        // Kritik verileri güncelle
        for (int i = 30; i < 40; i++)
        {
            string key = criticalKeys[i];
            string newValue = $"concurrent-update-{DateTime.Now.Ticks}";
            mixedTasks.Add(db.StringSetAsync(key, newValue));
        }
        
        // Master'dan kritik verileri oku
        for (int i = 0; i < 30; i++)
        {
            string key = criticalKeys[i];
            mixedTasks.Add(db.StringGetAsync(key, CommandFlags.PreferMaster));
        }
        
        // Slave'den standart verileri oku (farklı slave'lerden okuma simülasyonu)
        for (int i = 0; i < 50; i++)
        {
            string key = standardKeys[i];
            // CommandFlags.PreferSlave ile StackExchange.Redis kütüphanesi otomatik olarak 
            // mevcut slave'ler arasında yük dağılımı yapar
            mixedTasks.Add(db.StringGetAsync(key, CommandFlags.PreferSlave));
        }
        
        await Task.WhenAll(mixedTasks);
        Console.WriteLine($"Eşzamanlı karışık işlemler: {stopwatch.ElapsedMilliseconds} ms");
    }
    
    // Counter testi (artımsal değerler için)
    static async Task PerformCounterTest(IDatabase db, int iterations)
    {
        // Counter'ları sıfırla
        await db.StringSetAsync("counter:master", "0");
        await db.StringSetAsync("counter:slave", "0");
        
        Console.WriteLine("Artımsal değer testi başlıyor...");
        
        // Master'da counter artırma
        var masterStopwatch = Stopwatch.StartNew();
        for (int i = 0; i < iterations; i++)
        {
            await db.StringIncrementAsync("counter:master");
        }
        masterStopwatch.Stop();
        
        // Sonuçları kontrol et
        var masterValue = await db.StringGetAsync("counter:master");
        Console.WriteLine($"Master counter: {masterValue}, Süre: {masterStopwatch.ElapsedMilliseconds} ms");
        
        // Slave'in senkronize olması için bekle
        await Task.Delay(2000);
        
        // Her slave'den counter'ı oku
        var slave1Value = await db.StringGetAsync("counter:master", CommandFlags.PreferSlave);
        Console.WriteLine($"Slave'den okunan counter değeri: {slave1Value}");
        
        // İki farklı counter testi (ilki slaveden, ikincisi masterdan okunacak)
        await db.StringSetAsync("test:counter1", "0");
        await db.StringSetAsync("test:counter2", "0");
        
        // İki counter'ı paralel olarak artır
        Console.WriteLine("\nParalel counter artırma testi...");
        var parallelStopwatch = Stopwatch.StartNew();
        
        var tasks = new List<Task>();
        for (int i = 0; i < iterations/2; i++)
        {
            tasks.Add(db.StringIncrementAsync("test:counter1"));
            tasks.Add(db.StringIncrementAsync("test:counter2"));
        }
        
        await Task.WhenAll(tasks);
        parallelStopwatch.Stop();
        
        // Her slave'den counter değerlerini oku
        Console.WriteLine("\nHer slave'den counter değerlerini okuma:");
        var counter1 = await db.StringGetAsync("test:counter1", CommandFlags.PreferSlave);
        var counter2 = await db.StringGetAsync("test:counter2", CommandFlags.PreferMaster);
        
        Console.WriteLine($"Paralel counter artırma tamamlandı: {parallelStopwatch.ElapsedMilliseconds} ms");
        Console.WriteLine($"Counter1 (Slave'den okundu): {counter1}");
        Console.WriteLine($"Counter2 (Master'dan okundu): {counter2}");
    }
}