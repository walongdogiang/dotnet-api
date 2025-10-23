# User Service Benchmarks

Bộ benchmarks để so sánh performance giữa các service implementations của `IUsersSvc`.

## 🎯 Các Service được Benchmark

- **UsersSvc**: In-memory service (baseline)
- **EFUsrsSvc**: Entity Framework service với SQL Server
- **RdsUsrSvc**: Redis cache service với fallback
- **AioUsrSvc**: All-in-One service (EF + Redis cache)

## 📊 Các Loại Benchmarks

### 1. ServiceComparisonBenchmarks
So sánh performance giữa tất cả các service implementations:
- **GetAll**: Lấy tất cả users
- **GetById**: Lấy user theo ID
- **GetByKwd**: Tìm kiếm user theo keyword
- **Create**: Tạo user mới
- **Update**: Cập nhật user
- **Delete**: Xóa user

### 2. CacheBenchmarks
Test cache performance và behavior:
- **Cache Hit vs Miss**: So sánh performance cache hit vs miss
- **Cache Invalidation**: Test cache invalidation performance
- **Cache Rebuild**: Test cache rebuild performance
- **Cache Warm-up**: Test cache warm-up performance
- **Performance Comparison**: So sánh cache vs database performance

### 3. CrudBenchmarks
Test CRUD operations performance:
- **Create Operations**: Test tạo user performance
- **Read Operations**: Test đọc user performance
- **Update Operations**: Test cập nhật user performance
- **Delete Operations**: Test xóa user performance
- **Batch Operations**: Test batch operations performance
- **Complex Operations**: Test complex CRUD cycles

### 4. SimpleBenchmarks
Basic benchmarks cho UsersSvc (in-memory):
- **GetAll**: Lấy tất cả users
- **GetById**: Lấy user theo ID

## 🚀 Cách Chạy Benchmarks

### Chạy Tất Cả Benchmarks
```bash
dotnet run
```

### Chạy Benchmarks Cụ Thể
```bash
# Chạy Simple Benchmarks
dotnet run simple

# Chạy Service Comparison Benchmarks
dotnet run service

# Chạy Cache Benchmarks
dotnet run cache

# Chạy CRUD Benchmarks
dotnet run crud
```

### Chạy với Configuration
```bash
# Release mode (recommended for accurate results)
dotnet run -c Release

# Debug mode (for development)
dotnet run -c Debug
```

## 📈 Kết Quả Benchmark

### Metrics Được Đo
- **Mean**: Thời gian trung bình
- **Error**: Sai số
- **StdDev**: Độ lệch chuẩn
- **Gen 0/1/2**: Garbage collection
- **Allocated**: Memory allocated

### Baseline Comparison
- **UsersSvc** được sử dụng làm baseline (1.00)
- Các service khác được so sánh với baseline
- **Ratio < 1.00**: Nhanh hơn baseline
- **Ratio > 1.00**: Chậm hơn baseline

## 🔍 Phân Tích Kết Quả

### Expected Results

#### Read Operations (GetAll, GetById, GetByKwd)
- **UsersSvc**: Fastest (in-memory)
- **RdsUsrSvc**: Fast với cache hit, slower với cache miss
- **AioUsrSvc**: Fast với cache hit, slower với cache miss
- **EFUsrsSvc**: Slowest (database access)

#### Write Operations (Create, Update, Delete)
- **UsersSvc**: Fastest (in-memory)
- **EFUsrsSvc**: Fast (direct database)
- **RdsUsrSvc**: Slower (fallback + cache invalidation)
- **AioUsrSvc**: Slowest (database + cache invalidation)

#### Cache Performance
- **Cache Hit**: Nhanh hơn database access
- **Cache Miss**: Chậm hơn database access (do overhead)
- **Cache Invalidation**: Overhead khi write operations

## 🛠️ Configuration

### appsettings.json
```json
{
  "ConnectionStrings": {
    "Default": "Server=(localdb)\\mssqllocaldb;Database=UserDb;Trusted_Connection=true;MultipleActiveResultSets=true"
  }
}
```

### BenchmarkDotNet Configuration
- **MemoryDiagnoser**: Đo memory usage
- **SimpleJob**: Basic job configuration
- **IterationCount**: Số lần lặp (5-10)
- **WarmupCount**: Số lần warmup (2-3)

## 📝 Notes

### Performance Considerations
1. **Cache Hit Ratio**: Cache chỉ có lợi khi hit ratio cao
2. **Database Connection**: EFUsrsSvc cần database connection
3. **Memory Usage**: Cache services sử dụng nhiều memory hơn
4. **Network Latency**: Redis cache có thể có network latency

### Best Practices
1. **Run in Release mode** để có kết quả chính xác
2. **Close other applications** để tránh interference
3. **Run multiple times** để có kết quả ổn định
4. **Monitor system resources** trong khi chạy benchmarks

### Troubleshooting
1. **Database Connection Issues**: Kiểm tra connection string
2. **Memory Issues**: Tăng heap size nếu cần
3. **Slow Results**: Chạy với ít iterations hơn
4. **Inconsistent Results**: Chạy với nhiều warmup iterations

## 🎯 Use Cases

### Khi Nào Sử Dụng Cache
- **High Read Volume**: Nhiều read operations
- **Low Write Volume**: Ít write operations
- **Expensive Queries**: Queries phức tạp
- **Network Latency**: Database ở xa

### Khi Nào Không Sử Dụng Cache
- **High Write Volume**: Nhiều write operations
- **Real-time Data**: Dữ liệu cần real-time
- **Memory Constraints**: Hạn chế memory
- **Simple Queries**: Queries đơn giản

## 📊 Sample Results

```
| Method                    | Mean      | Error     | StdDev    | Ratio | Gen 0 | Allocated |
|-------------------------- |----------:|----------:|----------:|------:|------:|----------:|
| UsersSvc_GetAll          | 1.234 μs  | 0.012 μs  | 0.011 μs  | 1.00  | 0.001 | 1.2 KB    |
| EFUsrsSvc_GetAll         | 15.67 μs  | 0.156 μs  | 0.146 μs  | 12.70 | 0.002 | 2.1 KB    |
| RdsUsrSvc_GetAll         | 2.45 μs   | 0.024 μs  | 0.022 μs  | 1.99  | 0.001 | 1.5 KB    |
| AioUsrSvc_GetAll         | 3.12 μs   | 0.031 μs  | 0.029 μs  | 2.53  | 0.001 | 1.8 KB    |
```

## 🔗 Related Files

- `ServiceComparisonBenchmarks.cs`: So sánh tất cả services
- `CacheBenchmarks.cs`: Test cache performance
- `CrudBenchmarks.cs`: Test CRUD operations
- `SimpleBenchmarks.cs`: Basic benchmarks
- `Program.cs`: Entry point với command line options
