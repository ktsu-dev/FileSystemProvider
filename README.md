# ktsu.FileSystemProvider

[![NuGet](https://img.shields.io/nuget/v/ktsu.FileSystemProvider.svg)](https://www.nuget.org/packages/ktsu.FileSystemProvider/)
[![Build Status](https://github.com/ktsu-dev/FileSystemProvider/workflows/CI/badge.svg)](https://github.com/ktsu-dev/FileSystemProvider/actions)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

A clean, dependency injection-first provider for filesystem access in .NET applications using `System.IO.Abstractions`.

## ✨ Features

- **🔧 Thread-Safe**: Uses `AsyncLocal<T>` for safe concurrent access across async contexts
- **🧪 Testable**: Factory pattern for creating isolated mock filesystems in tests
- **⚡ Lazy Initialization**: Default filesystem instance is created only when needed
- **🎯 Clean API**: Single interface focused on dependency injection
- **🔄 Context Isolation**: Each async context gets its own filesystem instance when testing
- **🏭 Testing-Focused**: Factory pattern specifically designed for test isolation
- **🛡️ Production Safe**: Prevents accidental test mode usage in production environments
- **📦 Zero Configuration**: Works out of the box with sensible defaults
- **🔗 DI Integration**: Built for Microsoft.Extensions.DependencyInjection

## 🚀 Quick Start

### Installation

```bash
dotnet add package ktsu.FileSystemProvider
```

### Basic Setup

```csharp
using Microsoft.Extensions.DependencyInjection;
using ktsu.FileSystemProvider;

// Register services
var services = new ServiceCollection();
services.AddFileSystemProvider();
services.AddTransient<DocumentService>();
var serviceProvider = services.BuildServiceProvider();
```

### Basic Service

```csharp
public class DocumentService
{
    private readonly IFileSystemProvider _fileSystemProvider;

    public DocumentService(IFileSystemProvider fileSystemProvider)
    {
        _fileSystemProvider = fileSystemProvider;
    }

    public void SaveDocument(string path, string content)
    {
        _fileSystemProvider.Current.File.WriteAllText(path, content);
    }

    public string LoadDocument(string path)
    {
        return _fileSystemProvider.Current.File.ReadAllText(path);
    }
}
```

## 📖 API Reference

### IFileSystemProvider Interface

#### Properties
- **`Current`** - Gets the current filesystem instance (IFileSystem)
- **`IsInTestMode`** - Gets whether the provider is currently in test mode (i.e., a factory has been set)

#### Methods
- **`SetFileSystemFactory(Func<IFileSystem> factory)`** - Sets a factory for creating test filesystem instances
- **`ResetToDefault()`** - Resets to the default production filesystem

### Extension Methods

#### ServiceCollection Extensions
- **`AddFileSystemProvider()`** - Registers FileSystemProvider as singleton
- **`AddFileSystemProvider(FileSystemProviderOptions options)`** - Registers FileSystemProvider with configuration options
- **`AddFileSystemProvider(Action<FileSystemProviderOptions> configureOptions)`** - Registers FileSystemProvider with configuration action
- **`AddFileSystemProvider(Func<IServiceProvider, IFileSystemProvider> factory)`** - Registers with custom factory

### Configuration Options

#### FileSystemProviderOptions
- **`ThrowOnTestModeInProduction`** (bool, default: `true`) - Whether to throw an exception when test mode is used in production environments

## 💼 Production Usage

### Service Registration

```csharp
// Program.cs or Startup.cs
var services = new ServiceCollection();

// Register FileSystemProvider (default configuration)
services.AddFileSystemProvider();

// Or register with custom configuration
services.AddFileSystemProvider(options =>
{
    options.ThrowOnTestModeInProduction = false; // Allow test mode in production (not recommended)
});

// Or register with options object
var options = new FileSystemProviderOptions
{
    ThrowOnTestModeInProduction = true // Default: true
};
services.AddFileSystemProvider(options);

// Register your services
services.AddTransient<DocumentService>();
services.AddScoped<FileProcessor>();

var serviceProvider = services.BuildServiceProvider();
```

### File Processing Service

```csharp
public class FileProcessorService
{
    private readonly IFileSystemProvider _fileSystemProvider;
    private readonly ILogger<FileProcessorService> _logger;

    public FileProcessorService(
        IFileSystemProvider fileSystemProvider,
        ILogger<FileProcessorService> logger)
    {
        _fileSystemProvider = fileSystemProvider;
        _logger = logger;
    }

    public void ProcessFiles(string directoryPath)
    {
        var files = _fileSystemProvider.Current.Directory.GetFiles(directoryPath);
        foreach (var file in files)
        {
            var content = _fileSystemProvider.Current.File.ReadAllText(file);
            // Process file content...
            _logger.LogInformation("Processed {FileName}", file);
        }
    }
}
```

### Async Operations

```csharp
public class AsyncFileProcessor
{
    private readonly IFileSystemProvider _fileSystemProvider;
    private readonly ILogger<AsyncFileProcessor> _logger;

    public AsyncFileProcessor(
        IFileSystemProvider fileSystemProvider,
        ILogger<AsyncFileProcessor> logger)
    {
        _fileSystemProvider = fileSystemProvider;
        _logger = logger;
    }

    public async Task ProcessDirectoryAsync(string directoryPath)
    {
        try
        {
            var files = _fileSystemProvider.Current.Directory.GetFiles(directoryPath, "*.txt");
            
            foreach (var file in files)
            {
                _logger.LogInformation("Processing file: {FileName}", file);
                
                var content = await _fileSystemProvider.Current.File.ReadAllTextAsync(file);
                var processedContent = content.ToUpperInvariant();
                
                var outputFile = Path.ChangeExtension(file, ".processed.txt");
                await _fileSystemProvider.Current.File.WriteAllTextAsync(outputFile, processedContent);
                
                _logger.LogInformation("Completed processing: {FileName}", file);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing directory: {DirectoryPath}", directoryPath);
            throw;
        }
    }
}
```

### Complex Dependencies

```csharp
public class DocumentProcessor
{
    private readonly IFileSystemProvider _fileSystemProvider;
    private readonly ILogger<DocumentProcessor> _logger;
    private readonly IConfiguration _configuration;

    public DocumentProcessor(
        IFileSystemProvider fileSystemProvider,
        ILogger<DocumentProcessor> logger,
        IConfiguration configuration)
    {
        _fileSystemProvider = fileSystemProvider;
        _logger = logger;
        _configuration = configuration;
    }

    public async Task ProcessDocumentsAsync()
    {
        var inputPath = _configuration["DocumentProcessor:InputPath"];
        var outputPath = _configuration["DocumentProcessor:OutputPath"];
        
        var files = _fileSystemProvider.Current.Directory.GetFiles(inputPath, "*.txt");
        
        foreach (var file in files)
        {
            _logger.LogInformation("Processing {FileName}", file);
            
            var content = await _fileSystemProvider.Current.File.ReadAllTextAsync(file);
            var processed = ProcessContent(content);
            
            var outputFile = Path.Combine(outputPath, Path.GetFileName(file));
            await _fileSystemProvider.Current.File.WriteAllTextAsync(outputFile, processed);
        }
    }
    
    private string ProcessContent(string content) => content.ToUpperInvariant();
}
```

## 🧪 Testing

### Basic Unit Test

```csharp
using System.Collections.Generic;
using System.IO.Abstractions.TestingHelpers;
using ktsu.FileSystemProvider;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public class DocumentServiceTests
{
    [TestMethod]
    public void SaveDocument_CreatesFile_Successfully()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddFileSystemProvider();
        services.AddTransient<DocumentService>();
        
        using var serviceProvider = services.BuildServiceProvider();
        
        var provider = serviceProvider.GetRequiredService<IFileSystemProvider>();
        provider.SetFileSystemFactory(() => new MockFileSystem());
        
        // Act
        var documentService = serviceProvider.GetRequiredService<DocumentService>();
        documentService.SaveDocument("test.txt", "Hello World!");
        
        // Assert
        var content = provider.Current.File.ReadAllText("test.txt");
        Assert.AreEqual("Hello World!", content);
        
        // Cleanup
        provider.ResetToDefault();
    }
}
```

### Test Setup with Factory

```csharp
[TestClass]
public class DocumentServiceTests
{
    private IServiceProvider _serviceProvider = null!;
    private IFileSystemProvider _fileSystemProvider = null!;

    [TestInitialize]
    public void Setup()
    {
        var services = new ServiceCollection();
        services.AddFileSystemProvider();
        services.AddTransient<DocumentService>();
        
        _serviceProvider = services.BuildServiceProvider();
        _fileSystemProvider = _serviceProvider.GetRequiredService<IFileSystemProvider>();
        
        // Set up mock filesystem for all tests
        _fileSystemProvider.SetFileSystemFactory(() => new MockFileSystem());
    }

    [TestCleanup]
    public void Cleanup()
    {
        _fileSystemProvider.ResetToDefault();
        _serviceProvider.Dispose();
    }

    [TestMethod]
    public void LoadDocument_ReturnsContent_WhenFileExists()
    {
        // Arrange
        _fileSystemProvider.Current.File.WriteAllText("test.txt", "Test Content");
        var documentService = _serviceProvider.GetRequiredService<DocumentService>();
        
        // Act
        var content = documentService.LoadDocument("test.txt");
        
        // Assert
        Assert.AreEqual("Test Content", content);
    }

    [TestMethod]
    public void ProcessFiles_HandlesMultipleFiles_Successfully()
    {
        // Arrange
        _fileSystemProvider.Current.File.WriteAllText("C:\\data\\file1.txt", "Content 1");
        _fileSystemProvider.Current.File.WriteAllText("C:\\data\\file2.txt", "Content 2");
        
        var documentService = _serviceProvider.GetRequiredService<DocumentService>();
        
        // Act
        documentService.ProcessFiles(); // This should not throw
        
        // Assert
        Assert.IsTrue(_fileSystemProvider.Current.File.Exists("C:\\data\\file1.txt"));
        Assert.IsTrue(_fileSystemProvider.Current.File.Exists("C:\\data\\file2.txt"));
    }
}
```

### Testing with Pre-populated FileSystem

```csharp
[TestMethod]
public void ProcessExistingFiles_Works()
{
    // Arrange
    var mockFileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
    {
        { "C:\\data\\document1.txt", new MockFileData("Document 1 content") },
        { "C:\\data\\document2.txt", new MockFileData("Document 2 content") },
        { "C:\\config\\settings.json", new MockFileData("{\"setting\": \"value\"}") }
    });
    
    var services = new ServiceCollection();
    services.AddFileSystemProvider();
    services.AddTransient<DocumentService>();
    
    using var serviceProvider = services.BuildServiceProvider();
    
    var provider = serviceProvider.GetRequiredService<IFileSystemProvider>();
    provider.SetFileSystemFactory(() => mockFileSystem);
    
    // Act
    var documentService = serviceProvider.GetRequiredService<DocumentService>();
    var content = documentService.LoadDocument("C:\\data\\document1.txt");
    
    // Assert
    Assert.AreEqual("Document 1 content", content);
    
    // Cleanup
    provider.ResetToDefault();
}
```

### Parallel Test Isolation

```csharp
[TestMethod]
public void ParallelTests_AreIsolated()
{
    // Arrange
    var provider = new FileSystemProvider();
    provider.SetFileSystemFactory(() => new MockFileSystem());

    // Act - Run parallel tests
    Parallel.For(0, 10, i =>
    {
        var fileSystem = provider.Current;
        fileSystem.File.WriteAllText($"test{i}.txt", $"content{i}");
        
        // Each parallel execution gets its own MockFileSystem
        var mockFS = (MockFileSystem)provider.Current;
        Assert.IsTrue(mockFS.File.Exists($"test{i}.txt"));
        Assert.AreEqual($"content{i}", mockFS.File.ReadAllText($"test{i}.txt"));
    });
}
```

### Simple Test Pattern

```csharp
[TestClass]
public class MyTests
{
    private IFileSystemProvider _provider = null!;

    [TestInitialize]
    public void Setup()
    {
        _provider = new FileSystemProvider();
        _provider.SetFileSystemFactory(() => new MockFileSystem());
    }

    [TestCleanup]
    public void Cleanup()
    {
        _provider.ResetToDefault();
    }

    [TestMethod]
    public void MyTest()
    {
        // Each test gets its own isolated MockFileSystem
        var fs = _provider.Current;
        fs.File.WriteAllText("test.txt", "content");
        
        // Test your code...
    }
}
```

## 🔧 Advanced Usage

### Custom Factory Registration

```csharp
services.AddFileSystemProvider(serviceProvider =>
{
    // Create a custom configured provider
    var provider = new FileSystemProvider();
    
    // You could configure it here if needed
    // provider.SetFileSystemFactory(() => customFileSystem);
    
    return provider;
});
```

### Quick Testing Pattern

```csharp
[TestMethod]
public void QuickTest()
{
    // Arrange
    var provider = new FileSystemProvider(new FileSystemProviderOptions 
    { 
        ThrowOnTestModeInProduction = false 
    });
    
    Assert.IsFalse(provider.IsInTestMode);
    
    provider.SetFileSystemFactory(() => new MockFileSystem(new Dictionary<string, MockFileData>
    {
        { "test.txt", new MockFileData("Hello World") }
    }));
    
    Assert.IsTrue(provider.IsInTestMode);
    
    // Act
    var content = provider.Current.File.ReadAllText("test.txt");
    
    // Assert
    Assert.AreEqual("Hello World", content);
    
    // Cleanup
    provider.ResetToDefault();
    Assert.IsFalse(provider.IsInTestMode);
}
```

## 🏗️ Implementation Details

### Production Safety
By default, the library prevents test mode from being enabled in production environments. This is controlled by the `ThrowOnTestModeInProduction` setting (default: `true`). The library detects production environments by checking:

- Whether a debugger is attached (`Debugger.IsAttached`)
- Environment variables: `ASPNETCORE_ENVIRONMENT`, `DOTNET_ENVIRONMENT`, `ENVIRONMENT`
- Values considered non-production: "Development", "Test", "Testing" (case-insensitive)

```csharp
// This will throw InvalidOperationException in production:
provider.SetFileSystemFactory(() => new MockFileSystem());

// To allow test mode in production (not recommended):
var provider = new FileSystemProvider(new FileSystemProviderOptions 
{ 
    ThrowOnTestModeInProduction = false 
});
```

### Lazy Initialization
The default filesystem instance is created using `Lazy<T>` to ensure thread-safe, one-time initialization:

```csharp
private readonly Lazy<IFileSystem> _defaultInstance = new(() => new FileSystem());
```

### Async Context Isolation
Each async context gets its own filesystem instance when using test factories:

```csharp
private Func<IFileSystem>? _testFactory; // Shared across all contexts
private AsyncLocal<IFileSystem?> _asyncLocalCache = new(); // Cached per context
```

### Singleton Registration
The provider is registered as a singleton in the DI container, but test factories create isolated instances per async context for proper test isolation.

## 📋 Best Practices

### 1. Service Registration
```csharp
// Program.cs or Startup.cs
var services = new ServiceCollection();

// Register FileSystemProvider
services.AddFileSystemProvider();

// Register your services
services.AddTransient<DocumentService>();
services.AddScoped<FileProcessor>();

var serviceProvider = services.BuildServiceProvider();
```

### 2. Constructor Injection
```csharp
public class DocumentService
{
    private readonly IFileSystemProvider _fileSystemProvider;
    
    public DocumentService(IFileSystemProvider fileSystemProvider)
    {
        _fileSystemProvider = fileSystemProvider;
    }
    
    public void ProcessFile(string path)
    {
        var content = _fileSystemProvider.Current.File.ReadAllText(path);
        // Process content...
    }
}
```

### 3. Test Setup
```csharp
[TestClass]
public class MyTests
{
    private IServiceProvider _serviceProvider = null!;
    private IFileSystemProvider _fileSystemProvider = null!;

    [TestInitialize]
    public void Setup()
    {
        var services = new ServiceCollection();
        services.AddFileSystemProvider();
        services.AddTransient<YourService>();
        
        _serviceProvider = services.BuildServiceProvider();
        _fileSystemProvider = _serviceProvider.GetRequiredService<IFileSystemProvider>();
        
        // Set up mock filesystem for all tests
        _fileSystemProvider.SetFileSystemFactory(() => new MockFileSystem());
    }

    [TestCleanup]
    public void Cleanup()
    {
        _fileSystemProvider.ResetToDefault();
        _serviceProvider.Dispose();
    }

    [TestMethod]
    public void MyTest()
    {
        // Each test gets isolated filesystem instance
        var service = _serviceProvider.GetRequiredService<YourService>();
        // Test your service...
    }
}
```

## 🎯 Design Principles

- **Dependency Injection First**: Built for modern .NET applications
- **No Static State**: Avoids global state and service locator anti-patterns
- **Test Isolation**: Each test/async context gets its own filesystem
- **Simple Interface**: Single responsibility with minimal surface area
- **Thread Safety**: Safe for concurrent use across multiple threads

## 📝 Quick Reference

1. **Register as singleton**: Use `services.AddFileSystemProvider()` to register as singleton
2. **Inject interface**: Always inject `IFileSystemProvider` in constructors
3. **Use Current property**: Access filesystem through `provider.Current`
4. **Test with factories**: Use `SetFileSystemFactory()` for testing with mock filesystems
5. **Clean up tests**: Call `ResetToDefault()` in test cleanup to restore production filesystem

## 🔧 Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

## 📄 License

This project is licensed under the MIT License - see the [LICENSE.md](LICENSE.md) file for details.
